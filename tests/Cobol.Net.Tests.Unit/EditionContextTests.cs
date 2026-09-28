// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The P2.1 edition channels and severity seam (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan"):
/// <see cref="EditionContext.Diagnostics"/> is errors-only and fails the compile; <see cref="EditionContext.Warnings"/>
/// never fails; <see cref="EditionContext.Removed"/> is THE strict/permissive policy — error when strict,
/// warning (same code) when permissive; and the driver carries <c>Result.Warnings</c> on every outcome.
/// </summary>
public sealed class EditionContextTests
{
    [Fact]
    public void Removed_IsError_WhenStrict()
    {
        var ed = new EditionContext(2023);                     // strict is the default axis (§10 #1)
        Assert.False(ed.Permissive);
        ed.Removed(EditionCodes.RemovedConstruct, "LABEL RECORDS clause removed in COBOL-2002");
        Assert.True(ed.HasErrors);
        Assert.Empty(ed.Warnings);
        Assert.Contains(ed.Diagnostics, d => d.StartsWith("error COBOLNET0902", StringComparison.Ordinal));
    }

    [Fact]
    public void Removed_IsWarning_WhenPermissive()
    {
        var ed = new EditionContext(2023, permissive: true);   // the documented migration mode
        ed.Removed(EditionCodes.RemovedConstruct, "LABEL RECORDS clause removed in COBOL-2002");
        Assert.False(ed.HasErrors);                             // permissive must NOT fail the compile
        Assert.Contains(ed.Warnings, w => w.StartsWith("warning COBOLNET0902", StringComparison.Ordinal));
    }

    [Fact]
    public void Warning_NeverFails_OnEitherAxis()
    {
        foreach (bool permissive in new[] { false, true })
        {
            var ed = new EditionContext(2023, permissive);
            ed.Warning(EditionCodes.ObsoleteFlag, "EXIT PROGRAM is archaic in COBOL-2023 (ISO Annex F.1)");
            Assert.False(ed.HasErrors);
            Assert.Single(ed.Warnings);
        }
    }

    [Fact]
    public void Error_Fails_OnBothAxes()
    {
        // Introduction gating (0900) is an error even under permissive — the targeted edition has no
        // semantics for a construct newer than itself (P2.3 band table).
        var ed = new EditionContext(85, permissive: true);
        ed.Error(EditionCodes.Introduction, "EXIT METHOD requires COBOL-2002 (targeting COBOL-85)");
        Assert.True(ed.HasErrors);
    }

    [Fact]
    public void Driver_CarriesWarnings_OnSuccess()
    {
        // A clean program compiles warning-free on both axes, and Result.Warnings is present (not null) on
        // success — the CLI prints it unconditionally (P2.1 carriers).
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_EdCtx_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "clean.cob");
            File.WriteAllText(src,
                "IDENTIFICATION DIVISION.\nPROGRAM-ID. CLEAN-ED.\nPROCEDURE DIVISION.\nM.\n    STOP RUN.\n");
            foreach (bool permissive in new[] { false, true })
            {
                var r = CompilerDriver.Compile(new CompilerDriver.Options(
                    src, Path.Combine(dir, "clean.dll"), Permissive: permissive, SourceFormat: InitialReferenceFormat.Auto));
                Assert.True(r.Success, string.Join("\n", r.Errors));
                Assert.NotNull(r.Warnings);
                Assert.Empty(r.Warnings);
            }
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
