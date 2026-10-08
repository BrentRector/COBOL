// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using CobolNet;
using CobolNet.Runtime;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Exceptions;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// FUNCTION STANDARD-COMPARE (ISO §15.85) and the SPECIAL-NAMES ORDER TABLE clause (§12.3.7.2), increment T7 of
/// <c>docs/rearchitecture/DESIGN-locale-facility.md</c> §4.9 (kb/Work PB101; owner decision Q4, 2026-08-18).
/// Two halves: the RUNTIME function's returned-value rules (§15.85.4 r1–r7) over the derived CLDR/UCA collation
/// engine, and the BIND-time rules of the clause and of the reference (§12.3.7.3 SR9/SR10/SR11, §15.85.3 r4/r5/r6).
/// <para>⚖ Conformance statement, verbatim (owner decision Q4): "Implements collation behavior consistent with
/// ISO/IEC 14651 through derived tables and CLDR/UCA data."</para>
/// </summary>
public sealed class StandardCompareTests
{
    private const string Default = CollationEngine.DefaultOrderingTableName;   // "ISO 14651_2020_TABLE1"

    /// <summary>Run <paramref name="body"/> with EC-ORDER-NOT-SUPPORTED checking forced on, restoring it after —
    /// the flag is ambient run-unit state, so a leak would silently arm every later test in this class.</summary>
    private static void UnderChecking(Action body)
    {
        bool saved = ExceptionState.OrderNotSupportedChecking;
        ExceptionState.OrderNotSupportedChecking = true;
        try { body(); }
        finally { ExceptionState.OrderNotSupportedChecking = saved; }
    }

    // ── §15.85.4 r1 / r2 — the ordering LEVEL ────────────────────────────────────────────────────────────────

