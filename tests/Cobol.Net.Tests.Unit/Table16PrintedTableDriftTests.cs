// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE ONE TABLE 16 AGREES WITH THE PRINTED ONE, CELL FOR CELL (kb/Work PB2076). ISO §14.9.25.3 SR10's Table 16 is
/// written once, <see cref="MoveValidity.Table16Refusal"/>, in the runtime, because both the compiler's MOVE screens and
/// the universal dispatch's §9.3.6 match rule 7 ask it. This test reads the table out of specs/ISO_COBOL.md and asks the
/// code every cell over the nine headings it models (the Type-name row and column are SR2's and GR4's, see
/// <see cref="Table16Category"/>), so an arm edited without the table — or a table re-transcribed without the arms — is
/// red here rather than a silent acceptance at run time. It also pins the compiler's ONE mapping of its operand axes to
/// the headings (<c>Table16Operand.Heading</c>).
/// </summary>
public sealed class Table16PrintedTableDriftTests
{
    private static readonly Dictionary<string, Table16Category> Rows = new(StringComparer.Ordinal)
    {
        ["Alphabetic|"] = Table16Category.Alphabetic,
        ["Alphanumeric|"] = Table16Category.Alphanumeric,
        ["Alphanumeric-edited|"] = Table16Category.AlphanumericEdited,
        ["Boolean|"] = Table16Category.Boolean,
        ["National|"] = Table16Category.National,
        ["National-edited|"] = Table16Category.NationalEdited,
        ["Numeric|Integer"] = Table16Category.NumericInteger,
        ["Numeric|Noninteger"] = Table16Category.NumericNoninteger,
        ["Numeric-edited|"] = Table16Category.NumericEdited,
    };

    /// <summary>The printed columns, in order, with the headings each pairs (Type-name has none modeled).</summary>
    private static readonly Table16Category[][] Columns =
    [
        [Table16Category.Alphabetic],
        [Table16Category.AlphanumericEdited, Table16Category.Alphanumeric],
        [Table16Category.Boolean],
        [Table16Category.National, Table16Category.NationalEdited],
        [Table16Category.NumericInteger, Table16Category.NumericNoninteger, Table16Category.NumericEdited],
        [],
    ];

    private static List<(Table16Category Row, string[] Cells)> PrintedRows()
    {
        var lines = File.ReadAllLines(TestRepo.Specs("ISO_COBOL.md"));
        int title = Array.FindIndex(lines, l => l.Contains("**Table 16 — Validity of types of MOVE statements**",
            StringComparison.Ordinal));
        Assert.True(title >= 0, "Table 16's title is no longer in specs/ISO_COBOL.md");
        var rows = new List<(Table16Category, string[])>();
        foreach (string line in lines.Skip(title + 1).SkipWhile(l => !l.StartsWith('|')).TakeWhile(l => l.StartsWith('|')))
        {
            string[] cells = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (Rows.TryGetValue(cells[0] + "|" + cells[1], out var row)) rows.Add((row, cells[2..]));
        }
        return rows;
    }

    [Fact]
    public void EveryModeledCell_IsThePrintedVerdict()
    {
        var rows = PrintedRows();
        Assert.Equal(Rows.Count, rows.Count);
        var wrong = new List<string>();
        foreach (var (row, cells) in rows)
        {
            Assert.Equal(Columns.Length, cells.Length);
            for (int c = 0; c < Columns.Length; c++)
                foreach (var receiver in Columns[c])
                {
                    bool printedYes = cells[c] == "Yes";
                    Assert.True(printedYes || cells[c] == "No", $"cell {row}/{receiver} reads '{cells[c]}'");
                    if ((MoveValidity.Table16Refusal(row, receiver) is null) != printedYes)
                        wrong.Add($"{row} -> {receiver}: printed {cells[c]}");
                }
        }
        Assert.True(wrong.Count == 0, "MoveValidity.Table16Refusal disagrees with the printed Table 16 (ISO §14.9.25.3 "
            + "SR10): " + string.Join("; ", wrong));
    }

    [Fact]
    public void NoHeading_IsAdmittedOnEitherSide()
    {
        foreach (var c in Enum.GetValues<Table16Category>())
        {
            Assert.Null(MoveValidity.Table16Refusal(Table16Category.None, c));
            Assert.Null(MoveValidity.Table16Refusal(c, Table16Category.None));
        }
    }

    /// <summary>The compiler's operand axes reach the heading the table prints for them.</summary>
    [Theory]
    [InlineData(PicCategory.Alphanumeric, true, false, false, Table16Category.Alphabetic)]
    [InlineData(PicCategory.Alphanumeric, true, true, false, Table16Category.Alphabetic)]
    [InlineData(PicCategory.Alphanumeric, false, true, false, Table16Category.AlphanumericEdited)]
    [InlineData(PicCategory.Alphanumeric, false, false, false, Table16Category.Alphanumeric)]
    [InlineData(PicCategory.National, false, true, false, Table16Category.NationalEdited)]
    [InlineData(PicCategory.National, false, false, false, Table16Category.National)]
    [InlineData(PicCategory.Numeric, false, false, true, Table16Category.NumericNoninteger)]
    [InlineData(PicCategory.Numeric, false, false, false, Table16Category.NumericInteger)]
    [InlineData(PicCategory.NumericEdited, false, true, false, Table16Category.NumericEdited)]
    [InlineData(PicCategory.Boolean, false, false, false, Table16Category.Boolean)]
    [InlineData(PicCategory.Group, false, false, false, Table16Category.None)]
    [InlineData(PicCategory.Pointer, false, false, false, Table16Category.None)]
    [InlineData(PicCategory.ObjectReference, false, false, false, Table16Category.None)]
    public void TheOperandAxes_MapToThePrintedHeading(PicCategory category, bool alphabetic, bool edited,
        bool nonInteger, Table16Category heading) =>
        Assert.Equal(heading, new Table16Operand(category, alphabetic, edited, nonInteger).Heading);
}
