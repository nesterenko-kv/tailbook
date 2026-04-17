# Demo seed refresh from bookkeeping export

## Source files analyzed

This refresh was derived from the following uploaded artifacts:

- `Учет - Доходы(1).csv` — bookkeeping export with service labels, debit amount, groomer payout columns, delivery, comments, and origin.
- `price(2).png` — current public price cards used as the source of truth for **published current pricing**.
- `Tailbook_Rewrite_Blueprint(16).pdf` — used to keep the seed aligned with the intended Tailbook catalog/pricing/duration model.

## What was extracted

### Groomers / masters

Active payout columns found in the bookkeeping export:

- `Юля` — 280 payout rows, active from 2024-02-05 to 2024-10-25
- `Наташа` — 149 payout rows, active from 2023-08-06 to 2024-01-29
- `Лена` — 66 payout rows, active from 2024-02-01 to 2024-08-08
- `Ксения` — 37 payout rows, active from 2024-07-26 to 2024-10-26

These columns are seeded as development groomer profiles with **reasonable default schedules** rather than exact historical attendance.

### Procedures

The seed now uses a small atomic procedure catalog that can support the visible packages and standalone services:

- Bathing
- Coat Drying
- Brushing
- Deshedding Brushing
- Haircut
- Ear Cleaning
- Nail Trim & Filing
- Skin Care / Nourishing Treatment

### Commercial offers

The development catalog now contains:

- Dog Full Grooming
- Dog Express Deshedding
- Cat Haircut
- Cat Express Deshedding
- Delivery (add-on)
- Nail Trim & Filing (standalone)
- Ear Cleaning (standalone)

## Heuristics used

### Pricing source priority

1. **Current price cards (`price(2).png`) win for the published development price rules.**
   They represent the current salon price list and are the closest signal for "actual pricing for this moment".
2. **Bookkeeping medians were used as a sanity check and fallback signal**, especially for:
   - confirming which services were actually sold repeatedly,
   - confirming that cat haircut / cat express and delivery are real recurring flows,
   - choosing a flat demo delivery price.

### Notable bookkeeping signals

Examples from the bookkeeping export:

- historical `стрижка кота` / `стрижка кошки` rows cluster around **500 UAH median**,
- historical `элк кот*` / `элк кошка*` rows cluster around **550 UAH median**,
- historical `Доставка` rows have a **150 UAH median**,
- breed/service labels are noisy because they often mix breed + pet nickname.

Because the bookkeeping file is historic and noisy, the final published seed uses the **current visible price cards** instead of replaying old medians directly into current pricing.

### Label normalization

The bookkeeping export contains labels such as:

- `Йорк Микки`
- `Мальтипу Рокси`
- `ЭЛК кот Умка`
- `Мопс мотя (экспресс-линька)`

The refresh treats those as evidence for breed/service demand, not as catalog labels.

### Breed and taxonomy mapping

The pet taxonomy was extended with additional breeds needed by the current price cards:

- Biewer Terrier
- German Spitz
- Toy Terrier
- Alaskan Malamute
- Cocker Spaniel

Coat and size compatibility mappings were also added so they behave correctly in the pet profile flows.

## Limitations

- Delivery is seeded as a **flat 150 UAH development add-on**, even though real delivery can vary by route and was historically variable.
- `Вичіс ковтунів` is visible on the public price cards as an **hourly service**, but the current simplified MVP pricing model only supports fixed-amount rules, so it was intentionally not added as a standalone offer in this refresh.
- The schedules are **development defaults**, not imported historical shift calendars.
- This refresh does **not** create demo auth users for groomers. It only seeds groomer profiles and working schedules.

## Where the logic lives

- `backend/src/Tailbook.Api.Host/Infrastructure/DevelopmentDemoSalonSeeder.cs`
- `backend/src/Tailbook.Modules.Pets/Application/PetsCatalogSeeder.cs`

## Refresh checklist for future accounting exports

1. Review the new bookkeeping export for active groomer columns.
2. Re-check current public price cards or the active salon price source.
3. Update the development seed data structures in `DevelopmentDemoSalonSeeder.cs`.
4. If a current price card introduces a new breed bucket that Tailbook cannot represent, extend `PetsCatalogSeeder.cs` first.
5. Keep the seed **development-only** and idempotent.
6. Re-run automated tests to verify that the demo seed stays disabled in the `Testing` environment.
