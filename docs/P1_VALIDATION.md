# P1-A 0.2.0 validation

- Official target build: passed locally, 0 errors, 0 warnings.
- Core test executable: 2,058 checks passed (P0 regressions plus v1 migration, compressed huge talent levels, exact historical refunds, atomic transactions, randomized point conservation and fractional recovery).
- Native tModLoader runtime harness: **73 checks passed**. Actual max-stat hooks, damage, defense, speed, healing, regeneration, resource clamps, talent saves/death persistence, repeated request rejection, old-session rejection and registered talent import all passed.
- [Passing GitHub Actions run 34753311792](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34753311792), functional commit `d592f790a7603706da2cf9316b06508475c6452f`. Later documentation-only commits reuse this functional evidence; changes to code require renewed checks.
- First P1 CI run compiled and passed core/effect checks, then exposed a missing socket in the synthetic-client fixture when testing a rejection reply. The fixture now implements tML's public ISocket interface and receives actual ModPacket bytes. No production handler was bypassed and no failed check was skipped.
- [PR #3](https://github.com/zhu530319195-max/TerrariaProgression/pull/3), base `main`, head `feat/numeric-talents-ui`, not merged.
- GUI layout, keyboard/mouse interaction, actual character upgrade from the user's P0 save and two real clients require in-game acceptance. A dedicated-server harness does not prove GUI rendering or live networking.
- Previous P0 user-confirmed tests: XP correct; progression survives death, game restart and world changes. P0 real multiplayer contribution and segmented/multi-stage Boss encounters remain deferred by the user; they are not passed tests.
- No P1 merge authorization. Feature PR remains open pending user acceptance.
