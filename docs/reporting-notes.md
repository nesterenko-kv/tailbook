# Reporting Notes

Date: 2026-04-29

## Estimate Accuracy

Estimate accuracy reports closed visits and compares appointment estimate snapshots against visit final totals:

- `EstimatedAmount`: appointment item price snapshots.
- `FinalAmount`: estimated amount plus visit price adjustments.
- `AmountVariance`: signed adjustment total.
- `DurationVarianceMinutes`: actual service duration minus estimated service minutes.

## Package Performance

Package performance groups package appointment items by offer and reports booked count, closed count, estimated revenue, final revenue, and skipped included component count.

The relational query pre-aggregates skipped components per appointment item before grouping by offer so skipped-component joins do not multiply revenue. In-memory query paths use `AsNoTracking` and mirror the same adjustment rule: only closed visits contribute adjustment value to final revenue.

## Test Provider Note

The API integration test provider uses EF InMemory. Because reporting read models are mapped as relational views over operational tables, end-to-end smoke tests may not see operational rows through reporting read models under InMemory. Focused reporting accuracy tests seed reporting read-model entities directly to cover calculation behavior.
