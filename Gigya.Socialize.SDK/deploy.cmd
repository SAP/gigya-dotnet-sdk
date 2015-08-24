call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio 11.0\VC1\vcvarsall.bat"

msbuild /p:Configuration=Release

rmdir /S /Q ..\output
mkdir ..\output

xcopy /Y ..\ProjectTemplate\* ..\output\GSCSharpSDK\
xcopy /Y *.cs ..\output\GSCSharpSDK\
xcopy /Y Properties\*.cs ..\output\GSCSharpSDK\Properties\

msbuild ..\output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /target:Clean /p:Configuration=Release
msbuild ..\output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /p:Configuration=Release /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=..\..\DO_NOT_PUBLISH_AssemblyStrongNameKey.snk

msbuild ..\output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /target:Clean /p:Configuration=Debug
msbuild ..\output\GSCSharpSDK\Gigya.Socialize.SDK.csproj /p:Configuration=Debug   /p:SignAssembly=true /p:AssemblyOriginatorKeyFile=..\..\DO_NOT_PUBLISH_AssemblyStrongNameKey.snk

mkdir ..\output\bin
move ..\output\GSCSharpSDK\bin\Release\*.dll ..\output\bin
rmdir /S /Q ..\output\GSCSharpSDK\bin
rmdir /S /Q ..\output\GSCSharpSDK\obj

pause
