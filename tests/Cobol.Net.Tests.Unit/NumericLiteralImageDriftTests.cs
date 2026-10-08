// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A NUMERIC LITERAL THE PROGRAM WROTE KEEPS THE PROGRAM'S DECIMAL SEPARATOR — ONE PRODUCER (kb/Work PB1643).
/// ISO §14.9.11.4 GR1 leaves the conversion of a literal to the device to the implementor (CONFORMANCE.md DOC-A.1-56:
/// the literal as written), and §12.3.7.4 GR14 a) makes the comma the character "written in numeric literals to represent
/// the decimal separator" under DECIMAL-POINT IS COMMA. The bound literal therefore carries two spellings: <c>Text</c>,
/// the canonical dot-decimal value every numeric decoder reads, and <c>DecimalSeparator</c>, from which its character image
/// is built. A producer that builds the node from <c>CheckLiteral</c>'s text and forgets the separator prints
/// <c>DISPLAY 1,5</c> as <c>1.5</c>; so the two producers (the operand and the expression-tier literal) are the only
/// callers of <c>CheckLiteral</c>, and each sets the separator.
/// </summary>
public sealed class NumericLiteralImageDriftTests
{
    private static string Binding(params string[] path) => TestRepo.Src(["Cobol.Net.Compiler", "Binding", .. path]);

    [Fact]
    public void CheckLiteral_IsCalledOnlyBy_TheTwoProducersThatCarryTheSeparator()
    {
        string home = Binding("Procedure", "ExpressionBinder.cs");
        var elsewhere = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => f != home && Regex.IsMatch(File.ReadAllText(f), @"\bCheckLiteral\("))
            .Select(Path.GetFileName).ToList();
        Assert.True(elsewhere.Count == 0,
            "CheckLiteral is called outside ExpressionBinder (" + string.Join(", ", elsewhere) + "): a numeric literal the program wrote "
            + "is built by ExpressionBinder.NumericLiteralOperand / NumericLiteralExpr, which also carry the decimal separator "
            + "its character image needs (kb/Work PB1643)");

        string src = File.ReadAllText(home);
        var calls = Regex.Matches(src, @"\bCheckLiteral\(");
        // the definition, the two producers, and nothing else
        Assert.True(calls.Count == 3, $"ExpressionBinder calls CheckLiteral in {calls.Count - 1} places; the producers are the only two");
        foreach (string producer in new[] { "NumericLiteralOperand(string asWritten) =>", "NumericLiteralExpr(string asWritten) =>" })
        {
            int at = src.IndexOf(producer, StringComparison.Ordinal);
            Assert.True(at >= 0, $"{producer} is gone");
            Assert.Contains("new(CheckLiteral(asWritten)) { DecimalSeparator = ctx.Data.DecimalSeparator }",
                src.Substring(at, Math.Min(200, src.Length - at)));
        }
    }

    [Fact]
    public void TheLiteralImage_IsBuiltFromTheSeparator_AndTheDisplayArmReadsIt()
    {
        string tree = File.ReadAllText(Binding("Bound", "BoundTree.cs"));
        Assert.Contains("Text.Replace('.', DecimalSeparator)", tree);
        string text = File.ReadAllText(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "Emit", "OperandText.cs"));
        Assert.Contains("Visit(BoundNumericLiteral n) => EmitText.CsLiteral(deSign ? DeSign(n.Image) : n.Image)", text);
    }
}
