// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The run-unit program registry (one INSTANCE per run unit, owned by <see cref="RunUnit"/> — the verbatim port
/// of the pre-P8 static <c>ProgramRegistry</c> bodies): every compiled program unit registers at run-unit start
/// (name, containment, COMMON / INITIAL / RECURSIVE attributes, instance factory), each module whole through its
/// <c>__CobolModule.EnsureRegistered()</c> and <see cref="RegisterModule"/>; CALL resolves names per the
/// §8.4.6.3 scope rules and drives the §14.6.2.3 state model; CANCEL implements §14.9.5. Instances ARE the
/// state: a plain program's cached singleton realizes last-used persistence (§8.6.4 / §14.6.2.3.3); dropping the
/// instance realizes initial-state-on-next-CALL (§14.9.5 GR3); a fresh instance per activation realizes INITIAL
/// (§14.6.2.3.2) and RECURSIVE (deep-dive D3/D4). In-assembly static registration is the primary profile; an
/// unresolved outermost name additionally probes the application directory for a sibling compiled module
/// (<c>&lt;name&gt;.dll</c>) and invokes the <c>EnsureRegistered()</c> of the registrar its
/// <see cref="Repository.CobolRepositoryAttribute"/> record names (<see cref="ProbeSiblingModule"/>) — the
/// implementation-defined §14.9.4.4 GR3b "locate the program" mechanism (owner-approved; a host-composed run unit
/// registers every module itself for AOT/trimming, where the probe never fires).
/// </summary>
public sealed class ProgramTable
{
    private sealed class Node
    {
        public required string Path;        // containment path id (OutermostPath / ContainedPath), unique run-unit-wide
        public required string Name;        // program-name-1 / user-function-name-1 AS WRITTEN — what FUNCTION
                                            // MODULE-NAME reports (§15.65.4 r4 determination, CONFORMANCE DOC-A.1-135)
                                            // and what a diagnostic names. NEVER the AS literal.
        public required string CallName;    // the name externalized to the operating environment (§8.3.2.2 2): the
                                            // PROGRAM-ID/FUNCTION-ID `AS literal-1` when one is written, else Name.
                                            // §14.9.4.4 GR3b routes CALL/CANCEL/ENTRY resolution through §8.3.2.2, so
                                            // THIS is the name they match (per-outermost unique, §8.4.6.3). PB303.
        public string? ParentPath;
        public required string Module;      // the registrar of the module that registered it (Cobol.<S>.__CobolModule):
                                            // what a refused duplicate names (§8.3.2.2; RegisterModule)
        public bool Initial, Common, Recursive;
        public required Func<ICobolProgram?, ICobolProgram> Factory;   // parent instance → new instance
        public ICobolProgram? Instance;     // the cached (last-used) instance; null = initial state on next CALL
        public Action? StaticReset;         // re-initializes a RECURSIVE unit's STATIC working-storage (§13.5.4 GR1
                                            // static data lives on the CLASS, not the per-activation instance, so
                                            // dropping Instance alone cannot realize §14.9.5 GR3 / §14.6.2.3.2 for it)
        /// <summary>A FUNCTION-ID unit — its name is a function-name, invisible to program-name lookups
        /// (§8.4.6.3 first paragraph; kb/Work PB154).</summary>
        public bool IsFunction;
        public int Active;                  // activation depth (GR3f recursion check; GR5 cancel-active check)
        public bool CalledSinceCancel;      // §14.9.5 GR7's own predicate — false = "has not been called in this
                                            // run unit" or "is at present canceled". NOT derivable from Instance:
                                            // a returned RECURSIVE activation restores a NULL slot while the unit
                                            // still holds run-state to cancel (kb/Work PB154).
        public int FormalCount = -1;        // §14.8.2.1 (kb/Work PB133 wave C2b): declared formals; -1 = not registered
        public int RequiredCount;           // formals minus the TRAILING OPTIONAL run (the omissible tail)
        public bool ArgMismatchChecking;    // the ACTIVATED half of GR3d's enabled-in-both gate (TURN at PD entry)
        public BoundaryItem? Returning;     // §14.8.3.3 (kb/Work PB1040): the RETURNING item's registered description; null = none
        public BoundaryItem[]? Formals;     // §14.8.2 (kb/Work PB165): each formal's registered description, in order; null = none stated
        public List<Node> Children = [];    // contained programs, source order (GR4 cancels in REVERSE)
    }

    private readonly RunUnit _owner;
    private readonly Dictionary<string, Node> _byPath = new(ExternalizedNames.Comparer);
    private readonly List<Node> _order = [];
    private readonly HashSet<string> _probedNames = new(ExternalizedNames.Comparer);
    /// <summary>The run unit's ONE module-registration set (DESIGN-external-repository §11.2): the registrar full
    /// name of every module registered into it, by whatever route (the emitted <c>Main</c>, the sibling-module probe,
    /// a host). <see cref="RegisterModule"/> is its only writer.</summary>
    private readonly HashSet<string> _modules = new(StringComparer.Ordinal);
    /// <summary>The module <see cref="RegisterModule"/> is registering now, and the nodes its registrar has
    /// registered so far: committed together, or not at all.</summary>
    private (string Module, List<Node> Nodes, Dictionary<string, Node> ByPath)? _staging;
    /// <summary>Why the probe could not supply a name (a load failure, a module with no registrar, a refused module),
    /// keyed by <see cref="ProbeKey"/> — the externalized name AND the kind of unit the lookup asked for (kb/Work
    /// PB2847): a program lookup and a function lookup of one name are two probes with two reasons ("does not carry the
    /// program X" is not "does not carry the function X"), and CALL and SET … TO ENTRY, which look up a program the
    /// same way, share one. Appended to the EC-PROGRAM-NOT-FOUND / EC-FUNCTION-NOT-FOUND message by every path that
    /// reports a miss (<see cref="NotLocatedReason"/>), so a module that is present but unusable is diagnosable
    /// rather than silent.</summary>
    private readonly Dictionary<string, string> _probeFailure = new(ExternalizedNames.Comparer);

    public ProgramTable(RunUnit owner) => _owner = owner;

    /// <summary>⛔ THE path of an OUTERMOST unit — the run-unit key every <see cref="Register"/>, CALL, CANCEL and
    /// <see cref="RunMain"/> names a unit by, formed here ONCE for the compiler that emits it (kb/Work PB2843). It is
    /// rooted at the unit's EXTERNALIZED name, never its declared one: §8.3.2.2 2) makes the externalized name the one
    /// that identifies an outermost program within a run unit ("Within a run unit, all instances of a given name that
    /// is externalized to the operating environment shall identify the same kind of entity or item"), and
    /// <see cref="RegisterModule"/> refuses a second module carrying one, so the root is unique run-unit-wide. Two
    /// modules may each declare <c>PROGRAM-ID. WORKER</c> (one of them <c>AS "X"</c>); a declared-name root gave both
    /// the key <c>WORKER</c>, and the second module's node replaced the first's, so the first's contained programs
    /// were no longer contained in its caller (EC-PROGRAM-NOT-FOUND). The root is ESCAPED (<c>\</c> → <c>\\</c>,
    /// <c>/</c> → <c>\/</c>) because an AS literal may hold the separator: <c>AS "A/B"</c> must never meet the
    /// containee <c>B</c> of an outermost <c>A</c>. A containee's segment is a COBOL word, which holds neither
    /// character, so the whole path is injective.</summary>
    public static string OutermostPath(string externalizedName) =>
        ExternalizedNames.Form(externalizedName).Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("/", "\\/", StringComparison.Ordinal);

    /// <summary>The path of a CONTAINED unit: its container's path and its program-name. §8.4.6.3: "The names
    /// assigned to programs that are contained directly or indirectly within the same outermost program shall be
    /// unique within that outermost program", so the path is unique under its unique root (<see cref="OutermostPath"/>).</summary>
    public static string ContainedPath(string containerPath, string name) => containerPath + "/" + name;

    /// <summary>⛔ THE run-unit ROOT of a CLASS's keys — the root of its factory and object file connectors, of its
    /// EXTERNAL describers and of its SORT stores — formed here ONCE, the class sibling of <see cref="OutermostPath"/>
    /// (kb/Work PB2920). It is rooted at the class's EXTERNALIZED name (CLASS-ID <c>AS literal-1</c>, §11.3.4 GR1), never
    /// its declared one: §8.3.2.2 2) makes the externalized name the one that identifies a class within a run unit, so
    /// two separately compiled modules that each DECLARE <c>CLASS-ID. ACCT</c> (one of them <c>AS "LEDGER-ACCT"</c>) must
    /// not share a file connector or an EXTERNAL describer, and two that SHARE an externalized name are one class. The
    /// leading <c>/</c> is the namespace: <see cref="OutermostPath"/> escapes every <c>/</c> it holds, so no program's
    /// path, contained or not, can begin with one, and a class's keys never meet a program's.</summary>
    public static string ClassPath(string externalizedName) => "/" + OutermostPath(externalizedName);

    /// <summary>The run-unit identity of one HALF of a class (the OBJECT or the FACTORY paragraph — two source
    /// elements that each describe their own external items, §14.8.4.3): its class's <see cref="ClassPath"/> and the
    /// half's keyword. The <c>/</c> separator cannot occur unescaped in the root, so the identity is injective.</summary>
    public static string ClassHalfPath(string externalizedName, bool factory) =>
        ClassPath(externalizedName) + (factory ? "/FACTORY" : "/OBJECT");

