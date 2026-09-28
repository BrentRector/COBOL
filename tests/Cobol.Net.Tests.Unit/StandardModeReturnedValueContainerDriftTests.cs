// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Under a STANDARD arithmetic mode a numeric function's returned value is contained in an SDIDI in EVERY
/// reference context — and the set of functions that has to hold for is read out of the CATALOG, not out of a
/// list somebody remembered to update.
/// </summary>
/// <remarks>
/// <para><b>The rule.</b> ISO §15.4.1: "When standard-decimal arithmetic or standard-binary arithmetic is in
/// effect, the returned value for numeric and integer functions is contained in a temporary standard data item
/// in the intermediate form defined for the arithmetic mode in effect" — unconditional on whether the function
/// has an equivalent arithmetic expression and on the SHAPE of whatever consumes the value. §8.8.1.5.1 supplies
/// the half that is easy to elide: standard-decimal arithmetic is "a method of evaluating an arithmetic
/// expression, an arithmetic statement, the SUM clause, and certain integer and numeric functions as specified
/// in 15.4.1" — the FUNCTION is evaluated under the mode, not merely its use as an operand. §15.4.1's last
/// paragraph exempts the VALUE (an implementor-defined approximation for a function with no equivalent
/// arithmetic expression), never the CONTAINER.</para>
///
/// <para><b>Why this test exists (kb/Work PB253).</b> <c>IntrinsicRenderer.RenderFloat</c> tested the RECEIVER
/// SHAPE before the arithmetic mode, so under <c>ARITHMETIC IS STANDARD-DECIMAL</c> the SDIDI arm was
/// unreachable for every receiver-less or float-receiver reference and a raw binary64 escaped into the item-92
/// text channel, the MOVE-source channel and the float-receiver channel. Measured: <c>DISPLAY FUNCTION
/// SIN(1E-20)</c> printed <c>1E-20</c> (binary64 E-notation) where the SDIDI's text is
/// <c>0.00000000000000000001</c>, and <c>MOVE FUNCTION TAN(a)</c> / <c>COMPUTE r = FUNCTION TAN(a)</c> delivered
/// 16331239353195368.96 and 16331239353195370.00 for the SAME call — two returned values for one function and
/// one argument in a single run, which §15.4.1 forbids by name.</para>
///
/// <para><b>What makes it a drift test rather than three more goldens.</b> The goldens exercise a handful of
/// functions; this asserts the property over EVERY <c>Float: true</c> catalog row, and the fixture table is
/// checked against the rows the catalog source actually declares. Add a float-family function — or flip an
/// existing row's <c>Float</c> flag — and this fails until a fixture covers it, which is the difference between
/// "the functions we thought of are contained" and "every one is".</para>
///
/// <para><b>The probe is the GENERATED C#, because the property is about the CARRIER, not about digits.</b> An
/// SDIDI-carried returned value never reaches <c>CobolFloat.Display</c> (the binary64 item-92 text) and never
/// reaches <c>CobolFloat.ToScaled</c>/<c>ToScaledUnchecked</c> (the binary64 → fixed-point landing); it renders
/// through <c>CobolDec</c>. <c>CobolFloat.ScaledToDouble</c> — the ARGUMENT's conversion into the binary64 body
/// this family computes in — is expected and is not matched by either probe.</para>
/// </remarks>
public sealed class StandardModeReturnedValueContainerDriftTests : CobolNetTestBase
{
    /// <summary>The catalog's <c>Float: true</c> column, read from the source so the population cannot drift
    /// away from the table. The ninth positional argument of <c>Add(new(...))</c> is <c>Float</c>.</summary>
    private static readonly Regex FloatRow = new(
        "Add\\(new\\(\"(?<n>[A-Z0-9-]+)\",\\s*IntrinsicType\\.\\w+,\\s*IntrinsicArity\\.\\w+,\\s*[-\\w]+,\\s*[-\\w]+,"
        + "\\s*\"[^\"]*\",\\s*\"\\w+\",\\s*IntrinsicBind\\.\\w+,\\s*(?<float>true|false),",
        RegexOptions.Compiled);

