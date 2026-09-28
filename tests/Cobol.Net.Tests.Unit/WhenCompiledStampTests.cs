// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
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
    /// C# source it produced (where the §15.99.3 r2 constant is baked).</summary>
    private string CompileUnderClock(string programId, DateTimeOffset clock)
    {
        string srcPath = Path.Combine(TempDir, programId + ".cob");
        File.WriteAllText(srcPath, WcSource.Replace("{0}", programId));
        var prior = IntrinsicBinder.CompileClock;
        try
        {
            IntrinsicBinder.CompileClock = () => clock;
            var result = CompilerDriver.Compile(new CompilerDriver.Options(srcPath, DialectLevel: 2023, SourceFormat: InitialReferenceFormat.Auto));
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
}
