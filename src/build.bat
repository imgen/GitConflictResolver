@echo off
dotnet build
dotnet publish -c Release -r win10-x64
dotnet publish -c Release -r osx.10.12-x64
dotnet publish -c Release -r linux-x64
xcopy ".\bin\Release\netcoreapp2.2\win10-x64\publish\*.*" "..\binaries\win10" /K /D /H /Y
xcopy ".\bin\Release\netcoreapp2.2\osx.10.12-x64\publish\*.*" "..\binaries\osx.10.12" /K /D /H /Y
xcopy ".\bin\Release\netcoreapp2.2\linux-x64\publish\*.*" "..\binaries\linux" /K /D /H /Y
@echo on