// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY UNSUCCESSFUL READ AND START INVALIDATES THE FILE POSITION INDICATOR THROUGH ONE TERMINAL,
/// <c>FileConnector.Unsuccessful</c>, AND NO READ OR START ENTRY ASSIGNS <c>Status</c> ITSELF (kb/Work PB359).
/// </summary>
/// <remarks>
/// <para>
/// ISO §14.9.41.4 GR7 — "Following the unsuccessful execution of a START statement, the file position indicator is
/// set to indicate that no valid record position has been established" — and §14.9.30.4 GR18, its READ twin ("Unless
/// otherwise specified, at the completion of any unsuccessful execution of a READ statement, … the file position
/// indicator is set to indicate that no valid record position has been established"), are unconditional on the KIND
/// of failure. On the connector base that state is the <c>LastReadUnsuccessful</c> flag §9.1.13.7 item 6 reads back
/// as '46'; a keyed connector's indicator is a key value PLUS a validity bit (<c>_fpiValid</c>), which only its
/// override of the <c>InvalidateFilePosition</c> virtual clears.
/// </para>
/// <para>
/// The defect had three shapes: the '47' START returned <c>return Status = …</c> with nothing, in five hand-written
/// copies; the '47' READ, the random READ's '23' and the absent-OPTIONAL arms set the base flag by hand and skipped
/// the keyed validity bit; and a sequential exit wrote <c>Status = …; return false;</c> with no invalidation at all.
/// None was observable from a COBOL program, and only because of an argument about OPEN's current shape. So the
/// rule holds by construction: a READ or START entry (a method of a connector whose name begins with <c>Read</c> or
/// <c>Start</c>) never assigns <c>Status</c> — it returns through <c>Unsuccessful</c>, <c>ReadSucceeded</c> or
/// <c>StartPositioned</c>, and only <c>Unsuccessful</c> can report a failure. The guards the entries ask are pure,
/// so the pre-read record identity can ask them too; the flag's <c>true</c> and the keyed bit's <c>false</c> have
/// one writer each, the virtual and its overrides.
/// </para>
/// <para>
/// ⛔ <see cref="ThePredicates_Discriminate"/> drives each predicate with a source built to trip it and one built
/// to pass it, so a gate that stopped looking turns red rather than green.
/// </para>
/// </remarks>
public sealed class FilePositionInvalidationDriftTests
{
    /// <summary>The three terminals that assign a READ's or START's status. Only <c>Unsuccessful</c> reports a
    /// failure, and it invalidates first; the other two are the success epilogues.</summary>
    private static readonly HashSet<string> Terminals = new(StringComparer.Ordinal)
    {
        "ReadSucceeded", "StartPositioned", "Unsuccessful",
    };

    /// <summary>The guards a READ or START entry asks. They answer a status and change nothing.</summary>
    private static readonly string[] Guards =
    [
        "ReadOpenModeGuard", "SequentialReadGuard", "RandomReadAbsentOptionalGuard", "StartOpenModeGuard",
    ];

    /// <summary>The READ and START entries each organization is known to have. The sweep measures something only
    /// while it sees them (feedback_measure_the_selectors_complement).</summary>
    private static readonly (string File, string[] Methods)[] KnownEntries =
    [
        ("SequentialConnector.cs", ["Read", "ReadRecord", "ReadFailed", "StartFirstLast", "StartFirstLastRecord", "StartFail"]),
        ("RelativeConnector.cs", ["ReadSequential", "ReadRandom", "Start", "StartFirstLast", "StartAt", "StartFail"]),
        ("IndexedConnector.cs", ["ReadSequential", "ReadRandom", "Start", "StartFirstLast", "StartSucceeded", "StartFail"]),
    ];

