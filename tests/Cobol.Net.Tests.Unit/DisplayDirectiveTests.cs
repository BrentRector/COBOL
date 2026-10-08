// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.IO;
using System.Linq;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The DISPLAY directive (ISO §7.3.12, kb/Work PB807 + PB1538). Every expectation is derived from the standard's rule or
/// from the determination <c>docs/CONFORMANCE.md</c> records for the latitude §7.3.12.4 GR2 leaves (DOC-A.1-53 to
/// DOC-A.1-55): GR1 the operands are transferred, GR3 a PARAMETER value comes from the environment and its absence
/// means "no transfer shall take place", GR4 in the order written, GR5/GR6 the UPON phrase and its LISTING default. Every
/// case runs the real merged driver and reads what it TRANSFERRED off the diagnostic bag.
/// </summary>
public sealed class DisplayDirectiveTests
{
    private static (CompileOutputLine[] Output, DiagnosticBag Bag) Run(string directives)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([], bag, dialectLevel: 2023, permissive: false);
        ConditionalCompilationProcessor.ProcessWithCopy(directives + " PROCEDURE DIVISION.\n", copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        return (bag.CompileOutput.ToArray(), bag);
    }

    private static string[] Std(string directives) => Run(directives).Output
        .Where(o => o.Stream == CompileOutputStream.Standard).Select(o => o.Text).ToArray();

    private static string[] Codes(DiagnosticBag bag) => bag.Diagnostics.Select(d => d.Code).ToArray();

    [Fact] // DOC-A.1-53's own example: the images of one directive, in operand order (GR4), one space between them, ONE line
    public void OperandsAreTransferredInOrderAsOneLine()
        => Assert.Equal(["HELLO-COMPILE 42"], Std(" >>DISPLAY \"HELLO-COMPILE\" 42\n"));

    [Fact] // GR6: "If the UPON phrase is not specified, the default is as if UPON LISTING were specified" — and DOC-A.1-53/54
           // route LISTING to the standard output when no listing is produced
    public void NoUponPhrase_IsAsIfUponListing_WhichIsStandardOutput()
    {
        var plain = Run(" >>DISPLAY \"X\"\n").Output;
        var listing = Run(" >>DISPLAY \"X\" UPON LISTING\n").Output;
        Assert.Equal([new CompileOutputLine(CompileOutputStream.Standard, "X")], plain);
        Assert.Equal(plain, listing);
    }

    [Theory] // DOC-A.1-54: CONSOLE and SYSOUT are the compiler's standard output, SYSERR its standard error; a phrase that
             // names several destinations writes the line once to each, in the order written (§5.2.6.4: the device group and
             // LISTING each at most once, in any order)
    [InlineData("UPON CONSOLE", "S")]
    [InlineData("UPON SYSOUT", "S")]
    [InlineData("UPON SysErr", "E")]
    [InlineData("UPON LISTING", "S")]
    [InlineData("UPON CONSOLE SYSERR", "SE")]
    [InlineData("UPON SYSERR CONSOLE LISTING", "ESS")]
    [InlineData("UPON LISTING SYSERR", "SE")]
    [InlineData("UPON SYSERR LISTING SYSOUT", "ESS", false)]   // a second run of devices: refused (next test); nothing transferred
    public void Upon_NamesTheDestinations(string upon, string streams, bool valid = true)
    {
        var (output, _) = Run($" >>DISPLAY \"X\" {upon}\n");
        if (!valid) { Assert.Empty(output); return; }
        Assert.Equal(streams, string.Concat(output.Select(o => o.Stream == CompileOutputStream.Standard ? 'S' : 'E')));
        Assert.All(output, o => Assert.Equal("X", o.Text));
    }

