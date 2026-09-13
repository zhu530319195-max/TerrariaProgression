using System.Numerics;
using TerrariaProgression.Core;

int checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
void Bad(Action action, string message) {
    bool rejected = false;
    try { action(); } catch (Exception e) when (e is IOException or InvalidDataException) { rejected = true; }
    Check(rejected, message);
}
var scale = Experience.Scale;
Check(Experience.Requirement(1, 50000) == 250 * scale, "Lv1 requirement");
Check(Experience.Requirement(2, 50000) == 310 * scale, "Lv2 requirement");
Check(Experience.Requirement(69, 50000) == 49890 * scale, "last uncapped level");
Check(Experience.Requirement(70, 50000) == 50000 * scale, "cap starts at 70");
Check(Experience.Requirement(70, 0) == 51310 * scale, "zero disables cap");
Check(Experience.Requirement(BigInteger.Pow(10, 80), 50000) == 50000 * scale, "huge level capped");
Check(Experience.Requirement(BigInteger.Pow(10, 80), 0) > BigInteger.Pow(10, 160), "no integer overflow");
Check(Experience.Format(Experience.FromDecimal(5m)) == "5", "rabbit and formatting");
Check(Experience.Format(Experience.FromDecimal(.05m)) == "0.05", "fractional XP");
foreach (string text in new[] { "-1", "NaN", "Infinity", "1e50", ".", "1.0000001", "1..1", " 5", "+5" })
    Check(!Experience.TryParse(text, out _), "reject invalid XP " + text);
Check(Experience.TryParse("1000.000001", out var parsed) && parsed == 1000 * scale + 1, "parse micro XP");

var state = new ProgressionState();
Check(state.Award(1000 * scale, 50000) == 3 && state.Level == 4 && state.CurrentExperience == 50 * scale && state.AvailableTalentPoints == 3, "1000 XP multi-level");
state = new();
state.Award(Experience.Cost(1, 9, 50000), 50000);
Check(state.Level == 10, "reach lv10");
Check(state.Award(Experience.Cost(10, 4, 50000), 50000) == 4 && state.Level == 14 && state.AvailableTalentPoints == 13, "lv10 to lv14");

var random = new Random(116);
for (int i = 0; i < 1500; i++) {
    int level = random.Next(1, 140), cap = new[] { 0, 1, 100, 50000, 12345 }[random.Next(5)];
    BigInteger xp = random.Next(0, cap > 0 ? Math.Min(1000000, cap * 2000) : 1000000) * scale + random.Next(0, 1000000);
    BigInteger slowXp = xp, slowLevel = level;
    while (slowXp >= Experience.Requirement(slowLevel, cap)) {
        slowXp -= Experience.Requirement(slowLevel, cap); slowLevel++;
    }
    var fast = new ProgressionState { Level = level };
    fast.Award(xp, cap);
    Check(fast.Level == slowLevel && fast.CurrentExperience == slowXp, "fast settlement matches literal level loop");
}
state = new();
state.Award(BigInteger.Pow(10, 100) * scale, 50000);
Check(state.CurrentExperience < 50000 * scale && state.Level > BigInteger.Pow(10, 90), "enormous award completes");
var copy = StateCodec.Decode(StateCodec.Encode(state));
Check(copy.Level == state.Level && copy.TotalExperienceEarned == state.TotalExperienceEarned, "huge save roundtrip");
state = new();
for (int i = 0; i < 5000; i++) state.Award(Experience.FromDecimal(.05m), 50000);
Check(state.Level == 2 && state.CurrentExperience == 0, "fractional accumulation exact");
state.Award(10000 * scale, 50000);
Check(state.Invest("future:test", 2), "invest first cost");
Check(state.Invest("future:test", 3), "invest changed cost");
state.Talents["future:test"].Enabled = false;
state.Talents["future:test"].CurrentIntensity = .5m;
copy = StateCodec.Decode(StateCodec.Encode(state));
Check(!copy.Talents["future:test"].Enabled && copy.TotalSpentTalentPoints == 5 && copy.Talents["future:test"].CurrentIntensity == .5m, "toggle/intensity persist without refund");
Check(copy.RefundOne("future:test") == 3 && copy.RefundOne("future:test") == 2 && copy.TotalSpentTalentPoints == 0, "refund actual paid costs in reverse order");
byte[] bytes = StateCodec.Encode(copy);
Bad(() => StateCodec.Decode(bytes[..^1]), "truncated save rejected");
Bad(() => StateCodec.Decode(bytes.Concat(new byte[] { 0 }).ToArray()), "trailing save bytes rejected");
bytes[0] = 99;
Bad(() => StateCodec.Decode(bytes), "future save version rejected");
copy.AvailableTalentPoints++;
Bad(() => StateCodec.Decode(StateCodec.Encode(copy)), "inconsistent points rejected");

