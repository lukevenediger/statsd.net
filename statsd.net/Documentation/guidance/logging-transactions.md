# Logging Transactions
A quick win when getting started with adding metrics to a system is to log when transactions take place. A transaction is any significantly autonomous task that needs attention should it deviate from ordinary behaviour.

## Getting Started

Logging a transaction is done in three steps:

1. Log the attempt
2. Log the successful outcome, or
3. Log the failed outcome

For example, take a transaction that hails for a taxi. As the call is initiated, the following metric is logged:

`stats_counts.taxiApp.hail.attempt`

A successful request would result in

`stats_counts.taxiApp.hail.success`

While a failed request would be reported as

`stats_counts.taxiApp.hail.failure`

Logging transaction events this way makes creating a ratio graph in Graphite a snap. Take this example, for instance, which shows the ratio of attempted to successful calls per day:

`asPercent(summarize(stats_counts.taxiApp.hail.attempt, "1day"), summarize(stats_counts.taxiApp.hail.success))`

This can easily become a graph of failure ratio per day by changing the last series to *`stats_counts.taxiApp.hail.failure`*.

## Drop-off Rate

Some attempts never make it to *successful* or *failed* statuses. They are lost transactions that never arrive at a completed state. The transaction metric pattern helps to track down these lost attempts by showing where they're not.

This ratio begs the question of where the unsuccessful attempts wentWere they legitimate failures or is there a drop-off that's unaccounted for? 