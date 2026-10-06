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

taskkill /F /IM PexorisContextMenuEditor.exe 2>nul

echo [13/14] Compiling Tool 7: Pexoris ContextMenuEditor...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisContextMenuEditor\App.manifest ^
    /win32icon:src\PexorisContextMenuEditor\Assets\app.ico ^
    /out:build\PexorisContextMenuEditor.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisContextMenuEditor\Program.cs ^
    src\PexorisContextMenuEditor\Theme.cs ^
    src\PexorisContextMenuEditor\ContextMenuHelper.cs ^
    src\PexorisContextMenuEditor\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris ContextMenuEditor!
    exit /b %ERRORLEVEL%
)

echo [14/14] Packaging Pexoris ContextMenuEditor Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisContextMenuEditor.exe' -DestinationPath 'build\PexorisContextMenuEditor-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisServiceOptimizer.exe 2>nul

echo [15/16] Compiling Tool 8: Pexoris ServiceOptimizer...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisServiceOptimizer\App.manifest ^
    /win32icon:src\PexorisServiceOptimizer\Assets\app.ico ^
    /out:build\PexorisServiceOptimizer.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll,System.ServiceProcess.dll ^
    src\PexorisServiceOptimizer\Program.cs ^
    src\PexorisServiceOptimizer\Theme.cs ^
    src\PexorisServiceOptimizer\ServiceHelper.cs ^
    src\PexorisServiceOptimizer\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris ServiceOptimizer!
    exit /b %ERRORLEVEL%
)

echo [16/16] Packaging Pexoris ServiceOptimizer Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisServiceOptimizer.exe' -DestinationPath 'build\PexorisServiceOptimizer-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisHostsManager.exe 2>nul

echo [17/18] Compiling Tool 9: Pexoris HostsManager...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisHostsManager\App.manifest ^
    /win32icon:src\PexorisHostsManager\Assets\app.ico ^
    /out:build\PexorisHostsManager.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisHostsManager\Program.cs ^
    src\PexorisHostsManager\Theme.cs ^
    src\PexorisHostsManager\HostsHelper.cs ^
    src\PexorisHostsManager\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris HostsManager!
    exit /b %ERRORLEVEL%
)

echo [18/20] Packaging Pexoris HostsManager Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisHostsManager.exe' -DestinationPath 'build\PexorisHostsManager-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisTempCleaner.exe 2>nul

echo [19/20] Compiling Tool 10: Pexoris TempCleaner...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisTempCleaner\App.manifest ^
    /win32icon:src\PexorisTempCleaner\Assets\app.ico ^
    /out:build\PexorisTempCleaner.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisTempCleaner\Program.cs ^
    src\PexorisTempCleaner\Theme.cs ^
    src\PexorisTempCleaner\CleanerHelper.cs ^
    src\PexorisTempCleaner\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris TempCleaner!
    exit /b %ERRORLEVEL%
)

echo [20/22] Packaging Pexoris TempCleaner Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisTempCleaner.exe' -DestinationPath 'build\PexorisTempCleaner-v1.0-Portable.zip' -Force"

taskkill /F /IM PexorisStartupInspector.exe 2>nul

echo [21/22] Compiling Tool 11: Pexoris Startup Inspector...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu ^
    /win32manifest:src\PexorisStartupInspector\App.manifest ^
    /win32icon:src\PexorisStartupInspector\Assets\app.ico ^
    /out:build\PexorisStartupInspector.exe ^
    /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Core.dll ^
    src\PexorisStartupInspector\Program.cs ^
    src\PexorisStartupInspector\Theme.cs ^
    src\PexorisStartupInspector\StartupHelper.cs ^
    src\PexorisStartupInspector\MainForm.cs

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to compile Pexoris Startup Inspector!
    exit /b %ERRORLEVEL%
)

