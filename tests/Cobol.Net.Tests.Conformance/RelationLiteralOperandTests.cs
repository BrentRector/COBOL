// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §8.8.4.2.1 — "A relation condition shall contain at least one reference to an operand that is not a literal"
/// (kb/Work PB1470). The sentence is neither a general format nor a syntax rule, so §4.2.2's warning mechanism
/// indicates it: COBOLNET3146, a WARNING at every edition (docs/CONFORMANCE.md D-RELLITERAL), the relation compiling and
/// evaluating as written (<c>conformance:85/pb1470_relation_of_two_literals_runs</c> pins the values).
/// <para>The EXPECTED verdict is computed here from the rule over each operand kind's one property — is it a literal as
/// the source wrote it? A figurative constant is one (§8.3.3.6 is a clause of §8.3.3 Literals), a symbolic-character is
/// "a user-defined figurative constant" (§8.3.2.2.29), and a constant-name's effect "is as if literal-1 … were written
/// where constant-name-1 is written" (§13.10.4 GR1). A data item, an arithmetic expression and a function-identifier
/// (folded at bind time or not) are not. Adding an operand kind is one row of <see cref="Kinds"/>; every pair it forms
/// is then checked automatically.</para>
/// </summary>
public sealed class RelationLiteralOperandTests
{
    /// <param name="Name">The kind's key (used in the program-id and the failure message).</param>
    /// <param name="Operand">The operand as written in the relation.</param>
    /// <param name="Literal">A literal as the source wrote it (the rule's one question).</param>
    private sealed record Kind(string Name, string Operand, bool Literal);

    private static readonly Kind[] Kinds =
    [
        new("LI", "1", Literal: true),                         // numeric literal
        new("LA", "\"A\"", Literal: true),                     // alphanumeric literal
        new("LX", "X\"41\"", Literal: true),                   // hexadecimal alphanumeric literal (§8.3.3.2 Format 2)
        new("SP", "SPACE", Literal: true),                     // figurative constant
        new("ZR", "ZERO", Literal: true),
        new("HV", "HIGH-VALUE", Literal: true),
        new("AL", "ALL \"A\"", Literal: true),                 // ALL literal (§8.3.3.6.2 Format 6)
        new("SB", "SB", Literal: true),                        // symbolic-character (§8.3.2.2.29)
        new("KT", "KT", Literal: true),                        // constant-name of an alphanumeric literal (§13.10.4 GR1)
        new("KN", "KN", Literal: true),                        // constant-name of a numeric literal
        new("XA", "XA", Literal: false),                       // alphanumeric data item
        new("ND", "ND", Literal: false),                       // numeric data item
        new("EX", "ND + 1", Literal: false),                   // arithmetic expression of a data item
        new("EL", "1 + 1", Literal: false),                    // arithmetic expression of literals: an expression
        new("FL", "FUNCTION LENGTH(XA)", Literal: false),      // a function the binder folds (BoundNumericLiteral.FunctionValue)
        new("FU", "FUNCTION UPPER-CASE(XA)", Literal: false),  // a function it does not fold
    ];

    private const string Head = """
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   SYMBOLIC CHARACTERS SB IS 67.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 KT CONSTANT AS "B".
               01 KN CONSTANT AS 7.
               01 XA PIC X(4) VALUE "ABCD".
               01 ND PIC 9(4) VALUE 12.
               01 T1.
                  05 E1 PIC X OCCURS 3 INDEXED BY I1.
               PROCEDURE DIVISION.
               MAIN-PARA.

        """;

    public static TheoryData<string> Subjects() => [.. Kinds.Select(k => k.Name)];

