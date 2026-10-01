using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolSharp.Tests.Unit.Preprocessor;

/// <summary>
/// Mid-file <c>&gt;&gt;SOURCE FORMAT</c> switching (ISO/IEC 1989:2023 §7.3.24.3 GR1): each directive partitions the
/// source into a homogeneous-format SEGMENT; the directive line is discarded (§6.5 logical conversion, step 1) and
/// the new format governs the FOLLOWING segment. <c>FORMAT</c> and <c>IS</c> are optional words (§7.3.24.2).
/// </summary>
public class SourceFormatTests
{
    private static string Norm(string src) =>
        ReferenceFormatProcessor.NormalizeToFreeForm(src, gates: null, sourcePath: "t.cob");

    [Fact] // A FIXED segment then a FREE segment: the fixed lines are column-stripped, the free lines pass through.
    public void MidFile_FixedThenFree()
    {
        string src =
            "000100 IDENTIFICATION DIVISION.\n" +
            "000200 PROGRAM-ID. X.\n" +
            "000300 >>SOURCE FORMAT IS FREE\n" +       // §7.3.3 SR3: in fixed form, in the program-text area
            "PROCEDURE DIVISION.\n" +
            "MAIN. DISPLAY \"HI\".\n";
        string free = Norm(src);
        Assert.Contains("IDENTIFICATION DIVISION.", free);   // fixed segment: sequence area stripped
        Assert.DoesNotContain("000100", free);
        Assert.Contains("PROCEDURE DIVISION.", free);        // free segment: as-is (would be truncated if mis-read as fixed)
        Assert.Contains("MAIN. DISPLAY \"HI\".", free);
        Assert.DoesNotContain(">>SOURCE", free);             // the directive line is consumed
    }

    [Fact] // A FREE segment then a FIXED segment.
    public void MidFile_FreeThenFixed()
    {
        string src =
            ">>SOURCE FORMAT IS FREE\n" +
            "IDENTIFICATION DIVISION.\n" +
            "PROGRAM-ID. Y.\n" +
            ">>SOURCE FORMAT IS FIXED\n" +
            "000400 PROCEDURE DIVISION.\n" +
            "000500 MAIN. STOP RUN.\n";
        string free = Norm(src);
        Assert.Contains("IDENTIFICATION DIVISION.", free);   // free segment (would keep its column-1 text)
        Assert.Contains("PROCEDURE DIVISION.", free);        // fixed segment: sequence area stripped
        Assert.DoesNotContain("000400", free);
        Assert.DoesNotContain(">>SOURCE", free);
    }

    [Fact] // §7.3.24.2 — FORMAT and IS are optional words: >>SOURCE FREE is a valid directive.
    public void FormatAndIsWordsOptional()
    {
        string free = Norm(">>SOURCE FREE\nIDENTIFICATION DIVISION.\n");
        Assert.Contains("IDENTIFICATION DIVISION.", free);
        Assert.DoesNotContain(">>SOURCE", free);
    }

    [Fact] // The directive line is left as a BLANK line (not removed), so line slots stay aligned for a file with
           // no continuation joins.
    public void DirectiveLine_BecomesBlank_PreservesLineCount()
    {
        // The text starts in fixed form (§7.3.24.3 2)), so the mid-file directive is in the program-text area (§7.3.3 SR3).
        string src = "       IDENTIFICATION DIVISION.\n       >>SOURCE FORMAT IS FREE\nPROGRAM-ID. Z.\n";
        string free = Norm(src);
        Assert.Equal(src.Split('\n').Length, free.Split('\n').Length);   // no continuation joins ⇒ line count preserved
        Assert.Equal("", free.Split('\n')[1].Trim());                    // the discarded directive occupies a blank slot
    }

    [Fact] // Adversarial-review C2b — a BLANK source line terminating a FIXED segment must survive the switch
           // (SplitLines drops only the ConvertFixedToFree artifact newline, not blank lines), else the following
           // segment misaligns and a line is lost.
    public void FixedSegment_TrailingBlankLine_Preserved()
    {
        string src =
            "000100 DISPLAY \"HI\".\n" +
            "\n" +                              // blank source line terminating the fixed segment
            "000300 >>SOURCE FORMAT IS FREE\n" +       // §7.3.3 SR3: in fixed form, in the program-text area
            "DISPLAY \"BYE\".\n";
        string free = Norm(src);
        Assert.Equal(src.Split('\n').Length, free.Split('\n').Length);   // no line lost
        Assert.Contains("DISPLAY \"HI\".", free);
        Assert.Contains("DISPLAY \"BYE\".", free);
    }

    [Fact] // Adversarial-review C2b — §6.3 margin R: a fixed-form directive line carrying a card-image sequence tag
           // in columns 73-80 is still recognized (the tag is past the program-text area and ignored).
    public void FixedDirective_WithSequenceTag_Recognized()
    {
        string directive = "000300 >>SOURCE FORMAT IS FREE".PadRight(72) + "PROG0003";   // >>SOURCE at col 8; tag at 73-80
        string src = "000100 IDENTIFICATION DIVISION.\n" + directive + "\nPROGRAM-ID. X.\n";
        string free = Norm(src);
        Assert.DoesNotContain(">>SOURCE", free);       // the directive was recognized + consumed
        Assert.DoesNotContain("PROG0003", free);       // the cols-73-80 tag went with the discarded directive line
        Assert.Contains("PROGRAM-ID. X.", free);       // the following (free) segment as-is
    }

    [Fact] // Backward-compat: a single whole-file directive at the top still governs the whole file.
    public void TopOfFile_Directive_GovernsWholeFile()
    {
        string free = Norm(">>SOURCE FORMAT IS FREE\nPROCEDURE DIVISION.\nDISPLAY \"F\".\n");
        Assert.Contains("PROCEDURE DIVISION.", free);
        Assert.Contains("DISPLAY \"F\".", free);
        Assert.DoesNotContain(">>SOURCE", free);
    }
}
