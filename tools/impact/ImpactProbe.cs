// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
//
// ⛔ RECORDING BUILDS ONLY (kb/Work PB1683). This file is compiled into an assembly ONLY when the build passes
// -p:CustomAfterMicrosoftCommonTargets=<repo>/tools/impact/ImpactRecording.targets, which
// scripts/spec/record_impact_map.py does in its own detached worktree. No shipped or gated build ever contains it.
// The design — why a per-test hit recorder, what a hit means, and the holes it leaves — is
// docs/rearchitecture/DESIGN-test-build-ci.md §3.13.
#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CobolNet.Impact;

/// <summary>
/// The per-test impact recorder's probe. tools/impact/ImpactInstrumenter rewrites every method that carries
/// sequence points to call <see cref="Hit"/> on entry with a PROBE ID — the index of the set of source files that
/// method's execution proves were exercised (its own documents, plus the documents of every enclosing type's static
/// initializer, since a static field initializer runs once and every later caller depends on its result). A hit is
/// one byte store into the array of the CONTEXT that is running.
///
/// <para><b>Contexts.</b> Inside a test host every copy of this class (one per instrumented assembly) shares ONE
/// <see cref="AsyncLocal{T}"/>, kept in <see cref="AppContext"/> data because the copies share no type but the BCL's.
/// The recording test framework (ImpactTestFramework.cs) sets it when xunit reports a collection, class or test
/// starting, so a hit lands on the innermost of those that is running on this logical flow. A hit with no context —
/// test discovery, a thread that does not flow the ExecutionContext — lands in the AMBIENT array, which the map
/// reports as unattributed rather than dropping.</para>
///
/// <para><b>Child processes.</b> A compiled COBOL program runs as <c>dotnet prog.dll</c> in a child process that loads
/// the instrumented <c>Cobol.Net.Runtime.dll</c>. Every <c>Process.Start</c> in an instrumented assembly is
/// rewritten to <see cref="Start(ProcessStartInfo)"/> and its siblings, which hand the child a file path
/// (<see cref="OutVariable"/>) and queue it on the starting context; the child records into one array for its whole
/// life and writes it at exit, and the framework folds it into the test that started it. A child nobody attributed
/// (started without the rewrite) writes under <c>orphans/</c>, which the map also reports.</para>
/// </summary>
internal static class ImpactProbe
{
    // Not `const`: RuntimeConfigTests scans the runtime assembly for LITERAL environment-variable names and requires
    // each to be a registered runtime setting. These belong to the recorder, never to a run unit.
    internal static readonly string DirVariable = "COBOLNET_IMPACT_DIR";
    internal static readonly string ProbesVariable = "COBOLNET_IMPACT_PROBES";
    internal static readonly string OutVariable = "COBOLNET_IMPACT_OUT";
    private const string ContextKey = "CobolNet.Impact.Context";
    private const string AmbientKey = "CobolNet.Impact.Ambient";

    private static readonly AsyncLocal<Tuple<byte[], ConcurrentQueue<string>>?>? s_context;
    private static readonly byte[]? s_fallback;
    private static readonly int s_probes;
    private static readonly string? s_dir;