    [Theory] // the UPON phrase's format and GR5 b): refused as COBOLNET2698, and nothing is transferred
    [InlineData("UPON SYSIN")]                    // an INPUT-only device-name
    [InlineData("UPON NOSUCH")]                   // no such device
    [InlineData("UPON LISTING LISTING")]          // LISTING twice
    [InlineData("UPON CONSOLE LISTING SYSERR")]   // two separate runs of devices
    public void Upon_BreakingItsFormat_IsDiagnosedAndTransfersNothing(string upon)
    {
        var (output, bag) = Run($" >>DISPLAY \"X\" {upon}\n");
        Assert.Contains("COBOLNET2698", Codes(bag));
        Assert.Empty(output);
    }

    [Theory] // DOC-A.1-53: alphanumeric / national literals are their characters, boolean values their 0 and 1 characters,
             // numeric values the shortest decimal form
    [InlineData("\"plain text\"", "plain text")]
    [InlineData("\"say \"\"hi\"\"\"", "say \"hi\"")]
    [InlineData("N\"nat\"", "nat")]
    [InlineData("B\"1010\"", "1010")]
    [InlineData("B\"1100\" B-AND B\"1010\"", "1000")]
    [InlineData("42", "42")]
    [InlineData("0", "0")]
    [InlineData("-2.5", "-2.5")]
    [InlineData("0.125", "0.125")]
    [InlineData("+5", "5")]
    [InlineData("1.50", "1.5")]
    [InlineData("1.0", "1")]
    [InlineData("100", "100")]
    [InlineData("-0.001", "-0.001")]
    public void ConversionOfData_FollowsTheDeterminedImages(string operand, string image)
        => Assert.Equal([image], Std($" >>DISPLAY {operand}\n"));

    [Fact] // §7.3.11.4 GR1: a compilation-variable-name is usable in any directive where a literal of its category is
           // permitted — the DISPLAY directive reads the value the name was DEFINEd to, and its category decides the image
    public void ACompilationVariableName_IsAnOperand()
        => Assert.Equal(["3 wd 1100"], Std(" >>DEFINE N AS 3\n >>DEFINE W AS \"wd\"\n >>DEFINE B AS B\"1100\"\n >>DISPLAY N W B\n"));

    [Fact] // §7.3.11.4 GR2: after DEFINE … OFF the name "shall not be used except in a defined condition" — the DISPLAY
           // operand is a use (kb/Work PB1368)
    public void ANameAfterDefineOff_IsNotAnOperand()
    {
        var (output, bag) = Run(" >>DEFINE X AS 1\n >>DEFINE X OFF\n >>DISPLAY X\n");
        Assert.Contains("COBOLNET1619", Codes(bag));
        Assert.Empty(output);
    }

    [Fact] // a later DEFINE revives the name (§7.3.11.4 GR2 "unless it is redefined in a subsequent DEFINE directive")
    public void ANameRedefinedAfterOff_IsAnOperandAgain()
        => Assert.Equal(["2"], Std(" >>DEFINE X AS 1\n >>DEFINE X OFF\n >>DEFINE X AS 2\n >>DISPLAY X\n"));

    [Fact] // PARAMETER is a §8.12 compiler-directive word, so no compilation variable is named PARAMETER (§7.3.11.3 SR1): the
           // word with no name after it is not an operand
    public void ParameterWithNoName_IsNotAnOperand()
    {
        var (output, bag) = Run(" >>DISPLAY PARAMETER\n");
        Assert.Contains("COBOLNET1619", Codes(bag));
        Assert.Empty(output);
    }

