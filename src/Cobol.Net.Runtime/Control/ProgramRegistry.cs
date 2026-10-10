// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The inter-program runtime (COBOLNET_INTERPROGRAM_DESIGN D1–D5; ISO/IEC 1989:2023 §14.9.4 CALL / §14.9.5 CANCEL /
//  §14.2 procedure-division parameter passing / §8.4.6.3 program-name scope / §8.6.7 EXTERNAL sharing):
//    • ManagedPointer / ManagedPointer<T> — the ONE managed-reference carrier (design D1; the typed-native
//      re-implementation of the legacy ManagedPointer — internally the typed carrier, public name kept per the
//      settled SSOT §18 #12). Serves BY REFERENCE arguments and LINKAGE formals today; USAGE POINTER / ADDRESS OF /
//      BASED / ALLOCATE reuse the same carrier (singular-pattern rule).
//    • ICobolProgram / CobolArg — the uniform opaque calling ABI (design D2): an ordered (mode, carrier, meta)
//      argument list every compiled program-class accepts, so dynamic CALL (identifier) and cross-assembly CALL
//      need no knowledge of the callee's LINKAGE.
//    • ProgramReturn — the called-program return signal (settled SSOT §18 #10; Control/Signals/).
//    • ProgramTable (instance, on RunUnit) — program-name resolution honoring §8.4.6.3, the §14.6.2.3 state model,
//      and CANCEL semantics; ProgramRegistry (this file) is its static emitted-surface shim.
//    • ExternalTable (instance, on RunUnit) — one storage copy per external name per run unit (§8.6.7 /
//      §13.18.22); NOT reset by CANCEL (§14.9.5 GR8); ExternalStore is its static shim.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// An ACTIVATION-ATTEMPT failure of CALL / CANCEL / a user-function reference — ISO §14.9.4.4 GR3b–f's "the
/// program call is not successful" set: program not found (GR3b, EC-PROGRAM-NOT-FOUND), a NULL program-pointer
/// (GR3b, EC-PROGRAM-PTR-NULL), an argument-count violation (GR3d, EC-PROGRAM-ARG-MISMATCH), an external
/// non-conformance (GR3e, the EC-EXTERNAL trio), a re-entry of an active non-RECURSIVE program (GR3f,
/// EC-PROGRAM-RECURSIVE-CALL), CANCEL of an active program (§14.9.5 GR5, EC-PROGRAM-CANCEL-ACTIVE), or a
/// user-defined-function locate miss (§8.4.3.2.4 GR6b, EC-FUNCTION-NOT-FOUND). <see cref="CarriedNames"/> is
/// the whole set. At <c>--std 85</c> there is no EC machinery — a CALL site with an ON OVERFLOW phrase converts
/// this to the overflow branch (the 85 surface); without one, the run unit terminates loudly (abnormal
/// termination). <paramref name="ecName"/> carries the Table 13 level-3 name so a CALL site compiled with that
/// name's checking enabled (>>TURN, §7.3.25) can set the last exception status and run the §14.9.49 F3
/// declarative selection over the precise condition.
/// <para>⛔ NOT every failure inside a called program: a reference to an OMITTED argument raises
/// EC-PROGRAM-ARG-OMITTED (§14.9.4.4 GR12) at the reference, in the CALLEE (OmittedFormal), precisely because it is
/// an in-execution raise and not an activation failure — the old carrier threw this exception for it and every
/// enclosing CALL site read it as its own GR3h failure (kb/Work PB133). <see cref="ControlTransferred"/> is the
/// general form of that distinction.</para>
/// </summary>
public sealed class CobolCallException(string message, string ecName = "EC-PROGRAM-IMP") : Exception(message)
{
    /// <summary>The Table 13 level-3 exception-name of this failure (uppercase).</summary>
    public string EcName { get; } = ecName;

    /// <summary>ISO §14.9.4.4 GR3i — "If the program was successfully called, after control is returned from
    /// the called program the ON EXCEPTION phrase, if specified, is ignored." False while the condition is
    /// still THIS activation attempt's own GR3b–f/GR3e failure ("the program was not successfully called",
    /// GR3h); set to true by the activation boundary (<c>ProgramTable.CallProgram</c>) as the condition
    /// crosses OUT of the called program's execution. The mark is monotone: for every CALL site above the one
    /// that actually failed, control HAD been transferred, so those sites leave the condition to §14.6.13.1
    /// instead of running their own imperative-statement-1. Without it a nested failure ran — and was
    /// swallowed by — every enclosing CALL's ON EXCEPTION phrase (kb/Work PB233).</summary>
    public bool ControlTransferred { get; set; }

