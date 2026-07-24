using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;

namespace Kiota.Builder.PowerShellWrapper;

// Drives the "PowerShellWrapper" generation language: emits one PowerShell cmdlet class per
// selected OpenAPI operation, straight from the OpenApiDocument that KiotaBuilder already
// loaded and filtered with --include-path/--exclude-path. It bypasses the CodeDOM, refiner,
// and writer pipeline, the same way PluginsGenerationService does for plugin output.
public sealed partial class PowerShellWrapperGenerationService
{
    private readonly OpenApiDocument document;
    private readonly GenerationConfiguration config;
    private readonly ILogger logger;

    public PowerShellWrapperGenerationService(OpenApiDocument document, GenerationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);
        this.document = document;
        config = configuration;
        this.logger = logger;
    }

    // One GET operation from the first pass, held until we know whether it pairs with a
    // list/item partner. CollectionValueSchema is the response's "value" array property when
    // the response is a collection, null for a single entity. It is resolved once here so
    // later steps never re-walk the schema.
    private sealed record GetOperationRecord(CmdletNaming Naming, IOpenApiSchema ResponseSchema, IOpenApiSchema? CollectionValueSchema, IReadOnlyList<string> QueryParams)
    {
        public bool IsCollection => CollectionValueSchema is not null;
    }

    public async Task GenerateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ctx = new EmitContext(ClientNamespace: config.ClientNamespaceName);

        Directory.CreateDirectory(config.OutputPath);
        foreach (var stale in Directory.GetFiles(config.OutputPath, "*.g.cs"))
            File.Delete(stale);

        await File.WriteAllTextAsync(Path.Combine(config.OutputPath, "Shared.g.cs"), CmdletEmitter.EmitSharedAuth(ctx), cancellationToken).ConfigureAwait(false);
        LogWroteSharedFile();

        var written = 0;
        var getOperations = new List<GetOperationRecord>();

        foreach (var (pathTemplate, pathItem) in document.Paths)
        {
            foreach (var (httpMethod, operation) in pathItem.Operations ?? new Dictionary<HttpMethod, OpenApiOperation>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip operations the published SDK deliberately does not ship. NamingOverrides
                // holds the citation for each one.
                if (NamingOverrides.IsSuppressed(httpMethod.Method, pathTemplate))
                {
                    LogSuppressedOperation(httpMethod.Method, pathTemplate);
                    continue;
                }

                var pathParams = (operation.Parameters ?? []).Where(p => p.In == ParameterLocation.Path).Select(p => p.Name!).ToList();
                var queryParams = (operation.Parameters ?? []).Where(p => p.In == ParameterLocation.Query).Select(p => p.Name!).ToList();
                var headerParams = (operation.Parameters ?? []).Where(p => p.In == ParameterLocation.Header).Select(p => p.Name!).ToList();

                // Only GET responses need inspecting: the list/item pairing is decided by the
                // response shape, and DELETE returns 204 with no body.
                var responseSchema = httpMethod == HttpMethod.Get
                    ? operation.Responses?["2XX"].Content?["application/json"].Schema
                    : null;
                var collectionValueSchema = responseSchema is not null ? FindProperty(responseSchema, "value") : null;
                var isCollection = collectionValueSchema is not null;

                var operationId = operation.OperationId
                    ?? throw new InvalidOperationException($"Operation at '{pathTemplate}' ({httpMethod}) has no operationId.");
                var opInfo = new OperationInfo(httpMethod.Method, pathTemplate, operationId, pathParams, queryParams, operation.RequestBody is not null, isCollection, headerParams);
                var cmdletNaming = Naming.Resolve(opInfo);

                // GETs are held back: pairing is decided per noun, so it needs every GET first.
                if (httpMethod == HttpMethod.Get)
                {
                    getOperations.Add(new GetOperationRecord(cmdletNaming, responseSchema!, collectionValueSchema, queryParams));
                    continue;
                }

                string source = httpMethod switch
                {
                    _ when httpMethod == HttpMethod.Delete => CmdletEmitter.EmitRemove(cmdletNaming, ctx),
                    _ when httpMethod == HttpMethod.Post => EmitNewFor(cmdletNaming, ctx, operation),
                    _ when httpMethod == HttpMethod.Patch => EmitUpdateFor(cmdletNaming, ctx, operation),
                    _ => throw new NotSupportedException($"No PowerShell wrapper emitter for {httpMethod} {pathTemplate}."),
                };

                written += await WriteCmdletFileAsync(cmdletNaming, source, cancellationToken).ConfigureAwait(false);
            }
        }

        written += await EmitGetOperationsAsync(getOperations, ctx, cancellationToken).ConfigureAwait(false);

        LogWroteFiles(written + 1, config.OutputPath);
    }

    // Pairs a list GET (GET /users/{id}/messages) with its item GET
    // (GET /users/{id}/messages/{message-id}) and presents them as one public Get-MgX cmdlet,
    // matching the published SDK surface. The real work stays in two separate internal cmdlets
    // (the *_List/*_Get classes named by Naming.WithSuffix); the public dispatcher only picks
    // which one to invoke.
    //
    // A pairing is only trusted when it is structurally unambiguous: exactly one collection GET
    // and one single-entity GET share the noun, and the item's path is the list's path plus one
    // trailing id (Users[UserId].Messages -> Users[UserId].Messages[MessageId]). Everything
    // else keeps the standalone shape: singleton navs with no list (GET /users/{id}/calendar),
    // list-only endpoints such as delta queries, or an unexpected same-noun collision.
    private async Task<int> EmitGetOperationsAsync(List<GetOperationRecord> getOperations, EmitContext ctx, CancellationToken cancellationToken)
    {
        var written = 0;
        var consumed = new HashSet<GetOperationRecord>();

        var listsByNoun = getOperations.Where(o => o.IsCollection).ToLookup(o => o.Naming.Noun);
        var itemsByNoun = getOperations.Where(o => !o.IsCollection).ToLookup(o => o.Naming.Noun);

        foreach (var listGroup in listsByNoun)
        {
            if (listGroup.Count() != 1)
                continue;
            var listOp = listGroup.Single();

            var itemGroup = itemsByNoun[listGroup.Key];
            if (itemGroup.Count() != 1)
                continue;
            var itemOp = itemGroup.Single();

            if (itemOp.Naming.PathParamNames.Count != listOp.Naming.PathParamNames.Count + 1)
                continue;
            if (!itemOp.Naming.PathParamNames.Take(listOp.Naming.PathParamNames.Count).SequenceEqual(listOp.Naming.PathParamNames))
                continue;
            if (!itemOp.Naming.BuilderExpression.StartsWith(listOp.Naming.BuilderExpression + "[", StringComparison.Ordinal))
                continue;

            var listEntityType = ResolveListEntityTypeName(listOp.CollectionValueSchema!, ctx.ModelsNamespace);
            var entityType = ResolveEntityTypeName(itemOp.ResponseSchema, ctx.ModelsNamespace);
            var collectionResponseType = listEntityType + "CollectionResponse";

            // The two real implementations: separate, independently documented cmdlets, unchanged
            // from (and reusing) the standalone shapes used for unpaired GETs.
            var internalListNaming = Naming.WithSuffix(listOp.Naming, "_List");
            var internalItemNaming = Naming.WithSuffix(itemOp.Naming, "_Get");
            var internalListSource = CmdletEmitter.EmitListGet(internalListNaming, ctx, listEntityType, collectionResponseType, listOp.QueryParams.ToHashSet());
            var internalItemSource = CmdletEmitter.EmitItemGet(internalItemNaming, ctx, entityType);

            // The thin public dispatcher on top, presenting the merged Get-MgX surface.
            var dispatcherSource = CmdletEmitter.EmitGetDispatcher(listOp.Naming, itemOp.Naming,
                internalListNaming, internalItemNaming, ctx, entityType, collectionResponseType, listOp.QueryParams.ToHashSet());

            written += await WriteCmdletFileAsync(internalListNaming, internalListSource, cancellationToken).ConfigureAwait(false);
            written += await WriteCmdletFileAsync(internalItemNaming, internalItemSource, cancellationToken).ConfigureAwait(false);
            written += await WriteCmdletFileAsync(listOp.Naming, dispatcherSource, cancellationToken).ConfigureAwait(false);
            consumed.Add(listOp);
            consumed.Add(itemOp);
        }

        foreach (var op in getOperations)
        {
            if (consumed.Contains(op))
                continue;

            var source = op.IsCollection
                ? EmitListGetFor(op.Naming, ctx, op.CollectionValueSchema!, op.QueryParams)
                : CmdletEmitter.EmitItemGet(op.Naming, ctx, ResolveEntityTypeName(op.ResponseSchema, ctx.ModelsNamespace));

            written += await WriteCmdletFileAsync(op.Naming, source, cancellationToken).ConfigureAwait(false);
        }

        return written;
    }

    private async Task<int> WriteCmdletFileAsync(CmdletNaming naming, string source, CancellationToken cancellationToken)
    {
        var fileName = naming.ClassName.Replace("Command", "", StringComparison.Ordinal) + ".g.cs";
        await File.WriteAllTextAsync(Path.Combine(config.OutputPath, fileName), source, cancellationToken).ConfigureAwait(false);
        LogWroteCmdletFile(fileName, naming.VerbName, naming.Noun);
        return 1;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Wrote Shared.g.cs")]
    private partial void LogWroteSharedFile();
    [LoggerMessage(Level = LogLevel.Information, Message = "Wrote {FileName} ({Verb}-{Noun})")]
    private partial void LogWroteCmdletFile(string fileName, string verb, string noun);
    [LoggerMessage(Level = LogLevel.Information, Message = "Wrote {Count} file(s) to {OutputPath}")]
    private partial void LogWroteFiles(int count, string outputPath);
    [LoggerMessage(Level = LogLevel.Information, Message = "Suppressed {Method} {PathTemplate}: the published SDK ships no cmdlet for it (see NamingOverrides)")]
    private partial void LogSuppressedOperation(string method, string pathTemplate);

    private static string EmitListGetFor(CmdletNaming naming, EmitContext ctx, IOpenApiSchema collectionValueSchema, IReadOnlyList<string> queryParams)
    {
        var entityType = ResolveListEntityTypeName(collectionValueSchema, ctx.ModelsNamespace);
        // Appending "CollectionResponse" works for bare names ("User") and qualified ones
        // ("Ns.Security.Alert") alike, because Kiota puts the collection model next to the
        // item model.
        return CmdletEmitter.EmitListGet(naming, ctx, entityType, entityType + "CollectionResponse", queryParams.ToHashSet());
    }

    // collectionValueSchema is the already-resolved "value" array property from
    // GetOperationRecord, so nothing is re-walked here.
    private static string ResolveListEntityTypeName(IOpenApiSchema collectionValueSchema, string modelsNamespace)
    {
        var itemSchema = collectionValueSchema.Items
            ?? throw new InvalidOperationException("Expected the 'value' array items to be present.");
        return SchemaNameToTypeName(ResolveReferenceId(itemSchema), modelsNamespace);
    }

    // The old openApiDocs specs declare "value" directly on the schema; the KiotaCompat specs
    // compose it through allOf. Look in both places, recursively for nested composition.
    private static IOpenApiSchema? FindProperty(IOpenApiSchema schema, string propertyName)
    {
        if (schema.Properties?.TryGetValue(propertyName, out var direct) == true)
            return direct;

        foreach (var branch in schema.AllOf ?? [])
        {
            if (FindProperty(branch, propertyName) is { } found)
                return found;
        }

        return null;
    }

    private static string EmitNewFor(CmdletNaming naming, EmitContext ctx, OpenApiOperation operation)
    {
        var bodySchema = operation.RequestBody!.Content!["application/json"].Schema!;
        return CmdletEmitter.EmitNew(naming, ctx, ResolveEntityTypeName(bodySchema, ctx.ModelsNamespace),
            SchemaProperties.ExtractPrimitiveProperties(bodySchema), SchemaProperties.HasPasswordProfile(bodySchema));
    }

    private static string EmitUpdateFor(CmdletNaming naming, EmitContext ctx, OpenApiOperation operation)
    {
        var bodySchema = operation.RequestBody!.Content!["application/json"].Schema!;
        return CmdletEmitter.EmitUpdate(naming, ctx, ResolveEntityTypeName(bodySchema, ctx.ModelsNamespace),
            SchemaProperties.ExtractPrimitiveProperties(bodySchema), SchemaProperties.HasPasswordProfile(bodySchema));
    }

    private static string ResolveEntityTypeName(IOpenApiSchema schema, string modelsNamespace) =>
        SchemaNameToTypeName(ResolveReferenceId(schema), modelsNamespace);

    private static string ResolveReferenceId(IOpenApiSchema schema) =>
        schema.GetReferenceId() is string id && !string.IsNullOrEmpty(id)
            ? id
            : throw new InvalidOperationException("Expected a $ref schema for entity type resolution.");

    private static string SchemaNameToTypeName(string schemaName, string modelsNamespace)
    {
        var name = schemaName.StartsWith("microsoft.graph.", StringComparison.Ordinal)
            ? schemaName["microsoft.graph.".Length..]
            : schemaName;

        // Kiota nests each dot segment as a sub-namespace under Models ("security.alert"
        // becomes Models.Security.Alert). A using directive does not reach into nested
        // namespaces, so multi-segment names are fully qualified; single-segment names,
        // the common case, stay bare.
        var segments = name.Split('.').Select(static segment => char.ToUpperInvariant(segment[0]) + segment[1..]).ToArray();
        return segments.Length == 1 ? segments[0] : $"{modelsNamespace}.{string.Join('.', segments)}";
    }
}
