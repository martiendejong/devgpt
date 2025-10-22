@echo off
REM ==============================================================================
REM DevGPT NuGet Package Publishing Script
REM ==============================================================================
REM This script publishes all NuGet packages to nuget.org
REM
REM Prerequisites:
REM   1. Set NUGET_API_KEY environment variable:
REM      setx NUGET_API_KEY "your-api-key-here"
REM   OR provide it as first argument:
REM      nuget-publish.bat your-api-key
REM
REM   2. Packages must be built in Release mode:
REM      dotnet pack -c Release
REM ==============================================================================

setlocal enabledelayedexpansion

REM Check for API key from argument or environment variable
set "API_KEY=%~1"
if "%API_KEY%"=="" set "API_KEY=%NUGET_API_KEY%"

if "%API_KEY%"=="" (
    echo ERROR: NuGet API key not found!
    echo.
    echo Please provide the API key in one of these ways:
    echo   1. Set environment variable: setx NUGET_API_KEY "your-api-key"
    echo   2. Pass as argument: nuget-publish.bat your-api-key
    echo.
    exit /b 1
)

echo.
echo ========================================
echo Publishing DevGPT Packages to NuGet
echo ========================================
echo.

REM List of packages to publish
set "PACKAGES=DevGPT.AgentFactory DevGPT.Classes DevGPT.DocumentStore DevGPT.EmbeddingStore DevGPT.Generator DevGPT.Helpers DevGPT.HuggingFace DevGPT.LLMClient DevGPT.LLMClientTools DevGPT.OpenAI"

set "FAILED_PACKAGES="
set "SUCCESS_COUNT=0"
set "FAIL_COUNT=0"

for %%P in (%PACKAGES%) do (
    echo.
    echo [%%P] Searching for package...

    set "NUPKG_FILE="
    REM Find the .nupkg file (excluding .symbols.nupkg)
    for /f "delims=" %%F in ('dir /b /s "%%P\bin\Release\%%P.*.nupkg" 2^>nul ^| findstr /v "\.symbols\."') do (
        set "NUPKG_FILE=%%F"
    )

    if "!NUPKG_FILE!"=="" (
        echo [%%P] WARNING: Package not found in bin\Release - skipping
        set /a "FAIL_COUNT+=1"
        set "FAILED_PACKAGES=!FAILED_PACKAGES! %%P"
    ) else (
        echo [%%P] Found: !NUPKG_FILE!
        echo [%%P] Publishing...

        dotnet nuget push "!NUPKG_FILE!" --api-key "%API_KEY%" --source https://api.nuget.org/v3/index.json --skip-duplicate

        if errorlevel 1 (
            echo [%%P] FAILED to publish
            set /a "FAIL_COUNT+=1"
            set "FAILED_PACKAGES=!FAILED_PACKAGES! %%P"
        ) else (
            echo [%%P] SUCCESS
            set /a "SUCCESS_COUNT+=1"
        )
    )
)

echo.
echo ========================================
echo Publishing Summary
echo ========================================
echo Successful: %SUCCESS_COUNT%
echo Failed: %FAIL_COUNT%

if not "%FAILED_PACKAGES%"=="" (
    echo.
    echo Failed packages:%FAILED_PACKAGES%
)

echo ========================================
echo.

if %FAIL_COUNT% gtr 0 (
    exit /b 1
)

endlocal
