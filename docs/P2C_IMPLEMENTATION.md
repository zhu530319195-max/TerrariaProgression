# P2-C 0.6.0 — immunity and combat triggers

Based on accepted P2-B / merged PR #7 (`a3ac45aeccb739b42bbcf3d317a7ced45075b539`). P2-C is an unmerged acceptance build. Save version 3 remains; network protocol 8 requires all peers to update. The complete catalog has 71 entries (47 numeric registry + 24 functional registry).

| Entry | Cost | Implementation and boundary |
|---|---|---|
| StatusImmunity | 2 once | Composite; 13 independent free child switches set native `buffImmune`; enabling also clears an existing mapped status on its owner. Never removes immunity supplied by equipment. |
| StarRetaliation | 1/level | Adjustable native retaliation damage × active level; exactly three stars, survived hurt, native cooldown channels −1 or 1. |
| BeeRetaliation | 1/level | Adjustable native bee damage × active level; native random 1–3 bees, strong-bee equipment may add a fourth. Talent alone grants no Honey healing. |
| PanicSpeed | 2 once | Native `Player.panic` causes hurt to grant Panic for 480 ticks; native movement bonus +100%. No purchase/toggle trigger. |
| AttackBurn | 1/level | Native OnFire for 120 ticks/active level. |
| AttackPoison | 1/level | Native Poisoned for 120 ticks/active level. |

The four numeric entries have no gameplay level cap; intensity can be reduced without refund. Native integer damage/time conversions clamp safely. Repeated hits refresh the maximum duration, never add durations or shorten longer existing effects; native damage over time and NPC immunity remain unchanged. No per-level projectile spawning loop.

Immunity maps exactly Poisoned, Bleeding, Slow, Weak, BrokenArmor, Silenced, Cursed, Confused, Darkness, Chilled, Frozen, Stoned and OnFire. It excludes PotionSickness, ChaosState, MoonLeech, Venom, stronger burning statuses, lava damage and unknown mod statuses. Knockback immunity remains separate.

## Native integration

`CombatUtilityPlayer` uses UpdateEquips, PostHurt and OnHitNPC. Only the affected/attacking owner client spawns talent retaliation or requests NPC buffs; native engine networking handles projectiles/statuses. Server and spectator copies cannot repeat these events. Purchases and state changes remain server-confirmed. Attack statuses exclude friendly/town NPCs, invulnerable or dead targets, zero-damage events and player PvP. A minion using the standard owner hit flow is included; no arbitrary third-party hook compatibility is promised.

Real Star Cloak / Honey Comb pointers suppress the custom batch. `RetaliationProjectile.OnSpawn` scales only native `EntitySource_ItemUse_OnHurt` projectiles from those exact equipped item references, including the known cloak overrides. Spawn stats are already initialized and outgoing native networking has not yet run. It updates originalDamage once; remote/network sources and talent custom sources cannot double-scale. Existing cloak projectile types and real Honey Comb healing remain intact. Custom stars use native 75/150/225 normal/expert/master damage; bees use native difficulty, strong-bee and generic damage calculation before talent growth.

Disabling prevents future talent triggers. Already spawned projectiles, inflicted NPC debuffs and Panic naturally expire. Cancelling a child immunity never cancels real gear immunity. Other mods may still alter native hooks; custom retaliation registrations carry compatibility grade C.

API reference: [ModPlayer](https://docs.tmodloader.net/docs/stable/class_mod_player.html), [native item-use hurt source](https://docs.tmodloader.net/docs/stable/class_entity_source___item_use___on_hurt.html). Actual API signatures and behavior were checked against pinned tModLoader v2026.07.3.0 assemblies and native Player/Projectile code.

## Validation

Local: 2919 core assertions pass; official MOD and harness compilation pass. The PR's CI is the authoritative native runtime result and must pass before delivery. Added coverage includes all 13 native immunity exclusions, free child switches, numeric cost/intensity/refund/save, real Player.Hurt at multiple levels, real accessory deduplication, blocked hurt, owner/server gates, native player hits and actual projectile collision, NPC immunity and maximum refresh, and protocol 8 import. Prior P0/P1/P2-A/P2-B checks remain enabled.

The runtime harness is a CI-only mod and is never included in the player ZIP. Source packaging reads committed blobs, preventing tModLoader localization normalization during tests from changing the advertised source snapshot. Build metadata records the tested checkout commit, counts and .tmod SHA256.

This is not acceptance of real multiplayer, special bosses, previously deferred potion sickness/fishing yield, tiered crit or arbitrary third-party compatibility. P2-C in-game testing uses `P2C_TEST_GUIDE_zh-CN.md`. P2-D scanning, dodge and P3 are out of scope.
