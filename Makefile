.PHONY: build build-releases clean test help

# Get version from project file
VERSION := $(shell grep -oP '<Version>\K[^<]+' src/jot/jot.csproj)

help: ## Show this help message
	@echo "Available targets:"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

build: ## Build the application for current platform
	dotnet build src/jot/jot.csproj --configuration Release

run: ## Run the application locally
	dotnet run --project src/jot/jot.csproj

test: ## Run all tests
	dotnet test

build-releases: ## Build releases for all platforms
	./build-releases.sh $(VERSION)

clean: ## Clean build artifacts
	dotnet clean ./src
	rm -rf ./releases
	rm -rf src/*/bin
	rm -rf src/*/obj

install-deb: ## Install the Debian package locally (requires build-releases first)
	sudo dpkg -i ./releases/jot-$(VERSION).deb

test-package: ## Test the Debian package installation and functionality
	./test-package.sh

# Development targets
dev-build: ## Quick development build
	dotnet build src/jot/jot.csproj

dev-run: ## Run in development mode
	dotnet run --project src/jot/jot.csproj

# Package individual platforms
build-linux: ## Build only Linux release
	dotnet publish src/jot/jot.csproj -c Release -r linux-x64 --self-contained -o ./dist/linux

build-windows: ## Build only Windows release
	dotnet publish src/jot/jot.csproj -c Release -r win-x64 --self-contained -o ./dist/windows

build-macos: ## Build only macOS release
	dotnet publish src/jot/jot.csproj -c Release -r osx-x64 --self-contained -o ./dist/macos