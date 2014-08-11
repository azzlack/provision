@echo off
echo.
set /P key= Enter NuGet API Key:
echo.
echo Updating NuGet
nuget.exe update -self
echo.
echo Publishing Packages
nuget push Provision.1.1.1.nupkg -ApiKey %key%
nuget push Provision.Config.1.1.1.nupkg -ApiKey %key%
nuget push Provision.Providers.MemoryCache.1.1.0.nupkg -ApiKey %key%
nuget push Provision.Providers.Redis.1.1.0.nupkg -ApiKey %key%
nuget push Provision.Providers.PortableMemoryCache.1.1.0.nupkg -ApiKey %key%