Guid a = Guid.Parse("00000000-0000-0000-0000-000000000001"), b = Guid.Parse("00000000-0000-0000-0000-000000000002");
var ledger = new ContributionLedger();
ledger.Add(a, 700); ledger.Add(b, 300);
var shares = ledger.Allocate(1000 * scale);
Check(shares[a] == 700 * scale && shares[b] == 300 * scale, "70/30 contribution");
Check(ledger.Allocate(0).Count == 0, "statue zero multiplier awards zero");
Check(ledger.Allocate(500 * scale)[a] == 350 * scale && ledger.Allocate(2000 * scale)[b] == 600 * scale, "statue half/double shares");
var merged = new ContributionLedger(); merged.Add(a, 700); merged.Add(b, 300); ledger.Merge(merged);
shares = ledger.Allocate(BigInteger.Pow(10, 150) + 1);
Check(shares.Values.Aggregate(BigInteger.Zero, (x,y) => x+y) == BigInteger.Pow(10, 150) + 1, "allocation conserves every unit at huge values");
// P1: version-1 save migration, compressed unlimited paid levels and transactions.
using (var legacy = new MemoryStream()) {
    using (var w = new BinaryWriter(legacy, System.Text.Encoding.UTF8, true)) {
        w.Write(1);
        foreach (BigInteger value in new BigInteger[] { 6, 0, 10000 * scale, 0, 5 }) StateCodec.WriteInteger(w, value);
        w.Write(1); w.Write("MaxLife"); w.Write(false); w.Write(false); w.Write(2);
        StateCodec.WriteInteger(w, 2); StateCodec.WriteInteger(w, 3);
    }
    var migrated = StateCodec.Decode(legacy.ToArray());
    Check(migrated.Talents["MaxLife"].TalentLevel == 2 && !migrated.Talents["MaxLife"].Enabled, "P0 legacy paid costs and disabled state migrate");
    Check(migrated.RefundOne("MaxLife") == 3 && migrated.RefundAll("MaxLife") == 2, "legacy actual cost refunds survive migration");
}
state = new(); state.Award(BigInteger.Pow(10, 100) * scale, 50000);
Check(state.Invest("MaxLife", 1, BigInteger.Pow(10, 30)), "unlimited compressed talent investment");
Check(state.Talents["MaxLife"].CostRuns.Count == 1 && StateCodec.Encode(state).Length < 512, "huge same-price levels fit a compact snapshot");
copy = StateCodec.Decode(StateCodec.Encode(state));
Check(copy.RefundOne("MaxLife") == 1 && copy.RefundAll("MaxLife") == BigInteger.Pow(10, 30) - 1, "constant-time huge refunds preserve exact costs");
Check(NumericTalents.All.Count == 47 && NumericTalents.All.All(t => t.DefaultCost == 1 && t.MaxLevel == 0), "47 implemented unlimited numeric talents registered");
state = new(); state.Award(1000 * scale, 50000);
var result = TalentCatalog.Apply(state, TalentOperation.Upgrade, "MaxLife", TalentCategory.BaseStats, 1, out copy);
Check(result == TalentResult.Success && copy.AvailableTalentPoints == 2 && state.AvailableTalentPoints == 3, "transaction copy commits without mutating original");
state = copy;
TalentCatalog.Apply(state, TalentOperation.Disable, "MaxLife", TalentCategory.BaseStats, 1, out copy);
Check(copy.AvailableTalentPoints == 2 && copy.TotalSpentTalentPoints == 1 && NumericTalents.ActiveLevel(copy,"MaxLife") == 0, "disable suppresses effect without refund");
TalentCatalog.Apply(copy, TalentOperation.Upgrade, "MaxLife", TalentCategory.BaseStats, 1, out state);
Check(state.Talents["MaxLife"].TalentLevel == 2 && !state.Talents["MaxLife"].Enabled, "upgrading disabled talent does not silently enable it");
Check(TalentCatalog.Apply(state, TalentOperation.Upgrade, "MaxMana", TalentCategory.BaseStats, 2, out copy) == TalentResult.NotEnoughPoints && ReferenceEquals(state,copy), "insufficient batch purchase atomic");
Check(TalentCatalog.Apply(state, TalentOperation.Upgrade, "Unknown", TalentCategory.BaseStats, 1, out _) == TalentResult.UnknownTalent, "unknown talent rejected");
Check(TalentCatalog.Apply(state, (TalentOperation)99, "MaxLife", TalentCategory.BaseStats, 1, out _) == TalentResult.InvalidRequest, "invalid operation rejected");
Check(TalentCatalog.Apply(state, TalentOperation.Upgrade, "MaxLife", TalentCategory.BaseStats, 101, out _) == TalentResult.InvalidRequest, "unbounded request rejected");
Check(!TalentCatalog.ValidateImported(new ProgressionState { Talents = { ["Unknown"] = new TalentState() } }), "unregistered imported talent rejected");
state = new(); state.Award(1000000 * scale, 50000);
for (int i = 0; i < 500; i++) {
    var def = NumericTalents.All[random.Next(NumericTalents.All.Count)];
    var op = (TalentOperation)random.Next(12);
    TalentCatalog.Apply(state, op, def.Id, def.Category, 1, out state);
    copy = StateCodec.Decode(StateCodec.Encode(state));
    Check(copy.AvailableTalentPoints + copy.TotalSpentTalentPoints == copy.Level - 1 && copy.TotalSpentTalentPoints == copy.Talents.Values.Aggregate(BigInteger.Zero,(sum,t)=>sum+t.InvestedPoints), "random purchase/refund/toggle preserves point conservation");
}
state = new(); state.Award(10000 * scale, 50000); state.Invest("MaxLife", 2); state.Invest("MaxLife", 3); state.Invest("Damage",1);
TalentCatalog.Apply(state,TalentOperation.RefundCategory,"",TalentCategory.BaseStats,1,out copy);
Check(copy.TotalSpentTalentPoints == 1 && copy.Talents.ContainsKey("Damage") && !copy.Talents.ContainsKey("MaxLife"),"category refund preserves other categories and historic costs");
TalentCatalog.Apply(copy,TalentOperation.DisableEverything,"",TalentCategory.BaseStats,1,out state);
Check(!state.Talents["Damage"].Enabled && state.TotalSpentTalentPoints == 1,"disable all retains investments");
TalentCatalog.Apply(state,TalentOperation.RefundEverything,"",TalentCategory.BaseStats,1,out copy);
Check(copy.TotalSpentTalentPoints == 0 && copy.Talents.Count == 0,"refund all returns all actual points");
double carry = 0; int mana = 0;
for(int tick = 0; tick < 60; tick++) mana = TalentMath.Recover(mana, 100, 2, ref carry);
Check(mana == 2 && carry < 1e-7,"fixed 2 MP/sec retains fractional ticks");
Check(TalentMath.Recover(99,100,double.MaxValue/1024,ref carry)==100 && carry==0,"huge recovery clips in constant time without carry bank");
Check(Math.Abs(TalentMath.Remaining(.95,10) - .5987369392383787)<1e-10,"mana reduction retains confirmed multiplicative curve");
Check(Math.Abs(TalentMath.Remaining(.93,10) - .4839823071792932)<1e-10,"ammo reduction retains confirmed multiplicative curve");
// P1 completion: tier boundaries, exact quantity tails, luck distribution and modes.
Check(TalentMath.CritTier(0, 0, 0) == 0, "zero crit stays ordinary");
Check(TalentMath.CritTier(10, 0, .999) == 1, "level 10 grants guaranteed first tier");
Check(TalentMath.CritTier(20, 0, .999) == 2, "level 20 grants guaranteed second tier");
Check(TalentMath.CritTier(10, 25, .249) == 2 && TalentMath.CritTier(10, 25, .25) == 1, "125 percent crit has exact tier boundary");
Check(TalentMath.CritTier(BigInteger.Pow(10,100), 0, .5) == BigInteger.Pow(10,99), "huge tier math constant time");
Check(TalentMath.Quantity(5, 1, .49) == 6 && TalentMath.Quantity(5,1,.5)==5, "quantity 5.5 has 50 percent tail");
Check(TalentMath.Quantity(12, 10, .99)==24 && TalentMath.Quantity(12,0,0)==12, "quantity level 10 doubles and zero leaves original");
Check(Math.Abs(TalentMath.DropProbability(.01,10)-.0199)<1e-12 && Math.Abs(TalentMath.DropProbability(.5,10)-.75)<1e-12, "approved drop chance formula");
for (int denominator = 1; denominator <= 250; denominator++) {
    foreach (double luck in new[] { -.7, 0d, .7 }) {
        int numerator = Math.Max(1, denominator / 3);
        int lo = luck > 0 ? denominator/2 : denominator, hi = luck > 0 ? denominator : 2*denominator;
        double sum = 0;
        for (int k=lo;k<hi;k++) sum += k==0 ? 1 : Math.Min(1,(double)numerator/k);
        double brute = (double)numerator/denominator*(1-Math.Abs(luck)) + sum/(hi-lo)*Math.Abs(luck);
        Check(Math.Abs(TalentMath.LuckProbability(numerator,denominator,luck)-brute)<1e-11, "vanilla luck matches exhaustive denominator distribution");
    }
}
state = new(); state.Award(1000000 * scale, 50000); state.Invest("MeleeRange", 1, 10);
TalentCatalog.Apply(state,TalentOperation.DecreaseIntensity,"MeleeRange",TalentCategory.Combat,5,out copy);
Check(NumericTalents.ActiveLevel(copy,"MeleeRange")==5 && copy.TotalSpentTalentPoints==10 && state.Talents["MeleeRange"].CurrentIntensity==null,"mode changes are atomic without refund");
Check(TalentCatalog.ValidateImported(StateCodec.Decode(StateCodec.Encode(copy))),"intensity survives save and validated import");
TalentCatalog.Apply(copy,TalentOperation.MaximumIntensity,"MeleeRange",TalentCategory.Combat,1,out state);
Check(NumericTalents.ActiveLevel(state,"MeleeRange")==10,"maximum mode restores purchased strength");
copy.Talents["MeleeRange"].CurrentIntensity=11;
Check(!TalentCatalog.ValidateImported(copy),"over-level imported intensity rejected");
copy.Talents["MeleeRange"].CurrentIntensity=.5m;
Check(!TalentCatalog.ValidateImported(copy),"fractional active level rejected");
Check(TalentCatalog.Apply(state,TalentOperation.DecreaseIntensity,"Damage",TalentCategory.Combat,1,out _)==TalentResult.InvalidRequest,"unsupported intensity change rejected");
Console.WriteLine($"PASS: {checks} core checks (formulas, capped/uncapped multi-level, precision, save validation, refunds, multiplayer allocation).");

