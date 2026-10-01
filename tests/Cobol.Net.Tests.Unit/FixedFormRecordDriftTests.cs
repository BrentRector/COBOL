// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A FILE OF FIXED-LENGTH RECORDS CARRIES A VARIABLE-LENGTH RECORD IN ITS FIXED FORM, ASKED AT THE PLACES A PROGRAM'S
/// RECORD AREA ENTERS A CONNECTOR AND LEAVES IT (kb/Work PB1562, docs/CONFORMANCE.md §3 D-FRA (vi)).
/// </summary>
/// <remarks>
/// <para>An explicit RECORD CONTAINS integer-1 (§13.18.43.4 GR6) keeps the file FIXED, and §9.1.6 makes its record type and
/// size attributes every program shares, so a record with dynamic-length members can neither be framed nor carry an extent
/// table: it is its <c>CobolContiguousLayout.ToFixedForm</c>. The form is asked ONCE on the way out —
/// <c>FileConnector.FixedForm</c> by the registry's governed WRITE and REWRITE and by <c>IndexedConnector.AreaKey</c> — and
/// ONCE on the way in — <c>FileModel.FixedFormRecords</c> by the READ landing. A new organization, a new governed entry or an
/// edit that "just fits the image" re-opens the defect (a second program reads the framing, or the writing program reads
/// its own record mis-split) while every existing test stays green, so this gate pins the structure; the behaviour is
/// <c>2014/pb1562_fixed_clause_file_second_description</c>. <see cref="TheGate_ActuallyFails_WhenAnEntryDoesNotAskForTheForm"/>
/// drives the predicate with sources built to break it AND to pass it.</para></remarks>
public sealed class FixedFormRecordDriftTests
{
    private static string Runtime(string file) => File.ReadAllText(TestRepo.Src("Cobol.Net.Runtime", "IO", file));
    private static string Compiler(params string[] path) =>
        File.ReadAllText(TestRepo.Src(["Cobol.Net.Compiler", .. path]));

    /// <summary>True when the member whose declaration contains <paramref name="member"/> mentions
    /// <paramref name="needle"/> before the next member's doc comment begins.</summary>
    internal static bool MemberAsks(string source, string member, string needle)
    {
        int at = source.IndexOf(member, StringComparison.Ordinal);
        if (at < 0) return false;
        int end = source.IndexOf("    /// <summary>", at + member.Length, StringComparison.Ordinal);
        string body = end < 0 ? source[at..] : source[at..end];
        return body.Contains(needle, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRegistrysGovernedWriteAndRewrite_AskForTheFixedForm()
    {
        string registry = Runtime("FileRegistry.cs");
        Assert.True(MemberAsks(registry, "public string WriteShared(", ".FixedForm("),
            "FileRegistry.WriteShared must pass the record through FileConnector.FixedForm before the connector sees it — "
            + "a file of fixed-length records cannot frame the record or carry its extent table (D-FRA (vi)).");
        Assert.True(MemberAsks(registry, "public string RewriteShared(", ".FixedForm("),
            "FileRegistry.RewriteShared must pass the record through FileConnector.FixedForm, as WriteShared does.");
    }

    [Fact]
    public void TheIndexedKeyOfARecordAreaImage_IsTakenFromTheFixedForm()
    {
        Assert.True(MemberAsks(Runtime("IndexedConnector.cs"), "private string AreaKey(", "FixedForm("),
            "IndexedConnector.AreaKey slices a key from the program's record area image; in a fixed-length file the key "
            + "sits where the FIXED FORM puts it, which is where the registration declared it.");
    }

    [Fact]
    public void TheReadLanding_AsksTheOneFileModelPredicate()
    {
        Assert.True(MemberAsks(Compiler("CodeGen", "Verbs", "SequentialIoEmitter.cs"), "public void EmitRecordAreaStore(",
                "FixedFormRecords"),
            "SequentialIoEmitter.EmitRecordAreaStore (every READ organization and the sort RETURN route through it) must "
            + "ask FileModel.FixedFormRecords, or a record read from a fixed-length file is split by the take step with its padding.");
        string model = Compiler("Binding", "Model", "FileModel.cs");
        Assert.True(MemberAsks(model, "public bool ImpliesVariableFormat", "RecordContains is null"),
            "An explicit Format 1 RECORD clause keeps the file FIXED (§13.18.43.4 GR6): ImpliesVariableFormat requires an "
            + "ABSENT RECORD clause, as GR5's implied clause does.");
    }

    [Fact]
    public void TheGate_ActuallyFails_WhenAnEntryDoesNotAskForTheForm()
    {
        const string asks = """
                public string WriteShared(string name)
                {
                    var (medium, mediumExtents) = c.FixedForm(image, extents);
                    return WriteAnyOrg(c, medium);
                }

                /// <summary>the next member</summary>
            """;
        const string forgets = """
                public string WriteShared(string name)
                {
                    return WriteAnyOrg(c, image);
                }

                /// <summary>the next member — which does mention .FixedForm( but is not this one</summary>
                private void Other() { c.FixedForm(a, b); }
            """;
        Assert.True(MemberAsks(asks, "public string WriteShared(", ".FixedForm("));
        Assert.False(MemberAsks(forgets, "public string WriteShared(", ".FixedForm("));
        Assert.False(MemberAsks("", "public string WriteShared(", ".FixedForm("));
    }
}
