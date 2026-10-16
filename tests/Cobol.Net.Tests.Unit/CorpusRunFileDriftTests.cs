// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A CONFORMANCE CORPUS DIRECTORY TRACKS ONLY WHAT THE COMPILER READS — the programs, their expected output and
/// diagnostics, the manifests and the library text a program COPYs — and NEVER A FILE A PROGRAM WRITES WHEN IT RUNS
/// (kb/Work PB3101, branch-review finding R1-3; the root-directory twin is PB2608). The suites run every golden in a
/// directory of its own (<c>CorpusRunnerTests</c> stages a temporary run directory), so a run file committed beside its
/// program is never what the program reads: it is a byproduct of a by-hand run whose working directory was the corpus
/// directory, swept in by a <c>git add -A</c>. Eleven of them were tracked (eight from one PB3101 checkpoint, three
/// older: <c>2002/PB1226CON.TXT</c>, <c>2023/pb63f1e.dat</c>, <c>2023/pb63loc.txt</c>), and each one shows up as a
/// modified tracked file after any by-hand run whose output differs.
/// <para>⭐ The rule is positive, so the next byproduct fails whatever its extension or however the program named it: a
/// tracked file directly in a corpus directory is one of the corpus kinds (<see cref="CorpusKinds"/>) or is named by a
/// COPY statement of a program in that directory (<c>85/pb1355c.txt</c>, a COPY statement's library text). The population
/// is the COMMITTED tree (<see cref="TrackedTree.Files"/>), and <see cref="TheSweepFiresOnTheR1_3Shape"/> fires the
/// same <see cref="Strays"/> the tree sweep runs on the shape that was committed.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class CorpusRunFileDriftTests
{
    private const string Corpus = "tests/conformance/";

    /// <summary>The kinds a corpus directory holds by construction: COBOL source and library text, expected output
    /// (<c>.out</c>), expected diagnostics (<c>.err</c>), the discovery manifests and the README.</summary>
    private static readonly HashSet<string> CorpusKinds =
        new([".cob", ".cbl", ".cpy", ".out", ".err", ".json", ".md"], StringComparer.OrdinalIgnoreCase);

    /// <summary>No corpus directory tracks a file that no program in it COPYs.</summary>
    [Fact]
    public void NoCorpusDirectoryTracksARunFile()
    {
        var tracked = TrackedTree.Files();
        int population = tracked.Count(p => p.StartsWith(Corpus, StringComparison.Ordinal));
        Assert.True(population > 1000, $"the sweep saw only {population} tracked corpus files; the enumeration is broken");

        var strays = Strays(tracked, path => File.ReadAllText(Path.Combine(TestRepo.Root, path)));
        Assert.True(strays.Count == 0,
            $"{strays.Count} tracked file(s) in a conformance corpus directory are neither a corpus kind nor library text "
            + "a program there COPYs — a run file a by-hand run left beside its program:" + Environment.NewLine
            + string.Join(Environment.NewLine, strays.Select(s => "    " + s)) + Environment.NewLine
            + "`git rm --cached` each one (the suites run every program in its own directory, so none is an input).");
    }

    /// <summary>The detector fires on the committed shape (a run file beside the program that ASSIGNs it) and spares
    /// library text a program COPYs.</summary>
    [Fact]
    public void TheSweepFiresOnTheR1_3Shape()
    {
        var sources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tests/conformance/2014/pb3030_bit_odo_variable_group.cob"] = "           SELECT F ASSIGN TO \"pb3030.seq\".",
            ["tests/conformance/85/pb1355_library_text_located.cob"] = "           COPY \"pb1355c.txt\".",
        };
        string[] tracked =
        [
            .. sources.Keys,
            "tests/conformance/2014/pb3030.seq",
            "tests/conformance/85/pb1355c.txt",
            "tests/conformance/85/manifest.json",
            "tests/conformance/85/sub/LIB.cpy",
        ];

        Assert.Equal(["tests/conformance/2014/pb3030.seq"], Strays(tracked, path => sources[path]));
    }

    /// <summary>The tracked files directly in a corpus directory that are not a corpus kind and that no COPY statement of
    /// a program in the same directory names. Files in a subdirectory (a COPY library) are not swept.</summary>
    internal static IReadOnlyList<string> Strays(IReadOnlyList<string> tracked, Func<string, string> readSource)
    {
        var candidates = tracked
            .Where(p => p.StartsWith(Corpus, StringComparison.Ordinal)
                && p[Corpus.Length..].Count(c => c == '/') == 1
                && !CorpusKinds.Contains(Path.GetExtension(p)))
            .ToList();
        if (candidates.Count == 0) return [];
        var programsByDirectory = tracked
            .Where(p => p.StartsWith(Corpus, StringComparison.Ordinal)
                && p[Corpus.Length..].Count(c => c == '/') == 1
                && p.EndsWith(".cob", StringComparison.OrdinalIgnoreCase))
            .GroupBy(DirectoryOf, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(readSource).ToList(), StringComparer.Ordinal);
        return candidates
            .Where(file => !programsByDirectory.TryGetValue(DirectoryOf(file), out var programs)
                || !programs.Any(text => CopyNames(text, Path.GetFileName(file))))
            .ToList();
    }

    private static string DirectoryOf(string path) => path[..path.LastIndexOf('/')];

    /// <summary>True when <paramref name="source"/> holds a COPY statement whose text-name is
    /// <paramref name="fileName"/>, quoted or not.</summary>
    private static bool CopyNames(string source, string fileName) =>
        Regex.IsMatch(source, @"\bCOPY\s+""?" + Regex.Escape(fileName) + @"(""|\s|\.|$)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
}
