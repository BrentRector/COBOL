// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet;
using CobolNet.Binding.Procedure;
using CobolNet.Runtime;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB120 — WHEN-COMPILED's stamp is COMPILATION-scoped, and the PE is deterministic.
/// <para>§15.99.3 r2: "The returned value is the date and time of compilation of the compilation unit that
/// contains this function." The defect was a process-static <c>Lazy</c>: in a long-lived compiler process
/// (this very battery) every compilation after the first inherited the FIRST one's timestamp. The stamp is
/// now captured once per <c>ProgramEmitter</c> (one compilation) through the injectable
/// <c>IntrinsicBinder.CompileClock</c> seam — these tests inject two different clocks into two successive
/// in-process compilations and read the BAKED constant out of each generated source.</para>
/// <para>§15.99.3 r3's object-code half, decided explicitly (deterministic PE): the generated object code
/// provides no compilation date and time, so r3's "if provided" condition is not engaged — and identical
/// source + references must produce a byte-identical assembly.</para>
/// <para>The <c>&gt;&gt;LEAP-SECOND</c> facts (§7.3.17.4 GR2 / GR3, Annex A.1 item 111): WHEN-COMPILED is the one
/// GR2/GR3 source whose clock the COMPILER reads, so its leap-second pin lives here, through the same seam; the
/// three run-time sources are pinned by <c>LeapSecondReportedSecondsPinTests</c> in the Conformance assembly.</para>
/// </summary>
[Collection("process-globals")]   // kb/Work PB126: IntrinsicBinder.CompileClock is a PROCESS-global static this
                                  // class sets-and-restores. It was the only mutator until the D-D determination
                                  // added a second (CurrentDateOffsetDeterminationTests) — and the two then raced
                                  // immediately: this class's per-compilation assertion read the OTHER class's
                                  // injected clock and failed, green in isolation and red in the full run.
public sealed class WhenCompiledStampTests : CobolNetTestBase
{
    private const string WcSource = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {0}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WC PIC X(21).
        PROCEDURE DIVISION.
        MAIN.
            MOVE FUNCTION WHEN-COMPILED TO WC
            DISPLAY WC
            STOP RUN.
        """;

    /// <summary>Compile one WHEN-COMPILED program under an injected compile clock and return the generated
    /// C# source it produced (where the §15.99.3 r2 constant is baked). A non-empty
    /// <paramref name="directiveLine"/> is written as the source's first line, before the compilation unit
    /// (§7.3.17.3 SR1); a caller writing one passes <see cref="InitialReferenceFormat.Free"/> so it may start in
    /// column 1 — free form is selectable at every edition.</summary>
    private string CompileUnderClock(string programId, DateTimeOffset clock, string directiveLine = "",
        int dialectLevel = 2023, InitialReferenceFormat format = InitialReferenceFormat.Auto)
    {
        string srcPath = Path.Combine(TempDir, programId + ".cob");
        string source = WcSource.Replace("{0}", programId);
        File.WriteAllText(srcPath, directiveLine.Length == 0 ? source : directiveLine + "\n" + source);
        var prior = IntrinsicBinder.CompileClock;
        try
        {
            IntrinsicBinder.CompileClock = () => clock;
            var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: dialectLevel, SourceFormat: format));
            Assert.True(result.Success, string.Join("\n", result.Errors));
            Assert.NotNull(result.GeneratedCsPath);
            return File.ReadAllText(result.GeneratedCsPath!);
        }
        finally
        {
            IntrinsicBinder.CompileClock = prior;
        }
    }

    [Fact]
    public void WhenCompiled_StampIsPerCompilation_NotPerProcess()
    {
        // Two successive compilations in ONE process, a year apart on the injected clock. §15.99.3 r2 requires
        // each to bake ITS OWN compilation time; the pre-fix process-static Lazy handed the second compile
        // whichever stamp the process captured first (this assertion pair cannot both hold on that shape,
        // whatever the Lazy holds).
        var t1 = new DateTimeOffset(2031, 1, 2, 3, 4, 5, 60, TimeSpan.FromHours(-7));
        var t2 = new DateTimeOffset(2032, 6, 7, 8, 9, 10, 110, TimeSpan.FromHours(2));

        string gcs1 = CompileUnderClock("PB120A", t1);
        string gcs2 = CompileUnderClock("PB120B", t2);

        Assert.Contains(CobolDate.Format21(t1), gcs1);
        Assert.Contains(CobolDate.Format21(t2), gcs2);
        Assert.DoesNotContain(CobolDate.Format21(t1), gcs2);
    }

    [Fact]
    public void Backend_EmitsDeterministicPe_IdenticalSourceGivesIdenticalBytes()
    {
        // §15.99.3 r3's object-code half (kb/Work PB120): deterministic PE — no COFF wall-clock stamp, a
        // content-derived MVID. Same source, same assembly name (the module name embeds the output FILE name,
        // so the two outputs must share it), different directories → byte-identical assemblies. Pre-fix the
        // random MVID made these differ on every emit.
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB120DET.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "DET"
                STOP RUN.
            """;
        string srcPath = Path.Combine(TempDir, "pb120det.cob");
        File.WriteAllText(srcPath, source);
        string dir1 = Directory.CreateDirectory(Path.Combine(TempDir, "d1")).FullName;
        string dir2 = Directory.CreateDirectory(Path.Combine(TempDir, "d2")).FullName;