    /// <summary>ISO §14.9.4.4 GR3d — a §14.8.2 conformance violation detected while the ACTIVATED element adopts
    /// its formal parameters (<c>CobolArgAdapt.Unreadable</c>; kb/Work PB615). Adoption runs inside
    /// <c>ICobolProgram.Call</c> but before any of the element's statements, and GR3d makes such a violation "the
    /// program call is not successful" — an activation-attempt failure, GR3h's, not a condition propagated from
    /// the called program. The activation boundary CONSUMES this mark (clears it instead of setting
    /// <see cref="ControlTransferred"/>), so a boundary further out — for which control HAD been transferred —
    /// marks it in the ordinary way.</summary>
    public bool RaisedAtAdoption { get; set; }

    /// <summary>ISO §14.9.4.4 GR3h item 1's family partition — "if the exception condition is any of the
    /// EC-PROGRAM or EC-EXTERNAL exception conditions". THE one place that partition is written down: the CALL
    /// emitter's enabled-name split and the emitted phrase arm's runtime filter both ask this. Every other
    /// condition a <see cref="CobolCallException"/> can carry (today EC-FUNCTION-NOT-FOUND and
    /// EC-FUNCTION-PTR-NULL, §8.4.3.2.4 GR6b/GR6c)
    /// takes GR3h item 2's second disjunct instead — the applicable exception processing statements, never
    /// imperative-statement-1.</summary>
    public static bool IsProgramOrExternal(string ec) =>
        ec.StartsWith("EC-PROGRAM-", StringComparison.Ordinal)
        || ec.StartsWith("EC-EXTERNAL-", StringComparison.Ordinal);

    /// <summary>Every Table 13 level-3 exception-name this carrier can actually raise — the ONE declaration of
    /// what a CALL site's catch arms may usefully filter on. A CALL emitter that filtered on the statement's
    /// whole enabled set instead would emit a two-hundred-way dead name test under
    /// <c>&gt;&gt;TURN EC-ALL CHECKING ON</c>. Kept honest by <c>CallExceptionCarrierDriftTests</c>, which
    /// re-derives this set from the <c>new CobolCallException(...)</c> raise sites in the runtime sources —
    /// add a raise site with a new name and the test names this list, so nothing is hand-maintained blind.
    /// <para>EC-PROGRAM-IMP is the constructor default (§14.6.13.1.1's implementor <c>*-IMP</c> level-3 row):
    /// an internal invariant failure at the activation boundary raises it with no explicit name.</para></summary>
    public static readonly string[] CarriedNames =
    [
        "EC-EXTERNAL-DATA-MISMATCH",      // §14.8.4.2 via §14.9.4.4 GR3e
        "EC-EXTERNAL-FILE-MISMATCH",      // §14.8.4.4 via §14.9.4.4 GR3e
        "EC-EXTERNAL-FORMAT-CONFLICT",    // §14.8.4.3 via §14.9.4.4 GR3e
        "EC-FUNCTION-NOT-FOUND",          // §8.4.3.2.4 GR6b — a user-defined-function locate miss
        "EC-FUNCTION-PTR-NULL",           // §8.4.3.2.4 GR6c — a function-identifier through a NULL function-pointer
        "EC-PROGRAM-ARG-MISMATCH",        // §14.8.2.1 via §14.9.4.4 GR3d
        "EC-PROGRAM-CANCEL-ACTIVE",       // §14.9.5 GR5
        "EC-PROGRAM-IMP",                 // the ctor default (see above)
        "EC-PROGRAM-NOT-FOUND",           // §14.9.4.4 GR3b
        "EC-PROGRAM-PTR-NULL",            // §14.9.4.4 GR3b, first sentence
        "EC-PROGRAM-RECURSIVE-CALL",      // §14.9.4.4 GR3f
        "EC-PROGRAM-RESOURCES",           // §14.9.4.4 GR3c / §8.4.3.2.4 GR6c — the activation's stack (kb/Work PB2659)
    ];

