..\src\kiota\bin\debug\net5.0\kiota.exe --openapi ToDoApi.yaml `
                -o ./CSharp --loglevel information -n Todo -c TodoClient -l CSharp

..\src\kiota\bin\debug\net5.0\kiota.exe --openapi ToDoApi.yaml `
                -o ./TypeScript --loglevel information -n Todo -c TodoClient -l TypeScript

..\src\kiota\bin\debug\net5.0\kiota.exe --openapi ToDoApi.yaml `
                -o ./Java --loglevel information -n Todo -c TodoClient -l Java