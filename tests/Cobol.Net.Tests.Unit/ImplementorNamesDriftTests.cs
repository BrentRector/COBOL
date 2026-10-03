// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Editions.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Editions;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The implementor system-name table (<see cref="ImplementorNames"/> — ISO §12.3.7.3 SR8, Annex A.1 items
/// 189/190/191; kb/Work PB862) held against the three things that could drift from it, and the SPECIAL-NAMES entry
/// shape (kb/Work PB716) held from both sides.
/// <para>The table is the ONE place the names live; the documentation (docs/CONFORMANCE.md §7) and the COBOLNET2241
/// catalog text restate them for a reader and so CAN drift — these tests make a new row that is not documented, or a
/// documented name that is not a row, RED. The compile probes pin the COMPLEMENT as well as the refusal: every row is
/// accepted in the arm its kind allows (a closed table that rejected one of its own names would be green on every
/// negative golden), and the legal spellings the old grammar refused (OFF before ON; FOR before IS) now compile.</para>
/// </summary>
public sealed class ImplementorNamesDriftTests
{
    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string special, string procedure, int edition)
    {
        string source = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. INAMES.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   SPECIAL-NAMES.
            {special}
                   PROCEDURE DIVISION.
                   MAIN-PARA.
            {procedure}
                       STOP RUN.
            """;
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Inm_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Success ? [] : [.. r.Errors]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>The elements a reader must find for each kind: every non-numbered name, and both ENDS of each
    /// numbered run (<c>SWITCH-0</c> … <c>SWITCH-36</c>) — the same compaction <see cref="ImplementorNames.Describe"/>
    /// prints, derived here from the table rather than copied from it.</summary>
    private static IEnumerable<string> Elements(SystemNameKind kind) =>
        ImplementorNames.All.Values.Where(r => r.Kind == kind)
            .GroupBy(r => Regex.Replace(r.Name, "[0-9]+$", ""))
            .SelectMany(g => g.Count() == 1
                ? [g.Single().Name]
                : new[] { g.MinBy(r => int.Parse(r.Name[g.Key.Length..]))!.Name,
                          g.MaxBy(r => int.Parse(r.Name[g.Key.Length..]))!.Name });

    private static string Row(string key)
    {
        string doc = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        var m = Regex.Match(doc, $@"^\| {Regex.Escape(key)} \|.*$", RegexOptions.Multiline);
        Assert.True(m.Success, $"docs/CONFORMANCE.md §7 has no {key} row");
        return m.Value;
    }

    /// <summary>⛔ THE DRIFT GATE: each kind's Annex A.1 row in docs/CONFORMANCE.md §7 names every element of that
    /// kind's table rows, and the COBOLNET2241 catalog text names every element of every kind.</summary>
    [Theory]
    [InlineData(SystemNameKind.Device, "DOC-A.1-189")]
    [InlineData(SystemNameKind.Feature, "DOC-A.1-190")]
    [InlineData(SystemNameKind.Switch, "DOC-A.1-191")]
    public void EveryTableName_IsDocumented_AndNamedByTheDiagnostic(SystemNameKind kind, string key)
    {
        string row = Row(key);
        string catalog = DiagnosticCatalog.UnavailableImplementorName.Title;
        foreach (string e in Elements(kind))
        {
            Assert.True(row.Contains($"`{e}`", StringComparison.Ordinal), $"{key} does not name `{e}`");
            Assert.True(catalog.Contains(e, StringComparison.Ordinal), $"COBOLNET2241's text does not name {e}");
        }
    }

    /// <summary>§8.3.2.3.1 — "a given system-name shall not belong to more than one of the following types of
    /// system-names: device-name, feature-name, and switch-name". True by construction (one key per name); pinned so
    /// a second table can never be added beside this one without failing here first.</summary>
    [Fact]
    public void EveryName_BelongsToOneKind_AndCarriesOnlyItsKindsAttributes()
    {
        foreach (var r in ImplementorNames.All.Values)
        {
            Assert.Equal(r.Kind == SystemNameKind.Device, r.Device != DeviceCapability.None);
            Assert.Equal(r.Kind == SystemNameKind.Feature, r.Advance != FeatureAdvance.None);
        }
        Assert.Equal(ImplementorNames.All.Count,
            ImplementorNames.All.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    /// <summary>The COMPLEMENT: every row is accepted — in the mnemonic form, and for a switch-name in both
    /// status-only orders (§5.2.6.4 choice indicators: any order, each once) — at the first and the last edition.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void EveryTableName_IsAccepted_InTheArmItsKindAllows(int edition)
    {
        int i = 0;
        var lines = new List<string>();
        foreach (var r in ImplementorNames.All.Values.OrderBy(r => r.Name, StringComparer.Ordinal))
        {
            i++;
            lines.Add($"           {r.Name} IS MN-{i}");
            if (r.Kind == SystemNameKind.Switch)
                lines.Add(i % 2 == 0
                    ? $"           {r.Name} OFF STATUS IS C-OFF-{i} ON STATUS IS C-ON-{i}"
                    : $"           {r.Name} ON C-ON-{i}");
        }
        var (ok, errors) = Compile(string.Join("\n", lines) + ".", "           DISPLAY \"OK\".", edition);
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>The REFUSALS, one per arm of the new shape: an unavailable name (SR8), ON STATUS on a device-name,
    /// and a lone word — which is no entry at all (§12.3.7.2: every arm has a required continuation).</summary>
    [Theory]
    [InlineData("           WIBBLE WOBBLE.", "COBOLNET2241")]
    [InlineData("           CONSOEL IS PRT.", "COBOLNET2241")]
    [InlineData("           CONSOLE ON STATUS IS C-ON.", "COBOLNET2241")]
    [InlineData("           CSP OFF C-OFF.", "COBOLNET2241")]
    [InlineData("           CLASS DIGITS IS \"0\" ZOTZOT.", "COBOLNET1970")]
    [InlineData("           SWITCH-1 ON C1 ON C2.", "COBOL")]
    public void EntryShapeAndName_Refused(string special, string code)
    {
        var (ok, errors) = Compile(special, "           DISPLAY \"NO\".", 2023);
        Assert.False(ok, $"compiled: {special}");
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal));
    }

    /// <summary>The mnemonic's USE is keyed on its name's KIND (§12.3.7.3 SR5/SR6/SR7, §14.9.51.3 SR16): a
    /// switch's or a feature's mnemonic in DISPLAY and a device's in SET are refused (the WRITE ADVANCING leg, COBOLNET2243,
    /// is the negative golden pb862-write-advancing-device-mnemonic).</summary>
    [Theory]
    [InlineData("           SWITCH-1 IS SW1.", "           DISPLAY \"X\" UPON SW1.", "COBOLNET0817")]
    [InlineData("           CSP IS NOSP.", "           DISPLAY \"X\" UPON NOSP.", "COBOLNET0817")]
    [InlineData("           CONSOLE IS CON.", "           SET CON TO ON.", "COBOLNET1757")]
    [InlineData("           CSP IS NOSP.", "           SET NOSP TO ON.", "COBOLNET1757")]
    // kb/Work PB454 — an Option-2 switch entry (status phrases, no mnemonic) declares no name SET can take; the
    // switch-NAME is an implementor-name (§8.3.2.3.11), never mnemonic-name-1 (§14.9.39.3 SR5).
    [InlineData("           SWITCH-1 ON STATUS IS SW-ON.", "           SET SWITCH-1 TO ON.", "COBOLNET1757")]
    public void MnemonicUse_IsKeyedOnTheNamesKind(string special, string procedure, string code)
    {
        var (ok, errors) = Compile(special, procedure, 2023);
        Assert.False(ok, $"compiled: {special} / {procedure}");
        Assert.Contains(errors, e => e.Contains(code, StringComparison.Ordinal));
    }

    /// <summary>⛔ ONE WALK FOR MNEMONICS (kb/Work PB454). Every SPECIAL-NAMES mnemonic-name — a switch's for SET as
    /// much as a device's or a feature's — is answered by <c>Procedure.MnemonicRegistry</c>, whose value is the
    /// <see cref="ImplementorName"/> row. <c>DataBinder</c> used to keep a second, switch-only mnemonic map built by
    /// a second walk of the same entries, and that walk is where an Option-2 entry entered the switch-NAME itself as
    /// a mnemonic. This fails the moment a mnemonic-keyed member reappears on the data binder.</summary>
    [Fact]
    public void DataBinder_KeepsNoSecondMnemonicMap()
    {
        var members = typeof(CobolNet.Binding.DataBinder)
            .GetMembers(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
                        | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => m.MemberType is System.Reflection.MemberTypes.Field or System.Reflection.MemberTypes.Property)
            .Where(m => m.Name.Contains("Mnemonic", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Name).ToList();
        Assert.True(members.Count == 0,
            "DataBinder declares a mnemonic-keyed member (" + string.Join(", ", members) + "); mnemonic-names are "
            + "answered by Procedure.MnemonicRegistry alone (kb/Work PB454)");
    }

    /// <summary>The CLASS clause's FOR phrase at its PRINTED position (§12.3.7.2, folio 290 — between class-name-1
    /// and IS), with and without the optional FOR, is accepted; the old postfix position is not (COBOLNET2315, kb/Work
    /// PB977). A NATIONAL class's literals are national literals (§12.3.7.3 SR17 c3 — kb/Work PB976 made the phrase
    /// load-bearing, and this row used to write alphanumeric ones).</summary>
    [Theory]
    [InlineData("           CLASS HEX FOR ALPHANUMERIC IS \"0\" THRU \"9\" \"A\" THRU \"F\".", true)]
    [InlineData("           CLASS HEX NATIONAL IS N\"0\" THRU N\"9\".", true)]
    [InlineData("           CLASS HEX IS \"0\" THRU \"9\" FOR NATIONAL.", false)]
    public void ClassForPhrase_AtItsPrintedPosition(string special, bool legal)
    {
        var (ok, errors) = Compile(special, "           DISPLAY \"OK\".", 2023);
        Assert.True(ok == legal, $"{special} → {(ok ? "compiled" : string.Join("\n", errors))}");
    }
}
