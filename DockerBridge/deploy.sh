#!/bin/bash

# Deployment Automation GUI - Build Script for Linux/macOS
set -e

echo "=== Deployment Automation GUI Build Script ==="

# 检查.NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed."
    echo "Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "Cleaning previous builds..."
dotnet clean

echo "Restoring NuGet packages..."
dotnet restore

echo "Building project..."
dotnet build -c Release

if [ $? -eq 0 ]; then
    echo "Build successful!"
    
    # 确定目标运行时
    if [[ "$OSTYPE" == "darwin"* ]]; then
        RUNTIME="osx-x64"
    else
        RUNTIME="linux-x64"
    fi
    
    echo "Publishing for $RUNTIME..."
    dotnet publish -c Release -r $RUNTIME --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o ./publish-$RUNTIME
    
    echo ""
    echo "================================================"
    echo "Application published successfully!"
    echo "Location: ./publish-$RUNTIME/DeploymentAutomationGUI"
    echo ""
    echo "To run the application:"
    echo "  cd ./publish-$RUNTIME"
    echo "  ./DeploymentAutomationGUI"
    echo "================================================"
else
    echo "Build failed!"
    exit 1
fi