    /// <summary>One legal, domain-valid function reference per <c>Float: true</c> catalog row — the argument
    /// choices come from each function's own §15.x.3 argument rules (ACOS/ASIN need |argument-1| ≤ 1, LOG/LOG10
    /// need argument-1 &gt; 0, ANNUITY needs a non-negative rate and a positive integer period, RANDOM's seeded
    /// form is an integer). The KEY is the catalog's function name — that is what ties this table to the sweep.</summary>
    private static readonly Dictionary<string, string> Fixtures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ACOS"] = "FUNCTION ACOS(1)",
        ["ASIN"] = "FUNCTION ASIN(0)",
        ["ATAN"] = "FUNCTION ATAN(1)",
        ["COS"] = "FUNCTION COS(0)",
        ["SIN"] = "FUNCTION SIN(0)",
        ["TAN"] = "FUNCTION TAN(1)",
        ["SQRT"] = "FUNCTION SQRT(2)",
        ["LOG"] = "FUNCTION LOG(2)",
        ["LOG10"] = "FUNCTION LOG10(2)",
        ["ANNUITY"] = "FUNCTION ANNUITY(0.05 10)",
        ["PRESENT-VALUE"] = "FUNCTION PRESENT-VALUE(0.05 100 200)",
        ["RANDOM"] = "FUNCTION RANDOM(7)",
        ["STANDARD-DEVIATION"] = "FUNCTION STANDARD-DEVIATION(1 2 3)",
        ["VARIANCE"] = "FUNCTION VARIANCE(1 2 3)",
        ["E"] = "FUNCTION E",
        ["PI"] = "FUNCTION PI",
        ["EXP"] = "FUNCTION EXP(1)",
        ["EXP10"] = "FUNCTION EXP10(2)",
    };

    private static List<string> CatalogFloatRows()
    {
        string src = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "Binding", "IntrinsicCatalog.cs"));
        var all = FloatRow.Matches(src).ToList();
        Assert.True(all.Count >= 79, $"only {all.Count} catalog rows parsed — the Add(new(...)) shape changed; fix the regex, do not lower the floor.");
        return all.Where(m => m.Groups["float"].Value == "true").Select(m => m.Groups["n"].Value).ToList();
    }

    [Fact]
    public void EveryFloatFamilyRow_HasAFixture()
    {
        var rows = CatalogFloatRows();
        Assert.True(rows.Count >= 18, $"only {rows.Count} Float: true catalog rows found — the §15.4.1 float family shrank unexpectedly; check the regex before lowering this.");
        var missing = rows.Where(n => !Fixtures.ContainsKey(n)).ToList();
        var stale = Fixtures.Keys.Where(n => !rows.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        Assert.True(missing.Count == 0,
            "a new §15.4.1 float-family function has no container fixture — add one (a legal, domain-valid "
            + "reference) so its standard-mode returned value is proved to be SDIDI-carried:\n  " + string.Join("\n  ", missing));
        Assert.True(stale.Count == 0,
            "a fixture names a function the catalog no longer marks Float: true — remove it or fix the row:\n  "
            + string.Join("\n  ", stale));
    }

    [Fact]
    public void UnderStandardDecimal_TheReturnedValueNeverLeavesTheSdidiCarrier()
    {
        var offenders = new List<string>();
        int i = 0;
        foreach (string name in CatalogFloatRows().OrderBy(n => n, StringComparer.Ordinal))
        {
            string reference = Fixtures[name];
            string programId = "SDCONT" + (++i).ToString("00");
            string generated = CompileUnderStandardDecimal(programId, reference);
            // The three channels the receiver-shape-first order used to pre-empt: the item-92 TEXT channel, the
            // MOVE-source channel, and the arithmetic-receiver channel (which was already correct and is the
            // control). None of them may carry the returned value as a binary64.
            if (generated.Contains("CobolFloat.Display(", StringComparison.Ordinal))
                offenders.Add($"{name}: the item-92 text channel renders the returned value through CobolFloat.Display — a binary64, not the SDIDI §15.4.1 requires");
            if (generated.Contains("CobolFloat.ToScaled", StringComparison.Ordinal))
                offenders.Add($"{name}: the MOVE-source channel lands the returned value through CobolFloat.ToScaled — a binary64, not the SDIDI §15.4.1 requires");
        }
        Assert.True(offenders.Count == 0,
            "ISO §15.4.1 — under ARITHMETIC IS STANDARD-DECIMAL the returned value of a numeric function is "
            + "contained in a temporary standard data item in the intermediate form of the mode in effect, in "
            + "EVERY reference context. These render it as a raw binary64 instead (kb/Work PB253: the arithmetic "
            + "mode must be tested BEFORE the receiver shape):\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>Compile one STANDARD-DECIMAL program that references <paramref name="reference"/> in the text
    /// channel, the MOVE-source channel and the arithmetic-receiver channel, and return its generated C#.</summary>
    private string CompileUnderStandardDecimal(string programId, string reference)
    {
        string source = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {programId}.
                   OPTIONS.
                       ARITHMETIC IS STANDARD-DECIMAL.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 R PIC S9(18)V9(6).
                   PROCEDURE DIVISION.
                   MAIN.
                       DISPLAY {reference}
                       MOVE {reference} TO R
                       COMPUTE R = {reference}
                       STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, programId + ".cob");
        File.WriteAllText(srcPath, source);
        var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(result.Success, $"[{programId}] {reference} must compile: {string.Join("\n", result.Errors)}");
        Assert.NotNull(result.GeneratedCsPath);
        return File.ReadAllText(result.GeneratedCsPath!);
    }
}
