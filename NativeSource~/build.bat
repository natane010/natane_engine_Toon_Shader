@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x64
cd /d "%~dp0src"
cl.exe /O2 /MT /EHsc /std:c++17 /LD /Fe:..\..\Plugins\x86_64\NataneBrushNative.dll /I. /DNDEBUG /fp:fast natane_brush_native.cpp /link /DLL user32.lib comctl32.lib
echo EXIT_CODE=%ERRORLEVEL%
