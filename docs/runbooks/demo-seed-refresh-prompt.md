# Prompt template — refresh Tailbook development demo seed from accounting

You are continuing development of Tailbook from my uploaded source ZIP.

Working mode:
- Analyze the uploaded bookkeeping export first.
- Analyze the current public/internal price source for the salon.
- Inspect the existing demo seed implementation for pets taxonomy, groomers, procedures, offers, offer versions, price rules, and duration rules.
- Update the source directly.
- Keep the seed idempotent.
- Keep the demo seed disabled in test environment.
- Return the updated project as a ZIP.

What to extract:
1. Active groomer columns and reasonable default schedules.
2. Minimal atomic procedures needed by the sold packages/services.
3. A small practical catalog of package / standalone / add-on offers.
4. Current price rules based on the latest visible price source, using bookkeeping medians only as fallback/sanity signal.
5. Conservative duration rules.
6. Documentation of analyzed files, heuristics, mappings, and limitations.

Constraints:
- Do not invent cross-module references between Tailbook modules.
- Prefer host-level development seed orchestration if multiple module domains are involved.
- If the current price source introduces breeds that do not yet exist in taxonomy, extend taxonomy and compatibility mappings first.
- If the accounting data is noisy, normalize labels and document the assumptions.
