// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ THE EXCEPTION-NAMES A STATEMENT CAN RAISE ARE ONE STRUCTURE (kb/Work PB3102, PB1762;
/// COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D13). ISO §14.6.13.1.1 attaches a raise to the DETECTION: "If checking for an
/// exception condition is enabled and an exception status indicator is set as a result of an exception detected
/// during the execution of a statement, the associated exception condition is raised", and the statement's enabled set
/// has to carry every name its detections can set, or the raise is lost in silence. A detection has one of three
/// homes, and each is a TABLE here, so a test reads it without parsing C#:
/// <list type="number">
/// <item>a statement SHAPE — <see cref="NodeKindRaises"/>, <see cref="IoRaise"/> and <see cref="ActivationSiteNames"/>
/// (a READ's I-O status, a CALL's program lookup);</item>
/// <item>an OPERAND or value part, wherever it is written — <see cref="OperandRaises"/> (a program-address-identifier,
/// a function reference);</item>
/// <item>an AMBIENT site no bound node identifies — <see cref="AmbientRaises"/> (a named collating sequence at use, a
/// dynamic table's growth), riding every statement of a checking-on region: a guard that never fires is harmless.</item>
/// </list>
/// <see cref="DeclaredRaisableNames"/> is the union and the ONE enumeration <see cref="EcWrap"/> filters by
/// <c>TurnState.Enabled</c>; nothing else computes a statement's names. <c>EcRaiseSiteRegistryDriftTests</c> ties every
/// catalogued level-3 name to a declaration here and a raise site in the runtime or the emitters.
/// </summary>
internal sealed partial class EcBinder
{
    /// <summary>What a statement's ambient declarations depend on besides the statement itself: the kind of the
    /// ACTIVATED element that owns it (the *-ARG-OMITTED rule is one rule per element kind, kb/Work PB971) and whether
    /// the module declares a CHARACTER CLASSIFICATION (T5 of DESIGN-locale-facility §4.10).</summary>
    internal readonly record struct EcRaiseScope(ActivatedElementKind Element, bool LocaleClassification);

    /// <summary>A statement SHAPE that raises <see cref="Names"/> through its own node kind (no file connector).</summary>
    internal sealed record NodeKindRaise(Func<BoundStatement, bool> Matches, IReadOnlyList<string> Names);

    /// <summary>The names raised once PER FILE CONNECTOR a statement touches (the EC-I-O family): the pair
    /// (name, file) is what §7.3.25.3 SR4's <c>FILE</c> association keys on.</summary>
    internal sealed record FileRaise(Func<BoundStatement, IEnumerable<FileModel>> FilesOf, IReadOnlyList<string> Names);

    /// <summary>An exception-name that rides every statement of a checking-on region — or, when
    /// <see cref="Element"/> is set, every statement of an element of that kind; when
    /// <see cref="NeedsLocaleClassification"/> is set, every statement of a module with a CHARACTER CLASSIFICATION.</summary>
    internal sealed record AmbientRaise(string Name, ActivatedElementKind? Element = null,
        bool NeedsLocaleClassification = false);

    // ── The family arrays (PRECISE per-node declarations) ────────────────────────────────────────────────────

    /// <summary>The EC-SIZE family — ambient for EVERY statement (kb/Work PB75). §14.7.5: the size error condition "may
    /// occur as a result of … the evaluation of an arithmetic expression" — a condition, a function argument, a
    /// subscript, an INVOKE argument all render inline — and without a SIZE ERROR phrase the level-3 EC-SIZE-* "is set
    /// to exist, and processing proceeds as specified in 14.6.13.1.3". The raise sites are unconditional
    /// (<c>CobolSizeError</c>, a <c>CobolFatalException</c>), so the guard around a size-error-free statement is
    /// harmless; an ARITHMETIC statement's own §14.7.5 shape (EmitArith) handles the family and the emitter's generic
    /// guard skips it (<c>IArithmeticStatement</c>). EC-SIZE-UNDERFLOW was once missing here (kb/Work PB145): a §8.8.1.5.2
    /// r2 too-small raise was caught by ON SIZE ERROR but never recorded.</summary>
    private static readonly string[] SizeNames =
        ["EC-SIZE-TRUNCATION", "EC-SIZE-OVERFLOW", "EC-SIZE-UNDERFLOW", "EC-SIZE-ZERO-DIVIDE", "EC-SIZE-EXPONENTIATION"];

    /// <summary>The EC-I-O family a file connector can set to exist — §9.1.13.1's status-derived names plus the
    /// ones a rule names outright (EC-I-O-LINAGE, §13.18.34.4 GR6 b) 2) — in THE canonical mask order
    /// (<see cref="ExceptionCatalog.IoMaskNames"/>; the emitter's per-statement mask bits).</summary>
    private static readonly string[] IoNames = ExceptionCatalog.IoMaskNames;

    /// <summary>The conditions a format 1 SORT with an INPUT PROCEDURE raises itself (kb/Work PB1036): -ACTIVE
    /// (a nested SORT, §14.9.40.4 GR10 / GR13) and -FILE-OPEN (a GIVING file, or a USING file, open when its
    /// phase commences — §14.9.40.4 GR9).</summary>
    private static readonly string[] SortProcedureNames = ["EC-SORT-MERGE-ACTIVE", "EC-SORT-MERGE-FILE-OPEN"];

