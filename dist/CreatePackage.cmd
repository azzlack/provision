@echo off
echo.
echo Deleting old packages
del Provision.*.nupkg
echo.
echo Updating NuGet
nuget.exe update -self
echo Building solution
C:\Windows\Microsoft.Net\Framework64\v4.0.30319\MSBuild.exe ..\src\Provision.sln /p:Configuration=Release
echo.
echo Creating Provision Package
nuget pack nuget\provision\Provision.nuspec -Symbols
echo.
echo Creating Provision Config Package
nuget pack nuget\provision-config\Provision.Config.nuspec -Symbols
echo.
echo Creating Provision Redis Provider Package
nuget pack nuget\provision-providers-redis\Provision.Providers.Redis.nuspec -Symbols
echo.
echo Creating Provision MemoryCache Provider Package
nuget pack nuget\provision-providers-memorycache\Provision.Providers.MemoryCache.nuspec -Symbols
echo.
echo Creating Provision PortableMemoryCache Provider Package
nuget pack nuget\provision-providers-portablememorycache\Provision.Providers.PortableMemoryCache.nuspec -Symbols