@echo off
setlocal
echo ========================================================
echo       PEXORIS PORTABLE TOOLS ECOSYSTEM - 1-CLICK BUILD
echo ========================================================
echo.

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo [ERROR] C# Compiler not found at: %CSC%
    exit /b 1
)

if not exist "build" mkdir build

taskkill /F /IM PexorisFileUnlocker.exe 2>nul
taskkill /F /IM PexorisPortKiller.exe 2>nul

echo [1/4] Compiling Tool 1: Pexoris FileUnlocker...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisFileUnlocker\App.manifest ^
    /win32icon:src\PexorisFileUnlocker\Assets\app.ico ^
    /out:build\PexorisFileUnlocker.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisFileUnlocker\Program.cs ^
    src\PexorisFileUnlocker\Theme.cs ^
    src\PexorisFileUnlocker\RestartManager.cs ^
    src\PexorisFileUnlocker\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris FileUnlocker!
    exit /b %ERRORLEVEL%
)

echo [2/4] Packaging Pexoris FileUnlocker Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisFileUnlocker.exe' -DestinationPath 'build\PexorisFileUnlocker-v1.0-Portable.zip' -Force"

echo [3/4] Compiling Tool 2: Pexoris PortKiller...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisPortKiller\App.manifest ^
    /win32icon:src\PexorisPortKiller\Assets\app.ico ^
    /out:build\PexorisPortKiller.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisPortKiller\Program.cs ^
    src\PexorisPortKiller\Theme.cs ^
    src\PexorisPortKiller\SocketHelper.cs ^
    src\PexorisPortKiller\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris PortKiller!
    exit /b %ERRORLEVEL%
)

echo [4/6] Packaging Pexoris PortKiller Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisPortKiller.exe' -DestinationPath 'build\PexorisPortKiller-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisDoHSwitcher.exe 2>nul

echo [5/6] Compiling Tool 3: Pexoris DoH Switcher...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisDoHSwitcher\App.manifest ^
    /win32icon:src\PexorisDoHSwitcher\Assets\app.ico ^
    /out:build\PexorisDoHSwitcher.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisDoHSwitcher\Program.cs ^
    src\PexorisDoHSwitcher\Theme.cs ^
    src\PexorisDoHSwitcher\DnsHelper.cs ^
    src\PexorisDoHSwitcher\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris DoH Switcher!
    exit /b %ERRORLEVEL%
)

echo [6/8] Packaging Pexoris DoH Switcher Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisDoHSwitcher.exe' -DestinationPath 'build\PexorisDoHSwitcher-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisPrintFixer.exe 2>nul

echo [7/8] Compiling Tool 4: Pexoris PrintFixer...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisPrintFixer\App.manifest ^
    /win32icon:src\PexorisPrintFixer\Assets\app.ico ^
    /out:build\PexorisPrintFixer.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll,System.Management.dll,System.ServiceProcess.dll ^
    src\PexorisPrintFixer\Program.cs ^
    src\PexorisPrintFixer\Theme.cs ^
    src\PexorisPrintFixer\SpoolerHelper.cs ^
    src\PexorisPrintFixer\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris PrintFixer!
    exit /b %ERRORLEVEL%
)

echo [8/8] Packaging Pexoris PrintFixer Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisPrintFixer.exe' -DestinationPath 'build\PexorisPrintFixer-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisAIShield.exe 2>nul

echo [9/10] Compiling Tool 5: Pexoris AIShield...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisAIShield\App.manifest ^
    /win32icon:src\PexorisAIShield\Assets\app.ico ^
    /out:build\PexorisAIShield.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisAIShield\Program.cs ^
    src\PexorisAIShield\Theme.cs ^
    src\PexorisAIShield\AIShieldHelper.cs ^
    src\PexorisAIShield\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris AIShield!
    exit /b %ERRORLEVEL%
)

echo [10/10] Packaging Pexoris AIShield Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisAIShield.exe' -DestinationPath 'build\PexorisAIShield-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisUSBShield.exe 2>nul

echo [11/12] Compiling Tool 6: Pexoris USBShield...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisUSBShield\App.manifest ^
    /win32icon:src\PexorisUSBShield\Assets\app.ico ^
    /out:build\PexorisUSBShield.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisUSBShield\Program.cs ^
    src\PexorisUSBShield\Theme.cs ^
    src\PexorisUSBShield\USBShieldHelper.cs ^
    src\PexorisUSBShield\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris USBShield!
    exit /b %ERRORLEVEL%
)

echo [12/12] Packaging Pexoris USBShield Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisUSBShield.exe' -DestinationPath 'build\PexorisUSBShield-v1.0-Portable.zip' -Force"

if not exist "final softwere for g drive uplod" mkdir "final softwere for g drive uplod"
copy /y "build\PexorisFileUnlocker-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisFileUnlocker.zip" >nul
copy /y "build\PexorisPortKiller-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisPortKiller.zip" >nul
copy /y "build\PexorisDoHSwitcher-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisDoHSwitcher.zip" >nul
copy /y "build\PexorisPrintFixer-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisPrintFixer.zip" >nul
copy /y "build\PexorisAIShield-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisAIShield-v1.0-Portable.zip" >nul
copy /y "build\PexorisUSBShield-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisUSBShield-v1.0-Portable.zip" >nul
copy /y "build\PexorisFileUnlocker.exe" "final softwere for g drive uplod\PexorisFileUnlocker.exe" >nul
copy /y "build\PexorisPortKiller.exe" "final softwere for g drive uplod\PexorisPortKiller.exe" >nul
copy /y "build\PexorisDoHSwitcher.exe" "final softwere for g drive uplod\PexorisDoHSwitcher.exe" >nul
copy /y "build\PexorisPrintFixer.exe" "final softwere for g drive uplod\PexorisPrintFixer.exe" >nul
copy /y "build\PexorisAIShield.exe" "final softwere for g drive uplod\PexorisAIShield.exe" >nul
copy /y "build\PexorisUSBShield.exe" "final softwere for g drive uplod\PexorisUSBShield.exe" >nul

echo.
echo ========================================================
echo  [SUCCESS] All 6 Pexoris Tools Compiled Successfully!
echo  Tool 1: build\PexorisFileUnlocker.exe  (70 KB)
echo  Tool 2: build\PexorisPortKiller.exe    (78 KB)
echo  Tool 3: build\PexorisDoHSwitcher.exe  (38 KB)
echo  Tool 4: build\PexorisPrintFixer.exe   (73 KB)
echo  Tool 5: build\PexorisAIShield.exe     (53 KB)
echo  Tool 6: build\PexorisUSBShield.exe    (59 KB)
echo  Output copied to 'final softwere for g drive uplod/'
echo ========================================================
dir build\*.exe | findstr /i "Pexoris"

endlocal
