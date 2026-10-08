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
/// ISO §7.2.3.4 GR10 — "If the REPLACING phrase is specified, the library text shall not contain a COPY
/// statement" — against GR12, which permits nesting (at least 5 levels) only WITHOUT replacing (kb/Work R34).
/// The differential's syn_copy:630 ("COPY: recursive replacement", a GnuCOBOL EXTENSION) exposed the gap: the
/// preprocessor recursed into the nested copybook OUTSIDE the replacement scope, so the ISO-illegal
/// combination produced arbitrary partial text and a misleading downstream COBOLNET1639 on whatever name
/// failed to materialize. The verdict (reject) was right by accident; COBOLNET1640 makes the REASON right —
/// at the outer COPY's line, from the preprocessor that owns the rule.
/// </summary>
public sealed class CopyReplacingNestedCopyTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CobolNet_R34_" + Guid.NewGuid().ToString("N")[..8]);

    public CopyReplacingNestedCopyTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ } }

    private void Copybook(string name, string content) => File.WriteAllText(Path.Combine(_dir, name), content);

    private (string Text, DiagnosticBag Diags) Run(string mainText)
    {
        var bag = new DiagnosticBag();
        var copy = new CopyProcessor([_dir], bag, dialectLevel: 2023, permissive: false);
        string text = ConditionalCompilationProcessor.ProcessWithCopy(mainText, copy,
            CobolNet.Frontend.Frontend.LeftDirectives, diagnostics: bag, sourcePath: "t.cob", dialectLevel: 2023);
        return (text, bag);
    }

    [Fact] // GR10 — the ISO-illegal combination draws COBOLNET1640, not a downstream undefined-reference.
    public void ReplacingOverNestedCopy_DrawsGr10()
    {
        Copybook("inner.cpy", "01 TEST-VAR PIC X(2) VALUE \"OK\".\n");
        Copybook("outer.cpy", "COPY inner.\n");
        var (_, diags) = Run("COPY outer REPLACING ==TEST-VAR== BY ==COPY-VAR==.\n");
        Assert.Contains(diags.Diagnostics, d => d.Code == "COBOLNET1640" && d.IsError);
    }

    [Fact] // GR12 — nesting WITHOUT replacing is legal and expands.
    public void PlainNestedCopy_ExpandsClean()
    {
        Copybook("inner.cpy", "01 TEST-VAR PIC X(2) VALUE \"OK\".\n");
        Copybook("outer.cpy", "COPY inner.\n");
        var (text, diags) = Run("COPY outer.\n");
        Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        Assert.Contains("TEST-VAR", text);
    }

    [Fact] // GR12's floor — "at least 5 levels, including the first COPY statement in the sequence".
    public void FiveLevelNesting_WithoutReplacing_Expands()
    {
        Copybook("l5.cpy", "01 L5-VAR PIC X(2) VALUE \"L5\".\n");
        Copybook("l4.cpy", "COPY l5.\n");
        Copybook("l3.cpy", "COPY l4.\n");
        Copybook("l2.cpy", "COPY l3.\n");
        var (text, diags) = Run("COPY l2.\n");
        Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        Assert.Contains("L5-VAR", text);
    }

    [Fact] // The control: a FLAT copybook under REPLACING stays clean and replaced.
    public void FlatReplacing_StaysClean()
    {
        Copybook("flat.cpy", "01 TEST-VAR PIC X(2) VALUE \"OK\".\n");
        var (text, diags) = Run("COPY flat REPLACING ==TEST-VAR== BY ==COPY-VAR==.\n");
        Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        Assert.Contains("COPY-VAR", text);
        Assert.DoesNotContain("TEST-VAR", text);
    }
}
