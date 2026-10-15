// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY UNSUCCESSFUL READ AND START INVALIDATES THE FILE POSITION INDICATOR THROUGH ONE VIRTUAL,
/// <c>FileConnector.InvalidateFilePosition</c> (kb/Work PB359).
/// </summary>
/// <remarks>
/// <para>
/// ISO §14.9.41.4 GR7 — "Following the unsuccessful execution of a START statement, the file position indicator is
/// set to indicate that no valid record position has been established" — and §14.9.30.4 GR18, its READ twin ("Unless
/// otherwise specified, at the completion of any unsuccessful execution of a READ statement, … the file position
/// indicator is set to indicate that no valid record position has been established"), are unconditional on the KIND
/// of failure. On the connector base that state is the <c>LastReadUnsuccessful</c> flag §9.1.13.7 item 6 reads back
/// as '46'; a keyed connector's indicator is a key value PLUS a validity bit (<c>_fpiValid</c>), which only its
/// override of the virtual clears.
/// </para>
/// <para>
/// The '47' START once returned with a plain <c>return Status = …</c> in five hand-written copies, and the '47'
/// READ, the random READ's '23' and the absent-OPTIONAL arms set the base flag by hand and skipped the keyed
/// validity bit. None of them was observable from a COBOL program, and only because of an argument about OPEN's
/// current shape (every route into those exits had already invalidated the indicator or forced a reopening
/// OPEN). This gate makes the rule hold by construction instead: the flag's <c>true</c> and the keyed bit's
/// <c>false</c> have exactly one writer each — the virtual and its overrides — and no READ entry answers the pure
/// open-mode question as its own guard.
/// </para>
/// <para>
/// ⛔ <see cref="ThePredicates_Discriminate"/> drives each predicate with a source built to trip it and one built
/// to pass it, so a gate that stopped looking turns red rather than green.
/// </para>
/// </remarks>
public sealed class FilePositionInvalidationDriftTests
{
    private static IEnumerable<(string File, string Source)> IoSources() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Runtime", "IO"), "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

    // ── the pure predicates ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The lines of <paramref name="source"/> that set the base '46' flag by hand, outside the virtual.</summary>
    internal static IEnumerable<string> HandWrittenUnsuccessfulFlag(string source) =>
        Lines(source).Where(l => l.Contains("LastReadUnsuccessful = true", StringComparison.Ordinal)
            && !l.Contains("void InvalidateFilePosition()", StringComparison.Ordinal));

