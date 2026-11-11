# Trimming model properties

Property filters reduce the generated model surface and the models referenced only by omitted properties. They are independent of API path/operation filters and apply to client generation, not plugins.

Use repeatable `--include-property` and `--exclude-property` options with case-sensitive OpenAPI component keys and JSON property names:

```shell
kiota generate --openapi api.yaml --language CSharp --output client \
  --include-property 'microsoft.graph.user#displayName' \
  --include-property 'microsoft.graph.user#mail' \
  --exclude-property 'microsoft.graph.group#members'
```

The `component#property` syntax uses OpenAPI names, not generated class/property names or nested paths. The first `#` separates the component from the property name. `*` matches zero or more characters in either half; other characters, including `?` and `.`, are literal.

To avoid repeating a component, supply comma-separated property names to `--include-property`:

```shell
kiota generate --openapi api.yaml --language CSharp --output client \
  --include-property 'microsoft.graph.entity#id' \
  --include-property 'microsoft.graph.user#displayName,mail,userPrincipalName,preferredLanguage'
```

Repeat the option for multiple entries, as with `--include-path`; separate space-delimited values after one option are not supported. Whitespace around comma-separated property names is trimmed, and empty names or malformed groups are rejected.

For example:

```shell
kiota generate --openapi api.yaml --language CSharp --output client \
  --include-property 'microsoft.graph.*#display*,mail' \
  --exclude-property 'microsoft.graph.user#preferred*'
```

Comma lists are expanded before persistence; `*` patterns are stored and evaluated again against the description on regeneration. To select a literal name containing a comma or significant surrounding whitespace, configure a full selector directly in the workspace `includeProperties` array or builder configuration instead.

## Selection rules

- An include filter retains only the selected properties of each matched component. A component is filtered only when it owns at least one matching property; components with no matching property are unchanged.
- Exclusions remove individual properties. When both filters name an ordinary property, exclusion wins.
- Required and discriminator properties are always retained, including constraints applied through composition or inheritance. Omitting one from an include filter produces an informational diagnostic; excluding one fails generation even when matched by `*`.
- Unknown components/properties, patterns matching nothing, and malformed selectors fail generation rather than being ignored.
- Named components and their inline composition fragments are supported. Select an inherited property on its declaring base component, not on a derived component. Standalone inline-object members and external component aliases are not selectable.
- Selection is global per component: all endpoints using a model see the same generated surface. Enum members, composition branches, and whole models cannot be explicitly selected.
- Removing the final optional property preserves the object's union branch and inheritance relationship; it does not turn the schema into an unconstrained placeholder.

For example, excluding `microsoft.graph.user#profile` can remove a `profile` model referenced only by that property. A model used by another operation, retained property, or required inheritance/discriminator relationship remains.

## Saved clients and regeneration

The selections are saved as `includeProperties` and `excludeProperties` in client workspace configuration and legacy lock files. Both default to empty arrays for existing clients.

```json
{
  "includeProperties": ["microsoft.graph.user#displayName"],
  "excludeProperties": ["microsoft.graph.group#members"]
}
```

The client add/edit commands accept the same property options. Omitting options on edit keeps existing selections; supplying `--include-property` replaces saved includes with the union of its supplied entries. Comma groups are expanded into component/property selectors; star patterns remain stored as patterns. To clear both sets, use:

```shell
kiota client edit --client-name GraphClient --clear-property-filters
```

You can combine clearing with new property options, or clear selections by setting the saved arrays to `[]`. Update/client generation preserves saved selections. Changing a selection triggers regeneration; reordering identical selections does not.

Use a fresh output directory or the existing `--clean-output` option when checking which generated files disappeared. The property filter does not automatically delete stale files or modify the original OpenAPI description.

## Runtime behavior

This feature trims generated code, not API payloads. It does not add `$select` or change the request URL, permissions, or fields returned by the service.

The existing AdditionalData setting is unchanged. With AdditionalData enabled, fields omitted from the typed model can still be captured in `AdditionalData` and serialized again. With it disabled, unknown fields are not captured there.

Removing optional request properties also removes the ability to set them through typed members. Protected fields, discriminator mappings, and shared dependencies can limit size reduction.
