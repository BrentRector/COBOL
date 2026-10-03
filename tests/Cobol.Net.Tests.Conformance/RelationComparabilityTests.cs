// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE OPERAND-CLASS PAIR MATRIX for ISO §8.8.4.2.1's closed "Comparisons are defined for the following:" list
/// over the GENERAL relation (kb/Work PB1468). Every ordered pair of the operand kinds below is written as a relation
/// and its verdict is compared with the list as the standard writes it — item 1 (two numeric), items 2/3/5/7 (the
/// character classes), item 6 with §8.8.4.2.5 ("The numeric integer operand shall be an integer literal or an integer
/// numeric data item of usage display or national"), item 8 as §8.8.4.2.13 states it ("Relation tests may be made
/// only between" two index-names; an index-name and a numeric data item or numeric literal; an index data item and
/// an index-name or another index data item).
/// <para>The EXPECTED verdict is computed here from the spec's wording over each kind's declared properties, never
/// read from the compiler's table, so a change to <c>RelationComparability</c> that drifts from the list fails one
/// cell. Adding an operand kind is one row of <see cref="Kinds"/>; every pair it forms is then checked
/// automatically. A pair of two literals / figuratives is skipped: §8.8.4.2.1's "A relation condition shall contain
/// at least one reference to an operand that is not a literal" is a different rule (kb/Work PB1470).</para>
/// </summary>
public sealed class RelationComparabilityTests
{
    private enum Cls { Character, Numeric, Index, Zero }

    /// <param name="Name">The kind's key (used in the program-id and the failure message).</param>
    /// <param name="Operand">The operand as written in the relation.</param>
    /// <param name="Cls">Its §8.5.2.1 class (Zero = the figurative ZERO, whose class the context picks, §8.3.3.6.4 GR4).</param>
    /// <param name="NumericInteger">§8.8.4.2.5's admitted numeric operand: an integer literal or an integer numeric
    /// data item of usage display or national (or an INTEGER function — docs/CONFORMANCE.md D-RELCLASS).</param>
    /// <param name="Literal">A literal or figurative constant (a literal-to-literal pair is PB1470's rule, skipped).</param>
    /// <param name="IndexName">An index-name (§8.8.4.2.13 rows 1 and 2), as opposed to an index data item.</param>
    /// <param name="NumericItemOrLiteral">§8.8.4.2.13 row 2's partner: "a numeric data item or numeric literal".</param>
    private sealed record Kind(string Name, string Operand, Cls Cls, bool NumericInteger = false, bool Literal = false,
        bool IndexName = false, bool NumericItemOrLiteral = false);

    private static readonly Kind[] Kinds =
    [
        new("XA", "XA", Cls.Character),                       // alphanumeric
        new("XL", "XL", Cls.Character),                       // alphabetic (§8.8.4.2.1: treated as alphanumeric)
        new("XE", "XE", Cls.Character),                       // numeric-edited (Table 2: class alphanumeric)
        new("XN", "XN", Cls.Character),                       // national
        new("ND", "ND", Cls.Numeric, NumericInteger: true, NumericItemOrLiteral: true),   // PIC 9(4) DISPLAY
        new("NS", "NS", Cls.Numeric, NumericInteger: true, NumericItemOrLiteral: true),   // PIC S9(4) DISPLAY
        new("NU", "NU", Cls.Numeric, NumericInteger: true, NumericItemOrLiteral: true),   // PIC 9(4) NATIONAL
        new("NF", "NF", Cls.Numeric, NumericItemOrLiteral: true),                         // PIC 9(3)V9 — not integer
        new("NB", "NB", Cls.Numeric, NumericItemOrLiteral: true),                         // COMP
        new("NP", "NP", Cls.Numeric, NumericItemOrLiteral: true),                         // COMP-3
        new("NL", "NL", Cls.Numeric, NumericItemOrLiteral: true),                         // FLOAT-LONG
        new("LI", "12", Cls.Numeric, NumericInteger: true, Literal: true, NumericItemOrLiteral: true),
        new("LD", "1.5", Cls.Numeric, Literal: true, NumericItemOrLiteral: true),
        new("LA", "\"12\"", Cls.Character, Literal: true),
        new("SP", "SPACE", Cls.Character, Literal: true),
        new("ZR", "ZERO", Cls.Zero, Literal: true),
        new("EX", "ND + 1", Cls.Numeric),                                                 // arithmetic expression
        // A function-identifier references "the unique data item that results from the evaluation of a function"
        // (§8.4.3.2.1), numeric by §15.2 items 4 and 5, so it is row 2's "numeric data item" (kb/Work PB1662).
        new("FI", "FUNCTION INTEGER(NF)", Cls.Numeric, NumericInteger: true, NumericItemOrLiteral: true),   // INTEGER function (§15.2 item 5)
        new("FN", "FUNCTION SQRT(ND)", Cls.Numeric, NumericItemOrLiteral: true),                           // NUMERIC function (§15.2 item 4)
        new("FL", "FUNCTION LENGTH(XA)", Cls.Numeric, NumericInteger: true, NumericItemOrLiteral: true),   // an INTEGER function the binder FOLDS (kb/Work PB1662)
        new("IX", "I1", Cls.Index, IndexName: true),
        new("IY", "I2", Cls.Index, IndexName: true),
        new("ID", "IDA", Cls.Index),
    ];

