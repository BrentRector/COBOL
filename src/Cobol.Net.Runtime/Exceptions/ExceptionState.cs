// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The run-unit-wide LAST EXCEPTION STATUS register (ISO/IEC 1989:2023 §14.6.13.1.1: "a last exception status
/// exists for the entire run unit … set to indicate the last level-3 exception condition that was raised"), plus
/// the associated information the EXCEPTION-FILE / EXCEPTION-LOCATION / EXCEPTION-STATEMENT functions interrogate
/// (§15.28/§15.30/§15.32) and the propagation slot GOBACK/EXIT … RAISING uses to re-raise in the activator
/// (§14.9.18 / §14.9.14). It is written ONLY when checking for the condition is enabled (§14.6.13.1.1) — the
/// generated raise sites are themselves gated by the compile-time TurnState. <c>SET LAST EXCEPTION TO OFF</c>
/// (SET Format 13, §14.9.39) maps to <see cref="Clear"/>.
/// One INSTANCE per run unit, owned by <see cref="RunUnit"/> (DESIGN-runtime-library §2.6 — "a last exception
/// status exists for the entire run unit" makes the run unit the correct owner); the static
/// <see cref="ExceptionState"/> shim keeps the emitted surface unchanged pre-G8.
/// </summary>
public sealed class ExceptionEngine
{
    /// <summary>An engine with no activation record (a unit test driving the engine directly): every staging is
    /// taken by the next pickup, since there is no second activation to tell apart.</summary>
    public ExceptionEngine() { }

    /// <summary>The run unit's engine, linked to the run unit's ONE activation record (<see cref="ModuleStack"/>)
    /// so a staged <c>GOBACK / EXIT … RAISING</c> condition can name the activation it was staged FOR
    /// (§14.9.18.4 GR1 b); kb/Work PB892 Arm B).</summary>
    public ExceptionEngine(ModuleStack activations) => _activations = activations;

    private readonly ModuleStack? _activations;
    /// <summary>The last raised level-3 exception-name (uppercase), or null when no exception condition exists.</summary>
    public string? LastName { get; private set; }

    /// <summary>Whether the last raised exception condition is fatal (Table 13 category; EC-*-IMP ⇒ fatal here).</summary>
    public bool LastFatal { get; private set; }

    /// <summary>The file-connector key associated with the last EC-I-O exception (null for non-I-O conditions
    /// and for an EC-I-O raised by RAISE / EXIT RAISING / GOBACK RAISING — §15.28.4 r1b).</summary>
    public string? LastFile { get; private set; }

    /// <summary>The two-character I-O status associated with the last EC-I-O exception (§15.28.4 r1c).</summary>
    public string? LastIoStatus { get; private set; }

    /// <summary>The §15.30.3 r2 location string ("element; paragraph[ OF section]; line-id"), captured ONLY when
    /// the enabling TURN directive carried WITH LOCATION (§7.3.25.4 GR7; the implementor choice of §15.30.3 r1 is
    /// NO information without LOCATION — recorded in the deep-dive).</summary>
    public string? LastLocation { get; private set; }

    /// <summary>The uppercase statement name that raised the last exception (§15.32.3 r2), captured only WITH
    /// LOCATION (r1 otherwise).</summary>
    public string? LastStatement { get; private set; }

    /// <summary>EXCEPTION-OBJECT (§8.4.3.6) — the run unit's ONE predefined object reference (GR2):
    /// references the current exception object, null when none (GR1). Implicitly UNIVERSAL (SR2 :7249) —
    /// typed <see cref="CobolObject"/> per the D-U1 universal model. Set by RAISE identifier-1, by
    /// the GR1b2 activator-side pickup, and (GR15) it already holds the object when an F4 declarative
    /// enters; nulled by every NAMED raise (§14.9.29.4 GR1 / :24485).</summary>
    public CobolObject? ExceptionObject { get; private set; }

    /// <summary>Record a raised exception OBJECT (§14.6.13.1.5 (1)/(2): EXCEPTION-OBJECT references the
    /// object; the last exception status indicates an exception object — realized as the
    /// <see cref="ExceptionState.ObjectSentinel"/> LastName). Exception objects are NOT TURN-gated (§7.3.25
    /// takes exception-NAMES only) and a RAISE of an object is never fatal by itself (§14.9.29.4 GR2).</summary>
    public void SetObject(CobolObject? obj)
    {
        LastName = ExceptionState.ObjectSentinel;
        LastFatal = false;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = null;
        LastLocation = null;
        ExceptionObject = obj;
    }

    // ── The AMBIENT statement context (kb/Work R14 — §15.32.3 r2 / §15.30.3 r2). The (Table-12 statement
    // name, location) pair is a property of the RAISING STATEMENT, so it travels ambiently: the emitted
    // checked-statement wrapper enters it (with the set of names whose enabling TURN carried WITH LOCATION —
    // the pair is per-CONDITION, §15.32.3 r1 / kb/Work R06) and restores the prior context on exit, and
    // every raise site that calls the 2-argument Set — SEARCH's range conditions, CONTINUE AFTER, the
    // nonfatal ambient gates, the runtime string/storage sites — picks the pair up WITHOUT threading it.
    // Before this channel, only the sites an emitter could hand literals to answered r2; every other site
    // returned 63 spaces under WITH LOCATION (the F3 defect family). ──────────────────────────────────────────

    private string? _stmtName;
    private string? _stmtLoc;
    private string[]? _stmtLocNames;

    /// <summary>Enter a checked statement's ambient context; returns the PRIOR context for the emitted
    /// finally's <see cref="ExitStatement"/> (save/restore, so a nested activation — a CALL inside the
    /// statement — restores the caller's context on return).</summary>
    public (string? Stmt, string? Loc, string[]? Names) EnterStatement(string stmt, string loc, string[] locNames)
    {
        var prior = (_stmtName, _stmtLoc, _stmtLocNames);
        (_stmtName, _stmtLoc, _stmtLocNames) = (stmt, loc, locNames);
        return prior;
    }

    /// <summary>Restore the prior ambient statement context (the emitted finally).</summary>
    public void ExitStatement((string? Stmt, string? Loc, string[]? Names) prior) =>
        (_stmtName, _stmtLoc, _stmtLocNames) = prior;

    /// <summary>Record a raised non-I-O exception condition (§14.6.13.1.1: sets the last exception status;
    /// EXCEPTION-OBJECT is set to null — §14.9.29.4 GR1). With no explicit pair, the §15.32.3 r2 / §15.30.3 r2
    /// operands come from the AMBIENT statement context — and only when the raised NAME is one the enabling
    /// TURN covered WITH LOCATION at this statement (r1 answers spaces for the rest).</summary>
    public void Set(string name, bool fatal, string? statement = null, string? location = null)
    {
        LastName = name.ToUpperInvariant();
        if (statement is null && _stmtLocNames is { } names && System.Array.IndexOf(names, LastName) >= 0)
        {
            statement = _stmtName;
            location = _stmtLoc;
        }
        LastFatal = fatal;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = statement;
        LastLocation = location;
        ExceptionObject = null;
        _propagatedObject = default;   // a NAMED raise supersedes any staged object (the slots are exclusive)
    }

    /// <summary>Record a raised EC-I-O exception condition with its file connector and I-O status
    /// (the §15.28 EXCEPTION-FILE information).</summary>
    public void SetIo(string name, bool fatal, string file, string ioStatus, string? statement = null, string? location = null)
    {
        Set(name, fatal, statement, location);
        LastFile = file;
        LastIoStatus = ioStatus;
    }

    /// <summary><c>SET LAST EXCEPTION TO OFF</c> (ISO §14.9.39 Format 13): the last exception status indicates
    /// that no exception condition exists.</summary>
    public void Clear()
    {
        LastName = null;
        LastFatal = false;
        LastFile = null;
        LastIoStatus = null;
        LastStatement = null;
        LastLocation = null;
        ExceptionObject = null;
        _propagated = null;
        _propagatedObject = default;
    }

    // ── GOBACK / EXIT … RAISING propagation (§14.9.18 GR / §14.6.13.1.3 #6 shape) ────────────────────────────

    private (string Name, bool Fatal, string? Statement, string? Location, long For)? _propagated;

    // ── ⛔ A STAGED CONDITION NAMES THE ACTIVATION IT WAS STAGED FOR (kb/Work PB892 Arm B) ──────────────────────
    // §14.9.18.4 GR1 b) raises the condition "in the ACTIVATING runtime element if checking for that exception
    // condition is enabled in the activating runtime element" — ONE element, the one the returning element
    // returns to. Before this, the slot was run-unit-wide and anonymous, and "who takes it" was left to whichever
    // pickup ran next: an activator that emitted no pickup (an EC-free group — zero scaffolding) left it standing,
    // and the next EC-active pickup ANYWHERE in the run unit took it, running a declarative for a condition that
    // was never raised (a separately compiled EC-free main INVOKEs a raising method; a later EC-active program's
    // INVOKE of a method that raises NOTHING then ran its declarative). A CALL site hid the hole only because the
    // registry discarded behind every pickup-free CALL; an INVOKE is a direct .NET call with no such chokepoint.
    // So the RULE is written here, once, for every activation mechanism: staging records the activating
    // activation's identity (ModuleStack.ActivatingActivation — the frame beneath the returning element's own), and
    // a pickup takes the condition only when it runs IN that activation (ModuleStack.CurrentActivation). A staging
    // no pickup in its activator ever takes is simply never raised, which is GR1 b) for an unchecked activator.

    /// <summary>The activation a staging made NOW is for — the activator of the running (returning) element.</summary>
    private long StagingFor => _activations?.ActivatingActivation ?? 0;

    /// <summary>A staging made for <paramref name="stagedFor"/> is this pickup's to take: the pickup runs in that
    /// activation. A staging for any other activation is left alone — never raised here, and never discarded
    /// here either, because its own activator's pickup may still be ahead of it.</summary>
    private bool IsForThisActivation(long stagedFor) => stagedFor == (_activations?.CurrentActivation ?? 0);

    /// <summary>STAGE an exception condition for the ACTIVATOR (GOBACK / EXIT PROGRAM / method-return … RAISING,
    /// §14.9.18.4 GR1 b) / §14.9.14.4 GR3). Staging is UNCONDITIONAL and raises nothing here: §14.9.18.4 GR1 b) localises
    /// both the raise and its enablement test in the activating runtime element — "an exception condition is
    /// raised in the activating runtime element if checking for that exception condition is enabled in the
    /// activating runtime element" — and <see cref="TakeRaisedPropagation"/> is where both happen.
    ///
    /// <para>⛔ THIS USED TO CALL <see cref="Set"/> (kb/Work PB408). That made the returning element update the
    /// run unit's last exception status for a condition §14.6.13.1.1 raises only "if checking for [it] is
    /// enabled" — in the OTHER element. The §15.32.3 r2 / §15.30.3 r2 operands the RAISING statement contributes
    /// (kb/Work R07) now travel WITH the staged condition and are applied by the raise, so they still reach the
    /// activator's declarative; they simply no longer pre-empt the enablement question.</para></summary>
    public void SetPropagating(string name, bool fatal, string? statement = null, string? location = null)
    {
        _propagated = (name.ToUpperInvariant(), fatal, statement, location, StagingFor);
        _propagatedObject = default;   // the slots are mutually exclusive — a name supersedes a staged object
    }

    // ── The exception-OBJECT propagation slot (§14.6.13.1.5; GOBACK/EXIT/EXIT METHOD … RAISING identifier).
    //    Mutually exclusive with _propagated — a GOBACK stages exactly ONE of a name or an object. ──

    private (bool Has, CobolObject? Obj, long For) _propagatedObject;

    /// <summary>Stage an exception OBJECT for the activator (GOBACK / EXIT PROGRAM / method-return RAISING
    /// identifier-1, §14.9.18.4 GR1b): the returning element's status reflects the raise (GR1b1 via
    /// <see cref="SetObject"/>); the ACTIVATING site consumes via <see cref="TakePropagatedObject"/> and
    /// applies the §14.6.13.1.5 activator rules (F4 → declarative; none → EC-OO-EXCEPTION).</summary>
    public void SetPropagatingObject(CobolObject? obj)
    {
        SetObject(obj);
        _propagatedObject = (true, obj, StagingFor);
        _propagated = null;
    }

    /// <summary>Consume the staged exception object at the activating CALL/INVOKE site — only when it was staged
    /// for THIS activation (the same identity test as <see cref="TakeRaisedPropagation"/>).</summary>
    public bool TakePropagatedObject(out CobolObject? obj)
    {
        obj = null;
        if (!_propagatedObject.Has || !IsForThisActivation(_propagatedObject.For)) return false;
        obj = _propagatedObject.Obj;
        _propagatedObject = default;
        return true;
    }

    /// <summary>Stage the LAST EXCEPTION for the activator — <c>GOBACK / EXIT … RAISING LAST EXCEPTION</c>,
    /// ISO §14.9.18.4 GR1 b) 3.
    ///
    /// <para><b>3 a)</b> "If an exception condition is currently raised, that exception condition is set to exist
    /// in the activating runtime element. If the exception condition is an exception object, additional rules are
    /// specified in 14.6.13.1.5 … If the exception condition is a level-3 exception for EC-USER and that
    /// exception condition is not specified in the RAISING phrase of the procedure division header of the source
    /// element in which this EXIT statement is contained, the EC-RAISING-NOT-SPECIFIED exception condition is set
    /// to exist in the activating runtime element instead of the EC-USER exception condition."
    /// Table 13 (§14.6.13.1.6) makes EC-RAISING-NOT-SPECIFIED Fatal and names THIS case in its own third
    /// column.</para>
    /// <para><b>3 b)</b> "If no exception condition is raised, the RAISING phrase is ignored and no exception
    /// condition is set to exist in the activating runtime element" — a clear status stages nothing.</para>
    ///
    /// <para>The substitution is a RUN-TIME question and has no compile-time twin: the STATIC sibling of the same
    /// rule is §14.9.18.3 SR2, discharged at bind (COBOLNET0717) for <c>RAISING EXCEPTION exception-name-1</c>,
    /// whose name is known then. Only the LAST arm reaches here, which is exactly why it was the arm left out
    /// (kb/Work PB408).</para></summary>
    /// <param name="pdRaising">The containing source element's PROCEDURE DIVISION header RAISING list — the
    /// level-3 exception-names it specifies, or null when the header has no RAISING phrase. It crosses as the
    /// NAMES, with no encoding: an encode/decode pair would be one membership rule written in two places, and
    /// the site executes once per activation return.</param>
    /// <param name="statement">§15.32.3 r2 / §15.30.3 r2 operands for the SUBSTITUTED condition, which the
    /// RAISING statement itself causes; null unless its TURN carried WITH LOCATION (§7.3.25.4 GR7). The
    /// unsubstituted condition keeps the operands of the raise that actually set the status.</param>
    /// <param name="location">See <paramref name="statement"/>.</param>
    public void SetPropagatingLast(string[]? pdRaising = null, string? statement = null, string? location = null)
    {
        // An OBJECT status re-propagates the OBJECT (GR1b3a's second sentence → the §14.6.13.1.5 rules).
        if (LastName == ExceptionState.ObjectSentinel) { _propagatedObject = (true, ExceptionObject, StagingFor); _propagated = null; return; }
        if (LastName is not { } n) return;   // GR1b3b — nothing is raised, the RAISING phrase is ignored
        if (ExceptionCatalog.UnderLevel2(n, "EC-USER") && !Names(pdRaising).Contains(n, StringComparer.OrdinalIgnoreCase))
        {
            // GR1b3a's third sentence — the fatality comes from the catalog (Table 13), never a literal here.
            bool fatal = !ExceptionCatalog.TryGet(RaisingNotSpecified, out var rns)
                || rns.IsFatal;
            _propagated = (RaisingNotSpecified, fatal, statement, location, StagingFor);
        }
        else
            _propagated = (n, LastFatal, LastStatement, LastLocation, StagingFor);
        _propagatedObject = default;   // the slots are mutually exclusive
    }

