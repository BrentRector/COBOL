// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ISO §8.4.2.3.3 SR2 HAS TWO HALVES AND THE COMPILER ONLY EVER WROTE DOWN THE FIRST (kb/Work PB877).
/// SR2: "If a subscript is specified, the data description entry describing qualified-data-name-1 or the
/// conditional variable associated with qualified-condition-name-1 shall contain an OCCURS clause <b>or shall be
/// subordinate to a data description entry that contains an OCCURS clause</b>." <c>DataItem.IsTable</c> answers
/// the first half only. The segmenter that decides whether a <c>'('</c> after a name belongs to that name or
/// begins a new subscript (<c>ReferenceResolver.SegmentsOf</c>, a token splitter before kb/Work PB2113) asked
/// <c>IsTable</c>, so <c>X</c> — <c>PIC 9</c> under <c>02 G2 OCCURS 3</c> — was
/// judged unsubscriptable, its own <c>'('</c> opened a second subscript, and <c>Y (X(1))</c> became a
/// two-subscript reference to a one-dimensional table: rejected loud on the receiving side (COBOLNET0899) and
/// compiled-then-aborted-at-run-time on the sending side.
///
/// <para><b>What this pins, and why it is a source scan.</b> The property is not observable from behaviour once
/// the predicate is right — every arm looks identical from outside — so the guard has to be about WHERE the rule
/// is written. Three call sites segment a subscript list and each one used to spell its own lambda; two of
/// them spelled the same wrong one, and <c>SubscriptSegments</c>'s doc-comment CLAIMED they "use the same
/// declaration-informed '(' rule" while nothing made that true. It is true structurally now — one named
/// predicate, <c>ReferenceResolver.CannotBeSubscripted</c> — and these tests fail the moment a fourth caller
/// re-spells it or an edit puts <c>IsTable</c> back in the admission position.</para>
///
/// <para>The behavioural half lives in the conformance corpus, where it belongs:
/// <c>tests/conformance/85/pb877_subscripted_subscript.cob</c> (the positive, one line per receiver family plus
/// the sending, ref-mod, function-argument and SEARCH ALL arms) and the two negatives
/// <c>pb877-subscript-on-non-table-item</c> / <c>pb877-subscript-count-mismatch</c>.</para>
/// </summary>
public sealed class SubscriptAdmissionDriftTests
{
    private static string Src(params string[] parts) => Path.Combine([TestRepo.Root, "src", .. parts]);

    private static readonly string ReferenceResolver =
        Src("Cobol.Net.Compiler", "Binding", "ReferenceResolver.cs");
    private static readonly string DataItem =
        Src("Cobol.Net.Compiler", "Binding", "Model", "DataItem.cs");

