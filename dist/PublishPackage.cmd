@echo off
echo.
set /P key= Enter NuGet API Key:
echo.
echo Updating NuGet
nuget.exe update -self
echo.
echo Publishing Packages
nuget push Provision.1.2.4.nupkg -ApiKey %key%
nuget push Provision.Config.1.2.4.nupkg -ApiKey %key%
nuget push Provision.Providers.MemoryCache.1.2.4.nupkg -ApiKey %key%
nuget push Provision.Providers.Redis.1.2.4.nupkg -ApiKey %key%
nuget push Provision.Providers.PortableMemoryCache.1.2.4.nupkg -ApiKey %key%