# P0 implementation notes — 0.1.0

## Baseline and scope

Based on `chore/project-bootstrap` at `c06105917c3c7aab33fbd896f186f42f45d60124`. PR #1 remains unmerged; the P0 PR targets that branch and depends on #1. No P1/P2/P3 effects or appearance assets are registered.

Pinned target: official **tModLoader v2026.07.3.0**, Terraria **1.4.4.9**, .NET **8**, C# **12**, official `tMLMod.targets`. Do not switch the project to Terraria 1.4.5 preview APIs without a separate compatibility check.

Official references checked:

- https://github.com/tModLoader/tModLoader/releases/tag/v2026.07.3.0
- https://docs.tmodloader.net/docs/stable/class_global_n_p_c.html
- https://docs.tmodloader.net/docs/stable/class_mod_player.html
- https://docs.tmodloader.net/docs/stable/class_mod_system.html
- https://github.com/tModLoader/tModLoader/blob/v2026.07.3.0/patches/tModLoader/Terraria/MessageBuffer.cs.patch
- https://github.com/tModLoader/tModLoader/blob/v2026.07.3.0/patches/tModLoader/Terraria/NPC.cs.patch

## Modules

- `Core`: arbitrary-precision integer levels/points, fixed-point XP, fast exact level settlement, contribution apportionment, talent paid-cost history and versioned codec.
- `Players`: `ModPlayer.SaveData/LoadData`, character state, one-session authority, award gateway.
- `NPCs`: per-spawn lifetimes, shared-health/verified worm links/parent-source groups, deferred settlement, server strike observation.
- `Config`: server-side rules, validation, developer options.
- `Networking`: protocol version 1; one-time character import and server snapshots only.
- `Commands`, `UI`, `Localization`: status output and restricted XP testing in Chinese/English. No talent menu in P0.

## Mathematics and storage

XP = `lifeMax × HpToXpMultiplier × SourceMultiplier`. Micro-XP units (`1 XP = 1,000,000 units`) use `BigInteger`. Contribution fractions are apportioned with largest remainders: total awarded units never exceed or fall short of the group's budget. Rounding error per calculated NPC budget is at most half a micro-XP.

Level requirement remains exactly `250 + 50k + 10k²`, `k = Level - 1`, default cap 50000, 0 uncapped. Lv.69 requires 49890; Lv.70 and above require 50000 at default settings. Summed polynomials plus binary search are equivalent to a literal level-up loop, but even an award worth billions of levels cannot freeze the server by looping once per level. Each level awards 1 point; the core accepts an explicit reward parameter for a future approved config/migration.

`SaveData` stores `DataVersion = 1` and a `Progression` byte array. The versioned payload contains Level, CurrentExperience, TotalExperienceEarned, AvailableTalentPoints, TotalSpentTalentPoints and an empty-by-default talent dictionary. Each talent stores enabled state, actual per-purchase costs and optional intensity; level and invested points derive from that history. No empty catalog is generated. Disable does not refund; rollback returns the last actual price, unaffected by later price changes.

Save and packet decoding validate versions, positivity, point accounting and lengths. Unsupported/corrupt save payloads fail visibly rather than being silently replaced with Lv.1. A missing DataVersion means a new character without previous mod data. There are no death, ResetEffects or world-save mutations of growth. Transport safety limits (1024 bytes per integer, 60000 bytes per snapshot) are admission/storage resource limits rather than game-level balance caps; they are far beyond practical P0 play. Future large talent catalogs will need chunked synchronization and compressed cost runs before those limits matter.

## Multiplayer authority and trust boundary

