@echo off
setlocal
title Calradia AI Bridge - Build Script

echo ============================================================
echo         Building Calradia AI Bridge System
echo ============================================================

:: Determine directories (running inside Src)
set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "SRC_DIR=%SCRIPT_DIR%"
for %%I in ("%SCRIPT_DIR%\..") do set "ROOT_DIR=%%~fI"

:: Export to a dedicated "release" folder inside Src
set "OUT_DIR=%SRC_DIR%\release"

:: Project file detection (.NET Framework 4.7.2 only)
if exist "%SRC_DIR%\Ai_bridge_system-4.7.csproj" (
    set "PROJ_FILE=%SRC_DIR%\Ai_bridge_system-4.7.csproj"
) else (
    set "PROJ_FILE=%ROOT_DIR%\Ai_bridge_system-4.7.csproj"
)

echo [INFO] Target Framework: .NET Framework 4.7.2 (net472)
echo [INFO] Project directory: %ROOT_DIR%
echo [INFO] Source directory:  %SRC_DIR%
echo [INFO] Release directory: %OUT_DIR%
echo.

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%" >nul 2>&1

:: Delete and purge any bin folders
if exist "%ROOT_DIR%\bin" rmdir /s /q "%ROOT_DIR%\bin" >nul 2>&1
if exist "%SRC_DIR%\bin" rmdir /s /q "%SRC_DIR%\bin" >nul 2>&1

:: Clean up any duplicate bridge files outside of Src
if exist "%ROOT_DIR%\Ai_bridge_system.exe" del /f /q "%ROOT_DIR%\Ai_bridge_system.exe" >nul 2>&1
if exist "%ROOT_DIR%\updater.bat" del /f /q "%ROOT_DIR%\updater.bat" >nul 2>&1
if exist "%ROOT_DIR%\updater.sh" del /f /q "%ROOT_DIR%\updater.sh" >nul 2>&1
if exist "%ROOT_DIR%\update.sh" del /f /q "%ROOT_DIR%\update.sh" >nul 2>&1
if exist "%ROOT_DIR%\version.txt" del /f /q "%ROOT_DIR%\version.txt" >nul 2>&1

:: ------------------------------------------------------------
:: Method 1: Try building with dotnet CLI (.NET Framework 4.7.2)
:: ------------------------------------------------------------
where dotnet >nul 2>&1
if errorlevel 1 goto :FindMSBuild

echo [BUILD] Compiling .NET 4.7.2 using dotnet CLI into Src\release...
dotnet build "%PROJ_FILE%" -c Release -o "%OUT_DIR%" --nologo
if errorlevel 1 (
    echo [WARN] dotnet build failed. Falling back to MSBuild.
    echo.
    goto :FindMSBuild
)
goto :BuildSuccess

:: ------------------------------------------------------------
:: Method 2: Try locating MSBuild.exe
:: ------------------------------------------------------------
:FindMSBuild
echo [INFO] Searching for MSBuild...
set "MSBUILD_EXE="

:: Check vswhere in ProgramFiles(x86) and ProgramFiles
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=* delims=" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
        if exist "%%~i" set "MSBUILD_EXE=%%~i"
    )
)
if defined MSBUILD_EXE goto :RunMSBuild

