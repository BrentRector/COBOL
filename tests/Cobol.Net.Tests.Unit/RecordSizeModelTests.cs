// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE RECORD-SIZING MODEL, IN BYTES, READ BY EVERY CONSUMER (kb/Work PB1276, PB1277) — <see cref="FileModel"/>'s
/// <see cref="FileModel.RecordWidth"/> (the record area, ISO §13.18.43.4 GR2), <see cref="FileModel.VaryMin"/> /
/// <see cref="FileModel.VaryMax"/> (GR9 / GR10) and the per-record <c>MinRecordSize</c> / <c>MaxRecordSize</c>
/// (GR8 a / b) that §13.18.43.3 SR3 / SR4 screen against.
/// <para>Each case is one of the ways the model used to give a second answer: a Format 1 RECORD CONTAINS integer-1
/// that sized nothing (the descriptions sized the file), an explicit Format 2 clause without integer-3 whose maximum
/// was the character AREA while the implied clause's was the descriptions' maximum, sizes counted in CHARACTER
/// positions where GR3 counts "bytes ... regardless of the types of characters used" (a national position is two
/// bytes, D-N1), and a minimum that re-summed each USAGE BIT leaf as a byte of its own where GR4 counts only "the
/// entire byte in which that data item ends". Every expected value is computed from those rules, never read off a
/// run.</para>
/// </summary>
public sealed class RecordSizeModelTests
{
    /// <summary>§13.18.43.4 GR6 — "Integer-1 specifies the number of bytes contained in each record in the
    /// file" — on an FD and an SD alike; §13.18.43.3 SR3 permits the smaller description.</summary>
    [Theory]
    [InlineData("FD")]
    [InlineData("SD")]
    public void RecordContains_SizesTheFileAboveItsDescriptions(string entry)
    {
        var file = Single($"       {entry}  F1 RECORD CONTAINS 20 CHARACTERS.\r\n       01  R1 PIC X(10).");
        Assert.Equal(20, file.RecordWidth);
        Assert.False(file.RecordSizeVaries);
    }

    /// <summary>§13.18.43.4 GR10 — the unstated maximum is "the greatest number of bytes described for a record
    /// in that file", with a dynamic-length member at its maximum size (GR8 b): 3 + 20 = 23 — for the explicit
    /// clause without integer-3 AND for the implied Format 2 clause (D-FRA (iv)); GR9's minimum is the member at
    /// zero length, 3.</summary>
    [Theory]
    [InlineData(" RECORD IS VARYING IN SIZE")]
    [InlineData("")]
    public void UnstatedMaximum_IsTheSameGr10QuantityForTheExplicitAndTheImpliedClause(string clause)
    {
        var file = Single($"       FD  F1{clause}.\r\n       01  R1.\r\n           05 A PIC X(3).\r\n"
            + "           05 D PIC X DYNAMIC LENGTH LIMIT 20.");
        Assert.True(file.RecordSizeVaries);
        Assert.Equal(23, file.VaryMax);
        Assert.Equal(3, file.VaryMin);
    }

    /// <summary>GR3 / GR8 / GR9 / GR10 in BYTES: an unstated range over a record is its own byte size, both ends.
    /// A national position is two bytes (D-N1); two <c>PIC 1(3) USAGE BIT</c> items share one byte (GR4, the
    /// §8.5.1.6.3 walk); an occurs-depending table counts at its minimum occurrences for the minimum (GR8 a) and its
    /// maximum for the maximum (GR8 b) — for a national element and for a bit element, whose occurrences pack.</summary>
    [Theory]
    [InlineData("       01  R1 PIC N(10).", 20, 20)]
    [InlineData("       01  R1.\r\n           05 A PIC X(10).\r\n           05 C PIC N(5).", 20, 20)]
    [InlineData("       01  R1.\r\n           05 B1 PIC 1(3) USAGE BIT.\r\n           05 B2 PIC 1(3) USAGE BIT.", 1, 1)]
    [InlineData("       01  R1.\r\n           05 A PIC X(2).\r\n"
        + "           05 T PIC N(2) OCCURS 1 TO 3 TIMES DEPENDING ON K.", 6, 14)]
    [InlineData("       01  R1.\r\n           05 A PIC X(1).\r\n"
        + "           05 T PIC 1(3) USAGE BIT OCCURS 1 TO 4 TIMES DEPENDING ON K.", 2, 3)]
    public void UnstatedRange_IsTheRecordsByteSize(string record, int min, int max)
    {
        var file = Single("       FD  F1 RECORD IS VARYING IN SIZE.\r\n" + record);
        Assert.Equal(min, file.VaryMin);
        Assert.Equal(max, file.VaryMax);
    }

    /// <summary>Binds <paramref name="fileSection"/> and returns its one file. ⛔ The source must bind CLEAN, binder
    /// errors included: a size measured on source the compile refuses is not a record size (kb/Work PB1604 — e.g.
    /// a dynamic-capacity table under an FD record, which COBOLNET1526 refuses per §8.5.1.9.1 3)).</summary>
    private static FileModel Single(string fileSection)
    {
        string src = "       IDENTIFICATION DIVISION.\r\n"
            + "       PROGRAM-ID. RECSIZE.\r\n"
            + "       ENVIRONMENT DIVISION.\r\n"
            + "       INPUT-OUTPUT SECTION.\r\n"
            + "       FILE-CONTROL.\r\n"
            + "           SELECT F1 ASSIGN TO \"recsize.dat\".\r\n"
            + "       DATA DIVISION.\r\n"
            + "       FILE SECTION.\r\n"
            + fileSection + "\r\n"
            + "       WORKING-STORAGE SECTION.\r\n"
            + "       01  K PIC 9.\r\n"
            + "       PROCEDURE DIVISION.\r\n"
            + "       MAIN-PARA.\r\n"
            + "           STOP RUN.\r\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_recsize_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend() { InitialFormat = InitialReferenceFormat.Auto }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var data = new DataBinder();
            data.Bind(program);
            Assert.False(data.Edition.HasErrors, string.Join("\n", data.Edition.Diagnostics));
            return Assert.Single(data.Files);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
