@echo off
setlocal
cd /d "%~dp0"

echo Building Cuphead 3-Player Co-op Mod...

set CSC="C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist %CSC% set CSC="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist %CSC% set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

set MANAGED=..\Cuphead_Data\Managed
if not exist "%MANAGED%" set MANAGED=..\..\Cuphead\Cuphead_Data\Managed
if not exist "%MANAGED%" set MANAGED=E:\workspace\Cuphead\Cuphead_Data\Managed
if not exist "%MANAGED%" if defined CUPHEAD_GAME_DIR set MANAGED=%CUPHEAD_GAME_DIR%\Cuphead_Data\Managed

set BEP=..\BepInEx\core
if not exist "%BEP%" set BEP=..\..\Cuphead\BepInEx\core
if not exist "%BEP%" set BEP=E:\workspace\Cuphead\BepInEx\core
if not exist "%BEP%" if defined CUPHEAD_GAME_DIR set BEP=%CUPHEAD_GAME_DIR%\BepInEx\core

if not exist "%MANAGED%\Assembly-CSharp.dll" (
    echo [ERROR] Cannot find Cuphead Managed assemblies!
    echo Please set CUPHEAD_GAME_DIR environment variable or place folder next to Cuphead installation.
    exit /b 1
)

%CSC% /target:library /noconfig /nostdlib /optimize+ ^
  /r:%MANAGED%\mscorlib.dll ^
  /r:%MANAGED%\System.dll ^
  /r:%MANAGED%\System.Core.dll ^
  /r:%MANAGED%\UnityEngine.dll ^
  /r:%MANAGED%\UnityEngine.CoreModule.dll ^
  /r:%MANAGED%\UnityEngine.Physics2DModule.dll ^
  /r:%MANAGED%\UnityEngine.AnimationModule.dll ^
  /r:%MANAGED%\UnityEngine.IMGUIModule.dll ^
  /r:%MANAGED%\UnityEngine.TextRenderingModule.dll ^
  /r:%MANAGED%\UnityEngine.JSONSerializeModule.dll ^
  /r:%MANAGED%\UnityEngine.ImageConversionModule.dll ^
  /r:%MANAGED%\Rewired_Core.dll ^
  /r:%MANAGED%\Assembly-CSharp.dll ^
  /r:%BEP%\BepInEx.dll ^
  /r:%BEP%\0Harmony.dll ^
  /out:Cuphead3PCoop.dll ^
  *.cs

if %errorlevel% equ 0 (
    echo [OK] Build Successful! Output: Cuphead3PCoop.dll
    if exist ..\release (
        copy /Y Cuphead3PCoop.dll ..\release\Cuphead3PCoop.dll >nul
        echo [OK] Copied Cuphead3PCoop.dll to ..\release\
    )
    if exist ..\..\Cuphead\BepInEx\plugins (
        copy /Y Cuphead3PCoop.dll ..\..\Cuphead\BepInEx\plugins\Cuphead3PCoop.dll >nul
        echo [OK] Deployed Cuphead3PCoop.dll to Cuphead\BepInEx\plugins\
    )
) else (
    echo [ERROR] Build failed with error code %errorlevel%!
    exit /b %errorlevel%
)
exit /b 0
