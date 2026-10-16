// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.CodeGen;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB1079 — an external file connector's entry identity (<c>OoEmitter.SelectFingerprint</c>, the string two
/// describers compare for EC-EXTERNAL-FILE-MISMATCH) is the WHOLE of ISO §12.4.5.3 GR1: §14.8.4.4 says "the rules
/// specified in 12.4.5, File control entry General rule 1 apply", and GR1 lists a) through m). The fingerprint used to
/// omit c) RECORD DELIMITER, d) RESERVE, g) COLLATING SEQUENCE and k)'s SUPPRESS WHEN phrase, so an entry differing
/// only in one of them activated with no condition. Each row below changes ONE clause-level GR1 item on an otherwise
/// identical entry and must change the identity; the key items' descriptions and relative locations (j/k), which need
/// a bound record, are held by the golden <c>2023/w68r_pb1079_external_entry_identity</c>.
/// </summary>
public sealed class ExternalFileEntryIdentityTests
{
    private static FileModel Entry()
    {
        var f = new FileModel
        {
            CobolName = "XF", AssignTarget = "x.dat", Organization = FileOrganization.Indexed,
            OrganizationWritten = true, AccessMode = FileAccessMode.Dynamic, RecordKeyName = "XK",
        };
        f.AlternateKeyNames.Add(new AlternateKeyClause { Name = "XA", Qualifiers = [], Duplicates = true });
        return f;
    }

    /// <summary>A key-level clause naming one key by its bare data-name, unresolved (no record items in this model),
    /// so the fingerprint keys the operand by its spelling.</summary>
    private static KeyLevelCollatingClause KeyClause(string key, string alphabet) =>
        new([new CollatingKeyOperand { Name = key, Qualifiers = [] }], alphabet, default);

    public static TheoryData<string, Action<FileModel>> Items => new()
    {
        { "a) OPTIONAL", f => f.Optional = true },
        { "b) ASSIGN literal-1", f => f.AssignTarget = "y.dat" },
        { "b) ASSIGN USING data-name-1", f => f.AssignUsingName = "FNAME" },
        { "c) RECORD DELIMITER STANDARD-1", f => f.RecordDelimiter = "STANDARD-1" },
        { "c) RECORD DELIMITER feature-name-1", f => f.RecordDelimiter = "VARDELIM" },
        { "d) RESERVE integer-1", f => f.ReserveAreas = 3 },
        { "e) organization", f => f.Organization = FileOrganization.Relative },
        { "f) access mode", f => f.AccessMode = FileAccessMode.Random },
        { "g) file-level COLLATING SEQUENCE", f => f.FileLevelCollating = ("REV", null) },
        { "g) key-level COLLATING SEQUENCE", f => f.KeyLevelCollating.Add(KeyClause("XA", "REV")) },
        { "k) DUPLICATES phrase", f => f.AlternateKeyNames[0] = new AlternateKeyClause { Name = "XA", Qualifiers = [], Duplicates = false } },
        { "k) number of alternate record keys", f => f.AlternateKeyNames.Clear() },
        { "k) SUPPRESS WHEN phrase", f => f.AlternateKeyNames[0].SuppressWhen =
            new SuppressWhenOperand("\"XX\"", SuppressWhenForm.Literal, CobolNet.Common.LiteralClass.Alphanumeric, "XX", null) },
        // j)/k) "each data-name-6" / "each data-name-3": the SOURCE phrase's operands, in order, are the key (kb/Work
        // PB1099; review R1-3 found no witness that the identity carries them).
        { "j) RECORD KEY SOURCE phrase", f => f.RecordKeySource = Source("XK", "XB", "XC") },
        { "k) ALTERNATE RECORD KEY SOURCE phrase", f => f.AlternateKeyNames[0] =
            new AlternateKeyClause { Name = "XA", Qualifiers = [], Duplicates = true, Source = Source("XA", "XB") } },
        { "l) sharing mode", f => f.Sharing = SharingMode.NoOther },
        { "m) lock mode", f => f.LockMode = new LockModeInfo(LockKind.Manual, false) },
    };

    [Theory]
    [MemberData(nameof(Items))]
    public void Each_GR1_item_is_part_of_the_identity(string item, Action<FileModel> change)
    {
        var changed = Entry();
        change(changed);
        Assert.True(OoEmitter.SelectFingerprint(Entry()) != OoEmitter.SelectFingerprint(changed),
            $"§12.4.5.3 GR1 {item}: an entry differing only in it has the same identity '{OoEmitter.SelectFingerprint(changed)}'");
    }

    [Fact]
    public void Identical_entries_have_one_identity()
        => Assert.Equal(OoEmitter.SelectFingerprint(Entry()), OoEmitter.SelectFingerprint(Entry()));

    [Fact]
    public void Key_level_collating_is_compared_by_key_position_not_clause_order()
    {
        // GR1 g) "The same specification of COLLATING SEQUENCE clauses": the same key-to-alphabet assignments written
        // as two clauses in either order specify the same thing.
        var a = Entry(); a.KeyLevelCollating.Add(KeyClause("XK", "ALPHA1")); a.KeyLevelCollating.Add(KeyClause("XA", "ALPHA2"));
        var b = Entry(); b.KeyLevelCollating.Add(KeyClause("XA", "alpha2")); b.KeyLevelCollating.Add(KeyClause("XK", "alpha1"));
        Assert.Equal(OoEmitter.SelectFingerprint(a), OoEmitter.SelectFingerprint(b));
    }

    /// <summary>A SOURCE phrase as written, its operands unresolved (no record items in this model), so the
    /// fingerprint keys each operand by its spelling.</summary>
    private static RecordKeySource Source(string recordKeyName, params string[] operands) => new()
    {
        Name = recordKeyName,
        Operands = [.. operands.Select(o => new KeySourceOperand { Name = o, Qualifiers = [] })],
    };

    [Fact]
    public void Source_operand_order_is_part_of_the_identity()
    {
        // GR1 j) "each data-name-6 as well as their relative location": the key IS the concatenation in the order
        // specified (§12.4.5.12.4 GR2), so XB XC and XC XB are different keys.
        var a = Entry(); a.RecordKeySource = Source("XK", "XB", "XC");
        var b = Entry(); b.RecordKeySource = Source("XK", "XC", "XB");
        Assert.NotEqual(OoEmitter.SelectFingerprint(a), OoEmitter.SelectFingerprint(b));
    }
}
