// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.IO;

namespace CobolNet.Runtime;

/// <summary>
/// The single owner of all run-unit-lifetime state (ISO §14.6.1 run unit; DESIGN-runtime-library §2.1).
/// Replaces the five independent process-global static stores: one instance per run unit owning the program
/// table, the EC engine, the EXTERNAL store, the MODULE-NAME stack, the file registry, the clock, and the FUNCTION RANDOM
/// sequence (kb/Work PB307 — the sixth store, which the consolidation first missed). The
/// ambient current run unit is an <see cref="AsyncLocal{T}"/> so a host that thread-hops or runs run units
/// concurrently each see their own (uniform threading model — the former <c>[ThreadStatic]</c>-vs-plain-static
/// split is gone). The pre-existing static facades (<see cref="ProgramRegistry"/>, <see cref="ExceptionState"/>,
/// <see cref="CobolFile"/>, <see cref="CobolModule"/>, <see cref="ExternalStore"/>) remain the emitted surface
/// as thin delegators over <see cref="Current"/>, so generated code is byte-stable pre-G8; <see cref="Current"/>
/// lazily establishes an ambient run unit, which is what makes the emitted
/// <c>ProgramRegistry.Reset(); CobolFile.Init(); …</c> run-unit driver work unchanged (the §14.6.11 implicit
/// CloseAll and the §14.6.12 abnormal-termination surface are runtime-side — <see cref="ProgramTable.RunMain"/>).
/// </summary>
public sealed class RunUnit
{
    private static readonly AsyncLocal<RunUnit?> _current = new();

    /// <summary>The ambient current run unit — lazily established if none (the delegating static facades reach
    /// state through this, so a plain generated <c>Main</c> works without ever naming <see cref="RunUnit"/>).</summary>
    public static RunUnit Current => _current.Value ??= new RunUnit();

    /// <summary>The ambient current run unit, or null when none has been established (diagnostic/host use).</summary>
    internal static RunUnit? TryCurrent => _current.Value;

    public RunUnit()
    {
        Exceptions = new ExceptionEngine(Modules);   // the staged-RAISING slot names its activation (kb/Work PB892)
        Programs = new ProgramTable(this);
        // The text-processing subsystems' cheap, idempotent initialization (Collation/CollationRuntime.cs): the key
        // cache configuration from the environment; the derived tables stay lazy unless COBOL_COLLATION_WARMUP asks.
        Collation.CollationRuntime.Initialize();
    }

    /// <summary>The run-unit program registry (name resolution, §14.6.2.3 state model, CANCEL).</summary>
    public ProgramTable Programs { get; }

    /// <summary>The run-unit-wide LAST EXCEPTION STATUS register + propagation slots (§14.6.13.1.1).</summary>
    public ExceptionEngine Exceptions { get; }

    /// <summary>The run-unit EXTERNAL data store (§8.6.7 / §13.18.22).</summary>
    public ExternalTable External { get; } = new();

    /// <summary>The FUNCTION MODULE-NAME call-name stack (§15.65).</summary>
    public ModuleStack Modules { get; } = new();

    /// <summary>The external-switch store (§12.3.7 GR4 NOTE 1 — switch scope is the run unit).</summary>
    public SwitchStore Switches { get; } = new();

    /// <summary>The run-unit file-connector registry (§9.1; owns the physical-file sharing table).</summary>
    public FileRegistry Files { get; } = new();

    /// <summary>The run unit's ONE current FUNCTION RANDOM sequence (ISO §15.75.3 r4 — the implementor seed belongs
    /// to "the first reference to this function in the run unit"; kb/Work PB307).</summary>
    public RandomSequence Random { get; } = new();

    /// <summary>The run unit's Report Writer control-flow state: the USE BEFORE REPORTING range
    /// (ISO §14.9.49.4 GR10 — <see cref="ReportFlowState"/> records why the range is the RUN UNIT's).</summary>
    public ReportFlowState ReportFlow { get; } = new();

    /// <summary>The run unit's clock (ISO §14.9.1.4 GR7; injectable — a test may set a fixed clock).</summary>
    public IClock Clock { get; set; } = SystemClock.Instance;

    /// <summary>The run unit's LOCALE state (ISO §8.2.1 / §14.6.6; DESIGN-locale-facility §4.3): the two implementor
    /// defaults (determination L2) and the locale current per category — what a LOCALE-based collating sequence
    /// resolves at each use (<see cref="LocaleCollation"/>).</summary>
    public LocaleState Locale { get; } = new();