    [Fact] // GR3 + DOC-A.1-55: the environment variable named by the name in UPPER case, whatever the spelling; the value
           // is transferred unchanged, with no numeric conversion
    public void Parameter_IsReadFromTheEnvironment_ByTheUpperCaseName()
    {
        const string name = "WISEOWL_PB807_PARAM";
        Environment.SetEnvironmentVariable(name, "007.50");
        try
        {
            Assert.Equal(["007.50"], Std($" >>DISPLAY PARAMETER {name}\n"));
            Assert.Equal(["007.50"], Std($" >>DISPLAY PARAMETER {name.ToLowerInvariant()}\n"));
            Assert.Equal(["before 007.50 after"], Std($" >>DISPLAY \"before\" PARAMETER {name.ToLowerInvariant()} \"after\"\n"));
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact] // GR3: "If no value is made available from the operating environment no transfer shall take place" — none, for any
           // operand of that directive; and the name is not thereby defined as a compilation variable
    public void AbsentParameter_TransfersNothingAtAll()
    {
        var (output, bag) = Run(" >>DISPLAY \"A\" PARAMETER WISEOWL_PB807_ABSENT \"B\"\n >>IF WISEOWL_PB807_ABSENT IS DEFINED\n >>DISPLAY \"DEFINED\"\n >>END-IF\n");
        Assert.Empty(output);
        Assert.False(bag.HasErrors, string.Join("\n", bag.Diagnostics));
    }

    [Fact] // a directive in an OMITTED branch is not compiled, so it transfers nothing
    public void ADisplayInAnOmittedBranch_TransfersNothing()
        => Assert.Equal(["kept"], Std(" >>IF 1 = 2\n >>DISPLAY \"dropped\"\n >>ELSE\n >>DISPLAY \"kept\"\n >>END-IF\n"));

    [Fact] // §7.3.12.4 GR4 across directives: directives are processed in the order encountered
    public void SeveralDirectives_TransferInTheOrderMet()
        => Assert.Equal(["one", "two 2"], Std(" >>DISPLAY \"one\"\n >>DEFINE N AS 2\n >>DISPLAY \"two\" N\n"));

    [Theory] // §7.3.12.3 SR2 / SR3 and §7.3.3 SR10: the operand is not an arithmetic expression, a boolean expression, a
             // literal or a PARAMETER phrase, or is a literal form a directive bars — one diagnostic, nothing transferred
    [InlineData(")( 3 +", "COBOLNET1619")]            // the PB807 repro: four alternatives, none matched
    [InlineData("\"A\" & \"B\"", "COBOLNET1619")]     // concatenation expression (SR10)
    [InlineData("ZERO", "COBOLNET1619")]              // figurative constant (SR10)
    [InlineData("1.0E3", "COBOLNET1619")]             // floating-point literal (SR10)
    [InlineData("\"A\" UPON", "COBOLNET1619")]        // UPON with no destination
    [InlineData("UPON CONSOLE", "COBOLNET1619")]      // no operand before UPON
    [InlineData("\"A\" B-AND 1", "COBOLNET1619")]     // a boolean operator over a non-boolean operand
    public void AMalformedOperand_IsDiagnosedAndTransfersNothing(string operands, string code)
    {
        var (output, bag) = Run($" >>DISPLAY {operands}\n");
        Assert.Contains(code, Codes(bag));
        Assert.Empty(output);
    }

    [Fact] // one bad operand spoils the whole directive's transfer: a half-evaluated line is not what the author wrote
    public void OneBadOperand_TransfersNoPartialLine()
    {
        var (output, bag) = Run(" >>DISPLAY \"good\" )( \n");
        Assert.True(bag.HasErrors);
        Assert.Empty(output);
    }

    [Fact] // the CLI's carrier end to end: CompilerDriver hands every transferred line, with its stream, to the caller
    public void CompilerDriverResult_CarriesTheTransferredLines()
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB1538_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "d.cob");
            File.WriteAllText(path, "       >>DISPLAY \"to-stdout\" 1\n       >>DISPLAY \"to-stderr\" UPON SYSERR\n"
                + "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB1538D.\n       PROCEDURE DIVISION.\n           STOP RUN.\n");
            var result = CobolNet.CompilerDriver.Compile(new CobolNet.CompilerDriver.Options(path, CheckOnly: true));
            Assert.True(result.Success, string.Join("\n", result.Errors));
            Assert.Equal(
                [new CompileOutputLine(CompileOutputStream.Standard, "to-stdout 1"), new CompileOutputLine(CompileOutputStream.Error, "to-stderr")],
                result.CompileOutput);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
