module github.com/microsoft/kiota/authentication/azure/go

replace github.com/microsoft/kiota/abstractions/go => ../../../abstractions/go
//TODO remove this replace once we "publish" the package

go 1.16

require (
	github.com/Azure/azure-sdk-for-go/sdk/azcore v0.19.0 // indirect
	github.com/Azure/azure-sdk-for-go/sdk/internal v0.7.1 // indirect
	github.com/microsoft/kiota/abstractions/go v0.0.0-20211013091133-b793efa27646 // indirect
	github.com/pkg/browser v0.0.0-20210911075715-681adbf594b8 // indirect
	golang.org/x/crypto v0.0.0-20210921155107-089bfa567519 // indirect
	golang.org/x/net v0.0.0-20211011170408-caeb26a5c8c0 // indirect
	golang.org/x/sys v0.0.0-20211013075003-97ac67df715c // indirect
	golang.org/x/text v0.3.7 // indirect
)