    static ImpactProbe()
    {
        s_dir = Environment.GetEnvironmentVariable(DirVariable);
        if (string.IsNullOrEmpty(s_dir) || !int.TryParse(Environment.GetEnvironmentVariable(ProbesVariable), out s_probes))
        {
            return; // an instrumented assembly outside a recording run records nothing
        }

        string? outPath = Environment.GetEnvironmentVariable(OutVariable);
        if (string.IsNullOrEmpty(outPath) && IsTestHost())
        {
            // ⛔ ONE shared state per process, whichever instrumented assembly initializes first. typeof(AppContext)
            // is the one lock object every copy of this class can name.
            lock (typeof(AppContext))
            {
                s_context = AppContext.GetData(ContextKey) as AsyncLocal<Tuple<byte[], ConcurrentQueue<string>>?>;
                if (s_context is null)
                {
                    s_context = new AsyncLocal<Tuple<byte[], ConcurrentQueue<string>>?>();
                    AppContext.SetData(ContextKey, s_context);
                    AppContext.SetData(AmbientKey, new byte[s_probes]);
                }

                s_fallback = (byte[])AppContext.GetData(AmbientKey)!;
            }

            return;
        }

        // A child process: one array for the whole process, written when it exits.
        byte[] hits = new byte[s_probes];
        s_fallback = hits;
        string asm = typeof(ImpactProbe).Assembly.GetName().Name ?? "unknown";
        string target = string.IsNullOrEmpty(outPath)
            ? Path.Combine(s_dir, "orphans", $"{Environment.ProcessId}.{asm}.hits")
            : $"{outPath}.{asm}.hits";
        AppDomain.CurrentDomain.ProcessExit += (_, _) => WriteHits(target, hits);
        // An unhandled exception skips ProcessExit; the hits up to the crash are still what the test exercised.
        AppDomain.CurrentDomain.UnhandledException += (_, _) => WriteHits(target, hits);
    }

    /// <summary>Record that the calling method ran. Inserted by the instrumenter at every method entry.</summary>
    public static void Hit(int id)
    {
        byte[]? hits = s_context?.Value?.Item1 ?? s_fallback;
        if (hits is not null && (uint)id < (uint)hits.Length)
        {
            hits[id] = 1;
        }
    }

    /// <summary>True when this process is recording (a test host or an attributed child).</summary>
    internal static bool Recording => s_fallback is not null;

    /// <summary>The probe-table size of this run.</summary>
    internal static int ProbeCount => s_probes;

    /// <summary>The ambient array (hits outside any context) — null outside a recording test host.</summary>
    internal static byte[]? Ambient => s_context is null ? null : s_fallback;

    /// <summary>Start a new context on the calling logical flow and return it.</summary>
    internal static Tuple<byte[], ConcurrentQueue<string>>? Enter()
    {
        if (s_context is null)
        {
            return null;
        }

        var ctx = Tuple.Create(new byte[s_probes], new ConcurrentQueue<string>());
        s_context.Value = ctx;
        return ctx;
    }

    /// <summary>Write a hit array as the list of probe ids it holds, one per line.</summary>
    internal static void WriteHits(string path, byte[] hits)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var w = new StreamWriter(path);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != 0)
                {
                    w.WriteLine(i);
                }
            }
        }
        catch (IOException)
        {
            // A lost child file is surfaced by the recorder's population check, never by a crashed program.
        }
    }

    // ---- The Process.Start rewrites: every overload the instrumenter redirects lands here. ----

    public static Process Start(ProcessStartInfo psi)
    {
        Attach(psi);
        return Process.Start(psi)!;
    }

    public static Process Start(string fileName) => Start(new ProcessStartInfo(fileName));

    public static Process Start(string fileName, string arguments) => Start(new ProcessStartInfo(fileName, arguments));

    public static Process Start(string fileName, IEnumerable<string> arguments) =>
        Start(new ProcessStartInfo(fileName, arguments));

    public static bool StartInstance(Process process)
    {
        Attach(process.StartInfo);
        return process.Start();
    }

    private static void Attach(ProcessStartInfo psi)
    {
        var ctx = s_context?.Value;
        if (ctx is null || s_dir is null || psi.UseShellExecute)
        {
            return; // unattributed: the child (if instrumented) writes under orphans/
        }

        string path = Path.Combine(s_dir, "children", Guid.NewGuid().ToString("N"));
        psi.Environment[OutVariable] = path;
        ctx.Item2.Enqueue(path);
    }

    private static bool IsTestHost()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (a.GetName().Name is "xunit.execution.dotnet" or "testhost")
            {
                return true;
            }
        }

        return false;
    }
}
