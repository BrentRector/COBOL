// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler;
using CobolNet.Frontend.Diagnostics;
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Unit.Semantics;

/// <summary>
/// Item 9 (DEVLOG 307): CopyProcessor diagnostics (Deliverable A). A missing copybook is reported
/// (CBL3620, on every dialect — §7.2.3.4 GR2, kb/Work PB1355); a circular include is reported
/// unconditionally (CBL3621). Source-mapping into the copybook (Deliverable B) is deferred.
/// </summary>
public class CopyDiagnosticTests
{
    /// <summary>Compile <paramref name="source"/> (as TEST.cbl) with copybooks written into the same
    /// temp dir (added as a copy search path), under the given dialect; return all diagnostics.</summary>
    private static IReadOnlyList<Diagnostic> Compile(
        string source, DialectMode dialect, params (string name, string content)[] copybooks)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "cobolsharp_copy_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            foreach (var (name, content) in copybooks)
                File.WriteAllText(Path.Combine(tempDir, name + ".cpy"), content);
            string srcPath = Path.Combine(tempDir, "TEST.cbl");
            File.WriteAllText(srcPath, source);
            var comp = new Compilation { Options = new CompilationOptions { Dialect = dialect } };
            comp.AddCopySearchPath(tempDir);
            return comp.Compile(srcPath, Path.Combine(tempDir, "TEST.dll")).Diagnostics;
        }
        finally { try { Directory.Delete(tempDir, true); } catch { } }
    }

    private const string MissingCopySource = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY NOSUCHBOOK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";

    [Fact]
    public void Strict_MissingCopybook_ReportsCBL3620()
        => Assert.Contains(Compile(MissingCopySource, DialectMode.StrictCobol85), d => d.Code == "CBL3620");

    [Fact]
    public void Default_MissingCopybook_ReportsCBL3620()
        // §7.2.3.4 GR2: library text that cannot be located is an error on every dialect (kb/Work PB1355) — the
        // lenient comment fallback is gone.
        => Assert.Contains(Compile(MissingCopySource, DialectMode.Default), d => d.Code == "CBL3620");

    [Fact]
    public void Strict_ResolvedCopybook_NoCBL3620()
    {
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY MYBOOK.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE SPACE TO WS-COPIED.
           STOP RUN.
";
        var diags = Compile(src, DialectMode.StrictCobol85, ("MYBOOK", "       01 WS-COPIED PIC X(5).\n"));
        Assert.DoesNotContain(diags, d => d.Code == "CBL3620");
    }

    [Fact]
    public void CircularCopy_ReportsCBL3621()
    {
        var src = @"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TESTPROG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY BOOKA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
";
        // BOOKA copies BOOKB which copies BOOKA — unconditional error (tested in Default to prove it is
        // not dialect-gated).
        var diags = Compile(src, DialectMode.Default,
            ("BOOKA", "       COPY BOOKB.\n"),
            ("BOOKB", "       COPY BOOKA.\n"));
        Assert.Contains(diags, d => d.Code == "CBL3621");
    }
}
