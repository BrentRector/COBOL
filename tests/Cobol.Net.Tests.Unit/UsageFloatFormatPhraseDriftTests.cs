// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY usage keyword the grammar admits must have DECIDED whether the USAGE clause's float FORMAT phrases
/// (ISO §13.18.60.2's <c>endianness-phrase</c> and <c>encoding-phrase</c>) apply to it — and the decision is
/// checked against the keyword inventory read out of the GRAMMAR, not out of a list somebody remembered.
///
/// <para><b>Why this test exists.</b> §13.18.60.2's general format is transcribed PIECEWISE into
/// <c>CobolData.g4</c>'s <c>usageKeyword</c>, one alternative per printed line, and rendering the printed page
/// (PDF p.533 = printed 503) against that rule showed five of the diagram's optional TAILS had been dropped
/// silently — the endianness-phrase on FLOAT-BINARY-32/-64/-128 and the encoding+endianness group on
/// FLOAT-DECIMAL-16/-34 among them (kb/Work PB174). Each dropped tail rejects legal 2014+ source with a raw parse
/// error. The tails are now on the CLAUSE (<c>floatFormatPhrase*</c>) and the binder narrows, so the failure mode
/// moved: a NEW usage keyword can now be added and silently inherit "the phrase parses here", with no one having
/// asked whether the standard prints a phrase on that line.</para>
///
/// <para><b>What makes it a drift test.</b> The classification table below is compared for set-equality against
/// the <c>usageKeyword</c> alternatives the grammar actually offers. Add a usage — MESSAGE-TAG, whenever it lands
/// — and this fails until its row says which phrases the printed diagram gives it. The tree already accepts this
/// obligation in principle: <c>IntrinsicBinder</c> names "the usage-inventory drift test" as the thing that
/// "forces that decision when the usage lands"; that test covers the keyword SET, and this one covers its
/// TAILS.</para>
/// </summary>
public sealed class UsageFloatFormatPhraseDriftTests : CobolNetTestBase
{
    /// <summary>Which of §13.18.60.2's two float format phrases the PRINTED general format writes on each usage
    /// line. FLOAT-BINARY-32/-64/-128 print <c>[ endianness-phrase ]</c>; FLOAT-DECIMAL-16/-34 print a BRACKETED
    /// CHOICE-INDICATOR group over { encoding-phrase, endianness-phrase } (§5.2.6.4: zero or more, each at most
    /// once, any order). Every other line prints neither. The three parser RULES in the inventory
    /// (programPointerUsage / functionPointerUsage / objectReferenceUsage) print their own operand tails, never a
    /// float format phrase.</summary>
    private static readonly Dictionary<string, (bool Endianness, bool Encoding)> PrintedTails = new(StringComparer.Ordinal)
    {
        ["DISPLAY"] = (false, false),
        ["COMPUTATIONAL"] = (false, false),
        ["COMPUTATIONAL_1"] = (false, false),
        ["COMPUTATIONAL_2"] = (false, false),
        ["COMPUTATIONAL_3"] = (false, false),
        ["COMPUTATIONAL_4"] = (false, false),
        ["COMPUTATIONAL_5"] = (false, false),
        ["COMP"] = (false, false),
        ["COMP_1"] = (false, false),
        ["COMP_2"] = (false, false),
        ["COMP_3"] = (false, false),
        ["COMP_4"] = (false, false),
        ["COMP_5"] = (false, false),
        ["FLOAT_SHORT"] = (false, false),
        ["FLOAT_LONG"] = (false, false),
        ["FLOAT_EXTENDED"] = (false, false),
        ["FLOAT_BINARY_32"] = (true, false),
        ["FLOAT_BINARY_64"] = (true, false),
        ["FLOAT_BINARY_128"] = (true, false),
        ["FLOAT_DECIMAL_16"] = (true, true),
        ["FLOAT_DECIMAL_34"] = (true, true),
        ["BINARY_CHAR"] = (false, false),
        ["BINARY_SHORT"] = (false, false),
        ["BINARY_LONG"] = (false, false),
        ["BINARY_DOUBLE"] = (false, false),
        ["BINARY"] = (false, false),
        ["PACKED_DECIMAL"] = (false, false),
        ["INDEX"] = (false, false),
        // USAGE MESSAGE-TAG (kb/Work PB487) - the printed §13.18.60.2 line is the bare word with no tail;
        // the usage is DECLINED non-support (Annex A.3 item 4) and refused by name, COBOLNET1943.
        ["MESSAGE_TAG"] = (false, false),
        ["NATIONAL"] = (false, false),
        ["BIT"] = (false, false),
        // USAGE POINTER [TO type-name-1] is a RULE, not a terminal, since kb/Work PB153 added the
        // restricted data-pointer tail. The printed diagram gives that line a TO operand and NEITHER
        // float format phrase - which is exactly the decision this table exists to force.
        ["dataPointerUsage"] = (false, false),
        ["programPointerUsage"] = (false, false),
        ["functionPointerUsage"] = (false, false),
        ["objectReferenceUsage"] = (false, false),
    };

