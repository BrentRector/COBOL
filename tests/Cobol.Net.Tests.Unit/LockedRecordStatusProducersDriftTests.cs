// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE WITNESS FOR ANNEX A.1 ITEM 152'S "Condition absent." DETERMINATION (kb/Work PB1536 Q1, owner decision
/// R43 item 3). Item 152 — "circumstances other than a locked logical record that return a locked record status"
/// — is conditionally required, "conditioned on the existence of such circumstances in an implementation"
/// (A.1 item 152, <c>cite.py --check A.1 "conditioned on the existence of such circumstances in an
/// implementation"</c> → OK §A.1 152)), the latitude §9.1.16 grants — "The implementor may specify circumstances
/// other than a locked logical record that result in the return of a locked record status." (<c>cite.py --check
/// 9.1.16</c> → OK). docs/CONFORMANCE.md §7 records that no such circumstance exists: I-O status
/// '51' (§9.1.13.8 item 1, the record operation conflict) is produced ONLY for a record another file connector
/// holds locked.
///
/// <para>A universal negative cannot be observed by running a program, so the determination is pinned where it
/// lives: the set of places the runtime PRODUCES <see cref="FileStatusCode.RecordLocked"/>. Today there are two,
/// and each is guarded by a lock held by another connector — <c>PhysicalFileTable.LockRecord</c> (the record's
/// lock owner is a different connector) and <c>FileRegistry.ConflictOnLockedRecord</c>
/// (<c>PhysicalFileTable.IsLockedByOther</c>). A third producer turns this RED, and the answer is to re-adjudicate
/// item 152 (write the new circumstance into §7) — never to widen the expected set silently. The behavioural half,
/// '51' for a genuinely locked record, is pinned by <c>conformance:cases/A.1-Implementor-defined-language-element-list/pb669_lock_visibility_plain_connector</c>.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class LockedRecordStatusProducersDriftTests
{
    /// <summary>A use of the '51' constant that COMPARES it (a consumer), not one that yields it.</summary>
    private static readonly Regex Comparison = new(@"(==|!=)\s*FileStatusCode\.RecordLocked|FileStatusCode\.RecordLocked\s*(==|!=)",
        RegexOptions.Compiled);

    /// <summary>The nearest enclosing member declaration above a line: an access modifier, a return type and a name
    /// followed by an open parenthesis.</summary>
    private static readonly Regex Member = new(
        @"^\s*(?:public|private|internal|protected)\b[^=;]*?\b(?<name>[A-Z][A-Za-z0-9_]*)\s*(?:<[^>]*>)?\s*\(",
        RegexOptions.Compiled);

    private static IEnumerable<string> RuntimeAndCompilerSources() =>
        new[] { "Cobol.Net.Runtime", "Cobol.Net.Compiler" }
            .SelectMany(p => Directory.EnumerateFiles(TestRepo.Src(p), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    [Fact]
    public void LockedRecordStatus_IsProducedOnlyWhereAnotherConnectorHoldsTheLock()
    {
        Assert.Equal("51", FileStatusCode.RecordLocked);   // the constant this scan keys on IS the '51' status

        var producers = new SortedSet<string>(StringComparer.Ordinal);
        var literals = new List<string>();
        foreach (string file in RuntimeAndCompilerSources())
        {
            string[] lines = File.ReadAllLines(file);
            string owner = Path.GetFileNameWithoutExtension(file);
            for (int i = 0; i < lines.Length; i++)
            {
                string code = lines[i].Split("//", 2)[0];
                if (code.Contains("\"51\"", StringComparison.Ordinal) && !file.EndsWith("FileStatus.cs", StringComparison.Ordinal))
                    literals.Add($"{Path.GetRelativePath(TestRepo.Root, file)}:{i + 1}");
                if (!code.Contains("FileStatusCode.RecordLocked", StringComparison.Ordinal) || Comparison.IsMatch(code))
                    continue;
                string member = "?";
                for (int k = i; k >= 0; k--)
                    if (Member.Match(lines[k]) is { Success: true } m) { member = m.Groups["name"].Value; break; }
                producers.Add($"{owner}.{member}");
            }
        }

        Assert.True(literals.Count == 0,
            "the '51' status must be spelled FileStatusCode.RecordLocked so this witness can see every producer; a bare "
            + "literal hides one:\n  " + string.Join("\n  ", literals));
        Assert.Equal(["FileRegistry.ConflictOnLockedRecord", "PhysicalFileTable.LockRecord"], producers);
    }
}