- C→S `JoinCharacter`: protocol byte + kind byte + length-prefixed versioned character payload. Accepted once for the sender's own active connection. No client-specified player slot. Validate before replacing state. P0 rejects nonempty talent imports because no talent is registered.
- S→C `Snapshot`: same framing, sent privately to the owning player; replaces the display/save copy. Server rejects client snapshots and has no XP/level/point mutation packet.
- `OnEnterWorld` imports the existing portable character once; SyncPlayer never overwrites it with an uninitialized Lv.1 server object. Disconnect rotates the contribution session identifier so reused player slots cannot inherit damage.
- Offline characters are user-owned. A fresh server cannot authenticate prior singleplayer history. Import validates consistency, not the truth of offline play. Preventing edited imports would require server-only characters or a trusted external history, which conflicts with seamless portable saves unless separately designed. Within the session the server owns all XP and level decisions.
- Vanilla Terraria combat itself accepts owner-client strikes. We observe that existing stream, verify valid sender/target/class and actual health loss, then distribute only a server-calculated lifeMax budget. This is not independent server-side re-simulation of attacks or comprehensive anti-cheat.

`GlobalNPC.OnHitByItem` is owner-client-only in MP; `OnHitByProjectile` runs on the projectile owner. P0 therefore does **not** assume those hooks execute on the server. `ModSystem.HijackGetData` reads the pinned official strike payload, restores the reader position and returns false. A matching server `HitEffect` must confirm health loss before contribution is recorded. Overkill is capped by pre-hit remaining HP. No custom damage-claim packet is added. Item, ranged/magic projectiles, minions and sentries using the normal owner strike path participate without weapon or DamageClass whitelists.

Status is local read-only display; XP grant is singleplayer or dedicated-server console only. Server config edits from multiplayer clients are rejected; configure before hosting or edit server JSON while stopped. Debug commands and settlement logging default off.

## Conservative multi-entity protection and limits

Explicit parent sources, `realLife`, and reciprocal worm AI links merge transient encounters. Each encounter has one budget equal to the **largest observed lifeMax**, not the sum of body HP. Payout occurs only after completion and after on-hit callbacks, with a short two-tick settlement delay. Town ancestry excludes XP; statue ancestry applies its multiplier throughout descendants. Repeated death callbacks and late descendants of settled groups cannot award again. Disappearing/captured living entities are not kills. Killing only part of an independently killable worm and letting the rest despawn does not pay the completed encounter.

This intentionally conservative P0 strategy may under-award independently killable multipart bosses. Explicitly parented boss summons also share the parent's budget; P0 does not claim it can distinguish an independently rewardable minion from a body/phase solely from EntitySource. This is a compatibility/test limitation of the authorized generic protection, not a new per-boss experience table. Stable encounter links are required: unrelated spawn sources or unlinked custom phase chains cannot be guessed correctly. Separate simultaneous bosses are never merged just because their types or locations match.

Unattributed DoT/environment damage does not receive an invented owner. Already recorded contributors retain their relative direct-damage shares when DoT finishes a target; DoT is **not** claimed to have exact proportional ownership. Pure unowned DoT/environment kills with no recorded contributor give nobody XP. Server-owned custom attacks bypassing the normal player strike stream also require integration. Server-side integrations can call `EncounterSystem.ReportAttributedDamage(npc, player, actualHpLost)` and `LinkEncounter(child, parent)` in a cooperating mod; do not report an already observed hit twice. No public client packet can invoke these functions.

Disconnecting before settlement currently forfeits that connection's share; it is not redistributed to the last hitter or another occupant of the slot. Already awarded progress remains in the client's normal character save. Offline payout queues, crash-safe last-millisecond delivery, server handover proofs and arbitrary mod DoT provenance are outside P0.

## Validation entry points

`bash scripts/test.sh`: tests production core source directly with no external test framework.

`TML_PATH=/path/to/tModLoader bash scripts/build.sh`: official API compilation and `.tmod` packaging. Windows equivalent: `dotnet build TerrariaProgression/TerrariaProgression.csproj -c Release -p:TmlInstallPath="D:\...\tModLoader"`.

CI also builds a separate `ProgressionHarness` test mod and runs it in a disposable dedicated-server world. That harness is never included in player packages and refuses to execute without `TP_CI_HARNESS=1`. It tests native hooks/TagIO, simulated owner strike processing and generic encounter protection. It is not a substitute for two real clients, actual Eater/Destroyer fights, or third-party content-mod tests.
