# P2-Afflictions 0.9.0 implementation

Branch: `feat/p2-afflictions`. Accepted main baseline: `afe41c470faeb81e14eabbff2f35cf9183487ef1` (0.8.2, PR #10). The branch carries the approved PR #11 documentation from `d5bda49fa46675243b686e7cab20782a301d2527`; neither PR #11 nor this feature PR is automatically merged.

## Scope

NEXT-01/02/03 and MAINT-01/02 only. 87 talents: 51 numeric registrations and 36 functional registrations. Six additions cost one point per level, unlimited, adjustable: AfflictionDamage +20%; AttackFrostburn/AttackCursedInferno/AttackVenom/AttackIchor 2 seconds; FishingPower +5. Existing IDs, native burn/poison, paid-cost refund ledger, save v3 and UI groups remain. Network protocol 11 rejects mixed catalogs/snapshot layouts.

## Authority and damage

Single-player ModPlayer.OnHitNPC dispatches effective hits to AfflictionNpc. Multiplayer reuses the existing read-only native strike-28 observer; only its matching post-strike confirmation with actual health lost can create an application. Owned projectiles and summons use this same native strike path. Native AddBuff handles immunities, capacity, reapplication and coexistence; the server sends one completed NPCBuffs snapshot after applications.

Per-NPC transient sources store status, player slot, session GUID and the talent's own expiry, never the longer native duration from another source. Reapplication takes max expiry. Each tick resolves highest currently enabled effective AfflictionDamage level among valid sources. Disconnect/session replacement, removed buff, own expiry, NPC SetDefaults or world reset invalidate leases. Disabling the damage talent leaves the native debuff and lease lifetime unchanged, but contributes zero extra damage. No PvP/friendly/town/immortal target expansion.

Pinned official NPC.UpdateNPC_BuffApplyDOTs subtracts lifeRegen 8/12/16/48/60 for OnFire/Poisoned/Frostburn/CursedInferno/Venom (4/6/8/24/30 HP/s); Ichor supplies no DoT. Extra regen is base regen × level / 5. Fractional carry preserves low-level +20% precisely. Native Oiled, weapon-only debuffs, other native statuses and third-party DoTs are not multiplied. All damage remains in the native lifeRegen/death/loot pathway; no recursive hit, custom StrikeNPC damage or extra XP payout is introduced.

Server NPC extra-AI snapshots carry five bounded rate numerators only when nonzero (otherwise one bit). Rates are synchronized when they change, not every fractional tick. Clients use them for native prediction/display; servers never accept incoming rates as source authority. Unlimited state is preserved; engine output saturates with safe arithmetic and bounded native damage loops (D048).

## Fishing and passive mapping

GetFishingLevel receives the *environment multiplier* in this exact binary. UpdateEquips instead adds 5 per level to fishingSkill, letting native gear, pole, bait and weather/time calculation run afterward. Engine result saturates at 1,000,000 fishingSkill, saved levels do not.

Reports use the loaded Mod.Version. Added 31 explicit mappings: 13 information accessories, 11 immunity accessories, 7 fishing/building accessories. Information/immunity children and composite equipment retain partial status; brick layer and cement mixer map to distinct block/wall speed talents. No extra SetDefaults/ModifyTooltips/UpdateAccessory or unknown third-party behavior is invoked.

## Validation

Local core checks: 3112 passed. Production and runtime-harness compile with pinned official tModLoader v2026.07.3.0. Local dedicated-server startup is blocked by container `/proc/maps` access and MonoMod clrjit discovery; authoritative native test evidence will come from the repository's existing GitHub Actions workflow. Full CI and acceptance status are recorded in the PR and exact-build `BUILD_INFO.txt` after completion.

New native checks measure actual health loss for all six statuses; level-one fractional damage; weapon-only exclusion; immunity; refresh; native item and summon hit dispatch; confirmed server strikes; current strength/disable/refund/disconnect; NPC/world lifetime; extra-AI authority; oiled exclusion; saturation; existing XP settlement; native final fishing power and equipment addition; report version and mapping scope. Real two-client testing and all earlier deferred/incomplete items remain separate.

See [Chinese player guide](P2Afflictions_TEST_GUIDE_zh-CN.md). No P3, tiered crit repair, auto-jump revival or third-party accessory automation is included.
