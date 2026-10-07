// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using CobolNet.Tests.Shared;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Characterization;

/// <summary>One curated characterization program: a tiny, deterministic COBOL source whose emitted C# (gate 3) and
/// diagnostic surface (gate 2) are snapshotted so a later refactor can PROVE it changed nothing observable.</summary>
/// <param name="Name">The file base name (unique within positive/ or negative/).</param>
/// <param name="Path">Absolute path to the .cob.</param>
/// <param name="Edition">The ISO edition to compile at (from a `&lt;name&gt;.std` sidecar; default 85 — the core edition).</param>
/// <param name="Negative">True for a program that is EXPECTED to fail (a diagnostic snapshot only; no emitted C#).</param>
public sealed record CorpusProgram(string Name, string Path, int Edition, bool Negative)
{
    /// <summary>The stable snapshot key (also the xunit theory display name): <c>&lt;name&gt;.&lt;edition&gt;</c>.</summary>
    public string Key => $"{Name}.{Edition}";
    public override string ToString() => Key;
}

/// <summary>The result of probing one program through the public <see cref="CompilerDriver"/> API.</summary>
public sealed record ProbeResult(bool Ok, IReadOnlyList<string> Diagnostics, string? EmittedCSharp);

/// <summary>Corpus discovery + path resolution for the characterization harness.</summary>
public static class CharacterizationCorpus
{
    /// <summary>The corpus sources: <c>tests/characterization/{positive,negative}/*.cob</c>.</summary>
    public static string CorpusDir => TestRepo.Tests("characterization");

    /// <summary>The committed goldens: <c>tests/Cobol.Net.Tests.Characterization/Snapshots/</c>.</summary>
    public static string SnapshotDir => TestRepo.Tests("Cobol.Net.Tests.Characterization", "Snapshots");

    /// <summary>Every corpus program. A <c>&lt;name&gt;.std</c> sidecar (containing an edition year) overrides the
    /// default edition (85 — most core programs); feature programs that need a later edition carry a sidecar.</summary>
    public static IEnumerable<CorpusProgram> Discover()
    {
        foreach (var (sub, negative) in new[] { ("positive", false), ("negative", true) })
        {
            string dir = Path.Combine(CorpusDir, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (string cob in Directory.EnumerateFiles(dir, "*.cob").OrderBy(p => p, StringComparer.Ordinal))
            {
                string name = Path.GetFileNameWithoutExtension(cob);
                string std = Path.Combine(dir, name + ".std");
                int edition = File.Exists(std) && int.TryParse(File.ReadAllText(std).Trim(), out int e) ? e : 85;
                yield return new CorpusProgram(name, cob, edition, negative);
            }
        }
    }

    /// <summary>xunit <c>[MemberData]</c> source: every program (positive + negative), as
    /// <c>(name, edition, negative)</c> primitives (serializable — clean test display + no xunit warning).</summary>
    public static IEnumerable<object[]> All() =>
        Discover().Select(p => new object[] { p.Name, p.Edition, p.Negative });

    /// <summary>xunit <c>[MemberData]</c> source: positive programs only (they emit C#), as <c>(name, edition)</c>.</summary>
    public static IEnumerable<object[]> Positive() =>
        Discover().Where(p => !p.Negative).Select(p => new object[] { p.Name, p.Edition });

    /// <summary>Resolve a corpus source path from its <c>(name, negative)</c> identity.</summary>
    public static string SourcePath(string name, bool negative) =>
        Path.Combine(CorpusDir, negative ? "negative" : "positive", name + ".cob");
}

/// <summary>Probes a program through the PUBLIC <see cref="CompilerDriver"/> API only (no production seam): a
/// CheckOnly compile captures the WiseOwl COBOL diagnostic surface without Roslyn; a full compile writes the <c>.g.cs</c>
/// sidecar the emit snapshot reads. The Characterization project references the greenfield compiler ONLY, so this
/// never touches a second compiler.</summary>
public static class CompilerProbe
{
    public static ProbeResult Probe(string sourcePath, int edition, bool emit)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Char_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string dll = Path.Combine(dir, "p.dll");
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                sourcePath, dll, DialectLevel: edition, CheckOnly: !emit, SourceFormat: InitialReferenceFormat.Auto));
            var diags = r.Errors.Concat(r.Warnings).ToList();   // errors then warnings; the snapshot sorts for stability
            string? cs = emit && r.GeneratedCsPath is { } p && File.Exists(p) ? File.ReadAllText(p) : null;
            return new ProbeResult(r.Success, diags, cs);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch (IOException) { } }
    }
}

/// <summary>The one snapshot mechanism both gate tests share. When <c>COBOLNET_UPDATE_SNAPSHOTS=1</c> it WRITES the
/// golden (seed/re-baseline, local only — NEVER in CI); otherwise it asserts the committed golden equals the actual
/// (line-endings normalized to LF).</summary>
public static class Snapshot
{
    private static bool Update => Environment.GetEnvironmentVariable("COBOLNET_UPDATE_SNAPSHOTS") == "1";

    private static readonly string RepoRootFwd =
        TestRepo.Root.Replace('\\', '/').TrimEnd('/') + "/";

    /// <summary>Deterministic diagnostic text: the outcome line + the diagnostics PATH-NORMALIZED then sorted stably.
    /// Order is not a behavior (the SET of diagnostics is) — a refactor that reorders two diagnostics must NOT diff,
    /// but one that adds/removes/changes a diagnostic must. Paths are made portable (forward-slash + repo-relative)
    /// so the committed snapshot compares identically on Windows and on CI.</summary>
    public static string FormatDiagnostics(bool ok, IReadOnlyList<string> diagnostics)
    {
        var sb = new StringBuilder();
        sb.Append("ok=").Append(ok ? "true" : "false").Append('\n');
        foreach (string d in diagnostics.Select(NormalizePaths).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(d).Append('\n');
        return sb.ToString();
    }

    /// <summary>Forward-slash all separators and strip the absolute repo root, leaving a repo-relative source path.</summary>
    private static string NormalizePaths(string diagnostic) =>
        diagnostic.Replace('\\', '/').Replace(RepoRootFwd, "").ReplaceLineEndings("\n");

    public static void Assert(string snapshotFile, string actual)
    {
        actual = actual.ReplaceLineEndings("\n");
        if (Update)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotFile)!);
            File.WriteAllText(snapshotFile, actual);
            return;
        }
        Xunit.Assert.True(File.Exists(snapshotFile),
            $"missing characterization snapshot '{Path.GetFileName(snapshotFile)}' — seed it once with "
            + "COBOLNET_UPDATE_SNAPSHOTS=1 then commit.");
        Xunit.Assert.Equal(File.ReadAllText(snapshotFile).ReplaceLineEndings("\n"), actual);
    }
}
