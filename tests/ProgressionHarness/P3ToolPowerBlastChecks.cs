using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunToolPowerBlast()
    {
        int x=Main.spawnTileX+70,y=Main.spawnTileY-10;
        void Init() {
            Reset();GatheringSystem.Clear();A.Award(1000000000*Experience.Scale);B.Award(1000000000*Experience.Scale);
            Config.EnableWorldGathering=Config.EnableBlastRadius=true;Config.MaxBlastRadius=32;Config.MaxBlocksPerAction=0;Config.GatheringWorkPerTick=256;Config.ProtectedTileAreas.Clear();
            A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);A.Player.selectedItem=0;A.Player.Center=new Vector2(x*16,y*16);A.Player.pickSpeed=1;
            Main.hardMode=false;Main.gameMenu=false;Main.drawingPlayerChat=false;A.Player.mouseInterface=false;
            for(int dx=-26;dx<=26;dx++)for(int dy=-26;dy<=26;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var i in Main.item)i.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
        }
        void Put(int dx,int dy,ushort type=TileID.Stone) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        bool Has(int dx,int dy)=>Main.tile[x+dx,y+dy].HasTile;
        void Buy(string id,int n=1)=>Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,n)==TalentResult.Success,"tool/blast purchase: "+id);
        void Toggle(string id,bool on)=>A.ApplyTalent(on?TalentOperation.Enable:TalentOperation.Disable,id,TalentCategory.Utility,1);
        int Items(int type)=>Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==type).Sum(i=>i.stack);
        var tool=typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool",BindingFlags.Instance|BindingFlags.NonPublic)!;
        void Use(int dx=0,int dy=0) {A.Player.itemTime=0;tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x+dx,y+dy});}
        Init();Put(0,0,TileID.Meteorite);
        for(int n=0;n<12;n++)Use();Check(Has(0,0),"copper pick remains below meteorite gate without talent");
        Buy("PickPower",2);for(int n=0;n<12&&Has(0,0);n++)Use();
        Check(!Has(0,0)&&Items(ItemID.Meteorite)==1&&A.Player.HeldItem.pick==35,"native manual pick crosses gate without mutating item");
        Put(0,0,TileID.Hellstone);for(int n=0;n<12;n++)Use();
        Check(Has(0,0),"native pick does not double apply +20 to reach 65-percent hellstone gate");
        Toggle("PickPower",false);Put(0,0,TileID.Meteorite);for(int n=0;n<12;n++)Use();Check(Has(0,0),"disabling restores native gate");
        Toggle("PickPower",true);for(int n=0;n<12&&Has(0,0);n++)GatheringSystem.Hit(A.Player,new(x,y),GatheringMode.Area);
        Check(!Has(0,0),"batch hit shares enhanced pick power");
        Put(0,0,504);Buy("PickPower",1000);for(int n=0;n<12;n++)Use();Check(Has(0,0),"huge pick talent retains native unbreakable tile 504 rule");
        Init();A.Player.inventory[0]=new Item(ItemID.CopperAxe);int original=A.Player.HeldItem.axe;Buy("AxePower",10);
        int capturedDamage=0;
        int DamageSpy(On_HitTile.orig_AddDamage orig,HitTile hit,int buffer,int damage,bool updateAmount) {
            if(ReferenceEquals(hit,A.Player.hitTile))capturedDamage=Math.Max(capturedDamage,damage);
            return orig(hit,buffer,damage,updateAmount);
        }
        On_HitTile.AddDamage+=DamageSpy;
        try {
            Put(0,1,TileID.Grass);Put(0,0,TileID.Trees);Use();
            Check(capturedDamage==(int)((original+20)*1.2f)&&A.Player.HeldItem.axe==original,"native axe applies exactly +100 displayed power then restores item");
            Check(ToolPowerSystem.AxePower(B.Player,A.Player.HeldItem)==original,"same item on another character gets no leaked power");
            capturedDamage=0;Put(0,0,TileID.Trees);GatheringSystem.Hit(A.Player,new(x,y),GatheringMode.Tree);
            Check(capturedDamage==(int)((original+20)*1.2f)&&A.Player.HeldItem.axe==original,"batch axe applies exactly one power bonus");
        } finally {On_HitTile.AddDamage-=DamageSpy;}
        // Binding serialization: the engine starts registered MOD keys empty.
        var keys=new KeyConfiguration();keys.SetupKeys();var markers=new HashSet<string>();
        foreach(var pair in DefaultKeybindRules.Defaults) keys.KeyStatus[pair.Key]=new();
        Check(DefaultKeybindRules.Initialize("Native/Keyboard",keys.KeyStatus,markers),"native empty input profile receives one-time defaults");
        var saved=keys.WritePreferences();var restored=new KeyConfiguration();restored.SetupKeys();foreach(var pair in DefaultKeybindRules.Defaults)restored.KeyStatus.TryAdd(pair.Key,new());restored.ReadPreferences(saved);
        Check(DefaultKeybindRules.Defaults.All(p=>restored.KeyStatus[p.Key].SequenceEqual(new[]{p.Value})),"native preferences persist all three default keys");
        restored.KeyStatus["TerrariaProgression/GatheringAction"].Clear();saved=restored.WritePreferences();keys.SetupKeys();foreach(var pair in DefaultKeybindRules.Defaults)keys.KeyStatus.TryAdd(pair.Key,new());keys.ReadPreferences(saved);
        DefaultKeybindRules.Initialize("Native/Keyboard",keys.KeyStatus,markers);
        Check(keys.KeyStatus["TerrariaProgression/GatheringAction"].Count==0,"native saved intentional clear is not replaced after migration");
        RunNativeBlasts();
        RunNativeMining();
    }
}