    /// <summary>The COBOL spelling to write after USAGE for each grammar alternative, plus the picture (if any)
    /// the entry needs to be well-formed on its own. Only the usages that ADMIT a phrase and are supported need a
    /// runnable fixture — the rest are exercised through the negative direction.</summary>
    private static readonly Dictionary<string, string> Spelling = new(StringComparer.Ordinal)
    {
        ["FLOAT_BINARY_32"] = "FLOAT-BINARY-32",
        ["FLOAT_BINARY_64"] = "FLOAT-BINARY-64",
        ["COMP_1"] = "COMP-1",
        ["COMP_2"] = "COMP-2",
        ["FLOAT_SHORT"] = "FLOAT-SHORT",
        ["FLOAT_LONG"] = "FLOAT-LONG",
        ["FLOAT_EXTENDED"] = "FLOAT-EXTENDED",
        ["INDEX"] = "INDEX",
        ["dataPointerUsage"] = "POINTER",
        ["BINARY_DOUBLE"] = "BINARY-DOUBLE",
    };

    /// <summary>The <c>usageKeyword</c> rule's alternatives, read straight out of the .g4. The repo's grammar
    /// style puts a rule's name alone on its line and its body in indented <c>:</c>/<c>|</c> lines.</summary>
    private static SortedSet<string> GrammarUsageKeywords()
    {
        string path = Path.Combine(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "Core", "CobolData.g4");
        var found = new SortedSet<string>(StringComparer.Ordinal);
        bool inRule = false;
        var alt = new Regex(@"[A-Za-z_][A-Za-z0-9_]*");
        foreach (string line in File.ReadAllLines(path))
        {
            if (Regex.IsMatch(line, @"^usageKeyword\s*$")) { inRule = true; continue; }
            if (!inRule) continue;
            if (line.TrimStart().StartsWith(';')) break;                    // end of the rule
            string body = line.Split("//")[0].TrimStart();                  // strip the trailing comment
            if (!body.StartsWith(':') && !body.StartsWith('|')) continue;
            foreach (Match m in alt.Matches(body[1..])) found.Add(m.Value);
        }
        return found;
    }

    [Fact]
    public void EveryUsageKeyword_HasDecidedItsFloatFormatTails()
    {
        var grammar = GrammarUsageKeywords();
        Assert.NotEmpty(grammar);   // a sweep that found nothing would pass every assertion below vacuously
        var classified = new SortedSet<string>(PrintedTails.Keys, StringComparer.Ordinal);
        Assert.True(grammar.SetEquals(classified),
            "the grammar's usageKeyword inventory and this test's §13.18.60.2 tail classification have diverged.\n"
            + $"  grammar : {string.Join(", ", grammar)}\n"
            + $"  table   : {string.Join(", ", classified)}\n"
            + "RENDER THE PRINTED PAGE (python scripts/render-spec-page.py 533) and record which of the two float "
            + "format phrases the standard writes on the new usage's line. Five tails were dropped silently once "
            + "already (kb/Work PB174) and each one rejected legal source.");
    }

    /// <summary>The positive direction: a usage the printed diagram gives an endianness-phrase to ACCEPTS one.
    /// FLOAT-BINARY-128 and FLOAT-DECIMAL-16/-34 admit it too, but the usages themselves are documented
    /// non-support (COBOLNET1564, Annex A.3 items 17/19) so they cannot supply a clean compile.</summary>
    [Theory]
    [InlineData("FLOAT_BINARY_32", "HIGH-ORDER-LEFT")]
    [InlineData("FLOAT_BINARY_32", "HIGH-ORDER-RIGHT")]
    [InlineData("FLOAT_BINARY_64", "HIGH-ORDER-LEFT")]
    [InlineData("FLOAT_BINARY_64", "HIGH-ORDER-RIGHT")]
    public void StandardBinaryFloatUsage_AdmitsTheEndiannessPhrase_PinnedToSpec(string keyword, string phrase)
    {
        Assert.True(PrintedTails[keyword].Endianness, $"{keyword} is classified as printing no endianness-phrase");
        var errors = CompileAt2023($"pos-{keyword}-{phrase}", Spelling[keyword] + " " + phrase);
        Assert.True(errors.Count == 0,
            $"USAGE {Spelling[keyword]} {phrase} is legal 2014+ source (§13.18.60.2) but drew:\n"
            + string.Join("\n", errors));
    }

