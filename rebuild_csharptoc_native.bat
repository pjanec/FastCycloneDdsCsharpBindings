@echo off
setlocal
cd tests\CsharpToC.Roundtrip.Tests\Native
if not exist build mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Debug
cmake --build . --config Debug
if not exist Release mkdir Release
copy Debug\CsharpToC_Roundtrip_Native.dll Release\CsharpToC_Roundtrip_Native.dll
cd ..\..\..\..