if exist "%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=* delims=" %%i in (`"%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
        if exist "%%~i" set "MSBUILD_EXE=%%~i"
    )
)
if defined MSBUILD_EXE goto :RunMSBuild

:: Check standard drives and directories
for %%D in (C D E) do (
    if exist "%%D:\Program Files\vs 2019\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%%D:\Program Files\vs 2019\MSBuild\Current\Bin\MSBuild.exe"
    if exist "%%D:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%%D:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    if exist "%%D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_EXE=%%D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
)
if defined MSBUILD_EXE goto :RunMSBuild

if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" (
    set "MSBUILD_EXE=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    goto :RunMSBuild
)

:RunMSBuild
if not defined MSBUILD_EXE goto :FindCSC

echo [BUILD] Compiling using MSBuild into Src\release...
echo        "%MSBUILD_EXE%"
"%MSBUILD_EXE%" "%PROJ_FILE%" -t:Restore /p:Configuration=Release /verbosity:minimal /nologo >nul 2>&1
"%MSBUILD_EXE%" "%PROJ_FILE%" /p:Configuration=Release "/p:OutputPath=%OUT_DIR%/" /verbosity:minimal /nologo
if errorlevel 1 (
    echo [WARN] MSBuild failed. Falling back to direct C# compiler.
    echo.
    goto :FindCSC
)
goto :BuildSuccess

:: ------------------------------------------------------------
:: Method 3: Try compiling directly with Roslyn / .NET C# compiler (csc.exe)
:: ------------------------------------------------------------
:FindCSC
echo [INFO] Searching for C# compiler...
set "CSC_EXE="

:: If MSBUILD was located, check for Roslyn\csc.exe in the same bin directory
if defined MSBUILD_EXE (
    for %%F in ("%MSBUILD_EXE%") do (
        if exist "%%~dpFRoslyn\csc.exe" (
            set "CSC_EXE=%%~dpFRoslyn\csc.exe"
            goto :RunCSC
        )
    )
)

:: Search drives for Roslyn csc.exe
for %%D in (C D E) do (
    if exist "%%D:\Program Files\vs 2019\MSBuild\Current\Bin\Roslyn\csc.exe" (
        set "CSC_EXE=%%D:\Program Files\vs 2019\MSBuild\Current\Bin\Roslyn\csc.exe"
        goto :RunCSC
    )
    if exist "%%D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
        set "CSC_EXE=%%D:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
        goto :RunCSC
    )
    if exist "%%D:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe" (
        set "CSC_EXE=%%D:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
        goto :RunCSC
    )
)

if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set "CSC_EXE=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    goto :RunCSC
)
if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set "CSC_EXE=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
    goto :RunCSC
)

:RunCSC
if not defined CSC_EXE goto :BuildFailed

echo [BUILD] Compiling directly with csc into Src\release...
echo        "%CSC_EXE%"
"%CSC_EXE%" /target:exe /out:"%OUT_DIR%\Ai_bridge_system.exe" /platform:anycpu /optimize+ /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Net.Http.dll /r:System.Windows.Forms.dll "%SRC_DIR%\Program*.cs" "%SRC_DIR%\AppConfig.cs"
if not errorlevel 1 goto :BuildSuccess

:BuildFailed
echo.
echo ============================================================
echo [ERROR] Build failed!
echo Please ensure either .NET SDK (dotnet CLI) or MSBuild is installed.
echo ============================================================
pause
exit /b 1

:BuildSuccess
:: Clean any temp bin/obj folders that compilers might produce
if exist "%ROOT_DIR%\bin" rmdir /s /q "%ROOT_DIR%\bin" >nul 2>&1
if exist "%SRC_DIR%\bin" rmdir /s /q "%SRC_DIR%\bin" >nul 2>&1
if exist "%SRC_DIR%\obj" rmdir /s /q "%SRC_DIR%\obj" >nul 2>&1

:: Copy runtime configuration files into release (no updater or batch files in release)
if exist "%SRC_DIR%\items.json" (
    copy /y "%SRC_DIR%\items.json" "%OUT_DIR%\" >nul
) else if exist "%ROOT_DIR%\items.json" (
    copy /y "%ROOT_DIR%\items.json" "%OUT_DIR%\" >nul
)
if exist "%SRC_DIR%\npc_characters_prompts.json" (
    copy /y "%SRC_DIR%\npc_characters_prompts.json" "%OUT_DIR%\" >nul
) else if exist "%ROOT_DIR%\npc_characters_prompts.json" (
    copy /y "%ROOT_DIR%\npc_characters_prompts.json" "%OUT_DIR%\" >nul
)

echo.
echo ============================================================
echo [SUCCESS] Build completed successfully!
echo Program files are located inside Src\release:
echo   - %OUT_DIR%\Ai_bridge_system.exe
echo   - %OUT_DIR%\items.json
echo   - %OUT_DIR%\npc_characters_prompts.json
echo ============================================================
echo.

set /p "RUN_NOW=Would you like to run Ai_bridge_system now? [y/N]: "
if /i "%RUN_NOW%"=="Y" (
    start "" "%OUT_DIR%\Ai_bridge_system.exe"
)

exit /b 0
