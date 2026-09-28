// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The run-unit program registry (one INSTANCE per run unit, owned by <see cref="RunUnit"/> — the verbatim port
/// of the pre-P8 static <c>ProgramRegistry</c> bodies): every compiled program unit registers at run-unit start
/// (name, containment, COMMON / INITIAL / RECURSIVE attributes, instance factory); CALL resolves names per the
/// §8.4.6.3 scope rules and drives the §14.6.2.3 state model; CANCEL implements §14.9.5. Instances ARE the
/// state: a plain program's cached singleton realizes last-used persistence (§8.6.4 / §14.6.2.3.3); dropping the
/// instance realizes initial-state-on-next-CALL (§14.9.5 GR3); a fresh instance per activation realizes INITIAL
/// (§14.6.2.3.2) and RECURSIVE (deep-dive D3/D4). In-assembly static registration is the primary profile; an
/// unresolved outermost name additionally probes the application directory for a sibling compiled module
/// (<c>&lt;name&gt;.dll</c>) and invokes its public <c>__CobolModule.Register()</c> registrar — the
/// implementation-defined §14.9.4.4 GR3b "locate the program" mechanism (owner-approved; a
/// prebuilt-static-registry profile remains possible for AOT/trimming, where the probe never fires).
/// </summary>
public sealed class ProgramTable
{
    private sealed class Node
    {
        public required string Path;        // containment path id, e.g. "OUTER/INNER" (unique run-unit-wide)
        public required string Name;        // program-name-1 / user-function-name-1 AS WRITTEN — what FUNCTION
                                            // MODULE-NAME reports (§15.65.4 r4 determination, CONFORMANCE DOC-A.1-135)
                                            // and what a diagnostic names. NEVER the AS literal.
        public required string CallName;    // the name externalized to the operating environment (§8.3.2.2 2): the
                                            // PROGRAM-ID/FUNCTION-ID `AS literal-1` when one is written, else Name.
                                            // §14.9.4.4 GR3b routes CALL/CANCEL/ENTRY resolution through §8.3.2.2, so
                                            // THIS is the name they match (per-outermost unique, §8.4.6.3). PB303.
        public string? ParentPath;
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
        public List<Node> Children = [];    // contained programs, source order (GR4 cancels in REVERSE)
    }

    private readonly RunUnit _owner;
    private readonly Dictionary<string, Node> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Node> _order = [];
    private readonly HashSet<string> _probedModules = new(StringComparer.OrdinalIgnoreCase);