    /// <summary>…and one with a USING phrase, whose implicit release also raises -RELEASE (§14.9.40.4 GR12 b)).</summary>
    private static readonly string[] SortUsingNames = [.. SortProcedureNames, "EC-SORT-MERGE-RELEASE"];

    /// <summary>The conditions a MERGE raises itself (kb/Work PB1036): -ACTIVE (§14.9.24.4 GR8), -FILE-OPEN
    /// (GR7 / GR12), -RELEASE (GR7 b)) and -SEQUENCE (GR6).</summary>
    private static readonly string[] MergeNames = [.. SortUsingNames, "EC-SORT-MERGE-SEQUENCE"];

    /// <summary>The OO fatal conditions an INVOKE raises (§14.9.23.4 GR5 EC-OO-NULL, GR7b EC-OO-METHOD, GR7c
    /// EC-OO-UNIVERSAL) and the one a NEW form raises (§16.2.1.2 GR2 EC-OO-RESOURCE — every New form is an INVOKE that
    /// reaches the ONE runtime body <c>BASE__FACTORY.__New</c>; kb/Work PB1524 added the raise site and PB3102 the
    /// declaration the statement's guard needs). A PRECISE per-node gate, not an ambient tail one: an INVOKE is a
    /// distinguishable bound node.</summary>
    private static readonly string[] OoInvokeNames = ["EC-OO-NULL", "EC-OO-METHOD", "EC-OO-UNIVERSAL", "EC-OO-RESOURCE"];

    /// <summary>The conditions ONLY a capacity SET (Format 14) can raise, so they bind PRECISELY on that node:
    /// EC-FLOW-SEARCH (§14.9.39.4 GR31 — the SET executed during a SEARCH of the same table) and EC-BOUND-SET
    /// (GR30's second arm — the new capacity above the expected maximum capacity; kb/Work PB460). Implicit growth
    /// raises EC-BOUND-OVERFLOW instead (§8.5.1.9.6 GR1), so EC-BOUND-SET has no inline raise site.</summary>
    private static readonly string[] SetCapacityNames = ["EC-FLOW-SEARCH", "EC-BOUND-SET"];

    /// <summary>The conditions an INITIATE raises (ISO §14.9.21.4 GR2 EC-REPORT-ACTIVE, GR3 EC-REPORT-FILE-MODE
    /// — GR3 being the detection half of §14.9.27.4 GR7 — and §14.9.49.4 GR10 EC-FLOW-REPORT). PRECISE: the raise
    /// sites are <c>CobolReport.Initiate</c>'s three preconditions, reached from exactly this bound node.</summary>
    private static readonly string[] InitiateNames = ["EC-REPORT-ACTIVE", "EC-REPORT-FILE-MODE", "EC-FLOW-REPORT"];

    /// <summary>The conditions the report groups a GENERATE or TERMINATE PRODUCES can raise (kb/Work PB1130):
    /// EC-REPORT-LINE-OVERLAP (§13.18.35.4 GR3) and EC-REPORT-SUM-SIZE (§13.18.54.4 GR3 / GR4), and — kb/Work PB1188 —
    /// EC-REPORT-PAGE-LIMIT (§13.18.35.4 GR2), EC-REPORT-COLUMN-OVERLAP (§13.18.14.4 GR4) and EC-REPORT-PAGE-WIDTH
    /// (§13.18.14.4 GR5), and — kb/Work PB1305 — EC-REPORT-VARYING (§13.18.64.4 GR5). Their raise sites are inside the
    /// engine call the verb's node emits, so they bind as precisely as the preconditions do.</summary>
    private static readonly string[] ReportProductionNames =
        ["EC-REPORT-LINE-OVERLAP", "EC-REPORT-SUM-SIZE", "EC-REPORT-PAGE-LIMIT", "EC-REPORT-COLUMN-OVERLAP", "EC-REPORT-PAGE-WIDTH",
         "EC-REPORT-VARYING"];

    /// <summary>The conditions a GENERATE raises (ISO §14.9.16.4 GR7 EC-REPORT-INACTIVE, §14.9.49.4 GR10
    /// EC-FLOW-REPORT, and the <see cref="ReportProductionNames"/>). PRECISE, as <see cref="InitiateNames"/>.</summary>
    private static readonly string[] GenerateNames = ["EC-REPORT-INACTIVE", "EC-FLOW-REPORT", .. ReportProductionNames];

    /// <summary>The conditions a TERMINATE raises (ISO §14.9.46.4 GR1 EC-REPORT-INACTIVE, §14.9.49.4 GR10
    /// EC-FLOW-REPORT, and the <see cref="ReportProductionNames"/>). PRECISE, as <see cref="InitiateNames"/>.</summary>
    private static readonly string[] TerminateNames = ["EC-REPORT-INACTIVE", "EC-FLOW-REPORT", .. ReportProductionNames];

