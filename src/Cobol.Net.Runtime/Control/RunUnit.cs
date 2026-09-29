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
/// as thin delegators over <see cref="Current"/>, so generated code is byte-stable pre-G8. <b>A new run unit is a
/// NEW <see cref="RunUnit"/> object, on every path</b> (kb/Work PB1069): the emitted driver's
/// <c>ProgramRegistry.Reset()</c> is <see cref="Begin"/>, and a host's <see cref="Run"/> constructs one too — so no
/// member can be forgotten by a hand-written reset list (the switch, locale and report-flow state all were, and
/// survived into the next run unit a host began in the same process). The §14.6.11 implicit CloseAll and the
/// §14.6.12 abnormal-termination surface are runtime-side — <see cref="ProgramTable.RunMain"/> and <see cref="Terminate"/>.
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

    /// <summary>The run unit's clock (ISO §14.9.1.4 GR7; injectable — a test may set a fixed clock). HOST
    /// CONFIGURATION, not run-unit state: a run unit <see cref="Begin"/> starts inherits it from the ambient run unit
    /// it replaces (<see cref="HostConfiguration"/>).</summary>
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
    /// perturbing generated code. HOST CONFIGURATION like <see cref="Clock"/> (<see cref="HostConfiguration"/>).</summary>
    public bool DebugMode { get; set; } = true;

    /// <summary>The names of the members that are HOST CONFIGURATION rather than run-unit state: set by the host
    /// (or a test) that embeds the runtime, not by any COBOL statement, and carried from the ambient run unit into
    /// the one <see cref="Begin"/> starts in its place — a host that injects a fixed clock and then runs a compiled
    /// program's <c>Main</c> (whose first statement begins the run unit) keeps its clock. Every OTHER member is
    /// fresh in a new run unit; <c>RunUnitStateDriftTests</c> holds both halves by reflection.</summary>
    public static IReadOnlyList<string> HostConfiguration { get; } = [nameof(Clock), nameof(DebugMode)];

    /// <summary>The run unit's FACTORY OBJECTS, one per class, created at the first reference in THIS run unit
    /// (ISO §9.3.14.2: "A factory object is created before it is first referenced by a run unit", and "deleted
    /// after it is last referenced by a run unit" — both lifetimes are the RUN UNIT's; kb/Work PB1069). A factory
    /// used to be a process-lifetime <c>static readonly</c> singleton on its generated class, so its factory data —
    /// and every instance object, ALLOCATEd area and dynamic item reachable from it — outlived the run unit and was
    /// visible to the next one a host began in the same process, contrary to §14.6.11 items 3, 4 and 6. Keyed by
    /// the generated factory type; a run unit executes on one logical thread, so the table needs no lock.</summary>
    private readonly Dictionary<Type, CobolObject> _factoryObjects = [];

    /// <summary>The factory object of the class whose generated factory type is <typeparamref name="T"/> in this
    /// run unit, created on first reference (the emitted <c>__Instance</c> property reads it). Creation runs the
    /// generated constructor — the factory's VALUE initialization and its file registrations (§9.1.4) — once per
    /// run unit.</summary>
    public T FactoryObject<T>() where T : CobolObject, new()
    {
        if (_factoryObjects.TryGetValue(typeof(T), out var existing)) return (T)existing;
        var created = new T();
        _factoryObjects.Add(typeof(T), created);
        // The class's (and every superclass's) METHOD working-storage is static data: its first method activation
        // in this run unit is always preceded by this creation, because a method runs on a factory object or on an
        // instance its factory's New made — so the class adopts its static storage into this run unit HERE.
        created.__AdoptRunUnitStorage(this);
        return created;
    }

    /// <summary>The static-storage resets this run unit has ADOPTED, in adoption order (kb/Work PB1069). Static data
    /// — a RECURSIVE program's or a function's WORKING-STORAGE (§13.5.4 GR1) and a class's METHOD WORKING-STORAGE (OO
    /// deep-dive D3) — is a C# static on its generated type, so it is the ONE kind of run-unit state that a new
    /// <see cref="RunUnit"/> object cannot make fresh by construction. The set is keyed by the reset delegate (two
    /// delegates of one static method are equal), so a class reached through several subclass factories adopts once.</summary>
    private readonly List<Action> _staticStorage = [];
    private readonly HashSet<Action> _adoptedStaticStorage = [];

    /// <summary>Adopt one unit's static storage into this run unit: its <paramref name="reset"/> runs NOW — ISO
    /// §14.6.2.3.2 case 1, "The first time the function, method, or program in which it is described is activated in
    /// a run unit", holds even when the same loaded module served an earlier run unit in this process — and again at
    /// <see cref="Terminate"/>. Adopting the same reset twice is a no-op.</summary>
    public void AdoptStaticStorage(Action reset)
    {
        if (!_adoptedStaticStorage.Add(reset)) return;
        _staticStorage.Add(reset);
        reset();
    }

    /// <summary>Normal and abnormal run-unit termination's runtime epilogue — ONE method for both boundaries
    /// (<see cref="ProgramTable.RunMain"/>'s and <see cref="Run"/>'s), idempotent so a host that nests them runs it
    /// harmlessly twice. ISO §14.6.11 2): "An implicit CLOSE statement without any phrases is executed for each file
    /// that is in the open mode" (also after an abnormal termination, §14.6.12). Then every adopted static storage
    /// returns to its initial state, which RELEASES what it held — §14.6.11 3) "Any storage obtained with an ALLOCATE
    /// statement and not yet released by a FREE statement is released", 4) "All instance objects are destroyed" and
    /// 6) "Any resources occupied by dynamic-capacity tables or dynamic-length elementary items are freed": a static
    /// POINTER, OBJECT REFERENCE or dynamic item is managed memory that stays reachable exactly as long as the
    /// static field refers to it, and everything else the run unit owned is released with this object.</summary>
    public void Terminate()
    {
        Files.CloseAll();
        foreach (var reset in _staticStorage) reset();
    }

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

    /// <summary>Establish a FRESH ambient run unit for the duration of <paramref name="body"/> — the host's
    /// lifecycle boundary (begin = a new run unit, <see cref="StartAfter"/>; end = the §14.6.11 implicit CloseAll +
    /// ambient restore). Hosts embedding several run units use it; the emitted driver uses <see cref="Begin"/>.</summary>
    public static void Run(Action<RunUnit> body)
    {
        var prior = _current.Value;
        var ru = StartAfter(prior);
        _current.Value = ru;
        try { body(ru); }
        finally { ru.Terminate(); _current.Value = prior; }
    }

    /// <summary>Begin a NEW run unit and make it the ambient one — the emitted run-unit driver's first statement
    /// (<c>ProgramRegistry.Reset()</c>). Every member of the new run unit is fresh (program table, EXTERNAL store,
    /// MODULE-NAME stack, files, switches (§12.3.7.4 4) — the implementor-defined scope is the run unit), locale
    /// (§14.6.11 5), FUNCTION RANDOM sequence
    /// (§15.75.3 r4), report flow, exception status, factory objects (§9.3.14.2) and the termination status),
    /// because it is a new object — only the <see cref="HostConfiguration"/> carries over. The run unit it
    /// replaces is not touched: its own termination (<see cref="ProgramTable.RunMain"/>'s finally, or
    /// <see cref="Run"/>'s) closed its files, and dropping the last reference to it releases everything it owned
    /// (§14.6.11 items 3, 4 and 6 — ALLOCATEd storage, instance objects, dynamic items — are managed memory).</summary>
    public static RunUnit Begin()
    {
        var ru = StartAfter(_current.Value);
        _current.Value = ru;
        return ru;
    }

    /// <summary>A new run unit that follows <paramref name="prior"/> (null = none): fresh state, the prior's
    /// <see cref="HostConfiguration"/>, and the process exit code restated from the new run unit's own
    /// termination status (0) — <c>Environment.ExitCode</c> is the flushed copy of <see cref="ExitStatus"/>, so a
    /// status the PREVIOUS run unit's STOP RUN set must not become this run unit's.</summary>
    private static RunUnit StartAfter(RunUnit? prior)
    {
        var ru = new RunUnit();
        if (prior is not null)
        {
            ru.Clock = prior.Clock;
            ru.DebugMode = prior.DebugMode;
        }
        ru.ExitStatus = 0;
        return ru;
    }
}
