// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using System.Text;

namespace CobolNet.Tests.Shared;

/// <summary>
/// THE child-process observer for every test project — one rule, one place, and the rule is:
/// <b>a missing observation is not a negative observation.</b>
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THIS TYPE EXISTS (plan §11 A12; DESIGN-test-build-ci.md §3.10). Six separate copies of "start
/// <c>dotnet</c>, wait 30 seconds, return whatever came back" existed across the test tree
/// (<c>CutRunner.RunExit</c>, <c>AcceptDifferentialTests.AcceptRun</c>, <c>CobolNetTestBase.CompileAndRun</c>,
/// and three in the legacy <c>EndToEndTestBase</c>). Every one of them handled a timeout the same wrong way:
/// kill the process, return <i>partial or empty stdout</i>, and let the caller compare it against a golden.
/// A contention timeout therefore reported as a VALUE MISMATCH — indistinguishable from a semantic
/// regression. That is the mechanism behind the observation §11 A12 was opened for: two full Conformance runs
/// on an IDENTICAL tree returned 4159/4160 then 4160/4160, and the named test passed in isolation.
/// </para>
/// <para>
/// The fix is not a longer timeout — it is refusing to manufacture a value out of a run that never finished.
/// A process that timed out, was killed, or failed to launch produced NO OBSERVATION, and this type raises
/// <see cref="HarnessNonObservationException"/> rather than hand a caller something to compare. No assertion
/// anywhere can mistake that for a wrong answer, whatever it chose to assert on.
/// </para>
/// <para>
/// ⚖ ON THE RETRY. <see cref="ObserveOrThrow"/> re-attempts once before giving up, and that is NOT re-rolling
/// a failed assertion — it is re-attempting a MEASUREMENT THAT DID NOT COMPLETE. Nothing about the compiler
/// was learned on the first attempt, so there is no result being discarded. The retry is serialized behind
/// <see cref="RetryGate"/> so it runs with the least contention the run can offer, which is the whole point
/// given that contention is the named cause. Every retry is recorded (see <see cref="LogPath"/>) so the rate
/// is measurable rather than assumed — a silent retry would trade one blind spot for another.
/// </para>
/// </remarks>
internal enum ProcessOutcome
{
    /// <summary>The process ran to completion and its exit code and streams are real.</summary>
    Completed,
    /// <summary>The process exceeded its wall-clock budget and was killed. Its output is TRUNCATED, not wrong.</summary>
    TimedOut,
    /// <summary>The process could not be started at all. Nothing about the program under test was observed.</summary>
    LaunchFailed,
}

/// <summary>One completed observation of a child process — or an explicit statement that none was made.</summary>
internal readonly record struct ProcessObservation(
    ProcessOutcome Outcome, int ExitCode, string Stdout, string Stderr, string Detail)
{
    public bool Completed => Outcome == ProcessOutcome.Completed;
}

/// <summary>
/// Raised when a child process produced NO observation. Distinct from every assertion failure by TYPE and by
/// message, so a contention red can never be read as a semantic one — in a log, in a CI summary, or by a
/// reviewer skimming a verdict line.
/// </summary>
internal sealed class HarnessNonObservationException(string message) : Exception(message);

internal static class ProcessObserver
{
    /// <summary>
    /// The per-process wall-clock budget. Raised from the historic 30s because that value was chosen for a
    /// single run, not for a ~4,000-test suite that spawns a <c>dotnet</c> host per case on a loaded machine —
    /// where process startup alone can eat a large fraction of it. Override with
    /// <c>COBOLNET_RUN_TIMEOUT_MS</c>; a genuine hang still terminates, it just costs more before it does.
    /// </summary>
    public static int TimeoutMs { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("COBOLNET_RUN_TIMEOUT_MS"), out int ms) && ms > 0
            ? ms : 120_000;

    /// <summary>
    /// Where retries and non-observations are recorded, so the rate can be MEASURED across a run rather than
    /// inferred. §11 A12/A12d asked for a distribution; this is the file that supplies one.
    /// </summary>
    public static string LogPath { get; } =
        Environment.GetEnvironmentVariable("COBOLNET_HARNESS_LOG")
        ?? Path.Combine(Path.GetTempPath(), "cobolnet-harness-observations.log");

    /// <summary>Serializes RETRIES only. The first attempt of every test still runs fully parallel, so the
    /// suite pays nothing in the normal case; a retry gets the quietest machine the run can offer.</summary>
    private static readonly SemaphoreSlim RetryGate = new(1, 1);

    private static long _attempts, _retries, _nonObservations;

