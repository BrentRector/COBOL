// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Keeps the child-process observer collapsed to ONE implementation, and proves it can actually tell a
/// non-observation from a wrong answer.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ THE DEFECT THIS GUARDS (plan §11 A12; DESIGN-test-build-ci.md §3.10). Six copies of "start
/// <c>dotnet</c>, wait N seconds, return whatever came back" existed across five test projects, and every one
/// of them, on a timeout, KILLED THE PROCESS AND RETURNED ITS PARTIAL OR EMPTY STDOUT. The caller then compared
/// that against a golden, so a contention timeout was indistinguishable from a semantic regression — which is
/// the mechanism behind two full Conformance runs on an identical tree returning 4159/4160 then 4160/4160.
/// </para>
/// <para>
/// The extraction alone is not the fix, exactly as <c>TestRepoDriftTests</c> records for the repo-root walkers:
/// writing a fifteen-line process launcher is cheaper in the moment than finding the one that already exists,
/// so the copies grow back one project at a time. This test is the half that makes it stay fixed
/// (<c>feedback_one_rule_one_place</c>, <c>feedback_scan_all_similar</c>).
/// </para>
/// </remarks>
[Trait("Sweep", "tree")]
public sealed class ProcessObservationDriftTests
{
    /// <summary>The tell: a test source that both starts a process AND imposes its own bounded wait. Either
    /// half alone stays legal — a generator drift test that shells out to <c>pwsh</c> and waits is answering a
    /// different question than "what did the program under test print".</summary>
    private const string Launch = "Process.Start(";

    /// <summary>
    /// A BOUNDED wait is <c>WaitForExit(</c> with any argument; the argument-less <c>WaitForExit()</c> blocks
    /// until the child really exits and fabricates nothing, so it is not the defect.
    /// <para>⚠ The first draft of this test carried a hand-written list of numeric prefixes
    /// (<c>WaitForExit(1</c>, <c>WaitForExit(3</c>, …), which would have blessed <c>WaitForExit(45000)</c>
    /// silently — a hand-maintained list where a predicate belongs (CLAUDE.md rule 5). The regex IS the rule.</para>
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex BoundedWait =
        new(@"WaitForExit\s*\(\s*[^)\s]", System.Text.RegularExpressions.RegexOptions.Compiled);

    // The observer itself and this test (which must quote the needles). The two generator drift tests that were
    // exempt here no longer impose a bounded wait (kb/Work PB3022: no fixed time limit in a test), so they no longer
    // need to be; named individually so that a new exemption is a deliberate edit here, not a silent inheritance.
    private static readonly string[] Exempt =
    [
        "ProcessObservation.cs", "ProcessObservationDriftTests.cs",
    ];

