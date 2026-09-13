# ROADMAP.md

## P0 — Progression Core
Goal: stable character-bound infinite leveling foundation.

Implementation status: P0 0.1.0 is delivered through PR #2 (bootstrap dependency #1); official compilation, 1,537 core assertions and 28 tModLoader runtime assertions passed. On 2026-09-13 the user confirmed singleplayer XP and persistence across death, restart and world changes, and authorized merging. Real multiplayer contribution and segmented/multi-stage Boss tests are explicitly deferred, not passed. See `docs/P0_VALIDATION.md`. P0 is merged; see P1 below. P2/P3 remain unimplemented.

- tModLoader project skeleton
- `ModPlayer` progression data
- Level starts at 1; no level cap
- Current XP + total lifetime XP
- XP formula based on NPC `lifeMax`
- Configurable XP per max-HP point: 0.01–10.00, default 1.00
- Quadratic XP requirement formula
- Configurable per-level requirement cap: default 50,000; 0 = uncapped
- Multiple levels from one XP award
- Talent points granted per level
- Character save/load
- Death/world-change persistence
- Multiplayer synchronization foundation
- Server-authoritative XP settlement
- Configurable statue-spawn XP multiplier
- Town-NPC XP exclusion
- Store actual paid talent-point costs for safe refunds
- Generic talent enabled/disabled state
- Basic debug/test commands or diagnostics as needed

## P1 — Numeric Talents
Goal: implement stable numeric talents using the approved Lv.10 strong-state baseline.

P1-A 0.2.0 implements 21 entries and the talent panel; official compilation, 2,058 core checks and 73 runtime checks passed; the user has confirmed the core singleplayer talent interactions and persistence. PR #3 includes the 0.2.1 UI layout refinement for 200% scale, awaiting visual feedback and explicit merge authorization. Implemented: maximum HP/MP, defense, run speed/acceleration, fixed/natural HP/MP recovery, item HP/MP restoration, generic damage/attack speed/crit damage/armor penetration/knockback, ammo conservation, mana cost, minion/sentry capacity, pickup range. The remaining catalog below is still planned, particularly crit chance / tiers and economy / resource drops. See `docs/P1_IMPLEMENTATION.md`.

### Base Stats
- Max Life
- Max Mana
- Defense
- Movement Speed
- Movement acceleration
- Jump height/speed where reliable
- Breath capacity
- Knockback resistance

### Recovery & Sustain
- Fixed Life regeneration
- Natural Life regeneration multiplier
- Fixed Mana regeneration
- Natural Mana regeneration multiplier
- Healing/mana restoration modifiers
- Potion sickness / Debuff duration reduction where reliable

### Combat
- Global damage
- Attack speed
- Critical strike chance
- Critical damage
- Armor penetration
- Knockback
- Projectile speed where feasible
- Ammo conservation
- Mana cost reduction
- Minion / sentry capacity
- Invulnerability-frame enhancement if compatibility is acceptable
- Layered critical behavior above 100% after compatibility validation

### Economy & Resources
- Coin drop multiplier
- Loot quantity multiplier
- Dynamic drop-chance strengthening
- Mining yield
- Wood yield
- Herb / gem / fishing yield
- Pickup range
- Shop / sell / reforge modifiers where reliable

## P2 — Utility / Accessory-like Abilities
Goal: grant permanent utility and accessory-style powers without occupying real accessory slots.

Architecture first:
- `FunctionalTalentRegistry`
- Implementation kinds: `NativeFlag`, `NativeSystem`, `AccessoryBridge`, `Custom`, `Composite`
- Explicit stack/conflict/network policies
- One-time `U` unlocks default to 2 talent points
- Real equipped accessory + talent must not duplicate the same boolean/special effect by default

### P2-A — Highest-confidence effects
- No fall damage
- Unlimited underwater breathing
- Water walking
- Lava-surface walking
- Lava immunity
- Hot-tile/fire-block immunity
- Auto jump
- Multi-jump integration
- Knockback immunity
- All Information composite

### P2-B — Movement / building / fishing
- Dash
- Wall climb/slide
- Unlimited flight toggle
- Ice traction
- Selected building helpers
- Fishing-line protection / lava-fishing eligibility where reliable

### P2-C — Composite and combat-trigger effects
- Common status-immunity composite
- Selected on-hit/on-hurt accessory effects
- Selected attack-inflicted vanilla Debuffs
- Additional vanilla functions explicitly approved after testing

### P2-D — Developer discovery tooling
- `AccessoryTalentScanner`
- Report unmapped vanilla accessory candidates
- Scanner never auto-creates behavior or executes unknown third-party accessory logic
- Third-party auto-import remains experimental/off by default

Authoritative functional catalog:
- `docs/FUNCTIONAL_TALENTS_v0.1.md`

## P3 — Transcendent / World Interaction
Goal: high-power, high-risk abilities with strong multiplayer/world protections.

- Extreme mining speed
- Area mining with selectable active radius
- Vein mining with scalable chain limit
- One-action tree felling
- Area harvesting
- Auto-replanting
- Attack-driven terrain destruction
- Wall/building expansion helpers
- Protected-object filters
- Server master switches for world-altering abilities
- `MaxBlocksPerAction` performance protection (default 1000; 0 = unlimited)

## Later / Optional
- Presets / build profiles
- Import/export of talent configurations
- Additional mod compatibility adapters only when standard APIs are insufficient
- Explicit support modules for popular content mods
- Experimental third-party accessory bridging only after the vanilla functional registry is stable
- Character statistics and achievements based on lifetime XP
- Prestige/rebirth only if separately approved; it is not part of the current baseline

## Development policy
Each major roadmap item should be implemented in focused branches/PRs. Do not merge until the user explicitly accepts the in-game test result.