    /// <summary>The EC-PROGRAM family a CALL/CANCEL raises through <c>CobolCallException</c>.
    /// <para>⛔ NOT EC-PROGRAM-ARG-OMITTED (kb/Work PB971). §14.9.4.4 GR12 raises it at a REFERENCE in the called
    /// program, so it is an AMBIENT declaration of the callee's statements (<see cref="AmbientRaises"/>).</para>
    /// <para>EC-PROGRAM-IMP is the <c>CobolCallException</c> constructor default — the one implementor-defined name the
    /// implementation sets (docs/CONFORMANCE.md DOC-A.1-100: the program table's internal-consistency failure at an
    /// activation). The CALL guard filters on <see cref="CobolCallException.CarriedNames"/>, which lists it, so a
    /// program enabling it by name now reaches a guard that can catch it (kb/Work PB3102).</para></summary>
    private static readonly string[] ProgramNames =
    [
        "EC-PROGRAM-NOT-FOUND", "EC-PROGRAM-RECURSIVE-CALL", "EC-PROGRAM-CANCEL-ACTIVE",
        // §14.8.2.1 via §14.9.4.4 GR3d (kb/Work PB133 wave C2b) — the dynamic Format-1 count check at
        // activation; membership here is BOTH what lets the site's catch arm name it AND what makes
        // CallEmitter pass the ACTIVATING half of GR3d's enabled-in-both gate (siteArgMismatchChecking).
        "EC-PROGRAM-ARG-MISMATCH",
        // §14.9.4.4 GR3b — CALL through a program-pointer holding the predefined address NULL (ProgramTable.CallPointer).
        "EC-PROGRAM-PTR-NULL",
        "EC-PROGRAM-IMP",
    ];

    /// <summary>The condition a USER-FUNCTION activation raises through <c>CobolCallException</c> that a CALL
    /// statement does not: ISO §8.4.3.2.4 GR6b — a locate miss is EC-FUNCTION-NOT-FOUND, not EC-PROGRAM-NOT-FOUND, and
    /// GR6f sends it to "any declarative … associated with that exception condition" (kb/Work PB233). Queried only
    /// for a <c>BoundCallProgram</c> with <c>IsFunction</c>. EC-FUNCTION-PTR-NULL (GR6c) is raised only by an
    /// activation THROUGH a function-pointer (kb/Work PB847); EC-FUNCTION-ARG-OMITTED (GR8) is ambient to the
    /// function's own statements (kb/Work PB971).</summary>
    private static readonly string[] FunctionActivationNames = ["EC-FUNCTION-NOT-FOUND"];

    /// <summary>The EC-EXTERNAL family an ACTIVATION raises when the activated element's external descriptions do not
    /// conform (ISO §14.8.4): a CALL through <c>CobolCallException</c> (§14.9.4.4 GR3e), an INVOKE's method activation
    /// through <c>CobolFatalException</c> (§14.9.23.4 GR7 d); kb/Work PB1138). The checkable trio — EC-EXTERNAL-IMP has
    /// no raise site, this implementation defines no implementor-specific external checks. The site-enabled subset is
    /// ALSO §14.8.4.1's activating-element half (<c>EcEmitter.FatalAmbientGates</c>).</summary>
    internal static readonly string[] ExternalNames =
    [
        "EC-EXTERNAL-FORMAT-CONFLICT", "EC-EXTERNAL-DATA-MISMATCH", "EC-EXTERNAL-FILE-MISMATCH",
    ];

    /// <summary>What an operand activation window holds (kb/Work PB892): which activation kinds it holds is not visible
    /// at the statement, so every activation family is declared — the conservative wrap, as for the ambient families
    /// (a name no raise site of this statement can produce binds a guard that never fires).</summary>
    internal static readonly string[] ActivationSiteNames =
        [.. ProgramNames, .. ExternalNames, .. FunctionActivationNames, .. OoInvokeNames];

    // ── Source 1: the statement SHAPE ────────────────────────────────────────────────────────────────────────

    /// <summary>The files an I-O statement touches (§9.1.13.1 — every I-O verb is an I-O operation, including UNLOCK
    /// (§14.9.47) and DELETE FILE (§14.9.10 F2), the one verb once missing from this list: kb/Work PB141). The SORT and
    /// MERGE statements' USING / GIVING transfers perform as-if OPEN / READ / WRITE / CLOSE "such that any applicable USE
    /// procedures are executed" (§14.9.40.4 GR12 / GR15, §14.9.24.4 GR7 / GR12), each rendering
    /// <c>SequentialIoEmitter.EmitUseHook</c>, whose EC-I-O raise reads THIS statement's per-file mask — with no entry
    /// here the mask was always 0 and `&gt;&gt;TURN EC-I-O CHECKING ON` over a SORT USING a missing file ran no declarative
    /// (kb/Work PB1036). <c>EcRaiseSiteRegistryDriftTests</c> reads every bound statement carrying a FileModel and asks
    /// that this function names it.</summary>
    internal static IEnumerable<FileModel> IoFilesOf(BoundStatement s) => s switch
    {
        BoundOpen o => o.Files.Select(f => f.File),
        BoundClose c => c.Files.Select(f => f.File),
        BoundUnlock ul => [ul.File],
        BoundRead rd => [rd.File],
        BoundWrite wr => [wr.File],
        BoundRewrite rw => [rw.File],
        BoundKeyedRead k => [k.File],
        BoundKeyedWrite k => [k.File],
        BoundKeyedRewrite k => [k.File],
        BoundKeyedDelete k => [k.File],
        BoundKeyedDeleteFile k => [k.File],
        BoundKeyedStart k => [k.File],
        BoundSort so => so.Using.Concat(so.Giving),
        BoundMerge mg => mg.Using.Concat(mg.Giving),
        _ => [],
    };

    /// <summary>The EC-I-O family, once per file <see cref="IoFilesOf"/> names.</summary>
    internal static readonly FileRaise IoRaise = new(IoFilesOf, IoNames);

