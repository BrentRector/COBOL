// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE §13.18.60.4 GR1 EQUIVALENCE, over the WHOLE <see cref="Usage"/> enum (kb/Work PB495).
///
/// <para>GR1: <i>"If the USAGE clause is specified or implied at a group level, it applies only to each
/// elementary item in the group."</i> It does not say the group's clause INFLUENCES its leaves; it says the
/// clause APPLIES TO each of them. So for every usage the compiler admits and every picture a programmer can
/// write beside it, these two spellings describe THE SAME ITEM:</para>
/// <code>
///     01 G USAGE u.  05 A ⟨pic⟩.      ≡      01 G.  05 A ⟨pic⟩ USAGE u.
/// </code>
/// <para>— same verdict, same diagnostic code, same width. This test enumerates <see cref="Usage"/> and asserts
/// exactly that, so a usage added to the enum tomorrow is covered without anyone remembering to cover it.</para>
///
/// <para><b>Why an enumeration and not a list of cases.</b> Before PB495 the equivalence held for THREE usages.
/// The inheritance pass carried two hand-written sets — <c>ResolveIndexItems</c>' shed list and
/// <c>InheritUsageClauses</c>' <c>Binary or Packed or Comp5</c> — neither derived from anything, and every usage
/// in neither fell through SILENTLY: a picture-less leaf under a group-level BINARY-SHORT became a ZERO-LENGTH
/// item, a group-level FLOAT-SHORT emitted the group itself as a scalar <c>float</c>, and a PICTURE-bearing leaf
/// under a group-level BINARY-LONG kept its usage-display width with no diagnostic. Measured on 39479e19: of the
/// 138 (usage × picture × arm) cells this file's matrix covers, 108 DIFFERED between the two arms.</para>
///
/// <para><b>⛔ WHAT THIS FILE CANNOT CATCH.</b> The one hand-written table left is
/// <see cref="UsageFamilies.IsPictureless"/>, and it drives BOTH arms — which is the point of writing the set
/// down once, and which also means a WRONG ENTRY IN IT IS INVISIBLE HERE: both spellings move together and the
/// equivalence still holds. Measured while this file was written — dropping BINARY-SHORT from the set left every
/// test below green while `05 A PIC 9(4) USAGE BINARY-SHORT.` became legal on both arms. The independent oracle
/// is the standard's own sentence, and it lives in <c>PicturelessUsageSetDriftTests</c> (Unit), which re-reads
/// §13.16.3 SR8 out of <c>specs/ISO_COBOL.md</c>. Neither file is sufficient alone.</para>
/// </summary>
public sealed class UsageInheritanceDriftTests
{
    /// <summary>Every <see cref="Usage"/> member, spelled as a programmer would write it.</summary>
    public static TheoryData<Usage> AllUsages()
    {
        var d = new TheoryData<Usage>();
        foreach (Usage u in Enum.GetValues<Usage>()) d.Add(u);
        return d;
    }

    /// <summary>The pictures the §13.18.60.3 screens discriminate between: none, numeric, SIGNED numeric,
    /// alphanumeric, national, boolean, numeric-edited. One representative per category the rules name.
    /// <para>The signed representative is not decoration: §13.18.40.3 SR31 ("The symbol 'S' shall not be
    /// specified in character-string-1 when the NO SIGN phrase of the USAGE Clause is specified for the subject
    /// of the entry") is written over exactly that distinction, and with only an UNSIGNED numeric cell the
    /// matrix could not see it on either arm (kb/Work PB570).</para></summary>
    private static readonly string[] Pictures =
        ["", "PIC 9(4)", "PIC S9(4)", "PIC X(3)", "PIC N(2)", "PIC 1(3)", "PIC ZZ9"];

