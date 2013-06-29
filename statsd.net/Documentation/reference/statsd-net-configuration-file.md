# Statsd.net Configuration File
The service is configured through the statsdnet.toml file that lives along-side the statsdnet.exe executable file. Configuration is made up of these sections:

* General Settings - such as the given name of the service instance
* Listeners - services that listen out for new stats
* Backends - services that send the aggregated metrics off to graphite or, say, a message queue
* Calculation - settings such as how long to collect data for before flushing to the backends
* Console - including whether the management console is enabled or not

Let's go through each section.

## General Settings
```toml
[general]
name = "statsd"
```
* **name** - the name of your stats service.
 * Remarks: Don't bother changing this unless you have more than one collection point and need to distinguish between, say, major hosting location. Otherwise just "statsd" will be fine.

## Listeners
```toml
[listeners]
  [listeners.udp]
  enabled = true
  port = 12000

  [listeners.http]
  enabled = false
  port = 12001

  [listeners.tcp]
  enabled = false
  port = 12002
```
This describes two listeners - a UDP and HTTP listener. Both listeners accept [statsd-formatted lines](https://github.com/lukevenediger/statsd.net/wiki/Statsd.net-incoming-message-format).
* **enabled** - whether this listener is enabled or not. Can be **true** or **false**
* **port** - the port on which to bind the listener.

## Backends
Backends define what happens to the aggregated metrics.
```toml
[backends]
  [backends.sqlserver]
  enabled = false
  connectionString = "server=localhost;database=metrics;uid=mmetricsuser;password=metricsuser"
  
  [backends.graphite]
  enabled = true
  host = "graphite"
  port = 2003

  [backends.console]
  enabled = true
```
There are three backends:
* **[backends.sqlserver]** - this will write the aggregated data to a SQL database. You might use this as a rudimentary queueing system for your data.
* **[backends.graphite]** - metrics are written to graphite's UDP listener. Be sure to switch the UDP listener on inside [carbon-cache.conf](https://github.com/graphite-project/carbon/blob/master/conf/carbon.conf.example)
* **[backends.console]** - outputs aggregated metrics to the console window. Useful if you're running the service in interactive mode.

Looking at the configuration options:
* **connectionString** - the SqlConnection-compatible connection string for the sqlserver backend.
* **host** - graphite host or IP address
* **port** - graphite's UDP listen port

## Calculation
```toml
[calc]
flushIntervalSeconds = 10
gaugesNamespace = "gauges"
countersNamespace = "counts"
setsNamespace = "sets"
timersNamespace = "timers"

  [calc.percentiles.p90-5min]
  flushIntervalSeconds = 300
  percentile = 90

  [calc.percentiles.p90-1hour]
  flushIntervalSeconds = 3600
  percentile = 90
    
  [calc.percentiles.p50-1hour]
  flushIntervalSeconds = 3600
  percentile = 50
```

## Console
```
[console]
enabled = true
localhostOnly = true
port = 8080
```

## A quick word about TOML files and indentation.
statsd.net is configured using the [TOML](https://github.com/mojombo/toml#spec) file format so that it's easy to read and easy to update. There are five main sections to the configuration file:

Sections in the configuration file do not need to be indented. I've indented blocks to make the file more readable, but the indentation is not significant to the parser. As a matter of interest, the section headers use namespaces to indicate parent ownership.