    /// <summary>The statement-shape declarations — one row per node kind that raises through its own shape. A row is the
    /// PRECISE form (the raise site is a precondition of the engine call the node emits, so the guard binds exactly
    /// there); a name with no node kind to key on is AMBIENT instead (<see cref="AmbientRaises"/>).</summary>
    internal static readonly NodeKindRaise[] NodeKindRaises =
    [
        // §14.9.6.4 GR5 — closing a report file while an associated report is active completes the CLOSE and sets
        // EC-REPORT-NOT-TERMINATED (nonfatal; kb/Work PB141). The raise site is the emitted CLOSE, after the close
        // completed; the row is what gives it the statement's enabled set and its ambient (statement, location) pair
        // (§15.32.3 / §15.30.3 — `WITH LOCATION` answered spaces while the SequentialIoBinder decided the check itself).
        new(s => s is BoundClose, ["EC-REPORT-NOT-TERMINATED"]),
        new(s => s is BoundStringStmt, ["EC-OVERFLOW-STRING"]),
        new(s => s is BoundUnstringStmt, ["EC-OVERFLOW-UNSTRING"]),
        // §8.4.3.5.4 GR2–GR6 — PRECISE: an object-view's conformance check is raised only by its own pre-op
        // (kb/Work PB1425), which the statement carries in its hoisted sequence (walked by DeclaredRaisableNames).
        new(s => s is BoundObjectView { View.IsUniversal: false }, ["EC-OO-CONFORMANCE"]),
        // §14.9.23.4 GR5 / GR7b, and GR7 d) — a method activation is an EC-EXTERNAL raise point (§14.8.4; kb/Work PB1138)
        new(s => s is BoundInvoke or BoundInvokeUniversal, [.. OoInvokeNames, .. ExternalNames]),
        // CA37 is PRECISE: EC-FLOW-SEARCH can only arise from a capacity SET (§14.9.39.4 GR31), which is one bound
        // node. Its twin EC-BOUND-TABLE-LIMIT is NOT precise — growth also happens on IMPLICIT receiving-reference
        // growth, which renders inline — so it is ambient. The two are deliberately not merged.
        new(s => s is BoundSetCapacity, SetCapacityNames),
        // §14.9.18.4 GR6 — PRECISE: the only statement that can set EC-FLOW-GLOBAL-GOBACK is a GOBACK, and the
        // emitter's run-time `__useActive` test is the raise site (kb/Work PB409).
        new(s => s is BoundGoback, ["EC-FLOW-GLOBAL-GOBACK"]),
        // The three Report Writer verbs (kb/Work PB326). PRECISE: each condition's ONLY raise site is a precondition
        // of the engine call the node emits.
        new(s => s is BoundInitiate, InitiateNames),
        new(s => s is BoundGenerate, GenerateNames),
        new(s => s is BoundTerminate, TerminateNames),
        // The sort-merge flow conditions (kb/Work PB349, PB1036). PRECISE: the ONLY raise sites are CobolSort's
        // RELEASE / RETURN statement entries, reached from exactly these bound nodes — the implicit USING release and
        // GIVING return use the unchecked primitives, because GR1 of each verb governs the STATEMENT the program
        // writes. RELEASE's record size is §13.18.43.4 GR14 b) / GR19 b); the -ACTIVE rule for a RELEASE in an output
        // procedure / a RETURN in an input procedure is §14.9.40.4 GR10 / GR13, §14.9.24.4 GR8.
        new(s => s is BoundRelease, ["EC-FLOW-RELEASE", "EC-SORT-MERGE-ACTIVE", "EC-SORT-MERGE-RELEASE"]),
        new(s => s is BoundReturn, ["EC-FLOW-RETURN", "EC-SORT-MERGE-RETURN", "EC-SORT-MERGE-ACTIVE"]),
        // The SORT / MERGE statements themselves (kb/Work PB1036). PRECISE: every raise is CobolSort's, at the point
        // the verb's rule names. A name the statement's shape cannot raise is not asked (a SORT with no USING releases
        // nothing implicitly).
        new(s => s is BoundSort { Using.Count: > 0 }, SortUsingNames),
        new(s => s is BoundSort { Using.Count: 0 }, SortProcedureNames),
        new(s => s is BoundMerge, MergeNames),
        // The SEARCH range conditions (§14.9.37.4 GR4/GR6/GR9) RAISE via BoundSearch's own bound Check* flags — the row
        // exists so the statement carries the wrapper's AMBIENT context (kb/Work R14).
        new(s => s is BoundSearch, ["EC-RANGE-SEARCH-INDEX", "EC-RANGE-SEARCH-NO-MATCH"]),
        // CONTINUE AFTER (§14.9.9.4 GR1; kb/Work PB138): the raise is CobolTiming's, through
        // BoundContinueAfter.CheckLessThanZero, and 2-argument; the row supplies the ambient pair like SEARCH.
        new(s => s is BoundContinueAfter, ["EC-CONTINUE-LESS-THAN-ZERO"]),
        // CALL: the EC-PROGRAM family, the EC-EXTERNAL trio (§14.9.4.4 GR3e — the CALL is the EC-EXTERNAL raise point,
        // §14.8.4) and the activation's resources (§14.9.4.4 GR3c / §8.4.3.2.4 GR6c; kb/Work PB2659 — PRECISE: an
        // ACTIVATION raises it, so it is not in ProgramNames, which a CANCEL queries too).
        new(s => s is BoundCallProgram, [.. ProgramNames, .. ExternalNames, "EC-PROGRAM-RESOURCES"]),
        // A USER-FUNCTION activation is a DIFFERENT raise: a locate miss is EC-FUNCTION-NOT-FOUND (§8.4.3.2.4 GR6b).
        new(s => s is BoundCallProgram { IsFunction: true }, FunctionActivationNames),
        // PRECISE: only an activation THROUGH a function-pointer can find the pointer NULL (§8.4.3.2.4 GR6c; PB847).
        new(s => s is BoundCallProgram { IsFunction: true, IsPointerTarget: true }, ["EC-FUNCTION-PTR-NULL"]),
        new(s => s is BoundCancel, ProgramNames),   // CANCEL raises no EC-EXTERNAL — external state persists (§14.9.5 GR8)
        // ⛔ THE DEAD RAISE THIS ENUMERATION WAS MISSING (kb/Work PB452): EC-PROGRAM-NOT-FOUND was named ONLY in
        // ProgramNames, so PtrEmitter.EmitSetEntry's checking-gated §8.4.3.13 GR4 block could never be emitted.
        new(s => s is BoundSetEntry, ["EC-PROGRAM-NOT-FOUND"]),
        // The Format-8 twin (§8.4.3.12.4 GR4's locate miss and §14.9.39.4 GR14's signature screen).
        new(s => s is BoundSetFunctionAddress, ["EC-FUNCTION-NOT-FOUND", "EC-FUNCTION-PTR-INVALID"]),
        // §14.9.39.4 GR21 — SET LOCALE through a pointer holding no saved locale: only SET LOCALE can raise it.
        new(s => s is BoundSetLocale, ["EC-LOCALE-INVALID-PTR"]),
        new(s => s is BoundFree, ["EC-STORAGE-NOT-ALLOC"]),   // §14.9.15 GR1c (nonfatal; Phase-4b inc 2)
        // EC-RANGE-PERFORM-VARYING (fatal, §14.9.28.4 GR3): a PERFORM VARYING that initializes an index-name from a
        // non-positive FROM item raises it. A PERFORM is ONE identifiable node, so a PRECISE row drives the
        // FatalAmbientGates wrapper — the emitted index-init check (ControlFlowEmitter) throws inside that try.
        new(s => s is BoundInlinePerform { Control: PerformVarying { CheckIndexRange: true } }
                  or BoundOutOfLinePerform { Control: PerformVarying { CheckIndexRange: true } },
            ["EC-RANGE-PERFORM-VARYING"]),
        // EC-DATA-OVERFLOW (fatal, §14.9.25.4 GR6 d)4.a) is MOVE-only: a MOVE whose finite algebraic value overflows a
        // single-precision float receiver to ±Inf; MOVE CORRESPONDING expands to BoundMove steps the sequence walk sees.
        new(s => s is BoundMove, ["EC-DATA-OVERFLOW"]),
        // EC-RANGE-INSPECT-SIZE (fatal, §14.9.22.4 GR14/GR15/GR22 — kb/Work PB1126) is INSPECT's own: the three rules
        // that set it are written over INSPECT's operands and nothing else, so the node kind IS the rule's scope.
        new(s => s is BoundInspect, ["EC-RANGE-INSPECT-SIZE"]),
    ];