    /// <summary>Is <paramref name="ec"/> a name this carrier can raise (<see cref="CarriedNames"/>)?</summary>
    public static bool CanCarry(string ec) => Array.IndexOf(CarriedNames, ec) >= 0;
}

/// <summary>
/// The static facade over the run unit's <see cref="ProgramTable"/> (the emitted surface — generated run-unit
/// drivers call <c>ProgramRegistry.Reset()/RunModule(...)</c> (a host: <c>RunMain(...)</c>), a module's <c>__CobolModule.EnsureRegistered()</c> calls
/// <c>RegisterModule(...)</c> and its private registrar <c>Register(...)</c>, and call sites emit
/// <c>CallProgram/Cancel</c>; kept name-stable pre-G8, DESIGN-runtime-library §2.1). Every member forwards to
/// <c>RunUnit.Current.Programs</c>; <see cref="Reset"/> is the run-unit lifecycle's BEGIN
/// (<see cref="RunUnit.Begin"/>), keeping the emitted driver's name byte-stable.
/// </summary>
public static class ProgramRegistry
{
    /// <summary>Run-unit start — the emitted driver's first statement: begin a NEW run unit and make it the ambient
    /// one (<see cref="RunUnit.Begin"/>; kb/Work PB1069). Every piece of run-unit state is fresh because the run
    /// unit is a new object, not because a list of members was cleared.</summary>
    public static void Reset() => RunUnit.Begin();

    /// <inheritdoc cref="ProgramTable.RegisterModule"/>
    public static void RegisterModule(string registrar, string runtimeVersion, int callAbi, Action register)
        => RunUnit.Current.Programs.RegisterModule(registrar, runtimeVersion, callAbi, register);

    /// <inheritdoc cref="ProgramTable.Register"/>
    public static void Register(
        string path, string name, string? parentPath,
        bool initial, bool common, bool recursive,
        Func<ICobolProgram?, ICobolProgram> factory,
        Action? staticReset = null,
        int formalCount = -1, int requiredCount = 0, bool argMismatchChecking = false,
        bool isFunction = false, string? externalizedName = null, BoundaryItem? returning = null,
        BoundaryItem[]? formals = null)
        => RunUnit.Current.Programs.Register(path, name, parentPath, initial, common, recursive, factory,
            staticReset, formalCount, requiredCount, argMismatchChecking, isFunction, externalizedName, returning, formals);

    /// <inheritdoc cref="ProgramTable.RunMain"/>
    public static void RunMain(string path) => RunUnit.Current.Programs.RunMain(path);

    /// <inheritdoc cref="ProgramTable.RunModule"/>
    public static void RunModule(Action ensureRegistered, string? mainPath)
        => RunUnit.Current.Programs.RunModule(ensureRegistered, mainPath);

    /// <inheritdoc cref="ProgramTable.CallProgram"/>
    public static void CallProgram(string name, string callerPath, CobolArg[] args, CobolArg? returning,
        string notFoundEc = "EC-PROGRAM-NOT-FOUND", bool siteArgMismatchChecking = false, string[]? programSpecifiers = null,
        bool siteSizeTruncationChecking = false)
        => RunUnit.Current.Programs.CallProgram(name, callerPath, args, returning, notFoundEc, siteArgMismatchChecking,
            programSpecifiers, siteSizeTruncationChecking);

    /// <inheritdoc cref="ProgramTable.Cancel"/>
    public static void Cancel(string name, string callerPath) => RunUnit.Current.Programs.Cancel(name, callerPath);

    /// <inheritdoc cref="ProgramTable.EntryOf"/>
    public static ProgramPointer EntryOf(string name, out bool notFound)
        => RunUnit.Current.Programs.EntryOf(name, out notFound);

