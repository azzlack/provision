provision
=========

An easy-to-use and fast caching system for .NET with support for many storage systems:
* [Redis](http://redis.io/)
* [System.Runtime.Caching.MemoryCache](http://msdn.microsoft.com/en-us/library/system.runtime.caching.memorycache(v=vs.110).aspx)
* Portable Memory Cache for use with Windows Store, Windows Phone 8, Xamarin apps

##### Credits
[Mono](http://www.mono-project.com/) for `ConcurrentDictionary`, `ReadOnlyCollection` and `SplitOrderedList` used in the `PortableMemoryCache`  
[Quartz.NET](http://www.quartz-scheduler.net/) for the Cron expression parser used for config parsing.  
[C5](http://www.itu.dk/research/c5/) for the `TreeSet` collection used by `Quartz.NET`.
