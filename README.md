# Bunnings-project

## Edge cases already handled

- Orders with empty `entries` are safely ignored (no sales counted).
- Duplicate product lines within the same order are counted once per product (`Distinct()`).
- Multiple orders of the same product by the same customer on the same day are de-duplicated using a `(date, customerId, productId)` key.
- Cancellation records with no entries are supported by looking up the original completed order and crediting back its products.
- Cancellations with no matching completed order are ignored safely.
- Repeated cancellation events do not double-decrement totals (sale is removed from the “counted” set once credited back).
- Missing product names fall back to `"Unknown"` rather than failing.
- Days missing within the last-3-days window are skipped safely during period aggregation.
