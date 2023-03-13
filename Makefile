
.DEFAULT_GOAL:=help

.PHONY: help
help:
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

.PHONY: init
init: ## Initialize the .NET solution
	dotnet workload restore
	dotnet restore kiota.sln

publish: ## Locally publish the Kiota CLI executable
	dotnet publish ./src/kiota/kiota.csproj -c Release -p:PublishSingleFile=true -p:PublishReadyToRun=true -o ./publish

.PHONY: it-setup
it-setup: ## Setup the integration tests, usage: make it-setup description="file/url" language=java
	$(eval ADDITIONAL_COMMAND=$(shell ./it/get-additional-arguments.ps1 -descriptionUrl ${description} -language ${language}))
	@echo "Additional command: ${ADDITIONAL_COMMAND}"
	./publish/kiota generate --language ${language} --openapi ${description} ${ADDITIONAL_COMMAND}

.PHONY: it-run
it-run: ## Run the integration tests, usage: make it-run language=java
	$(shell ./it/get-cmd.ps1 -language ${language})

.PHONY: it-clean
it-clean: ## Clean the it tests folder, usage: make it-clean language=java
	$(shell ./it/do-clean.ps1 -language ${language})

.PHONY: it
it: it-setup it-run it-clean ## Prepare and run the integration tests, usage: make it description="file/url" language=java