    /// <summary>A §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER evaluated as a CALL ARGUMENT (ISO §14.9.4.3 SR3; kb/Work
    /// PB239) — <see cref="EntryOf"/>, with §8.4.3.13.4 GR4's miss delivered the way §14.9.4.4 GR3a requires of an
    /// argument: "identifier-2 … [is] evaluated … at the beginning of the execution of the CALL statement. If an
    /// exception condition exists, no program is called and execution proceeds as specified in General rule 3h."
    /// With EC-PROGRAM-NOT-FOUND checking enabled (<paramref name="checkNotFound"/>, the CALL statement's own
    /// compile-time TURN state) the miss is raised as the CALL's own pre-transfer failure — thrown while the
    /// argument array is built, before the registry is entered, so the CALL site's GR3h arms take it exactly as
    /// they take its locate failure. Unchecked, the condition is not raised (§14.6.13.1.4) and the argument is
    /// GR4's predefined address NULL.</summary>
    public static ProgramPointer EntryOfArgument(string name, bool checkNotFound)
    {
        var p = EntryOf(name, out bool notFound);
        if (notFound && checkNotFound)
            throw new CobolCallException(
                $"ADDRESS OF PROGRAM '{ExternalizedNames.Form(name)}': the program could not be located{RunUnit.Current.Programs.NotLocatedReason(name)} (ISO §8.4.3.13.4 GR4 — "
                + "EC-PROGRAM-NOT-FOUND; §14.9.4.4 GR3a — no program is called)", "EC-PROGRAM-NOT-FOUND");
        return p;
    }

    /// <summary>A §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER evaluated as an ordinary OPERAND — a relation-condition
    /// operand (§8.8.4.2.2 Format 3) or an INVOKE argument (§14.9.23.3 SR9/SR19); kb/Work PB1021. §8.4.3.13.4 GR4:
    /// "If the runtime system cannot locate the program, the EC-PROGRAM-NOT-FOUND exception condition is set to
    /// exist and the value of the address-identifier is the predefined address NULL." EC-PROGRAM-NOT-FOUND is a
    /// FATAL condition (Table 13), so with its checking enabled (<paramref name="checkNotFound"/>, the carrying
    /// statement's compile-time TURN state) the miss is raised the way every in-expression fatal condition is — the
    /// last-exception status set, then the fatal throw the statement guard selects a USE declarative for. Unchecked,
    /// the condition is not raised (§14.6.13.1.4) and the value is GR4's NULL. The CALL argument has its own
    /// <see cref="EntryOfArgument"/>, because §14.9.4.4 GR3a/GR3h make the miss the CALL's pre-transfer failure.</summary>
    public static ProgramPointer EntryOfOperand(string name, bool checkNotFound)
    {
        var p = EntryOf(name, out bool notFound);
        if (notFound && checkNotFound)
        {
            string detail = $"ADDRESS OF PROGRAM '{ExternalizedNames.Form(name)}': the program could not be located{RunUnit.Current.Programs.NotLocatedReason(name)} (ISO §8.4.3.13.4 GR4)";
            ExceptionState.Set("EC-PROGRAM-NOT-FOUND", fatal: true);
            throw new CobolFatalException("EC-PROGRAM-NOT-FOUND", detail);
        }
        return p;
    }

    /// <inheritdoc cref="ProgramTable.FunctionAddressOf"/>
    public static FunctionPointer FunctionAddressOf(string name, out bool notFound)
        => RunUnit.Current.Programs.FunctionAddressOf(name, out notFound);

    /// <inheritdoc cref="ProgramTable.FunctionSignatureMatches"/>
    public static bool FunctionSignatureMatches(FunctionPointer p, int expectedFormals)
        => RunUnit.Current.Programs.FunctionSignatureMatches(p, expectedFormals);

    /// <inheritdoc cref="ProgramTable.CallPointer"/>
    public static void CallPointer(ProgramPointer target, string callerPath, CobolArg[] args,
        CobolArg? returning, bool siteArgMismatchChecking = false, string[]? programSpecifiers = null,
        bool siteSizeTruncationChecking = false)
        => RunUnit.Current.Programs.CallPointer(target, callerPath, args, returning, siteArgMismatchChecking,
            programSpecifiers, siteSizeTruncationChecking);

    /// <inheritdoc cref="ProgramTable.CallFunctionPointer"/>
    public static void CallFunctionPointer(FunctionPointer target, string callerPath, CobolArg[] args,
        CobolArg? returning, bool siteArgMismatchChecking = false)
        => RunUnit.Current.Programs.CallFunctionPointer(target, callerPath, args, returning, siteArgMismatchChecking);
}