    /// <summary>The X3.23-1985 OBJECT-TIME (run-time) debug switch (the '85 debug module — deleted 2002, absent 2023;
    /// WiseOwl COBOL models the facility only at <c>--std 85</c>, VCR Table 7 row 7.17). It is implementor-defined; for a
    /// CCVS run it is ON. Default ON so a program compiled WITH DEBUGGING MODE runs its debugging declaratives (the
    /// COMPILE-time switch — SOURCE-COMPUTER … WITH DEBUGGING MODE — is what gates whether the debug scaffolding is
    /// emitted at all; this is the second switch that gates whether emitted triggers actually fire). The emitted
    /// <c>__RunDebug</c> helper reads it, giving a future CLI <c>--debug-mode off</c> override a single home without
    /// perturbing generated code.</summary>
    public bool DebugMode { get; set; } = true;

    private long _exitStatus;

    /// <summary>The run-unit termination status "passed to the operating system" by STOP RUN / a main-program
    /// GOBACK with a status phrase (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10). On .NET the single observable is the
    /// process exit code (<c>Environment.ExitCode</c>), so the STATUS value and the ERROR/NORMAL indication
    /// collapse into this ONE canonical integer (the documented implementor mapping — <c>docs/CONFORMANCE.md</c>
    /// §4.2.16; Annex A "required documented behavior" items 192/193): the STATUS value when specified, else
    /// ERROR ⇒ 1 / NORMAL ⇒ 0. <b>Writing this field flushes to <c>Environment.ExitCode</c> AT THE WRITE SITE</b>
    /// (the setter below), which is what makes the status cross assembly boundaries: STOP RUN terminates the WHOLE
    /// run unit from anywhere (§14.9.42.4 GR6), so a status set by a separately-compiled CALLed module reaches the
    /// process exit code even though the run unit's MAIN program carries no status phrase of its own — the flush
    /// cannot live in the main group's generated <c>Main</c>, which never sees the sibling module's parse tree.
    /// Default 0 (a status-free run unit never writes this field, so <c>Environment.ExitCode</c> keeps its 0
    /// default and the generated <c>Main</c> stays scaffolding-free — the zero-scaffolding invariant, DESIGN §18.16).
    /// The future RETURN-CODE special register writes this SAME field (singular-pattern — one exit-code source AND
    /// one flush, never two).
    /// <para>The host exit code is a 32-bit integer, so the status is CLAMPED to that range through the one
    /// saturating narrowing (<see cref="CobolNum.Position32"/>), never wrapped: a bare <c>(int)</c> turned
    /// <c>STOP RUN WITH ERROR STATUS 4294967296</c> into exit 0 — a NORMAL termination for an ERROR phrase
    /// (kb/Work PB1178). docs/CONFORMANCE.md item 192 documents the range (and a POSIX host's own low-8-bit
    /// report).</para></summary>
    public long ExitStatus
    {
        get => _exitStatus;
        set { _exitStatus = value; Environment.ExitCode = CobolNum.Position32(value); }
    }

    /// <summary>The emitted-surface shim STOP RUN / GOBACK write (kept name-stable over <see cref="Current"/>,
    /// mirroring the <see cref="ExceptionState"/>/<see cref="ProgramRegistry"/> facades): set the run unit's
    /// termination status (ISO §14.9.42.4 GR5 / §14.9.18.4 GR10). The <see cref="ExitStatus"/> setter flushes the
    /// value to <c>Environment.ExitCode</c> at the write site (so the status crosses assembly boundaries).</summary>
    public static void SetExitStatus(long status) => Current.ExitStatus = status;

    /// <summary>Establish a FRESH ambient run unit for the duration of <paramref name="body"/> — the one
    /// lifecycle boundary (begin = a clean run unit; end = the §14.6 implicit CloseAll + ambient restore).
    /// The DEFAULT emitted run-unit driver does not call this (it stays on the lazy-ambient
    /// <c>ProgramRegistry.Reset()</c> path for byte-stability); hosts embedding multiple run units use it.</summary>
    public static void Run(Action<RunUnit> body)
    {
        var ru = new RunUnit();
        var prior = _current.Value;
        _current.Value = ru;
        try { body(ru); }
        finally { ru.Files.CloseAll(); _current.Value = prior; }
    }

    /// <summary>Reset the AMBIENT run unit's program/external/module state — the exact semantics of the
    /// pre-P8 <c>ProgramRegistry.Reset()</c> (clear registrations + EXTERNAL store + MODULE-NAME stack), plus the
    /// FUNCTION RANDOM sequence (§15.75.3 r4); files
    /// and the last-exception status reset through their own emitted entry points, exactly as before). Called
    /// by the <see cref="ProgramRegistry"/> shim from the emitted run-unit driver.</summary>
    public static void ResetCurrent()
    {
        RunUnit ru = Current;
        ru.Programs.Reset();
        ru.Random.Reset();   // §15.75.3 r4: the next RANDOM is again the run unit's FIRST reference (kb/Work PB307)
    }
}