    /// <summary>Register one program unit (emitted once per unit at run-unit start, containers before containees).
    /// <paramref name="staticReset"/> — supplied ONLY by a RECURSIVE-and-not-INITIAL unit with static WS
    /// storage or unit-scoped file connectors (kb/Work PB168) — re-initializes that unit's STATIC WS fields
    /// (§13.5.4 GR1: WS of a non-initial program / a function is static data, ONE copy on the class) and
    /// resets its static file-registration guard: invoked here so §14.6.2.3.2 case 1 (initial state "the
    /// first time the function … or program … is activated in a run unit") holds even when the same loaded
    /// module serves a SECOND run unit in one process, and by <see cref="CancelNode"/> (§14.9.5 GR3 /
    /// §14.6.2.3.2 case 3).</summary>
    /// <param name="externalizedName">The PROGRAM-ID / FUNCTION-ID <c>AS literal-1</c> value (§11.10.4 GR1 /
    /// §11.5.4 GR1) — the name CALL, CANCEL and the program-address-identifier resolve. OMITTED (null) when the
    /// unit wrote no AS phrase, which §8.3.2.2 2) makes the declared name itself.</param>
    /// <param name="returning">The unit's RETURNING item's description (<see cref="BoundaryItem"/>; kb/Work PB1040) —
    /// what the activating CALL's receiver is checked against at §14.9.4.4 GR3 d); null for a unit with no RETURNING
    /// item.</param>
    /// <param name="formals">Each FORMAL's registered description (<see cref="BoundaryItem"/>; kb/Work PB165), in order —
    /// what an activating element's argument is compared with at a dynamic CALL (§14.8.2), and what it is LANDED through
    /// at one (§14.2.3 GR9's second regime and GR10, <see cref="BoundaryItem.Land"/>; kb/Work PB2549). The whole description
    /// is registered only by a unit that checks EC-PROGRAM-ARG-MISMATCH itself (the activated half of GR3d's
    /// enabled-in-both gate); every other unit registers each NUMERIC formal's profile and carrier alone, because the
    /// crossing's conversion is not a check. An entry that states nothing is neither compared nor landed.</param>
    public void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory,
        Action? staticReset = null,
        int formalCount = -1, int requiredCount = 0, bool argMismatchChecking = false,
        bool isFunction = false, string? externalizedName = null, BoundaryItem? returning = null,
        BoundaryItem[]? formals = null)
    {
        // RegisterModule is the only writer of the registration set (DESIGN-external-repository §11.2): a unit is
        // registered only by its module's registrar, inside the module's staging, so a module is registered whole or
        // not at all, and never twice.
        var (module, staged, stagedByPath) = _staging
            ?? throw new InvalidOperationException(
                $"program {name} registered outside ProgramTable.RegisterModule: a module registers only through its "
                + "__CobolModule.EnsureRegistered() (kb/Work PB2097)");
        var node = new Node
        {
            Path = path, Name = name, CallName = ExternalizedNames.Form(externalizedName ?? name), ParentPath = parentPath,
            Module = module,
            Initial = initial, Common = common, Recursive = recursive, Factory = factory,
            FormalCount = formalCount, RequiredCount = requiredCount, ArgMismatchChecking = argMismatchChecking,
            Returning = returning, Formals = formals,
            StaticReset = staticReset, IsFunction = isFunction,
        };
        // A path is one unit's (OutermostPath / ContainedPath are injective), so a second unit under a path the module
        // already staged is a compiler defect: LOUD, never a silent replacement (kb/Work PB2843).
        if (stagedByPath.ContainsKey(path))
            throw new InvalidOperationException(
                $"program {name} registered under the path '{path}', which module {module} already registered (kb/Work PB2843)");
        if (parentPath is not null)
        {
            // A containee whose container is unregistered would SILENTLY drop out of the GR4 cancel cascade
            // and the ParentInstance chain — the registrar emits containers first, so this is a compiler
            // defect and LOUD (kb/Work PB154). A container is in its containee's own module (one source element).
            if (!stagedByPath.TryGetValue(parentPath, out var parent))
                throw new InvalidOperationException(
                    $"program {name} registered before its container {parentPath} — the registrar emits containers first (kb/Work PB154)");
            parent.Children.Add(node);
        }
        stagedByPath.Add(path, node);
        staged.Add(node);
    }

    /// <summary>Register one compiled module into this run unit: the body of every module's one public registration
    /// member, <c>__CobolModule.EnsureRegistered()</c> (DESIGN-external-repository §11.2; kb/Work PB2097). The emitted
    /// <c>Main</c>, the sibling-module probe and a host composing a run unit all reach a module through it, so there is
    /// one way in:
    /// <list type="number">
    ///   <item>a module already in the run unit's registration set (<paramref name="registrar"/>) returns at once: a
    ///     module a host registered is never registered again by a probe, and vice versa;</item>
    ///   <item>a module compiled against another runtime MAJOR or another call ABI is refused
    ///     (<see cref="RuntimeAbi.Skew"/>; design §11.3, §15.4): loading it would fail later inside the callee, or read
    ///     argument images the two sides lay out differently;</item>
    ///   <item><paramref name="register"/> runs into a STAGING list, committed only when none of the module's
    ///     outermost programs or functions is already registered by another module: within a run unit one externalized
    ///     name identifies one instance, of one kind (ISO §8.3.2.2 2), so a second module carrying it is refused whole, naming both
    ///     modules, and nothing of it is registered.</item>
    /// </list>
    /// A refusal is a <see cref="CobolCallException"/> with EC-PROGRAM-NOT-FOUND and the reason: the condition the
    /// activation that needed the module raises anyway (ISO §14.9.4.4 GR3 b), so the carried names are unchanged; reached
    /// from the probe it becomes the probe's failure reason.</summary>
    /// <param name="registrar">The module's registrar full name, <c>Cobol.&lt;S&gt;.__CobolModule</c>: its identity in
    /// the registration set.</param>
    /// <param name="runtimeVersion">The runtime version the module was compiled against.</param>
    /// <param name="callAbi">The call ABI the module was compiled for.</param>
    /// <param name="register">The module's private registrar body: one <see cref="Register"/> per unit, containers
    /// before containees.</param>
    public void RegisterModule(string registrar, string runtimeVersion, int callAbi, Action register)
    {
        if (_modules.Contains(registrar)) return;
        if (RuntimeAbi.Skew(runtimeVersion, callAbi) is { } skew)
            throw new CobolCallException($"module {registrar} cannot run in this run unit: {skew} "
                + "(ISO §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)", "EC-PROGRAM-NOT-FOUND");
        if (_staging is { } busy)
            throw new InvalidOperationException(
                $"module {registrar} registered while module {busy.Module} is registering: a registrar registers only its own units");
        var staged = new List<Node>();
        _staging = (registrar, staged, new Dictionary<string, Node>(ExternalizedNames.Comparer));
        try { register(); }
        finally { _staging = null; }
        // ISO §8.3.2.2 2): "Within a run unit, all instances of a given name that is externalized to the operating
        // environment shall identify the same kind of entity or item", and two source elements naming one externalized
        // name "refer to the same instance". A second module defining an outermost program or function under a name
        // another module already registered (as a program or as a function) is a second definition of that one
        // instance, so the module is refused WHOLE: never a second node rule 4 would shadow, nor a half-registered module.
        foreach (var n in staged)
        {
            if (n.ParentPath is not null) continue;
            foreach (var existing in _order)
                if (existing.ParentPath is null && ExternalizedNames.Same(existing.CallName, n.CallName))
                    throw new CobolCallException(
                        $"module {registrar} cannot run in this run unit: its {(n.IsFunction ? "function" : "program")} "
                        + $"'{n.CallName}' is already registered, as a {(existing.IsFunction ? "function" : "program")}, by "
                        + $"module {existing.Module}, and an externalized name identifies one instance in a run unit "
                        + "(ISO §8.3.2.2 2; §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)",
                        "EC-PROGRAM-NOT-FOUND");
        }
        // Once no outermost name repeats, no path does (OutermostPath roots each at its unique externalized name), so a
        // repeated path is a module whose registrar did not form its paths through OutermostPath / ContainedPath: LOUD,
        // because committing it would replace another module's node (kb/Work PB2843).
        foreach (var n in staged)
            if (_byPath.TryGetValue(n.Path, out var other))
                throw new InvalidOperationException(
                    $"module {registrar} registers the path '{n.Path}' module {other.Module} already registered: a path "
                    + "is formed by ProgramTable.OutermostPath / ContainedPath (kb/Work PB2843)");
        _modules.Add(registrar);
        foreach (var n in staged)
        {
            _byPath.Add(n.Path, n);
            _order.Add(n);
            // Run-unit start = initial state for the unit's static data (§14.6.2.3.2 case 1), and run-unit termination
            // releases it (§14.6.11 3/4/6) — both through the run unit's ONE static-storage adoption (kb/Work PB1069).
            if (n.StaticReset is not null) _owner.AdoptStaticStorage(n.StaticReset);
        }
    }

    /// <summary>Run the run unit's MAIN program (the first program of the compilation group), owning the run-unit
    /// TERMINATION epilogue: the §14.6.12 abnormal-termination surface (a fatal EC escaping the run unit → the
    /// documented diagnostic + the run unit's nonzero <see cref="RunUnit.ExitStatus"/>) and the §14.6.11 implicit CLOSE of
    /// ALL open run-unit connectors. Both are RUN-UNIT-scoped (not main-compilation-group-scoped), so each applies even when the fatal
    /// EC or the open file originates in a SEPARATELY-COMPILED CALLed module whose descriptors the main group's
    /// entry wrapper never saw — the entry wrapper only catches <see cref="StopRun"/> (the normal-termination unwind
    /// boundary; the STOP status itself is the run unit's <see cref="RunUnit.ExitStatus"/>, written by the STOP statement
    /// wherever in the run unit it executes). This is the HOST entry: it never writes the host process's exit code
    /// (DOC-A.1-167; kb/Work PB3163) — the host reads <see cref="RunUnit.ExitStatus"/>. The standalone executable's entry,
    /// which IS the process, is <see cref="RunModule"/>.
    /// <para>The whole of it runs on the run unit's own thread (<see cref="ActivationStack.RunOnRunUnitThread"/>,
    /// kb/Work PB2659), whose stack is sized for COBOL activation frames, so a RECURSIVE program reaches a realistic
    /// depth and the activation past the last one the stack can hold is §14.9.4.4 GR3c's EC-PROGRAM-RESOURCES rather
    /// than a process kill. The thread is transparent: the <see cref="StopRun"/> unwind still reaches the entry
    /// wrapper's catch on the calling thread.</para></summary>
    public void RunMain(string path) => ActivationStack.RunOnRunUnitThread(() => RunOnThisThread(null, path));

    /// <summary>Run a compiled module as the run unit's entry — the standalone executable's <c>Main</c> (kb/Work PB2846):
    /// <see cref="RunMain"/>'s boundary with the module's own registration INSIDE it. <paramref name="ensureRegistered"/>
    /// is the module's registration member (<c>__CobolModule.EnsureRegistered</c>), and a registration the run unit
    /// refuses (a module built against another runtime major or call ABI, or one carrying an already-registered
    /// outermost name; PB2097) is a <see cref="CobolCallException"/> that reaches the same §14.6.12 abnormal-termination
    /// surface as any other fatal condition, because no statement of the entry runs outside the boundary that owns it.
    /// A .NET host never enters here: it activates a program through the program's generated host entry,
    /// <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call</c>, whose activation is <see cref="CallProgram"/>'s outermost arm
    /// (DESIGN-external-repository §4.3, §4.4).
    /// <para>This is the boundary that IS the process (DOC-A.1-167: "A standalone <c>cobol</c> executable IS its own
    /// host"), so it alone delivers the run unit's termination status to the process exit code, once, when the run unit
    /// has ended (normally, by STOP RUN's unwind, or abnormally): the one write of <c>Environment.ExitCode</c> in the
    /// runtime, clamped to the host's 32-bit range by <see cref="CobolNum.Position32"/> (kb/Work PB1178, PB3163).</para></summary>
    /// <param name="ensureRegistered">The module's registration member.</param>
    /// <param name="mainPath">The main program's path. A module with no main program (a library, DESIGN-external-repository
    /// §7.2) has no <c>Main</c> and never enters here.</param>
    public void RunModule(Action ensureRegistered, string mainPath)
    {
        try { ActivationStack.RunOnRunUnitThread(() => RunOnThisThread(ensureRegistered, mainPath)); }
        finally { Environment.ExitCode = CobolNum.Position32(_owner.ExitStatus); }   // StopRun unwinds through here too
    }

    private void RunOnThisThread(Action? ensureRegistered, string path)
    {
        // The standard display device writes UTF-8 (fix-queue PB59; CONFORMANCE.md item 59). ⛔ The rule lives in
        // ONE place — CobolNet.Runtime.IO.StandardStreams — because this `if` used to BE that place and the
        // `cobol` CLI's diagnostic path had no copy of it, so every ISO citation the compiler printed left the
        // process mangled at the OS code page (kb/Work PB899, feedback_two_arm_dispatch).
        IO.StandardStreams.EnsureUtf8();
        try
        {
            // The module's own registration is a statement of the run unit like any other (kb/Work PB2846): a refusal
            // is a CobolCallException (EC-PROGRAM-NOT-FOUND, §14.9.4.4 GR3 b), and the catches below end the run unit.
            ensureRegistered?.Invoke();
            ActivateMain(path);
        }
        // The §14.6.12 abnormal-termination surface for a FATAL condition that reached the run-unit boundary
        // unhandled — every family, so none escapes as a raw CLR crash: the run-unit fatals of EndsRunUnitAbnormally
        // AND the CALL/CANCEL machinery fatals (CobolCallException — EC-PROGRAM-NOT-FOUND / -RECURSIVE-CALL /
        // -CANCEL-ACTIVE / EC-FUNCTION-NOT-FOUND, §14.9.4.4 GR3h → §14.6.13.1.3 #8), which a COBOL main's boundary ends
        // the run unit for and a host's boundary hands its activator (CallFromHost). Runtime-side so a fatal from ANY
        // runtime element reaches the surface, incl. a separately-compiled CALLed module (the settled SSOT §18.16
        // implementor choice).
        catch (Exception e) when (EndsRunUnitAbnormally(e) || e is CobolCallException) { AbnormalTermination(e.Message); }
        finally
        {
            // §14.6.11's runtime epilogue — item 2's implicit CLOSE of EVERY open file in the RUN UNIT (executed even
            // when termination is abnormal, §14.6.12) and items 3/4/6's release of what static storage still holds —
            // is RunUnit.Terminate, the ONE epilogue the RunUnit.Run embedding boundary runs too (idempotent, so
            // nesting them is harmless). StopRun unwinding from Activate passes THROUGH here on its way to the entry
            // wrapper's catch.
            _owner.Terminate();
        }
    }

    /// <summary>Activate the run unit's main program: the ModuleStack main frame, the activation's nonfatal selector and the
    /// §14.9.5 GR7 "called" mark, all restored when the activation ends. Runs inside <see cref="RunOnThisThread"/>'s
    /// termination boundary.</summary>
    private void ActivateMain(string path)
    {
        var n = _byPath[path];
        var inst = n.Instance ??= n.Factory(null);
        n.Active++;
        n.CalledSinceCancel = true;   // §14.9.5 GR7 — the main activation counts as "called in this run unit"
        _owner.Modules.PushMain(n.Name);   // TOP-LEVEL / the run-unit main (§15.65.4 r5/r10)
        // The §14.6.13.1.4 #3 selector for a NONFATAL condition raised at a RUNTIME site is the ACTIVATION's
        // (kb/Work PB367b) — same scope, same boundary, as the ModuleStack frame and the §14.9.28.4 PERFORM
        // depth above it. The main program's activator is the run-unit boundary, so the prior value is null.
        var mainExc = _owner.Exceptions;
        var savedNonfatalDispatcher = mainExc.NonfatalDispatcher;
        mainExc.NonfatalDispatcher = inst;
        // A GOBACK … RAISING in the MAIN program stages for no activation at all (ModuleStack.ActivatingActivation
        // is −1 for the main frame), so nothing can ever take it — §14.9.18.4 GR3: "A RAISING phrase, if
        // specified, is ignored" (kb/Work PB892 Arm B).
        try { inst.Activate(); }
        finally { n.Active--; _owner.Modules.Pop(); mainExc.NonfatalDispatcher = savedNonfatalDispatcher; }
    }

    /// <summary>The §14.6.12 abnormal-run-unit-termination indication (§14.6.11 CLOSE is the caller's finally): the
    /// OS "shall indicate an abnormal termination" — this implementation writes the diagnostic to stderr and sets the run
    /// unit's termination status to the nonzero ERROR value (Annex A ERROR ⇒ 1; the settled §18.16 implementor choice),
    /// which the entry that is the process delivers as its exit code (<see cref="RunModule"/>) and a host reads from
    /// <see cref="RunUnit.ExitStatus"/>; it overrides a STATUS an earlier STOP RUN wrote, as the exit code always did.</summary>
    private void AbnormalTermination(string message)
    {
        Console.Error.WriteLine(AbnormalTerminationReport(message));
        _owner.ExitStatus = AbnormalTerminationStatus;
    }

    /// <summary>The termination status of an abnormal run-unit termination (§14.6.12): Annex A's ERROR ⇒ 1, the settled
    /// §18.16 implementor choice — the run unit's <see cref="RunUnit.ExitStatus"/> (which <see cref="RunModule"/> delivers
    /// as a standalone run unit's process exit code), and the
    /// <see cref="RunUnitEndedException.ExitStatus"/> a host receives.</summary>
    private const int AbnormalTerminationStatus = 1;

    /// <summary>The one wording of an abnormal run-unit termination's report, on stderr for a standalone run unit and as
    /// the <see cref="RunUnitEndedException"/> message for a host.</summary>
    private static string AbnormalTerminationReport(string message) => "abnormal run-unit termination: " + message;

    /// <summary>The run-unit FATALS: the conditions that terminate the run unit abnormally (§14.6.12) when they reach its
    /// outermost activation boundary, at either kind of boundary (<see cref="RunOnThisThread"/> for a COBOL main,
    /// <see cref="CallFromHost"/> for a .NET host). <see cref="CobolFatalException"/> (with its
    /// <see cref="CobolSizeError"/>) is an exception condition that terminates the run unit — a checking-enabled
    /// unresumed fatal EC, §14.6.13.1.3 7), or a runtime raise point such as a NULL BASED reference or an OO
    /// EC-OO-UNIVERSAL; <see cref="CobolImplementorFatalException"/> is an implementor-defined stop that raised NO
    /// exception condition (§14.6.13.1.1 NOTE 3), which ends the run unit through the same boundary because a .NET stack
    /// trace is not a diagnostic a COBOL programmer can act on. A <see cref="CobolCallException"/> is not one of them:
    /// each boundary decides it itself.</summary>
    private static bool EndsRunUnitAbnormally(Exception e) => e is CobolFatalException or CobolImplementorFatalException;

    /// <summary>
    /// Execute one CALL (ISO §14.9.4.4): resolve <paramref name="name"/> from <paramref name="callerPath"/> per
    /// the §8.4.6.3 scope rules (GR3b), enforce the non-recursive re-entry rule (GR3f), pick the instance per the
    /// §14.6.2.3 state model, activate, and apply the INITIAL program's implicit CANCEL on return (§14.9.18 GR2).
    /// Failures raise <see cref="CobolCallException"/> — the call site's ON OVERFLOW / ON EXCEPTION phrase (when
    /// present) converts it to the exception branch (GR3h); otherwise the run unit terminates loudly.
    /// </summary>
    /// <param name="programSpecifiers">The externalized names of the activating element's §12.3.8.2 program-specifiers
    /// (empty when it writes none), stated by a site that held NO signature of the activated program when it was compiled
    /// and either describes its arguments for GR3 d) (kb/Work PB165) or passes one BY CONTENT or BY VALUE (kb/Work
    /// PB2549); null for a site with no argument left to land here — it held the signature, and checked and landed every
    /// argument at compile time, or it passes every argument BY REFERENCE.
    /// §14.8.2.3.2 and §14.8.2.3.3 give "a
    /// program for which there is a program-specifier in the REPOSITORY paragraph of the activating element" rule 2 (the
    /// clause identity, or a COMPUTE, SET or MOVE) and every other program rule 1 (the same length), and which program a
    /// CALL reaches is known only here — a CALL by data-name or through a program-pointer can reach either. The same split
    /// decides §14.2.3 GR9's crossing (kb/Work PB2549), so a site whose activated program's formals it did not know states
    /// them whenever it passes an argument BY CONTENT, checking or not.</param>
    /// <param name="siteSizeTruncationChecking">The CALL statement's EC-SIZE-TRUNCATION checking state, stated by a site whose
    /// activated program's formals it did not know: §14.2.3 GR9/GR10's COMPUTE is the ACTIVATING element's (kb/Work PB640),
    /// so its checking decides whether an argument that overflows its formal raises (§14.7.5) when the landing happens
    /// here (<see cref="LandArguments"/>).</param>
    /// <param name="callerPath">The activating program's path (<see cref="OutermostPath"/>, <see cref="ContainedPath"/>),
    /// whose §8.4.6.3 scope locates <paramref name="name"/>; <c>""</c> for a CALL written in a METHOD, which is contained in
    /// no program; <c>null</c> for an activator outside every program, a .NET host — the run unit's OUTERMOST activation
    /// boundary (<see cref="CallFromHost"/>).</param>
    public void CallProgram(string name, string? callerPath, CobolArg[] args, CobolArg? returning,
        string notFoundEc = "EC-PROGRAM-NOT-FOUND", bool siteArgMismatchChecking = false,
        string[]? programSpecifiers = null, bool siteSizeTruncationChecking = false)
    {
        if (callerPath is null)
            CallFromHost(name, args, returning, notFoundEc, siteArgMismatchChecking, programSpecifiers, siteSizeTruncationChecking);
        else
            Activate(name, callerPath, args, returning, notFoundEc, siteArgMismatchChecking, programSpecifiers, siteSizeTruncationChecking);
    }

    /// <summary>⛔ THE RUN UNIT'S OUTERMOST ACTIVATION BOUNDARY UNDER A .NET HOST (owner decision R48; docs/CONFORMANCE.md
    /// DOC-A.1-167; DESIGN-external-repository §4.1, §4.3 "The exception contract"): the host's counterpart of
    /// <see cref="RunOnThisThread"/>. The activation is <see cref="Activate"/>'s, the one §14.9.4.4 GR3 boundary every COBOL
    /// CALL crosses; only §8.4.6.3's outermost-program rule locates the program, because the activator is in no program.
    /// What this arm adds is the run-unit ENDING: a STOP RUN performs "The operations described in 14.6.11, Normal run unit
    /// termination" and then "Execution of the run unit terminates and control is transferred to the operating system"
    /// (ISO §14.9.42.4 1) and 6)), and a run-unit fatal (<see cref="EndsRunUnitAbnormally"/>) terminates the run unit
    /// abnormally, after which "the runtime system attempts to perform the operations of normal termination as specified in
    /// 14.6.11" and "The operating system shall indicate an abnormal termination of the run unit if such a capability exists
    /// within the operating system" (§14.6.12). Here the host is that system: the run unit performs the §14.6.11 actions
    /// (<see cref="RunUnit.EndForHost"/>), the host's ambient run unit becomes a new one, and the host receives the ONE
    /// documented <see cref="RunUnitEndedException"/> with the termination status and whether the ending was abnormal —
    /// never the internal <see cref="StopRun"/> or fatal signals, never a process exit (§14.2.3 13): the implementor
    /// specifies the mechanisms for a non-COBOL activator).
    /// <para>A <see cref="CobolCallException"/> leaves as itself (the exception contract's item 2): an activation attempt
    /// that failed, or such a condition propagating out of the activated program, which the host may handle and continue in
    /// the same run unit.</para>
    /// <para>The ending is recognized by an exception FILTER and the epilogue runs after the try statement, never in a catch
    /// block: a catch block runs on top of the stack the condition was thrown from (kb/Work PB2659), and a host thread has
    /// only the .NET runtime's stack margin, which the §14.6.11 file closes must not have to share with the deepest
    /// activation of a recursion that exhausted it.</para></summary>
    private void CallFromHost(string name, CobolArg[] args, CobolArg? returning, string notFoundEc,
        bool siteArgMismatchChecking, string[]? programSpecifiers, bool siteSizeTruncationChecking)
    {
        Exception? ending = null;
        try
        {
            Activate(name, callerPath: null, args, returning, notFoundEc, siteArgMismatchChecking, programSpecifiers,
                siteSizeTruncationChecking);
        }
        catch (Exception e) when (e is StopRun || EndsRunUnitAbnormally(e)) { ending = e; }
        if (ending is null) return;
        bool abnormal = ending is not StopRun;
        long status = abnormal ? AbnormalTerminationStatus : _owner.ExitStatus;
        _owner.EndForHost();
        throw new RunUnitEndedException(status, abnormal, abnormal
            ? AbnormalTerminationReport(ending.Message)
            : $"STOP RUN ended the run unit during the host's activation of '{ExternalizedNames.Form(name)}' (ISO §14.9.42.4 6))");
    }

    /// <summary>The §14.9.4.4 GR3 activation boundary every CALL, function activation and host activation crosses
    /// (<see cref="CallProgram"/>, whose parameters it takes; <paramref name="callerPath"/> null for a host).</summary>
    private void Activate(string name, string? callerPath, CobolArg[] args, CobolArg? returning,
        string notFoundEc, bool siteArgMismatchChecking, string[]? programSpecifiers, bool siteSizeTruncationChecking)
    {
        // The ACTIVATING element's EC-EXTERNAL half (§14.8.4.1 / §14.9.4.4 GR3e): the CALL statement guard's
        // checking flags, read HERE, before this activation's own checking scope opens below. The flags are a
        // saved-and-restored scope, so a failed attempt (NOT-FOUND, RECURSIVE-CALL) leaks nothing into the next
        // statement — the invariant the retired pending-mask register had to enforce by hand (kb/Work PB1138).
        var excState = _owner.Exceptions;
        int activatingExternalMask = excState.ExternalActivatingMask;
        var n = ResolveVisible(name, callerPath, wantFunction: notFoundEc == "EC-FUNCTION-NOT-FOUND")
            ?? throw new CobolCallException(
                notFoundEc == "EC-FUNCTION-NOT-FOUND"
                    ? $"FUNCTION '{ExternalizedNames.Form(name)}': the user-defined function could not be located in the run unit"
                      + $"{NotLocatedReason(name, wantFunction: true)} (ISO §8.4.3.2.4 GR6b — EC-FUNCTION-NOT-FOUND)"
                    : $"CALL '{ExternalizedNames.Form(name)}': program not found in the run unit{NotLocatedReason(name, wantFunction: false)} "
                      + "(ISO §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)",
                notFoundEc);
        // §14.9.4.4 GR3c / §8.4.3.2.4 GR6c (kb/Work PB2659): "If the program is located but the resources necessary to
        // execute the program are not available, the EC-PROGRAM-RESOURCES exception condition is set to exist, the program
        // call is not successful". The one resource checked is the stack the activation needs (ActivationStack; Annex A.1
        // items 14 and 89), asked before anything of the activation is set up, so a failed attempt leaves nothing to undo.
        if (!ActivationStack.IsAvailable())
            throw new CobolCallException(
                notFoundEc == "EC-FUNCTION-NOT-FOUND"
                    ? $"FUNCTION '{n.Name}': the stack the function activation needs {ActivationStack.Exhausted} "
                      + "(ISO §8.4.3.2.4 GR6c — EC-PROGRAM-RESOURCES)"
                    : $"CALL '{n.Name}': the stack the program needs {ActivationStack.Exhausted} "
                      + "(ISO §14.9.4.4 GR3c — EC-PROGRAM-RESOURCES)",
                "EC-PROGRAM-RESOURCES");
        if (n.Active > 0 && !n.Recursive)
            throw new CobolCallException(
                $"CALL '{n.Name}': program is already active and has no RECURSIVE attribute (ISO §14.9.4.4 GR3f — EC-PROGRAM-RECURSIVE-CALL)",
                "EC-PROGRAM-RECURSIVE-CALL");
        // §14.8.2.1 via §14.9.4.4 GR3d (kb/Work PB133 wave C2b): the argument COUNT shall equal the formal
        // count except trailing OPTIONAL formals omitted. Raised ONLY "if checking for it is enabled in both
        // the activated program and activating runtime element" — the activated half is registered (the
        // unit's TURN state at its PD entry, the §14.8.4.1 ExternalCheckMask precedent), the activating half
        // rides the call. Unchecked, the call proceeds LENIENTLY: a missing argument behaves as omitted, an
        // excess argument is ignored (the design doc's documented posture). The AS NESTED lane diagnoses the
        // same rule at BIND (COBOLNET1684) and never reaches this.
        bool mismatchChecked = siteArgMismatchChecking && n.ArgMismatchChecking;
        if (n.FormalCount >= 0 && (args.Length > n.FormalCount || args.Length < n.RequiredCount) && mismatchChecked)
            throw new CobolCallException(
                $"CALL '{n.Name}': {args.Length} argument(s) against {n.FormalCount} formal parameter(s) "
                + $"({n.RequiredCount} required) — ISO §14.8.2.1 via §14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
                "EC-PROGRAM-ARG-MISMATCH");
        // §14.8.2 via §14.9.4.4 GR3d (kb/Work PB165): each ARGUMENT meets its FORMAL's registered description at the same
        // point, for the same reason — a violation makes "the program call … not successful", so the callee never runs
        // and never sees a window of the wrong length over the caller's storage. The activating element states each
        // argument's description (and length) and the activated unit its formals' (BoundaryItem.ArgumentViolation over
        // ActivationRelations.CallArgumentViolation): every §14.8.2 rule, with §14.8.2.3.2 / §14.8.2.3.3's elementary rule
        // chosen by whether the activating element has a program-specifier for THIS program — rule 2's clause identity /
        // COMPUTE / SET / MOVE when it does, rule 1's "same length" when it does not. An omitted argument (§14.9.4.4 GR11)
        // has no storage to describe. Unchecked, the call proceeds LENIENTLY, as it does for the argument count above.
        bool specifiedProgram = programSpecifiers is not null
            && Array.Exists(programSpecifiers, s => ExternalizedNames.Same(s, n.CallName));
        if (mismatchChecked && n.Formals is { } formals)
            for (int i = 0; i < args.Length && i < formals.Length; i++)
                if (!args[i].Carrier.IsNull
                    && args[i].Item.ArgumentViolation(args[i].Mode, formals[i], specifiedProgram) is { } why)
                    throw new CobolCallException(
                        $"CALL '{n.Name}': argument {i + 1} ({args[i].Item.Describe()}, {args[i].Mode.ToString().ToUpperInvariant()}) "
                        + $"and the corresponding formal parameter ({formals[i].Describe()}) do not conform: {why} — "
                        + "ISO §14.8.2 via §14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
                        "EC-PROGRAM-ARG-MISMATCH");
        // §14.8.3 via §14.9.4.4 GR3d (kb/Work PB1040, PB165): the RETURNING items' conformance, the same check at the same
        // point — "the program call is not successful", so the callee never runs. §14.8.3 does not split on the
        // program-specifier, so every lane asks the whole rule over the two descriptions (BoundaryItem.ReturningViolation
        // over ActivationRelations.CallReturningViolation), and a result a string carrier takes at the wrong length can
        // never corrupt the receiver's image. Unchecked, the call proceeds and the delivery stores into the receiver's own
        // width (CobolArgAdapt.StoreReturn).
        if (mismatchChecked && returning is { } rcv && n.Returning is { } sent && rcv.Item.ReturningViolation(sent) is { } retWhy)
            throw new CobolCallException(
                $"CALL '{n.Name}': the RETURNING item of the called program ({sent.Describe()}) and the receiving item "
                + $"({rcv.Item.Describe()}) do not conform: {retWhy} — ISO §14.8.3.3 via §14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
                "EC-PROGRAM-ARG-MISMATCH");
        // §14.2.3 GR9 / GR10 (kb/Work PB2549): the records the activating element allocates "during the process of
        // initiating the activation", filled once the program is located and its arguments conform — still before GR3g's
        // transfer, so an EC-SIZE-TRUNCATION the COMPUTE raises is this CALL statement's.
        // A site that states no program-specifiers has no argument left to land here (see the parameter).
        if (programSpecifiers is not null && n.Formals is { } landings)
            args = LandArguments(args, landings, specifiedProgram, siteSizeTruncationChecking);

        ICobolProgram inst;
        bool freshInstance = n.Initial || n.Recursive;
        ICobolProgram? displacedInstance = null;
        if (freshInstance)
        {
            // kb/Work PB133: the registry slot is how CONTAINED programs reach their container's instance
            // (ParentInstance), so a nested activation must RESTORE the instance it displaces — at depth 2
            // the old code left the depth-2 instance in the slot after return, and the still-running depth-1
            // activation's contained callees aliased a DEAD frame's automatic data (§14.6.2.3.2).
            displacedInstance = n.Instance;
            // INITIAL: initial state on EVERY activation (§14.6.2.3.2) — a fresh instance IS the initial state
            // (its WS is INITIAL data, §13.5.4 GR2, emitted as instance fields).
            // RECURSIVE (incl. every FUNCTION, §8.6.6): per-activation instance (deep-dive D3/D4) — the instance
            // carries the AUTOMATIC data (LOCAL-STORAGE, §13.6.4 GR1), the formal carriers, and the PERFORM
            // control state (§14.6.2.2), each in initial state per activation; the unit's WORKING-STORAGE is
            // STATIC data (§13.5.4 GR1) emitted as STATIC fields — ONE copy in last-used state (§14.6.2.3.3)
            // shared across all concurrent and successive activations, untouched by this fresh instance.
            inst = n.Factory(ParentInstance(n));
            n.Instance = inst;   // contained-program factories reach their container through the registry
            if (n.Initial) CancelContained(n);   // contained programs re-initialize too (ISO §11.10.4 GR3)
        }
        else
            inst = n.Instance ??= n.Factory(ParentInstance(n));   // cached singleton — last-used state (§14.6.2.3.3)

        n.Active++;
        n.CalledSinceCancel = true;   // §14.9.5 GR7 — called in this run unit (cleared again by CANCEL)
        _owner.Modules.Push(n.Name, OutermostName(n), n.ParentPath is not null);   // §15.65.4 r7/r8 frame
        // Latch the activating half (§14.8.4.1 / §14.9.4.4 GR3e) as the activated element's ACTIVATOR mask for its
        // ExternalStore.Describe gate; restored on return, so a nested activation never sees its activator's.
        var exc = excState;
        int savedActivator = exc.ActivatorExternalMask;
        exc.ActivatorExternalMask = activatingExternalMask;   // captured before resolution (GR3e)
        // Per-activation scope for the Format-3 exception-checking PERFORM interceptor (ISO §14.9.28.4): snapshot
        // the frame-stack depth so a called program's raise is NOT intercepted by the caller's active WHEN frame
        // (the cross-activation GR1 "in range" reading is a documented STAGED item). TrimPerformTo on return also
        // balances the stack if the callee unwound abnormally past its own pops.
        int savedPerformDepth = exc.PerformDepth;
        // Per-activation scope for the §14.6.13.1.4 #3 selector of a NONFATAL condition raised at a RUNTIME site
        // (kb/Work PB367b): the declaratives that qualify are the ACTIVATED element's (§14.9.49.4 GR3 analyzes
        // "the USE statements in the source element", GR4 a) "the source element that contains the statement that
        // caused the condition"), so the callee's own selector — the interface default's "no qualifying
        // declarative" when it has none — displaces the activator's for the duration of the activation.
        var savedNonfatalDispatcher = exc.NonfatalDispatcher;
        exc.NonfatalDispatcher = inst;
        // The AMBIENT checking flags are per SOURCE TEXT, not per run unit (kb/Work PB841): §7.3.25.4 GR6 enables
        // checking "for the procedure division statements and procedure division headers that follow in the
        // compilation group", so the activated element's statements start from all-off and each sets only what
        // its OWN line enables. The activator's flags — standing because this CALL (or function activation, which
        // comes through here too) runs inside its statement guard — are saved and handed back on return. Taken
        // BEFORE GR3e: its EC-EXTERNAL checks read the latched masks above, never these flags.
        var savedChecking = exc.PushAllCheckingOff();
        // §14.9.4.4 GR3e→GR3g — the ACTIVATION BOUNDARY, and the one place that knows which side of it a
        // failure came from. GR3e's external-conformance check is an activation-attempt step ("the program
        // call is not successful"), so it runs HERE, before the transfer, and its raise stays attributable to
        // this CALL's GR3h. Everything inside inst.Call is GR3g's "control is transferred to the called
        // program", so a CobolCallException escaping it is marked: GR3i then makes every enclosing CALL site
        // ignore its own ON EXCEPTION phrase and leave the condition to §14.6.13.1 (kb/Work PB233 — before
        // this, a callee's unhandled failure ran the ACTIVATOR's imperative-statement-1 and was swallowed).
        bool transferred = false, returned = false, rangeTerminated = false;
        try
        {
            inst.DescribeExternals();   // GR3e — pre-transfer; inside the try so the finally still balances
            transferred = true;         // GR3g — control is transferred to the called program
            inst.Call(args, returning);
            returned = true;
            // §14.9.40.4 GR17 for a USE procedure of the activated element that ended in EXIT PROGRAM or GOBACK (a
            // completion that is not normal, §14.6.13.1.2 1)) while a SORT whose range activated it is active: the
            // activation has returned, and the SORT is terminated here, in the activator (kb/Work PB1177).
            _owner.SortFiles.AfterActivationReturn();
        }
        // ⛔ A FILTER, NEVER A CATCH-AND-RETHROW (kb/Work PB2659). A catch block runs on top of the stack the condition
        // was thrown from, so a `throw;` inside it starts a NESTED dispatch there: a condition crossing N activation
        // boundaries stacked N dispatches, and the EC-PROGRAM-RESOURCES of a recursion that had used the stack overflowed
        // it again on the way out. The filter marks the condition during the dispatch's first pass — in the same
        // innermost-boundary-first order the rethrow chain had — and declines it, so it passes through untouched.
        catch (CobolCallException cx) when (transferred && MarkBoundaryCrossing(cx))
        {
            throw new System.Diagnostics.UnreachableException("MarkBoundaryCrossing declines every condition");
        }
        // §14.9.40.4 GR17 (kb/Work PB1177): a USE procedure invoked while a format 1 SORT is active did not complete
        // normally, and this activation is in the terminated SORT's range (GR10/GR13 — "all statements that are executed
        // as the result of a transfer of control in the range"), so it ends with it: the signal passes through to the
        // SORT statement's landing. Noted by a declining filter, as above, so the INITIAL program's implicit CANCEL
        // in the finally still runs for an activation that ended this way.
        catch (SortTerminationSignal) when (SortTerminationSignal.EndsActivation(ref rangeTerminated))
        {
            throw new System.Diagnostics.UnreachableException("EndsActivation declines every signal");
        }
        finally
        {
            n.Active--; _owner.Modules.Pop();
            exc.ActivatorExternalMask = savedActivator;
            exc.TrimPerformTo(savedPerformDepth);
            exc.NonfatalDispatcher = savedNonfatalDispatcher;
            exc.RestoreChecking(savedChecking);
            if (freshInstance)
            {
                // The fresh instance dies with its activation: the storage it owned (an INITIAL program's items — "An
                // initial item persists while the program is in active state" — and a RECURSIVE one's LOCAL-STORAGE)
                // has ended (ISO §8.6.4; kb/Work PB1216), so a pointer taken into it is no longer a valid address.
                inst.EndStorage(StorageEnd.ActivationEnded);
                n.Instance = displacedInstance;   // kb/Work PB133 — see above
            }
            if (n.Initial && (returned || rangeTerminated)) EndInitialActivation(n, inst);
        }

        // ⛔ NO BOUNDARY DEFAULT HERE (kb/Work PB892 Arm B). A condition the callee staged with GOBACK / EXIT
        // PROGRAM … RAISING names THIS activation's activator (ExceptionEngine — the staged slot carries the
        // ModuleStack identity), so only a pickup emitted in the activating element can take it, and an activator
        // that emitted none never has it raised — §14.9.18.4 GR1 b) for an unchecked activator. A discard here
        // was the CALL-only half of that rule; the INVOKE half had no chokepoint to put one in.
    }

    /// <summary>The end of an INITIAL program's activation — on return, or when the range of a terminated format 1 SORT
    /// that activated it ends (§14.9.40.4 GR17; kb/Work PB1177) — whose activation always starts from a fresh instance,
    /// so the one that ends must not keep its files open.</summary>
    private void EndInitialActivation(Node n, ICobolProgram inst)
    {
        // "If the program … is an initial program, an implicit CANCEL statement referencing that program is
        // executed upon return" (ISO §14.9.18 GR2): close its files (§14.9.5 GR9), cascade (GR4), drop state.
        // No `Active == 0` guard (kb/Work PB1507): an INITIAL program is never recursive — §11.10.3 SR5 forbids
        // the INITIAL clause under a recursive container, so the attribute §11.10.4 GR4 hands down never reaches
        // one, and SR6/0886 keep the two attributes apart in a single program — hence it can never return
        // while an outer activation of itself is running (the guard served that nonconforming shape).
        inst.CloseFiles();
        CancelContained(n);
        n.Instance = null;
        n.CalledSinceCancel = false;   // §14.9.18 GR2 IS a CANCEL — the unit is "at present canceled",
                                       // so a later explicit CANCEL is GR7's no-op
    }

    /// <summary>Mark a <see cref="CobolCallException"/> leaving the called program across its activation boundary
    /// (<see cref="CallProgram"/>'s exception filter), and decline it — always false. GR3d (kb/Work PB615): a formal the
    /// activated element could not adopt is THIS attempt's failure, so the mark is consumed and the next boundary out sees
    /// an ordinary propagated condition. Otherwise the condition escaped the called program's execution, and §14.9.4.4
    /// GR3i makes every enclosing CALL site ignore its ON EXCEPTION phrase (kb/Work PB233).</summary>
    private static bool MarkBoundaryCrossing(CobolCallException cx)
    {
        if (cx.RaisedAtAdoption) cx.RaisedAtAdoption = false;
        else cx.ControlTransferred = true;
        return false;
    }

    /// <summary>⛔ ISO §14.2.3 GR9 AND GR10 AT A CALL WHOSE ACTIVATING ELEMENT DID NOT KNOW THE FORMALS (kb/Work PB2549). GR9
    /// splits a BY CONTENT crossing on the program reached: for "a program for which there is no program-specifier in the
    /// REPOSITORY paragraph of the activating runtime element" the argument "is moved to this allocated record without
    /// conversion" (the argument passes untouched — the formal's adapter reads its image), while for "a program for which
    /// there is a program-specifier" (<paramref name="specifiedProgram"/>) the record has the formal's description and, "if
    /// the formal parameter is numeric, a COMPUTE statement without the ROUNDED phrase" fills it. GR10 makes every BY VALUE
    /// crossing that COMPUTE whatever the program. Each such argument is landed through its formal's registered carrier
    /// (<see cref="BoundaryItem.Land"/>), the landing the activating element performs itself whenever it knows the formal,
    /// with the CALL statement's own EC-SIZE-TRUNCATION state (<paramref name="checking"/>). An omitted argument (§14.9.4.4
    /// GR11) has nothing to send, and a BY REFERENCE one is GR8's shared storage. The caller's array is never written: the
    /// landed arguments are a copy, made only when one changes.</summary>
    private static CobolArg[] LandArguments(CobolArg[] args, BoundaryItem[] formals, bool specifiedProgram, bool checking)
    {
        CobolArg[]? landed = null;
        for (int i = 0; i < args.Length && i < formals.Length; i++)
        {
            var a = args[i];
            if (a.Carrier.IsNull || formals[i].Landing is null) continue;
            if (!(a.Mode is CobolPassMode.Value || a.Mode is CobolPassMode.Content && specifiedProgram)) continue;
            var to = formals[i].Land(a, checking);
            if (to == a) continue;
            landed ??= (CobolArg[])args.Clone();
            landed[i] = to;
        }
        return landed ?? args;
    }

    /// <summary>Resolve a program-address-identifier's ENTRY operand (ISO §8.4.3.13): locate the OUTERMOST
    /// program <paramref name="name"/> names (GR1/GR2 — "the address is that of the outermost program
    /// identified by the externalized program-name"; the §8.4.6.3 rule-4 scope, including the separately-
    /// compiled sibling-module probe). Not locatable → GR4: <paramref name="notFound"/> is set (the emitted
    /// site raises EC-PROGRAM-NOT-FOUND per its checking state) and the result is the NULL program address.
    /// The returned pointer carries the CANONICAL registered name, so pointer equality (§8.8.4.2.16) holds
    /// across differently-cased ENTRY spellings.</summary>
    public ProgramPointer EntryOf(string name, out bool notFound)
    {
        // GR1/GR2 name the EXTERNALIZED program-name explicitly, so this matches — and the pointer carries —
        // Node.CallName (kb/Work PB303; identical to Name for a program with no AS phrase).
        string target = ExternalizedNames.Form(name);
        foreach (var n in _order)
            if (n.ParentPath is null && !n.IsFunction && ExternalizedNames.Same(n.CallName, target)) { notFound = false; return new ProgramPointer(n.CallName); }
        if (ProbeSiblingModule(target, wantFunction: false))
            foreach (var n in _order)
                if (n.ParentPath is null && !n.IsFunction && ExternalizedNames.Same(n.CallName, target)) { notFound = false; return new ProgramPointer(n.CallName); }
        notFound = true;
        return ProgramPointer.Null;   // §8.4.3.13 GR4 — the value is the predefined address NULL
    }

    /// <summary>Resolve a function-address-identifier (ISO §8.4.3.12, <c>ADDRESS OF FUNCTION</c>): locate the
    /// function <paramref name="name"/> names — §8.4.3.12.4 GR2, "For a COBOL function, the address is that of
    /// the function identified by the externalized function-name in its FUNCTION-ID paragraph", which is
    /// <c>Node.CallName</c> on a FUNCTION-ID node (kb/Work PB303). Not locatable → GR4: "If the runtime system
    /// cannot locate the function, the EC-FUNCTION-NOT-FOUND exception condition is set to exist and the value
    /// of the address-identifier is the predefined address NULL" — <paramref name="notFound"/> is set and the
    /// result is <see cref="FunctionPointer.Null"/>.
    /// <para>The PROGRAM twin of <see cref="EntryOf"/>, deliberately written to the same shape over the same
    /// <c>_order</c> scan and the same separately-compiled sibling-module probe (§8.4.6.6 scopes the NAME, and a
    /// separately compiled function definition is found exactly as a separately compiled program is). The ONE
    /// difference is the <see cref="Node.IsFunction"/> discriminator, which §8.4.6.3's first paragraph requires
    /// in both directions: a function-name is not a program-name, so ENTRY must not see a function and this must
    /// not see a program. Function definitions are NOT nested (§9.4 / §10.6), so the <c>ParentPath is null</c>
    /// screen the program twin needs is not written here — a FUNCTION-ID unit is always a source element.</para>
    /// </summary>
    public FunctionPointer FunctionAddressOf(string name, out bool notFound)
    {
        string target = ExternalizedNames.Form(name);
        foreach (var n in _order)
            if (n.IsFunction && ExternalizedNames.Same(n.CallName, target)) { notFound = false; return new FunctionPointer(n.CallName); }
        if (ProbeSiblingModule(target, wantFunction: true))
            foreach (var n in _order)
                if (n.IsFunction && ExternalizedNames.Same(n.CallName, target)) { notFound = false; return new FunctionPointer(n.CallName); }
        notFound = true;
        return FunctionPointer.Null;   // §8.4.3.12.4 GR4 — the value is the predefined address NULL
    }

    /// <summary>The run-time half of the function-pointer signature invariant (ISO §13.18.60.4 GR26 — "A
    /// function-pointer shall contain only the predefined address NULL or the address of a function with the same
    /// signature as that identified by the specified function-prototype-name-1"), enforced statement by statement
    /// by §14.9.39.4 GR14: true when <paramref name="p"/> is NULL (GR26's first alternative) or addresses a
    /// registered function whose declared formal count is <paramref name="expectedFormals"/>.
    /// <para>⛔ GRANULARITY, STATED: the compare is ARITY-ONLY, because a registered unit carries
    /// <c>FormalCount</c>/<c>RequiredCount</c> and nothing finer. Both compile-time compares are richer, because each
    /// has the two bound <c>CalleeSignature</c>s: the COBOLNET1513 prototype-vs-definition check
    /// (<c>BinderDriver.CheckPrototypeSignaturePairs</c>, the full <c>PrototypeSignatures.Same</c> comparison since
    /// kb/Work PB894) and the §14.9.39.3 SR20 compare in <c>SetBinder</c>. This site exists for the one case SR20
    /// cannot reach — a sender resolved from a run-time NAME (§8.4.3.12.4 GR1 a), the identifier-1 form).</para>
    /// <para><c>Math.Max(FormalCount, 0)</c> is LOAD-BEARING, not defensive: <c>ProgramEmitter.EmitEntryWrapper</c>
    /// omits the <c>formalCount:</c> argument entirely when the unit has no formals, so a ZERO-formal function
    /// registers with the "not registered" sentinel −1.</para></summary>
    public bool FunctionSignatureMatches(FunctionPointer p, int expectedFormals)
    {
        if (p.IsNull) return true;   // §13.18.60.4 GR26 — NULL is always an admissible content
        foreach (var n in _order)
            if (n.IsFunction && ExternalizedNames.Same(n.CallName, p.Name!))
                return Math.Max(n.FormalCount, 0) == expectedFormals;
        return false;   // an address no registration backs is not "the address of a function … with the same signature"
    }

    /// <summary>Execute a CALL through a program-pointer (ISO §14.9.4 SR1 — identifier-1 references a
    /// program-pointer item; GR at :26177 — the item "contains the location of the program being called").
    /// A NULL pointer has no program to call: §14.9.4.4's "invalid program address" execution is undefined —
    /// this implementation defines it as the EC-PROGRAM-NOT-FOUND loud failure (never a silent no-op). The
    /// held name is an OUTERMOST program's identity, so the §8.4.6.3 rule-4 leg of the SAME
    /// <see cref="CallProgram"/> resolution finds it from any caller (the singular-pattern rule).</summary>
    public void CallPointer(ProgramPointer target, string callerPath, CobolArg[] args, CobolArg? returning,
        bool siteArgMismatchChecking = false, string[]? programSpecifiers = null, bool siteSizeTruncationChecking = false)
    {
        // §14.9.4.4 GR3b names TWO DISTINCT conditions and the NULL case is the FIRST of them: "If the data item
        // referenced by identifier-1 contains the predefined address NULL, the EC-PROGRAM-PTR-NULL exception
        // condition is set to exist. If the program cannot be located or identifier-1 references a zero-length
        // item, the EC-PROGRAM-NOT-FOUND exception condition is set to exist." This site used to raise
        // EC-PROGRAM-NOT-FOUND for NULL, so a `USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL` declarative
        // could never select. (GR3g's "invalid program address … undefined" governs a NON-null bad address, not
        // NULL — which is why the old message's appeal to it was misplaced.) Table 13: Fatal.
        if (target.IsNull)
        {
            throw new CobolCallException(
                "CALL through a NULL program-pointer: the pointer contains the predefined address NULL "
                + "(ISO §14.9.4.4 GR3b — EC-PROGRAM-PTR-NULL)", "EC-PROGRAM-PTR-NULL");
        }
        CallProgram(target.Name!, callerPath, args, returning, siteArgMismatchChecking: siteArgMismatchChecking,
            programSpecifiers: programSpecifiers, siteSizeTruncationChecking: siteSizeTruncationChecking);
    }

    /// <summary>Activate the function a FUNCTION-POINTER holds — a function-identifier written with
    /// function-pointer-name-1 (ISO §8.4.3.2; kb/Work PB847), the consumer SET Format 8 was missing. §8.4.3.2.4
    /// GR6c: "If function-pointer-name-1 is specified, the runtime system attempts to execute the function at the
    /// address pointed to by function-pointer-name-1. If function-pointer-name-1 is NULL, the EC-FUNCTION-PTR-NULL
    /// exception condition is set to exist, no function is activated" (Table 13: Fatal). A non-NULL pointer holds
    /// the function's externalized name (<see cref="FunctionPointer"/>), so the activation is the SAME function
    /// resolution a function-prototype reference takes — <see cref="CallProgram"/> with GR6b's
    /// EC-FUNCTION-NOT-FOUND as the locate-miss name — never a second lookup path. The program-pointer twin is
    /// <see cref="CallPointer"/>.</summary>
    public void CallFunctionPointer(FunctionPointer target, string callerPath, CobolArg[] args,
        CobolArg? returning, bool siteArgMismatchChecking = false)
    {
        if (target.IsNull)
        {
            throw new CobolCallException(
                "function-identifier through a NULL function-pointer: the pointer contains the predefined address "
                + "NULL, so no function is activated (ISO §8.4.3.2.4 GR6c — EC-FUNCTION-PTR-NULL)",
                "EC-FUNCTION-PTR-NULL");
        }
        CallProgram(target.Name!, callerPath, args, returning, notFoundEc: "EC-FUNCTION-NOT-FOUND",
            siteArgMismatchChecking: siteArgMismatchChecking);
    }

    /// <summary>
    /// Execute one CANCEL target (ISO §14.9.5): a zero-length name is a no-op (GR12); a name not in the run unit
    /// is a no-op (the never-made-available case); an ACTIVE program raises (GR5 — EC-PROGRAM-CANCEL-ACTIVE, the
    /// program is NOT canceled); otherwise contained programs cancel in reverse source order (GR4), the
    /// program's open file connectors close implicitly (GR9 — no optional phrases, no USE procedures), and the
    /// next CALL finds the program in its initial state (GR3). EXTERNAL data is untouched (GR8). A never-called
    /// or already-canceled program is a no-op (GR7).
    /// </summary>
    /// <param name="callerPath">As <see cref="CallProgram"/>'s: <c>null</c> for a .NET host, whose CANCEL locates only an
    /// outermost program (§8.4.6.3).</param>
    public void Cancel(string name, string? callerPath)
    {
        string n = ExternalizedNames.Form(name);
        if (n.Length == 0) return;   // §14.9.5 GR12
        // GR7: a name not located in the run unit is NO ACTION — so the sibling-module probe must not run
        // (it loads assemblies, fires registrars/static resets, and caches the MISS, suppressing a later
        // CALL's legitimate probe — kb/Work PB154); and a FUNCTION name is not a program-name (§8.4.6.3).
        var node = ResolveVisible(n, callerPath, wantFunction: false, probe: false);
        if (node is null) return;
        CancelNode(node);
    }

    private void CancelNode(Node n)
    {
        if (n.Active > 0)
            throw new CobolCallException(
                $"CANCEL '{n.Name}': program is active (ISO §14.9.5.4 GR5 — EC-PROGRAM-CANCEL-ACTIVE; not canceled)",
                "EC-PROGRAM-CANCEL-ACTIVE");
        // GR7 — "has not been called in this run unit or has been called and is at present canceled": NO
        // ACTION, the whole body. The predicate is modeled, never derived from Instance nullness (a returned
        // RECURSIVE activation restores a NULL slot while run-state remains) and never from the registry (a
        // never-called unit's connectors were never registered, so the transient close below would ask for
        // names the registry cannot hold — NIST IC203A CNCL-TEST-04 measured exactly that; kb/Work PB154).
        if (!n.CalledSinceCancel) return;
        for (int i = n.Children.Count - 1; i >= 0; i--)   // GR4 — contained programs, REVERSE source order
            CancelNode(n.Children[i]);
        // GR9 — implicit CLOSE of every open internal file connector, EVEN with no cached instance: a
        // RECURSIVE activation restores a NULL slot on return while its connectors persist (and stay open)
        // in the run-unit registry — the old instance-gated close leaked the OS handle and the post-CANCEL
        // reopen answered '30' (kb/Work PB154). A transient factory instance supplies the emitted CloseFiles
        // surface; its construction runs field initializers only. The parent lookup is TOLERANT here, unlike
        // the activation path's ParentInstance invariant: the §14.9.18 GR2 implicit cancel runs AFTER the
        // activation's finally restored an INITIAL/RECURSIVE container's slot to null, so a canceled
        // containee's container legitimately has no live instance — and CloseFiles never touches container
        // data, only run-unit-registry connectors keyed by name.
        var parentInst = n.ParentPath is not null && _byPath.TryGetValue(n.ParentPath, out var pp)
            ? pp.Instance : null;
        (n.Instance ?? n.Factory(parentInst)).CloseFiles();
        // §8.6.4: a static item persists to "the execution of a CANCEL statement of a program that directly or
        // indirectly contains the items" — the instance's storage ends HERE (contained programs already did, above),
        // and a pointer taken into it is no longer a valid address of storage (§8.6.5; §13.18.5.4 GR4; kb/Work PB1216).
        n.Instance?.EndStorage(StorageEnd.Cancelled);
        n.Instance = null;   // GR3 — the next CALL finds the initial state (GR8: the external store untouched)
        // A RECURSIVE unit's WS is STATIC data on the class (§13.5.4 GR1) — dropping the instance does not
        // touch it; the emitted __ResetStatics reassigns every static WS field/index cell to its initializer
        // so the next CALL finds the §14.6.2.3.2 initial state (GR3; also case 2 — an INITIAL container's
        // activation cascades here via CancelContained). EXTERNAL data stays untouched (GR8: it lives on
        // the run unit's ExternalStore, never in these statics).
        // ⛔ OUTSIDE the instance check (kb/Work PB133 wave A): the activation-slot restore leaves the cached
        // instance correctly NULL once every activation of a recursive unit has returned, so the GR3 reset can
        // no longer hide behind it — the old dangling depth-1 instance was MASKING this (recursive_ws's CANCEL
        // then re-call found WS-CTR=03 where §14.9.5 GR3 derives 0). On a never-called unit the reset is an
        // idempotent no-op, so GR7's no-op posture is preserved.
        n.StaticReset?.Invoke();
        n.CalledSinceCancel = false;   // GR7 — "at present canceled": the next CANCEL is a no-op
    }

    private void CancelContained(Node n)
    {
        for (int i = n.Children.Count - 1; i >= 0; i--) CancelNode(n.Children[i]);
    }

    private ICobolProgram? ParentInstance(Node n) =>
        n.ParentPath is null ? null
        : _byPath.TryGetValue(n.ParentPath, out var p)
            ? p.Instance ?? throw new CobolCallException(
                $"internal: contained program '{n.Name}' activated while its container is not instantiated")
            : null;

    /// <summary>
    /// Resolve a CALL/CANCEL program-name from the calling program per ISO §8.4.6.3. The name matched is the
    /// EXTERNALIZED one (<see cref="Node.CallName"/>): §14.9.4.4 GR3b makes literal-1 "the program-name of the
    /// program being called, as described in 8.3.2.2, User-defined words", and §8.3.2.2 2) makes an AS phrase's
    /// literal that name (kb/Work PB303). Without an AS phrase CallName IS Name, so every AS-less program keeps
    /// the identical behaviour. — the scope arms: (1) a program DIRECTLY
    /// contained in the caller; (2) the caller itself when RECURSIVE (self-call); (3) a COMMON program contained
    /// in a (transitive) container of the caller — except from within that COMMON program or its containees
    /// unless it is recursive; (4) an OUTERMOST program of the run unit (callable from anywhere).
    /// </summary>
    private Node? ResolveVisible(string? name, string? callerPath, bool wantFunction = false, bool probe = true)
    {
        // §8.4.6.3's FIRST paragraph scopes PROGRAM-names to CALL/CANCEL/program-address/end-marker, and a
        // FUNCTION-ID's name is a function-name, never a program-name — so a program lookup must not see a
        // registered function (CANCEL of a UDF name re-initialized its statics before — kb/Work PB154) and a
        // function lookup (the EC-FUNCTION-NOT-FOUND callers) must not see a program. One discriminator per arm.
        string target = ExternalizedNames.Form(name);
        if (target.Length == 0) return null;
        Node? caller = callerPath is not null && _byPath.TryGetValue(callerPath, out var c) ? c : null;

        if (caller is not null)
        {
            foreach (var child in caller.Children)                                   // rule 1
                if (child.IsFunction == wantFunction && ExternalizedNames.Same(child.CallName, target)) return child;
            if (caller.IsFunction == wantFunction && ExternalizedNames.Same(caller.CallName, target) && caller.Recursive)
                return caller;                                                       // rule 2
            for (var anc = ParentOf(caller); anc is not null; anc = ParentOf(anc))   // rule 3 — nearest container first
                foreach (var sib in anc.Children)
                {
                    if (sib.IsFunction != wantFunction || !sib.Common || !ExternalizedNames.Same(sib.CallName, target)) continue;
                    // §8.4.6.3 2)'s exception — written ONCE, shared with the bind-time AS NESTED table (kb/Work PB1460).
                    if (ProgramNameScope.CommonProgramReferable(sib, sib.Recursive, caller, ParentOf)) return sib;
                }
        }
        foreach (var n in _order)                                                    // rule 4 — outermost programs
            if (n.ParentPath is null && n.IsFunction == wantFunction && ExternalizedNames.Same(n.CallName, target)) return n;

        // Rule-4 fallthrough: the run unit may be composed of SEPARATELY COMPILED modules ("a run unit contains
        // one or more runtime modules", ISO §14.6.1; §14.9.4.4 GR3b — the runtime system "attempts to locate"
        // the called program; the locating mechanics beyond the §8.4.6.3 name scope are implementor-defined).
        // Probe the application directory for a sibling compiled module named after the program, register it through
        // its one registration member, and retry rule 4 once.
        if (probe && ProbeSiblingModule(target, wantFunction))
            foreach (var n in _order)
                if (n.ParentPath is null && n.IsFunction == wantFunction && ExternalizedNames.Same(n.CallName, target)) return n;
        return null;
    }

    /// <summary>The run-time locate step for a name no registered module carries (DESIGN-external-repository §11.2;
    /// the implementor-defined part of ISO §14.9.4.4 GR3b and §8.4.3.2.4 GR6b): load the sibling compiled module
    /// <c>&lt;name&gt;.dll</c> from <see cref="AppContext.BaseDirectory"/> (exact name first, then a case-insensitive
    /// scan, because Linux file systems are case-sensitive) into the default
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>, read the registrar its
    /// <see cref="Repository.CobolRepositoryAttribute"/> names, and invoke that registrar's
    /// <c>EnsureRegistered()</c>, the module's one way in (<see cref="RegisterModule"/>). The record is read as
    /// attribute DATA, never instantiated, so a module of another runtime major is refused with its reason rather than
    /// failing to decode.
    /// <para>Returns true when the module registered <paramref name="name"/> (the caller re-resolves). Every other
    /// outcome is false with its reason recorded in <c>_probeFailure</c>, which the EC-PROGRAM-NOT-FOUND /
    /// EC-FUNCTION-NOT-FOUND message carries: no such file is the plain miss (no reason); a file that does not load, a
    /// module with no record or no registrar, a version skew, a registrar that throws, a module <see cref="RegisterModule"/>
    /// refuses, or a module that does not carry the name. None escapes the activation: "not located" is the CALL's
    /// condition (§14.9.4.4 GR3 b). Probed names are cached, hit or miss: one probe per name and kind per run unit.</para></summary>
    private bool ProbeSiblingModule(string name, bool wantFunction)
    {
        if (!_probedNames.Add(ProbeKey(name, wantFunction))) return false;
        string path = "";
        try
        {
            string dir = AppContext.BaseDirectory;
            path = System.IO.Path.Combine(dir, name + ".dll");
            if (!System.IO.File.Exists(path))
                path = System.IO.Directory.EnumerateFiles(dir, "*.dll").FirstOrDefault(f =>
                    string.Equals(System.IO.Path.GetFileNameWithoutExtension(f), name,
                        StringComparison.OrdinalIgnoreCase)) ?? "";   // not a COBOL word: a file name
            if (path.Length == 0) return false;
            var asm = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            var record = asm.GetCustomAttributesData()
                .FirstOrDefault(a => a.AttributeType.FullName == typeof(Repository.CobolRepositoryAttribute).FullName);
            if (record is not { ConstructorArguments: [_, { Value: string runtimeVersion }, { Value: int callAbi }] })
                return ProbeFailed(name, wantFunction, $"'{path}' is not a WiseOwl COBOL module: it carries no CobolRepository record");
            if (RuntimeAbi.Skew(runtimeVersion, callAbi) is { } skew)
                return ProbeFailed(name, wantFunction, $"'{path}': {skew}");
            string? registrar = record.NamedArguments
                .FirstOrDefault(a => a.MemberName == nameof(Repository.CobolRepositoryAttribute.Registrar)).TypedValue.Value as string;
            var ensure = string.IsNullOrEmpty(registrar) ? null
                : asm.GetType(registrar)?.GetMethod("EnsureRegistered", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            if (ensure is null)
                return ProbeFailed(name, wantFunction, $"'{path}' has no registrar {(string.IsNullOrEmpty(registrar) ? "(none named)" : registrar)} with an EnsureRegistered() member");
            ensure.Invoke(null, null);   // → RegisterModule, the one way in
            foreach (var n in _order)
                if (n.ParentPath is null && n.IsFunction == wantFunction && ExternalizedNames.Same(n.CallName, name)) return true;
            return ProbeFailed(name, wantFunction, $"'{path}' does not carry the {(wantFunction ? "function" : "program")} {name}");
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException or BadImageFormatException
                                      or TargetInvocationException or TypeLoadException or ArgumentException)
        {
            // IOException covers FileNotFoundException (a file removed between the listing and the load) and
            // FileLoadException; a RegisterModule refusal or a throwing registrar arrives as TargetInvocationException;
            // ArgumentException is a registrar name Assembly.GetType cannot parse (a hand-made or damaged record).
            return ProbeFailed(name, wantFunction, $"'{path}': {e.GetBaseException().Message}");
        }
    }

    /// <summary>⛔ THE ONE KEY of the probe's two per-lookup tables (<see cref="_probedNames"/>, <see cref="_probeFailure"/>):
    /// the externalized name and the KIND of unit asked for (kb/Work PB2847). §8.4.6.3's first paragraph makes a
    /// program-name and a function-name different kinds of name, so a probe for one is not a probe for the other.</summary>
    private static string ProbeKey(string name, bool wantFunction) => $"{(wantFunction ? 'F' : 'P')}:{name}";

    private bool ProbeFailed(string name, bool wantFunction, string reason)
    {
        _probeFailure[ProbeKey(name, wantFunction)] = reason;
        return false;
    }

    /// <summary>The probe's reason a module beside the application could not supply <paramref name="name"/> as the kind
    /// of unit <paramref name="wantFunction"/> names, as a suffix for the not-found message the activation raises
    /// (empty when no module was found at all). The one source of that suffix for every not-found path: CALL, a
    /// function reference, and the three address-identifiers (<see cref="AddressNotLocated"/>).</summary>
    internal string NotLocatedReason(string name, bool wantFunction) =>
        _probeFailure.TryGetValue(ProbeKey(ExternalizedNames.Form(name), wantFunction), out var reason)
            ? $"; the sibling module was not usable: {reason}" : "";

    /// <summary>⛔ THE ONE WORDING of a miss on an address-identifier (kb/Work PB2847): ISO §8.4.3.13.4 GR4 for a
    /// <c>PROGRAM</c> / <c>ENTRY</c> operand, §8.4.3.12.4 GR4 for <c>FUNCTION</c> — "If the runtime system cannot locate
    /// the program [function], the EC-PROGRAM-NOT-FOUND [EC-FUNCTION-NOT-FOUND] exception condition is set to exist and
    /// the value of the address-identifier is the predefined address NULL" — with the probe's reason, so every
    /// surface that raises it (<c>ADDRESS OF PROGRAM</c> as a CALL argument or an operand, <c>SET … TO ENTRY</c>,
    /// <c>ADDRESS OF FUNCTION</c>) tells the user why the module was not found.</summary>
    internal string AddressNotLocated(string name, bool function) =>
        $"ADDRESS OF {(function ? "FUNCTION" : "PROGRAM")} '{ExternalizedNames.Form(name)}': "
        + $"the {(function ? "function" : "program")} could not be located{NotLocatedReason(name, function)}";

    private Node? ParentOf(Node n) =>
        n.ParentPath is not null && _byPath.TryGetValue(n.ParentPath, out var p) ? p : null;

    /// <summary>The outermost (top-level) program-id name of a node's compilation-unit containment chain
    /// (ISO §15.65.4 r7 — MODULE-NAME CURRENT). Equals the node's own name for a top-level program.</summary>
    private string OutermostName(Node n)
    {
        var top = n;
        for (var p = ParentOf(top); p is not null; p = ParentOf(top)) top = p;
        return top.Name;
    }
}