    /// <summary>The negative direction, which is what keeps the classification honest — a table that said
    /// "everything admits it" would pass the positive leg alone. COBOLNET1716 is §13.18.60.2's own scoping,
    /// corroborated by §13.18.60.4 GR19c/d.</summary>
    [Theory]
    [InlineData("COMP_1")]
    [InlineData("COMP_2")]
    [InlineData("FLOAT_SHORT")]
    [InlineData("FLOAT_LONG")]
    [InlineData("FLOAT_EXTENDED")]
    [InlineData("INDEX")]
    [InlineData("dataPointerUsage")]
    [InlineData("BINARY_DOUBLE")]
    public void NonStandardFloatUsage_RejectsTheEndiannessPhrase_PinnedToSpec(string keyword)
    {
        Assert.False(PrintedTails[keyword].Endianness, $"{keyword} is classified as printing an endianness-phrase");
        var errors = CompileAt2023($"neg-{keyword}", Spelling[keyword] + " HIGH-ORDER-RIGHT");
        Assert.Contains(errors, e => e.Contains("COBOLNET1716", StringComparison.Ordinal));
    }

    /// <summary>The encoding-phrase's scope is NARROWER than the endianness-phrase's — only the two standard
    /// DECIMAL float usages (§13.18.60.4 GR20a) — so a standard BINARY float usage must reject it. Getting this
    /// wrong in the permissive direction is invisible without this leg: FLOAT-BINARY-32 admits ONE of the two
    /// phrases.</summary>
    [Fact]
    public void StandardBinaryFloatUsage_RejectsTheEncodingPhrase_PinnedToSpec()
    {
        var errors = CompileAt2023("enc-fb32", "FLOAT-BINARY-32 BINARY-ENCODING");
        Assert.Contains(errors, e => e.Contains("COBOLNET1717", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1716", StringComparison.Ordinal));
    }

    /// <summary>§5.2.6.4: "any single alternative may be specified only once" — and, in the same clause, "The
    /// alternatives may be specified in any order", so the ORDER leg must stay clean while the REPEAT leg
    /// rejects. Without the order leg a screen that rejected the second phrase unconditionally would look
    /// correct.</summary>
    [Fact]
    public void RepeatedPhrase_Rejects_ButOrderIsFree_PinnedToSpec()
    {
        var repeated = CompileAt2023("rep", "FLOAT-BINARY-32 HIGH-ORDER-LEFT HIGH-ORDER-RIGHT");
        Assert.Contains(repeated, e => e.Contains("COBOLNET1718", StringComparison.Ordinal));

        // FLOAT-DECIMAL-16 is the only line that prints BOTH phrases, so it is the only place the any-order rule
        // is observable. The usage is documented non-support (COBOLNET1564) — assert that the ONLY complaint is
        // that non-support, in EITHER order: no 1705/1706/1707.
        foreach (string order in new[] { "BINARY-ENCODING HIGH-ORDER-RIGHT", "HIGH-ORDER-RIGHT BINARY-ENCODING" })
        {
            var errors = CompileAt2023("ord-" + order.Replace(' ', '-'), "FLOAT-DECIMAL-16 " + order);
            Assert.Contains(errors, e => e.Contains("COBOLNET1564", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1716", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1717", StringComparison.Ordinal));
            Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1718", StringComparison.Ordinal));
        }
    }

    /// <summary>The 2014 introduction gate (constructs.json <c>usage-float-format-phrase-2014</c>): the phrase is
    /// a 2014 language element in its own right — Annex A.3 item 18 names it separately from item 17's usages —
    /// so it draws COBOLNET0900 below 2014 and nothing at 2014+.</summary>
    [Fact]
    public void EndiannessPhrase_IsGatedAt2014_PinnedToSpec()
    {
        const string source = "FLOAT-BINARY-32 HIGH-ORDER-RIGHT";
        Assert.Contains(CompileAt("gate2002", source, 2002), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.DoesNotContain(CompileAt("gate2014", source, 2014), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
        Assert.DoesNotContain(CompileAt("gate2023", source, 2023), e => e.Contains("COBOLNET0900", StringComparison.Ordinal));
    }

    /// <summary>A PROGRAM-ID unique per fixture — .NET serves a STALE same-named assembly otherwise, so a shared
    /// name would silently make every later case re-observe the first one's diagnostics.</summary>
    private static string Entry(string programId, string usageTail) => $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W USAGE {usageTail}.
                   PROCEDURE DIVISION.
                   MAIN.
                       STOP RUN.
            """;

    /// <summary>A COBOL word (letters, digits, hyphens; ≤30 characters) derived from the fixture tag.</summary>
    private static string ProgramId(string tag, int dialect)
    {
        string cleaned = Regex.Replace(tag, "[^A-Za-z0-9]", "-").Trim('-');
        string id = $"UFFP-{cleaned}-{dialect}";
        return id.Length <= 30 ? id : id[^30..].TrimStart('-');
    }

    private List<string> CompileAt2023(string tag, string usageTail) => CompileAt(tag, usageTail, 2023);

    private List<string> CompileAt(string tag, string usageTail, int dialect)
    {
        string src = Path.Combine(TempDir, $"{tag}_{dialect}.cob");
        File.WriteAllText(src, Entry(ProgramId(tag, dialect), usageTail));
        var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: dialect, SourceFormat: InitialReferenceFormat.Auto));
        return [.. r.Errors, .. r.Warnings];
    }
}
