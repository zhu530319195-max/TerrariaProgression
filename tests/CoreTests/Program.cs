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
Console.WriteLine($"PASS: {checks} core checks (formulas, capped/uncapped multi-level, precision, save validation, refunds, multiplayer allocation).");
