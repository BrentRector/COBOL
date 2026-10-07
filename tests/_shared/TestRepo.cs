// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE repo-root locator for every test project — one rule, one place.
/// </summary>
/// <remarks>
/// <para>
/// Before this type there were SIXTEEN private <c>RepoRoot()</c> walkers across five test projects, answering the
/// same question with FIVE different sentinels: <c>Cobol.Net.sln</c>, <c>tests/version-matrix</c>,
/// <c>tests/nist</c>, <c>src/Cobol.Net.Compiler</c> and <c>PROMPT.md</c>. Every one of those is a PROXY for "the
/// repo root", and each proxy is a separate way for the answer to drift: move or rename any one of them and only
/// the tests keyed on that proxy break, silently and locally. That is the duplication anti-pattern the DA wave
/// recorded as <c>feedback_one_rule_one_place</c> — a single construct failing usually means one rule was written
/// down in more than one place, and the extraction IS the fix.
/// </para>
/// <para>
/// This file is not owned by any one test project: <c>tests/Directory.Build.props</c> links it into every project
/// under <c>tests/</c>, so a NEW test project gets <see cref="Root"/> with no wiring at all.
/// <c>TestRepoDriftTests</c> keeps it collapsed — it fails if any test source grows a private walker again.
/// </para>
/// </remarks>
internal static class TestRepo
{
    /// <summary>
    /// The repository root, located once per test assembly.
    /// </summary>
    /// <remarks>
    /// The marker is <c>Cobol.Net.sln</c> — the one file that genuinely defines the root rather than merely
    /// living at it — CORROBORATED by the co-presence of <c>src/</c> and <c>tests/</c>. The corroboration is not
    /// ceremony: a lone <c>.sln</c> can be copied anywhere (a build drop, a packaging staging directory), and a
    /// walker that accepted it would resolve to a tree with no sources in it, then fail with a file-not-found
    /// raised far away from the cause.
    /// </remarks>
    public static string Root { get; } = Locate();

    /// <summary>An absolute path under the repo root, from root-relative segments.</summary>
    public static string At(params string[] segments) => Path.Combine([Root, .. segments]);

    /// <summary><c>src/</c> — the five product assemblies.</summary>
    public static string Src(params string[] segments) => At(["src", .. segments]);

    /// <summary>
    /// The build configuration (<c>Debug</c> | <c>Release</c>) and target framework this test assembly was
    /// built for, read from its own output path.
    /// </summary>
    /// <remarks>
    /// A test that drives a SIBLING project's binary — the <c>cobol</c> driver, say — must run the binary
    /// built in the SAME configuration, or a Release CI leg silently tests a stale Debug build. Hard-coding
    /// "Debug" is the bug; re-deriving the answer per test file is the duplication <see cref="TestRepo"/>
    /// exists to prevent, and <c>TestRepoDriftTests</c> fails any test source that climbs from
    /// <c>AppContext.BaseDirectory</c> on its own. So the derivation lives HERE, once.
    /// </remarks>
    public static string Configuration { get; } = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;

    /// <inheritdoc cref="Configuration"/>
    public static string TargetFramework { get; } = new DirectoryInfo(AppContext.BaseDirectory).Name;

    /// <summary>A path inside a <c>src/</c> project's build output for THIS configuration and framework —
    /// <c>SrcBin("Cobol.Net.Cli", "cobol.exe")</c>.</summary>
    public static string SrcBin(string project, params string[] segments) =>
        Src([project, "bin", Configuration, TargetFramework, .. segments]);

    /// <summary><c>tests/</c> — the corpora, the goldens and the test projects.</summary>
    public static string Tests(params string[] segments) => At(["tests", .. segments]);

    /// <summary><c>docs/</c> — the plan, the design SSOTs and the published references.</summary>
    public static string Docs(params string[] segments) => At(["docs", .. segments]);

    /// <summary><c>scripts/</c> — the generators and gates.</summary>
    public static string Scripts(params string[] segments) => At(["scripts", .. segments]);

    /// <summary><c>specs/</c> — the public ISO transcription (<c>ISO_COBOL.md</c>).</summary>
    public static string Specs(params string[] segments) => At(["specs", .. segments]);

    /// <summary><c>kb/</c> — the knowledge base, and under <c>kb/Work/</c> THE work register.</summary>
    public static string Kb(params string[] segments) => At(["kb", .. segments]);

    /// <summary><c>tests/version-matrix/</c> — constructs, reserved words, the traceability inventory.</summary>
    public static string VersionMatrix(params string[] segments) => Tests(["version-matrix", .. segments]);

    /// <summary><c>tests/nist/</c> — the NIST CCVS corpus, its manifest and its goldens.</summary>
    public static string Nist(params string[] segments) => Tests(["nist", .. segments]);

    private static string Locate()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "Cobol.Net.sln"))
                && Directory.Exists(Path.Combine(d.FullName, "src"))
                && Directory.Exists(Path.Combine(d.FullName, "tests")))
            {
                return d.FullName;
            }
        }

        throw new InvalidOperationException(
            $"repo root not found above '{AppContext.BaseDirectory}' — looked for a directory holding "
            + "Cobol.Net.sln alongside src/ and tests/.");
    }
}
