Thank you for choosing statsd.net! This wiki is here to help you get started, guide you through making the most of your stats collector and to look under the hood at various parts of the system.

## What is Statsd.net?
Statsd.net is a service that collects and aggregates events, latencies and other statistical data produced by your application, and ships this data off to Graphite.

## Quickstart and Installation Guide
Getting started with statsd.net is easy - you don't need to install anything to start playing around.
* [[Trying out Statsd.net]]
* [[Installing Statsd.net]]

## Guidance

### System Configuration
* [[Common System Configurations]]
* [[Configuration File Examples]]
* [[Graphite Configuration Examples]]
* [[Setting Up Graphite to work with Statsd.net]]
* [[Scaling Outwards for increased redundancy]]
* [[Measuring System Health]]

### Logging Metrics (or Lessons Learned from the Trenches)
* [[Type of Metrics]]
* [Metric Anti-Patterns](guidance/metric-anti-patterns.md)
* [[Logging Transactions]]

## Reference
* [[Statsd.net Configuration File]]
* [[Other Statsd-like Collectors]]
* [[Client Libraries]]
* [[Statsd.net incoming message format]]

## Under The Hood
* [[DataFlowModel|The flow of data as described in TPL Data Blocks]]
* [[Understanding the Aggregators]]
* [[List of Libraries Used]]

## FAQ
1. *Why did you create a C# version of [etsy's statsd](https://github.com/etsy/statsd) service?*
 * I work in a highly distributed Microsoft-centric environment. I looked at etsy's (beautifully crafted) statsd service and found that it didn't suit me because I needed to write output data to SQL Server and I needed additional configuration options to support having multiple hosting locations. It also gave me a chance to play with the [.Net DataFlow library](http://msdn.microsoft.com/en-us/library/hh228603.aspx) which rocks.
1. *Will this service ever run on Linux / Mono?*
 * I don't know - I don't have any immediate plans to make it work on Mono, and don't have an immediate need for this either.

## Support
For bugs and feature requests:
* Open an issue on github: [create new issue](https://github.com/lukevenediger/statsd.net/issues/new)

For help and support, please drop me a line at lukev-at-lukev.net. I'll create an IRC channel and a Google Group as usage increases.