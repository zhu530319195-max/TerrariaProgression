# DECISIONS.md

This file records accepted product/architecture decisions. New entries should be appended rather than silently rewriting history.

## D001 — Independent progression module
TerrariaProgression is developed independently from Xiaoyu visual replacement work. Visual sprites/hair/PlayerDrawLayer are out of scope for this repository.

## D002 — Character-bound infinite level
Progress is stored on the Terraria character, not the world. Initial level is 1 and there is no level cap.

## D003 — No death/world transition penalty
Death, changing worlds, and switching between singleplayer and multiplayer do not reduce level or experience.

## D004 — XP derives from NPC maximum life
Base kill XP uses `NPC.lifeMax`. Default conversion is 1.00 XP per max-HP point. The configurable range is 0.01–10.00.

## D005 — Upgrade requirement curve and cap
Base requirement for the next level is:

`250 + 50 * (level - 1) + 10 * (level - 1)^2`

The actual requirement is capped by a configurable per-level maximum. Default cap: 50,000 XP. Config value `0` means no cap.

## D006 — Statue XP is configurable
Statue-spawned NPC experience is controlled by a multiplier. A value of 0 disables XP; 1.0 means normal XP.

## D007 — Town NPCs do not give XP by default
Town NPC kills are excluded from normal experience to avoid trivial farming.

## D008 — Multiplayer XP is not last-hit-only
XP is allocated by meaningful participation/damage contribution. The server is authoritative for final settlement.

## D009 — Level-up grants talent points
Each level grants talent points. Default: 1 point per level. Large XP awards may produce multiple level-ups in one settlement.

## D010 — Talent disable and refund are different operations
Disabling a talent temporarily suppresses its effect without changing its level or invested points. Refunding lowers/reset levels and returns the points actually paid.

## D011 — Free reversible respec
Current design allows unlimited talent rollback/refund with no additional penalty. Exact paid costs must be stored so configuration changes cannot create refund exploits.

## D012 — Six talent categories
Approved categories: Base Stats; Recovery & Sustain; Combat; Economy & Resources; Utility/Accessory-like Abilities; Transcendent/World Interaction.

## D013 — High-risk world abilities require server authority
Area mining, vein mining, tree felling, terrain destruction, auto-replanting and similar world-changing effects must respect server-side control and protection rules.

## D014 — Mod compatibility should be data/API driven
Prefer standard tModLoader runtime properties and DamageClass/API behavior over hard-coded vanilla or third-party content lists, so modded NPCs/items work automatically when possible.

## D015 — P1 values are not finalized
The catalog is approved conceptually, but exact default effect-per-level, costs, and caps for P1 talents remain a design task. Implementation must not invent permanent balance values without approval.
