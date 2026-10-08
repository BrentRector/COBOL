// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Diagnostics;

namespace CobolNet.Tests.Shared;

/// <summary>
/// ⭐ THE COMMITTED TREE — the files git tracks, as repo-root-relative paths with <c>/</c> separators (kb/Work PB735,
/// PB927). Every byte-level sweep of the repository (<c>ConflictMarkerDriftTests</c>, <c>NulByteDriftTests</c>) draws
/// its population from here, and from git only: the alternative, walking the filesystem with a hand-written exclusion
/// list, needs that list because gitignored corpora (<c>tests/external/gnucobol/</c>) carry marker-shaped lines and
/// binary bytes as TEST DATA, whereas <c>git ls-files</c> excludes them structurally.
/// <para>
/// It RAISES when git cannot be run: an empty list from a missing <c>git</c> would read as "the tree is clean"
/// (<c>feedback_verdict_evidence_invariant</c>). It lives beside <see cref="TestRepo"/> rather than in it because it
/// needs <see cref="ProcessObserver"/>, which the benchmark project (it links only <c>TestRepo.cs</c>) does not have.
/// </para>
/// </summary>
internal static class TrackedTree
{
    private static readonly Lazy<IReadOnlyList<string>> s_files = new(List);

    /// <summary>The tracked paths, taken from git once per test assembly.</summary>
    public static IReadOnlyList<string> Files() => s_files.Value;

    /// <summary><c>git ls-files -z</c> at the repo root. NUL-separated so a path with a space, a quote or a non-ASCII
    /// character arrives verbatim rather than in git's C-quoted form. Launched through the ONE observer, not another
    /// private launcher: it drains both pipes asynchronously and RAISES on a launch failure or a timeout instead of
    /// returning an empty string (<c>ProcessObservationDriftTests</c> keeps it collapsed).</summary>
    private static IReadOnlyList<string> List()
    {
        ProcessObservation obs = ProcessObserver.ObserveOrThrow(
            new ProcessStartInfo("git", "ls-files -z") { WorkingDirectory = TestRepo.Root });

        if (obs.ExitCode != 0)
            throw new InvalidOperationException($"`git ls-files -z` in {TestRepo.Root} exited {obs.ExitCode}: {obs.Stderr}");

        return obs.Stdout.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }
}
