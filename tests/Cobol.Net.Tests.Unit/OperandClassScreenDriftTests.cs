// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Binding.Procedure;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT GUARD FOR THE ONE OPERAND-CLASS SCREEN (kb/Work PB210 · PB211 · PB212).
///
/// <para><b>Why it exists.</b> GO TO … DEPENDING (§14.9.17.3 SR1), SEARCH … VARYING (§14.9.37.3 SR5) and the SET
/// Format-1 receiver (§14.9.39.3 SR1) each close an identifier position to a set of classes, and each binder bound
/// the position with a bare resolve and asked nothing — the SAME absence, found three times by three sweeps, and
/// the PERFORM VARYING induction variable (§14.9.28.3 SR2) was a fourth. <see cref="OperandPositions"/> makes a
/// class-closed position a ROW; these facts keep "one row and one call" true for the next one.</para>
///
/// <para><b>What is pinned.</b> (1) <see cref="OperandPositions.All"/> holds every declared row. (2) Every row is
/// ASKED by some binder — a row nothing calls is a rule written down and enforced nowhere. (3) Every row is named
/// in DESIGN-binder-bound-tree.md §3.6's entry-point table. (4) END TO END, for every row and every class shape,
/// the row's diagnostic is drawn exactly when the shape's SPEC-DERIVED classes (written below, independently of
/// <see cref="OperandClassScreen.ClassesOf"/>) are disjoint from what the row admits — so a new row is exercised
/// against every shape by adding its template, and a template-less row fails (5).</para>
/// </summary>
public sealed class OperandClassScreenDriftTests
{
    /// <summary>The class shapes, each with the classes ISO assigns it: §8.5.2.1 Table 2 for the class, §5.5
    /// 2)b)2 for "integer" (fixed-point, scale zero). A numeric-edited item is class ALPHANUMERIC in Table 2; a
    /// group is alphanumeric; a floating-point item is numeric and never an integer data item.</summary>
    private static readonly (string Name, string Decl, OperandClasses Classes)[] Shapes =
    [
        ("integer",        "01 SLOT PIC 9(2).",            OperandClasses.IntegerItem | OperandClasses.NumericElementaryItem),
        ("scaled numeric", "01 SLOT PIC S9(2)V9.",         OperandClasses.NumericElementaryItem),
        ("floating-point", "01 SLOT USAGE FLOAT-LONG.",    OperandClasses.NumericElementaryItem),
        ("alphanumeric",   "01 SLOT PIC X(2).",            OperandClasses.None),
        ("numeric-edited", "01 SLOT PIC Z9.",              OperandClasses.None),
        ("group",          "01 SLOT. 05 SLOT-A PIC 9(2).", OperandClasses.None),
        ("index data item","01 SLOT USAGE INDEX.",         OperandClasses.IndexDataItem),
    ];

    /// <summary>One PROCEDURE DIVISION body per row, with <c>SLOT</c> in the row's position and every OTHER operand
    /// legal, so the row's code is the only one the shape can draw from it.</summary>
    private static readonly Dictionary<OperandPosition, string> Templates = new()
    {
        [OperandPositions.GoToDependingSelector] =
            "           GO TO P1 P2 DEPENDING ON SLOT.\n       P1.\n           STOP RUN.\n       P2.\n           STOP RUN.\n",
        [OperandPositions.SearchVaryingIdentifier] =
            "           SEARCH E VARYING SLOT AT END CONTINUE WHEN E(IDX) = \"A\" CONTINUE END-SEARCH.\n           STOP RUN.\n",
        [OperandPositions.SetIndexAssignmentReceiver] =
            "           SET SLOT TO IDX.\n           STOP RUN.\n",
        [OperandPositions.PerformVaryingIdentifier] =
            "           PERFORM VARYING SLOT FROM 1 BY 1 UNTIL F = 1 CONTINUE END-PERFORM.\n           STOP RUN.\n",
        [OperandPositions.PerformVaryingIdentifierFromIndex] =
            "           PERFORM VARYING SLOT FROM IDX BY 1 UNTIL F = 1 CONTINUE END-PERFORM.\n           STOP RUN.\n",
        [OperandPositions.WriteAdvancingIdentifier] =
            "           OPEN OUTPUT OCSFILE.\n           WRITE OCSREC AFTER ADVANCING SLOT LINES.\n           CLOSE OCSFILE.\n           STOP RUN.\n",
    };

    /// <summary>The ONE file every template may name (a WRITE row needs a file to write): a sequential print file
    /// <c>OCSFILE</c> with record <c>OCSREC</c> (PF is a reserved word).
    /// Declared for every template so the source prelude is one shape.</summary>
    private const string FileControl =
        "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT OCSFILE ASSIGN TO \"ocs.txt\" ORGANIZATION SEQUENTIAL.\n";
    private const string FileSection = "       FILE SECTION.\n       FD OCSFILE.\n       01 OCSREC PIC X(5).\n";