echo [22/22] Packaging Pexoris Startup Inspector Portable ZIP...
powershell -Command "Compress-Archive -Path 'build\PexorisStartupInspector.exe' -DestinationPath 'build\PexorisStartupInspector-v1.0-Portable.zip' -Force"

if not exist "final softwere for g drive uplod" mkdir "final softwere for g drive uplod"
copy /y "build\PexorisFileUnlocker-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisFileUnlocker.zip" >nul
copy /y "build\PexorisPortKiller-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisPortKiller.zip" >nul
copy /y "build\PexorisDoHSwitcher-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisDoHSwitcher.zip" >nul
copy /y "build\PexorisPrintFixer-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisPrintFixer.zip" >nul
copy /y "build\PexorisAIShield-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisAIShield-v1.0-Portable.zip" >nul
copy /y "build\PexorisUSBShield-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisUSBShield-v1.0-Portable.zip" >nul
copy /y "build\PexorisContextMenuEditor-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisContextMenuEditor.zip" >nul
copy /y "build\PexorisServiceOptimizer-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisServiceOptimizer.zip" >nul
copy /y "build\PexorisHostsManager-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisHostsManager.zip" >nul
copy /y "build\PexorisTempCleaner-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisTempCleaner.zip" >nul
copy /y "build\PexorisStartupInspector-v1.0-Portable.zip" "final softwere for g drive uplod\PexorisStartupInspector.zip" >nul
copy /y "build\PexorisFileUnlocker.exe" "final softwere for g drive uplod\PexorisFileUnlocker.exe" >nul
copy /y "build\PexorisPortKiller.exe" "final softwere for g drive uplod\PexorisPortKiller.exe" >nul
copy /y "build\PexorisDoHSwitcher.exe" "final softwere for g drive uplod\PexorisDoHSwitcher.exe" >nul
copy /y "build\PexorisPrintFixer.exe" "final softwere for g drive uplod\PexorisPrintFixer.exe" >nul
copy /y "build\PexorisAIShield.exe" "final softwere for g drive uplod\PexorisAIShield.exe" >nul
copy /y "build\PexorisUSBShield.exe" "final softwere for g drive uplod\PexorisUSBShield.exe" >nul
copy /y "build\PexorisContextMenuEditor.exe" "final softwere for g drive uplod\PexorisContextMenuEditor.exe" >nul
copy /y "build\PexorisServiceOptimizer.exe" "final softwere for g drive uplod\PexorisServiceOptimizer.exe" >nul
copy /y "build\PexorisHostsManager.exe" "final softwere for g drive uplod\PexorisHostsManager.exe" >nul
copy /y "build\PexorisTempCleaner.exe" "final softwere for g drive uplod\PexorisTempCleaner.exe" >nul
copy /y "build\PexorisStartupInspector.exe" "final softwere for g drive uplod\PexorisStartupInspector.exe" >nul

echo.
echo ========================================================
echo  [SUCCESS] All 11 Pexoris Tools Compiled Successfully!
echo  Tool 1:  build\PexorisFileUnlocker.exe        (70 KB)
echo  Tool 2:  build\PexorisPortKiller.exe          (78 KB)
echo  Tool 3:  build\PexorisDoHSwitcher.exe        (38 KB)
echo  Tool 4:  build\PexorisPrintFixer.exe         (73 KB)
echo  Tool 5:  build\PexorisAIShield.exe           (53 KB)
echo  Tool 6:  build\PexorisUSBShield.exe          (59 KB)
echo  Tool 7:  build\PexorisContextMenuEditor.exe  (49 KB)
echo  Tool 8:  build\PexorisServiceOptimizer.exe    (69 KB)
echo  Tool 9:  build\PexorisHostsManager.exe       (57 KB)
echo  Tool 10: build\PexorisTempCleaner.exe       (56 KB)
echo  Tool 11: build\PexorisStartupInspector.exe   (57 KB)
echo  Output copied to 'final softwere for g drive uplod/'
echo ========================================================
dir build\*.exe | findstr /i "Pexoris"

endlocal