Check(TalentMath.ExtraRolls(0, 0) == 0, "zero extra rolls");
Check(TalentMath.ExtraRolls(5, .49) == 1 && TalentMath.ExtraRolls(5, .5) == 0, "fractional extra roll threshold");
Check(TalentMath.ExtraRolls(10, .9) == 1 && TalentMath.ExtraRolls(20, .9) == 2, "whole additional loot rolls");
Check(TalentMath.ExtraRolls(BigInteger.Pow(10, 80), .5) == BigInteger.Pow(10, 79), "unbounded level roll arithmetic");
Console.WriteLine($"Final core checks passed: {checks}");

// P2: variable lifetime rewards, old v2 import, binary purchases and child settings.
state = new(); state.Award(1000 * scale, 50000, 5);
Check(state.Level == 4 && state.TotalTalentPointsEarned == 15 && state.AvailableTalentPoints == 15, "three new levels pay configured five points");
state.Award(Experience.Requirement(state.Level,50000)-state.CurrentExperience,50000,BigInteger.Pow(10,50));
Check(state.TotalTalentPointsEarned == 15+BigInteger.Pow(10,50), "huge reward has no int ceiling and preserves historic awards");
state.Award(Experience.Requirement(state.Level,50000),50000,0);
Check(state.Level == 6 && state.TotalTalentPointsEarned == 15+BigInteger.Pow(10,50), "zero reward still advances level");
Check(StateCodec.Decode(StateCodec.Encode(state)).TotalTalentPointsEarned == state.TotalTalentPointsEarned, "variable reward ledger round trip");
using (var legacy = new MemoryStream()) {
    using var writer = new BinaryWriter(legacy, System.Text.Encoding.UTF8, true);
    writer.Write(2);
    foreach (BigInteger n in new BigInteger[]{4,50*scale,1000*scale,1,2}) StateCodec.WriteInteger(writer,n);
    writer.Write(1); writer.Write("MaxLife"); writer.Write(false); writer.Write(false); writer.Write(1);
    StateCodec.WriteInteger(writer,2); StateCodec.WriteInteger(writer,1); writer.Flush();
    var migrated = StateCodec.Decode(legacy.ToArray());
    Check(migrated.TotalTalentPointsEarned == 3 && migrated.RefundAll("MaxLife") == 2 && migrated.AvailableTalentPoints == 3, "v2 paid costs and total rewards migrate exactly");
}
state = new(); state.Award(10000*scale,50000);
Check(TalentCatalog.Apply(state,TalentOperation.Upgrade,"NoFallDamage",TalentCategory.Utility,1,out copy)==TalentResult.Success && copy.TotalSpentTalentPoints==2,"binary unlock costs two points");
Check(TalentCatalog.Apply(copy,TalentOperation.Upgrade,"NoFallDamage",TalentCategory.Utility,1,out _)==TalentResult.NoChange,"binary repurchase forbidden");
TalentCatalog.Apply(copy,TalentOperation.RefundTalent,"NoFallDamage",TalentCategory.Utility,1,out state);
Check(state.TotalSpentTalentPoints==0 && state.AvailableTalentPoints==state.TotalTalentPointsEarned,"binary refund returns two paid points");
TalentCatalog.Apply(state,TalentOperation.Upgrade,"AllInformation",TalentCategory.Utility,1,out state);
TalentCatalog.Apply(state,TalentOperation.ToggleChild,"AllInformation",TalentCategory.Utility,3,out copy);
Check(!FunctionalTalentRegistry.ChildEnabled(copy,"AllInformation","Compass") && FunctionalTalentRegistry.ChildEnabled(copy,"AllInformation","Time") && copy.TotalSpentTalentPoints==2,"free child toggle preserves other effects and paid cost");
Check(!FunctionalTalentRegistry.ChildEnabled(StateCodec.Decode(StateCodec.Encode(copy)),"AllInformation","Compass"),"child state persisted");
Check(TalentCatalog.Apply(copy,TalentOperation.ToggleChild,"AllInformation",TalentCategory.Utility,13,out _)==TalentResult.InvalidRequest,"invalid child index rejected");
copy.Talents["AllInformation"].DisabledEffects.Add("Unknown"); Check(!TalentCatalog.ValidateImported(copy),"unknown imported child rejected");
state=new(); state.Award(1000000*scale,50000); state.Invest("MultiJump",1,10);
TalentCatalog.Apply(state,TalentOperation.DecreaseIntensity,"MultiJump",TalentCategory.Utility,7,out copy);
Check(NumericTalents.ActiveLevel(copy,"MultiJump")==3 && copy.TotalSpentTalentPoints==10,"jump active intensity independent of purchased count");
state.Talents["NoFallDamage"]=new TalentState(); state.Talents["NoFallDamage"].AddCost(2,2);
Check(!TalentCatalog.ValidateImported(state),"import cannot exceed binary level cap");
Console.WriteLine($"Final P2 core checks passed: {checks}");