    /// <summary>Attempt one observation. Never throws for a process-level failure — it REPORTS it.</summary>
    public static ProcessObservation Observe(ProcessStartInfo psi, string? stdinText, int timeoutMs)
    {
        Interlocked.Increment(ref _attempts);
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        psi.RedirectStandardInput = true;
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        // The standard display device writes UTF-8 (CONFORMANCE.md item 59; the PB59 repertoire-identity
        // landing) — decode the capture the same way, or a wide character in a golden reads back as OEM
        // mojibake (Ω arrived as '╬⌐' before this line existed).
        psi.StandardOutputEncoding = new System.Text.UTF8Encoding(false);
        psi.StandardErrorEncoding = new System.Text.UTF8Encoding(false);
        // The ACCEPT device decodes a redirected standard input as UTF-8 (CONFORMANCE.md item 1; kb/Work PB3193), so
        // the harness encodes what it pipes the same way; left unset, .NET on Windows encodes it in the console's input
        // code page. BOM-less, or the first record of every run would begin with U+FEFF.
        psi.StandardInputEncoding = new System.Text.UTF8Encoding(false);

        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception e)                                     // Win32Exception, IOException, …
        {
            return new ProcessObservation(ProcessOutcome.LaunchFailed, -1, "", "",
                $"could not start '{psi.FileName} {psi.Arguments}': {e.GetType().Name}: {e.Message}");
        }
        if (proc is null)
            return new ProcessObservation(ProcessOutcome.LaunchFailed, -1, "", "",
                $"Process.Start returned null for '{psi.FileName} {psi.Arguments}'");

        using (proc)
        {
            // Read both streams asynchronously BEFORE waiting: a child that fills a redirected pipe blocks
            // forever otherwise, which would present as a timeout that is really our own deadlock.
            var outTask = proc.StandardOutput.ReadToEndAsync();
            var errTask = proc.StandardError.ReadToEndAsync();
            try
            {
                if (stdinText is not null) proc.StandardInput.Write(stdinText);
            }
            catch (IOException) { /* the child closed stdin first — legitimate, and not our observation */ }
            try { proc.StandardInput.Close(); } catch (IOException) { /* same */ }

            if (!proc.WaitForExit(timeoutMs))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* already gone */ }
                proc.WaitForExit(5_000);
                // ⛔ The partial stdout is DELIBERATELY NOT RETURNED as output. Handing a caller a truncated
                // report is what turned a contention timeout into a golden mismatch in the first place.
                return new ProcessObservation(ProcessOutcome.TimedOut, -1, "", "",
                    $"process did not exit within {timeoutMs} ms and was killed");
            }
            return new ProcessObservation(ProcessOutcome.Completed, proc.ExitCode,
                outTask.Result, errTask.Result, "");
        }
    }

    /// <summary>
    /// Observe, re-attempting once (serialized) if the first attempt observed nothing, and raising
    /// <see cref="HarnessNonObservationException"/> if the second attempt observes nothing either. Callers get
    /// a real exit code and real streams, or an exception — never a fabricated value.
    /// </summary>
    public static ProcessObservation ObserveOrThrow(ProcessStartInfo psi, string? stdinText = null, int? timeoutMs = null)
    {
        int budget = timeoutMs ?? TimeoutMs;
        var first = Observe(psi, stdinText, budget);
        if (first.Completed) return first;

        Interlocked.Increment(ref _retries);
        Record($"RETRY  {psi.FileName} {psi.Arguments} :: {first.Outcome} :: {first.Detail}");
        RetryGate.Wait();
        ProcessObservation second;
        try { second = Observe(psi, stdinText, budget); }
        finally { RetryGate.Release(); }
        if (second.Completed)
        {
            Record($"RECOVERED  {psi.FileName} {psi.Arguments} :: exit {second.ExitCode}");
            return second;
        }

        Interlocked.Increment(ref _nonObservations);
        Record($"NON-OBSERVATION  {psi.FileName} {psi.Arguments} :: {second.Outcome} :: {second.Detail}");
        throw new HarnessNonObservationException(
            "⛔ HARNESS NON-OBSERVATION — this is NOT a value mismatch and NOT a compiler verdict.\n" +
            $"  command : {psi.FileName} {psi.Arguments}\n" +
            $"  attempt1: {first.Outcome} — {first.Detail}\n" +
            $"  attempt2: {second.Outcome} — {second.Detail}\n" +
            $"  budget  : {budget} ms (set COBOLNET_RUN_TIMEOUT_MS to change)\n" +
            $"  run so far: {Interlocked.Read(ref _attempts)} attempts, {Interlocked.Read(ref _retries)} retried, " +
            $"{Interlocked.Read(ref _nonObservations)} unobserved\n" +
            "  The process never finished, so NOTHING was learned about the program under test. Do not read\n" +
            "  this as a failing assertion: no output was compared. See plan §11 A12 and the observation log\n" +
            $"  at {LogPath}.");
    }

    /// <summary>Append one line to the observation log. Best-effort and lock-tolerant: this is an instrument,
    /// and an instrument that can fail a test run is worse than one that occasionally drops a line.</summary>
    private static void Record(string line)
    {
        try
        {
            File.AppendAllText(LogPath,
                $"{DateTime.UtcNow:O}\t{Environment.ProcessId}\t{line}{Environment.NewLine}", Encoding.UTF8);
        }
        catch (IOException) { /* concurrent append lost — acceptable for an instrument */ }
        catch (UnauthorizedAccessException) { /* same */ }
    }
}