    /// <summary>§15.85.4 r5 — "Argument-1 and argument-2 are compared in accordance with the ordering table and
    /// ordering level being used". Level 1 is the base-letter level: case, accents and the level-4 punctuation
    /// weight are all invisible there.</summary>
    [Fact]
    public void Level1_ComparesBaseLettersOnly()
    {
        Assert.Equal("=", CobolIntrinsics.StandardCompare("a", "A", null, 1));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("a", "á", null, 1));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("A", "á", null, 1));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a", "b", null, 1));
        Assert.Equal(">", CobolIntrinsics.StandardCompare("b", "a", null, 1));
    }

    /// <summary>Level 2 adds the accents and nothing else — "a" = "A" still, "a" &lt; "á" now.</summary>
    [Fact]
    public void Level2_AddsAccents_NotCase()
    {
        Assert.Equal("=", CobolIntrinsics.StandardCompare("a", "A", null, 2));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a", "á", null, 2));
        Assert.Equal(">", CobolIntrinsics.StandardCompare("á", "a", null, 2));
    }

    /// <summary>Level 3 adds case: the CLDR root orders lowercase before uppercase at the tertiary level.</summary>
    [Fact]
    public void Level3_AddsCase()
    {
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a", "A", null, 3));
        Assert.Equal(">", CobolIntrinsics.StandardCompare("A", "a", null, 3));
    }

    /// <summary>
    /// Level 4 is the one the ISO/IEC 14651 default table exists for: variable elements (space, punctuation,
    /// symbols) are IGNORED through level 3 and weighted only at level 4 — UCA "shifted". So "a-b" and "ab" are
    /// EQUAL at levels 1–3 and differ at level 4.
    /// </summary>
    /// <remarks>⚠ This is the fact that would go green for the wrong reason under the CLDR/ICU default
    /// (non-ignorable), where the hyphen has a primary weight and the two strings differ at level 1. Pinning
    /// both directions is what proves the alternate handling is Shifted rather than merely that some comparison
    /// happened.</remarks>
    [Fact]
    public void Level4_WeighsPunctuation_WhichLevels1To3Ignore()
    {
        Assert.Equal("=", CobolIntrinsics.StandardCompare("a-b", "ab", null, 1));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("a-b", "ab", null, 3));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a-b", "ab", null, 4));
    }

    /// <summary>§15.85.4 r1 — "If argument-4 is unspecified, the highest level defined in the ordering table is
    /// used for the comparison." The renderer passes 0 for an omitted argument-4; the highest level of this
    /// table is 4, so the answer must equal the explicit level-4 answer and differ from level 3's.</summary>
    [Fact]
    public void OmittedLevel_IsTheHighestLevelTheTableDefines()
    {
        Assert.Equal(CobolIntrinsics.StandardCompare("a-b", "ab", null, 4),
                     CobolIntrinsics.StandardCompare("a-b", "ab", null, null));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a-b", "ab", null, null));
        Assert.NotEqual(CobolIntrinsics.StandardCompare("a-b", "ab", null, 3),
                        CobolIntrinsics.StandardCompare("a-b", "ab", null, null));
    }

    // ── §15.85.4 r4 / r6 / r7 — the operands and the result ──────────────────────────────────────────────────

    /// <summary>§15.85.4 r4: "For purposes of comparison, trailing spaces are truncated from the operands except
    /// that an operand consisting of all spaces is truncated to a single space." NOT a plain trim — the
    /// all-spaces operand becomes ONE space, so it is greater than a zero-length one, not equal to it.</summary>
    [Fact]
    public void TrailingSpaces_AreTruncated_AllSpacesToOne()
    {
        Assert.Equal("=", CobolIntrinsics.StandardCompare("abc", "abc   ", null, null));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("abc  ", "abc", null, null));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("     ", " ", null, null));
        Assert.Equal("=", CobolIntrinsics.StandardCompare("", "", null, null));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("", " ", null, null));   // "" stays ""; "   " becomes " "
        Assert.Equal("=", CobolIntrinsics.StandardCompare(null, "", null, null));
    }

    /// <summary>§15.85.4 r6 — the three returned values — and r7: "The length of the returned value is 1."</summary>
    [Fact]
    public void ReturnedValue_IsOneOfThreeCharacters_LengthOne()
    {
        foreach (string r in new[]
        {
            CobolIntrinsics.StandardCompare("a", "a", null, null),
            CobolIntrinsics.StandardCompare("a", "b", null, null),
            CobolIntrinsics.StandardCompare("b", "a", null, null),
            CobolIntrinsics.StandardCompare("resume", "Résumé", null, null),
        })
        {
            Assert.Equal(1, r.Length);
            Assert.Contains(r, new[] { "<", "=", ">" });
        }
        Assert.Equal("=", CobolIntrinsics.StandardCompare("abc", "abc", null, null));
        // Culturally sensitive, and "not necessarily a character-by-character comparison" (§15.85.4's NOTE):
        // ordinally 'R' (0x52) < 'r' (0x72), so a code-unit comparison would answer ">" here.
        Assert.Equal("<", CobolIntrinsics.StandardCompare("resume", "Résumé", null, null));
    }

    // ── §15.85.3 r5 / §12.3.7.4 GR17 — which ordering table ──────────────────────────────────────────────────

    /// <summary>§15.85.3 r5 names the default table 'ISO 14651_2020_TABLE1' and §12.3.7.4's NOTE 5 spells it
    /// 'ISO_14651_2020_TABLE1'. The standard therefore uses BOTH spellings for the same table, so an explicit
    /// ORDER TABLE naming either must behave exactly as the omitted-ordering-name form does.</summary>
    [Theory]
    [InlineData("ISO 14651_2020_TABLE1")]
    [InlineData("ISO_14651_2020_TABLE1")]
    [InlineData("iso 14651_2020_table1")]
    [InlineData("ISO_14651 2020 TABLE1")]
    public void TheDefaultTable_IsNamedInEverySpelling(string name)
    {
        foreach (int level in new[] { 0, 1, 2, 3, 4 })
            Assert.Equal(CobolIntrinsics.StandardCompare("a-b", "ab", null, level),
                         CobolIntrinsics.StandardCompare("a-b", "ab", name, level));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("a", "A", name, 3));
    }

    /// <summary>§12.3.7.4 GR17 — "The implementor specifies the allowable content of literal-9" — and WiseOwl COBOL's
    /// determination is that a CLDR locale tag naming a tailored collation is allowable. Spanish orders ñ as a
    /// letter of its own between n and o, so a name the root table orders BELOW "nz" is ordered ABOVE it under
    /// es-ES: the same two operands, two tables, two answers.</summary>
    [Fact]
    public void ALocaleTag_NamesItsTailoredOrderingTable()
    {
        Assert.Equal("<", CobolIntrinsics.StandardCompare("ñu", "nz", Default, null));
        Assert.Equal(">", CobolIntrinsics.StandardCompare("ñu", "nz", "es-ES", null));
        Assert.Equal("<", CobolIntrinsics.StandardCompare("ñu", "nz", null, null));
    }

    // ── §15.85.4 r2 — EC-ORDER-NOT-SUPPORTED ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// §15.85.4 r2: "If the cultural ordering table is not available on the processor, or the specified ordering
    /// level is not available, or the level number specified by argument-4 is not defined in the ordering table,
    /// the EC-ORDER-NOT-SUPPORTED exception condition is set to exist." Table 13 makes it FATAL, so with
    /// checking enabled it throws for the statement guard to dispatch to a USE declarative.
    /// </summary>
    [Theory]
    [InlineData("NO SUCH TABLE", null)]
    [InlineData("zz-Nowhere-42", null)]    // not a locale .NET knows and not the default name
    [InlineData(null, 5L)]                 // a level the table does not define
    [InlineData("ISO 14651_2020_TABLE1", 9L)]
    public void UnavailableTableOrLevel_RaisesEcOrderNotSupported(string? table, long? level)
    {
        RunUnit.Run(_ => UnderChecking(() =>
        {
            ExceptionState.Clear();
            var ex = Assert.Throws<CobolFatalException>(
                () => CobolIntrinsics.StandardCompare("a", "b", table, level));
            Assert.Equal("EC-ORDER-NOT-SUPPORTED", ex.EcName);
            Assert.Equal("EC-ORDER-NOT-SUPPORTED", ExceptionState.LastName);
            Assert.True(ExceptionState.LastFatal);
            Assert.Contains("15.85.4 r2", ex.Message);
        }));
    }

    /// <summary>kb/Work PB2631 - §15.85.3 r6: "Argument-4, if specified, shall be a positive nonzero integer". A
    /// SPECIFIED zero or negative level (a data item's run-time value) violates the ARGUMENT rule: EC-ARGUMENT-FUNCTION,
    /// and with checking off the one-character substituted result (row DOC-A.1-90) is one space. Level 0 used to mean
    /// "unspecified" and compared at level 4; a negative one raised EC-ORDER-NOT-SUPPORTED, which is r2's rule for a
    /// level the table does not define.</summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void ASpecifiedNonPositiveLevel_IsAnArgumentError_NotAnOmittedOrUnsupportedLevel(long level)
    {
        RunUnit.Run(_ =>
        {
            ExceptionState.Clear();
            Assert.Equal(" ", CobolIntrinsics.StandardCompare("a-b", "ab", null, level));
            Assert.Null(ExceptionState.LastName);                                  // checking off: nothing recorded
            bool saved = ExceptionState.ArgumentFunctionChecking;
            ExceptionState.ArgumentFunctionChecking = true;
            try
            {
                var ex = Assert.Throws<CobolFatalException>(() => CobolIntrinsics.StandardCompare("a-b", "ab", null, level));
                Assert.Equal("EC-ARGUMENT-FUNCTION", ex.EcName);
            }
            finally { ExceptionState.ArgumentFunctionChecking = saved; }
        });
    }

    /// <summary>
    /// ⛔ WITH CHECKING OFF, NOTHING IS RAISED AND NOTHING IS RECORDED. §14.6.13.1.1: "If no exception is
    /// detected during the execution of a statement or if checking for an exception that occurs is not enabled,
    /// no exception condition is raised" — so the last exception status stays clear. §14.6.13.1.3 #8 then leaves
    /// the outcome to the implementor, and WiseOwl COBOL's determination (CONFORMANCE.md §4 item 5) is to continue
    /// and return "=", a value §15.85.4 r6 defines and r7 sizes.
    /// </summary>
    [Fact]
    public void UnavailableTable_WithCheckingOff_ReturnsEqual_AndRecordsNothing()
    {
        RunUnit.Run(_ =>
        {
            bool saved = ExceptionState.OrderNotSupportedChecking;
            ExceptionState.OrderNotSupportedChecking = false;
            try
            {
                ExceptionState.Clear();
                Assert.Equal("=", CobolIntrinsics.StandardCompare("a", "b", "NO SUCH TABLE", null));
                Assert.Equal("=", CobolIntrinsics.StandardCompare("a", "b", null, 5));
                Assert.Null(ExceptionState.LastName);
            }
            finally { ExceptionState.OrderNotSupportedChecking = saved; }
        });
    }

    // ── The BIND side: the ORDER TABLE clause and the reference ──────────────────────────────────────────────

    private static (bool Ok, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) Compile(
        string specialNames, string body, int edition = 2002)
    {
        string dir = Path.Combine(Path.GetTempPath(), "cn_sc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "sc.cob");
            File.WriteAllText(src,
                "       IDENTIFICATION DIVISION.\n"
                + "       PROGRAM-ID. SCUNIT.\n"
                + (specialNames.Length == 0 ? "" :
                    "       ENVIRONMENT DIVISION.\n"
                    + "       CONFIGURATION SECTION.\n"
                    + "       SPECIAL-NAMES.\n" + specialNames)
                + "       DATA DIVISION.\n"
                + "       WORKING-STORAGE SECTION.\n"
                + "       01 R PIC X.\n"
                + "       01 LV PIC 9 VALUE 2.\n"
                + "       PROCEDURE DIVISION.\n"
                + "       MAIN.\n" + body
                + "           STOP RUN.\n");
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "sc.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Errors, r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private const string Ot1 = "           ORDER TABLE OT1 IS \"ISO 14651_2020_TABLE1\".\n";
    private const string Move = "           MOVE FUNCTION STANDARD-COMPARE(\"a\" \"b\") TO R.\n";

    /// <summary>The clause parses and binds, and the reference compiles in every written shape §15.85.2's two
    /// positional optionals admit: two arguments, three-with-an-ordering-name, three-with-a-level (as a literal
    /// AND as the integer DATA ITEM §15.3 type 6 admits), and four.</summary>
    [Fact]
    public void TheClauseAndEveryReferenceShape_Compile()
    {
        foreach (string call in new[]
        {
            "FUNCTION STANDARD-COMPARE(\"a\" \"b\")",
            "FUNCTION STANDARD-COMPARE(\"a\" \"b\" OT1)",
            "FUNCTION STANDARD-COMPARE(\"a\" \"b\" 2)",
            "FUNCTION STANDARD-COMPARE(\"a\" \"b\" LV)",       // §15.3 type 6 admits an integer DATA ITEM
            "FUNCTION STANDARD-COMPARE(\"a\" \"b\" OT1 4)",
        })
        {
            var (ok, errors, _) = Compile(Ot1, $"           MOVE {call} TO R.\n");
            Assert.True(ok, $"{call}: {string.Join("; ", errors)}");
        }
    }

    /// <summary>§12.3.7.4 GR17 leaves literal-9's allowable content to the implementor, so a spelling this
    /// implementation cannot resolve is LEGAL SOURCE — the compile succeeds — with the COBOLNET1662 advisory
    /// saying every reference will set EC-ORDER-NOT-SUPPORTED (§15.85.4 r2). Rejecting it would refuse a
    /// conforming program; saying nothing would ship a silently inoperative one.</summary>
    [Fact]
    public void AnUnresolvableLiteral9_IsAWarning_NotAnError()
    {
        var (ok, errors, warnings) = Compile(
            "           ORDER TABLE OT1 IS \"NO SUCH TABLE\".\n",
            "           MOVE FUNCTION STANDARD-COMPARE(\"a\" \"b\" OT1) TO R.\n");
        Assert.True(ok, string.Join("; ", errors));
        Assert.Contains(warnings, w => w.Contains("COBOLNET1662") && w.Contains("EC-ORDER-NOT-SUPPORTED"));
        // …and a resolvable one is silent.
        var (ok2, _, warnings2) = Compile(Ot1, Move);
        Assert.True(ok2);
        Assert.DoesNotContain(warnings2, w => w.Contains("COBOLNET1662"));
    }

    /// <summary>§12.3.7.3 SR10 — "Literal-4 and literal-9 shall be alphanumeric or national literals" — and
    /// SR11's zero-length half. A national literal IS admitted; a numeric or boolean one is not.</summary>
    [Theory]
    [InlineData("N\"ISO 14651_2020_TABLE1\"", true, "")]
    [InlineData("42", false, "SR10")]
    [InlineData("B\"01\"", false, "SR10")]
    [InlineData("\"\"", false, "SR11")]
    public void Literal9_ObeysSr10AndSr11(string literal, bool expectOk, string rule)
    {
        var (ok, errors, _) = Compile($"           ORDER TABLE OT1 IS {literal}.\n", Move);
        Assert.Equal(expectOk, ok);
        if (!expectOk)
            Assert.Contains(errors, e => e.Contains("COBOLNET0898") && e.Contains(rule));
    }

    /// <summary>The §12.3.7.2 general format brackets the ORDER TABLE clause WITHOUT the ellipsis its repeatable
    /// neighbours carry (CLASS, CURRENCY, LOCALE, the switch entry, SYMBOLIC CHARACTERS), so a second clause is a
    /// form violation — measured against the printed format, not assumed from the singular wording of GR17.</summary>
    [Fact]
    public void ASecondOrderTableClause_IsRejected()
    {
        var (ok, errors, _) = Compile(
            Ot1 + "           ORDER TABLE OT2 IS \"es-ES\".\n", Move);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET0898") && e.Contains("only once"));
    }

    /// <summary>§15.85.3 r5 / §15.3 argument type 12: with four arguments written the third position IS
    /// ordering-name-1 (§15.85.2's two optionals are positional), so a word naming no ORDER TABLE clause is
    /// COBOLNET1663 — and r6's "positive nonzero integer" is decidable for a literal level.</summary>
    [Theory]
    [InlineData("FUNCTION STANDARD-COMPARE(\"a\" \"b\" NOSUCH 2)", "ordering-name")]
    [InlineData("FUNCTION STANDARD-COMPARE(\"a\" \"b\" OT1 0)", "positive nonzero")]
    [InlineData("FUNCTION STANDARD-COMPARE(\"a\" \"b\" OT1 -1)", "positive nonzero")]
    public void BadOrderingNameOrLevel_IsCobolnet1663(string call, string fragment)
    {
        var (ok, errors, _) = Compile(Ot1, $"           MOVE {call} TO R.\n");
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1663") && e.Contains(fragment));
    }

    /// <summary>§15.85.3 r4 — "Neither argument-1 nor argument-2 shall be a zero-length literal" — reached
    /// through the ONE ordinal schema every other zero-length clause rides (kb/Work PB35), not a hand-written
    /// arm; and r1/r2's class screen rejects a numeric operand.</summary>
    [Theory]
    [InlineData("FUNCTION STANDARD-COMPARE(\"\" \"b\")", "COBOLNET1627")]
    [InlineData("FUNCTION STANDARD-COMPARE(\"a\" \"\")", "COBOLNET1627")]
    [InlineData("FUNCTION STANDARD-COMPARE(5 \"b\")", "COBOLNET1627")]
    public void Argument1And2_ObeyTheClassAndZeroLengthRules(string call, string code)
    {
        var (ok, errors, _) = Compile(Ot1, $"           MOVE {call} TO R.\n");
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains(code));
    }

    /// <summary>The D8 edition window is ENFORCED again now that the row's Bind is Runtime: below 2002 the
    /// function is rejected by name+edition, and the clause by the construct gate. The suppression that used to
    /// hide this was keyed on <c>Bind == Unsupported</c> (kb/Work PB27 §③), and lifting it was the forcing
    /// function for verifying the 2002 window (VERSION_CHANGE_REFERENCE row 7.19).</summary>
    [Fact]
    public void Below2002_TheFunctionAndTheClause_AreEditionGated()
    {
        var (ok, errors, _) = Compile("", Move, edition: 85);
        Assert.False(ok);
        Assert.Contains(errors, e => e.Contains("COBOLNET1502") && e.Contains("STANDARD-COMPARE"));
        // At 85 ORDER is not reserved, so `ORDER TABLE OT1 IS "…"` is an implementor-switch entry there and the
        // clause's own gate cannot fire — which is exactly why the grammar predicate is edition-gated. The
        // construct row's four-edition matrix case (order-table-2002) owns that half.
        var (ok2, _, _) = Compile("", Move, edition: 2002);
        Assert.True(ok2);
    }
}
