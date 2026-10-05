// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ <b>A REJECTED DECLARATION IS DIAGNOSED ONCE</b> (kb/Work PB960). When a PICTURE fails ISO §13.18.40.3's
/// syntax rules the binder substitutes a RECOVERY profile so binding can continue; its <c>Category</c> is a storage
/// placeholder (alphanumeric), not an analysis of anything the program wrote. Every §8.5.2.1 class/category
/// reader a syntax-rule screen consults now reads <c>PicInfo.AnalyzedCategory</c>, which is null for a recovery
/// profile, so the screens fail open instead of telling the user their <c>PIC 9V9V9</c> item is "of category
/// alphanumeric".
/// <para>⭐ THE ASSERTION IS AN ABSENCE, and it is a DRIFT TEST over operand CONTEXTS: each row puts the rejected
/// item in one more operand position, and the ONLY diagnostic allowed is the declaration's own. A new screen that
/// reads the storage category instead of the analyzed one turns its row red.</para>
/// </summary>
public sealed class RecoveryCategoryCascadeTests
{
    private static string Prog(string stmt) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB960CC.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 W-BAD PIC 9V9V9.
               01 W-N   PIC 9(4) VALUE 0.
               01 W-E   PIC ZZ9.
               01 W-X   PIC X(4).
               01 W-B   PIC 1(4).
               01 W-TG.
                  05 W-T PIC X OCCURS 3 INDEXED BY IX.
               PROCEDURE DIVISION.
               MAIN.
                   {stmt}
                   STOP RUN.
        """;

    [Theory]
    [InlineData("ADD W-BAD TO W-N")]                                  // §8.8.1.1 sending operand
    [InlineData("COMPUTE W-N = W-BAD * 2")]                           // §8.8.1.1 in an expression
    [InlineData("ADD 1 TO W-BAD")]                                    // the arithmetic resultant screen
    [InlineData("COMPUTE W-BAD = 1")]
    [InlineData("DIVIDE W-N BY 2 GIVING W-N REMAINDER W-BAD")]
    [InlineData("MOVE W-T (W-BAD) TO W-X")]                           // a subscript (§8.4.2.3.2)
    [InlineData("MOVE FUNCTION SQRT (W-BAD) TO W-N")]                 // a §15.3 class-numeric argument
    [InlineData("SET IX TO W-BAD")]
    [InlineData("IF W-BAD = W-B DISPLAY \"Y\" END-IF")]               // §8.8.4.2.2 boolean relation
    [InlineData("IF W-BAD > 3 DISPLAY \"Y\" END-IF")]
    [InlineData("MOVE W-BAD TO W-E")]
    [InlineData("MOVE W-B TO W-BAD")]
    [InlineData("EVALUATE W-BAD WHEN 1 THRU 2 CONTINUE END-EVALUATE")]
    [InlineData("STRING W-BAD DELIMITED BY SIZE INTO W-X")]
    [InlineData("INSPECT W-BAD TALLYING W-N FOR ALL \"1\"")]
    public void RejectedPicture_IsDiagnosedOnce_AtItsDeclaration(string stmt)
    {
        var diags = EditionHarness.GetDiagnostics(Prog(stmt), 2023);
        Assert.Contains(diags, d => d.Contains("COBOLNET1934"));
        Assert.All(diags, d => Assert.Contains("W-BAD", d));
        Assert.Single(diags);
    }
}