    private static IEnumerable<(string File, string Source)> IoSources() =>
        Directory.GetFiles(TestRepo.Src("Cobol.Net.Runtime", "IO"), "*.cs", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetFileName(f), File.ReadAllText(f)));

    private static IEnumerable<(string File, string Source)> ConnectorSources() =>
        IoSources().Where(s => s.File.EndsWith("Connector.cs", StringComparison.Ordinal));

    // ── the pure predicates ──────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<MethodDeclarationSyntax> Methods(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();

    private static bool IsReadOrStartEntry(MethodDeclarationSyntax m) =>
        (m.Identifier.Text.StartsWith("Read", StringComparison.Ordinal)
            || m.Identifier.Text.StartsWith("Start", StringComparison.Ordinal))
        && !Terminals.Contains(m.Identifier.Text);

    private static bool AssignsStatus(AssignmentExpressionSyntax a) => a.Left switch
    {
        IdentifierNameSyntax { Identifier.Text: "Status" } => true,
        MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name.Identifier.Text: "Status" } => true,
        _ => false,
    };

    /// <summary>The READ and START entries named in <paramref name="source"/> (every method of a connector whose
    /// name begins with <c>Read</c> or <c>Start</c>, other than the terminals).</summary>
    internal static IEnumerable<string> ReadOrStartEntries(string source) =>
        Methods(source).Where(IsReadOrStartEntry).Select(m => m.Identifier.Text);

    /// <summary>The places in <paramref name="source"/> where a READ or START entry assigns <c>Status</c> itself —
    /// <c>return Status = …;</c> or <c>Status = …; return false;</c> — instead of returning through a terminal.
    /// That is the shape that can report a failure without §14.9.30.4 GR18 / §14.9.41.4 GR7's invalidation.</summary>
    internal static IEnumerable<string> EntryAssignsStatus(string source) =>
        from m in Methods(source).Where(IsReadOrStartEntry)
        from a in m.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(AssignsStatus)
        select $"{m.Identifier.Text}: {a}";

    /// <summary>The guards in <paramref name="source"/> that change state: any assignment, or a call to
    /// <c>InvalidateFilePosition</c>. A guard answers a status and nothing else, which is what lets the pre-read
    /// record identity (<c>SequentialReadReachesRetrieval</c>) ask the same guard a READ asks.</summary>
    internal static IEnumerable<string> ImpureGuard(string source) =>
        from m in Methods(source).Where(m => Guards.Contains(m.Identifier.Text))
        from n in m.DescendantNodes()
        where n is AssignmentExpressionSyntax
            || n is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "InvalidateFilePosition" } }
        select $"{m.Identifier.Text}: {n}";

    /// <summary>True when <paramref name="source"/> declares the failure terminal <c>Unsuccessful</c> and its body
    /// invalidates the file position indicator before it assigns the status.</summary>
    internal static bool TheFailureTerminalInvalidates(string source)
    {
        var terminal = Methods(source).SingleOrDefault(m => m.Identifier.Text == "Unsuccessful");
        if (terminal?.Body is null) return false;
        var statements = terminal.Body.Statements;
        int invalidate = statements.IndexOf(s => s is ExpressionStatementSyntax
        {
            Expression: InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "InvalidateFilePosition" } },
        });
        int assign = statements.IndexOf(s => s.DescendantNodes().OfType<AssignmentExpressionSyntax>().Any(AssignsStatus));
        return invalidate >= 0 && assign > invalidate;
    }

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

    private static string[] Lines(string source) => source.Split('\n');

    // ── the gates ────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>No READ or START entry assigns <c>Status</c>: every exit returns through a terminal.</summary>
    [Fact]
    public void NoReadOrStartEntry_AssignsStatusItself()
    {
        var offenders = ConnectorSources()
            .SelectMany(s => EntryAssignsStatus(s.Source).Select(o => $"{s.File}: {o}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "A READ/START entry assigns Status itself, so a failing exit can skip the file position indicator's "
            + "invalidation (§14.9.30.4 GR18 / §14.9.41.4 GR7) — return Unsuccessful(status) for a failure, "
            + "ReadSucceeded/StartPositioned for a success:\n" + string.Join("\n", offenders));

        foreach (var (file, methods) in KnownEntries)
        {
            var seen = ReadOrStartEntries(ConnectorSources().Single(s => s.File == file).Source).ToHashSet();
            foreach (string method in methods)
                Assert.True(seen.Contains(method), $"{file}: the sweep no longer sees the READ/START entry {method}.");
        }
    }

    /// <summary>The failure terminal invalidates, and the guards stay pure.</summary>
    [Fact]
    public void TheFailureTerminal_Invalidates_AndTheGuardsArePure()
    {
        string fileConnector = IoSources().Single(s => s.File == "FileConnector.cs").Source;
        Assert.True(TheFailureTerminalInvalidates(fileConnector),
            "FileConnector.Unsuccessful must call InvalidateFilePosition() before it assigns Status "
            + "(§14.9.30.4 GR18 / §14.9.41.4 GR7).");

        var offenders = ConnectorSources()
            .SelectMany(s => ImpureGuard(s.Source).Select(o => $"{s.File}: {o}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "A READ/START guard changes state; it shall only answer a status (the entry returns it through "
            + "Unsuccessful):\n" + string.Join("\n", offenders));
        var declared = Methods(fileConnector).Select(m => m.Identifier.Text).ToHashSet();
        foreach (string guard in Guards) Assert.True(declared.Contains(guard), $"FileConnector no longer declares {guard}.");
    }

    /// <summary>The base flag's <c>true</c> has one writer: <c>FileConnector.InvalidateFilePosition</c>.</summary>
    [Fact]
    public void TheUnsuccessfulFlag_IsSetOnlyByTheVirtual()
    {
        var offenders = IoSources()
            .SelectMany(s => HandWrittenUnsuccessfulFlag(s.Source).Select(l => $"{s.File}: {l.Trim()}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "An unsuccessful READ/START sets the '46' flag by hand, skipping a keyed connector's validity bit "
            + "(§14.9.41.4 GR7 / §14.9.30.4 GR18) — return Unsuccessful(status):\n" + string.Join("\n", offenders));
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
            + "'46' reads is not set with it — return Unsuccessful(status):\n" + string.Join("\n", offenders));
        // The sweep measures something only while both keyed overrides exist (feedback_measure_the_selectors_complement).
        foreach (string keyed in new[] { "RelativeConnector.cs", "IndexedConnector.cs" })
            Assert.Contains(IoSources(), s => s.File == keyed
                && s.Source.Contains("protected override void InvalidateFilePosition()", StringComparison.Ordinal)
                && s.Source.Contains("_fpiValid = false", StringComparison.Ordinal));
    }

    /// <summary>⛔ The failure proof: each predicate trips on the defect's shape and passes the fixed one.</summary>
    [Fact]
    public void ThePredicates_Discriminate()
    {
        // The three shapes kb/Work PB359 named, each inside a READ/START entry, and the review's (w1084e R1-1)
        // bare 'Status = X; return false;' exit.
        const string bareReturn = "class C { string Start() { if (x) return Status = FileStatusCode.ReadNotOpenForInput; return StartPositioned(); } }";
        const string bareBool = "class C { bool ReadRecord() { if (_reader is null) { Status = FileStatusCode.ReadNotOpenForInput; return false; } return true; } }";
        const string thisStatus = "class C { string ReadRandom() { return this.Status = FileStatusCode.RecordNotFound; } }";
        const string fixedExits = "class C { string Start() { if (x) return Unsuccessful(notOpen); return StartPositioned(); } "
            + "bool ReadRecord() { if (_reader is null) return ReadUnsuccessful(FileStatusCode.ReadNotOpenForInput); return true; } "
            + "string Write() { return Status = FileStatusCode.Success; } }";
        Assert.Single(EntryAssignsStatus(bareReturn));
        Assert.Single(EntryAssignsStatus(bareBool));
        Assert.Single(EntryAssignsStatus(thisStatus));
        Assert.Empty(EntryAssignsStatus(fixedExits));   // a WRITE is not a READ/START entry; the terminals are excluded
        Assert.Equal(["Start", "ReadRecord"], ReadOrStartEntries(fixedExits));

        Assert.Single(ImpureGuard(
            "class C { string? StartOpenModeGuard() { if (x) { InvalidateFilePosition(); return s; } return null; } }"));
        Assert.Single(ImpureGuard(
            "class C { string? SequentialReadGuard() { if (OptionalAbsent) { LastReadUnsuccessful = true; return s; } return null; } }"));
        Assert.Empty(ImpureGuard("class C { string? StartOpenModeGuard() => IsOpen ? null : s; }"));

        Assert.True(TheFailureTerminalInvalidates(
            "class C { string Unsuccessful(string status) { InvalidateFilePosition(); return Status = status; } }"));
        Assert.False(TheFailureTerminalInvalidates(
            "class C { string Unsuccessful(string status) { return Status = status; } }"));
        Assert.False(TheFailureTerminalInvalidates(
            "class C { string Unsuccessful(string status) { Status = status; InvalidateFilePosition(); return status; } }"));

        Assert.Single(HandWrittenUnsuccessfulFlag(
            "        {\n            LastReadUnsuccessful = true;\n            return Status = FileStatusCode.RecordNotFound;\n"));
        Assert.Empty(HandWrittenUnsuccessfulFlag(
            "    protected virtual void InvalidateFilePosition() => LastReadUnsuccessful = true;\n"));

        Assert.Single(HandWrittenValidityClear(
            "        {\n            LastReadUnsuccessful = true;\n            _fpiValid = false;\n            return Status = x;\n"));
        Assert.Empty(HandWrittenValidityClear(
            "    protected override void InvalidateFilePosition()\n    {\n        _fpiValid = false;\n        base.InvalidateFilePosition();\n"));
    }
}
