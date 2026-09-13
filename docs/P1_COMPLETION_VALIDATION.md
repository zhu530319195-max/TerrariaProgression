# P1 completion 0.3.0 validation

- Base main: 5f23b0fa544726106e88cb2b9d5db999c7a8b797 (PR #3 merged with user authorization).
- Target unchanged: official tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8.
- Local core checks: 2,822 passed. Includes prior P0/P1-A regression coverage, tier boundaries, quantities and fractional tails, drop formula, 750 vanilla luck-distribution comparisons, active-intensity validation and round trips.
- Local official compilation: production mod and isolated runtime harness pass with zero warnings/errors.
- Native dedicated-server checks are run by GitHub Actions; current result is on the PR. They cover actual attributes, critical damage and DisableCrit, source-tagged item creation, real ore mining, private-spawn exclusion, prices, buffs, mode persistence and server request regression.
- Local GUI rendering and two physical multiplayer clients are unavailable here. Prior 0.2.0 player acceptance is preserved; it does not automatically accept 0.3.0 effects or UI changes.
- P0 multiplayer contribution / segmented Boss real-game acceptance remains explicitly deferred by the user.
- Limitations and incomplete catalog mappings: `P1_COMPLETION_IMPLEMENTATION.md`.