    /// <summary>⛔ THE OPTIONAL PHRASES §13.18.60.2 PRINTS INSIDE A USAGE ALTERNATIVE — read off the rendered
    /// general format, keyed by the usage word they ride on. A phrase is PART OF the USAGE clause, not a clause
    /// of its own, so §13.18.60.4 GR1 applies it to each elementary item in the group exactly as it applies the
    /// word: the equivalence below is asserted over `word + phrase`, not just over `word`.
    /// <para><b>Why the table exists.</b> kb/Work PB570: `WITH NO SIGN` was read from the ENTRY'S OWN
    /// <c>usageClause</c> and adjudicated against the entry's own PICTURE, which a group header has not got, so
    /// the group spelling lost BOTH §13.18.40.3 SR31 and §13.18.60.4 GR11's width — silently, at every nesting
    /// depth — while the enum-driven matrix above stayed green because it only ever wrote the bare word.</para>
    /// <para>⛔ EXCLUDED BY PREDICATE, not by omission: a phrase that carries an OPERAND (<c>POINTER TO
    /// type-name-1</c>, <c>FUNCTION-POINTER TO function-prototype-name-1</c>, <c>PROGRAM-POINTER TO
    /// program-prototype-name-1</c>, and OBJECT REFERENCE's interface-name / <c>FACTORY OF</c> / <c>ONLY</c>)
    /// needs a declared name, so the two arms would not be the same PROGRAM and the comparison would not be
    /// about GR1. Those belong to their own subsystem's tests. Every keyword-only phrase in the figure is
    /// here.</para></summary>
    private static readonly (string Word, string Phrase)[] UsageClausePhrases =
    [
        ("BINARY-CHAR", "SIGNED"), ("BINARY-CHAR", "UNSIGNED"),
        ("BINARY-SHORT", "SIGNED"), ("BINARY-SHORT", "UNSIGNED"),
        ("BINARY-LONG", "SIGNED"), ("BINARY-LONG", "UNSIGNED"),
        ("BINARY-DOUBLE", "SIGNED"), ("BINARY-DOUBLE", "UNSIGNED"),
        ("PACKED-DECIMAL", "WITH NO SIGN"),
        ("FLOAT-BINARY-32", "HIGH-ORDER-LEFT"), ("FLOAT-BINARY-32", "HIGH-ORDER-RIGHT"),
        ("FLOAT-BINARY-64", "HIGH-ORDER-LEFT"), ("FLOAT-BINARY-64", "HIGH-ORDER-RIGHT"),
        ("FLOAT-BINARY-128", "HIGH-ORDER-LEFT"), ("FLOAT-BINARY-128", "HIGH-ORDER-RIGHT"),
        ("FLOAT-DECIMAL-16", "BINARY-ENCODING"), ("FLOAT-DECIMAL-16", "DECIMAL-ENCODING"),
        ("FLOAT-DECIMAL-16", "HIGH-ORDER-LEFT"), ("FLOAT-DECIMAL-16", "HIGH-ORDER-RIGHT"),
        ("FLOAT-DECIMAL-34", "BINARY-ENCODING"), ("FLOAT-DECIMAL-34", "DECIMAL-ENCODING"),
        ("FLOAT-DECIMAL-34", "HIGH-ORDER-LEFT"), ("FLOAT-DECIMAL-34", "HIGH-ORDER-RIGHT"),
    ];

    /// <summary>Every keyword-only USAGE-clause phrase of <see cref="UsageClausePhrases"/>, as a theory row.</summary>
    public static TheoryData<string, string> AllUsagePhrases()
    {
        var d = new TheoryData<string, string>();
        foreach ((string word, string phrase) in UsageClausePhrases) d.Add(word, phrase);
        return d;
    }

