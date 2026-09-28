// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Scaffold for the WiseOwl COBOL conformance / differential harness (docs/COBOLNET_DESIGN.md §2, §18.7).
/// </summary>
/// <remarks>
/// The G5 build-out: an <c>ICompilerUnderTest { Compile+Run(src, dialect, nist?) }</c> abstraction with a
/// <c>LegacyCompiler</c> (the byte-engine oracle, 364 NIST-green) and a <c>CobolNetCompiler</c> implementation,
/// driven by a <c>DifferentialNistTests</c> theory that asserts
/// <c>CobolNet stdout == Legacy stdout == tests/nist/valid/*.txt</c> for every NIST program — turning the legacy's
/// 364 passing programs into an instant regression net for the greenfield compiler. Until the PC-dispatcher (G4)
/// and the files/interprogram subsystems (G5) exist, the new compiler cannot run the CCVS corpus, so this file
/// holds only a build-anchoring smoke. Do not add red NIST theories here before G5.
/// </remarks>
public sealed class DifferentialHarness
{
    [Fact]
    public void Scaffold_NewCompilerCompilesATrivialProgram()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "CobolNet_Conf_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            string src = Path.Combine(tempDir, "p.cob");
            File.WriteAllText(src,
                "IDENTIFICATION DIVISION.\nPROGRAM-ID. P.\nPROCEDURE DIVISION.\nMAIN.\n    DISPLAY \"OK\".\n    STOP RUN.\n");
            var result = CompiledProgramCache.Compile(new CompilerDriver.Options(src, Path.Combine(tempDir, "p.dll"), SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(result.Success, $"{result.Status}: {string.Join("\n", result.Errors)}");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
