// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P10 Step-2/3 substrate PINS (PHASE-10 exit criterion "national CharImage confirmed
/// one-UTF-16-char-per-position"): a NATIONAL leaf (ISO §13.18.60.4 GR8; the documented D-N1 implementor
/// choice — one .NET UTF-16 <c>char</c> per national position, §8.1.2 NOTE 2) and a BOOLEAN leaf
/// (§13.18.60.3 SR5/SR13b; D-B1 — one '0'/'1' char per boolean position) both ride
/// <see cref="StorageForm.CharImage"/> with <c>ImageWidth == PICTURE Length</c> — NEVER the legacy
/// 2-bytes-per-character doubling. The backing C# field for a <c>PIC N(n)</c>/<c>PIC 1(n)</c> item is a
/// string of exactly <c>Width</c> chars (the ValueInitializer/StrStore width discipline reads this same
/// fact), so pinning the computed <c>CharImage.Width</c> pins the runtime backing-string length.
/// </summary>
public sealed class NationalStorageFormTests
{
    /// <summary>Bind a WORKING-STORAGE fragment through the FULL pipeline (frontend → CSharpEmitter.Bind runs
    /// the StorageFormPass group tail) and return the unit's populated <see cref="DataBinder"/>.</summary>
    private static DataBinder BindWs(string ws)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SFPIN{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            {ws}
            PROCEDURE DIVISION.
            MAIN-PARA.
                STOP RUN.
            """;
        string path = Path.Combine(Path.GetTempPath(), "cn_natsf_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2002 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var emitter = new CSharpEmitter();
            var edition = new EditionContext(2002, permissive: false);
            var bound = emitter.Bind(tree!, edition, frontend.Directives);
            return bound.Units.Single().Data;
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>PIC N(5): ImageWidth == Length == 5 (one UTF-16 char per national position, D-N1) — expect 5,
    /// NOT the legacy byte-doubled 10 — and the computed form is CharImage(5, National).</summary>
    [Fact]
    public void National_PicN5_ImageWidthEqualsLength_OneCharPerPosition()
    {
        var d = BindWs("01 N-ITEM PIC N(5).");
        var item = d.ByName["N-ITEM"][0];
        Assert.Equal(PicCategory.National, item.Pic!.Category);
        Assert.Equal(5, item.Pic.Length);
        Assert.Equal(5, item.ImageWidth);                       // NOT 10 — never byte-doubled (D-N1)
        var ci = Assert.IsType<StorageForm.CharImage>(item.Storage);
        Assert.Equal(5, ci.Width);
        Assert.Equal(PicCategory.National, ci.Category);
    }

    /// <summary>USAGE NATIONAL with an implied-usage PIC N: the same CharImage form (§13.18.60.4 GR8 — usage
    /// national IS the national character form; SR13a lets PIC N imply it).</summary>
    [Fact]
    public void National_UsageNational_SameCharImageForm()
    {
        var d = BindWs("01 N-ITEM USAGE NATIONAL PIC N(3).");
        var item = d.ByName["N-ITEM"][0];
        Assert.Equal(3, item.ImageWidth);
        var ci = Assert.IsType<StorageForm.CharImage>(item.Storage);
        Assert.Equal(3, ci.Width);
        Assert.Equal(PicCategory.National, ci.Category);
    }

    /// <summary>⛔ THE TWO BOOLEAN USAGES OCCUPY DIFFERENT WIDTHS, and this test used to assert they did not.
    /// It read "D-B1 — one boolean character per position; §13.18.60.3 SR5/SR13b make DISPLAY and BIT the same
    /// string storage" and pinned <c>ImageWidth == 4</c> for <c>PIC 1(4) USAGE BIT</c>. **Neither cited rule says
    /// that**: SR13(b) implies USAGE DISPLAY only when NO usage clause is written, and SR5 merely requires a bit
    /// item to have a boolean picture. The rule that governs is <b>§13.18.60.4 GR5</b> — "the USAGE BIT clause
    /// specifies that bits SHALL be used" — so a bit item occupies <c>ceil(n/8)</c> character positions
    /// (design D19, fix-queue PB43). A GREEN test resting on a misreading is how the defect stayed invisible;
    /// see <c>feedback_green_test_can_hold_a_gap_open</c>.</summary>
    [Fact]
    public void Boolean_UsageBit_OccupiesCeilingOfEighth_DisplayFormStaysOnePerPosition()
    {
        var bit = BindWs("01 B-ITEM PIC 1(4) USAGE BIT.").ByName["B-ITEM"][0];
        Assert.Equal(PicCategory.Boolean, bit.Pic!.Category);
        Assert.Equal(Usage.Bit, bit.Pic.Usage);
        Assert.Equal(4, bit.Pic.Length);            // four BOOLEAN positions — what §15.50.4 r1 returns
        Assert.Equal(1, bit.ImageWidth);            // ceil(4/8) CHARACTER positions — what it OCCUPIES
        Assert.Equal(1, bit.ByteWidth);

        // The DISPLAY form is the other half of D-B1 and is REQUIRED to stay one character per position:
        // §13.18.60.3 SR13(b) implies USAGE DISPLAY, and §13.18.60.4 GR7 makes DISPLAY "an alphanumeric coded
        // character set". It is not merely licensed — it is the rule for this declaration.
        var disp = BindWs("01 D-ITEM PIC 1(4).").ByName["D-ITEM"][0];
        Assert.Equal(PicCategory.Boolean, disp.Pic!.Category);
        Assert.Equal(Usage.Display, disp.Pic.Usage);
        Assert.Equal(4, disp.ImageWidth);
        var ci = Assert.IsType<StorageForm.CharImage>(disp.Storage);
        Assert.Equal(4, ci.Width);
        Assert.Equal(PicCategory.Boolean, ci.Category);
    }

    /// <summary>§8.5.1.6.3 is a LAYOUT, not a sum — the case that separates them. Two 3-bit items SEPARATED by a
    /// character item are two runs, each taking its own byte ("aligned at the first bit position of the first
    /// available byte"), so the group is 3 positions. Summing bits and rounding once gives 14 bits → 2, which is
    /// the answer the fix-queue entry's stated shape would have produced.</summary>
    [Fact]
    public void BitRunsSeparatedByACharacterItem_EachTakeTheirOwnByte()
    {
        var d = BindWs("01 G.\n  05 C1 PIC 1(3) USAGE BIT.\n  05 C2 PIC X(1).\n  05 C3 PIC 1(3) USAGE BIT.");
        Assert.Equal(3, d.ByName["G"][0].ImageWidth);

        // …and two ADJACENT same-level bit items DO share a byte (the next-bit-position rule), so 5 + 3 = 1 byte.
        var shared = BindWs("01 H.\n  05 B1 PIC 1(5) USAGE BIT.\n  05 B2 PIC 1(3) USAGE BIT.");
        Assert.Equal(1, shared.ByName["H"][0].ImageWidth);
    }
}
