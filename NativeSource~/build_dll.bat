@echo off
setlocal

set MSVC_VER=14.44.35207
set MSVC_BASE=C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\%MSVC_VER%
set WINSDK_VER=10.0.26100.0
set WINSDK_INC=C:\Program Files (x86)\Windows Kits\10\Include\%WINSDK_VER%
set WINSDK_LIB=C:\Program Files (x86)\Windows Kits\10\Lib\%WINSDK_VER%

set PATH=%MSVC_BASE%\bin\Hostx64\x64;%PATH%
set INCLUDE=%MSVC_BASE%\include;%WINSDK_INC%\ucrt;%WINSDK_INC%\um;%WINSDK_INC%\shared
set LIB=%MSVC_BASE%\lib\x64;%WINSDK_LIB%\ucrt\x64;%WINSDK_LIB%\um\x64

set SRC_DIR=%~dp0src
set OUT_DIR=%~dp0..\Plugins\x86_64

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

cd /d "%SRC_DIR%"

echo === Building NataneBrushNative.dll (x64 Release) ===
cl.exe /O2 /MT /EHsc /std:c++17 /LD /I. /DNDEBUG /fp:fast /Fe:"%OUT_DIR%\NataneBrushNative.dll" natane_brush_native.cpp /link /DLL user32.lib comctl32.lib

if %ERRORLEVEL% EQU 0 (
    echo === BUILD SUCCESS ===
    dir "%OUT_DIR%\NataneBrushNative.dll"
) else (
    echo === BUILD FAILED (exit code %ERRORLEVEL%) ===
)

rem Clean up intermediate files
del /q *.obj *.exp *.lib 2>nul

endlocal
