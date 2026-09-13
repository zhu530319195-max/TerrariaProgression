using System;
using System.IO;
using System.Linq;
using System.Numerics;
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

public sealed class ProgressionHarness : Mod { }

public sealed class RuntimeChecks : ModSystem
{
    private bool ran;
    private int count;
    private ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    private ProgressionPlayer A => Main.player[0].GetModPlayer<ProgressionPlayer>();
    private ProgressionPlayer B => Main.player[1].GetModPlayer<ProgressionPlayer>();
    private void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        count++;
        Console.WriteLine("RUNTIME PASS: " + name);
    }
    private void Reset()
    {
        foreach (var n in Main.npc) n.active = false;
        ModContent.GetInstance<EncounterSystem>().ClearWorld();
        Main.netMode = NetmodeID.SinglePlayer;
        Main.myPlayer = 0;
        for (int i = 0; i < 2; i++) {
            Main.player[i].active = true;
            Main.player[i].dead = false;
            Main.player[i].GetModPlayer<ProgressionPlayer>().Initialize();
            Main.player[i].GetModPlayer<ProgressionPlayer>().SessionReady = true;
        }
        Config.HpToXpMultiplier = 1;
        Config.StatueExperienceMultiplier = 0;
        Config.ExperienceRequirementCap = 50000;
    }
    private NPC Spawn(int hp, IEntitySource? source = null)
    {
        int index = NPC.NewNPC(source ?? new EntitySource_Misc("CI"), Main.spawnTileX * 16, Main.spawnTileY * 16, NPCID.BlueSlime);
        var npc = Main.npc[index];
        npc.life = npc.lifeMax = hp;
        npc.aiStyle = -1;
        EncounterSystem.Observe(npc);
        return npc;
    }
    private void Finish()
    {
        var system = ModContent.GetInstance<EncounterSystem>();
        system.PostUpdateEverything();
        foreach (var npc in Main.npc.Where(n => !n.active)) {
            // Do not manufacture lifetimes for all unused slots.
            if (npc.lifeMax <= 0) continue;
        }
    }
    private void Settle(params NPC[] members)
    {
        var group = EncounterSystem.Get(members[0]).Group.Root;
        group.EmptySince = Main.GameUpdateCount - 3;
        ModContent.GetInstance<EncounterSystem>().PostUpdateEverything();
    }
    private static NPC.HitInfo Hit(int damage) => new() { Damage = damage, SourceDamage = damage, DamageType = DamageClass.Generic, HideCombatText = true };
    private void Kill(NPC npc, int player = 0)
    {
        var hit = Hit(npc.life);
        int dealt = npc.StrikeNPC(hit);
        // The game's item/projectile caller invokes the on-hit hook after StrikeNPC.
        NPCLoader.OnHitByItem(npc, Main.player[player], new Item(), hit, dealt);
    }
    public override void PostUpdateEverything()
    {
        if (ran || Main.GameUpdateCount < 10) return;
        ran = true;
        if (!Main.dedServ || Environment.GetEnvironmentVariable("TP_CI_HARNESS") != "1") {
            Console.WriteLine("CI HARNESS REFUSED: explicit environment flag required.");
            return;
        }
        try {
            Run();
            Console.WriteLine($"TP_RUNTIME_PASS {count}");
            Environment.Exit(0);
        }
        catch (Exception error) {
            Console.WriteLine("TP_RUNTIME_FAIL " + error);
            Environment.Exit(1);
        }
    }
    private void Run()
    {
        Reset();
        A.Award(1000 * Experience.Scale);
        var tag = new TagCompound(); A.SaveData(tag);
        using var stream = new MemoryStream();
        TagIO.ToStream(tag, stream); stream.Position = 0;
        var decoded = TagIO.FromStream(stream);
        B.LoadData(decoded);
        Check(B.State.Level == 4 && B.State.CurrentExperience == 50 * Experience.Scale, "real TagIO / SaveData / LoadData");
        A.Player.dead = true; A.UpdateDead(); A.OnRespawn();
        Check(A.State.Level == 4, "death hooks preserve progression");
        ModContent.GetInstance<EncounterSystem>().ClearWorld(); A.OnEnterWorld();
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "world clear preserves character");

        Reset(); var npc = Spawn(1000);
        Kill(npc);
        Check(A.State.TotalExperienceEarned == 0, "fatal strike waits for contribution callback");
        Settle(npc);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale && A.State.Level == 4, "native kill awards lifeMax and levels");
        NPCLoader.OnKill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "duplicate death callback pays once");

        Reset(); npc = Spawn(5); // Use actual rabbit so initial slime defaults cannot affect budget.
        npc.active = false;
        int rabbitSlot = NPC.NewNPC(new EntitySource_Misc("CI"), Main.spawnTileX * 16, Main.spawnTileY * 16, NPCID.Bunny);
        npc = Main.npc[rabbitSlot];
        int rabbitHp = npc.lifeMax;
        Kill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == rabbitHp * Experience.Scale, "critter XP uses actual lifeMax");

        foreach (float multiplier in new[] { 0f, .5f, 1f, 2f }) {
            Reset(); Config.StatueExperienceMultiplier = multiplier;
            npc = Spawn(100); npc.SpawnedFromStatue = true;
            Kill(npc); Settle(npc);
            Check(A.State.TotalExperienceEarned == Experience.FromDecimal(100m * (decimal)multiplier), "statue multiplier " + multiplier);
        }
        Reset(); npc = Spawn(100); npc.townNPC = true;
        Kill(npc); Settle(npc);
        Check(A.State.TotalExperienceEarned == 0, "town NPC excluded");

        Reset(); var root = Spawn(1000); var child = Spawn(1000, new EntitySource_Parent(root));
        Kill(child); Kill(root); Settle(root);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "parent/child budget is max once");
        Reset(); root = Spawn(1000); child = Spawn(1000);
        child.realLife = root.whoAmI; EncounterSystem.Observe(child);
        Kill(root); child.active = false; Settle(root);
        Check(A.State.TotalExperienceEarned == 1000 * Experience.Scale, "shared root kill with auto-removed body");
        Reset(); root = Spawn(1000); child = Spawn(1000);
        root.aiStyle = child.aiStyle = NPCAIStyleID.Worm;
        root.ai[0] = child.whoAmI; child.ai[1] = root.whoAmI;
        EncounterSystem.Observe(root); EncounterSystem.Observe(child);
        Kill(child); root.active = false; Settle(root);
        Check(A.State.TotalExperienceEarned == 0, "partial worm despawn has no completed-boss payout");

        Reset(); npc = Spawn(1000);
        ServerStrike(npc, 0, 700); ServerStrike(npc, 1, 999999); // Overkill must count only remaining 300.
        Main.netMode = NetmodeID.SinglePlayer; Settle(npc);
        Check(A.State.TotalExperienceEarned == 700 * Experience.Scale && B.State.TotalExperienceEarned == 300 * Experience.Scale,
            "official server strike reader + HitEffect: 700/300, overkill clipped");

        Reset(); npc = Spawn(1000);
        Kill(npc); A.PlayerDisconnect(); Settle(npc);
        Check(B.State.TotalExperienceEarned == 0, "disconnected contribution never moves to another slot");

        Reset(); Config.HpToXpMultiplier = float.NaN; Config.StatueExperienceMultiplier = -5; Config.ExperienceRequirementCap = -1; Config.OnChanged();
        Check(Config.HpToXpMultiplier == 1 && Config.StatueExperienceMultiplier == 0 && Config.ExperienceRequirementCap == 0, "config boundary validation");
        Main.netMode = NetmodeID.Server;
        var message = Terraria.Localization.NetworkText.Empty;
        Check(!Config.AcceptClientChanges(Config, 0, ref message), "server rejects client rule edits");
    }
    private void ServerStrike(NPC npc, int sender, int damage)
    {
        Main.netMode = NetmodeID.Server;
        Netplay.Clients[sender].State = 10;
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true)) {
            writer.Write((short)npc.whoAmI);
            writer.Write7BitEncodedInt(damage); writer.Write7BitEncodedInt(damage);
            writer.Write7BitEncodedInt(DamageClass.Generic.Type);
            writer.Write((sbyte)0); writer.Write(0f); writer.Write((byte)4);
        }
        stream.Position = 0;
        var reader = new BinaryReader(stream);
        byte messageType = 28;
        bool hijacked = ModContent.GetInstance<StrikeObserver>().HijackGetData(ref messageType, ref reader, sender);
        Check(!hijacked && stream.Position == 0, "strike observer preserves vanilla packet");
        npc.StrikeNPC(Hit(damage), fromNet: true);
        Netplay.Clients[sender].State = 0;
    }
}
