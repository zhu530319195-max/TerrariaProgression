using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;

namespace TerrariaProgression.Core;

// One versioned representation for character tags and network snapshots.
public static class StateCodec
{
    public const int MaxPacketBytes = 64_000;
    public static void WriteInteger(BinaryWriter writer, BigInteger value)
    {
        if (value < 0) throw new InvalidDataException("Negative progression value.");
        byte[] bytes = value.ToByteArray(isUnsigned: true);
        if (bytes.Length > 1024) throw new InvalidDataException("Progression number exceeds transport capacity.");
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }
    public static BigInteger ReadInteger(BinaryReader reader)
    {
        int count = reader.ReadUInt16();
        if (count is < 1 or > 1024) throw new InvalidDataException("Invalid progression number length.");
        byte[] bytes = reader.ReadBytes(count);
        if (bytes.Length != count) throw new EndOfStreamException();
        return new BigInteger(bytes, isUnsigned: true);
    }
    public static byte[] Encode(ProgressionState state)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(ProgressionState.DataVersion);
        WriteInteger(writer, state.Level);
        WriteInteger(writer, state.CurrentExperience);
        WriteInteger(writer, state.TotalExperienceEarned);
        WriteInteger(writer, state.TotalTalentPointsEarned);
        writer.Write(state.ActiveLoadoutId.ToByteArray());
        writer.Write(state.Loadouts.Count);
        foreach (var page in state.Loadouts) {
            writer.Write(page.Id.ToByteArray());
            writer.Write(page.Name);
            WriteInteger(writer, page.AvailablePoints);
            WriteInteger(writer, page.SpentPoints);
            writer.Write(page.Talents.Count);
            foreach (var (id, talent) in page.Talents) {
                writer.Write(id);
                writer.Write(talent.Enabled);
                writer.Write(talent.CurrentIntensity.HasValue);
                if (talent.CurrentIntensity.HasValue) writer.Write(talent.CurrentIntensity.Value);
                writer.Write(talent.DisabledEffects.Count);
                foreach (string child in talent.DisabledEffects) writer.Write(child);
                writer.Write(talent.CostRuns.Count);
                foreach (var run in talent.CostRuns) {
                    WriteInteger(writer, run.Cost);
                    WriteInteger(writer, run.Count);
                }
            }
            if (stream.Length > MaxPacketBytes) throw new InvalidDataException("Progression snapshot exceeds transport capacity.");
        }
        if (stream.Length > MaxPacketBytes) throw new InvalidDataException("Progression snapshot exceeds transport capacity.");
        return stream.ToArray();
    }
    public static ProgressionState Decode(byte[] bytes)
    {
        if (bytes.Length > MaxPacketBytes) throw new InvalidDataException("Oversized progression snapshot.");
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);
        int version = reader.ReadInt32();
        if (version is not (1 or 2 or 3 or ProgressionState.DataVersion)) throw new InvalidDataException($"Unsupported progression DataVersion {version}; use a compatible mod version. Data was not reset.");
        var result = new ProgressionState {
            Level = ReadInteger(reader), CurrentExperience = ReadInteger(reader),
            TotalExperienceEarned = ReadInteger(reader)
        };
        if (version < 4) {
            result.AvailableTalentPoints = ReadInteger(reader);
            result.TotalSpentTalentPoints = ReadInteger(reader);
            result.TotalTalentPointsEarned = version >= 3 ? ReadInteger(reader) : result.Level - 1;
            ReadTalents(reader, version, result.ActiveLoadout, result.TotalTalentPointsEarned);
        }
        else {
            result.TotalTalentPointsEarned = ReadInteger(reader);
            result.ActiveLoadoutId = ReadGuid(reader);
            int pages = reader.ReadInt32();
            if (pages is < 1 or > MaxPacketBytes / 28) throw new InvalidDataException("Invalid loadout count.");
            result.Pages.Clear();
            var ids = new HashSet<Guid>();
            for (int i = 0; i < pages; i++) {
                var page = new TalentLoadout { Id = ReadGuid(reader), Name = reader.ReadString(),
                    AvailablePoints = ReadInteger(reader), SpentPoints = ReadInteger(reader) };
                if (page.Id == Guid.Empty || !ids.Add(page.Id) || !TalentLoadouts.ValidName(page.Name)) throw new InvalidDataException("Invalid loadout identity.");
                ReadTalents(reader, version, page, result.TotalTalentPointsEarned);
                result.Pages.Add(page);
            }
            if (!ids.Contains(result.ActiveLoadoutId)) throw new InvalidDataException("Missing active loadout.");
        }
        // Today's XP requirement and reward settings cannot reconstruct history.
        if (stream.Position != stream.Length || result.Level < 1
            || result.TotalExperienceEarned < result.CurrentExperience
            || result.TotalExperienceEarned - result.CurrentExperience < (result.Level - 1) * Experience.Scale)
            throw new InvalidDataException("Inconsistent progression snapshot.");
        return result;
    }
    private static Guid ReadGuid(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(16);
        if (bytes.Length != 16) throw new EndOfStreamException();
        return new Guid(bytes);
    }
    private static void ReadTalents(BinaryReader reader, int version, TalentLoadout page, BigInteger earned)
    {
        int count = reader.ReadInt32();
        if (count is < 0 or > 512) throw new InvalidDataException("Invalid talent count.");
        BigInteger spent = 0;
        for (int i = 0; i < count; i++) {
            string id = reader.ReadString();
            if (id.Length is 0 or > 128 || page.Talents.ContainsKey(id)) throw new InvalidDataException("Invalid talent ID.");
            var talent = new TalentState { Enabled = reader.ReadBoolean() };
            if (reader.ReadBoolean()) {
                talent.CurrentIntensity = reader.ReadDecimal();
                if (talent.CurrentIntensity < 0) throw new InvalidDataException("Invalid intensity.");
            }
            if (version >= 3) {
                int children = reader.ReadInt32();
                if (children is < 0 or > 64) throw new InvalidDataException("Invalid sub-effect count.");
                for (int k = 0; k < children; k++) {
                    string child = reader.ReadString();
                    if (child.Length is 0 or > 128 || !talent.DisabledEffects.Add(child)) throw new InvalidDataException("Invalid sub-effect.");
                }
            }
            int costs = reader.ReadInt32();
            if (costs is < 1 or > 10000) throw new InvalidDataException("Invalid paid-cost history.");
            for (int j = 0; j < costs; j++) {
                var cost = ReadInteger(reader);
                var levels = version == 1 ? BigInteger.One : ReadInteger(reader);
                if (cost <= 0 || levels <= 0) throw new InvalidDataException("Invalid talent cost.");
                talent.AddCost(cost, levels);
                spent += cost * levels;
            }
            page.Talents.Add(id, talent);
        }
        if (spent != page.SpentPoints || page.AvailablePoints + spent != earned)
            throw new InvalidDataException("Inconsistent loadout ledger.");
        // Retired in 0.4.1: validate each original ledger, then refund it once.
        if (page.Talents.Remove("AutoJump", out var retired)) {
            page.AvailablePoints += retired.InvestedPoints;
            page.SpentPoints -= retired.InvestedPoints;
        }
    }
}
