@echo off
echo Building Deployment Automation GUI...

REM 检查.NET SDK
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET SDK is not installed. Please install .NET 8.0 SDK or later.
    pause
    exit /b 1
)

REM 清理
echo Cleaning previous builds...
dotnet clean

REM 还原包
echo Restoring NuGet packages...
dotnet restore

REM 构建
echo Building project...
dotnet build -c Release

if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

echo Build completed successfully.

REM 发布
echo Publishing application...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish

echo.
echo Application published to: .\publish\DeploymentAutomationGUI.exe
echo.
pause