    /// <summary>The lines of <paramref name="source"/> that clear a keyed validity bit outside an override of the
    /// virtual: every <c>_fpiValid = false;</c> must sit in the body that an <c>InvalidateFilePosition()</c>
    /// override opens, i.e. within the three lines after its signature.</summary>
    internal static IEnumerable<string> HandWrittenValidityClear(string source)
    {
        string[] lines = Lines(source);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("_fpiValid = false", StringComparison.Ordinal)) continue;
            bool inOverride = Enumerable.Range(Math.Max(0, i - 3), Math.Min(3, i))
                .Any(j => lines[j].Contains("override void InvalidateFilePosition()", StringComparison.Ordinal));
            if (!inOverride) yield return lines[i].Trim();
        }
    }

    /// <summary>The lines of <paramref name="source"/> where a READ entry returns the PURE open-mode answer as its
    /// own status — the shape that skipped GR18 on the '47' READ. A READ entry asks <c>SequentialReadGuard</c> or
    /// <c>RandomReadOpenModeGuard</c>, which invalidate on the way out.</summary>
    internal static IEnumerable<string> PureGuardReturnedAsStatus(string source) =>
        Lines(source).Where(l => l.Contains("ReadOpenModeGuard() is { }", StringComparison.Ordinal)
            && !l.Contains("RandomReadOpenModeGuard()", StringComparison.Ordinal)
            && l.Contains("return Status =", StringComparison.Ordinal));

    private static string[] Lines(string source) => source.Split('\n');

    // ── the gates ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The base flag's <c>true</c> has one writer: <c>FileConnector.InvalidateFilePosition</c>.</summary>
    [Fact]
    public void TheUnsuccessfulFlag_IsSetOnlyByTheVirtual()
    {
        var offenders = IoSources()
            .SelectMany(s => HandWrittenUnsuccessfulFlag(s.Source).Select(l => $"{s.File}: {l.Trim()}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "An unsuccessful READ/START sets the '46' flag by hand, skipping a keyed connector's validity bit "
            + "(§14.9.41.4 GR7 / §14.9.30.4 GR18) — call InvalidateFilePosition():\n" + string.Join("\n", offenders));
        Assert.Contains(IoSources(), s => s.File == "FileConnector.cs"
            && s.Source.Contains("void InvalidateFilePosition() => LastReadUnsuccessful = true;", StringComparison.Ordinal));
    }

    /// <summary>A keyed validity bit is cleared only inside an override of the virtual.</summary>
    [Fact]
    public void TheKeyedValidityBit_IsClearedOnlyByTheOverride()
    {
        var offenders = IoSources()
            .SelectMany(s => HandWrittenValidityClear(s.Source).Select(l => $"{s.File}: {l}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "A keyed connector clears _fpiValid outside its InvalidateFilePosition override, so the base flag the "
            + "'46' reads is not set with it — call InvalidateFilePosition():\n" + string.Join("\n", offenders));
        // The sweep measures something only while both keyed overrides exist (feedback_measure_the_selectors_complement).
        foreach (string keyed in new[] { "RelativeConnector.cs", "IndexedConnector.cs" })
            Assert.Contains(IoSources(), s => s.File == keyed
                && s.Source.Contains("protected override void InvalidateFilePosition()", StringComparison.Ordinal)
                && s.Source.Contains("_fpiValid = false", StringComparison.Ordinal));
    }

    /// <summary>No READ entry returns the pure open-mode answer as its own status.</summary>
    [Fact]
    public void NoReadEntry_ReturnsThePureOpenModeAnswer()
    {
        var offenders = IoSources()
            .SelectMany(s => PureGuardReturnedAsStatus(s.Source).Select(l => $"{s.File}: {l.Trim()}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "A READ entry returns ReadOpenModeGuard()'s '47' without GR18's invalidation — ask "
            + "RandomReadOpenModeGuard() (or SequentialReadGuard()):\n" + string.Join("\n", offenders));
    }

    /// <summary>⛔ The failure proof: each predicate trips on the defect's shape and passes the fixed one.</summary>
    [Fact]
    public void ThePredicates_Discriminate()
    {
        Assert.Single(HandWrittenUnsuccessfulFlag(
            "        {\n            LastReadUnsuccessful = true;\n            return Status = FileStatusCode.RecordNotFound;\n"));
        Assert.Empty(HandWrittenUnsuccessfulFlag(
            "    protected virtual void InvalidateFilePosition() => LastReadUnsuccessful = true;\n"));

        Assert.Single(HandWrittenValidityClear(
            "        {\n            LastReadUnsuccessful = true;\n            _fpiValid = false;\n            return Status = x;\n"));
        Assert.Empty(HandWrittenValidityClear(
            "    protected override void InvalidateFilePosition()\n    {\n        _fpiValid = false;\n        base.InvalidateFilePosition();\n"));

        Assert.Single(PureGuardReturnedAsStatus(
            "        if (ReadOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47'\n"));
        Assert.Empty(PureGuardReturnedAsStatus(
            "        if (RandomReadOpenModeGuard() is { } notOpen) return Status = notOpen;   // '47' + GR18\n"));
        Assert.Empty(PureGuardReturnedAsStatus(
            "        if (ReadOpenModeGuard() is { } notOpen) return notOpen;                     // '47' GR2\n"));
    }
}
