// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// <see cref="CobolCallException.CarriedNames"/> is what a CALL site's emitted catch arms filter on
/// (<c>CallEmitter.EnabledOtherCallNames</c> — ISO §14.9.4.4 GR3h item 2's second disjunct), so a level-3 name
/// this carrier can raise but that the list omits reaches NO arm at all: with checking enabled the condition
/// silently bypasses its declarative and terminates the run unit instead. That is exactly how
/// EC-FUNCTION-NOT-FOUND went unhandled until kb/Work PB233 — nothing in the tree contradicted a name that was
/// simply absent from a lookup. A STALE entry costs the other way: a dead disjunct in the filter of every CALL
/// statement compiled under that name's checking.
/// <para>So the list is not hand-maintained blind. This re-derives it from the four files that own the
/// carrier and asserts set equality. The scan is deliberately over TEXT rather than a parse: every
/// exception-name literal on a CODE line — the raise sites, their messages (each quotes the condition it
/// raises), the constructor's own <c>ecName</c> default, and <c>CarriedNames</c> itself. Doc-comment lines are
/// excluded, because a comment may legitimately name a condition this carrier does NOT raise:
/// EC-PROGRAM-ARG-OMITTED is exactly such a name, and saying so is the point of that comment. Level-3 names
/// never end in a hyphen (§14.6.13.1.1's open-suffix rule), which is what separates a real name from the two
/// FAMILY PREFIXES <see cref="CobolCallException.IsProgramOrExternal"/> tests.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CallExceptionCarrierDriftTests
{
    /// <summary>The files that own the carrier: its declaration and every <c>throw new CobolCallException</c>.
    /// A fifth file gaining a raise site is itself drift — <see cref="OnlyTheCarrierFiles_RaiseIt"/>.</summary>
    private static readonly string[] CarrierFiles =
    [
        Path.Combine("Cobol.Net.Runtime", "Control", "ProgramRegistry.cs"),
        Path.Combine("Cobol.Net.Runtime", "Control", "ProgramTable.cs"),
        Path.Combine("Cobol.Net.Runtime", "Control", "ExternalTable.cs"),
        // PB165: StoreReturn is total — a result it cannot store raises EC-PROGRAM-ARG-MISMATCH (§14.9.4.4 GR4)
        // instead of being silently discarded, so the RETURNING landing is a carrier file too.
        Path.Combine("Cobol.Net.Runtime", "Control", "CallAbi.cs"),
    ];

    /// <summary>A level-3 exception-name literal — §14.6.13.1.1's open suffix forbids a trailing hyphen, so
    /// this does not match the <c>"EC-PROGRAM-"</c> / <c>"EC-EXTERNAL-"</c> family prefixes.</summary>
    private static readonly Regex NameLiteral =
        new("\"(EC-[A-Z0-9-]*[A-Z0-9])\"", RegexOptions.Compiled);

    [Fact]
    public void CarriedNames_EqualsTheNamesTheCarrierFilesRaise()
    {
        var found = new SortedSet<string>(StringComparer.Ordinal);
        int codeLines = 0;
        foreach (string rel in CarrierFiles)
        {
            string path = Path.Combine(TestRepo.Src(), rel);
            Assert.True(File.Exists(path), $"{rel} moved — this drift test scans a path that no longer exists");
            foreach (string line in File.ReadAllLines(path))
            {
                string s = line.TrimStart();
                if (s.StartsWith("//", StringComparison.Ordinal)) continue;   // doc and ordinary comments alike
                codeLines++;
                foreach (Match m in NameLiteral.Matches(line)) found.Add(m.Groups[1].Value);
            }
        }

        Assert.True(codeLines >= 200,
            $"only {codeLines} code lines scanned across the carrier files — the scan is broken, so this test "
            + "would pass by looking at nothing (a run must assert its population)");
        Assert.Equal(new SortedSet<string>(CobolCallException.CarriedNames, StringComparer.Ordinal), found);
    }

    /// <summary>A raise site outside the four carrier files would be invisible to the scan above, so the set
    /// equality it asserts would stop meaning anything. Fail here instead of passing hollowly.</summary>
    [Fact]
    public void OnlyTheCarrierFiles_RaiseIt()
    {
        var strays = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(TestRepo.Src(), file);
            if (rel.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || rel.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")) continue;
            if (CarrierFiles.Contains(rel)) continue;
            if (File.ReadAllText(file).Contains("new CobolCallException(", StringComparison.Ordinal))
                strays.Add(rel);
        }
        Assert.True(strays.Count == 0,
            "a CobolCallException raise site outside its carrier files — add the file to CarrierFiles (and its "
            + "name to CarriedNames) or the CALL-site filter will not see the condition:\n  "
            + string.Join("\n  ", strays));
    }

    /// <summary>The list is the CALL emitter's filter, so every member must be a registered level-3 name — a
    /// typo would silently narrow the arm rather than fail anything.</summary>
    [Fact]
    public void EveryCarriedName_IsARegisteredLevel3Condition()
    {
        foreach (string name in CobolCallException.CarriedNames)
        {
            Assert.True(ExceptionCatalog.TryGet(name, out var info), $"{name} is not a registered exception-name");
            Assert.Equal(3, info.Level);
        }
    }

    /// <summary>ISO §14.9.4.4 GR3h item 1's partition, checked on the carrier's own name set: exactly the
    /// EC-PROGRAM-* and EC-EXTERNAL-* members answer true, and at least one member does not — the CALL
    /// emitter's two arms both exist only because the set is genuinely mixed.</summary>
    [Fact]
    public void IsProgramOrExternal_PartitionsTheCarriedNames()
    {
        var family = CobolCallException.CarriedNames.Where(CobolCallException.IsProgramOrExternal).ToList();
        var other = CobolCallException.CarriedNames.Except(family).ToList();
        Assert.All(family, n => Assert.True(n.StartsWith("EC-PROGRAM-", StringComparison.Ordinal)
            || n.StartsWith("EC-EXTERNAL-", StringComparison.Ordinal)));
        Assert.NotEmpty(other);
        Assert.All(other, n => Assert.False(CobolCallException.IsProgramOrExternal(n)));
        Assert.All(CobolCallException.CarriedNames, n => Assert.True(CobolCallException.CanCarry(n)));
        Assert.False(CobolCallException.CanCarry("EC-BOUND-SUBSCRIPT"));
    }
}