    /// <summary>The compile verdict, reduced to what GR1 makes comparable: the diagnostic CODES (order-
    /// insensitive, de-duplicated) and — for a clean compile — the item's width as the program itself reports it.
    /// Positions and message text legitimately differ (each names its own entry); the VERDICT may not.</summary>
    private static string Verdict(string source)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(source);
        string text = ok ? stdout : detail;
        var codes = Regex.Matches(text, @"COBOL(?:NET)?\d{4}").Select(m => m.Value).Distinct().Order().ToList();
        if (!ok && codes.Count == 0) codes.Add("FAIL-WITHOUT-A-CODE:" + text.Split('\n')[0].Trim());
        return codes.Count > 0 ? string.Join(",", codes) : "OK " + stdout.Trim();
    }

    private static string Program(string id, string ws) =>
        $"""
         IDENTIFICATION DIVISION.
         PROGRAM-ID. {id}.
         DATA DIVISION.
         WORKING-STORAGE SECTION.
         {ws}
         PROCEDURE DIVISION.
             DISPLAY "LEN=" FUNCTION BYTE-LENGTH(A).
             STOP RUN.
         """;

    [Theory]
    [MemberData(nameof(AllUsages))]
    public void InheritedUsage_IsTheSameItemAsAWrittenOne(Usage usage) =>
        AssertGr1Equivalence(UsageFamilies.UsageWord(usage), "");

    /// <summary>The SAME equivalence over the USAGE clause's optional PHRASES (<see cref="UsageClausePhrases"/>).
    /// A phrase is part of the clause §13.18.60.4 GR1 applies, so `01 G USAGE u p. 05 A ⟨pic⟩.` describes the
    /// same item as `01 G. 05 A ⟨pic⟩ USAGE u p.` — including the SR31 rejection when the picture carries 'S'
    /// and the phrase is WITH NO SIGN. Before kb/Work PB570 the second spelling was the only one adjudicated at
    /// all: the first lost both its syntax rule and its width, in silence.</summary>
    [Theory]
    [MemberData(nameof(AllUsagePhrases))]
    public void InheritedUsagePhrase_IsTheSameItemAsAWrittenOne(string word, string phrase) =>
        AssertGr1Equivalence(word, phrase);

    /// <summary>One usage-clause spelling, compared written-on-the-leaf against written-on-the-group over every
    /// picture in <see cref="Pictures"/>. The ONE comparison both theories run — the §13.18.60.4 GR1 equivalence
    /// is a single rule, and a second copy of the walk would be a second place for it to be wrong.</summary>
    private static void AssertGr1Equivalence(string word, string phrase)
    {
        string clause = phrase.Length == 0 ? word : word + " " + phrase;
        string tag = Regex.Replace(clause, "[^A-Z0-9]", "");
        foreach (string pic in Pictures)
        {
            // Unique PROGRAM-IDs per cell — .NET serves a stale same-named assembly otherwise.
            string cell = tag + Regex.Replace(pic, "[^A-Z0-9]", "");
            string direct = Program($"UD{cell}", $"01 G.\n    05 A {pic} USAGE {clause}.");
            string inherited = Program($"UI{cell}", $"01 G USAGE {clause}.\n    05 A {pic}.");
            string dv = Verdict(direct), iv = Verdict(inherited);
            string label = $"{clause} / {(pic.Length == 0 ? "(no picture)" : pic)}";

            Assert.Equal($"{label} → {dv}", $"{label} → {iv}");
        }
    }

    /// <summary>The set against the REPRESENTATION MACHINERY — not against the standard (that is
    /// <c>PicturelessUsageSetDriftTests</c>, and this one cannot substitute for it: the SR8 screen is driven by
    /// the same predicate, so the first assertion below is circular by construction and is kept only to pin the
    /// screen's presence). The SECOND assertion is not circular and is the one that earns its keep: a usage the
    /// set calls picture-less must have a synthesized profile in <c>BindEntry</c>'s picture-less chain, or
    /// <c>05 A USAGE u.</c> binds an item with NO PicInfo — the zero-length cell whose MOVE receiver crashed the
    /// emitter (kb/Work PB495). The table and that chain are two different hand-written places, and this is what
    /// holds them together.
    ///
    /// <para>Some members are excluded because the compiler refuses the KEYWORD before any picture rule is
    /// reached, so neither spelling is evidence about SR8: FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 are
    /// documented processor-dependent non-support (COBOLNET1564, Annex A.3 items 17/19), and the five pointer/object
    /// phrases (MESSAGE-TAG, declined non-support COBOLNET1943, Annex A.3 item 4, kb/Work PB487, among them) are
    /// refused by §13.18.60.3 SR14 at level 05 whatever the picture — which they are on BOTH spellings since
    /// kb/Work PB819 made the elementary arm read the phrase, so no carve-out for a refused keyword remains in the
    /// GR1 equivalence above. They are named by PREDICATE — the 1564 test and the 1724 test — never by list, so a
    /// member that later lands is tested automatically.</para>
    ///
    /// <para>The second sentence's arm is asserted only in the picture-less direction (a picture-less item of a
    /// picture-less usage compiles). Its converse — a picture-REQUIRING usage with no picture — is kb/Work
    /// PB504's open mechanism (§13.16.3 SR9's VALUE-implied picture is synthesized nowhere), so it is asserted
    /// as the CURRENT state with the note named, not as the rule.</para></summary>
    [Theory]
    [MemberData(nameof(AllUsages))]
    public void Sr8Set_MatchesWhatTheCompilerActuallyDoes(Usage usage)
    {
        string word = UsageFamilies.UsageWord(usage);
        string tag = Regex.Replace(word, "[^A-Z0-9]", "");
        string withPicture = Verdict(Program($"S8P{tag}", $"01 G.\n    05 A PIC 9(4) USAGE {word}."));
        string without = Verdict(Program($"S8N{tag}", $"01 G.\n    05 A USAGE {word}."));

        // The keyword itself refused (documented non-support COBOLNET1564): neither spelling is SR8 evidence,
        // because no picture rule is ever reached.
        if (withPicture.Contains("COBOLNET1564")) return;
        // §13.18.60.3 SR14 refuses the five pointer/object phrases at level 05 whatever the picture — the
        // placement rule fires first, so again neither spelling is SR8 evidence here.
        if (withPicture.Contains("COBOLNET1724") && without.Contains("COBOLNET1724")) return;

        if (UsageFamilies.IsPictureless(usage))
        {
            Assert.True(withPicture.StartsWith("COBOL"),
                $"USAGE {word} is in UsageFamilies.IsPictureless, so ISO §13.16.3 SR8 forbids a PICTURE beside "
                + $"it — but `05 A PIC 9(4) USAGE {word}.` compiled: {withPicture}");
            Assert.True(without.StartsWith("OK"),
                $"USAGE {word} is picture-less, so `05 A USAGE {word}.` describes a complete item — got {without}");
        }
        else
        {
            Assert.True(withPicture.StartsWith("OK") || withPicture.Contains("COBOLNET0881"),
                $"USAGE {word} is NOT in UsageFamilies.IsPictureless, so ISO §13.16.3 SR8 REQUIRES a PICTURE "
                + $"beside it — `05 A PIC 9(4) USAGE {word}.` must be accepted, or refused by a §13.18.60.3 "
                + $"picture-category rule (COBOLNET0881), not by an SR8 prohibition: {withPicture}");
        }
    }

    /// <summary>The headline REPRESENTATION half, end to end and in one program: a PICTURE-less elementary item
    /// under a group that wrote a picture-less usage gets THAT USAGE'S width — the width §13.18.60.4 GR12/GR13
    /// and the implementor's Annex A.1 determination fix — where before PB495 it got a ZERO-LENGTH cell and a
    /// MOVE into it crashed the compiler (NullReferenceException in <c>MoveEmitter.ConvertSource</c>).</summary>
    [Fact]
    public void PicturelessLeafUnderAGroupUsage_TakesTheUsagesWidth()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495WIDTHS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GC USAGE BINARY-CHAR.
                05 AC.
            01 GS USAGE BINARY-SHORT.
                05 AS-I.
            01 GL USAGE BINARY-LONG.
                05 AL.
            01 GD USAGE BINARY-DOUBLE.
                05 AD.
            01 GF USAGE FLOAT-SHORT.
                05 AF.
            01 GG USAGE FLOAT-LONG.
                05 AG.
            01 GI USAGE INDEX.
                05 AI.
            PROCEDURE DIVISION.
                MOVE 12 TO AS-I.
                DISPLAY "C=" FUNCTION BYTE-LENGTH(AC)
                        " S=" FUNCTION BYTE-LENGTH(AS-I)
                        " L=" FUNCTION BYTE-LENGTH(AL)
                        " D=" FUNCTION BYTE-LENGTH(AD)
                        " F=" FUNCTION BYTE-LENGTH(AF)
                        " G=" FUNCTION BYTE-LENGTH(AG)
                        " I=" FUNCTION BYTE-LENGTH(AI)
                        " GRP=" FUNCTION BYTE-LENGTH(GS).
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // 1/2/4/8 for binary-char/-short/-long/-double (§13.18.60.4 GR12's minimum ranges over the
        // implementor's two's-complement widths, Annex A.1); 4/8 for float-short/float-long (GR13 + A.1);
        // 8 for the index item (§13.18.60.4 GR10, implementor-defined — the R40 pin). The GROUP is the width
        // of its one leaf, never zero and never a scalar of the usage's own type.
        Assert.Equal("C=1 S=2 L=4 D=8 F=4 G=8 I=8 GRP=2", stdout);
    }

    /// <summary>§13.18.60.3 SR2 — a subordinate entry may write a USAGE clause beside its group's, but "the same
    /// usage shall be specified in both entries". The rule had NO site at all before PB495: the nearer clause
    /// simply won and the outer one was discarded silently, changing the item's representation and the group's
    /// width from what either written clause asked for.</summary>
    [Fact]
    public void Sr2_ContradictingUsages_AreDiagnosed_AndTheAgreeingSpellingCompiles()
    {
        const string bad = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495SR2BAD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G USAGE COMPUTATIONAL.
                05 A PIC 9(4).
                05 B PIC 9(4) USAGE DISPLAY.
            PROCEDURE DIVISION.
                DISPLAY "X".
                STOP RUN.
            """;
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(bad);
        Assert.False(ok, "a USAGE DISPLAY leaf inside a USAGE COMPUTATIONAL group violates ISO §13.18.60.3 SR2");
        Assert.Contains("COBOLNET1927", detail);
        Assert.Contains("§13.18.60.3 SR2", detail);

        // SR6 makes COMP the abbreviation for COMPUTATIONAL, and this implementation identifies BINARY with
        // both (§13.18.60.4 GR4/GR6 are each implementor-defined) — so "the same usage" is satisfied and all
        // three leaves are the 2-byte binary item, group width 6.
        const string ok3 = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB495SR2OK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 G USAGE COMPUTATIONAL.
                05 A PIC 9(4).
                05 B PIC 9(4) USAGE COMP.
                05 C PIC 9(4) USAGE BINARY.
            PROCEDURE DIVISION.
                DISPLAY "A=" FUNCTION BYTE-LENGTH(A) " B=" FUNCTION BYTE-LENGTH(B)
                        " C=" FUNCTION BYTE-LENGTH(C) " G=" FUNCTION BYTE-LENGTH(G).
                STOP RUN.
            """;
        var (ok2, stdout, detail2) = new CobolNetCompiler(2023).CompileAndRun(ok3);
        Assert.True(ok2, detail2);
        Assert.Equal("A=2 B=2 C=2 G=6", stdout);
    }
}
