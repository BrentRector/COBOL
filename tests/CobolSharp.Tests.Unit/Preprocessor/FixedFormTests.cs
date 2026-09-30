using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolSharp.Tests.Unit.Preprocessor;

public class FixedFormTests
{
    [Fact]
    public void IsFixedForm_NistProgram()
    {
        // Simulate a NIST-style fixed-form program
        string source = "000100 IDENTIFICATION DIVISION.                                         IF1014.2\n"
                       + "000200 PROGRAM-ID.                                                      IF1014.2\n"
                       + "000300     IF101A.                                                      IF1014.2\n";

        Assert.True(ReferenceFormatProcessor.IsFixedForm(PhysicalLines.Read(source).Lines));
    }

    [Fact]
    public void NormalizeFixed_StripsColumns()
    {
        string source = "000100 IDENTIFICATION DIVISION.                                         IF1014.2\n"
                       + "000200 PROGRAM-ID.                                                      IF1014.2\n"
                       + "000300     IF101A.                                                      IF1014.2\n";

        string free = ReferenceFormatProcessor.NormalizeToFreeForm(source, InitialReferenceFormat.Fixed);
        Assert.Contains("IDENTIFICATION DIVISION.", free);
        Assert.DoesNotContain("IF1014.2", free);
        Assert.DoesNotContain("000100", free);
    }

    [Fact]
    public void NormalizeFixed_LargeFile_Completes()
    {
        // Generate a large fixed-form file (1000 lines)
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            string seq = (i * 100 + 100).ToString("D6");
            string content = $" DISPLAY \"Line {i}\".";
            string line = seq + content.PadRight(65) + "TEST4.2";
            sb.AppendLine(line);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        string result = ReferenceFormatProcessor.NormalizeToFreeForm(sb.ToString(), InitialReferenceFormat.Fixed);
        sw.Stop();

        // No wall-clock ceiling (kb/Work PB1590) — completion is the property; a loaded runner breaches any fixed limit.
        Assert.Contains("DISPLAY", result);
    }

    [Fact]
    public void CopyProcessor_LargeFile_Completes()
    {
        // Generate a large free-form file with no COPY statements
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("IDENTIFICATION DIVISION.");
        sb.AppendLine("PROGRAM-ID. BIGTEST.");
        sb.AppendLine("PROCEDURE DIVISION.");
        for (int i = 0; i < 1000; i++)
        {
            sb.AppendLine($"    DISPLAY \"Line {i}\".");
        }
        sb.AppendLine("    STOP RUN.");

        var processor = new CopyProcessor();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string result = processor.Process(sb.ToString(), ".");
        sw.Stop();
        Assert.Contains("STOP RUN", result);

        // No wall-clock ceiling (kb/Work PB1590) — completion is the property; a loaded runner breaches any fixed limit.
    }
}