        var r1 = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, OutputPath: Path.Combine(dir1, "out.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r1.Success, string.Join("\n", r1.Errors));
        var r2 = CompilerDriver.Compile(new CompilerDriver.Options(
            srcPath, OutputPath: Path.Combine(dir2, "out.dll"), DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
        Assert.True(r2.Success, string.Join("\n", r2.Errors));

        Assert.Equal(
            File.ReadAllBytes(Path.Combine(dir1, "out.dll")),
            File.ReadAllBytes(Path.Combine(dir2, "out.dll")));
    }

    // ── >>LEAP-SECOND, the WHEN-COMPILED source (§7.3.17.4 GR2 / GR3, Annex A.1 item 111) ──────────────────────

    /// <summary>The last tick .NET can represent before the 2016-12-31 23:59:60 UTC leap second — the one compile
    /// instant at which a renderer that rounded the sub-second tail, or carried it into the seconds field, would
    /// bake a seconds value of 60.</summary>
    private static readonly DateTimeOffset LastTickBeforeLeapSecond =
        DateTimeOffset.Parse("2016-12-31T23:59:59.9999999+00:00", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The §15.99.3 r1 21-character value for <see cref="LastTickBeforeLeapSecond"/>, derived position by
    /// position, never read off a run: 2016 12 31 (1-8), 23 (9-10), 59 (11-12), seconds 59 (13-14 — "00 through 59"
    /// under OFF, "00 through nn" under ON with nn = 59 per DOC-A.1-111), hundredths 99 (15-16 — "hundredths of a
    /// second past the second, in the range 00 through 99": 0.9999999 s past the second is 99 COMPLETED hundredths),
    /// '+' (17 — a zero offset is "the same as" UTC), 00 (18-19), 00 (20-21). cite.py --check 15.99.3 → OK 1).</summary>
    private const string StampAtLastTickBeforeLeapSecond = "2016123123595999+0000";

    /// <summary>Compile at <paramref name="dialectLevel"/> under <paramref name="directiveLine"/> with the compile
    /// clock pinned to <see cref="LastTickBeforeLeapSecond"/>, extract the baked stamp by its SHAPE (a stamp with
    /// seconds 60 has the same shape, so the equality below is what discriminates), and require the derived value.</summary>
    private void AssertWhenCompiledStamp(string programId, string directiveLine, int dialectLevel)
    {
        string generated = CompileUnderClock(programId, LastTickBeforeLeapSecond, directiveLine, dialectLevel,
            InitialReferenceFormat.Free);
        var m = Regex.Match(generated, "\"(?<v>[0-9]{16}[-+0][0-9]{4})\"");
        Assert.True(m.Success, "no §15.99.3 21-character stamp was baked into the generated source");
        Assert.Equal(StampAtLastTickBeforeLeapSecond, m.Groups["v"].Value);
    }

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void WhenCompiled_LeapSecondOn_LastTickBeforeLeapSecond_Bakes59(int dialectLevel) =>
        // GR2 + A.1 item 111, ON SPECIFIED: the determination is "never > 59, maximum 59" (DOC-A.1-111), so the
        // compile-time stamp at the tick before the leap second reports second 59, hundredths 99.
        AssertWhenCompiledStamp($"L1G4WN{dialectLevel % 100:00}", ">>LEAP-SECOND ON", dialectLevel);

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void WhenCompiled_LeapSecondImpliedOn_BareDirective_LastTickBeforeLeapSecond_Bakes59(int dialectLevel) =>
        // GR2's "or implied" arm: a bare >>LEAP-SECOND selects ON, which §7.3.17.2 prints un-underlined (an optional
        // word, §5.2.3: "shown in uppercase and not underlined in general formats" — cite.py --check 5.2.3 → OK).
        AssertWhenCompiledStamp($"L1G4WB{dialectLevel % 100:00}", ">>LEAP-SECOND", dialectLevel);

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void WhenCompiled_LeapSecondOff_LastTickBeforeLeapSecond_Bakes59(int dialectLevel) =>
        // GR3, OFF SPECIFIED: "a value greater than 59 shall not be reported in the seconds position of the value
        // returned from … the WHEN-COMPILED intrinsic function" (cite.py --check 7.3.17.4 → OK 3)).
        AssertWhenCompiledStamp($"L1G4WF{dialectLevel % 100:00}", ">>LEAP-SECOND OFF", dialectLevel);

    [Theory]
    [InlineData(2002)]
    [InlineData(2023)]
    public void WhenCompiled_LeapSecondImpliedOff_LastTickBeforeLeapSecond_Bakes59(int dialectLevel) =>
        // GR3 with GR1's implied OFF: no directive at all (cite.py --check 7.3.17.4 → OK 1)). Compiled in free form
        // too, so this arm differs from the OFF-specified arm ONLY by the directive line.
        AssertWhenCompiledStamp($"L1G4WI{dialectLevel % 100:00}", "", dialectLevel);
}
