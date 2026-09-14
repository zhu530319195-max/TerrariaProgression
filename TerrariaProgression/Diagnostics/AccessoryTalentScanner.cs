using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using TerrariaProgression.Core;

namespace TerrariaProgression.Diagnostics;

internal sealed record AccessoryMapping(bool Complete, string[] Talents, string Note);
internal sealed record AccessoryCandidate(int ItemId, string Source, string InternalName, string DisplayName,
    string Culture, string? NameZh, string? NameEn, string Tooltip, bool Accessory, bool Information,
    string MappingStatus, string[] TalentIds, string[] ImplementationKinds, string[] CompatibilityGrades,
    string Validation, string Note);

internal static class AccessoryTalentScanner
{
    // Explicit documentation mappings only. Composite items deliberately retain
    // partial status until every effect has been reviewed. Never run item hooks.
    private static AccessoryMapping Full(params string[] ids) => new(true, ids, "Known functional effects mapped; mapping is not a compatibility test.");
    private static AccessoryMapping Partial(string note, params string[] ids) => new(false, ids, note);
    private static readonly IReadOnlyDictionary<int,AccessoryMapping> Mappings = new Dictionary<int,AccessoryMapping> {
        [ItemID.LuckyHorseshoe] = Full("NoFallDamage"),
        [ItemID.ObsidianSkull] = Partial("Native defense bonus is not part of hot-tile immunity.","HotTileImmunity"),
        [ItemID.CobaltShield] = Partial("Native defense bonus remains an independent stat.","KnockbackImmunity"),
        [ItemID.ObsidianShield] = Partial("Defense and composite equipment behavior not reproduced.","HotTileImmunity","KnockbackImmunity"),
        [ItemID.AnkhCharm] = Partial("Compare individual immunity coverage; composite equivalence not asserted.","StatusImmunity"),
        [ItemID.AnkhShield] = Partial("Defense and full composite equivalence not asserted.","StatusImmunity","KnockbackImmunity","HotTileImmunity"),
        [ItemID.CloudinaBottle] = Partial("MultiJump supplies a custom extra-jump sequence, not this item's exact jump.","MultiJump"),
        [ItemID.Tabi] = Full("Dash"),
        [ItemID.TigerClimbingGear] = Full("WallClimb","WallSlide"),
        [ItemID.ShoeSpikes] = Partial("Tier-one equipment has native wall behavior; talent modes are independent.","WallSlide"),
        [ItemID.ClimbingClaws] = Partial("Tier-one equipment has native wall behavior; talent modes are independent.","WallSlide"),
        [ItemID.IceSkates] = Full("IceTraction"),
        [ItemID.WaterWalkingBoots] = Full("WaterWalking"),
        [ItemID.ObsidianWaterWalkingBoots] = Full("WaterWalking","HotTileImmunity"),
        [ItemID.LavaCharm] = Partial("Timed equipment lava protection differs from the permanent talent.","LavaImmunity"),
        [ItemID.Flipper] = Partial("Swim speed does not grant flipper swimming jumps.","SwimSpeed"),
        [ItemID.AngelWings] = Partial("Talents need existing wings and do not grant this item.","FlightTime","FlightSpeed","UnlimitedFlight","NoFallDamage"),
        [ItemID.StarCloak] = Full("StarRetaliation"),
        [ItemID.HoneyComb] = Partial("Native Honey healing is not granted by the talent.","BeeRetaliation"),
        [ItemID.PanicNecklace] = Full("PanicSpeed"),
        [ItemID.HighTestFishingLine] = Full("FishingLine"),
        [ItemID.TackleBox] = Partial("Talent uses scalable probability, not the tackle-box formula.","BaitSaving"),
        [ItemID.ArchitectGizmoPack] = Partial("Combined reach/speed/material traits require individual review.","BuildReach","PlacementSpeed","AutoPaint"),
        [ItemID.PaintSprayer] = Full("AutoPaint"),
        [ItemID.Ruler] = Partial("Native ruler is also available freely; no exclusive paid ownership.","BuildingRuler"),
        [ItemID.GPS] = Partial("Three readouts are covered by the larger information composite.","AllInformation"),
        [ItemID.FishFinder] = Partial("Three readouts are covered by the larger information composite.","AllInformation"),
        [ItemID.PDA] = Full("AllInformation")
    };
    internal static IReadOnlyList<AccessoryCandidate> Scan()
    {
        var culture=Language.ActiveCulture.Name;
        var rows=new List<AccessoryCandidate>();
        foreach(var pair in ContentSamples.ItemsByType.OrderBy(p=>p.Key)) {
            var item=pair.Value;
            if(pair.Key<=0 || !item.accessory)continue;
            bool vanilla=item.ModItem==null;
            AccessoryMapping? mapping=vanilla?Mappings.GetValueOrDefault(pair.Key):null;
            var ids=mapping?.Talents??Array.Empty<string>();
            // Loaded static tooltip only: no ModifyTooltips, SetDefaults, UpdateAccessory,
            // reflection differences or language changes during discovery.
            var tooltip=Lang.GetTooltip(pair.Key);
            string description=string.Join("\n",Enumerable.Range(0,tooltip.Lines).Select(tooltip.GetLine));
            rows.Add(new(pair.Key,vanilla?"Terraria":item.ModItem!.Mod.Name,
                vanilla?ItemID.Search.GetName(pair.Key):item.ModItem!.Name,item.Name,culture,
                culture.StartsWith("zh",StringComparison.Ordinal)?item.Name:null,culture=="en-US"?item.Name:null,
                description,true,ids.Contains("AllInformation"),
                mapping==null?"Unmapped":mapping.Complete?"Mapped":"Partial",ids,
                ids.Select(id=>FunctionalTalentRegistry.TryGet(id,out var t)?t.ImplementationKind.ToString():"Numeric").Distinct().ToArray(),
                mapping==null?new[]{"X"}:ids.Select(id=>FunctionalTalentRegistry.TryGet(id,out var t)?t.CompatibilityGrade:"Numeric").Distinct().ToArray(),
                "Candidate only; no test was performed by this scan.",
                mapping?.Note??(vanilla?"Manual effect review required.":"Unknown third-party logic was not executed.")));
        }
        return rows;
    }
    internal static string WriteReport(string? directory=null)
    {
        directory??=Path.Combine(Main.SavePath,"TerrariaProgression","Reports");
        Directory.CreateDirectory(directory);
        string stem=Path.Combine(directory,"accessories-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
        var rows=Scan();
        var report=new { SchemaVersion=1, ModVersion="0.8.0", GeneratedUtc=DateTime.UtcNow, Culture=Language.ActiveCulture.Name,
            Notice="Passive metadata report. Mapped is not in-game acceptance; unavailable language names are null. No effects were run.",
            Summary=new {Total=rows.Count,Mapped=rows.Count(r=>r.MappingStatus=="Mapped"),Partial=rows.Count(r=>r.MappingStatus=="Partial"),Unmapped=rows.Count(r=>r.MappingStatus=="Unmapped")},
            Whitelist=Mappings.Select(p=>new {ItemId=p.Key,p.Value.Complete,p.Value.Talents,p.Value.Note}).ToArray(),
            Registry=TalentCatalog.All.Select(t=>new {t.Id,Menu=TalentNavigation.Find(t.Id)?.CategoryId,Group=TalentNavigation.Find(t.Id)?.Id,
                HasSourceMapping=Mappings.Values.Any(m=>m.Talents.Contains(t.Id)),
                Implementation=FunctionalTalentRegistry.TryGet(t.Id,out var f)?f.ImplementationKind.ToString():"Numeric"}).ToArray(),
            Items=rows };
        File.WriteAllText(stem+".json",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}),Encoding.UTF8);
        static string Csv(string value) {
            // A mod-provided name is data, not a spreadsheet formula.
            if(value.Length>0 && "=+-@\t\r\n".Contains(value[0])) value="'"+value;
            return "\""+value.Replace("\"","\"\"")+"\"";
        }
        var csv=new StringBuilder("ItemId,Source,InternalName,DisplayName,Culture,MappingStatus,Talents,Compatibility,Note\n");
        foreach(var row in rows) csv.AppendLine(string.Join(",",new[]{row.ItemId.ToString(),row.Source,row.InternalName,row.DisplayName,row.Culture,row.MappingStatus,string.Join(";",row.TalentIds),string.Join(";",row.CompatibilityGrades),row.Note}.Select(Csv)));
        File.WriteAllText(stem+".csv",csv.ToString(),new UTF8Encoding(true));
        return stem;
    }
}
