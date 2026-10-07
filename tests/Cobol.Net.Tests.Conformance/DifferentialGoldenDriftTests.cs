// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The differential-golden drift guard (rearchitecture P0 step 11) — the "nothing silently orphaned" backstop for the
/// committed differential goldens, mirroring <see cref="CorpusManifestTests"/>/<c>ConstructRegistryDriftTests</c>. It pins the folder-level
/// correspondence between the converted test classes and their committed goldens under <c>tests/differential/</c>:
/// <list type="number">
/// <item>every test class that calls <c>DifferentialGolden.Assert</c> has a NON-EMPTY <c>tests/differential/&lt;Class&gt;/</c>
///   folder — catches a class whose goldens were never committed;</item>
/// <item>every <c>tests/differential/&lt;Class&gt;/</c> folder maps to a live converted class of that exact name — catches
///   a whole class deleted/renamed while its goldens linger (a folder-level orphan).</item>
/// </list>
/// Scope note (no silent cap — memory feedback): this is FOLDER-level. A single stale <c>&lt;hash&gt;.out</c> left by a
/// removed/edited <c>[InlineData]</c> case is NOT flagged here: it is harmless, because the comparison never reads a
/// hash no test requests, and a case-level census needs the run-time hash of every case, which no static scan has.
/// </summary>
public sealed class DifferentialGoldenDriftTests
{
    private static string ConformanceDir => TestRepo.Tests("Cobol.Net.Tests.Conformance");
    private static string DifferentialDir => TestRepo.Tests("differential");

    /// <summary>Test classes (by file base name) whose body calls <c>DifferentialGolden.Assert</c> — the classes that
    /// own committed goldens. The infrastructure files (<c>DifferentialGolden</c> the helper, and this drift test — both
    /// named <c>DifferentialGolden*</c>) reference the identifier in code/prose but own no goldens, so they are
    /// excluded; no real converted class is named <c>DifferentialGolden*</c>.</summary>
    private static HashSet<string> ConvertedClasses() =>
        Directory.EnumerateFiles(ConformanceDir, "*.cs")
            .Where(f => !Path.GetFileNameWithoutExtension(f)!.StartsWith("DifferentialGolden", StringComparison.Ordinal)
                        && File.ReadAllText(f).Contains("DifferentialGolden.Assert"))
            .Select(f => Path.GetFileNameWithoutExtension(f)!)
            .ToHashSet(StringComparer.Ordinal);

    private static IEnumerable<string> GoldenFolders() =>
        Directory.Exists(DifferentialDir)
            ? Directory.EnumerateDirectories(DifferentialDir).Select(d => Path.GetFileName(d)!)
            : [];

    [Fact]
    public void EveryConvertedClass_HasNonEmptyGoldenFolder()
    {
        var missing = ConvertedClasses()
            .Where(c => !Directory.Exists(Path.Combine(DifferentialDir, c))
                        || !Directory.EnumerateFiles(Path.Combine(DifferentialDir, c), "*.out").Any())
            .Order().ToList();
        Assert.True(missing.Count == 0,
            $"converted classes with no committed goldens (each missing case's failing DifferentialGolden.Assert names the " +
            $"path to commit its spec-derived expected stdout at): {string.Join(", ", missing)}");
    }

    [Fact]
    public void EveryGoldenFolder_MapsToAConvertedClass()
    {
        var converted = ConvertedClasses();
        var orphans = GoldenFolders().Where(f => !converted.Contains(f)).Order().ToList();
        Assert.True(orphans.Count == 0,
            $"orphaned golden folders (no live class of that name calls DifferentialGolden.Assert): {string.Join(", ", orphans)}");
    }

    /// <summary>Sanity floor: the committed golden set is not empty (guards a lost or unchecked-out tests/differential).</summary>
    [Fact]
    public void GoldenSet_IsNonEmpty()
    {
        int total = Directory.Exists(DifferentialDir)
            ? Directory.EnumerateFiles(DifferentialDir, "*.out", SearchOption.AllDirectories).Count() : 0;
        Assert.True(total > 0, "no differential goldens on disk under tests/differential");
    }
}
