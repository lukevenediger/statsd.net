# statsd.net
A high-performance stats collection service based on [etsy's](http://etsy.com/) [statsd service](https://github.com/etsy/statsd/) and written in c#.net.

## Key Features
* Compatible with etsy's statds protocol for sending counts, timings and latencies
* Enables multiple latency buckets for the same metric to measure things like p90/5min and p90/1hour in one go

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