    private const string Data = DataBody + "\n";

    private const string DataBody = """
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 T1.
                  05 E1 PIC X(4) OCCURS 5 INDEXED BY I1 I2.
               01 XA PIC X(4) VALUE "0012".
               01 XL PIC A(4) VALUE "ABCD".
               01 XE PIC ZZ9 VALUE 12.
               01 XN PIC N(4) VALUE N"0012".
               01 ND PIC 9(4) VALUE 12.
               01 NS PIC S9(4) VALUE 12.
               01 NU PIC 9(4) USAGE NATIONAL VALUE 12.
               01 NF PIC 9(3)V9 VALUE 1.2.
               01 NB PIC 9(4) COMP VALUE 12.
               01 NP PIC 9(4) COMP-3 VALUE 12.
               01 NL USAGE FLOAT-LONG VALUE 12.
               01 IDA USAGE INDEX.
               PROCEDURE DIVISION.
               MAIN-PARA.
        """;

    /// <summary>The verdict §8.8.4.2.1 gives the pair: null = defined, else the diagnostic code of the rule it breaks.</summary>
    private static string? Expected(Kind l, Kind r)
    {
        if (l.Cls == Cls.Index || r.Cls == Cls.Index)
        {
            if (l.Cls == Cls.Index && r.Cls == Cls.Index) return null;                     // rows 1 and 3
            var (ix, other) = l.Cls == Cls.Index ? (l, r) : (r, l);
            // Row 2 — "an index-name and a numeric data item or numeric literal"; ZERO is the numeric value 0 there.
            return ix.IndexName && (other.NumericItemOrLiteral || other.Cls == Cls.Zero) ? null : "COBOLNET2533";
        }
        if (l.Cls == Cls.Zero || r.Cls == Cls.Zero || l.Cls == r.Cls) return null;        // items 1, 2, 3, 5, 7
        var num = l.Cls == Cls.Numeric ? l : r;                                            // item 6 + §8.8.4.2.5
        return num.NumericInteger ? null : "COBOLNET2532";
    }

    public static TheoryData<string> Subjects() => [.. Kinds.Select(k => k.Name)];

    /// <summary>One program per SUBJECT kind, one relation per line against every object kind; each line's
    /// diagnostics (by line number) are the pair's verdict.</summary>
    [Theory]
    [MemberData(nameof(Subjects))]
    public void EveryPair_MatchesTheComparisonsDefinedList(string subject)
    {
        var l = Kinds.Single(k => k.Name == subject);
        var sb = new StringBuilder();
        sb.Append($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RCMP{subject}.\n").Append(Data);
        int firstLine = sb.ToString().Split('\n').Length;          // the 1-based line the first relation lands on
        var lineOf = new Dictionary<int, Kind>();
        int line = firstLine;
        foreach (var r in Kinds)
        {
            if (l.Literal && r.Literal) continue;
            sb.Append($"           IF {l.Operand} = {r.Operand} CONTINUE END-IF\n");
            lineOf[line++] = r;
        }
        sb.Append("           STOP RUN.\n");

        var errors = CheckOnly(sb.ToString(), 2023);
        var codesByLine = errors
            .Select(e => Regex.Match(e, @"\((\d+),\d+\): error (COBOLNET\d{4})"))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value), m => m.Groups[2].Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        Assert.True(codesByLine.Keys.All(lineOf.ContainsKey) && errors.Count == codesByLine.Values.Sum(v => v.Count),
            $"subject {subject}: a diagnostic landed off the relation lines — {string.Join(" | ", errors)}");

        var wrong = new List<string>();
        foreach (var (ln, r) in lineOf)
        {
            string? want = Expected(l, r);
            var got = codesByLine.GetValueOrDefault(ln) ?? [];
            if (want is null ? got.Count != 0 : got is not [var only] || only != want)
                wrong.Add($"{l.Operand} = {r.Operand}: expected {want ?? "no diagnostic"}, got "
                    + (got.Count == 0 ? "none" : string.Join(",", got)));
        }
        Assert.True(wrong.Count == 0, $"ISO §8.8.4.2.1 / §8.8.4.2.5 / §8.8.4.2.13 verdicts differ:\n  "
            + string.Join("\n  ", wrong));
    }

