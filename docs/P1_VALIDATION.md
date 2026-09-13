# P1-A 0.2.0 validation

- Official target build: passed locally, 0 errors, 0 warnings.
- Core test executable: 2,058 checks passed (P0 regressions plus v1 migration, compressed huge talent levels, exact historical refunds, atomic transactions, randomized point conservation and fractional recovery).
- Native tModLoader runtime harness: pending current CI run. Extended tests exercise actual max-stat hooks, damage, defense, speed, healing, regeneration, resource clamps, saves and authoritative request handling.
- GUI layout, keyboard/mouse interaction, actual character upgrade from the user's P0 save and two real clients require in-game acceptance. A dedicated-server harness does not prove GUI rendering or live networking.
- Previous P0 user-confirmed tests: XP correct; progression survives death, game restart and world changes. P0 real multiplayer contribution and segmented/multi-stage Boss encounters remain deferred by the user; they are not passed tests.
- No P1 merge authorization. Feature PR remains open pending user acceptance.