    // ── Source 2: the OPERAND or value part ──────────────────────────────────────────────────────────────────

    /// <summary>One operand or value-part kind that raises an exception condition wherever it is written, and the name it
    /// raises (kb/Work PB1453, PB1762). ISO §14.6.13.1.1 makes a raise the consequence of a DETECTION during "the
    /// execution of a statement", so the enabled set a statement's guard carries is derived from what the statement
    /// CARRIES, not from its node kind — a node-kind switch is a hand-maintained list, and each statement kind added
    /// without its arm silently lost the raise (PB452, PB409, PB326, PB349/PB1036, PB233 before this). The predicate reads
    /// one generated value part (<see cref="BoundStatementTree.OwnValueParts"/>).</summary>
    internal static readonly (Func<object, bool> Carries, string Name)[] OperandRaises =
    [
        // §8.4.3.13.4 GR4 — "If the runtime system cannot locate the program, the EC-PROGRAM-NOT-FOUND exception
        // condition is set to exist": the program-address-identifier is the operand, in a relation (§8.8.4.2.2
        // Format 3) and an INVOKE argument (§14.9.23.3 SR9) alike.
        (p => OperandsIn(p).Any(o => o is BoundAddressOperand { Program: not null }), "EC-PROGRAM-NOT-FOUND"),
        // ⛔ THE FUNCTION REFERENCE (kb/Work PB26, PB3102). ISO §15.3 item 14 attaches EC-ARGUMENT-FUNCTION to the
        // REFERENCE — "If the evaluation of an argument results in an incorrect value for that argument or for the
        // returned value according to the rules specified in the function definition … the EC-ARGUMENT-FUNCTION exception
        // condition is set to exist" — with no statement-kind qualification, and the families that ride the same inline
        // intrinsic call are the same shape: EC-ORDER-NOT-SUPPORTED (fatal, §15.85.4 r2 — FUNCTION STANDARD-COMPARE),
        // EC-DATA-CONVERSION (nonfatal, §15.19.4 r1/r3 — FUNCTION CONVERT's substitution character) and EC-LOCALE-INVALID
        // (§8.2.1 — the LOCALE intrinsics over incomplete locale content). The intrinsic renders inline inside an
        // arbitrary expression, so the statement's guard wraps the STATEMENT and the raise site consults the flag; a guard
        // around a statement whose argument is valid is a no-op.
        (PartHasIntrinsic, "EC-ARGUMENT-FUNCTION"),
        (PartHasIntrinsic, "EC-ORDER-NOT-SUPPORTED"),
        (PartHasIntrinsic, "EC-DATA-CONVERSION"),
        (PartHasIntrinsic, "EC-LOCALE-INVALID"),
    ];