    /// <summary>⛔ ONE CHECKPOINT: the same breaches at every surface that lowers to a relation (§14.9.13.3 SR7 a) — the
    /// selection objects "shall be valid operands for comparison to the corresponding operand in the set of selection
    /// subjects in accordance with 8.8.4.2").</summary>
    [Theory]
    [InlineData("RCMPS1", "EVALUATE NB WHEN XA CONTINUE END-EVALUATE", "COBOLNET2532")]
    [InlineData("RCMPS2", "EVALUATE XA WHEN NF THRU ND CONTINUE END-EVALUATE", "COBOLNET2532")]
    [InlineData("RCMPS3", "PERFORM UNTIL I1 = XA CONTINUE END-PERFORM", "COBOLNET2533")]
    [InlineData("RCMPS4", "SET I1 TO 1 SEARCH E1 AT END CONTINUE WHEN IDA = 3 CONTINUE END-SEARCH", "COBOLNET2533")]
    [InlineData("RCMPS5", "IF NB = ND AND = XA CONTINUE END-IF", "COBOLNET2532")]
    [InlineData("RCMPS6", "EVALUATE I1 WHEN \"3\" CONTINUE END-EVALUATE", "COBOLNET2533")]
    public void EverySurface_ReportsTheBreach(string pid, string statement, string code)
    {
        var errors = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n{Data}           {statement}.\n"
            + "           STOP RUN.\n", 2023);
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>⛔ THE SIBLING ARM (kb/Work PB1468): a THROUGH range with an IN alphabet-name lowers to
    /// <c>BoundRangeMembership</c>, not the inclusive relation pair, and that node used to be built outside the
    /// checkpoint — so the same breach compiled clean once the phrase was written. §14.9.13.4 GR4 a) 5. makes it
    /// "selection-subject &gt;= left-part AND selection-subject &lt;= right-part" either way.</summary>
    [Theory]
    [InlineData("RCMPR1", "NB", "\"0001\"", "\"0020\"", "COBOLNET2532")]
    [InlineData("RCMPR2", "I1", "\"1\"", "\"5\"", "COBOLNET2533")]
    public void RangeMembershipArm_ReportsTheBreach(string pid, string subject, string lo, string hi, string code)
    {
        var errors = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n"
            + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
            + "           ALPHABET AL IS NATIVE.\n" + Data
            + $"           EVALUATE {subject} WHEN {lo} THRU {hi} IN AL CONTINUE END-EVALUATE.\n"
            + "           STOP RUN.\n", 2023);
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>⛔ THE WRITTEN OPERAND IS JUDGED, NOT THE IMPLEMENTOR'S INTERMEDIATE. A subject read by more than one
    /// WHEN is materialized (§14.9.13.4 GR3), and an index-name subject's intermediate is a numeric item — so the
    /// table, asked of the intermediate, refused these legal §8.8.4.2.13 pairs (rows 1, 2 and 3) as numeric-vs-index.
    /// The complement: the same subject against an alphanumeric object is still refused.</summary>
    [Fact]
    public void EvaluateSubjectIntermediate_IsScreenedAsWritten()
    {
        var legal = CheckOnly("       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RCMPE1.\n" + Data
            + "           EVALUATE I1 WHEN I2 CONTINUE WHEN IDA CONTINUE WHEN 3 CONTINUE WHEN > NB CONTINUE\n"
            + "                       WHEN 1 THRU 4 CONTINUE END-EVALUATE.\n           STOP RUN.\n", 2023);
        Assert.True(legal.Count == 0, string.Join(" | ", legal));
        var bad = CheckOnly("       IDENTIFICATION DIVISION.\n       PROGRAM-ID. RCMPE2.\n" + Data
            + "           EVALUATE I1 WHEN I2 CONTINUE WHEN XA CONTINUE END-EVALUATE.\n           STOP RUN.\n", 2023);
        EditionHarness.AssertHasDiagnostic(bad, "COBOLNET2533");
    }

