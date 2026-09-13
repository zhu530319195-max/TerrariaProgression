# ROADMAP.md

## P0 — Progression Core
Goal: stable character-bound infinite leveling foundation.

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
- Basic debug/test commands or diagnostics as needed

## P1 — Numeric Talents
Goal: implement stable, mostly numeric talents first.

### Base Stats
- Max Life
- Max Mana
- Defense
- Movement Speed
- Optional jump/mobility numeric stats after verification

### Recovery & Sustain
- Life regeneration
- Mana regeneration
- Mana recovery behavior
- Healing/mana restoration modifiers where safe

### Combat
- Global damage
- Attack speed
- Critical strike chance
- Critical damage
- Armor penetration
- Knockback
- Ammo conservation
- Mana cost reduction
- Minion / sentry capacity where appropriate

### Economy & Resources
- Coin drop multiplier
- Loot quantity multiplier
- Drop chance multiplier
- Mining yield
- Wood yield
- Additional gathering multipliers after base implementation stabilizes

## P2 — Utility / Accessory-like Abilities
Goal: grant vanilla-style utility effects without occupying accessory slots.

- Extra jumps
- Fall-damage immunity
- Water breathing
- Water walking
- Lava walking / lava immunity
- Hot-tile immunity
- Information accessory functions
- Debuff immunities
- Dash / wall movement / flight only after compatibility review

## P3 — Transcendent / World Interaction
Goal: high-power, high-risk abilities with strong multiplayer/world protections.

- Extreme mining speed
- Area mining with selectable active radius
- Vein mining
- One-action tree felling
- Area harvesting
- Auto-replanting
- Attack-driven terrain destruction
- Protected-object filters
- Server master switches for world-altering abilities

## Later / Optional
- Presets / build profiles
- Import/export of talent configurations
- Additional mod compatibility adapters only when standard APIs are insufficient
- Character statistics and achievements based on lifetime XP
- Prestige/rebirth only if separately approved; it is not part of the current baseline

## Development policy
Each major roadmap item should be implemented in focused branches/PRs. Do not merge until the user explicitly accepts the in-game test result.
