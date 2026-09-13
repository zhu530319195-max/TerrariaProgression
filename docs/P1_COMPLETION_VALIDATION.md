# P1 completion 0.3.0 validation

- Base main: 5f23b0fa544726106e88cb2b9d5db999c7a8b797 (PR #3 merged with user authorization).
- Target unchanged: official tModLoader v2026.07.3.0 / Terraria 1.4.4.9 / .NET 8.
- Local core checks: 2,822 passed. Includes prior P0/P1-A regression coverage, tier boundaries, quantities and fractional tails, drop formula, 750 vanilla luck-distribution comparisons, active-intensity validation and round trips.
- Local official compilation: production mod and isolated runtime harness pass with zero warnings/errors.
- Initial native dedicated-server run: **135 checks passed**, [Actions 34756647607](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34756647607) at 6bde62a9e3663fc9e7cdbafa1dbbb167c1c5770b. Follow-up adds a regression check for reserved private item slots; final result is on the PR. They cover actual attributes, critical damage and DisableCrit, source-tagged item creation, real ore mining, private-spawn exclusion, prices, buffs, mode persistence and server request regression.
- Local GUI rendering and two physical multiplayer clients are unavailable here. Prior 0.2.0 player acceptance is preserved; it does not automatically accept 0.3.0 effects or UI changes.
- P0 multiplayer contribution / segmented Boss real-game acceptance remains explicitly deferred by the user.
- Limitations and incomplete catalog mappings: `P1_COMPLETION_IMPLEMENTATION.md`.

## Follow-up evidence and user feedback (2026-09-13)

[Actions 34758260039](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34758260039) at a4cac0fa24b35186a3393580f51338223953f60b passed compilation, 2,822 core checks and **161 native runtime checks**. The added fixture purchases only MiningYield Lv.10, uses normal copper-pick power (three hits per tile), and verifies copper/tin/iron/lead output through enable → disable → enable: 2 → 1 → 2 items per tile. All partial/final mining scopes restore. This is headless native testing, not GUI acceptance.

The user subsequently corrected their report: mining yield **does work**. Also confirmed: critical chance, wooden-sword swing size, tool reach. Thrusting shortswords remain unsupported by the swing adapter. Tiered crit failed in-game and its fix is explicitly deferred by the user; the synthetic HitModifiers tests do not supersede that real-game result. No tiered-crit acceptance or PR #4 merge authorization is inferred. Unpublished diagnostic code was discarded; production remains 0.3.0.