    [Fact]
    public void All_HoldsEveryDeclaredRow()
    {
        var declared = typeof(OperandPositions).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OperandPosition))
            .Select(f => (OperandPosition)f.GetValue(null)!)
            .ToHashSet();
        Assert.Equal(declared.Count, OperandPositions.All.Count);
        Assert.True(declared.SetEquals(OperandPositions.All), "OperandPositions.All is missing a declared row");
    }

    [Fact]
    public void EveryRow_IsAskedByABinder()
    {
        string src = TestRepo.At("src/Cobol.Net.Compiler");
        var text = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith("OperandClassScreen.cs", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToList();
        foreach (var name in RowNames())
            Assert.True(text.Any(t => t.Contains($"OperandPositions.{name}", StringComparison.Ordinal)),
                $"OperandPositions.{name} is declared and no binder asks it — the rule is written down and enforced nowhere");
    }

    [Fact]
    public void EveryRow_IsNamedInTheDesignDocEntryPointTable()
    {
        string doc = File.ReadAllText(TestRepo.At("docs/rearchitecture/DESIGN-binder-bound-tree.md"));
        foreach (var name in RowNames())
            Assert.True(doc.Contains($"OperandPositions.{name}", StringComparison.Ordinal),
                $"DESIGN-binder-bound-tree.md §3.6 does not name OperandPositions.{name}");
    }

    [Fact]
    public void EveryRow_HasATemplate()
    {
        foreach (var row in OperandPositions.All)
            Assert.True(Templates.ContainsKey(row), $"{row.Statement} {row.Operand} ({row.Rule}) has no end-to-end template");
    }

    public static IEnumerable<object[]> RowsAndShapes() =>
        from i in Enumerable.Range(0, OperandPositions.All.Count)
        from j in Enumerable.Range(0, Shapes.Length)
        select new object[] { i, j };

    [Theory]
    [MemberData(nameof(RowsAndShapes))]
    public void EveryRow_RefusesExactlyTheShapesItsRuleExcludes(int row, int shape)
    {
        var pos = OperandPositions.All[row];
        var (name, decl, classes) = Shapes[shape];
        if (!Templates.TryGetValue(pos, out var body)) return;   // EveryRow_HasATemplate reports it
        string source =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. OCS" + row + "X" + shape + ".\n" + FileControl +
            "       DATA DIVISION.\n" + FileSection + "       WORKING-STORAGE SECTION.\n" +
            "       01 T.\n          05 E OCCURS 3 INDEXED BY IDX PIC X.\n" +
            "       01 F PIC 9 VALUE 1.\n" +
            "       " + decl + "\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n" + body;
        var errors = CompileErrors(source);
        bool refused = errors.Any(e => e.Contains(pos.Diagnostic.Code, StringComparison.Ordinal));
        bool excluded = (classes & pos.Admits) == 0;
        Assert.True(refused == excluded,
            $"{pos.Statement} {pos.Operand} ({pos.Rule}) over a {name} item: expected "
            + (excluded ? $"{pos.Diagnostic.Code}" : "no " + pos.Diagnostic.Code)
            + $"; errors were: {string.Join(" | ", errors)}");
    }

    /// <summary>The FUNCTION-IDENTIFIER shapes (kb/Work PB1421), each with the classes of the temporary item it
    /// references (§8.4.3.2.4 GR1): an integer function's item is an integer numeric elementary item, a numeric
    /// function's a numeric one that §8.4.3.2.3 SR11 keeps out of every integer position, an alphanumeric
    /// function's neither.</summary>
    private static readonly (string Text, OperandClasses Classes)[] FunctionShapes =
    [
        ("FUNCTION INTEGER(2.5)",     OperandClasses.IntegerItem | OperandClasses.NumericElementaryItem),
        ("FUNCTION LENGTH(F)",        OperandClasses.IntegerItem | OperandClasses.NumericElementaryItem),
        ("FUNCTION SQRT(4)",          OperandClasses.NumericElementaryItem),
        ("FUNCTION UPPER-CASE(\"A\")", OperandClasses.None),
    ];

    /// <summary>The SENDING rows — the identifier positions whose grammar slot admits a function-identifier
    /// (§8.4.3.2.3 SR1 keeps one out of every receiving position, and those slots do not parse one).</summary>
    private static readonly OperandPosition[] SendingRows =
        [OperandPositions.GoToDependingSelector, OperandPositions.WriteAdvancingIdentifier];

    public static IEnumerable<object[]> SendingRowsAndFunctionShapes() =>
        from i in Enumerable.Range(0, SendingRows.Length)
        from j in Enumerable.Range(0, FunctionShapes.Length)
        select new object[] { i, j };

    /// <summary>A function-identifier in a sending row is classed by its RESULT, end to end: refused exactly when
    /// its temporary item's classes miss what the row admits, and never with a parse error (kb/Work PB1421 — the
    /// GO TO DEPENDING and WRITE ADVANCING slots admitted no function-identifier at all).</summary>
    [Theory]
    [MemberData(nameof(SendingRowsAndFunctionShapes))]
    public void SendingRow_ClassesAFunctionIdentifierByItsResult(int row, int shape)
    {
        var pos = SendingRows[row];
        var (text, classes) = FunctionShapes[shape];
        string source =
            "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. OCF" + row + "X" + shape + ".\n" + FileControl +
            "       DATA DIVISION.\n" + FileSection + "       WORKING-STORAGE SECTION.\n" +
            "       01 F PIC 9 VALUE 1.\n" +
            "       PROCEDURE DIVISION.\n       MAIN.\n" + Templates[pos].Replace("SLOT", text, StringComparison.Ordinal);
        var errors = CompileErrors(source);
        bool refused = errors.Any(e => e.Contains(pos.Diagnostic.Code, StringComparison.Ordinal));
        bool excluded = (classes & pos.Admits) == 0;
        Assert.True(refused == excluded && errors.Count == (excluded ? 1 : 0),
            $"{pos.Statement} {pos.Operand} ({pos.Rule}) over {text}: expected "
            + (excluded ? $"exactly {pos.Diagnostic.Code}" : "no error")
            + $"; errors were: {string.Join(" | ", errors)}");
    }

    private static IEnumerable<string> RowNames() =>
        typeof(OperandPositions).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(OperandPosition))
            .Select(f => f.Name);

    private static IReadOnlyList<string> CompileErrors(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Ocs_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "ocs.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "ocs.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
