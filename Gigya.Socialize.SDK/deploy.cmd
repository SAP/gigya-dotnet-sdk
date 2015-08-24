call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC1\vcvarsall.bat"

msbuild /p:Configuration=Release

rmdir /S /Q ..\Output
mkdir ..\Output

xcopy /Y ..\ProjectTemplate\* ..\Output\GSCSharpSDK\
xcopy /Y *.cs ..\Output\GSCSharpSDK\
xcopy /Y Properties\*.cs ..\Output\GSCSharpSDK\Properties\

msbuild ..\Output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /target:Clean /p:Configuration=Release
msbuild ..\Output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=..\..\DO_NOT_PUBLISH_AssemblyStrongNameKey.snk

msbuild ..\Output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /target:Clean /p:Configuration=Debug
msbuild ..\Output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /p:Configuration=Debug   /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=..\..\DO_NOT_PUBLISH_AssemblyStrongNameKey.snk

mkdir ..\Output\Bin
move ..\Output\GSCSharpSDK\bin\Release\*.dll ..\Output\Bin
rmdir /S /Q ..\Output\GSCSharpSDK\bin
rmdir /S /Q ..\Output\GSCSharpSDK\obj

pause