    /// <summary>⛔ THE OPERAND-DECLARED RAISE NAMES OF A STATEMENT (kb/Work PB1453, PB3102): the exception-names that a value
    /// part in THIS statement's own value parts (<see cref="BoundStatementTree.OwnValueParts"/>, generated from the semantic
    /// model — a statement kind or operand slot added tomorrow is covered without an edit) can raise, per
    /// <see cref="OperandRaises"/>. Nested statements are NOT walked: each is bound and wrapped through its own
    /// <see cref="EcWrap"/> and is an EC region boundary of its own (kb/Work PB441), so a part is asked where it is
    /// written, under that statement's own TURN state — the former nested-statement recursion of the intrinsic test
    /// wrapped twice, and let an enclosing statement's line switch on a flag its nested statement's line had off.</summary>
    internal static IEnumerable<string> OperandRaisableNames(BoundStatement s)
    {
        var parts = s.OwnValueParts().ToList();
        if (parts.Count == 0) return [];
        return OperandRaises.Where(r => parts.Any(r.Carries)).Select(r => r.Name);
    }

    /// <summary>The operands one generated value part holds: itself when it is one, or those a condition compares,
    /// classifies or tests (the condition hierarchy is walked to its leaves). Expressions hold no address operand
    /// (§8.5.2.1 Table 2 — a pointer is not an arithmetic operand), so they contribute none.</summary>
    private static IEnumerable<BoundOperand> OperandsIn(object part) => part switch
    {
        BoundOperand o => [o],
        BoundCondition c => ConditionOperands(c),
        _ => [],
    };

    private static IEnumerable<BoundOperand> ConditionOperands(BoundCondition c) => c switch
    {
        BoundRelational r => [r.Left, r.Right],
        BoundLogical l => l.Operands.SelectMany(ConditionOperands),
        BoundNot n => ConditionOperands(n.Operand),
        BoundClassCondition cc => [cc.Operand],
        BoundUserClassCondition uc => [uc.Operand],
        BoundCodedSetClassCondition cs => [cs.Operand],
        BoundUdfEvaluated u => ConditionOperands(u.Inner),
        _ => [],
    };

    // ── Source 3: the AMBIENT sites ──────────────────────────────────────────────────────────────────────────

