// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE STRUCTURAL CLAMP ON HOW A FILE CLAUSE'S data-name OPERAND IS CAPTURED AND RESOLVED (kb/Work PB489).
/// <para>The defect was ONE LINE written in five places and ONE LOOKUP written in four: a clause reduced its
/// written <c>dataReference</c> to <c>d.cobolWord()?.GetText()</c> — the FIRST word, so every IN/OF qualifier
/// was discarded — and then resolved that bare word with <c>ByName.TryGetValue(n, out var l) ? l[0] : null</c>,
/// a first-match lookup with no uniqueness test. Both halves are silent: the LINAGE clause built a program's
/// whole logical page on another data item's value, and the compile output was EMPTY.</para>
/// <para>The fix is structural — one capture (<c>ClauseDataName</c>, which also refuses the shapes a
/// <i>data-name-n</i> position does not admit) and one resolver (<c>ResolveClauseOperand</c>, over the single
/// ISO §8.4.2.2 candidate-set matcher <c>DataBinder.QualifiedCandidates</c>) — so the guard has to be that the
/// OLD SHAPES DO NOT COME BACK. A behavioural test proves today's clauses are right
/// (<c>ClauseOperandQualificationSpecTests</c>); this one keeps the NEXT clause automatic, which is the half a
/// behavioural test cannot reach: a new clause written with the old idiom would pass every existing test.</para>
/// <para>⚠ It reads SOURCE TEXT, which is unusual here and deliberate. The property is "no second copy of this
/// rule exists", and a second copy is by definition something no run-time observation of the first can see.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ClauseOperandCaptureDriftTests
{
    private static string DataBinderSource() => File.ReadAllText(
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "DataBinder.cs"));

    /// <summary>The region of a file between a member's signature and the next member at the same indentation —
    /// good enough to scope an assertion to ONE method, and asserted non-trivial by its callers.</summary>
    private static string MethodBody(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"'{signature}' is not in DataBinder.cs — the member was renamed; re-derive this guard.");
        int end = source.IndexOf("\n    /// <summary>", start + signature.Length, StringComparison.Ordinal);
        return source[start..(end < 0 ? source.Length : end)];
    }

    /// <summary>⛔ PROVE THE SCANNER CAN FAIL before any absence is trusted (a green check that never looked at
    /// anything is not evidence): the first-word reduction IS present in DataBinder.cs — it is the legitimate
    /// one, inside the shared capture — and the first-match lookup idiom IS still present somewhere in the file,
    /// so "not in ResolveFiles" below is a measurement rather than a spelling accident.</summary>
    [Fact]
    public void TheScanners_FindTheShapesTheyLookFor()
    {
        string src = DataBinderSource();
        Assert.Matches(FirstWordReduction, src);
        Assert.Contains("ByName.TryGetValue", src, StringComparison.Ordinal);
        Assert.NotEqual(0, MethodBody(src, "internal void ResolveFiles()").Length);
    }

    /// <summary>Inside the FILE CLAUSE binder, a <c>dataReference</c> is reduced to its first word in EXACTLY ONE
    /// place. Every clause that captures a data-name operand goes through it, so a qualifier can no longer be
    /// dropped by a new clause simply repeating the idiom — which is how FILE STATUS, RELATIVE KEY,
    /// RECORD … DEPENDING ON and all four LINAGE operands each acquired their own copy of it.
    /// <para>⚠ THE SCOPE IS <c>DataBinder.cs</c>, AND THAT IS STATED RATHER THAN IMPLIED. The idiom is measured
    /// in ten files of <c>src/Cobol.Net.Compiler</c> — the report-writer captures (<c>DataBinder.Reports.cs</c>,
    /// the SUM UPON detail-names), the OCCURS DEPENDING capture, and seven procedure-division sites where the
    /// full reference is bound elsewhere and the base word is taken only as a lookup key. Whether each of those
    /// is a defect is a question per subsystem, not this guard's; asserting ONE across the tree would be a claim
    /// nobody has measured, and leaving the scope unsaid would let this test be read as that claim
    /// (feedback_measure_the_selectors_complement).</para></summary>
    [Fact]
    public void TheFirstWordReduction_IsWrittenDownOnce_InTheFileClauseBinder()
    {
        string src = DataBinderSource();
        var hits = Regex.Matches(src, FirstWordReduction);
        Assert.Single(hits);
        // internal since kb/Work PB1018: the SORT table-key phrase captures its qualifiers through this same one.
        string capture = MethodBody(src, "internal static (string Base, IReadOnlyList<string> Quals) KeyReference(");
        Assert.Contains(hits[0].Value, capture, StringComparison.Ordinal);
    }

    /// <summary>⛔ THE DATA-NAME-N SCREEN READS SUFFIXES THROUGH THE ONE LEXICAL READER (kb/Work PB481). A
    /// reference-modifier reaches the binder through two carriers frozen at lex time — the parsed <c>refModPart</c>
    /// and the SUBSCRIPT-mode captured group with a depth-0 colon — and a screen that walked the parse tree itself
    /// counted every <c>subscriptPart</c> as a subscript, refusing <c>RECORD KEY IS IX-KEY(1:3)</c> as "written
    /// with a subscript". Only <c>ReferenceResolver.ReadOperandSuffixes</c> knows both carriers.</summary>
    [Fact]
    public void TheDataNameScreen_ReadsSuffixesThroughTheOneReader()
    {
        string body = MethodBody(DataBinderSource(), "internal static bool ScreenDataNameShape(");
        Assert.Contains("ReferenceResolver.ReadOperandSuffixes(", body, StringComparison.Ordinal);
        Assert.DoesNotContain(".subscriptPart()", body, StringComparison.Ordinal);
        Assert.DoesNotContain(".refModPart()", body, StringComparison.Ordinal);
    }

    /// <summary>⛔ EVERY <c>KeyReference</c> CAPTURE OUTSIDE THE REPORT SECTION IS SCREENED (kb/Work PB481).
    /// <c>KeyReference</c> keeps the qualifiers and DROPS a subscript or reference-modifier, so a data-name-n
    /// operand that reaches it unscreened binds as if unmodified — the SORT table key did, and sorted on the whole
    /// item. The report-section callers (<c>DataBinder.Reports.cs</c>) are the principled exceptions: §13.18.16.3
    /// SR4 and its twins PERMIT a reference-modifier there and read it through <c>ReadOperandSuffixes</c>. Any
    /// other file that calls <c>KeyReference</c> must also call the screen, so a new key-naming caller cannot
    /// re-open the drop by repeating the idiom.</summary>
    [Fact]
    public void EveryKeyReferenceCapture_OutsideTheReportSection_IsScreened()
    {
        int callers = 0;
        foreach (string path in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || Path.GetFileName(path) == "DataBinder.Reports.cs")
                continue;
            string src = File.ReadAllText(path);
            if (!Regex.IsMatch(src, @"KeyReference\((?!Core\.)")) continue;   // a call, not the definition
            callers++;
            Assert.True(src.Contains("ScreenDataNameShape(", StringComparison.Ordinal)
                        || src.Contains("ScreenClauseOperandShape(", StringComparison.Ordinal),
                $"{Path.GetFileName(path)} calls KeyReference without the data-name-n shape screen (kb/Work PB481)");
        }
        Assert.True(callers >= 2, "the scan found fewer KeyReference callers than DataBinder + SortBinder — re-derive it");
    }

    /// <summary>The post-build file-clause resolution holds NO first-match name lookup. §8.4.2.2.1 makes the
    /// NUMBER of candidates the answer — "uniqueness shall be established through qualification" — so a site that
    /// takes <c>[0]</c> has substituted declaration order for the standard's rule, silently.</summary>
    [Fact]
    public void ResolveFiles_HasNoFirstMatchNameLookup()
    {
        string body = MethodBody(DataBinderSource(), "internal void ResolveFiles()");
        Assert.DoesNotContain("ByName.TryGetValue", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ByName[", body, StringComparison.Ordinal);
    }

    /// <summary>⭐ ONE §8.4.2.2 QUALIFICATION MATCHER IN THE WHOLE BINDER. There were two: the complete one in
    /// <c>ReferenceResolver</c> (candidate set, exactly one survivor, the file-name as the outermost qualifier)
    /// and a weaker private twin in <c>DataBinder</c> that knew neither the file-name qualifier nor uniqueness —
    /// so the procedure division read the complete rule and the data division the partial one. The matcher is
    /// now defined once; every other file may CALL it, and none may define its own.</summary>
    [Fact]
    public void TheQualificationMatcher_IsDefinedOnce()
    {
        var definitions = new List<string>();
        foreach (string path in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;
            foreach (string line in File.ReadLines(path))
                if (Regex.IsMatch(line, @"\b(bool|DataItem\??)\s+(QualifierChainMatches|QualifiersMatch|FindQualified)\s*\("))
                    definitions.Add($"{Path.GetFileName(path)}: {line.Trim()}");
        }
        Assert.Single(definitions);
        Assert.StartsWith("DataBinder.cs:", definitions[0], StringComparison.Ordinal);
    }

    /// <summary>The first-word reduction, as it was written at every one of the sites this change collapsed:
    /// <c>x.cobolWord()?.GetText() ?? x.GetText()</c> (with or without a null-forgiving operator).</summary>
    private const string FirstWordReduction = @"\w+!?\.cobolWord\(\)\?\.GetText\(\) \?\? \w+!?\.GetText\(\)";
}
