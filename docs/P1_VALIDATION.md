# P1-A 0.2.0 validation

- Official target build: passed locally, 0 errors, 0 warnings.
- Core test executable: 2,058 checks passed (P0 regressions plus v1 migration, compressed huge talent levels, exact historical refunds, atomic transactions, randomized point conservation and fractional recovery).
- Native tModLoader runtime harness: **73 checks passed**. Actual max-stat hooks, damage, defense, speed, healing, regeneration, resource clamps, talent saves/death persistence, repeated request rejection, old-session rejection and registered talent import all passed.
- [Passing GitHub Actions run 34753311792](https://github.com/zhu530319195-max/TerrariaProgression/actions/runs/34753311792), functional commit `d592f790a7603706da2cf9316b06508475c6452f`. Later documentation-only commits reuse this functional evidence; changes to code require renewed checks.
- First P1 CI run compiled and passed core/effect checks, then exposed a missing socket in the synthetic-client fixture when testing a rejection reply. The fixture now implements tML's public ISocket interface and receives actual ModPacket bytes. No production handler was bypassed and no failed check was skipped.
- [PR #3](https://github.com/zhu530319195-max/TerrariaProgression/pull/3), base `main`, head `feat/numeric-talents-ui`, not merged.
- Two real clients still require in-game acceptance; a dedicated-server harness does not prove live networking. User-confirmed singleplayer results are recorded below.
- Previous P0 user-confirmed tests: XP correct; progression survives death, game restart and world changes. P0 real multiplayer contribution and segmented/multi-stage Boss encounters remain deferred by the user; they are not passed tests.
- No P1 merge authorization. Feature PR remains open pending user acceptance.

## User acceptance and UI follow-up — 2026-09-13

User confirmed in 0.2.0: upgrades, shortcut and command opening, disable/re-enable with no refund and no instant HP/MP refill, refunds, world changes and game restart persistence. This does not imply every talent / third-party interaction was individually verified.

User's 200% UI screenshot showed excessive fixed spacing, always-visible scrollbars and per-talent operations hidden below the scrolling details. 0.2.1 keeps native global UI scale and changes only the talent panel: responsive category rows, bottom-anchored actions outside the detail scroll, text-measured detail height, selected row/category highlights and scrollbars only when needed. Singleplayer does not display the unavailable resync button.

Official 0.2.1 compilation / packaging passed locally with 0 errors and 0 warnings. Static viewport geometry was checked at 100%, 150% and 200% for 1920×1080, 1964×1024, 2560×1440, 1280×720 and 1024×768; the action region stays inside the body with a positive text viewport. This is a geometry check, not a rendered GUI test. Updated CI results are available on PR #3. GUI rendering at the user's 200% scale remains a focused in-game follow-up. No merge authorization was given.