    /// <summary>The names whose raise sites render INLINE into any verb, so no node kind and no operand identifies the
    /// statement: each rides every statement of a checking-on region (a guard that never fires is harmless — the
    /// raise fires only at its site). A precise filter is the documented refinement of every row.</summary>
    internal static readonly AmbientRaise[] AmbientRaises =
    [
        // The EC-LOCALE family (kb/Work PB64 T1; DESIGN-locale-facility §4.10). EC-LOCALE-MISSING is a NAMED IS LOCALE
        // collating sequence at use (§8.2.1 — inline in a relation, a SORT key, a MAX/MIN …) and so rides any statement;
        // EC-LOCALE-INCOMPATIBLE (§8.8.4.2.11, L6) is an inline comparison outcome — any statement.
        new("EC-LOCALE-MISSING"),
        new("EC-LOCALE-INCOMPATIBLE"),
        // EC-LOCALE-INVALID also rides ANY statement of a module WITH a CHARACTER CLASSIFICATION (T5): a class test
        // (§12.3.6.4 GR7b) is not an intrinsic-bearing statement and can raise it at use (LocaleFacts.Require), as can a
        // case function without a phrase (GR7a). The intrinsic-bearing arm is an OperandRaises row.
        new("EC-LOCALE-INVALID", NeedsLocaleClassification: true),
        // EC-LOCALE-SIZE (§13.18.40.5 r14 b; PB64 T6 — the ONE raise site is CobolLocaleEdit.Format's move of the
        // hypothetical item into the SIZE-declared item): any statement that stores into a format-2 item can raise it.
        new("EC-LOCALE-SIZE"),
        // EC-BOUND-OVERFLOW (nonfatal, §8.5.1.9.6 GR1): a dynamic-capacity table's implicit growth past its expected
        // capacity (CobolDynTable.RefReceiving) records the last exception status while checking is enabled.
        new("EC-BOUND-OVERFLOW"),
        // EC-FLOW-USE (fatal, §14.9.49.4 GR2 — kb/Work PB368): GR2's subject is "a statement [that] raises an exception
        // condition that would cause the execution of a USE procedure that had previously been activated", and the set
        // of statements that can cause a USE procedure to execute is every statement that can raise ANY condition with a
        // declarative. There is no node kind to key on, and the SYNTACTIC alternative (a statement lexically inside
        // DECLARATIVES) is wrong outright, because a declarative may PERFORM a paragraph anywhere. The raise fires only
        // inside the generated __RunUse guard.
        new("EC-FLOW-USE"),
        // EC-BOUND-REF-MOD (fatal, §8.4.3.3.4): a reference modification whose leftmost/length is out of range (or an
        // unallowed zero-length) raises it at an actual out-of-range evaluation.
        new("EC-BOUND-REF-MOD"),
        // EC-DATA-PTR-NULL / EC-BOUND-PTR (fatal, §13.18.5.4 GR3/GR4), EC-SIZE-ADDRESS (§14.9.39 Format 10 GR19) and
        // EC-RANGE-PTR (§14.9.39.4 GR20): a BASED dereference renders INLINE through the generated bridge property that
        // aliases CobolPtr.Deref; GR19 and GR20 are TWO rules over one statement (the amount's value and the result's
        // range) and need two gates, or turning checking on for one silently arms the other's raise (kb/Work PB465).
        new("EC-DATA-PTR-NULL"),
        new("EC-BOUND-PTR"),
        new("EC-SIZE-ADDRESS"),
        new("EC-RANGE-PTR"),
        // EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2) and EC-BOUND-ODO (§13.18.38.4 GR7): a subscripted reference renders inline
        // through CobolTable.At and an ODO group extent through CobolTable.OdoExtent.
        new("EC-BOUND-SUBSCRIPT"),
        new("EC-BOUND-ODO"),
        // EC-RANGE-INDEX (fatal, §13.18.38.4 GR2): "An index may be modified only by a PERFORM VARYING statement, a SEARCH
        // statement, and a SET statement" — SetEmitter's store/augment pair alone is ridden by three verbs, and a
        // per-node-kind list of every emitter path that stores into an index is how kb/Work PB230 happened. kb/Work PB459.
        new("EC-RANGE-INDEX"),
        // EC-BOUND-TABLE-LIMIT (§14.9.39.4 GR30) is ambient, unlike its CA37 twin: a dynamic table grows both from an
        // explicit capacity SET and from an IMPLICIT receiving reference (CobolDynTable.RefReceiving).
        new("EC-BOUND-TABLE-LIMIT"),
        // EC-STORAGE-NOT-AVAIL (§14.9.2.4 ALLOCATE GR / CobolDynString's growth sites) and EC-RANGE-INVALID (§14.9.13.4 —
        // a THRU range with the ends reversed; CobolString.ThruMember) raise from RUNTIME sites that render inline,
        // riding the tail so the 2-argument Set picks up the ambient (statement, location) pair (kb/Work R14).
        new("EC-STORAGE-NOT-AVAIL"),
        new("EC-RANGE-INVALID"),
        // EC-DATA-NOT-FINITE (fatal, §14.6.13.2 item 3): any non-exempt read of a NaN/±Inf standard-float sending
        // operand raises it (the always-emitted CobolFloat.Sending wrap at the two float read chokepoints).
        new("EC-DATA-NOT-FINITE"),
        // EC-DATA-INCOMPATIBLE (fatal, §14.6.13.2) — ⛔ NOT a node-kind row (kb/Work PB230). Rule 2 is not MOVE-specific:
        // "When the content of a numeric sending item that is not described with a standard floating-point usage is
        // REFERENCED DURING THE EXECUTION OF A STATEMENT and the content of that sending operand would evaluate to false in
        // a numeric class condition … an EC-DATA-INCOMPATIBLE exception condition is set to exist". The raise fires only
        // at an actual windowed sending read (CobolNum.ParseImageSending / SendingImage; CobolEdit.DeEdit for rule 4).
        new("EC-DATA-INCOMPATIBLE"),
        // THE *-ARG-OMITTED CONDITION OF THE ELEMENT'S KIND (kb/Work PB971 — §14.9.4.4 GR12 program, §8.4.3.2.4 GR8
        // function, §14.9.23.4 GR10 method): the raise site is the REFERENCE, rendered inline through the formal's guarded
        // root (OmittedFormalGuard) in any operand of any verb of the ACTIVATED element, so one row per element kind, derived
        // from the runtime's ONE mapping (OmittedFormal.ConditionName).
        .. Enum.GetValues<ActivatedElementKind>().Select(k => new AmbientRaise(OmittedFormal.ConditionName(k), Element: k)),
    ];

    // ── The one enumeration ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE ENUMERATION (kb/Work PB3102): every (exception-name, file) pair <paramref name="node"/> can raise —
    /// the union of the statement-shape rows, the operand rows and the ambient rows — before the TurnState filter
    /// <see cref="EcWrap"/> applies. A desugar wrapper is TRANSPARENT to the families: the carrying statement keeps ITS
    /// families, and each hoisted activation, receiver accessor or implicit statement of a series contributes its own
    /// (the M2-UDF-1 review finding: a checked COMPUTE that also carries a function reference lost its EC-SIZE wrap). An
    /// implicit-statement series (ISO §14.9.20.4 GR3 and its six siblings) is ONE written statement, so the query is the
    /// UNION over its members (§7.3.25.4 GR6 keys on the source LINE).</summary>
    internal static IEnumerable<(string Ec, FileModel? File)> DeclaredRaisableNames(BoundStatement node, EcRaiseScope scope)
    {
        switch (node)
        {
            case BoundSequence seq:
                return seq.Steps.SelectMany(st => DeclaredRaisableNames(st, scope));
            // The per-receiver accessors of a statement that stores its receivers one at a time (kb/Work PB2078) are
            // activations exactly like a sequence's hoisted steps: the statement keeps its families and they add theirs.
            case BoundReceiverBrackets receivers:
                return DeclaredRaisableNames(receivers.Inner, scope)
                    .Concat(receivers.AllSteps().SelectMany(st => DeclaredRaisableNames(st, scope)));
            case BoundImplicitSeries series:
                return series.Members.SelectMany(m => DeclaredRaisableNames(m, scope));
            // A statement carrying OPERAND activations (kb/Work PB892) keeps its own families and its activations
            // contribute theirs — including the per-evaluation ones inside a condition or an operand, which no
            // statement-shaped step exposes to the rows.
            case BoundActivationSite site:
                return ActivationSiteNames.Select(n => (n, (FileModel?)null))
                    .Concat(DeclaredRaisableNames(site.Inner, scope));
            default:
                return LeafRaisableNames(node, scope);
        }
    }

