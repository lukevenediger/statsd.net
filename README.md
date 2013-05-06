# statsd.net
A high-performance stats collection service based on [etsy's](http://etsy.com/) [statsd service](https://github.com/etsy/statsd/) and written in c#.net.

## Key Features
* Compatible with etsy's statds protocol for sending counts, timings, latencies and sets
* Enables multiple latency buckets for the same metric to measure things like p90/5min and p90/1hour in one go

## Project Status
This system is not yet ready for prime-time. I'm working toward the first milestone of [stats.net v1.0](https://github.com/lukevenediger/statsd.net/issues?milestone=1&state=open) which is due on the 28th of May. Right now you can take a build and run it to play around, but it's not in a production-ready state just yet.

## Installation, Guidance, Configuration and Reference Information
* Find all this and more on the **[statsd.net wiki](https://github.com/lukevenediger/statsd.net/wiki)**

## Coming Soon
* [App Fabric](http://msdn.com/appfabric) and [memcached](http://memcached.org/) support to allow horizontal scaling, with load balancing and storage re
* More backends
* Web-based management console with a RESTful API
* Histogram stats



## About the Codebase

### Maintainers
Luke Venediger - lukev@lukev.net

### Licence
MIT Licence.
