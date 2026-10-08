// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A WORD THAT STANDS FOR A LITERAL IS RESOLVED IN ONE PLACE (kb/Work PB1544). A constant-name ("as if literal-1 …
/// were written where constant-name-1 is written", ISO §13.10.4 GR1) and a symbolic-character ("defines a figurative
/// constant", §12.3.7.4 GR11 a)) both stand where a format specifies a literal, and every procedure-division position
/// asks <c>ExpressionBinder.LiteralAliasOf</c> for them. The chain used to be written separately in six binders, and the
/// positions that lacked a half refused conforming source: CALL Format 2's BY CONTENT and BY VALUE literal-2 and INVOKE
/// had no symbolic-character arm (COBOLNET1639 "not defined"), and BY CONTENT of an alphanumeric constant-name drew the
/// arithmetic refusal COBOLNET0844. So no procedure binder reads the constant table or the symbolic-character table
/// itself; the next literal position calls the one resolution and gets both kinds.
/// </summary>
public sealed class LiteralAliasResolutionDriftTests
{
    private static string Procedure(params string[] path) =>
        TestRepo.Src(["Cobol.Net.Compiler", "Binding", "Procedure", .. path]);

    [Fact]
    public void NoProcedureBinder_ReadsTheAliasTables_OutsideTheOneResolution()
    {
        string home = Procedure("ExpressionBinder.cs");
        var elsewhere = Directory.EnumerateFiles(Procedure(), "*.cs", SearchOption.AllDirectories)
            .Where(f => f != home && Regex.IsMatch(File.ReadAllText(f), @"\.(ConstantOf|SymbolicOf)\("))
            .Select(Path.GetFileName).ToList();
        Assert.True(elsewhere.Count == 0,
            "a procedure binder reads the constant-name or symbolic-character table itself (" + string.Join(", ", elsewhere)
            + "): ask ExpressionBinder.LiteralAliasOf, the one resolution of a literal-alias word (kb/Work PB1544)");

        string src = File.ReadAllText(home);
        // The resolution reads each table once. The one other read is FigurativeOperand's Format 7 `ALL symbolic-character-1`,
        // whose operand is a cobolWord the grammar already placed in a figurative constant, not a data reference.
        Assert.Single(Regex.Matches(src, @"\.ConstantOf\("));
        Assert.Equal(2, Regex.Matches(src, @"\.SymbolicOf\(").Count);
        int resolution = src.IndexOf("internal LiteralAlias? LiteralAliasOf(Core.DataReferenceContext dref) =>", StringComparison.Ordinal);
        Assert.True(resolution >= 0, "ExpressionBinder.LiteralAliasOf is gone");
        string body = src[resolution..src.IndexOf(';', resolution)];
        Assert.Contains("ctx.Data.ConstantOf(dref)", body);
        Assert.Contains("ctx.Data.SymbolicOf(dref)", body);
    }
}
