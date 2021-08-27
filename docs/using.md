# Using the Kiota tool

## How to install the tool

There are a variety of ways to install this tool:

- you can install Kiota as a [dotnet tool](generators/tool.md) from the GitHub packages feed.
- you can run Kiota in a [Docker container](generator/docker.md).
- you can build your own [Kiota executable](generator/build.md).
- you can download a Kiota executable from the releases page.

## Command line options

```text
Usage:
  kiota [options]

Options:
  -o, --output <output>                                     The output directory path for the generated code
                                                            files. [default: ./output]
  -l, --language                                            The target language for the generated code files.
  <CSharp|Go|Java|PHP|Python|Ruby|TypeScript>               [default: CSharp]
  -d, --openapi <openapi>                                   The path to the OpenAPI description file used to
                                                            generate the code files. [default: openapi.yml]
  -b, --backing-store <backing-store>                       The fully qualified name for the backing store class
                                                            to use.
  -c, --class-name <class-name>                             The class name to use for the core client class.
                                                            [default: ApiClient]
  --ll, --loglevel                                          The log level to use when logging messages to the main
  <Critical|Debug|Error|Information|None|Trace|Warning>     output. [default: Warning]
  -n, --namespace-name <namespace-name>                     The namespace to use for the core client class
                                                            specified with the --class-name option. [default:
                                                            ApiClient]
  -s, --serializer <serializer>                             The fully qualified class names for serializers.
                                                            [default:
                                                            System.Collections.Generic.List`1[System.String]]
  --deserializer, --ds <deserializer>                       The fully qualified class names for deserializers.
                                                            [default:
                                                            System.Collections.Generic.List`1[System.String]]
  --version                                                 Show version information
  -?, -h, --help                                            Show help and usage information
```

## Examples

```text
kiota.exe -d ../msgraph-sdk-powershell/openApiDocs/v1.0/mail.yml --language csharp -o ../somepath -n namespaceprefix
```