    /// <summary>Every caller that segments a subscript list passes THE shared §8.4.2.3.3 SR2 predicate.
    /// A call with any other argument — or with none, which is how the intrinsic table(ALL) caller silently kept
    /// the pre-PB136 join — is the drift this test exists to catch.</summary>
    [Fact]
    public void EverySegmentsOfCallSite_PassesTheOneSr2Predicate()
    {
        var sites = new List<(string File, string Args)>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestRepo.Root, "src", "Cobol.Net.Compiler"),
                     "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            foreach (Match m in Regex.Matches(text,
                         @"(?<!internal static List<List<IToken>> )(?<![A-Za-z])SegmentsOf\((?<args>[^;]*?)\)\s*[;),]",
                         RegexOptions.Singleline))
                sites.Add((Path.GetFileName(file), m.Groups["args"].Value));
        }

        // The declaration itself is excluded by the lookbehind; what is left is call sites only.
        Assert.True(sites.Count >= 3,
            $"expected at least the three known SegmentsOf call sites, found {sites.Count} — if the "
            + "segmenter moved, this guard moves with it (kb/Work PB877)");
        foreach (var (file, args) in sites)
            Assert.True(args.Contains("CannotBeSubscripted", StringComparison.Ordinal),
                $"{file}: SegmentsOf is called with '{args.Trim()}' instead of the shared "
                + "ReferenceResolver.CannotBeSubscripted predicate. ISO §8.4.2.3.3 SR2 decides whether a '(' after "
                + "a name belongs to that name; writing the test a second time is how PB877 happened.");
    }

    /// <summary>The predicate asks SR2's WHOLE question. <c>IsTable</c> in this position is the defect itself.</summary>
    [Fact]
    public void TheSr2Predicate_AsksIsTableElement_NotIsTable()
    {
        string text = File.ReadAllText(ReferenceResolver);
        // The predicate takes the WHOLE qualified name (kb/Work PB1455): a '(' after `E OF T` subscripts `E OF T`.
        var m = Regex.Match(text, @"bool CannotBeSubscripted\(string name, List<string> qualifiers\)\s*=>(?<body>[^;]*);");
        Assert.True(m.Success, "ReferenceResolver.CannotBeSubscripted is gone — §8.4.2.3.3 SR2's admission test "
                               + "has no home, and the subscript splitter has no shared rule (kb/Work PB877)");
        string body = m.Groups["body"].Value;
        Assert.Contains("IsTableElement", body, StringComparison.Ordinal);
        Assert.DoesNotContain("IsTable:", body, StringComparison.Ordinal);
    }

    /// <summary>SR2's second half IS the definition of <c>IsTableElement</c>: an item is subscriptable when its
    /// description — its own entry or any ancestor's — contains an OCCURS clause, which is exactly
    /// <c>SubscriptArity &gt; 0</c>.</summary>
    [Fact]
    public void IsTableElement_IsDefinedAsANonZeroSubscriptArity()
    {
        string text = File.ReadAllText(DataItem);
        Assert.Matches(@"public bool IsTableElement\s*=>\s*SubscriptArity > 0;", text);
        Assert.Contains("public int SubscriptArity", text, StringComparison.Ordinal);
        Assert.Contains("public List<DataItem> SubscriptLevels()", text, StringComparison.Ordinal);
    }

    /// <summary>§8.4.2.3.3 SR3's arity walk — "the number of subscripts shall equal the number of OCCURS clauses
    /// in the description of the table element being referenced" — exists in exactly ONE place. Five copies of
    /// this ancestor walk were in the compiler when PB877 landed; the ones that answer a DIFFERENT question (a
    /// class-scoped Tier-B window walk, which counts <c>Occurs is not null</c> and stops at the REDEFINES class
    /// boundary) do not match this pattern and are untouched.</summary>
    [Fact]
    public void TheSr3ArityWalk_IsWrittenExactlyOnce()
    {
        var files = new List<string>();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(TestRepo.Root, "src", "Cobol.Net.Compiler"),
                     "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (Regex.IsMatch(text,
                    @"for \(DataItem\? (?<v>\w+) = [^;]+; \k<v> is not null; \k<v> = \k<v>\.Parent\)\s*if \(\k<v>\.IsTable\)"))
                files.Add(Path.GetFileName(file));
        }
        Assert.Equal(["DataItem.cs"], files.Distinct().Order().ToArray());
    }

    /// <summary>Both rules are reported by name, on both sides of a statement. Deleting either screen would put
    /// COBOLNET0899's "not yet implemented" promise back on permanently illegal source (the PB489 shape).</summary>
    [Fact]
    public void BothSubscriptScreens_AreWiredAtTheOneResolutionSite()
    {
        string text = File.ReadAllText(ReferenceResolver);
        // kb/Work PB1030: the written list is read by ONE method, ReadSubscripts, which every entry calls.
        Assert.Single(Regex.Matches(text, @"ScreenSubscriptArity\(dref, item, e\.Count\)"));
        Assert.Contains("DiagnosticCatalog.SubscriptOnNonTableItem", text, StringComparison.Ordinal);
        Assert.Contains("DiagnosticCatalog.SubscriptCountMismatch", text, StringComparison.Ordinal);
    }

    /// <summary>kb/Work PB681 — the THIRD arm of the written-subscript family, §8.4.2.3.3 SR5's omitted list, on
    /// EVERY entry that resolves an identifier: <c>Resolve</c>, <c>ResolveForItem</c> (the condition-name
    /// conditional variable) and <c>ResolveForAddressOf</c>. Each used to return an unreported null — or, for
    /// ADDRESS OF, the first occurrence's address — so <c>MOVE E TO B</c> over a table element compiled clean and
    /// aborted at run time. An entry that forgets the omitted arm is the two-arm-dispatch shape
    /// (feedback_two_arm_dispatch); this pins all three.</summary>
    [Fact]
    public void TheOmittedSubscriptList_IsScreenedAtEveryIdentifierEntry()
    {
        string text = File.ReadAllText(ReferenceResolver);
        // kb/Work PB1030: the three entries share ONE reading of the written list (ReadSubscripts), so the omitted
        // arm is written once and each entry is pinned to calling it — an entry that stops calling it is red here.
        Assert.Single(Regex.Matches(text, @"ScreenSubscriptArity\(dref, item, 0\)"));
        Assert.Equal(3, Regex.Matches(text, @"ReadSubscripts\(dref, item, [^,]+, out var \w+\)").Count);
        Assert.Contains("DiagnosticCatalog.TableElementNotSubscripted", text, StringComparison.Ordinal);
    }
}