    /// <summary>One program per SUBJECT kind, one relation per line against every object kind; the COBOLNET3146 warnings
    /// by line are the pairs' verdicts — exactly one where both operands are literals, none otherwise.</summary>
    [Theory]
    [MemberData(nameof(Subjects))]
    public void EveryPair_WarnsExactlyWhenBothOperandsAreLiterals(string subject)
    {
        var l = Kinds.Single(k => k.Name == subject);
        var sb = new StringBuilder($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RLIT{subject}.\n{Head}");
        int line = sb.ToString().Split('\n').Length;          // the 1-based line the first relation lands on
        var lineOf = new Dictionary<int, Kind>();
        foreach (var r in Kinds)
        {
            sb.Append($"           IF {l.Operand} = {r.Operand} CONTINUE END-IF\n");
            lineOf[line++] = r;
        }
        sb.Append("           STOP RUN.\n");

        var warned = WarningLines(CheckOnly(sb.ToString()).Warnings);
        var wrong = new List<string>();
        foreach (var (ln, r) in lineOf)
        {
            int want = l.Literal && r.Literal ? 1 : 0;
            int got = warned.GetValueOrDefault(ln);
            if (got != want) wrong.Add($"{l.Operand} = {r.Operand}: expected {want} COBOLNET3146, got {got}");
        }
        Assert.True(wrong.Count == 0 && warned.Keys.All(lineOf.ContainsKey),
            "ISO §8.8.4.2.1 verdicts differ:\n  " + string.Join("\n  ", wrong));
    }

    /// <summary>⛔ EVERY SURFACE THAT WRITES A RELATION CONDITION asks the rule (<c>ConditionBinder.WrittenRelational</c>):
    /// PERFORM UNTIL, SEARCH WHEN, an abbreviated relation's inserted subject (§8.8.4.12 — <c>1 = ND OR 2</c> holds the
    /// relation <c>1 = 2</c>), and an EVALUATE partial-expression, which §14.9.13.3 SR8 treats "as though it were
    /// specified as condition-2". The complement: an EVALUATE subject/object pair and a range are governed by
    /// §14.9.13.3 SR10's Table 15, which permits a range-expression against a literal subject, and an abbreviated
    /// relation whose inserted subject is a data item compares a data item.</summary>
    [Theory]
    [InlineData("RLITS1", "PERFORM UNTIL 1 = 1 CONTINUE END-PERFORM", 1)]
    [InlineData("RLITS2", "SET I1 TO 1 SEARCH E1 AT END CONTINUE WHEN \"A\" = SPACE CONTINUE END-SEARCH", 1)]
    [InlineData("RLITS3", "IF 1 = ND OR 2 CONTINUE END-IF", 1)]
    [InlineData("RLITS4", "EVALUATE 1 WHEN > 0 CONTINUE END-EVALUATE", 1)]
    [InlineData("RLITS5", "IF ND = 1 AND KN = 7 CONTINUE END-IF", 1)]
    [InlineData("RLITS6", "IF ND = 1 OR 2 CONTINUE END-IF", 0)]
    [InlineData("RLITS7", "EVALUATE 3 WHEN 1 THRU 5 CONTINUE END-EVALUATE", 0)]
    [InlineData("RLITS8", "EVALUATE ND WHEN 1 CONTINUE WHEN > 2 CONTINUE END-EVALUATE", 0)]
    public void EveryWrittenRelationSurface_AsksTheRule(string pid, string statement, int warnings)
    {
        var (errors, found) = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n{Head}"
            + $"           {statement}.\n           STOP RUN.\n");
        Assert.True(errors.Count == 0, string.Join(" | ", errors));
        Assert.Equal(warnings, WarningLines(found).Values.Sum());
    }

    /// <summary>A WARNING at every edition, never an error: the program compiles strict and the relation is evaluated
    /// as written (§4.2.2's warning mechanism; D-RELLITERAL).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void IsAWarningAtEveryEdition(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. RLITED{edition}.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       IF ZERO = ZERO CONTINUE END-IF
                       STOP RUN.
            """, edition);
        Assert.True(ok, string.Join(" | ", errors));
        Assert.Single(warnings, w => w.Contains("warning COBOLNET3146", StringComparison.Ordinal)
            && w.Contains("§8.8.4.2.1", StringComparison.Ordinal));
    }

    private static Dictionary<int, int> WarningLines(IEnumerable<string> warnings) =>
        warnings.Select(w => Regex.Match(w, @"\((\d+),\d+\): warning COBOLNET3146"))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value))
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>Parse + bind only (no Roslyn emit): the rule is settled where the relation is bound.</summary>
    private static (IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) CheckOnly(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Rl_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "prog.cob"), source);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"),
                DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")], r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
