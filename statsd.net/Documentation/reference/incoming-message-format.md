# Incoming Message Format

There are several types of metrics accepted by statsd.net:
* Counts - a record of an event occuring
* Timings - a record of how long an event took, in milliseconds
* Gauges - a snapshot value
* Sets - a unique occurrence of an artifact

### Counts
```
<namespace>:<value>|c
```
For example
```
homepage.hits:1|c
order.itemsAdded:4|c
```

### Timings
```
<namespace>:<measurement in milliseconds>|ms
```
For example:
```
homepage.db.fetchPromotions:3021|ms
homepage.db.clientLoad:8994|ms
```

### Gauges
```
<namespace>:<value>|g
```
For example:
```
homepage.activeUsers:49|g
homepage.baskets.itemsInBasket:204|g
```

### Sets
```
<namespace>:<value>|s
```
For example:
```
basket.itemAdded.10034|s
session.open.South_Africa|s
```

## Notes
This mirrors etsy's statsd [message format](https://github.com/etsy/statsd/blob/master/docs/metric_types.md).

There are several types of metrics accepted by statsd.net:
* Counts - a record of an event occuring
* Timings - a record of how long an event took, in milliseconds
* Gauges - a snapshot value
* Sets - a unique occurrence of an artifact

## Counts
```
<namespace>:<value>|c
```
For example
```
homepage.hits:1|c
order.itemsAdded:4|c
```

## Timings
```
<namespace>:<measurement in milliseconds>|ms
```
For example:
```
homepage.db.fetchPromotions:3021|ms
homepage.db.clientLoad:8994|ms
```

## Gauges
```
<namespace>:<value>|g
```
For example:
```
homepage.activeUsers:49|g
homepage.baskets.itemsInBasket:204|g
```

## Sets
```
<namespace>:<value>|s
```
For example:
```
basket.itemAdded.10034|s
session.open.South_Africa|s
```

## Notes
This mirrors etsy's statsd [message format](https://github.com/etsy/statsd/blob/master/docs/metric_types.md).