    private const string RaisingNotSpecified = "EC-RAISING-NOT-SPECIFIED";

    /// <summary>AUTOMATIC PROPAGATION of an unhandled FATAL exception condition — ISO §14.6.13.1.3 6): "If checking
    /// for the exception condition is enabled, and the exception condition is neither EC-FLOW-GLOBAL-EXIT nor
    /// EC-FLOW-GLOBAL-GOBACK, and there is an applicable PROPAGATE ON directive, the exception condition is propagated
    /// as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were executed" (the §7.3.21.4 GR2 effect of
    /// <c>&gt;&gt;PROPAGATE ON</c>; kb/Work PB1119). The emitted fatal default calls this only where the other three
    /// conditions already hold — checking is enabled (the raise happened), no declarative or WHEN phrase qualified,
    /// and the element was compiled under PROPAGATE ON — so what is decided here is the name exclusion, and the
    /// staging IS <see cref="SetPropagatingLast"/>: the condition just raised is the last exception status. Returns
    /// false (stage nothing) for the two excluded names, whose #7 termination then stands. The caller performs the
    /// GOBACK's return.</summary>
    public bool StageAutomaticPropagation(string name)
    {
        if (string.Equals(name, "EC-FLOW-GLOBAL-EXIT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "EC-FLOW-GLOBAL-GOBACK", StringComparison.OrdinalIgnoreCase))
            return false;
        SetPropagatingLast();   // "as if … RAISING LAST EXCEPTION" — no PD-header operand: a fatal name is never EC-USER
        return true;
    }

    /// <summary>AUTOMATIC PROPAGATION of an exception OBJECT no declarative of the activating element took — ISO
    /// §14.6.13.1.5, EXIT/GOBACK item 3: "if a PROPAGATE ON directive is in effect for the activating runtime
    /// element, the exception is propagated as if a GOBACK statement with the RAISING LAST EXCEPTION phrase were
    /// specified in this activating runtime element. However, if no applicable class or interface is specified in the
    /// RAISING phrase of the procedure division header in the activating element, the RAISING phrase is EXCEPTION
    /// EC-OO-EXCEPTION, instead of LAST EXCEPTION" (kb/Work PB1119). <paramref name="applicable"/> is the emitted type
    /// test against that header's classes and interfaces; the object is the current exception status here (the pickup
    /// made it so), so the LAST arm re-stages the object itself.</summary>
    public void StageAutomaticObjectPropagation(bool applicable)
    {
        if (applicable) { SetPropagatingLast(); return; }
        bool fatal = !ExceptionCatalog.TryGet(OoException, out var info) || info.IsFatal;   // Table 13, never a literal
        SetPropagating(OoException, fatal);
    }

    private const string OoException = "EC-OO-EXCEPTION";

    private static string[] Names(string[]? names) => names ?? [];

    /// <summary>THE ACTIVATOR-SIDE RAISE of a staged <c>GOBACK / EXIT … RAISING</c> condition — ISO §14.9.18.4
    /// GR1 b), written in the one element the rule names. Take the staged condition; raise it here only if
    /// checking for THAT condition is enabled in THIS element at THIS statement
    /// (<paramref name="activatorChecking"/> is the activating statement's §7.3.25 profile); when it is, update
    /// the last exception status (§14.6.13.1.1 — "the associated exception condition is raised, the last
    /// exception status is set to indicate that exception condition") and report true so the site runs the
    /// §14.9.49 Format-3 selection and the §14.6.13.1.3/.4 disposition.
    ///
    /// <para>When it is NOT enabled the condition is discarded and false is reported: §14.6.13.1.1's
    /// contrapositive — "if checking for an exception that occurs is not enabled, no exception condition is
    /// raised" — so there is nothing for §14.6.13.1.3 to dispose of, fatal or not. (The FATAL arm used to
    /// terminate the run unit from inside the CALLEE, citing §14.6.13.1.3 #8; that rule's latitude governs what
    /// may happen once a fatal condition EXISTS, and GR1 b) stops it coming into existence in an unchecked
    /// activator at all — the same misapplication <see cref="Control.ProgramTable"/> already had removed on the
    /// boundary-default path. kb/Work PB408.)</para></summary>
    public bool TakeRaisedPropagation(string activatorChecking, out string name, out bool fatal)
    {
        name = "";
        fatal = false;
        if (_propagated is not { } p || !IsForThisActivation(p.For)) return false;   // not staged for THIS activation
        _propagated = null;
        if (!EcCheckingProfile.Of(activatorChecking).Enabled(p.Name)) return false;   // GR1b — not raised here
        Set(p.Name, p.Fatal, p.Statement, p.Location);
        name = p.Name;
        fatal = p.Fatal;
        return true;
    }

    // ── The ambient checking flags: ONE save/restore discipline (kb/Work PB891 / PB841) ──────────────────────

    /// <summary>Every ambient <c>…Checking</c> flag as ONE value — see <see cref="CheckingFlags"/> for why this
    /// is a struct and not loose fields. The public properties below delegate to it, so generated code and every
    /// runtime raise site are unaffected by the storage shape.</summary>
    private CheckingFlags _checking;

    /// <summary>Open a checking SCOPE that keeps the current state: return it so the scope's <c>finally</c> can
    /// hand it back to <see cref="RestoreChecking"/>. A statement guard takes this before it sets the flags its
    /// own statement enables, so leaving the statement RESTORES what was in force — it never assumes "off"
    /// (kb/Work PB891: the former <c>= false</c> reset cleared an ENCLOSING statement's enable, and the raise
    /// site it guarded later in the same statement read "not enabled").</summary>
    public CheckingFlags SaveChecking() => _checking;

    /// <summary>Open a checking scope at the <b>BASELINE</b> — return the current state and disable ALL of it.
    /// Pair with <see cref="RestoreChecking"/> in a <c>finally</c>.
    ///
    /// <para>Enablement is a property of the SOURCE TEXT at the executing statement (§7.3.25.4 GR6: checking "is
    /// enabled for the procedure division statements and procedure division headers that follow in the
    /// compilation group"; GR5: a TURN inside a statement "applies to any succeeding statement … whether or not
    /// that succeeding statement is within the scope of the statement in which the TURN directive is
    /// specified"). So wherever control reaches OTHER source statements while a statement guard's flags are
    /// standing, those statements start from all-off and set only what their own lines enable: a nested
    /// statement list inside a guarded statement, a procedure range run by a guarded PERFORM / SORT / MERGE, a
    /// USE procedure or exception-checking PERFORM handler (<c>__RunUse</c>), and an ACTIVATION — a CALL or
    /// function activation (<c>ProgramTable.CallProgram</c>) or a method body. §14.9.28.4 GR14's "implicit PUSH
    /// ALL followed by TURN OFF ALL" for imp-2..imp-5 is one instance of this, not a separate mechanism.</para></summary>
    public CheckingFlags PushAllCheckingOff()
    {
        var saved = _checking;
        _checking = default;   // TURN OFF ALL — every flag, including any added after this was written
        return saved;
    }

    /// <summary>Close a checking scope opened by <see cref="SaveChecking"/> or <see cref="PushAllCheckingOff"/>:
    /// restore the ambient state it returned (GR14's <b>implicit POP ALL</b> is this).</summary>
    public void RestoreChecking(CheckingFlags saved) => _checking = saved;

    // ── §14.6.13.1.1's RAISE RULE, WRITTEN ONCE ───────────────────────────────────────────────────────────────
    //
    // "If checking for an exception condition is enabled and an exception status indicator is set as a result of an
    // exception detected during the execution of a statement, the associated exception condition is raised, the
    // last exception status is updated …" (§14.6.13.1.1) — and, the same clause's contrapositive, "if checking for
    // an exception that occurs is not enabled, no exception condition is raised". Every per-condition helper below
    // is THAT ONE rule applied to ONE (ambient checking flag, exception-name) pair, so the rule lives here and each
    // helper is a single expression naming its pair.
    //
    // ⛔ WHY THIS IS A METHOD AND NOT A SHAPE TO COPY (kb/Work PB676). The rule used to be transcribed by hand once
    // per condition — twenty-two fatal copies of the identical five-line
    // `if (<X>Checking) { Set("<EC>", fatal: true); throw new CobolFatalException("<EC>", detail); }` plus two
    // nonfatal ones. A copy written with the wrong `fatal:` argument, a missing `Set`, or a NEIGHBOUR's checking
    // flag reads exactly like its twenty-one siblings, and the last of those is silent in BOTH directions: the
    // condition never raises where the emitter enabled it, and raises where the emitter did not.
    // `ExceptionRaiseHelperDriftTests` proves, per helper and behaviourally, that the flag a helper READS is the
    // flag `EcEmitter`'s gate table SETS for the name that helper raises, and that the fatality it records is the
    // one ExceptionCatalog carries from Table 13.

    /// <summary>Raise a Table 13 FATAL exception condition when <paramref name="enabled"/> — checking for it at this
    /// statement (§14.6.13.1.1) — is true: record the last exception status and throw
    /// <see cref="CobolFatalException"/>, which the emitted statement guard catches for the §14.6.13.1.3 #4/#5
    /// dispatch (a USE declarative, an exception-checking PERFORM's WHEN) and otherwise carries to the run-unit
    /// boundary for #7's abnormal termination. When it is false this RETURNS and the caller's documented lenient
    /// outcome stands — "no exception condition is raised", so nothing is recorded either.</summary>
    private void FatalIfEnabled(bool enabled, string ec, string detail)
    {
        if (!enabled) return;
        Set(ec, fatal: true);
        throw new CobolFatalException(ec, detail);
    }

    /// <summary>Record a Table 13 NONFATAL exception condition when <paramref name="enabled"/> is true — and RUN
    /// THE §14.6.13.1.4 #3 SELECTION, which is the half a runtime raise site used to skip. Nothing is recorded
    /// when checking is off (§14.6.13.1.1). There is no <c>detail</c> operand: a nonfatal condition carries no
    /// exception object to attach one to.
    /// <para>§14.6.13.1.4: "If checking for that exception condition is enabled, processing of the statement is
    /// INTERRUPTED and one of the following occurs in the order specified … 3) If there is an applicable USE
    /// statement in the source unit that specifies the exception-name associated with the exception condition …
    /// the associated declarative is executed … 4) Execution of the statement continues as specified in the
    /// rules for that statement." #3 and #4 are ALTERNATIVES, so the outcome is decided by whether a declarative
    /// qualified:</para>
    /// <list type="bullet">
    /// <item><c>-3</c> — no qualifying declarative (or this element has no F3 machinery at all): #4, the raise
    /// site returns and its caller's documented outcome stands (the substitution character is stored, the table
    /// grows, the range is empty).</item>
    /// <item><c>-1</c> — the declarative RAN and completed normally (§14.6.13.1.2). #3's own sentence places the
    /// return: "If execution of the declarative completes normally, execution continues as specified in the
    /// RULES FOR NORMAL EXECUTION", and every condition reaching this method has a rule that names that
    /// continuation outright — §14.7.8 rule 2 ("upon completion of any exception processing, execution proceeds
    /// as if the range of values were empty"), §8.5.1.9.6 GR1 (the capacity change happens), §14.9.39 Format 16
    /// GR37/GR38 (the length is 0 / clamped), §15.19.4 r3 (the substitution character is used). So the raise site
    /// returns and the statement finishes. ⚠ §14.9.49.4 GR13 a) — "control is returned to an implicit CONTINUE
    /// statement following the statement whose execution caused the exception" — reads as the alternative for a
    /// condition whose statement rules name NO continuation (a RAISE, a size error: there the raise is at the end
    /// of the statement's own execution and the two readings coincide). Where a specific rule DOES name one, #3's
    /// "the rules for normal execution" is that rule, and it is the only reading under which §14.7.8's empty range
    /// is observable at all. Recorded as PB367b's determination.</item>
    /// <item><c>-2</c> / <c>≥ 0</c> — RESUME AT NEXT STATEMENT (§14.9.33.4 GR2) / RESUME AT procedure-name
    /// (GR3, ≡ GO TO): an explicit transfer of control out of the interrupted statement, which unwinds the rest
    /// of it through <see cref="RaiseResumeSignal"/> — the raise site's own leg of the unwind, landing at the
    /// emitted nonfatal-gate wrapper (the declarative's own <see cref="ResumeSignal"/> leg ended at
    /// <c>__RunUse</c>, which is where this result came from).</item>
    /// </list>
    /// <para>⛔ The dispatcher is the ACTIVATION's, never a captured one: <see cref="NonfatalDispatcher"/> is set
    /// by the activation boundary (<c>ProgramTable</c>) to the runtime element now executing, so a condition
    /// raised inside a CALLed program selects THAT program's declaratives (§14.9.49.4 GR4 a) and never the
    /// activator's.</para></summary>
    /// <param name="nextStatementContinues">True for a condition whose own rule names RESUME AT NEXT STATEMENT as
    /// a CONTINUATION of the interrupted operation rather than a transfer out of it — today only EC-BOUND-OVERFLOW,
    /// whose §8.5.1.9.6 1) says a declarative "that executes a RESUME statement with the NEXT STATEMENT phrase"
    /// leaves "the operation … allowed to continue, thus exceeding the receiving table's specified expected
    /// capacity" (kb/Work PB1269). The raise site then returns as it does for a declarative that completed
    /// normally; the statement finishes and control reaches the next statement, which is where the resume was
    /// going. RESUME AT procedure-name (≥ 0) still unwinds.</param>
    private void NonfatalIfEnabled(bool enabled, string ec, bool nextStatementContinues = false)
    {
        if (!enabled) return;
        Set(ec, fatal: false);
        int r = NonfatalDispatcher?.NonfatalDispatch(ec) ?? NoDeclarative;
        // -1 (#3, the declarative completed normally) and -3 (#4, none qualified) both leave the statement to
        // finish under its own rules; only an explicit RESUME transfers control out of it.
        if (r is DeclarativeCompleted or NoDeclarative) return;
        if (nextStatementContinues && r == ResumeSignal.NextStatement) return;
        throw new RaiseResumeSignal(r);   // §14.9.33.4 GR2 (-2 ≡ ResumeSignal.NextStatement) / GR3 (≥ 0)
    }

    /// <summary>The dispatch result protocol's "the declarative completed normally" (§14.6.13.1.2) — shared with
    /// the generated <c>__RunUse</c> / <c>__EcDispatch</c> (EcEmitter documents the protocol).</summary>
    private const int DeclarativeCompleted = -1;

    /// <summary>The dispatch result protocol's "no qualifying declarative" (§14.9.49.4 GR3 g) last sentence).</summary>
    private const int NoDeclarative = -3;

    /// <summary>The runtime element whose declaratives §14.6.13.1.4 #3 selects over — the ACTIVATION now
    /// executing, installed and restored by the activation boundary (<c>ProgramTable.RunMain</c> /
    /// <c>CallProgram</c>), null outside any COBOL activation. A generated program class overrides
    /// <see cref="INonfatalSelector.NonfatalDispatch"/> only when it has Format-3 selection machinery; every other
    /// element takes the interface's default and answers <see cref="NoDeclarative"/>, which is why installing it
    /// unconditionally is correct and why an element with no declaratives costs nothing.
    /// <para>⛔ This is the channel a RUNTIME raise site reaches a declarative through, and it has to be the
    /// ACTIVATION's rather than the statement's: the ambient checking flags are run-unit state that a CALL does
    /// not currently save (see kb/Work PB367b's report), so binding the selector to the raising STATEMENT would
    /// let a callee's raise run the ACTIVATOR's declaratives — a wrong-program dispatch. Bound to the activation,
    /// the callee's own (possibly default) selector answers.</para></summary>
    public INonfatalSelector? NonfatalDispatcher { get; set; }

    // ── EC-ARGUMENT-FUNCTION ambient statement gate ───────────────────────────────────────────────────────────

    /// <summary>True while the currently-executing statement has EC-ARGUMENT-FUNCTION checking enabled (set and
    /// reset by the generated statement guard). Intrinsic-function domain-error sites consult it: when set, the
    /// §15.3 default-result-0 convention is replaced by raising EC-ARGUMENT-FUNCTION (fatal — Table 13); when
    /// clear, the default result stands (§14.6.13.1.4). Ambient (not a per-call argument) because intrinsic
    /// calls render inline inside arbitrary expressions — threading a mask through every runtime signature would
    /// fork each intrinsic into checked/unchecked twins. Run-unit-scoped since P8 (was process-global).</summary>
    public bool ArgumentFunctionChecking
    {
        get => _checking.ArgumentFunction;
        set => _checking.ArgumentFunction = value;
    }

    /// <summary>Raise EC-ARGUMENT-FUNCTION for an intrinsic argument/domain error when checking is enabled
    /// (Table 13: Fatal — thrown as <see cref="CobolFatalException"/>, caught by the statement guard for USE F3
    /// dispatch, else terminating the run unit per §14.6.13.1.3 #7); otherwise return 0, the §15.3
    /// default-result convention.</summary>
    public long ArgumentError(string detail)
    {
        FatalIfEnabled(ArgumentFunctionChecking, "EC-ARGUMENT-FUNCTION", detail);
        return 0;
    }

    /// <summary>The TEXT twin of <see cref="ArgumentError(string)"/>: raise EC-ARGUMENT-FUNCTION identically —
    /// same fatal path, same checking gate — and, checking off, hand back a <b>zero-length value</b> as the
    /// result of the function reference instead of the numeric zero.</summary>
    /// <remarks><para>⛔ ONE MECHANISM, ONE PLACE (kb/Work PB383). The numeric half of "raise, then substitute"
    /// has always lived in exactly one place — <see cref="ArgumentError(string)"/>'s own <c>return 0</c>. The
    /// text half did not: every string-returning guard raised in one statement and wrote its own substitute in
    /// the next, and two statements that must agree eventually do not. <c>CobolIntrinsics.BooleanOfInteger</c>
    /// answered the one-position boolean <c>"0"</c> where its sibling <c>CobolIntrinsics.BaseConvert</c>
    /// answered the zero-length value, for the SAME documented determination. Raise and substitute are one
    /// expression here, so the pair cannot drift.</para>
    /// <para>⚠ THIS METHOD DOES NOT DECIDE THAT THE ANSWER IS ZERO LENGTH — the CALL SITE's citation does, and
    /// the two provenances are different obligations that must each be cited on their own:</para>
    /// <list type="bullet">
    /// <item>the STANDARD itself mandates it — §15.87.4 rule 1, "the returned value is of zero length"; or</item>
    /// <item>the standard hands the result to the implementor (§15.3 rule 14, §15.4) and docs/CONFORMANCE.md
    /// writes the determination down — row <c>DOC-A.1-90</c>, whose zero-length class is every function whose
    /// returned LENGTH is itself derived from the rejected argument, and row <c>DOC-A.1-93</c>, a returned
    /// value past the documented 8,191-position maximum.</item>
    /// </list>
    /// <para>So a rejected text function does NOT automatically come here. Its sibling
    /// <see cref="ArgumentErrorSpaces(string, int)"/> carries row DOC-A.1-90's OTHER text class — the general
    /// alphanumeric/national "spaces" clause, for a function whose returned length is fixed by something that
    /// SURVIVES the rejection (the function itself for <c>CHAR</c> / <c>CHAR-NATIONAL</c>, the locale for the
    /// <c>LOCALE-DATE</c> / <c>LOCALE-TIME</c> family).</para></remarks>
    public string ArgumentErrorZeroLength(string detail)
    {
        ArgumentError(detail);      // the raise, the fatal path and the checking gate stay in ONE place
        return ArgumentSubstitute.ZeroLength;
    }

    /// <summary>The OTHER text twin of <see cref="ArgumentError(string)"/>: raise EC-ARGUMENT-FUNCTION
    /// identically — same fatal path, same checking gate — and, checking off, hand back
    /// <paramref name="positions"/> <b>SPACES</b>, docs/CONFORMANCE.md row <c>DOC-A.1-90</c>'s general clause
    /// for an alphanumeric or national result ("the zero value of the type the function returns").</summary>
    /// <remarks><para>⚠ THIS METHOD DOES NOT DECIDE EITHER THE CLASS OR THE LENGTH — the CALL SITE's citation
    /// does, which is why <paramref name="positions"/> has no default. Row DOC-A.1-90 settles the class by
    /// asking what determines the returned item's LENGTH:</para>
    /// <list type="bullet">
    /// <item>the REJECTED argument determines it — nothing survives, so the answer is the zero-length value and
    /// the site belongs at <see cref="ArgumentErrorZeroLength(string)"/> instead;</item>
    /// <item>the FUNCTION fixes it — <c>CHAR</c> / <c>CHAR-NATIONAL</c> return exactly one character position
    /// (§15.15.4 / §15.16.4), so one space;</item>
    /// <item>something else that SURVIVES the rejection fixes it — the LOCALE, for <c>LOCALE-DATE</c>,
    /// <c>LOCALE-TIME</c> and <c>LOCALE-TIME-FROM-SECONDS</c> (§15.52.4 / §15.53.4 / §15.54.4 rule 3, "the
    /// length of the returned value depends on the format indicated in the locale"). Rejecting argument-1 does
    /// not disturb the locale, so the zero-length class does not reach these and the general clause does. The
    /// locale's format is a culture pattern (DETERMINATION L10) whose rendered width varies with the VALUE as
    /// well as the format, so no single "the length the locale would have produced" exists once the value is
    /// rejected; the standard's own answer for an alphanumeric function whose length is content-derived and
    /// whose content is absent is <b>one alphanumeric space character</b> (§15.30.3 rule 1), and row DOC-A.1-90
    /// adopts it here (kb/Work PB470).</item>
    /// </list></remarks>
    public string ArgumentErrorSpaces(string detail, int positions)
    {
        ArgumentError(detail);      // the raise, the fatal path and the checking gate stay in ONE place
        return ArgumentSubstitute.Spaces(positions);
    }

    // ── EC-DATA-CONVERSION ambient statement gate (CONVERT / DISPLAY-OF / NATIONAL-OF) ────────────────────────

    /// <summary>True while the currently-executing statement has EC-DATA-CONVERSION checking enabled (the
    /// nonfatal twin of <see cref="ArgumentFunctionChecking"/>).</summary>
    public bool DataConversionChecking
    {
        get => _checking.DataConversion;
        set => _checking.DataConversion = value;
    }

    /// <summary>Record EC-DATA-CONVERSION for an untranslatable repertoire value — CONVERT (§15.19.4 r1/r3)
    /// and the argument-2-unspecified DISPLAY-OF/NATIONAL-OF forms (§15.26.4 r3 / §15.66.4 r3). Nonfatal
    /// (Table 13), so it never throws; it only sets the last exception status, and only while checking for the
    /// condition is enabled (§14.6.13.1.1). The substitution character is applied by the caller regardless.</summary>
    public void DataConversionError(string detail) => NonfatalIfEnabled(DataConversionChecking, "EC-DATA-CONVERSION");

    // ── EC-BOUND-OVERFLOW ambient statement gate (OCCURS DYNAMIC implicit growth past expected capacity) ───────

    /// <summary>True while the currently-executing statement has EC-BOUND-OVERFLOW checking enabled (the
    /// nonfatal twin of <see cref="DataConversionChecking"/>). A dynamic-capacity table's implicit growth past
    /// its expected (TO) capacity consults it.</summary>
    public bool BoundOverflowChecking
    {
        get => _checking.BoundOverflow;
        set => _checking.BoundOverflow = value;
    }

    /// <summary>Record EC-BOUND-OVERFLOW when an implicit capacity change of a dynamic-capacity table (a receiving
    /// subscript, §8.5.1.9.3; a variable-length group transfer's recreation, §14.6.9.2) first exceeds its expected
    /// capacity (§8.5.1.9.6 1) — an already-exceeded implicit change raises nothing; <c>CobolDynTable</c> asks
    /// that). Nonfatal (Table 13); it sets the last exception status only while checking is enabled
    /// (§14.6.13.1.1). The change proceeds regardless — after a declarative that completes normally AND after one
    /// that executes RESUME AT NEXT STATEMENT, which §8.5.1.9.6 1) names as this condition's continuation
    /// (kb/Work PB1269).</summary>
    public void BoundOverflowError(string detail) =>
        NonfatalIfEnabled(BoundOverflowChecking, "EC-BOUND-OVERFLOW", nextStatementContinues: true);

    // ── EC-BOUND-SET ambient statement gate (OCCURS DYNAMIC explicit SET past the expected capacity) ─────────────

    /// <summary>True while the currently-executing statement has EC-BOUND-SET checking enabled. A dynamic-capacity
    /// table's explicit capacity SET (<c>CobolDynTable.SetCapacity</c>) consults it.</summary>
    public bool BoundSetChecking
    {
        get => _checking.BoundSet;
        set => _checking.BoundSet = value;
    }

    /// <summary>Record EC-BOUND-SET when a SET Format 14 statement gives a dynamic-capacity table a new capacity
    /// above its expected maximum capacity (§14.9.39.4 GR30, second arm — every such SET, not only the first
    /// crossing). Nonfatal (Table 13), so it never throws on its own account; the change follows the raise (kb/Work PB460).</summary>
    public void BoundSetError(string detail) => NonfatalIfEnabled(BoundSetChecking, "EC-BOUND-SET");

    // ── EC-RANGE-INVALID ambient statement gate (an inverted alphanumeric/national THROUGH range) ──────────────

    /// <summary>True while the currently-executing statement has EC-RANGE-INVALID checking enabled. The THROUGH
    /// range membership test (<c>CobolString.ThruMember</c> / <c>CobolCollation.ThruMember</c>) consults it.</summary>
    public bool RangeInvalidChecking
    {
        get => _checking.RangeInvalid;
        set => _checking.RangeInvalid = value;
    }

    /// <summary>Record EC-RANGE-INVALID for a THROUGH range whose starting value collates AFTER its ending value
    /// (§14.7.8 rule 2 — a level-88 VALUE THRU or an EVALUATE WHEN range). Nonfatal (Table 13): the range is
    /// treated as empty whether or not the condition is raised.</summary>
    public void RangeInvalidError(string detail) => NonfatalIfEnabled(RangeInvalidChecking, "EC-RANGE-INVALID");

    // ── EC-STORAGE-NOT-AVAIL ambient statement gate (SET [SIZE OF] dynamic-length-item TO …) ───────────────────

    /// <summary>True while the currently-executing statement has EC-STORAGE-NOT-AVAIL checking enabled. The
    /// dynamic-length resize site (<c>CobolDynString.SetSize</c>) consults it.</summary>
    public bool StorageNotAvailChecking
    {
        get => _checking.StorageNotAvail;
        set => _checking.StorageNotAvail = value;
    }

    /// <summary>Record EC-STORAGE-NOT-AVAIL for a dynamic-length resize whose evaluated value is not nonnegative
    /// (§14.9.39 Format 16 GR37) or exceeds the item's maximum size (GR38). Nonfatal (Table 13): the length is
    /// set to 0 / clamped to the maximum whether or not the condition is raised. (The ALLOCATE raise of the same
    /// name is detected by the GENERATED code — §14.9.3.4 GR5c — and dispatches at its own site.)</summary>
    public void StorageNotAvailError(string detail) => NonfatalIfEnabled(StorageNotAvailChecking, "EC-STORAGE-NOT-AVAIL");

    // ── EC-BOUND-REF-MOD ambient statement gate (reference modification out of bounds / zero-length) ───────────

    /// <summary>True while the currently-executing statement has EC-BOUND-REF-MOD checking enabled (the fatal
    /// twin of <see cref="ArgumentFunctionChecking"/>). Reference-modification evaluation sites consult it.</summary>
    public bool BoundRefModChecking
    {
        get => _checking.BoundRefMod;
        set => _checking.BoundRefMod = value;
    }

    /// <summary>Raise EC-BOUND-REF-MOD for a reference-modification whose leftmost-position or length is out of
    /// range — a zero-length result (unless the REF-MOD-ZERO-LENGTH directive is in effect), a specified negative
    /// length (never relaxable, review C14), a leftmost &lt; 1, or a position outside the data item (ISO §8.4.3.3.4
    /// item 5b/5c, spec :7085-7089; Table 13 Fatal). When checking is
    /// enabled it throws <see cref="CobolFatalException"/> (caught by the statement guard for USE F3 dispatch, else
    /// terminating the run unit per §14.6.13.1.3 #5/#7); when checking is OFF it returns and the caller's lenient
    /// clamp/space-pad default stands (byte-identical to a pre-slice build).</summary>
    public void RefModError(string detail) => FatalIfEnabled(BoundRefModChecking, "EC-BOUND-REF-MOD", detail);

    // ── The pointer fatal ECs (CA9): EC-DATA-PTR-NULL / EC-BOUND-PTR / EC-SIZE-ADDRESS ────────────────────────
    //
    // ⛔ CHECKING-OFF IS NOT UNIFORM ACROSS THESE THREE, and the split is the owner's decided rule (2026-07-28):
    // LENIENT wherever the standard NAMES the outcome, LOUD ABORT wherever it names none.
    //   · SET pointer UP/DOWN BY — §14.9.39 Format 10 GR19 names it: "the execution of the SET statement is
    //     unsuccessful, and the content of identifier-9 is unchanged". So with checking off these RETURN and the
    //     caller leaves the operand alone.
    //   · A DEREFERENCE — §13.18.5.4 GR3/GR4 name NO outcome, and `CobolPtr.Deref` must return a StorageCell, so
    //     "lenient" there would mean fabricating a cell and continuing on garbage. It keeps its unconditional
    //     throw. A five-compiler survey (GnuCOBOL, gcobol, Micro Focus, IBM Enterprise COBOL, NetCOBOL) found
    //     every one of them hard-stops a null dereference; "checking off" nowhere means lenient, it means
    //     UNGUARDED, and only our managed StorageCell makes leniency reachable at all.

    /// <summary>True while the currently-executing statement has EC-DATA-PTR-NULL checking enabled (fatal).</summary>
    public bool DataPtrNullChecking
    {
        get => _checking.DataPtrNull;
        set => _checking.DataPtrNull = value;
    }

    /// <summary>Raise EC-DATA-PTR-NULL (§13.18.5.4 GR3 / §14.9.39 Format 10 GR18; Table 13 Fatal) when checking
    /// is enabled. When it is OFF this RETURNS and the caller applies GR19's unchanged-operand outcome — used by
    /// the SET UP/DOWN BY sites only; a dereference never routes through here (see the note above).</summary>
    public void DataPtrNullError(string detail) => FatalIfEnabled(DataPtrNullChecking, "EC-DATA-PTR-NULL", detail);

    /// <summary>True while the currently-executing statement has EC-BOUND-PTR checking enabled (fatal).</summary>
    public bool BoundPtrChecking
    {
        get => _checking.BoundPtr;
        set => _checking.BoundPtr = value;
    }

    /// <summary>Raise EC-BOUND-PTR (§13.18.5.4 GR4; Table 13 Fatal) when checking is enabled; otherwise return
    /// and let the caller apply its unchanged-operand outcome.</summary>
    public void BoundPtrError(string detail) => FatalIfEnabled(BoundPtrChecking, "EC-BOUND-PTR", detail);

    /// <summary>True while the currently-executing statement has EC-SIZE-ADDRESS checking enabled (fatal).</summary>
    public bool SizeAddressChecking
    {
        get => _checking.SizeAddress;
        set => _checking.SizeAddress = value;
    }

    /// <summary>Raise EC-SIZE-ADDRESS for a SET pointer UP/DOWN BY amount that "does not evaluate to an integer"
    /// (§14.9.39.4 GR19; Table 13 Fatal) when checking is enabled; otherwise return, and GR19's "the execution of
    /// the SET statement is unsuccessful, and the content of identifier-9 is unchanged" stands.
    /// <para>⛔ GR19 is a test on the amount's VALUE and nothing else. A magnitude no address can hold is
    /// <see cref="RangePtrError"/>'s rule — folding it in here reported a condition whose antecedent was FALSE
    /// and terminated run units on legal COBOL (kb/Work PB465).</para></summary>
    public void SizeAddressError(string detail) => FatalIfEnabled(SizeAddressChecking, "EC-SIZE-ADDRESS", detail);

    /// <summary>True while the currently-executing statement has EC-RANGE-PTR checking enabled (fatal). The
    /// displacement site in <see cref="CobolPtr"/> consults it.</summary>
    public bool RangePtrChecking
    {
        get => _checking.RangePtr;
        set => _checking.RangePtr = value;
    }

    /// <summary>Raise EC-RANGE-PTR when a SET pointer UP/DOWN BY produces an address "outside the range of
    /// values allowed by the implementor for a data-pointer data item" (§14.9.39.4 GR20; Table 13 Fatal) — the
    /// range being DOC-A.1-216 in <c>docs/CONFORMANCE.md</c> §7, as §A.1 216 requires. When checking is not
    /// enabled this returns and GR20's own named outcome stands: "the value of the data item referenced by
    /// identifier-9 is unchanged".
    /// <para>⛔ The condition had NO raise site at all until kb/Work PB465 — the catalog carried its Table 13 row
    /// while <see cref="CobolPtr"/> answered every out-of-range displacement with EC-SIZE-ADDRESS or with a
    /// silent 64-bit wrap, so a grep for the name found the row and the condition read as wired.</para></summary>
    public void RangePtrError(string detail) => FatalIfEnabled(RangePtrChecking, "EC-RANGE-PTR", detail);

    // ── The table-bound fatal ECs (CA10): EC-BOUND-SUBSCRIPT / EC-BOUND-ODO ───────────────────────────────────
    // Checking-OFF stays LENIENT for both, per the owner's rule, and here the standard supplies the outcome to
    // be lenient WITH: §13.18.38.4 GR7 ends "The content of a data item whose occurrence number exceeds the
    // value of the data item referenced by data-name-1 is undefined", so the existing scratch-slot read and the
    // [0,max] clamp are conforming implementor choices. Only the NAMED condition is new.

    /// <summary>True while the currently-executing statement has EC-BOUND-SUBSCRIPT checking enabled (fatal).</summary>
    public bool BoundSubscriptChecking
    {
        get => _checking.BoundSubscript;
        set => _checking.BoundSubscript = value;
    }

    /// <summary>Raise EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2: "If the value of the subscript is not a positive
    /// integer or is less than one or is greater than the highest permissible occurrence number, the
    /// EC-BOUND-SUBSCRIPT exception condition is set to exist"; Table 13 Fatal) when checking is enabled;
    /// otherwise return and the caller's scratch-slot read stands, byte-identical to a pre-EC build.</summary>
    public void SubscriptError(string detail) => FatalIfEnabled(BoundSubscriptChecking, "EC-BOUND-SUBSCRIPT", detail);

    /// <summary>True while the currently-executing statement has EC-BOUND-ODO checking enabled (fatal).</summary>
    public bool BoundOdoChecking
    {
        get => _checking.BoundOdo;
        set => _checking.BoundOdo = value;
    }

    /// <summary>Raise EC-BOUND-ODO (§13.18.38.4 GR7: the value of the data item referenced by data-name-1
    /// "shall fall within the bounds from integer-1 through integer-2"; Table 13 Fatal) when checking is
    /// enabled; otherwise return and the caller's clamp stands.</summary>
    public void OdoError(string detail) => FatalIfEnabled(BoundOdoChecking, "EC-BOUND-ODO", detail);

    // ── EC-RANGE-INDEX ambient statement gate (an index driven outside the implementor range) ─────────────────
    //
    // ⛔ THE CONDITION HAD NO RAISE SITE AT ALL until kb/Work PB459 — the catalog carried its Table 13 row and
    // FlagConformancePass asked `_turn.Enabled("EC-RANGE-INDEX", …)` to FLAG a SET, so a grep for the name
    // returned four hits and the condition read as wired while it could never occur.

    /// <summary>True while the currently-executing statement has EC-RANGE-INDEX checking enabled (fatal).
    /// The index store/augment sites in <see cref="CobolIndex"/> consult it.</summary>
    public bool RangeIndexChecking
    {
        get => _checking.RangeIndex;
        set => _checking.RangeIndex = value;
    }

    /// <summary>Raise EC-RANGE-INDEX when a PERFORM VARYING, SEARCH or SET "creates a value for the index that is
    /// outside the range of the values allowed by the implementor" (§13.18.38.4 GR2 a; §14.9.39.4 GR2 a) 1. b /
    /// 2. a / 3. b and GR4 a) name the same limit; Table 13 Fatal) when checking is enabled; otherwise return, and
    /// the caller applies the SET rules' own named outcome — "the execution of the SET statement is unsuccessful,
    /// and the content of the receiving operand is unchanged" — which is also §13.18.38.4 GR2 b's "the value of
    /// the index is undefined UNLESS the value of the index is specified by the rules of that statement".</summary>
    public void RangeIndexError(string detail) => FatalIfEnabled(RangeIndexChecking, "EC-RANGE-INDEX", detail);

    // ── EC-RANGE-INSPECT-SIZE ambient statement gate (kb/Work PB1126) ─────────────────────────────────────────
    //
    // ⛔ CATALOGUED, NEVER RAISED, until PB1126: the runtime skipped a mis-sized REPLACING operand and clamped
    // CONVERTING to the common prefix under a comment promising that "the 2002+ EC model will raise".

    /// <summary>True while the currently-executing INSPECT has EC-RANGE-INSPECT-SIZE checking enabled (fatal).
    /// <c>CobolInspect.Replace</c> / <c>Convert</c> consult it.</summary>
    public bool RangeInspectSizeChecking
    {
        get => _checking.RangeInspectSize;
        set => _checking.RangeInspectSize = value;
    }

    /// <summary>Raise EC-RANGE-INSPECT-SIZE when an INSPECT operand pair is not of equal size (ISO §14.9.22.4 GR14,
    /// GR15, GR22: "the EC-RANGE-INSPECT-SIZE exception condition is set to exist and the results of the execution
    /// of the INSPECT statement are undefined"; Table 13 Fatal) when checking is enabled; otherwise return, and the
    /// caller's deterministic outcome for the undefined result stands.</summary>
    public void RangeInspectSizeError(string detail) =>
        FatalIfEnabled(RangeInspectSizeChecking, "EC-RANGE-INSPECT-SIZE", detail);

    /// <summary>True while EC-PROGRAM-ARG-OMITTED checking is enabled (fatal).</summary>
    public bool ProgramArgOmittedChecking
    {
        get => _checking.ProgramArgOmitted;
        set => _checking.ProgramArgOmitted = value;
    }

    /// <summary>Raise EC-PROGRAM-ARG-OMITTED (§14.9.4.4 GR12: a reference to a formal parameter for which
    /// the omitted-argument condition is true, outside an argument position or the condition itself; Table 13
    /// Fatal) when checking is enabled; otherwise return and the carrier's lenient benign value stands
    /// (kb/Work PB133 wave C — the CA10 posture).</summary>
    public void ProgramArgOmittedError(string detail)
        => FatalIfEnabled(ProgramArgOmittedChecking, "EC-PROGRAM-ARG-OMITTED", detail);

    /// <summary>True while EC-FUNCTION-ARG-OMITTED checking is enabled (fatal).</summary>
    public bool FunctionArgOmittedChecking
    {
        get => _checking.FunctionArgOmitted;
        set => _checking.FunctionArgOmitted = value;
    }

    /// <summary>Raise EC-FUNCTION-ARG-OMITTED (§8.4.3.2.4 GR8: a reference, in an activated FUNCTION, to a
    /// parameter for which the omitted-argument condition is true; Table 13 Fatal) when checking is enabled;
    /// otherwise return and the lenient benign value stands (kb/Work PB971).</summary>
    public void FunctionArgOmittedError(string detail)
        => FatalIfEnabled(FunctionArgOmittedChecking, "EC-FUNCTION-ARG-OMITTED", detail);

    /// <summary>True while EC-OO-ARG-OMITTED checking is enabled (fatal).</summary>
    public bool OoArgOmittedChecking
    {
        get => _checking.OoArgOmitted;
        set => _checking.OoArgOmitted = value;
    }

    /// <summary>Raise EC-OO-ARG-OMITTED (§14.9.23.4 GR10: a reference, in an invoked METHOD, to a parameter for
    /// which the omitted-argument condition is true; Table 13 Fatal) when checking is enabled; otherwise return
    /// and the method's initial-state local stands (kb/Work PB971).</summary>
    public void OoArgOmittedError(string detail)
        => FatalIfEnabled(OoArgOmittedChecking, "EC-OO-ARG-OMITTED", detail);

    /// <summary>True while EC-OO-RESOURCE checking is enabled (fatal).</summary>
    public bool OoResourceChecking
    {
        get => _checking.OoResource;
        set => _checking.OoResource = value;
    }

    /// <summary>Raise EC-OO-RESOURCE (§16.2.1.2 GR2: "If resources needed to create a new object are not
    /// available, the returned object reference is set to NULL, and the EC-OO-RESOURCE exception condition is set
    /// to exist and is propagated back to the runtime element that invoked the New method"; Table 13 Fatal) when
    /// checking is enabled; otherwise return, and the NULL reference the standard class BASE's New returns stands
    /// (kb/Work PB1524).</summary>
    public void OoResourceError(string detail)
        => FatalIfEnabled(OoResourceChecking, "EC-OO-RESOURCE", detail);

    // ── EC-OO-UNIVERSAL: the ACTIVATOR half of the §14.9.23.4 GR7c "enabled in both" gate ─────────────────────

    /// <summary>True while the currently-executing INVOKE has EC-OO-UNIVERSAL checking enabled in the ACTIVATING
    /// runtime element. §14.9.23.4 GR7c sets the condition only "if checking for it is enabled in BOTH the
    /// activated method and the activating runtime element", and the two halves are known in different places:
    /// this flag carries the activator's, set by the emitted statement guard around the INVOKE and read by the
    /// callee's generated <c>__CobolInvoke</c>, which runs synchronously on the same run unit. The method's half
    /// cannot be a flag at all — it is a property of the CALLEE's source, so it is folded at bind time and baked
    /// as a compile-time literal per emitted method.</summary>
    public bool OoUniversalChecking
    {
        get => _checking.OoUniversal;
        set => _checking.OoUniversal = value;
    }

    // ── The dynamic-table fatal ECs (CA37/CA38): EC-FLOW-SEARCH / EC-BOUND-TABLE-LIMIT ─────────────────
    //
    // Both are LENIENT with checking off, and unusually the standard states the lenient outcome outright, which
    // is what makes the owner's rule easy here: §14.9.39.4 GR31 ends "and the SET statement is not executed",
    // GR30 "and the capacity of the table is unchanged". So with checking off these helpers RETURN and the
    // caller performs neither the SET nor the growth — a behaviour change from the previous UNCONDITIONAL throw,
    // and the one the standard describes.

    /// <summary>True while the currently-executing statement has EC-FLOW-SEARCH checking enabled (fatal).</summary>
    public bool FlowSearchChecking
    {
        get => _checking.FlowSearch;
        set => _checking.FlowSearch = value;
    }

    /// <summary>Raise EC-FLOW-SEARCH (§14.9.39.4 GR31; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the caller leaves the SET unexecuted exactly as GR31 requires.</summary>
    public void FlowSearchError(string detail) => FatalIfEnabled(FlowSearchChecking, "EC-FLOW-SEARCH", detail);

    // ──── THE SORT-MERGE FLOW conditions (§14.9.32.4 GR1, §14.9.34.4 GR1 + GR3; kb/Work PB349) ─────────────
    //
    // All three are Table 13 FATAL. The phase they test is CobolSort's (the store's procedure phase and its
    // at-end latch), and the raise precedes the statement's action, so a USE declarative that RESUMEs AT NEXT
    // STATEMENT leaves the RELEASE / RETURN unexecuted. With checking OFF the standard states no lenient outcome
    // for GR1 and calls GR3's result undefined, so the statement proceeds exactly as it did before the raise
    // existed (the deterministic refinements CobolSort documents) — §14.6.13.1.1: nothing is raised.

    /// <summary>True while the currently-executing statement has EC-FLOW-RELEASE checking enabled (fatal).</summary>
    public bool FlowReleaseChecking
    {
        get => _checking.FlowRelease;
        set => _checking.FlowRelease = value;
    }

    /// <summary>Raise EC-FLOW-RELEASE (§14.9.32.4 GR1; Table 13 Fatal) when checking is enabled; otherwise return.</summary>
    public void FlowReleaseError(string detail) => FatalIfEnabled(FlowReleaseChecking, "EC-FLOW-RELEASE", detail);

    /// <summary>True while the currently-executing statement has EC-FLOW-RETURN checking enabled (fatal).</summary>
    public bool FlowReturnChecking
    {
        get => _checking.FlowReturn;
        set => _checking.FlowReturn = value;
    }

    /// <summary>Raise EC-FLOW-RETURN (§14.9.34.4 GR1; Table 13 Fatal) when checking is enabled; otherwise return.</summary>
    public void FlowReturnError(string detail) => FatalIfEnabled(FlowReturnChecking, "EC-FLOW-RETURN", detail);

    /// <summary>True while the currently-executing statement has EC-SORT-MERGE-RETURN checking enabled (fatal).</summary>
    public bool SortMergeReturnChecking
    {
        get => _checking.SortMergeReturn;
        set => _checking.SortMergeReturn = value;
    }

    /// <summary>Raise EC-SORT-MERGE-RETURN (§14.9.34.4 GR3; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the RETURN's "undefined" result is the store's deterministic at-end-again.</summary>
    public void SortMergeReturnError(string detail) =>
        FatalIfEnabled(SortMergeReturnChecking, "EC-SORT-MERGE-RETURN", detail);

    // ──── THE SORT-MERGE STATEMENT conditions (kb/Work PB1036) ──────────────────────────────────────────────
    //
    // All four are Table 13 FATAL and each is raised by CobolSort at the point its rule names. When the statement
    // that raised one is the SORT or MERGE itself, §14.6.13.1.3 2) hands the disposition to that verb's rules —
    // the statement is terminated and execution continues after it (EcEmitter's statement guard). With checking
    // OFF nothing is raised (§14.6.13.1.1) and the statement proceeds as CobolSort documents for each.

    /// <summary>True while the currently-executing statement has EC-SORT-MERGE-ACTIVE checking enabled (fatal).</summary>
    public bool SortMergeActiveChecking
    {
        get => _checking.SortMergeActive;
        set => _checking.SortMergeActive = value;
    }

    /// <summary>Raise EC-SORT-MERGE-ACTIVE (§14.9.40.4 GR10 / GR13, §14.9.24.4 GR8; Table 13 Fatal) when checking
    /// is enabled; otherwise return.</summary>
    public void SortMergeActiveError(string detail) =>
        FatalIfEnabled(SortMergeActiveChecking, "EC-SORT-MERGE-ACTIVE", detail);

    /// <summary>True while the currently-executing statement has EC-SORT-MERGE-FILE-OPEN checking enabled (fatal).</summary>
    public bool SortMergeFileOpenChecking
    {
        get => _checking.SortMergeFileOpen;
        set => _checking.SortMergeFileOpen = value;
    }

    /// <summary>Raise EC-SORT-MERGE-FILE-OPEN (§14.9.40.4 GR9, §14.9.24.4 GR7 / GR12; Table 13 Fatal) when
    /// checking is enabled; otherwise return.</summary>
    public void SortMergeFileOpenError(string detail) =>
        FatalIfEnabled(SortMergeFileOpenChecking, "EC-SORT-MERGE-FILE-OPEN", detail);

    /// <summary>True while the currently-executing statement has EC-SORT-MERGE-RELEASE checking enabled (fatal).</summary>
    public bool SortMergeReleaseChecking
    {
        get => _checking.SortMergeRelease;
        set => _checking.SortMergeRelease = value;
    }

    /// <summary>Raise EC-SORT-MERGE-RELEASE (§13.18.43.4 GR14 b) / GR19 b), §14.9.40.4 GR12 b), §14.9.24.4
    /// GR7 b); Table 13 Fatal) when checking is enabled; otherwise return.</summary>
    public void SortMergeReleaseError(string detail) =>
        FatalIfEnabled(SortMergeReleaseChecking, "EC-SORT-MERGE-RELEASE", detail);

    /// <summary>True while the currently-executing statement has EC-SORT-MERGE-SEQUENCE checking enabled (fatal).</summary>
    public bool SortMergeSequenceChecking
    {
        get => _checking.SortMergeSequence;
        set => _checking.SortMergeSequence = value;
    }

    /// <summary>Raise EC-SORT-MERGE-SEQUENCE (§14.9.24.4 GR6; Table 13 Fatal) when checking is enabled;
    /// otherwise return.</summary>
    public void SortMergeSequenceError(string detail) =>
        FatalIfEnabled(SortMergeSequenceChecking, "EC-SORT-MERGE-SEQUENCE", detail);

    // ──── THE REPORT WRITER's four statement-precondition conditions (kb/Work PB326) ───────────────
    //
    // All four are Table 13 FATAL and all four are LENIENT with checking off, and -- as with EC-FLOW-SEARCH above
    // -- the standard states the lenient outcome outright, so the caller needs no second decision: §14.9.49.4
    // GR10 ends "the result of the execution of the GENERATE, INITIATE, or TERMINATE statement is unsuccessful,
    // and the state of the report is unchanged"; §14.9.21.4 GR2 "the execution of the INITIATE statement has no
    // other effect"; GR3 "no action is taken on the report"; §14.9.46.4 GR1 "the execution of the statement is
    // unsuccessful". The RWCS engine therefore returns without performing the statement WHETHER OR NOT checking
    // is enabled, and these helpers only decide whether the condition is also RAISED (§14.6.13.1.1).

    /// <summary>True while the currently-executing statement has EC-FLOW-REPORT checking enabled (fatal).</summary>
    public bool FlowReportChecking
    {
        get => _checking.FlowReport;
        set => _checking.FlowReport = value;
    }

    /// <summary>Raise EC-FLOW-REPORT (§14.9.49.4 GR10; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the caller leaves the GENERATE / INITIATE / TERMINATE unexecuted exactly as GR10 requires.</summary>
    public void FlowReportError(string detail) => FatalIfEnabled(FlowReportChecking, "EC-FLOW-REPORT", detail);

    // ──── THE USE-PROCEDURE RE-ENTRANCY CONDITION (§14.9.49.4 GR2; kb/Work PB368) ─────────────────────────
    //
    // GR2's SOLE normative content is the RAISE — "During the execution of a USE procedure, if a statement
    // raises an exception condition that would cause the execution of a USE procedure that had previously been
    // activated and had not yet returned control to the activating entity, the EC-FLOW-USE exception condition
    // is set to exist". It says nothing about what then happens to the declining invocation, so the generated
    // __RunUse guard's quiet decline is the §14.6.13.1.3 #8 implementor answer for checking OFF (the condition
    // is not raised, and this implementation continues by not re-entering the active procedure) and remains the
    // outcome for the raise's own unwind path. Unlike its EC-FLOW-SEARCH / EC-FLOW-REPORT neighbours the
    // standard does NOT state a lenient outcome outright — there is no statement to leave unexecuted — so the
    // decline is a recorded determination rather than a quotation (COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D11).

    /// <summary>True while the currently-executing statement has EC-FLOW-USE checking enabled (fatal).</summary>
    public bool FlowUseChecking
    {
        get => _checking.FlowUse;
        set => _checking.FlowUse = value;
    }

    /// <summary>Raise EC-FLOW-USE (§14.9.49.4 GR2; Table 13 Fatal) when checking is enabled; otherwise return,
    /// and the caller declines to re-invoke the active USE procedure (the §14.6.13.1.3 #8 implementor choice).
    /// Called from the generated <c>__RunUse</c> guard — the ONE place that knows a selected DECLARATIVE was
    /// already active — so every selection path (<c>__IoCheck</c>, <c>__IoCheckEc</c>, <c>__EcDispatch</c>,
    /// <c>__EcObjDispatch</c>, <c>__RunGlobalUse</c> and the report engine's BEFORE REPORTING hook) raises it
    /// identically. ⛔ NOT the exception-checking PERFORM's imp-2/3/4 handler slots, which share that array but
    /// are not USE procedures (§14.9.28.4 GR17, not §14.9.49.4 GR3) — the emitter excludes them by id.</summary>
    public void FlowUseError(string detail) => FatalIfEnabled(FlowUseChecking, "EC-FLOW-USE", detail);

    // ──── THE GLOBAL-DECLARATIVE GOBACK CONDITION (§14.9.18.4 GR6; kb/Work PB409) ─────────────────────────
    //
    // GR6: "If a GOBACK statement is executed within the RANGE of a declarative procedure whose USE statement
    // contains the GLOBAL phrase and that USE statement is specified in the same program as the GOBACK
    // statement, the EC-FLOW-GLOBAL-GOBACK exception condition is set to exist."
    //
    // ⛔ WITHIN THE RANGE OF, NOT SPECIFIED IN. §14.9.18.3 SR1 forbids a GOBACK *written inside* a global
    // declarative and is a COMPILE-TIME refusal (COBOLNET2102). GR6 governs LEGAL source — a GOBACK in an
    // ordinary paragraph that a global declarative PERFORMs — so it cannot be decided at bind time at all, and
    // the generated GOBACK asks the run-time question the emitted `__useActive` array already answers: is one of
    // THIS program's GLOBAL declaratives currently activated and not yet returned. "Same program" comes free:
    // `__useActive` is per program instance, and a container's global declarative selected on behalf of a
    // CONTAINED program (§14.9.49.4 GR4 b)) runs in the CONTAINER's instance, so a contained program's GOBACK
    // sees its own array and correctly raises nothing.
    //
    // ⚠ Table 13's neighbour EC-FLOW-GLOBAL-EXIT has NO helper here, and that is a DETERMINATION, not an
    // omission: §14.9.14.4 gives the Format-2 EXIT statement no general rule that sets it (GR3 imports
    // "14.9.18, GOBACK statement, General rules 3 and 4" — NOT GR6), and Table 13's third column is a
    // description, not a setting rule. The only normative statement about EXIT PROGRAM in a global declarative
    // is §14.9.14.3 SR2, which this compiler enforces at compile time. Inventing a raise site for a condition
    // the standard never sets would be behaviour with no rule behind it.

    /// <summary>True while the currently-executing statement has EC-FLOW-GLOBAL-GOBACK checking enabled
    /// (fatal).</summary>
    public bool FlowGlobalGobackChecking
    {
        get => _checking.FlowGlobalGoback;
        set => _checking.FlowGlobalGoback = value;
    }

    /// <summary>Raise EC-FLOW-GLOBAL-GOBACK (§14.9.18.4 GR6; Table 13 Fatal) when checking is enabled;
    /// otherwise return, and the GOBACK proceeds. ⛔ The lenient outcome is a RECORDED DETERMINATION, not a
    /// quotation: GR6 states only that the condition "is set to exist" and gives the GOBACK no alternative
    /// behaviour, so with checking off (§14.6.13.1.1 — the condition is not raised at all) there is nothing to
    /// leave unexecuted and the return proceeds, exactly as EC-FLOW-USE's decline is the §14.6.13.1.3 #8
    /// implementor answer.</summary>
    public void FlowGlobalGobackError(string detail) =>
        FatalIfEnabled(FlowGlobalGobackChecking, "EC-FLOW-GLOBAL-GOBACK", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-ACTIVE checking enabled (fatal).</summary>
    public bool ReportActiveChecking
    {
        get => _checking.ReportActive;
        set => _checking.ReportActive = value;
    }

    /// <summary>Raise EC-REPORT-ACTIVE (§14.9.21.4 GR2; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the INITIATE "has no other effect" exactly as GR2 requires.</summary>
    public void ReportActiveError(string detail) => FatalIfEnabled(ReportActiveChecking, "EC-REPORT-ACTIVE", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-INACTIVE checking enabled (fatal).</summary>
    public bool ReportInactiveChecking
    {
        get => _checking.ReportInactive;
        set => _checking.ReportInactive = value;
    }

    /// <summary>Raise EC-REPORT-INACTIVE (§14.9.16.4 GR7 / §14.9.46.4 GR1; Table 13 Fatal) when checking is
    /// enabled; otherwise return, and the GENERATE / TERMINATE is unsuccessful exactly as those rules require.</summary>
    public void ReportInactiveError(string detail)
        => FatalIfEnabled(ReportInactiveChecking, "EC-REPORT-INACTIVE", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-FILE-MODE checking enabled (fatal).</summary>
    public bool ReportFileModeChecking
    {
        get => _checking.ReportFileMode;
        set => _checking.ReportFileMode = value;
    }

    /// <summary>Raise EC-REPORT-FILE-MODE (§14.9.21.4 GR3, the detection half of §14.9.27.4 GR7's
    /// "the OPEN statement for a report file connector shall be executed before the execution of an INITIATE
    /// statement"; Table 13 Fatal) when checking is enabled; otherwise return, and "no action is taken on the
    /// report" exactly as GR3 requires.</summary>
    public void ReportFileModeError(string detail)
        => FatalIfEnabled(ReportFileModeChecking, "EC-REPORT-FILE-MODE", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-SUM-SIZE checking enabled (fatal).</summary>
    public bool ReportSumSizeChecking
    {
        get => _checking.ReportSumSize;
        set => _checking.ReportSumSize = value;
    }

    /// <summary>Raise EC-REPORT-SUM-SIZE (§13.18.54.4 GR3 — a sum counter addition is a size error; GR4 — a sum
    /// counter whose size error indicator is set is presented; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the caller's lenient outcome stands exactly as GR3/GR4 state it — the counter is left as the
    /// ADD with ON SIZE ERROR leaves it and its indicator is set; the printable item is filled with spaces.</summary>
    public void ReportSumSizeError(string detail)
        => FatalIfEnabled(ReportSumSizeChecking, "EC-REPORT-SUM-SIZE", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-VARYING checking enabled (fatal).</summary>
    public bool ReportVaryingChecking
    {
        get => _checking.ReportVarying;
        set => _checking.ReportVarying = value;
    }

    /// <summary>Raise EC-REPORT-VARYING (§13.18.64.4 GR5 — "If the evaluation of arithmetic-expression-1 or
    /// arithmetic-expression-2 produces a noninteger value and the VARYING clause was specified in a report
    /// description entry, the EC-REPORT-VARYING exception condition is set to exist"; Table 13 Fatal) when checking
    /// is enabled; otherwise return, and the caller's lenient outcome stands — GR5 leaves "the content of the print
    /// line" undefined (§A.2 item 63), and the counter takes the value's integer part (kb/Work PB1305).</summary>
    public void ReportVaryingError(string detail)
        => FatalIfEnabled(ReportVaryingChecking, "EC-REPORT-VARYING", detail);

    /// <summary>True while the currently-executing statement has EC-REPORT-LINE-OVERLAP checking enabled
    /// (nonfatal).</summary>
    public bool ReportLineOverlapChecking
    {
        get => _checking.ReportLineOverlap;
        set => _checking.ReportLineOverlap = value;
    }

    /// <summary>Raise EC-REPORT-LINE-OVERLAP (§13.18.35.4 GR3; Table 13 NF) when checking is enabled, running the
    /// qualifying declarative (§14.6.13.1.4). Returns whether the condition was RAISED: §14.9.16.4 GR8 /
    /// §14.9.46.4 GR5 — "If a nonfatal exception condition is raised during the execution of a GENERATE
    /// [TERMINATE] statement, execution resumes at the next report item, line, or report group" — so a true
    /// answer tells the report engine to abandon the overlapping line; with checking off nothing is raised and
    /// GR3's "the results are undefined" leaves the line to the engine.</summary>
    public bool ReportLineOverlapError(string detail)
    {
        if (!ReportLineOverlapChecking) return false;
        NonfatalIfEnabled(true, "EC-REPORT-LINE-OVERLAP");
        return true;
    }

    /// <summary>True while the currently-executing statement has EC-REPORT-PAGE-LIMIT checking enabled (nonfatal).</summary>
    public bool ReportPageLimitChecking
    {
        get => _checking.ReportPageLimit;
        set => _checking.ReportPageLimit = value;
    }

    /// <summary>Raise EC-REPORT-PAGE-LIMIT (§13.18.35.4 GR2 — a report group split between two pages; Table 13 NF)
    /// when checking is enabled. Returns whether it was RAISED — the engine then resumes at the next report group
    /// (§14.9.16.4 GR8 / §14.9.46.4 GR5; kb/Work PB1188); with checking off GR2's "the results are undefined"
    /// leaves the group to the engine.</summary>
    public bool ReportPageLimitError(string detail)
    {
        if (!ReportPageLimitChecking) return false;
        NonfatalIfEnabled(true, "EC-REPORT-PAGE-LIMIT");
        return true;
    }

    /// <summary>True while the currently-executing statement has EC-REPORT-COLUMN-OVERLAP checking enabled
    /// (nonfatal).</summary>
    public bool ReportColumnOverlapChecking
    {
        get => _checking.ReportColumnOverlap;
        set => _checking.ReportColumnOverlap = value;
    }

    /// <summary>Raise EC-REPORT-COLUMN-OVERLAP (§13.18.14.4 GR4 — two printable items of one line use a column
    /// position; Table 13 NF) when checking is enabled. Returns whether it was RAISED — the engine then resumes at
    /// the next report item, leaving the overlapping one unplaced (§14.9.16.4 GR8 / §14.9.46.4 GR5; kb/Work
    /// PB1188); with checking off GR4's "the results are undefined" leaves the item to the engine.</summary>
    public bool ReportColumnOverlapError(string detail)
    {
        if (!ReportColumnOverlapChecking) return false;
        NonfatalIfEnabled(true, "EC-REPORT-COLUMN-OVERLAP");
        return true;
    }

    /// <summary>True while the currently-executing statement has EC-REPORT-PAGE-WIDTH checking enabled (nonfatal).</summary>
    public bool ReportPageWidthChecking
    {
        get => _checking.ReportPageWidth;
        set => _checking.ReportPageWidth = value;
    }

    /// <summary>Raise EC-REPORT-PAGE-WIDTH (§13.18.14.4 GR5 — a printable item ends past the page width; Table 13
    /// NF) when checking is enabled. GR5 states the outcome itself — "the report line is truncated, and the report
    /// line is printed" — so the engine does that whether or not the condition was raised, and nothing is
    /// returned (kb/Work PB1188).</summary>
    public void ReportPageWidthError(string detail) => NonfatalIfEnabled(ReportPageWidthChecking, "EC-REPORT-PAGE-WIDTH");

    /// <summary>True while the currently-executing statement has EC-BOUND-TABLE-LIMIT checking enabled (fatal).</summary>
    public bool BoundTableLimitChecking
    {
        get => _checking.BoundTableLimit;
        set => _checking.BoundTableLimit = value;
    }

    /// <summary>Raise EC-BOUND-TABLE-LIMIT (§14.9.39.4 GR30; Table 13 Fatal) when checking is enabled; otherwise
    /// return, and the caller leaves the capacity unchanged exactly as GR30 requires.</summary>
    public void BoundTableLimitError(string detail)
        => FatalIfEnabled(BoundTableLimitChecking, "EC-BOUND-TABLE-LIMIT", detail);

    // ── EC-ORDER-NOT-SUPPORTED ambient statement gate (FUNCTION STANDARD-COMPARE, §15.85.4 r2) ────────────────
    //
    // Ambient for the reason EC-ARGUMENT-FUNCTION is: an intrinsic renders INLINE inside an arbitrary expression,
    // so the guard wraps the STATEMENT and the raise site consults the flag. Checking-OFF is LENIENT, and here
    // the implementor supplies the outcome §14.6.13.1.3 #8 asks for: the comparison answers "=", the value
    // §15.85.4 r6 gives for equal arguments — a defined, deterministic result rather than an undefined one.

    /// <summary>True while the currently-executing statement has EC-ORDER-NOT-SUPPORTED checking enabled (fatal).</summary>
    public bool OrderNotSupportedChecking
    {
        get => _checking.OrderNotSupported;
        set => _checking.OrderNotSupported = value;
    }

    /// <summary>Raise EC-ORDER-NOT-SUPPORTED (§15.85.4 r2: "If the cultural ordering table is not available on
    /// the processor, or the specified ordering level is not available, or the level number specified by
    /// argument-4 is not defined in the ordering table, the EC-ORDER-NOT-SUPPORTED exception condition is set to
    /// exist"; Table 13 Fatal) when checking is enabled; otherwise return, and the caller's documented "="
    /// result stands (§14.6.13.1.3 #8). ⚠ Nothing is recorded when checking is off — §14.6.13.1.1: "if checking
    /// for an exception that occurs is not enabled, no exception condition is raised".</summary>
    public void OrderNotSupportedError(string detail)
        => FatalIfEnabled(OrderNotSupportedChecking, "EC-ORDER-NOT-SUPPORTED", detail);

    // ── The EC-LOCALE ambient statement gates (kb/Work PB64 T1; DESIGN-locale-facility §4.10) ─────────────────
    //
    // Ambient like EC-ORDER-NOT-SUPPORTED: a locale comparison renders INLINE in a relation condition, and SET
    // LOCALE's outcomes are runtime facts (availability, a pointer's content), so the guard wraps the STATEMENT and
    // the raise sites consult the flag. Checking-OFF is lenient and the standard names each outcome: GR21/GR24 —
    // the SET is unsuccessful (the state is unchanged); §8.8.4.2.11 — the comparison still answers a
    // deterministic order (L6). §14.6.13.1.1: nothing is recorded when checking is off.

    /// <summary>True while the currently-executing statement has EC-LOCALE-MISSING checking enabled (fatal).</summary>
    public bool LocaleMissingChecking
    {
        get => _checking.LocaleMissing;
        set => _checking.LocaleMissing = value;
    }

    /// <summary>Raise EC-LOCALE-MISSING (§14.9.39.4 GR24: "If the locale specified by locale-name-1 is not available, the
    /// EC-LOCALE-MISSING exception condition is set to exist"; §8.2.1; Table 13 Fatal) when checking is enabled;
    /// otherwise return and the caller leaves the state unchanged / answers the root order.</summary>
    public void LocaleMissingError(string detail) => FatalIfEnabled(LocaleMissingChecking, "EC-LOCALE-MISSING", detail);

    /// <summary>True while the currently-executing statement has EC-LOCALE-INVALID-PTR checking enabled (fatal).</summary>
    public bool LocaleInvalidPtrChecking
    {
        get => _checking.LocaleInvalidPtr;
        set => _checking.LocaleInvalidPtr = value;
    }

    /// <summary>Raise EC-LOCALE-INVALID-PTR (§14.9.39.4 GR21: "The content of the pointer data item referenced by
    /// identifier-10 shall reference saved locale information; otherwise, the EC-LOCALE-INVALID-PTR exception
    /// condition is set to exist and the SET statement is unsuccessful"; Table 13 Fatal) when checking is enabled;
    /// otherwise return and the caller leaves the state unchanged.</summary>
    public void LocaleInvalidPtrError(string detail)
        => FatalIfEnabled(LocaleInvalidPtrChecking, "EC-LOCALE-INVALID-PTR", detail);

    /// <summary>True while the currently-executing statement has EC-LOCALE-INCOMPATIBLE checking enabled (fatal).</summary>
    public bool LocaleIncompatibleChecking
    {
        get => _checking.LocaleIncompatible;
        set => _checking.LocaleIncompatible = value;
    }

    /// <summary>Raise EC-LOCALE-INCOMPATIBLE (§8.8.4.2.11 — the locale "does not define a collating sequence for all
    /// characters of the operands"; DETERMINATION L6: an ill-formed UTF-16 operand; Table 13 Fatal) when checking is
    /// enabled; otherwise return and the comparison answers its deterministic order.</summary>
    public void LocaleIncompatibleError(string detail)
        => FatalIfEnabled(LocaleIncompatibleChecking, "EC-LOCALE-INCOMPATIBLE", detail);

    /// <summary>True while the currently-executing statement has EC-LOCALE-INVALID checking enabled (fatal).</summary>
    public bool LocaleInvalidChecking
    {
        get => _checking.LocaleInvalid;
        set => _checking.LocaleInvalid = value;
    }

    /// <summary>Raise EC-LOCALE-INVALID (§8.2.1: "If the locale content is invalid or incomplete during an operation
    /// using a locale, the EC-LOCALE-INVALID exception condition is set to exist and the operation is unsuccessful";
    /// Table 13 Fatal) when checking is enabled; otherwise return and the caller's documented stand-in (the
    /// invariant culture's content) answers.</summary>
    public void LocaleInvalidError(string detail) => FatalIfEnabled(LocaleInvalidChecking, "EC-LOCALE-INVALID", detail);

    /// <summary>True while the currently-executing statement has EC-LOCALE-SIZE checking enabled (fatal).</summary>
    public bool LocaleSizeChecking
    {
        get => _checking.LocaleSize;
        set => _checking.LocaleSize = value;
    }

    /// <summary>Raise EC-LOCALE-SIZE (§13.18.40.5 r14 b — locale editing's move of the hypothetical data item into
    /// the SIZE-declared item truncated a character that is "neither a zero nor a space caused by a suppressed
    /// zero"; Table 13 Fatal; the ONE raise site is <c>CobolLocaleEdit.Format</c>, kb/Work PB64 T6) when checking
    /// is enabled; otherwise return — the item holds the truncated content, exactly as r14 b's own text stores it,
    /// and execution continues.</summary>
    public void LocaleSizeError(string detail) => FatalIfEnabled(LocaleSizeChecking, "EC-LOCALE-SIZE", detail);

    // ── EC-RANGE-PERFORM-VARYING ambient statement gate (an index-name varied from a non-positive FROM item) ────

    /// <summary>True while the currently-executing statement has EC-RANGE-PERFORM-VARYING checking enabled (fatal).
    /// The PERFORM VARYING index-name initialization site consults it.</summary>
    public bool PerformVaryingChecking
    {
        get => _checking.PerformVarying;
        set => _checking.PerformVarying = value;
    }

    /// <summary>Raise EC-RANGE-PERFORM-VARYING when a PERFORM VARYING (or AFTER) initializes an INDEX-NAME from a
    /// data-item FROM operand whose value is NOT POSITIVE (&lt;= 0) at the time of initialization (ISO §14.9.28.4 GR3,
    /// spec :29222; Table 13 Fatal). The <paramref name="value"/> is the DATA ITEM's value (GR3 tests the data item,
    /// not the post-conversion index). Same fatal throw/dispatch contract as <see cref="RefModError"/>.</summary>
    public void PerformVaryingIndexError(long value, string detail)
        => FatalIfEnabled(PerformVaryingChecking && value <= 0, "EC-RANGE-PERFORM-VARYING", detail);

    // ── EC-DATA-NOT-FINITE ambient statement gate (a non-finite standard-float sending operand referenced) ──────

    /// <summary>True while the currently-executing statement has EC-DATA-NOT-FINITE checking enabled (fatal, the twin
    /// of <see cref="BoundRefModChecking"/>). Every non-exempt read of a standard-float SENDING operand consults it —
    /// the always-emitted <see cref="CobolFloat.Sending(double)"/> wrap at both float read chokepoints.</summary>
    public bool FloatNotFiniteChecking
    {
        get => _checking.FloatNotFinite;
        set => _checking.FloatNotFinite = value;
    }

    /// <summary>Raise EC-DATA-NOT-FINITE when a standard-float sending operand whose content is NaN or ±Infinity is
    /// referenced (ISO §14.6.13.2 item 3, spec :24571; Table 13 Fatal), unless one of the four exemptions applies
    /// (class condition, sign condition, same-usage MOVE, VALIDATE — realized as a raw, unwrapped read at those sites,
    /// so this raise never reaches them). When checking is enabled it throws <see cref="CobolFatalException"/> (caught
    /// by the statement guard for USE F3 dispatch, else terminating the run unit per §14.6.13.1.3 #5/#7); when checking
    /// is OFF it returns and the caller's value stands (byte-identical to a pre-slice build).</summary>
    public void FloatNotFiniteError(string detail)
        => FatalIfEnabled(FloatNotFiniteChecking, "EC-DATA-NOT-FINITE", detail);

    // ── EC-DATA-OVERFLOW ambient statement gate (a MOVE algebraic value overflows a standard-float receiver) ─────

    /// <summary>True while the currently-executing statement has EC-DATA-OVERFLOW checking enabled (fatal). Only a
    /// MOVE into a single-precision standard-float receiver consults it — the <see cref="CobolFloat.StoreSingleChecked"/>
    /// store site (§14.9.25.4 GR6 d)4.a is MOVE-only).</summary>
    public bool FloatOverflowChecking
    {
        get => _checking.FloatOverflow;
        set => _checking.FloatOverflow = value;
    }

    /// <summary>Raise EC-DATA-OVERFLOW when a MOVE's finite sending algebraic value is farther from zero than the
    /// standard-float receiver's usage can represent — an exponent overflow to ±Infinity (ISO §14.9.25.4 GR6 d)4.a,
    /// spec :28634; Table 13 Fatal). GR6 d)4.a is MOVE-specific, so an arithmetic ±Inf result is never this EC (a
    /// standard-float conversion follows ISO/IEC 60559 — §14.6.8.3 rule 2), and neither is a NaN/±Inf SOURCE (that
    /// is EC-DATA-NOT-FINITE). Same fatal throw/dispatch contract as
    /// <see cref="FloatNotFiniteError"/>.</summary>
    public void FloatOverflowError(string detail) => FatalIfEnabled(FloatOverflowChecking, "EC-DATA-OVERFLOW", detail);

    // ── EC-DATA-INCOMPATIBLE ambient statement gate (an invalid sending-operand content, §14.6.13.2) ─────────────

    /// <summary>True while the currently-executing statement has EC-DATA-INCOMPATIBLE checking enabled (fatal,
    /// Table 13). Three raise sites consult it: the FIXED-POINT numeric sending read
    /// (<see cref="CobolNum.ParseImageSending"/> / <see cref="CobolNum.SendingImage"/>, §14.6.13.2 rule 2 —
    /// kb/Work PB230) and the numeric-edited de-edits (<see cref="CobolEdit.DeEdit"/> /
    /// <see cref="CobolEdit.DeEditFloat"/>, rule 4 — kb/Work PB66).</summary>
    public bool DataIncompatibleChecking
    {
        get => _checking.DataIncompatible;
        set => _checking.DataIncompatible = value;
    }

    // ── §14.7.6 last paragraph — the CORRESPONDING DEFERRAL ────────────────────────────────────────────────────
    // "For any statement with the CORRESPONDING phrase, if any of the implied statements would set the
    // EC-DATA-INCOMPATIBLE exception condition to exist, the EC-DATA-INCOMPATIBLE exception condition is set to
    // exist AFTER ALL OF THE IMPLIED STATEMENTS ARE COMPLETED." A fatal raise inside pair 1 would abandon pairs
    // 2..n, which is precisely what that sentence forbids — so inside the region the raise is LATCHED instead,
    // the implied statements all run (with the undefined results §14.6.13.2 rule 2 permits), and the emitter
    // raises once on the way out. This is the same discipline §14.7.6's SIZE ERROR paragraph already gets from
    // ArithmeticEmitter's latching __sizeErr flag; one shape for both of the clause's aggregation sentences.
    // A flat flag suffices because the implied statements of a CORRESPONDING are simple MOVE/ADD/SUBTRACT — the
    // syntax admits no nested CORRESPONDING — and nothing inside the region can raise, so no declarative can run
    // re-entrantly during it.
    private bool _dataIncompatibleDeferring;
    private string? _dataIncompatiblePending;

    /// <summary>Enter the §14.7.6 CORRESPONDING deferral region: an EC-DATA-INCOMPATIBLE that an implied
    /// statement would raise is latched rather than thrown.</summary>
    public void DataIncompatibleDeferBegin()
    {
        _dataIncompatibleDeferring = true;
        _dataIncompatiblePending = null;
    }

    /// <summary>Leave the region (always, including on an unrelated fatal), returning the latched detail — null
    /// when no implied statement would have set the condition. The caller raises it OUTSIDE its <c>finally</c>,
    /// so a different exception already in flight is never displaced by this one.</summary>
    public string? DataIncompatibleDeferEnd()
    {
        _dataIncompatibleDeferring = false;
        string? pending = _dataIncompatiblePending;
        _dataIncompatiblePending = null;
        return pending;
    }

    /// <summary>Raise EC-DATA-INCOMPATIBLE for a sending operand whose content is not valid — a fixed-point
    /// numeric item that "would evaluate to false in a numeric class condition" (ISO §14.6.13.2 rule 2), or a
    /// de-editing MOVE's numeric-edited sender holding content that is not a possible result of any editing
    /// operation in that item (rule 4). Table 13 Fatal, with the same throw/dispatch contract as
    /// <see cref="FloatOverflowError"/>; with checking off the caller's tolerant value stands. Inside a §14.7.6
    /// CORRESPONDING region the FIRST such detail is latched and the caller continues.</summary>
    public void DataIncompatibleError(string detail)
    {
        // §14.7.6's CORRESPONDING region (above): the raise is LATCHED here, never thrown, so the remaining
        // implied statements still run and the emitter raises once on the way out.
        if (DataIncompatibleChecking && _dataIncompatibleDeferring) { _dataIncompatiblePending ??= detail; return; }
        FatalIfEnabled(DataIncompatibleChecking, "EC-DATA-INCOMPATIBLE", detail);
    }

    // ── EC-EXTERNAL enablement (§14.8.4.1 — the both-elements pairing) ──────────────────────────────────────────

    /// <summary>EC-EXTERNAL-FORMAT-CONFLICT checking in the ACTIVATING statement — see
    /// <see cref="CheckingFlags.ExternalFormatConflict"/>.</summary>
    public bool ExternalFormatConflictChecking
    {
        get => _checking.ExternalFormatConflict;
        set => _checking.ExternalFormatConflict = value;
    }

    /// <summary>EC-EXTERNAL-DATA-MISMATCH checking in the ACTIVATING statement — see
    /// <see cref="CheckingFlags.ExternalDataMismatch"/>.</summary>
    public bool ExternalDataMismatchChecking
    {
        get => _checking.ExternalDataMismatch;
        set => _checking.ExternalDataMismatch = value;
    }

    /// <summary>EC-EXTERNAL-FILE-MISMATCH checking in the ACTIVATING statement — see
    /// <see cref="CheckingFlags.ExternalFileMismatch"/>.</summary>
    public bool ExternalFileMismatchChecking
    {
        get => _checking.ExternalFileMismatch;
        set => _checking.ExternalFileMismatch = value;
    }

    /// <summary>⛔ THE ACTIVATING ELEMENT'S EC-EXTERNAL HALF (§14.8.4.1), as <see cref="ExternalChecks"/> bits — read
    /// from the statement guard's checking flags by EVERY activation boundary (a CALL or function activation in
    /// <c>ProgramTable.CallProgram</c>, a method activation in <c>ExternalStore.DescribeAtMethodActivation</c>)
    /// BEFORE the activated element's own checking scope opens. The flags are a saved-and-restored SCOPE (kb/Work
    /// PB891), so the half can never leak into a later statement — which is why this replaced the pending
    /// CALL-site mask register that every failure path had to remember to zero (kb/Work PB1138).</summary>
    public int ExternalActivatingMask =>
        (ExternalFormatConflictChecking ? (int)ExternalChecks.FormatConflict : 0)
        | (ExternalDataMismatchChecking ? (int)ExternalChecks.DataMismatch : 0)
        | (ExternalFileMismatchChecking ? (int)ExternalChecks.FileMismatch : 0);

    /// <summary>The current activation's ACTIVATING-element EC-EXTERNAL mask (§14.8.4.1's other half): latched by
    /// the activation boundary from <see cref="ExternalActivatingMask"/>, saved/restored around nested
    /// activations. The activated element's <c>ExternalStore.Describe</c> gate is this mask ANDed with its own
    /// before-Environment-division mask. Zero at the main-program activation (no activating element).</summary>
    public int ActivatorExternalMask { get; set; }

    // ── Format-3 (exception-checking) PERFORM frame stack (§14.9.28.4 GR17–GR22) ──────────────────────────────

    /// <summary>The active Format-3 PERFORM interceptor frames (innermost on top). A List, not a
    /// <c>Stack&lt;T&gt;</c>, so <see cref="RunTopFrame"/> can walk it top-down by index (§14.9.28.4 GR17 — the
    /// innermost PERFORM whose imperative-statement-1 is executing). Run-unit-scoped for free (this engine is per
    /// run unit); nesting is the stack; CALL-safety is <see cref="PerformDepth"/>/<see cref="TrimPerformTo"/>.</summary>
    private readonly List<PerformFrame> _perform = new();

    /// <summary>Push the interceptor frame the emitted Format-3 PERFORM installs around imperative-statement-1
    /// (paired with <see cref="PopPerformFrame"/> in a generated <c>finally</c>, so any unwind still balances).</summary>
    public void PushPerformFrame(PerformFrame f) => _perform.Add(f);

    /// <summary>Pop the top interceptor frame at the end of a Format-3 PERFORM (before its FINALLY runs, so imp-5
    /// behaves as if in a Format-2 PERFORM — GR21).</summary>
    public void PopPerformFrame() => _perform.RemoveAt(_perform.Count - 1);

    /// <summary>The current frame-stack depth (the CALL-boundary snapshot point).</summary>
    internal int PerformDepth => _perform.Count;

    /// <summary>Restore the frame stack to <paramref name="depth"/> — the activation boundary's per-activation
    /// scope (§14.9.28.4 GR1's cross-activation "in range" reading is a documented STAGED item; the safe default
    /// is that a called program's raise is not intercepted by the caller's frame).</summary>
    internal void TrimPerformTo(int depth) { while (_perform.Count > depth) _perform.RemoveAt(_perform.Count - 1); }

    /// <summary>The activation FLOOR — <see cref="RunTopFrame"/> never walks BELOW it. Zero by default (a program's
    /// whole stack is visible — byte-identical behaviour). An OO method is a separate source element (ISO §14.9.18.3
    /// SR2/SR4a): on entry an F3 method RAISES the floor to the current depth so its own unmatched raises are NOT
    /// intercepted by the ACTIVATOR's WHEN (design SSOT §9.10.1-C2 — ECs cross a method boundary only via GOBACK/EXIT
    /// … RAISING). Nests via the saved/restored old floor. The cross-CALL/INVOKE "in range" reading stays staged.</summary>
    private int _floor;

    /// <summary>Raise the frame-stack floor to the current depth (an F3-method entry), returning the previous floor
    /// for a later <see cref="RestorePerformFloor"/>. Below this, <see cref="RunTopFrame"/> does not walk.</summary>
    public int RaisePerformFloor() { int old = _floor; _floor = _perform.Count; return old; }

    /// <summary>Restore the frame-stack floor to a value captured by <see cref="RaisePerformFloor"/> (the F3-method
    /// exit — paired in a generated <c>finally</c> so any unwind still balances).</summary>
    public void RestorePerformFloor(int floor) => _floor = floor;

    /// <summary>Select and run the innermost matching WHEN handler of an active exception-checking PERFORM
    /// (ISO §14.9.28.4 GR17 — the closest PERFORM whose imperative-statement-1 is executing; GR18 WHEN OTHER;
    /// GR21 — a frame is transparent to exception conditions raised while it is handling). Walks the stack
    /// innermost→outermost, skipping frames already <see cref="PerformFrame.Handling"/>; the first frame whose
    /// matcher does not return <see cref="PerformFrame.NoMatch"/> handled it (<paramref name="handled"/> = true),
    /// and its returned dispatch action (<c>-1</c>/<c>-2</c>/pc) is passed back to the raise site. Every frame
    /// visited in THIS resolution stays marked <c>Handling</c> until it completes (deferred clear), so an
    /// exception raised inside a selected (outer) handler is not re-caught by a skipped inner frame whose imp-1 is
    /// suspended. When no frame matches, returns <see cref="PerformFrame.NoMatch"/> and the caller falls to the
    /// USE dispatch (<c>__EcDispatch</c>) — GR17's "otherwise" tail.</summary>
    public int RunTopFrame(string ec, string? file, out bool handled)
    {
        handled = false;
        var marked = new List<PerformFrame>(4);   // per-raise (the EC path is rare); re-entrancy-safe
        try
        {
            for (int i = _perform.Count - 1; i >= _floor; i--)   // innermost → the activation floor (§9.10.1-C2)
            {
                var f = _perform[i];
                if (f.Handling) continue;                   // GR21 — its own imp-1/handler is transparent
                f.Handling = true;
                marked.Add(f);                              // deferred clear ⇒ stays Handling for the whole walk
                int a = f.Matcher(ec, file);                // runs imp-2 (+COMMON) synchronously iff it matches
                if (a != PerformFrame.NoMatch) { handled = true; return a; }
            }
            return PerformFrame.NoMatch;                    // → caller falls to __EcDispatch (USE) / -3
        }
        finally { foreach (var f in marked) f.Handling = false; }
    }
}

/// <summary>
/// ⛔ THE TWO SUBSTITUTED TEXT RESULTS OF A REJECTED FUNCTION REFERENCE, EACH WRITTEN DOWN EXACTLY ONCE
/// (kb/Work PB383, PB470). §15.3 rule 14 hands the result of a function reference whose argument rules were
/// violated to the implementor when EC-ARGUMENT-FUNCTION checking is off, and docs/CONFORMANCE.md row
/// <c>DOC-A.1-90</c> states the determination. Its NUMERIC half is
/// <see cref="ExceptionEngine.ArgumentError(string)"/>'s own <c>return 0</c>; its TEXT half is these two class
/// values, and <see cref="ExceptionEngine.ArgumentErrorZeroLength(string)"/> /
/// <see cref="ExceptionEngine.ArgumentErrorSpaces(string, int)"/> are the raise-and-substitute expressions
/// built over them.
/// <para>⚠ A GUARD NEVER SPELLS ITS OWN SUBSTITUTE. That is not style: while raise and substitute were two
/// statements, <c>BooleanOfInteger</c> answered <c>"0"</c> where <c>BaseConvert</c> answered the zero-length
/// value for the SAME determination (PB383), and the three LOCALE functions answered a zero-length value where
/// row DOC-A.1-90's own words give spaces (PB470) — both silent, both in a user's program. The two members
/// below exist so a site that has ALREADY raised (a <c>bool</c> screening predicate whose callers substitute
/// differently by return type — <c>CobolDate.SecondsOutOfStandardForm</c>, <c>CobolDate.OffsetOutOfRange</c>)
/// still reads the class rather than writing a literal. <c>ArgumentSubstituteDriftTests</c> keeps it true.</para>
/// </summary>
public static class ArgumentSubstitute
{
    /// <summary>Row DOC-A.1-90's zero-length class: the returned LENGTH is itself derived from the rejected
    /// argument, so nothing survives the rejection to size a result. Also the value §15.87.4 rule 1 states
    /// outright for SUBSTITUTE — that site shares the mechanism, not the determination.</summary>
    public static string ZeroLength => string.Empty;

    /// <summary>Row DOC-A.1-90's general alphanumeric/national clause — "the zero value of the type the
    /// function returns" — at the <paramref name="positions"/> the CALL SITE derives (see
    /// <see cref="ExceptionEngine.ArgumentErrorSpaces(string, int)"/>; one position for every site today).</summary>
    public static string Spaces(int positions) => positions == 1 ? " " : new string(' ', positions);
}

/// <summary>
/// The static facade over the run unit's <see cref="ExceptionEngine"/> (the emitted surface — generated raise
/// sites and the EXCEPTION-* function plumbing call these; kept name-stable pre-G8, DESIGN-runtime-library §2.1).
/// Every member forwards to <c>RunUnit.Current.Exceptions</c>.
/// </summary>
public static class ExceptionState
{
    /// <summary>§15.33.3 rule 1's literal EXCEPTION-STATUS value when the last exception is an exception
    /// OBJECT — the LastName sentinel (NOT a catalog exception-name; ExceptionCatalog.TryGet fails on it
    /// by design, and the EXCEPTION-* functions degrade correctly: not an IO name → "00", no location →
    /// spaces).</summary>
    public const string ObjectSentinel = "EXCEPTION-OBJECT";

    private static ExceptionEngine E => RunUnit.Current.Exceptions;

    /// <inheritdoc cref="ExceptionEngine.LastName"/>
    public static string? LastName => E.LastName;

    /// <inheritdoc cref="ExceptionEngine.LastFatal"/>
    public static bool LastFatal => E.LastFatal;

    /// <inheritdoc cref="ExceptionEngine.LastFile"/>
    public static string? LastFile => E.LastFile;

    /// <inheritdoc cref="ExceptionEngine.LastIoStatus"/>
    public static string? LastIoStatus => E.LastIoStatus;

    /// <inheritdoc cref="ExceptionEngine.LastLocation"/>
    public static string? LastLocation => E.LastLocation;

    /// <inheritdoc cref="ExceptionEngine.LastStatement"/>
    public static string? LastStatement => E.LastStatement;

    /// <inheritdoc cref="ExceptionEngine.ExceptionObject"/>
    public static CobolObject? ExceptionObject => E.ExceptionObject;

    /// <inheritdoc cref="ExceptionEngine.SetObject"/>
    public static void SetObject(CobolObject? obj) => E.SetObject(obj);

    /// <inheritdoc cref="ExceptionEngine.Set"/>
    public static void Set(string name, bool fatal, string? statement = null, string? location = null)
        => E.Set(name, fatal, statement, location);

    /// <inheritdoc cref="ExceptionEngine.EnterStatement"/>
    public static (string? Stmt, string? Loc, string[]? Names) EnterStatement(string stmt, string loc, string[] locNames)
        => E.EnterStatement(stmt, loc, locNames);

    /// <inheritdoc cref="ExceptionEngine.ExitStatement"/>
    public static void ExitStatement((string? Stmt, string? Loc, string[]? Names) prior) => E.ExitStatement(prior);

    /// <inheritdoc cref="ExceptionEngine.SetIo"/>
    public static void SetIo(string name, bool fatal, string file, string ioStatus, string? statement = null,
        string? location = null)
        => E.SetIo(name, fatal, file, ioStatus, statement, location);

    /// <inheritdoc cref="ExceptionEngine.Clear"/>
    public static void Clear() => E.Clear();

    /// <inheritdoc cref="ExceptionEngine.SetPropagating"/>
    public static void SetPropagating(string name, bool fatal, string? statement = null, string? location = null)
        => E.SetPropagating(name, fatal, statement, location);

    /// <inheritdoc cref="ExceptionEngine.SetPropagatingObject"/>
    public static void SetPropagatingObject(CobolObject? obj) => E.SetPropagatingObject(obj);

    /// <inheritdoc cref="ExceptionEngine.TakePropagatedObject"/>
    public static bool TakePropagatedObject(out CobolObject? obj) => E.TakePropagatedObject(out obj);

    /// <inheritdoc cref="ExceptionEngine.SetPropagatingLast"/>
    public static void SetPropagatingLast(string[]? pdRaising = null, string? statement = null, string? location = null)
        => E.SetPropagatingLast(pdRaising, statement, location);

    /// <inheritdoc cref="ExceptionEngine.StageAutomaticPropagation"/>
    public static bool StageAutomaticPropagation(string name) => E.StageAutomaticPropagation(name);

    /// <inheritdoc cref="ExceptionEngine.StageAutomaticObjectPropagation"/>
    public static void StageAutomaticObjectPropagation(bool applicable) => E.StageAutomaticObjectPropagation(applicable);

    /// <inheritdoc cref="ExceptionEngine.TakeRaisedPropagation"/>
    public static bool TakeRaisedPropagation(string activatorChecking, out string name, out bool fatal)
        => E.TakeRaisedPropagation(activatorChecking, out name, out fatal);


    /// <inheritdoc cref="ExceptionEngine.DataPtrNullChecking"/>
    public static bool DataPtrNullChecking
    {
        get => E.DataPtrNullChecking;
        set => E.DataPtrNullChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.DataPtrNullError"/>
    public static void DataPtrNullError(string detail) => E.DataPtrNullError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundPtrChecking"/>
    public static bool BoundPtrChecking
    {
        get => E.BoundPtrChecking;
        set => E.BoundPtrChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.BoundPtrError"/>
    public static void BoundPtrError(string detail) => E.BoundPtrError(detail);

    /// <inheritdoc cref="ExceptionEngine.SizeAddressChecking"/>
    public static bool SizeAddressChecking
    {
        get => E.SizeAddressChecking;
        set => E.SizeAddressChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SizeAddressError"/>
    public static void SizeAddressError(string detail) => E.SizeAddressError(detail);

    /// <inheritdoc cref="ExceptionEngine.RangePtrChecking"/>
    public static bool RangePtrChecking
    {
        get => E.RangePtrChecking;
        set => E.RangePtrChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.RangePtrError"/>
    public static void RangePtrError(string detail) => E.RangePtrError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundSubscriptChecking"/>
    public static bool BoundSubscriptChecking
    {
        get => E.BoundSubscriptChecking;
        set => E.BoundSubscriptChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SubscriptError"/>
    public static void SubscriptError(string detail) => E.SubscriptError(detail);

    /// <inheritdoc cref="ExceptionEngine.ProgramArgOmittedChecking"/>
    public static bool ProgramArgOmittedChecking
    {
        get => E.ProgramArgOmittedChecking;
        set => E.ProgramArgOmittedChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ProgramArgOmittedError"/>
    public static void ProgramArgOmittedError(string detail) => E.ProgramArgOmittedError(detail);

    /// <inheritdoc cref="ExceptionEngine.FunctionArgOmittedChecking"/>
    public static bool FunctionArgOmittedChecking
    {
        get => E.FunctionArgOmittedChecking;
        set => E.FunctionArgOmittedChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FunctionArgOmittedError"/>
    public static void FunctionArgOmittedError(string detail) => E.FunctionArgOmittedError(detail);

    /// <inheritdoc cref="ExceptionEngine.OoArgOmittedChecking"/>
    public static bool OoArgOmittedChecking
    {
        get => E.OoArgOmittedChecking;
        set => E.OoArgOmittedChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.OoArgOmittedError"/>
    public static void OoArgOmittedError(string detail) => E.OoArgOmittedError(detail);

    /// <inheritdoc cref="ExceptionEngine.OoResourceChecking"/>
    public static bool OoResourceChecking
    {
        get => E.OoResourceChecking;
        set => E.OoResourceChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.OoResourceError"/>
    public static void OoResourceError(string detail) => E.OoResourceError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundOdoChecking"/>
    public static bool BoundOdoChecking
    {
        get => E.BoundOdoChecking;
        set => E.BoundOdoChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.OdoError"/>
    public static void OdoError(string detail) => E.OdoError(detail);

    /// <inheritdoc cref="ExceptionEngine.RangeIndexChecking"/>
    public static bool RangeIndexChecking
    {
        get => E.RangeIndexChecking;
        set => E.RangeIndexChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.RangeIndexError"/>
    public static void RangeIndexError(string detail) => E.RangeIndexError(detail);

    /// <inheritdoc cref="ExceptionEngine.RangeInspectSizeChecking"/>
    public static bool RangeInspectSizeChecking
    {
        get => E.RangeInspectSizeChecking;
        set => E.RangeInspectSizeChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.RangeInspectSizeError"/>
    public static void RangeInspectSizeError(string detail) => E.RangeInspectSizeError(detail);

    /// <inheritdoc cref="ExceptionEngine.OoUniversalChecking"/>
    public static bool OoUniversalChecking
    {
        get => E.OoUniversalChecking;
        set => E.OoUniversalChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowSearchChecking"/>
    public static bool FlowSearchChecking
    {
        get => E.FlowSearchChecking;
        set => E.FlowSearchChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowSearchError"/>
    public static void FlowSearchError(string detail) => E.FlowSearchError(detail);

    /// <inheritdoc cref="ExceptionEngine.FlowReleaseChecking"/>
    public static bool FlowReleaseChecking
    {
        get => E.FlowReleaseChecking;
        set => E.FlowReleaseChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowReleaseError"/>
    public static void FlowReleaseError(string detail) => E.FlowReleaseError(detail);

    /// <inheritdoc cref="ExceptionEngine.FlowReturnChecking"/>
    public static bool FlowReturnChecking
    {
        get => E.FlowReturnChecking;
        set => E.FlowReturnChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowReturnError"/>
    public static void FlowReturnError(string detail) => E.FlowReturnError(detail);

    /// <inheritdoc cref="ExceptionEngine.SortMergeReturnChecking"/>
    public static bool SortMergeReturnChecking
    {
        get => E.SortMergeReturnChecking;
        set => E.SortMergeReturnChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SortMergeReturnError"/>
    public static void SortMergeReturnError(string detail) => E.SortMergeReturnError(detail);

    /// <inheritdoc cref="ExceptionEngine.SortMergeActiveChecking"/>
    public static bool SortMergeActiveChecking
    {
        get => E.SortMergeActiveChecking;
        set => E.SortMergeActiveChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SortMergeActiveError"/>
    public static void SortMergeActiveError(string detail) => E.SortMergeActiveError(detail);

    /// <inheritdoc cref="ExceptionEngine.SortMergeFileOpenChecking"/>
    public static bool SortMergeFileOpenChecking
    {
        get => E.SortMergeFileOpenChecking;
        set => E.SortMergeFileOpenChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SortMergeFileOpenError"/>
    public static void SortMergeFileOpenError(string detail) => E.SortMergeFileOpenError(detail);

    /// <inheritdoc cref="ExceptionEngine.SortMergeReleaseChecking"/>
    public static bool SortMergeReleaseChecking
    {
        get => E.SortMergeReleaseChecking;
        set => E.SortMergeReleaseChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SortMergeReleaseError"/>
    public static void SortMergeReleaseError(string detail) => E.SortMergeReleaseError(detail);

    /// <inheritdoc cref="ExceptionEngine.SortMergeSequenceChecking"/>
    public static bool SortMergeSequenceChecking
    {
        get => E.SortMergeSequenceChecking;
        set => E.SortMergeSequenceChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.SortMergeSequenceError"/>
    public static void SortMergeSequenceError(string detail) => E.SortMergeSequenceError(detail);

    /// <inheritdoc cref="ExceptionEngine.FlowReportChecking"/>
    public static bool FlowReportChecking
    {
        get => E.FlowReportChecking;
        set => E.FlowReportChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowReportError"/>
    public static void FlowReportError(string detail) => E.FlowReportError(detail);

    /// <inheritdoc cref="ExceptionEngine.FlowUseChecking"/>
    public static bool FlowUseChecking
    {
        get => E.FlowUseChecking;
        set => E.FlowUseChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowUseError"/>
    public static void FlowUseError(string detail) => E.FlowUseError(detail);

    /// <inheritdoc cref="ExceptionEngine.FlowGlobalGobackChecking"/>
    public static bool FlowGlobalGobackChecking
    {
        get => E.FlowGlobalGobackChecking;
        set => E.FlowGlobalGobackChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FlowGlobalGobackError"/>
    public static void FlowGlobalGobackError(string detail) => E.FlowGlobalGobackError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportActiveChecking"/>
    public static bool ReportActiveChecking
    {
        get => E.ReportActiveChecking;
        set => E.ReportActiveChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportActiveError"/>
    public static void ReportActiveError(string detail) => E.ReportActiveError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportInactiveChecking"/>
    public static bool ReportInactiveChecking
    {
        get => E.ReportInactiveChecking;
        set => E.ReportInactiveChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportInactiveError"/>
    public static void ReportInactiveError(string detail) => E.ReportInactiveError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportFileModeChecking"/>
    public static bool ReportFileModeChecking
    {
        get => E.ReportFileModeChecking;
        set => E.ReportFileModeChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportFileModeError"/>
    public static void ReportFileModeError(string detail) => E.ReportFileModeError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportSumSizeChecking"/>
    public static bool ReportSumSizeChecking
    {
        get => E.ReportSumSizeChecking;
        set => E.ReportSumSizeChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportSumSizeError"/>
    public static void ReportSumSizeError(string detail) => E.ReportSumSizeError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportVaryingChecking"/>
    public static bool ReportVaryingChecking
    {
        get => E.ReportVaryingChecking;
        set => E.ReportVaryingChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportVaryingError"/>
    public static void ReportVaryingError(string detail) => E.ReportVaryingError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportLineOverlapChecking"/>
    public static bool ReportLineOverlapChecking
    {
        get => E.ReportLineOverlapChecking;
        set => E.ReportLineOverlapChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportLineOverlapError"/>
    public static bool ReportLineOverlapError(string detail) => E.ReportLineOverlapError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportPageLimitChecking"/>
    public static bool ReportPageLimitChecking
    {
        get => E.ReportPageLimitChecking;
        set => E.ReportPageLimitChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportPageLimitError"/>
    public static bool ReportPageLimitError(string detail) => E.ReportPageLimitError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportColumnOverlapChecking"/>
    public static bool ReportColumnOverlapChecking
    {
        get => E.ReportColumnOverlapChecking;
        set => E.ReportColumnOverlapChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportColumnOverlapError"/>
    public static bool ReportColumnOverlapError(string detail) => E.ReportColumnOverlapError(detail);

    /// <inheritdoc cref="ExceptionEngine.ReportPageWidthChecking"/>
    public static bool ReportPageWidthChecking
    {
        get => E.ReportPageWidthChecking;
        set => E.ReportPageWidthChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ReportPageWidthError"/>
    public static void ReportPageWidthError(string detail) => E.ReportPageWidthError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundTableLimitChecking"/>
    public static bool BoundTableLimitChecking
    {
        get => E.BoundTableLimitChecking;
        set => E.BoundTableLimitChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.BoundTableLimitError"/>
    public static void BoundTableLimitError(string detail) => E.BoundTableLimitError(detail);

    /// <inheritdoc cref="ExceptionEngine.OrderNotSupportedChecking"/>
    public static bool OrderNotSupportedChecking
    {
        get => E.OrderNotSupportedChecking;
        set => E.OrderNotSupportedChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.OrderNotSupportedError"/>
    public static void OrderNotSupportedError(string detail) => E.OrderNotSupportedError(detail);

    /// <inheritdoc cref="ExceptionEngine.LocaleMissingChecking"/>
    public static bool LocaleMissingChecking
    {
        get => E.LocaleMissingChecking;
        set => E.LocaleMissingChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.LocaleMissingError"/>
    public static void LocaleMissingError(string detail) => E.LocaleMissingError(detail);

    /// <inheritdoc cref="ExceptionEngine.LocaleInvalidPtrChecking"/>
    public static bool LocaleInvalidPtrChecking
    {
        get => E.LocaleInvalidPtrChecking;
        set => E.LocaleInvalidPtrChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.LocaleInvalidPtrError"/>
    public static void LocaleInvalidPtrError(string detail) => E.LocaleInvalidPtrError(detail);

    /// <inheritdoc cref="ExceptionEngine.LocaleIncompatibleChecking"/>
    public static bool LocaleIncompatibleChecking
    {
        get => E.LocaleIncompatibleChecking;
        set => E.LocaleIncompatibleChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.LocaleIncompatibleError"/>
    public static void LocaleIncompatibleError(string detail) => E.LocaleIncompatibleError(detail);

    /// <inheritdoc cref="ExceptionEngine.LocaleInvalidChecking"/>
    public static bool LocaleInvalidChecking
    {
        get => E.LocaleInvalidChecking;
        set => E.LocaleInvalidChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.LocaleInvalidError"/>
    public static void LocaleInvalidError(string detail) => E.LocaleInvalidError(detail);

    /// <inheritdoc cref="ExceptionEngine.LocaleSizeChecking"/>
    public static bool LocaleSizeChecking
    {
        get => E.LocaleSizeChecking;
        set => E.LocaleSizeChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.LocaleSizeError"/>
    public static void LocaleSizeError(string detail) => E.LocaleSizeError(detail);

    /// <inheritdoc cref="ExceptionEngine.SaveChecking"/>
    public static CheckingFlags SaveChecking() => E.SaveChecking();

    /// <inheritdoc cref="ExceptionEngine.PushAllCheckingOff"/>
    public static CheckingFlags PushAllCheckingOff() => E.PushAllCheckingOff();

    /// <inheritdoc cref="ExceptionEngine.RestoreChecking"/>
    public static void RestoreChecking(CheckingFlags saved) => E.RestoreChecking(saved);

    /// <inheritdoc cref="ExceptionEngine.ArgumentFunctionChecking"/>
    public static bool ArgumentFunctionChecking
    {
        get => E.ArgumentFunctionChecking;
        set => E.ArgumentFunctionChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ArgumentError"/>
    public static long ArgumentError(string detail) => E.ArgumentError(detail);

    /// <inheritdoc cref="ExceptionEngine.ArgumentErrorZeroLength"/>
    public static string ArgumentErrorZeroLength(string detail) => E.ArgumentErrorZeroLength(detail);

    /// <inheritdoc cref="ExceptionEngine.ArgumentErrorSpaces"/>
    public static string ArgumentErrorSpaces(string detail, int positions) => E.ArgumentErrorSpaces(detail, positions);

    /// <inheritdoc cref="ExceptionEngine.DataConversionChecking"/>
    public static bool DataConversionChecking
    {
        get => E.DataConversionChecking;
        set => E.DataConversionChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.DataConversionError"/>
    public static void DataConversionError(string detail) => E.DataConversionError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundOverflowChecking"/>
    public static bool BoundOverflowChecking
    {
        get => E.BoundOverflowChecking;
        set => E.BoundOverflowChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.BoundOverflowError"/>
    public static void BoundOverflowError(string detail) => E.BoundOverflowError(detail);

    /// <inheritdoc cref="ExceptionEngine.BoundSetChecking"/>
    public static bool BoundSetChecking
    {
        get => E.BoundSetChecking;
        set => E.BoundSetChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.BoundSetError"/>
    public static void BoundSetError(string detail) => E.BoundSetError(detail);

    /// <inheritdoc cref="ExceptionEngine.RangeInvalidChecking"/>
    public static bool RangeInvalidChecking
    {
        get => E.RangeInvalidChecking;
        set => E.RangeInvalidChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.RangeInvalidError"/>
    public static void RangeInvalidError(string detail) => E.RangeInvalidError(detail);

    /// <inheritdoc cref="ExceptionEngine.StorageNotAvailChecking"/>
    public static bool StorageNotAvailChecking
    {
        get => E.StorageNotAvailChecking;
        set => E.StorageNotAvailChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.StorageNotAvailError"/>
    public static void StorageNotAvailError(string detail) => E.StorageNotAvailError(detail);

    /// <inheritdoc cref="ExceptionEngine.NonfatalDispatcher"/>
    public static INonfatalSelector? NonfatalDispatcher
    {
        get => E.NonfatalDispatcher;
        set => E.NonfatalDispatcher = value;
    }

    /// <inheritdoc cref="ExceptionEngine.BoundRefModChecking"/>
    public static bool BoundRefModChecking
    {
        get => E.BoundRefModChecking;
        set => E.BoundRefModChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.RefModError"/>
    public static void RefModError(string detail) => E.RefModError(detail);

    /// <inheritdoc cref="ExceptionEngine.PerformVaryingChecking"/>
    public static bool PerformVaryingChecking
    {
        get => E.PerformVaryingChecking;
        set => E.PerformVaryingChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.PerformVaryingIndexError"/>
    public static void PerformVaryingIndexError(long value, string detail) => E.PerformVaryingIndexError(value, detail);

    /// <inheritdoc cref="ExceptionEngine.FloatNotFiniteChecking"/>
    public static bool FloatNotFiniteChecking
    {
        get => E.FloatNotFiniteChecking;
        set => E.FloatNotFiniteChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FloatNotFiniteError"/>
    public static void FloatNotFiniteError(string detail) => E.FloatNotFiniteError(detail);

    /// <inheritdoc cref="ExceptionEngine.DataIncompatibleChecking"/>
    public static bool DataIncompatibleChecking
    {
        get => E.DataIncompatibleChecking;
        set => E.DataIncompatibleChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.DataIncompatibleError"/>
    public static void DataIncompatibleError(string detail) => E.DataIncompatibleError(detail);

    /// <inheritdoc cref="ExceptionEngine.DataIncompatibleDeferBegin"/>
    public static void DataIncompatibleDeferBegin() => E.DataIncompatibleDeferBegin();

    /// <inheritdoc cref="ExceptionEngine.DataIncompatibleDeferEnd"/>
    public static string? DataIncompatibleDeferEnd() => E.DataIncompatibleDeferEnd();

    /// <inheritdoc cref="ExceptionEngine.FloatOverflowChecking"/>
    public static bool FloatOverflowChecking
    {
        get => E.FloatOverflowChecking;
        set => E.FloatOverflowChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.FloatOverflowError"/>
    public static void FloatOverflowError(string detail) => E.FloatOverflowError(detail);

    /// <inheritdoc cref="ExceptionEngine.ExternalFormatConflictChecking"/>
    public static bool ExternalFormatConflictChecking
    {
        get => E.ExternalFormatConflictChecking;
        set => E.ExternalFormatConflictChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ExternalDataMismatchChecking"/>
    public static bool ExternalDataMismatchChecking
    {
        get => E.ExternalDataMismatchChecking;
        set => E.ExternalDataMismatchChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ExternalFileMismatchChecking"/>
    public static bool ExternalFileMismatchChecking
    {
        get => E.ExternalFileMismatchChecking;
        set => E.ExternalFileMismatchChecking = value;
    }

    /// <inheritdoc cref="ExceptionEngine.ExternalActivatingMask"/>
    public static int ExternalActivatingMask => E.ExternalActivatingMask;

    /// <inheritdoc cref="ExceptionEngine.ActivatorExternalMask"/>
    public static int ActivatorExternalMask
    {
        get => E.ActivatorExternalMask;
        set => E.ActivatorExternalMask = value;
    }

    /// <inheritdoc cref="ExceptionEngine.PushPerformFrame"/>
    public static void PushPerformFrame(PerformFrame f) => E.PushPerformFrame(f);

    /// <inheritdoc cref="ExceptionEngine.PopPerformFrame"/>
    public static void PopPerformFrame() => E.PopPerformFrame();

    /// <inheritdoc cref="ExceptionEngine.RunTopFrame"/>
    public static int RunTopFrame(string ec, string? file, out bool handled) => E.RunTopFrame(ec, file, out handled);

    /// <inheritdoc cref="ExceptionEngine.PerformDepth"/>
    public static int PerformDepth => E.PerformDepth;

    /// <inheritdoc cref="ExceptionEngine.TrimPerformTo"/>
    public static void TrimPerformTo(int depth) => E.TrimPerformTo(depth);

    /// <inheritdoc cref="ExceptionEngine.RaisePerformFloor"/>
    public static int RaisePerformFloor() => E.RaisePerformFloor();

    /// <inheritdoc cref="ExceptionEngine.RestorePerformFloor"/>
    public static void RestorePerformFloor(int floor) => E.RestorePerformFloor(floor);
}