    private static IEnumerable<(string Ec, FileModel? File)> LeafRaisableNames(BoundStatement node, EcRaiseScope scope)
    {
        foreach (var row in NodeKindRaises)
            if (row.Matches(node))
                foreach (string n in row.Names)
                    yield return (n, null);
        foreach (var file in IoRaise.FilesOf(node))
            foreach (string n in IoRaise.Names)
                yield return (n, file);
        foreach (string n in OperandRaisableNames(node))
            yield return (n, null);
        foreach (string n in SizeNames)
            yield return (n, null);
        foreach (var a in AmbientRaises)
            if ((a.Element is null || a.Element == scope.Element) && (!a.NeedsLocaleClassification || scope.LocaleClassification))
                yield return (a.Name, null);
    }

    /// <summary>Every exception-name any of the tables declares — the set <c>EcRaiseSiteRegistryDriftTests</c> compares to
    /// the catalog and to the raise sites. Derived from the tables, never maintained.</summary>
    internal static IReadOnlySet<string> AllDeclaredNames { get; } = new HashSet<string>(
        NodeKindRaises.SelectMany(r => r.Names)
            .Concat(IoRaise.Names)
            .Concat(ActivationSiteNames)
            .Concat(OperandRaises.Select(r => r.Name))
            .Concat(SizeNames)
            .Concat(AmbientRaises.Select(a => a.Name)),
        StringComparer.Ordinal);

    // ── The intrinsic walk (an OperandRaises predicate) ──────────────────────────────────────────────────────

    /// <summary>One generated value part → does it carry an intrinsic call? The four value hierarchies each have
    /// their own walker below; a part of any other shape (a <c>Place</c>, a receiver) carries no expression and
    /// answers false. ⛔ ISO §15.3 item 14 attaches the condition to the FUNCTION REFERENCE with no statement-kind
    /// qualification, so the list of parts is the GENERATED <see cref="BoundStatementTree.OwnValueParts"/> — a hand-written
    /// switch over statement kinds with a `_ => false` default was a silent wrong answer (FUNCTION LOG10(0) raised in
    /// COMPUTE, MOVE, DISPLAY and IF and was silent in STRING; kb/Work PB26). <c>EcArgumentFunctionGateDriftTests</c> pins
    /// the walk's shape.</summary>
    private static bool PartHasIntrinsic(object part) => part switch
    {
        BoundExpr e => ExprHasIntrinsic(e),
        BoundCondition c => CondHasIntrinsic(c),
        BoundOperand o => OpHasIntrinsic(o),
        BoundBoolExpr b => BoolExprHasIntrinsic(b),
        _ => false,
    };

    private static bool OpHasIntrinsic(BoundOperand op) => op switch
    {
        BoundComputedOperand c => ExprHasIntrinsic(c.Expr),
        BoundBoolOperand b => BoolExprHasIntrinsic(b.Expr),
        _ => false,
    };

    /// <summary>The intrinsic walk over the boolean channel (ISO §8.8.2). Boolean-op operands are boolean items/
    /// literals today — intrinsic operands inside a boolean expression are named residue — but the walk is TOTAL
    /// from day one (the DEVLOG-607 rule: a new node must register in every exhaustive walk).</summary>
    private static bool BoolExprHasIntrinsic(BoundBoolExpr e) => e switch
    {
        BoundBoolBinary b => BoolExprHasIntrinsic(b.Left) || BoolExprHasIntrinsic(b.Right),
        BoundBoolNot n => BoolExprHasIntrinsic(n.Operand),
        BoundBoolShift s => BoolExprHasIntrinsic(s.Operand) || ExprHasIntrinsic(s.Count),   // the count is a numeric expr
        BoundBoolCall => true,   // a boolean-result function reference (kb/Work PB68)
        _ => false,
    };

    private static bool ExprHasIntrinsic(BoundExpr e) => e switch
    {
        BoundIntrinsicCall => true,
        BoundBinary b => ExprHasIntrinsic(b.Left) || ExprHasIntrinsic(b.Right),
        BoundNegate n => ExprHasIntrinsic(n.Operand),
        BoundPower p => ExprHasIntrinsic(p.Base) || ExprHasIntrinsic(p.Exp),
        _ => false,
    };

    private static bool CondHasIntrinsic(BoundCondition c) => c switch
    {
        BoundRelational r => OpHasIntrinsic(r.Left) || OpHasIntrinsic(r.Right),
        BoundLogical l => l.Operands.Any(CondHasIntrinsic),
        BoundNot n => CondHasIntrinsic(n.Operand),
        BoundSignCondition s => ExprHasIntrinsic(s.Expr),
        BoundBooleanCondition bc => BoolExprHasIntrinsic(bc.Expr),
        _ => false,
    };
}