    private static IEnumerable<string> TestSources() =>
        Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !Exempt.Contains(Path.GetFileName(f)));

    [Fact]
    public void NoTestSource_RunsAProgramUnderItsOwnTimeout()
    {
        var offenders = new List<string>();
        foreach (string file in TestSources())
        {
            string body = File.ReadAllText(file);
            if (!body.Contains(Launch, StringComparison.Ordinal)) continue;
            var wait = BoundedWait.Match(body);
            if (wait.Success)
                offenders.Add($"{Path.GetRelativePath(TestRepo.Root, file)}  ({Launch} … {wait.Value}…)");
        }

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} test source(s) run a child process under a private timeout instead of "
            + $"CobolNet.Tests.Shared.ProcessObserver:{Environment.NewLine}"
            + string.Join(Environment.NewLine, offenders.Select(o => "    " + o))
            + $"{Environment.NewLine}A private timeout returns PARTIAL OUTPUT on expiry, which the caller then "
            + "compares against a golden — a contention timeout reported as a value mismatch (plan §11 A12). "
            + "Use ProcessObserver.ObserveOrThrow — see tests/_shared/ProcessObservation.cs.");
    }

    // ── The observer's own behaviour, proven rather than assumed ───────────────────────────────────────────

    /// <summary>The ordinary case still works: a process that finishes yields its real exit code and streams.</summary>
    [Fact]
    public void ACompletedProcess_IsObservedNormally()
    {
        var obs = ProcessObserver.ObserveOrThrow(ExitsWith(7));
        Assert.Equal(ProcessOutcome.Completed, obs.Outcome);
        Assert.Equal(7, obs.ExitCode);
    }

    /// <summary>stdout is captured, not merely awaited.</summary>
    [Fact]
    public void ACompletedProcess_YieldsItsStdout()
    {
        var obs = ProcessObserver.ObserveOrThrow(Echoes("HELLO-FROM-CHILD"));
        Assert.Contains("HELLO-FROM-CHILD", obs.Stdout, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⛔ THE ONE THAT MATTERS. A process that never finishes must raise — never hand back an empty string that
    /// an <c>Assert.Equal(expected, stdout)</c> would report as a wrong answer.
    /// <c>feedback_green_gates_arent_evidence</c>: this is the guard failing once, on purpose, forever.
    /// </summary>
    [Fact]
    public void AProcessThatNeverFinishes_RaisesInsteadOfReturningEmptyOutput()
    {
        // A short budget so the test costs ~2×1s rather than 2×120s; the retry path is exercised either way.
        var ex = Assert.Throws<HarnessNonObservationException>(
            () => ProcessObserver.ObserveOrThrow(RunsForever(), timeoutMs: 1_000));
        Assert.Contains("HARNESS NON-OBSERVATION", ex.Message, StringComparison.Ordinal);
        Assert.Contains("NOT a value mismatch", ex.Message, StringComparison.Ordinal);
        // Both attempts must be reported — a single-attempt message would mean the retry never ran.
        Assert.Contains("attempt1", ex.Message, StringComparison.Ordinal);
        Assert.Contains("attempt2", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>A process that cannot be started produced no observation either, and says which.</summary>
    [Fact]
    public void AProcessThatCannotStart_RaisesAsALaunchFailure()
    {
        var psi = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "cobolnet-no-such-exe-" + Guid.NewGuid().ToString("N")));
        var ex = Assert.Throws<HarnessNonObservationException>(() => ProcessObserver.ObserveOrThrow(psi, timeoutMs: 1_000));
        Assert.Contains("LaunchFailed", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>The non-fatal probe reports rather than throws, so a caller that wants to inspect can.</summary>
    [Fact]
    public void Observe_ReportsATimeoutWithoutThrowing()
    {
        var obs = ProcessObserver.Observe(RunsForever(), null, 1_000);
        Assert.Equal(ProcessOutcome.TimedOut, obs.Outcome);
        // ⛔ And it is EMPTY on purpose: the partial stdout of a killed run is exactly what must never reach a
        // comparison. If this ever starts returning content, the A12 defect is back.
        Assert.Equal("", obs.Stdout);
    }

    // ⛔ THE SHELL COMMANDS ARE WRITTEN PER-OS, NOT TRANSLATED. The first version of this helper took one
    // cmd.exe string and string-replaced `>nul` for `>/dev/null` on POSIX — which left `exit /b 7` intact, and
    // `/bin/sh` does not accept it. The Windows leg was green, the Linux CI leg was red, and the defect was a
    // TRANSLATION that only covered the token I happened to think of. A cross-platform helper validated on one
    // platform is not cross-platform (`feedback_wsl_linux_repro`: build on Windows, RUN under WSL).
    private static ProcessStartInfo Shell(string windows, string posix) =>
        OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe", $"/c {windows}")
            : new ProcessStartInfo("/bin/sh", $"-c \"{posix}\"");

    /// <summary>A process that terminates immediately with a chosen exit code.</summary>
    private static ProcessStartInfo ExitsWith(int code) => Shell($"exit /b {code}", $"exit {code}");

    /// <summary>A process that writes one marker line to stdout and exits.</summary>
    private static ProcessStartInfo Echoes(string marker) => Shell($"echo {marker}", $"echo {marker}");

    /// <summary>A process that does not finish within any budget this test would set.</summary>
    private static ProcessStartInfo RunsForever() =>
        Shell("ping -n 30 127.0.0.1 >nul", "sleep 30");
}
