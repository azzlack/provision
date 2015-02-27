provision
=========

An easy-to-use and fast caching framework for .NET with support for many storage systems:
* [Redis](http://redis.io/)
* [System.Runtime.Caching.MemoryCache](http://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx)
* Portable Memory Cache for use with Windows Store, Windows Phone 8, Xamarin apps

### Usage
#### Basic initialization
```csharp
var cacheHandler = new RedisCacheHandler(new RedisCacheHandlerConfiguration("localhost", 6379, 3));
var cacheHandler = new MemoryCacheHandler(new MemoryCacheHandlerConfiguration("0 0 0/1 1/1 * ? *"));
var cacheHandler = new PortableMemoryCacheHandler(new PortableMemoryCacheHandlerConfiguration("0 0 0/1 1/1 * ? *"));
```
#### Configure from app.config or web.config
```csharp
var cacheHandler = ProvisionConfiguration.Current.GetHandler();
```
```xml
<configuration>
  <configSections>
    <section name="provision" type="Provision.Config.ProvisionConfiguration, Provision.Config" />
  </configSections>
  <provision handler="Provision.Providers.Redis.RedisCacheHandler, Provision.Providers.Redis" defaultConfiguration="redis">
		<add name="redis" type="Provision.Providers.Redis.RedisCacheHandlerConfiguration, Provision.Providers.Redis" database="3" host="10.1.14.149" prefix="glue" />
	</provision>
</configuration>
```
#### Add object to cache
```csharp
var cacheHandler = new MemoryCacheHandler();

var d = new Report() { Items = new List<ReportItem>() { new ReportItem() { Key = "1", Data = 100 } } };

var key = cacheHandler.CreateKey("xyz", "1", "something"); // Returns "xyz_1_something"
await cacheHandler.AddOrUpdate(key, d, DateTime.Now.AddDays(1)); // Adds the report to the cache with the key and sets the expiry date to 1 day forward
// NOTE: The expiry date is optional, the cache handler will use the global value for the cache handler if not specified
```
#### Get object from cache
```csharp
var cacheHandler = new MemoryCacheHandler();

var key = cacheHandler.CreateKey("xyz", "1", "something"); // Returns "xyz_1_something"
var cacheItem = await cacheHandler.Get<Report>(key); // Gets the cache item wrapper with the specified key
var report = cacheItem.Value;

// You can also use the following shorthand to get the value:
var report2 = await cacheHandler.GetValue<Report>(key); 
````
#### Remove object from cache
```csharp
var cacheHandler = new MemoryCacheHandler();

var key = cacheHandler.CreateKey("xyz", "1", "something"); // Returns "xyz_1_something"
await cacheHandler.Remove(key); // Removes the cache item with the specified key
```
#### Check if key exists in cache
```csharp
var cacheHandler = new MemoryCacheHandler();

var key = cacheHandler.CreateKey("xyz", "1", "something"); // Returns "xyz_1_something"
var exists = await cacheHandler.Contains(key);
```
#### Purge cache
```csharp
var cacheHandler = new MemoryCacheHandler();

var purged = await cacheHandler.Purge();
```


##### Credits
[Mono](http://www.mono-project.com/) for `ConcurrentDictionary`, `ReadOnlyCollection` and `SplitOrderedList` used in the `PortableMemoryCacheHandler`  
[Quartz.NET](http://www.quartz-scheduler.net/) for the Cron expression parser used for config parsing.  
[C5](http://www.itu.dk/research/c5/) for the `TreeSet` collection used by `Quartz.NET`.