    /// <summary>§8.8.4.2.5's refusal is the DA6 disposition — an error, with the pair kept under --permissive as a
    /// WARNING and evaluated as the character comparison it always was (IDA-free, so it runs).</summary>
    [Fact]
    public void NumericNotInteger_IsAWarningUnderPermissive_AndRuns()
    {
        string src = """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. RCMPPERM.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 NB PIC 9(4) COMP VALUE 12.
                   01 XA PIC X(4) VALUE "0012".
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       IF NB = XA DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
                       STOP RUN.
            """;
        var (ok, errors, warnings) = EditionHarness.CompileFull(src, 2023, permissive: true);
        Assert.True(ok, string.Join(" | ", errors));
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET2532");
        var (strictOk, strictErrors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(strictOk);
        EditionHarness.AssertHasDiagnostic(strictErrors, "COBOLNET2532");
    }

    /// <summary>kb/Work PB1427 at the same checkpoint: NULL is class pointer (§8.4.3.10.1) and §8.4.3.10.3 SR1 a)
    /// admits it only "in a pointer-or-object-reference relation condition" — opposite a character or numeric operand
    /// it no longer borrows that operand's class (COBOLNET0869); and a boolean operand is compatible only with an
    /// operand that may BE boolean (§8.3.3.6.4 GR4: ZERO may; SPACE may not — COBOLNET0844).</summary>
    [Theory]
    [InlineData("RCMPN1", "IF XA = NULL CONTINUE END-IF", "COBOLNET0869")]
    [InlineData("RCMPN2", "IF ND = NULL CONTINUE END-IF", "COBOLNET0869")]
    [InlineData("RCMPN3", "EVALUATE XA WHEN NULL CONTINUE END-EVALUATE", "COBOLNET0869")]
    [InlineData("RCMPN4", "IF B1 = SPACE CONTINUE END-IF", "COBOLNET0844")]
    [InlineData("RCMPN5", "IF B1 = IDA CONTINUE END-IF", "COBOLNET0844")]
    public void NullAndBoolean_OutsideTheirRelation_Rejected(string pid, string statement, string code)
    {
        var errors = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n"
            + Data.Replace("PROCEDURE DIVISION.", "01 B1 PIC 1(4) USAGE BIT VALUE B\"1010\".\n       01 PP USAGE POINTER.\n       PROCEDURE DIVISION.")
            + $"           {statement}.\n           STOP RUN.\n", 2023);
        EditionHarness.AssertHasDiagnostic(errors, code);
    }

    /// <summary>The complement of the theory above (feedback_measure_the_selectors_complement): the relations
    /// NULL and a boolean operand ARE defined in still compile.</summary>
    [Theory]
    [InlineData("RCMPC1", "IF PP = NULL CONTINUE END-IF")]
    [InlineData("RCMPC2", "IF NULL NOT = PP CONTINUE END-IF")]
    [InlineData("RCMPC3", "IF B1 = ZERO CONTINUE END-IF")]
    [InlineData("RCMPC4", "IF B1 = B\"1010\" CONTINUE END-IF")]
    public void NullAndBoolean_InTheirRelation_Compile(string pid, string statement)
    {
        var errors = CheckOnly($"       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {pid}.\n"
            + Data.Replace("PROCEDURE DIVISION.", "01 B1 PIC 1(4) USAGE BIT VALUE B\"1010\".\n       01 PP USAGE POINTER.\n       PROCEDURE DIVISION.")
            + $"           {statement}.\n           STOP RUN.\n", 2023);
        Assert.True(errors.Count == 0, string.Join(" | ", errors));
    }

    /// <summary>Parse + bind only (no Roslyn emit): every verdict here is settled at the relation checkpoint.</summary>
    private static IReadOnlyList<string> CheckOnly(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Rc_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = CompiledProgramCache.StageSource(Path.Combine(dir, "prog.cob"), source);
            var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"),
                DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return r.Success ? [] : [.. r.Errors.DefaultIfEmpty($"status {r.Status}")];
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
