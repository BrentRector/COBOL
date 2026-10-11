// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DRIFT TEST for the "rounded up to the next whole number" landing (kb/Work PB142-B).
/// <para>The standard says that phrase in exactly TWO normative places — §14.7.9.3 GR1 (the RETRY phrase's
/// arithmetic-expression-1, an n-TIMES count) and §14.9.3.4 GR1 (ALLOCATE's arithmetic-expression-1, a byte
/// count) — and for a long time only ONE of them implemented it. ALLOCATE ceilinged (kb/Work PB151); RETRY
/// reached <c>NumericRenderer.Align(…, 0)</c>, whose every lane passes <c>CobolRounding.Truncation</c>, so
/// <c>RETRY 1.5 TIMES</c> emitted 1 re-attempt where GR1 requires 2 and <c>RETRY 0.5 TIMES</c> emitted 0,
/// silently degrading to the no-retry case. That is the one-rule-two-places shape: the fix was to hoist the
/// round-up into <c>NumericRenderer.AlignRoundedUp</c> and give BOTH clauses that one place.</para>
/// <para>This test is what keeps "one place" true. It is a SOURCE-TEXT check on purpose: the defect was never
/// that the helper computed the wrong thing, it was that a call site chose the wrong helper, and only the call
/// sites can witness that. The behavioural half — that the helper itself ceilings, and that the emitted RETRY
/// count really is the ceiling — lives in <c>CobolFileLockTests</c> and the conformance goldens.</para>
/// <para>Proven to FAIL before it was trusted: pointing <c>RetryTimes</c> back at <c>Align(…, 0)</c> makes
/// <see cref="EveryRoundUpRuledEmitSite_UsesAlignRoundedUp"/> red by name.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class NumericRoundUpSiteDriftTests
{
    /// <summary>The emit sites governed by a "rounded up to the next whole number" clause: the file, the method
    /// that renders the clause's arithmetic-expression, and the clause itself.</summary>
    private static readonly (string File, string Method, string Clause)[] RoundUpSites =
    [
        (Path.Combine("Cobol.Net.Compiler", "CodeGen", "Verbs", "SequentialIoEmitter.cs"), "RetryTimes",
            "ISO 14.7.9.3 GR1 — RETRY arithmetic-expression-1"),
        (Path.Combine("Cobol.Net.Compiler", "CodeGen", "Verbs", "PtrEmitter.cs"), "EmitAllocate",
            "ISO 14.9.3.4 GR1 — ALLOCATE arithmetic-expression-1"),
    ];

    /// <summary>Each round-up-ruled site renders through the ONE helper, and none of them reaches the truncating
    /// <c>Align</c>. ALLOCATE's native-float lane is the single named exemption: <c>CobolPtr.AllocateReal</c>
    /// fuses the same ceiling with GR2's ≤ 0 ⇒ NULL and the storage-not-available outcomes only that statement
    /// defines, so the rounding still happens once — inside the runtime entry, which this test names rather than
    /// letting it be an unexplained absence.</summary>
    [Fact]
    public void EveryRoundUpRuledEmitSite_UsesAlignRoundedUp()
    {
        foreach (var (file, method, clause) in RoundUpSites)
        {
            string body = MethodBody(TestRepo.Src(file), method);
            Assert.True(body.Contains("AlignRoundedUp", StringComparison.Ordinal),
                $"{file}#{method} implements {clause} but does not render through "
                + $"NumericRenderer.AlignRoundedUp — the round-up rule has grown a second home.");
            Assert.False(Regex.IsMatch(body, @"\bAlign\s*\([^)]*,\s*0\s*\)"),
                $"{file}#{method} implements {clause} but reaches the TRUNCATING Align(…, 0). "
                + "That is the PB142-B defect exactly: the helper was right and the call site chose the wrong one.");
        }
    }

    /// <summary>The population is CLOSED, not sampled — the spec sweep that justifies a two-entry list. If a
    /// future revision of the transcription grows a third "rounded up to the next whole number" clause, this
    /// fails and the new site has to be added above rather than quietly implemented somewhere else.</summary>
    [Fact]
    public void TheSpecSaysRoundedUp_InExactlyTheClausesThisTestCovers()
    {
        string spec = File.ReadAllText(TestRepo.Specs("ISO_COBOL.md"));
        var clauses = new List<string>();
        foreach (string line in spec.Split('\n'))
        {
            if (!line.Contains("rounded up", StringComparison.OrdinalIgnoreCase)) continue;
            // The two known sites are recognised by their own subject nouns, which is what makes a THIRD one
            // (whatever it is about) fall through to the failure below.
            if (line.Contains("arithmetic-expression-1", StringComparison.Ordinal)
                && line.Contains("number of times", StringComparison.Ordinal))
                clauses.Add("14.7.9.3 GR1");
            else if (line.Contains("arithmetic-expression-1", StringComparison.Ordinal)
                     && line.Contains("bytes of storage", StringComparison.Ordinal))
                clauses.Add("14.9.3.4 GR1");
            else
                clauses.Add("UNCLASSIFIED: " + line.Trim());
        }

        Assert.Equal(RoundUpSites.Length, clauses.Count);
        Assert.DoesNotContain(clauses, c => c.StartsWith("UNCLASSIFIED", StringComparison.Ordinal));
        Assert.Contains("14.7.9.3 GR1", clauses);
        Assert.Contains("14.9.3.4 GR1", clauses);
    }

    /// <summary>Brace-matched body of <paramref name="method"/> in <paramref name="path"/>.</summary>
    private static string MethodBody(string path, string method)
    {
        string src = File.ReadAllText(path);
        // The DECLARATION, not a call site — an earlier call to the same name would otherwise be scanned
        // instead (it was, and this test failed on its own helper before it ever looked at the subject).
        var m = Regex.Match(src,
            $@"^[ \t]*(?:private|public|internal|protected)[^\n(]*\b{Regex.Escape(method)}\s*\(",
            RegexOptions.Multiline);
        Assert.True(m.Success, $"{path} has no method named {method} — the drift list names a site that moved.");
        int i = src.IndexOfAny(['{', '=', ';'], m.Index);
        Assert.True(i >= 0 && src[i] != ';', $"{method} in {path} has no body.");
        if (src[i] == '=')   // expression-bodied: to the terminating semicolon
            return src[i..src.IndexOf(';', i)];
        int depth = 0;
        for (int j = i; j < src.Length; j++)
        {
            if (src[j] == '{') depth++;
            else if (src[j] == '}' && --depth == 0) return src[i..(j + 1)];
        }
        throw new Xunit.Sdk.XunitException($"unbalanced braces scanning {method} in {path}");
    }
}
