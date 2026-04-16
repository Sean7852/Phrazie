@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  Phrazie — clean build + release packager
::  Usage:  release.bat [version]
::  Example: release.bat 0.2.0
::  Default version: 0.1.0
:: ============================================================

set VERSION=%~1
if "%VERSION%"=="" set VERSION=0.1.0

set RELEASE_NAME=Phrazie-v%VERSION%-win-x64
set OUT_DIR=publish\%RELEASE_NAME%
set ZIP_PATH=publish\%RELEASE_NAME%.zip
set DB_PATH=%LOCALAPPDATA%\Phrazie\phrazie.db

echo.
echo ============================================================
echo  Phrazie Release Builder   v%VERSION%
echo ============================================================
echo.

:: ── 1. Clean bin / obj across all projects ──────────────────
echo [1/5] Cleaning bin and obj folders...
for /d %%P in (src\*) do (
    if exist "%%P\bin" (
        echo   Removing %%P\bin
        rd /s /q "%%P\bin"
    )
    if exist "%%P\obj" (
        echo   Removing %%P\obj
        rd /s /q "%%P\obj"
    )
)

:: ── 2. Remove old publish output ────────────────────────────
echo.
echo [2/5] Clearing previous publish output...
if exist "%OUT_DIR%" rd /s /q "%OUT_DIR%"
if exist "%ZIP_PATH%" del /q "%ZIP_PATH%"
if not exist "publish" mkdir publish

:: ── 3. Reset local database ─────────────────────────────────
echo.
echo [3/5] Resetting local database...
if exist "%DB_PATH%" (
    del /q "%DB_PATH%"
    echo   Deleted %DB_PATH%
) else (
    echo   No database found, skipping.
)

:: ── 4. Publish ───────────────────────────────────────────────
echo.
echo [4/5] Publishing (Release / win-x64 / self-contained)...
dotnet publish src\Phrazie.Desktop\Phrazie.Desktop.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:DebugType=none ^
    -p:DebugSymbols=false ^
    -o "%OUT_DIR%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo  ERROR: dotnet publish failed. Aborting.
    exit /b 1
)

:: ── 5. Zip ───────────────────────────────────────────────────
echo.
echo [5/5] Creating zip: %ZIP_PATH%
powershell -NoProfile -Command ^
    "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force"

if %ERRORLEVEL% neq 0 (
    echo.
    echo  ERROR: Zip failed.
    exit /b 1
)

:: ── Done ─────────────────────────────────────────────────────
echo.
echo ============================================================
echo  Done!
echo  Output : %ZIP_PATH%
for %%F in ("%ZIP_PATH%") do echo  Size   : %%~zF bytes
echo ============================================================
echo.
