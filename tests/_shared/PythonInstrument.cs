// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;

namespace CobolNet.Tests.Shared;

/// <summary>
/// Runs one of the repository's Python instruments (<c>scripts/spec/*.py</c>) as a gate.
/// </summary>
/// <remarks>
/// <para>
/// Several of this repository's conformance invariants are enforced by Python scripts that also run by hand —
/// the Annex A.1 register audit, the derived-selector engine's own self-test. Each of them was, at some point,
/// run only when a human remembered (measured 2026-09-01: <c>audit_annex_a1.py</c> appeared in no gate at all),
/// so each gets shelled from the Unit assembly, which runs in the per-commit wave-local gate, in battery phase 1
/// and in both CI unit jobs.
/// </para>
/// <para>
/// ⛔ THE INTERPRETER PROBE IS WRITTEN ONCE. It was private to <c>AnnexA1RegisterDriftTests</c>; the second gate
/// that needed it would have copied it, and two probes disagreeing about which interpreter launches is a
/// difference nobody would ever see reported — <c>feedback_one_rule_one_place</c>.
/// </para>
/// <para>
/// A missing interpreter is a LOUD failure and never a skip: a silent green from a check that never ran is the
/// exact failure mode these gates exist to repair (<c>feedback_verdict_evidence_invariant</c>).
/// </para>
/// </remarks>
internal static class PythonInstrument
{
    /// <summary>
    /// The interpreter name that actually launches here, resolved once. <c>ProcessObserver.Observe</c> is the
    /// non-throwing form on purpose — a probe wants LaunchFailed REPORTED, not raised and retried.
    /// </summary>
    private static readonly Lazy<string> Interpreter = new(() =>
    {
        foreach (string exe in new[] { "python", "python3" })
        {
            var psi = new ProcessStartInfo(exe);
            psi.ArgumentList.Add("--version");
            if (ProcessObserver.Observe(psi, null, ProcessObserver.TimeoutMs).Outcome != ProcessOutcome.LaunchFailed) return exe;
        }

        throw new InvalidOperationException(
            "neither `python` nor `python3` launches here — the repository's spec instruments under "
            + "scripts/spec are Python, and a gate over them cannot run without it. That is a hard failure "
            + "rather than a skip on purpose: an unrun register audit reporting green is what let a "
            + "determination sit under the wrong A.1 obligation for five days (kb/Work A11).");
    });

    /// <summary>Run <c>scripts/&lt;segments&gt;</c> with <paramref name="args"/>, from the repo root.</summary>
    /// <remarks>
    /// The script path is resolved through <see cref="TestRepo"/> and its existence is the CALLER's assertion —
    /// a missing script must fail as "this gate's subject is gone", not as a Python traceback.
    /// </remarks>
    internal static ProcessObservation Run(string script, params string[] args)
    {
        var psi = new ProcessStartInfo(Interpreter.Value) { WorkingDirectory = TestRepo.Root };
        psi.ArgumentList.Add(script);
        foreach (string a in args) psi.ArgumentList.Add(a);
        // ProcessObserver decodes both streams as UTF-8; tell CPython to encode them that way rather than
        // falling back to the host ANSI code page, or a report's § and ⛔ read back mangled.
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        return ProcessObserver.ObserveOrThrow(psi);
    }
}