    public ProgramTable(RunUnit owner) => _owner = owner;

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
    public void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory,
        Action? staticReset = null,
        int formalCount = -1, int requiredCount = 0, bool argMismatchChecking = false,
        bool isFunction = false, string? externalizedName = null)
    {
        var node = new Node
        {
            Path = path, Name = name, CallName = externalizedName ?? name, ParentPath = parentPath,
            Initial = initial, Common = common, Recursive = recursive, Factory = factory,
            FormalCount = formalCount, RequiredCount = requiredCount, ArgMismatchChecking = argMismatchChecking,
            StaticReset = staticReset, IsFunction = isFunction,
        };
        _byPath[path] = node;
        _order.Add(node);
        if (parentPath is not null)
        {
            // A containee whose container is unregistered would SILENTLY drop out of the GR4 cancel cascade
            // and the ParentInstance chain — the registrar emits containers first, so this is a compiler
            // defect and LOUD (kb/Work PB154).
            if (!_byPath.TryGetValue(parentPath, out var parent))
                throw new InvalidOperationException(
                    $"program {name} registered before its container {parentPath} — the registrar emits containers first (kb/Work PB154)");
            parent.Children.Add(node);
        }
        staticReset?.Invoke();   // run-unit start = initial state for the unit's static data (§14.6.2.3.2 case 1)
    }

    /// <summary>Run the run unit's MAIN program (the first program of the compilation group), owning the run-unit
    /// TERMINATION epilogue: the §14.6.12 abnormal-termination surface (a fatal EC escaping the run unit → the
    /// documented diagnostic + a nonzero process exit) and the §14.6.11 implicit CLOSE of ALL open run-unit
    /// connectors. Both are RUN-UNIT-scoped (not main-compilation-group-scoped), so each applies even when the fatal
    /// EC or the open file originates in a SEPARATELY-COMPILED CALLed module whose descriptors the main group's
    /// entry wrapper never saw — the entry wrapper only catches <see cref="StopRun"/> (the normal-termination unwind
    /// boundary; the STOP status itself is flushed run-unit-side by the <see cref="RunUnit.ExitStatus"/> setter).</summary>
    public void RunMain(string path)
    {
        // The standard display device writes UTF-8 (fix-queue PB59; CONFORMANCE.md item 59). ⛔ The rule lives in
        // ONE place — CobolNet.Runtime.IO.StandardStreams — because this `if` used to BE that place and the
        // `cobol` CLI's diagnostic path had no copy of it, so every ISO citation the compiler printed left the
        // process mangled at the OS code page (kb/Work PB899, feedback_two_arm_dispatch).
        IO.StandardStreams.EnsureUtf8();
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
        try
        {
            // A GOBACK … RAISING in the MAIN program stages for no activation at all (ModuleStack.ActivatingActivation
            // is −1 for the main frame), so nothing can ever take it — §14.9.18.4 GR3: "A RAISING phrase, if
            // specified, is ignored" (kb/Work PB892 Arm B).
            try { inst.Activate(); }
            finally { n.Active--; _owner.Modules.Pop(); mainExc.NonfatalDispatcher = savedNonfatalDispatcher; }
        }
        // The §14.6.12 abnormal-termination surface for a FATAL condition that reached the run-unit boundary
        // unhandled — BOTH families, so neither escapes as a raw CLR crash: exception-condition fatals
        // (CobolFatalException — a checking-enabled unresumed EC, §14.6.13.1.3 #7, or a raw runtime raise-point like
        // a NULL BASED deref / an OO __CobolInvoke EC-OO-UNIVERSAL) AND the CALL/CANCEL machinery fatals
        // (CobolCallException — EC-PROGRAM-NOT-FOUND / -RECURSIVE-CALL / -CANCEL-ACTIVE / EC-FUNCTION-NOT-FOUND,
        // §14.9.4.4 GR3h → §14.6.13.1.3 #8). Runtime-side so a fatal from ANY runtime element reaches the surface,
        // incl. a separately-compiled CALLed module (the settled SSOT §18.16 implementor choice).
        catch (CobolFatalException fx) { AbnormalTermination(fx.Message); }
        catch (CobolCallException cx) { AbnormalTermination(cx.Message); }
        // An implementor-defined stop that raised NO exception condition (§14.6.13.1.1 NOTE 3) still terminates
        // the run unit through the COBOL boundary — a .NET stack trace on the console is not a diagnostic a COBOL
        // programmer can act on, and letting one escape is the runtime twin of emitting a Roslyn CS error on
        // generated user source. Reported WITHOUT an exception-name, because there is none to name.
        catch (CobolImplementorFatalException ix) { AbnormalTermination(ix.Message); }
        finally
        {
            // §14.6.11(2): an implicit CLOSE without phrases for EVERY open file in the RUN UNIT, executed even when
            // termination is abnormal (§14.6.12). Idempotent (FileRegistry.CloseAll), so the RunUnit.Run embedding
            // path's own finally-CloseAll is a harmless double. StopRun unwinding from Activate passes THROUGH here
            // (CloseAll runs) on its way to the entry wrapper's catch.
            _owner.Files.CloseAll();
        }
    }

    /// <summary>The §14.6.12 abnormal-run-unit-termination indication (§14.6.11 CLOSE is the caller's finally): the
    /// OS "shall indicate an abnormal termination" — this implementation writes the diagnostic to stderr and sets a
    /// nonzero exit code (Annex A ERROR ⇒ 1; the settled §18.16 implementor choice).</summary>
    private static void AbnormalTermination(string message)
    {
        Console.Error.WriteLine("abnormal run-unit termination: " + message);
        Environment.ExitCode = 1;
    }

    /// <summary>
    /// Execute one CALL (ISO §14.9.4.4): resolve <paramref name="name"/> from <paramref name="callerPath"/> per
    /// the §8.4.6.3 scope rules (GR3b), enforce the non-recursive re-entry rule (GR3f), pick the instance per the
    /// §14.6.2.3 state model, activate, and apply the INITIAL program's implicit CANCEL on return (§14.9.18 GR2).
    /// Failures raise <see cref="CobolCallException"/> — the call site's ON OVERFLOW / ON EXCEPTION phrase (when
    /// present) converts it to the exception branch (GR3h); otherwise the run unit terminates loudly.
    /// </summary>
    public void CallProgram(string name, string callerPath, CobolArg[] args, CobolArg? returning,
        string notFoundEc = "EC-PROGRAM-NOT-FOUND", bool siteArgMismatchChecking = false)
    {
        // The EC-EXTERNAL enablement handshake, half 1 (§14.8.4.1 / §14.9.4.4 GR3e; kb/Work PB133): the
        // site's pending mask is consumed by THIS activation attempt — success or failure — so a NOT-FOUND
        // or RECURSIVE-CALL throw cannot leak it into the NEXT statement's activator latch. (The old code
        // zeroed it only after a successful resolution.)
        var excState = _owner.Exceptions;
        int pendingExternalMask = excState.ExternalCheckMask;
        excState.ExternalCheckMask = 0;
        var n = ResolveVisible(name, callerPath, wantFunction: notFoundEc == "EC-FUNCTION-NOT-FOUND")
            ?? throw new CobolCallException(
                notFoundEc == "EC-FUNCTION-NOT-FOUND"
                    ? $"FUNCTION '{name?.Trim()}': the user-defined function could not be located in the run unit "
                      + "(ISO §8.4.3.2.4 GR6b — EC-FUNCTION-NOT-FOUND)"
                    : $"CALL '{name?.Trim()}': program not found in the run unit (ISO §14.9.4.4 GR3b — EC-PROGRAM-NOT-FOUND)",
                notFoundEc);
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
        if (n.FormalCount >= 0 && (args.Length > n.FormalCount || args.Length < n.RequiredCount)
            && siteArgMismatchChecking && n.ArgMismatchChecking)
            throw new CobolCallException(
                $"CALL '{n.Name}': {args.Length} argument(s) against {n.FormalCount} formal parameter(s) "
                + $"({n.RequiredCount} required) — ISO §14.8.2.1 via §14.9.4.4 GR3d — EC-PROGRAM-ARG-MISMATCH",
                "EC-PROGRAM-ARG-MISMATCH");

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
        // The EC-EXTERNAL enablement handshake (§14.8.4.1 / §14.9.4.4 GR3e): latch the CALL site's pending mask
        // as the activated element's ACTIVATOR mask (the "activating runtime element" half of the pair), then
        // zero the pending mask so a site-emit-free nested CALL correctly reads "checking not enabled". Both
        // restore/re-zero on return — the mask never leaks across statements or activations.
        var exc = excState;
        int savedActivator = exc.ActivatorExternalMask;
        exc.ActivatorExternalMask = pendingExternalMask;   // half 2 — captured before resolution (GR3e)
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
        bool transferred = false;
        try
        {
            inst.DescribeExternals();   // GR3e — pre-transfer; inside the try so the finally still balances
            transferred = true;         // GR3g — control is transferred to the called program
            inst.Call(args, returning);
        }
        catch (CobolCallException cx) when (transferred)
        {
            // GR3d (kb/Work PB615): a formal the activated element could not adopt is THIS attempt's failure —
            // consume the mark so the next boundary out sees an ordinary propagated condition. Otherwise GR3i.
            if (cx.RaisedAtAdoption) cx.RaisedAtAdoption = false;
            else cx.ControlTransferred = true;
            throw;
        }
        finally
        {
            n.Active--; _owner.Modules.Pop();
            exc.ActivatorExternalMask = savedActivator; exc.ExternalCheckMask = 0;
            exc.TrimPerformTo(savedPerformDepth);
            exc.NonfatalDispatcher = savedNonfatalDispatcher;
            exc.RestoreChecking(savedChecking);
            if (freshInstance) n.Instance = displacedInstance;   // kb/Work PB133 — see above
        }

        if (n.Initial && n.Active == 0)
        {
            // "If the program … is an initial program, an implicit CANCEL statement referencing that program is
            // executed upon return" (ISO §14.9.18 GR2): close its files (§14.9.5 GR9), cascade (GR4), drop state.
            // Guarded on Active == 0 (kb/Work PB133): with 11.10.4 GR4 inheritance an INITIAL containee of a
            // recursive container can return while an OUTER activation of itself is still running — cancelling
            // the live activation's contained state would corrupt it (§14.9.5's CANCEL-of-active shape).
            inst.CloseFiles();
            CancelContained(n);
            n.Instance = null;
            n.CalledSinceCancel = false;   // §14.9.18 GR2 IS a CANCEL — the unit is "at present canceled",
                                           // so a later explicit CANCEL is GR7's no-op
        }

        // ⛔ NO BOUNDARY DEFAULT HERE (kb/Work PB892 Arm B). A condition the callee staged with GOBACK / EXIT
        // PROGRAM … RAISING names THIS activation's activator (ExceptionEngine — the staged slot carries the
        // ModuleStack identity), so only a pickup emitted in the activating element can take it, and an activator
        // that emitted none never has it raised — §14.9.18.4 GR1 b) for an unchecked activator. A discard here
        // was the CALL-only half of that rule; the INVOKE half had no chokepoint to put one in.
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
        string target = name?.Trim() ?? "";
        foreach (var n in _order)
            if (n.ParentPath is null && !n.IsFunction && NameEquals(n.CallName, target)) { notFound = false; return new ProgramPointer(n.CallName); }
        if (ProbeSiblingModule(target))
            foreach (var n in _order)
                if (n.ParentPath is null && !n.IsFunction && NameEquals(n.CallName, target)) { notFound = false; return new ProgramPointer(n.CallName); }
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
        string target = name?.Trim() ?? "";
        foreach (var n in _order)
            if (n.IsFunction && NameEquals(n.CallName, target)) { notFound = false; return new FunctionPointer(n.CallName); }
        if (ProbeSiblingModule(target))
            foreach (var n in _order)
                if (n.IsFunction && NameEquals(n.CallName, target)) { notFound = false; return new FunctionPointer(n.CallName); }
        notFound = true;
        return FunctionPointer.Null;   // §8.4.3.12.4 GR4 — the value is the predefined address NULL
    }

    /// <summary>The run-time half of the function-pointer signature invariant (ISO §13.18.60.4 GR26 — "A
    /// function-pointer shall contain only the predefined address NULL or the address of a function with the same
    /// signature as that identified by the specified function-prototype-name-1"), enforced statement by statement
    /// by §14.9.39.4 GR14: true when <paramref name="p"/> is NULL (GR26's first alternative) or addresses a
    /// registered function whose declared formal count is <paramref name="expectedFormals"/>.
    /// <para>⛔ GRANULARITY, STATED: the compare is ARITY-ONLY, because a registered unit carries
    /// <c>FormalCount</c>/<c>RequiredCount</c> and nothing finer. That is the SAME granularity as the
    /// compile-time COBOLNET1513 prototype-vs-definition check (<c>BinderDriver.BuildUserFunctionTable</c> —
    /// "Light check (argument count; full §8.13 external-repository conformance is staged residue)"), and the
    /// COMPILE-TIME §14.9.39.3 SR20 compare in <c>SetBinder</c> is richer: it has both bound
    /// <c>CalleeSignature</c>s. This site exists for the one case SR20 cannot reach — a sender resolved from a
    /// run-time NAME (§8.4.3.12.4 GR1 a), the identifier-1 form).</para>
    /// <para><c>Math.Max(FormalCount, 0)</c> is LOAD-BEARING, not defensive: <c>ProgramEmitter.EmitEntryWrapper</c>
    /// omits the <c>formalCount:</c> argument entirely when the unit has no formals, so a ZERO-formal function
    /// registers with the "not registered" sentinel −1.</para></summary>
    public bool FunctionSignatureMatches(FunctionPointer p, int expectedFormals)
    {
        if (p.IsNull) return true;   // §13.18.60.4 GR26 — NULL is always an admissible content
        foreach (var n in _order)
            if (n.IsFunction && NameEquals(n.CallName, p.Name!))
                return Math.Max(n.FormalCount, 0) == expectedFormals;
        return false;   // an address no registration backs is not "the address of a function … with the same signature"
    }

    /// <summary>Execute a CALL through a program-pointer (ISO §14.9.4 SR1 — identifier-1 references a
    /// program-pointer item; GR at :26177 — the item "contains the location of the program being called").
    /// A NULL pointer has no program to call: §14.9.4.4's "invalid program address" execution is undefined —
    /// this implementation defines it as the EC-PROGRAM-NOT-FOUND loud failure (never a silent no-op). The
    /// held name is an OUTERMOST program's identity, so the §8.4.6.3 rule-4 leg of the SAME
    /// <see cref="CallProgram"/> resolution finds it from any caller (the singular-pattern rule).</summary>
    public void CallPointer(ProgramPointer target, string callerPath, CobolArg[] args, CobolArg? returning)
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
            // The EC-EXTERNAL handshake's half 1 applies to EVERY activation attempt, failed ones included
            // (CallProgram's own rule): a NULL throw that skipped it would leak this site's pending mask into the
            // NEXT statement's activator latch.
            _owner.Exceptions.ExternalCheckMask = 0;
            throw new CobolCallException(
                "CALL through a NULL program-pointer: the pointer contains the predefined address NULL "
                + "(ISO §14.9.4.4 GR3b — EC-PROGRAM-PTR-NULL)", "EC-PROGRAM-PTR-NULL");
        }
        CallProgram(target.Name!, callerPath, args, returning);
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
        CobolArg? returning)
    {
        if (target.IsNull)
        {
            _owner.Exceptions.ExternalCheckMask = 0;   // half 1 of the EC-EXTERNAL handshake — see CallPointer
            throw new CobolCallException(
                "function-identifier through a NULL function-pointer: the pointer contains the predefined address "
                + "NULL, so no function is activated (ISO §8.4.3.2.4 GR6c — EC-FUNCTION-PTR-NULL)",
                "EC-FUNCTION-PTR-NULL");
        }
        CallProgram(target.Name!, callerPath, args, returning, notFoundEc: "EC-FUNCTION-NOT-FOUND");
    }

    /// <summary>
    /// Execute one CANCEL target (ISO §14.9.5): a zero-length name is a no-op (GR12); a name not in the run unit
    /// is a no-op (the never-made-available case); an ACTIVE program raises (GR5 — EC-PROGRAM-CANCEL-ACTIVE, the
    /// program is NOT canceled); otherwise contained programs cancel in reverse source order (GR4), the
    /// program's open file connectors close implicitly (GR9 — no optional phrases, no USE procedures), and the
    /// next CALL finds the program in its initial state (GR3). EXTERNAL data is untouched (GR8). A never-called
    /// or already-canceled program is a no-op (GR7).
    /// </summary>
    public void Cancel(string name, string callerPath)
    {
        string n = name?.Trim() ?? "";
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
        string target = name?.Trim() ?? "";
        if (target.Length == 0) return null;
        Node? caller = callerPath is not null && _byPath.TryGetValue(callerPath, out var c) ? c : null;

        if (caller is not null)
        {
            foreach (var child in caller.Children)                                   // rule 1
                if (child.IsFunction == wantFunction && NameEquals(child.CallName, target)) return child;
            if (caller.IsFunction == wantFunction && NameEquals(caller.CallName, target) && caller.Recursive)
                return caller;                                                       // rule 2
            for (var anc = ParentOf(caller); anc is not null; anc = ParentOf(anc))   // rule 3 — nearest container first
                foreach (var sib in anc.Children)
                {
                    if (sib.IsFunction != wantFunction || !sib.Common || !NameEquals(sib.CallName, target)) continue;
                    bool onOwnChain = caller.Path.Equals(sib.Path, StringComparison.OrdinalIgnoreCase)
                        || caller.Path.StartsWith(sib.Path + "/", StringComparison.OrdinalIgnoreCase);
                    if (!onOwnChain || sib.Recursive) return sib;
                }
        }
        foreach (var n in _order)                                                    // rule 4 — outermost programs
            if (n.ParentPath is null && n.IsFunction == wantFunction && NameEquals(n.CallName, target)) return n;

        // Rule-4 fallthrough: the run unit may be composed of SEPARATELY COMPILED modules ("a run unit contains
        // one or more runtime modules", ISO §14.6.1; §14.9.4.4 GR3b — the runtime system "attempts to locate"
        // the called program; the locating mechanics beyond the §8.4.6.3 name scope are implementor-defined).
        // Probe the application directory for a sibling compiled module named after the program, invoke its
        // public __CobolModule.Register() registrar (generated classes are internal — the registrar IS the
        // discovery surface), and retry rule 4 once. Probed names are cached, hit or miss — one I/O probe per
        // name per run unit.
        if (probe && ProbeSiblingModule(target))
            foreach (var n in _order)
                if (n.ParentPath is null && n.IsFunction == wantFunction && NameEquals(n.CallName, target)) return n;
        return null;
    }

    /// <summary>Load the sibling compiled module <c>&lt;name&gt;.dll</c> from <see cref="AppContext.BaseDirectory"/>
    /// (exact name first, then a case-insensitive scan — Linux filesystems are case-sensitive) into the default
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> and run its <c>__CobolModule.Register()</c>.
    /// Returns true when a registrar ran (the caller re-resolves); a missing file / foreign dll / load failure
    /// is a quiet false — the CALL then raises the ordinary EC-PROGRAM-NOT-FOUND surface.</summary>
    private bool ProbeSiblingModule(string name)
    {
        if (!_probedModules.Add(name)) return false;   // already probed this run unit (negative/positive cache)
        try
        {
            string dir = AppContext.BaseDirectory;
            string path = System.IO.Path.Combine(dir, name + ".dll");
            if (!System.IO.File.Exists(path))
                path = System.IO.Directory.EnumerateFiles(dir, "*.dll").FirstOrDefault(f =>
                    string.Equals(System.IO.Path.GetFileNameWithoutExtension(f), name,
                        StringComparison.OrdinalIgnoreCase)) ?? "";
            if (path.Length == 0) return false;
            var asm = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            var register = asm.GetType("__CobolModule")?.GetMethod("Register", Type.EmptyTypes);
            if (register is null) return false;   // not a WiseOwl COBOL module — no registrar surface
            register.Invoke(null, null);
            return true;
        }
        catch
        {
            return false;   // an unloadable/foreign dll is simply "not found" (§14.9.4.4 GR3b)
        }
    }

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

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
