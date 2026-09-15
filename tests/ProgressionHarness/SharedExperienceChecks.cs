using System;
using BigInteger = System.Numerics.BigInteger;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.NPCs;
using TerrariaProgression.Players;

namespace ProgressionHarness;

public sealed partial class RuntimeChecks
{
    private void RunSharedExperience()
    {
        void Shared() { Reset(); Config.ShareExperienceServerWide = true; }
        void Both(BigInteger expected, string message) => Check(A.State.TotalExperienceEarned == expected * Experience.Scale && B.State.TotalExperienceEarned == expected * Experience.Scale, message);
        try {
            Check(!new ProgressionConfig().ShareExperienceServerWide, "XP sharing defaults off");
            var legacy = JsonConvert.DeserializeObject<ProgressionConfig>("{\"TalentPointsPerLevel\":\"3\"}")!;
            Check(!legacy.ShareExperienceServerWide && legacy.PointsPerLevel == 3, "old configuration inherits sharing off without changing other settings");
            legacy.ShareExperienceServerWide = true;
            var roundTrip = JsonConvert.DeserializeObject<ProgressionConfig>(JsonConvert.SerializeObject(legacy))!;
            Check(roundTrip.ShareExperienceServerWide && roundTrip.PointsPerLevel == 3, "sharing survives configuration serialization");

            Reset(); var npc = Spawn(1000); ServerStrike(npc, 0, 700); ServerStrike(npc, 1, 999999); Settle(npc);
            Check(A.State.TotalExperienceEarned == 700 * Experience.Scale && B.State.TotalExperienceEarned == 300 * Experience.Scale, "sharing off retains real server 70/30 damage allocation");
            Reset(); npc = Spawn(100); Kill(npc); Settle(npc);
            Check(A.State.TotalExperienceEarned == 100 * Experience.Scale && B.State.TotalExperienceEarned == 0, "sharing off excludes spectators");

            Shared(); B.Player.position = new Vector2(500000, 500000); A.Player.team = 1; B.Player.team = 2; B.Player.dead = true;
            npc = Spawn(1000); ServerStrike(npc, 0, 999999); Settle(npc);
            Both(1000, "full XP for killer and distant dead non-participant on another team");
            Check(A.State.Level == 4 && B.State.Level == 4 && B.State.AvailableTalentPoints == 3, "shared XP uses normal leveling and talent point awards");
            Check(((RecordingSocket)Netplay.Clients[0].Socket).Sent > 0 && ((RecordingSocket)Netplay.Clients[1].Socket).Sent > 0, "server publishes real progression snapshot packets after shared awards");
            NPCLoader.OnKill(npc); Settle(npc); Both(1000, "duplicate death callback never repeats shared XP");
            var saved = new TagCompound(); B.SaveData(saved); var loaded = new Player { whoAmI = 2 }.GetModPlayer<ProgressionPlayer>(); loaded.Initialize(); loaded.LoadData(saved);
            Check(loaded.State.TotalExperienceEarned == B.State.TotalExperienceEarned && loaded.State.Level == B.State.Level, "shared XP persists through unchanged character save/load");

            Shared(); npc = Spawn(1000); ServerStrike(npc, 0, 700); ServerStrike(npc, 1, 999999); Settle(npc);
            Both(1000, "two contributors each receive full budget, with no extra 70/30 payment");

            Shared(); B.Player.active = false; npc = Spawn(100); Kill(npc); Settle(npc);
            Check(A.State.TotalExperienceEarned == 100 * Experience.Scale && B.State.TotalExperienceEarned == 0, "offline players receive no shared XP");
            B.Player.active = true; B.SessionReady = true; Settle(npc);
            Check(B.State.TotalExperienceEarned == 0, "joining after settlement gets no catch-up reward");
            npc = Spawn(200); Kill(npc); Settle(npc);
            Check(A.State.TotalExperienceEarned == 300 * Experience.Scale && B.State.TotalExperienceEarned == 200 * Experience.Scale, "sharing adds only new XP and never equalizes old totals");

            Shared(); B.SessionReady = false; npc = Spawn(100); Kill(npc); Settle(npc);
            Check(B.State.TotalExperienceEarned == 0, "uninitialized character receives no shared XP");
            B.SessionReady = true; Settle(npc); Check(B.State.TotalExperienceEarned == 0, "initialization after settlement has no backfill");
            Shared(); B.SessionReady = false; npc = Spawn(100); Kill(npc); B.SessionReady = true; Settle(npc);
            Both(100, "recipient population is determined at settlement time");

            Shared(); npc = Spawn(100); Kill(npc); A.PlayerDisconnect(); Settle(npc);
            Check(A.State.TotalExperienceEarned == 0 && B.State.TotalExperienceEarned == 100 * Experience.Scale, "offline contributor excluded but other online player gets full eligible reward");
            A.Initialize(); A.SessionReady = true; Settle(npc); Check(A.State.TotalExperienceEarned == 0, "new character session cannot reclaim a settled reward");

            Shared(); npc = Spawn(100); npc.StrikeNPC(Hit(npc.life)); Settle(npc); Both(0, "environment kill without player contribution grants no server-wide XP");
            Shared(); npc = Spawn(100); EncounterSystem.ReportAttributedDamage(npc, A.Player, 1); npc.active = false; Settle(npc); Both(0, "despawn with past contribution does not create shared XP");
            Shared(); npc = Spawn(100); npc.townNPC = true; Kill(npc); Settle(npc); Both(0, "town NPC remains excluded in shared mode");
            foreach (float multiplier in new[] { 0f, .5f, 2f }) {
                Shared(); Config.StatueExperienceMultiplier = multiplier; Config.HpToXpMultiplier = 1.5f;
                npc = Spawn(100); npc.SpawnedFromStatue = true; Kill(npc); Settle(npc);
                Both((int)(150 * multiplier), "shared XP preserves statue and HP multipliers: " + multiplier);
            }
            Shared(); var root = Spawn(1000); var child = Spawn(500, new EntitySource_Parent(root)); Kill(child); Kill(root); Settle(root);
            Both(1000, "linked NPC family shares max budget once, not per-part or summed XP");
            NPCLoader.OnKill(child); NPCLoader.OnKill(root); Settle(root); Both(1000, "repeated multipart callbacks do not multiply shared XP");

            Shared(); npc = Spawn(100); Kill(npc); Main.netMode = NetmodeID.MultiplayerClient; Settle(npc); Both(0, "client cannot settle shared experience locally");
            Main.netMode = NetmodeID.Server; Settle(npc); Both(100, "authority settles shared experience exactly once after client attempt");

            Shared(); npc = Spawn(100); Kill(npc); Config.ShareExperienceServerWide = false; Settle(npc);
            Check(A.State.TotalExperienceEarned == 100 * Experience.Scale && B.State.TotalExperienceEarned == 0, "sharing toggle uses policy at settlement");
            Config.ShareExperienceServerWide = true; Settle(npc); Check(B.State.TotalExperienceEarned == 0, "enabling sharing does not replay previous kills");
            Config.TalentPointsPerLevel = "2"; npc = Spawn(1000); Kill(npc); Settle(npc);
            Check(B.State.TotalExperienceEarned == 1000 * Experience.Scale && B.State.AvailableTalentPoints == 6, "shared level-up respects configured points per level");
            Config.RestoreDefaultTalentLimits = true; Config.OnChanged(); Check(Config.ShareExperienceServerWide, "reset talent limits leaves sharing option intact");
            var message = Terraria.Localization.NetworkText.Empty; Main.netMode = NetmodeID.Server;
            Check(!Config.AcceptClientChanges(Config, 0, ref message), "sharing adds no remote server configuration permission");
            Shared(); A.Award(100 * Experience.Scale); Check(B.State.TotalExperienceEarned == 0, "direct developer XP award is not broadcast as a kill");
        }
        finally { Reset(); Config.OnChanged(); }
    }
}
