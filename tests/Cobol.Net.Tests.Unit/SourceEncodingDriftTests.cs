// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A UTF-8 BOM ON A `.g4` FILE BREAKS THE ANTLR BUILD, AND IT BROKE CI WHILE EVERY LOCAL GATE STAYED GREEN.
/// A tool edit wrote <c>CobolLexer.g4</c> with Python's <c>utf-8-sig</c> encoding — which PREPENDS a byte-order
/// mark on write — and the grammar compiler answered
/// <c>error(50): CobolLexer.g4:1:0: syntax error: '∩' came as a complete surprise to me</c>
/// (the BOM's three bytes rendered through a single-byte code page). The Windows Release job failed on
/// "Build (Release, warnings-as-errors)"; the Linux jobs and every local build passed, because the local
/// incremental build had already produced the generated parser and never re-ran the grammar compiler on the
/// changed file the way a clean checkout does.
/// <para>
/// So this guard exists because the failure is INVISIBLE to the fast feedback loop by construction: the file
/// still reads correctly in every editor, the diff shows nothing, and the compiler only objects on a cold
/// build. It is a byte-level fact, so it needs a byte-level test.
/// </para>
/// <para>
/// ⚠ Scoped to the file types where a BOM is actually FATAL or against the repo's convention. C# tolerates a
/// BOM — Roslyn strips it — but the repo is overwhelmingly BOM-less, and a stray one is the fingerprint of
/// exactly the same bad write, so the compiler sources are covered too. Files that legitimately carry a BOM
/// would be listed here with a reason; none does today.
/// </para>
/// </summary>
public sealed class SourceEncodingDriftTests
{
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    /// <summary>Source trees whose files shall not begin with a byte-order mark, with why it matters. The test
    /// sources are covered too (kb/Work PB927): the same bad tool write produced them, and a file a sweep reads
    /// wrongly is a file the sweep misjudges.</summary>
    public static IEnumerable<object[]> Scopes =>
    [
        ["src", "*.g4", "the ANTLR grammar compiler rejects a BOM outright (error(50), 'came as a complete surprise')"],
        ["src", "*.cs", "the repo convention is BOM-less, and a stray BOM is the fingerprint of a bad tool write"],
        ["tests", "*.cs", "the repo convention is BOM-less, and a stray BOM is the fingerprint of a bad tool write"],
    ];

    [Theory]
    [MemberData(nameof(Scopes))]
    public void NoSourceFileStartsWithAByteOrderMark(string tree, string pattern, string why)
    {
        var offenders = new List<string>();
        byte[] head = new byte[3];   // hoisted: CA2014 forbids a stackalloc inside the loop
        string root = TestRepo.At(tree);
        foreach (string f in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            // Generated/ and obj/ are BUILD OUTPUTS (feedback_generated_parser_is_a_build_output) — the tool
            // that writes them chooses their encoding, and they are never committed.
            if (rel.Contains("/obj/") || rel.Contains("/bin/") || rel.Contains("Generated/")
                || rel.EndsWith(".g.cs", StringComparison.Ordinal)) continue;

            using var s = File.OpenRead(f);
            if (s.Read(head, 0, 3) == 3 && head.AsSpan().SequenceEqual(Bom)) offenders.Add(rel);
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} {pattern} file(s) start with a UTF-8 BOM — {why}:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + Environment.NewLine
            + "Strip the first three bytes (EF BB BF). If a tool wrote the file, check its encoding: Python's "
            + "'utf-8-sig' STRIPS a BOM when reading and ADDS one when writing, so a read/modify/write round "
            + "trip with that encoding introduces one into a file that never had it.");
    }

    /// <summary>The scan must FIND the files it claims to check — an empty enumeration would make the assertion
    /// above pass on a broken path (feedback_verdict_evidence_invariant).</summary>
    [Theory]
    [MemberData(nameof(Scopes))]
    public void TheScanSeesFiles(string tree, string pattern, string why)
    {
        _ = why;
        int n = Directory.EnumerateFiles(TestRepo.At(tree), pattern, SearchOption.AllDirectories).Count();
        Assert.True(n > 0, $"the {pattern} scan found no files under {TestRepo.At(tree)} — the scan is broken");
    }
}
