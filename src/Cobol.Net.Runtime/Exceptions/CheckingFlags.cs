// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The AMBIENT exception-checking state as ONE copyable value — every <c>…Checking</c> flag the generated
/// statement guards set inside a saved-and-restored scope (kb/Work PB891; ISO §14.6.13.1.1: "if checking for an exception that occurs is not enabled,
/// no exception condition is raised").
///
/// <para><b>Why a struct and not loose fields.</b> §14.9.28.4 GR14 requires an exception-checking PERFORM to
/// take "an implicit PUSH ALL followed by TURN OFF ALL … at the end of imperative-statement-1" and an
/// "implicit POP ALL" immediately before END-PERFORM — a WHOLESALE save/clear/restore of every flag at once.
/// Expressed over individual properties that is a hand-maintained list, and the failure mode is silent: an
/// exception condition added later that forgets to join the list simply stops obeying GR14, with no test
/// failing and no diagnostic. Holding the flags in one value makes PUSH ALL a struct copy and TURN OFF ALL a
/// <c>default</c>, so a new condition is covered by construction the moment its field exists here.</para>
///
/// <para><b>Adding a condition</b> is five declarations and no prose: a field HERE, a delegating property on
/// <see cref="ExceptionEngine"/>, its static shim forwarder, the (exception-name → this flag) row in
/// <c>EcEmitter</c>'s gate table, and a ONE-LINE raise helper
/// (<c>… => FatalIfEnabled(&lt;flag&gt;, "&lt;EC&gt;", detail)</c> — kb/Work PB676). Nothing else has to know.
/// <c>ExceptionCheckingFlagsDriftTests</c> fails if an engine flag is ever declared outside this struct, so GR14's
/// PUSH ALL cannot be escaped; <c>ExceptionRaiseHelperDriftTests</c> fails if a helper reads a flag other than the
/// one the emitter pairs to the name it raises, or records a fatality Table 13 does not carry. Both invariants are
/// enforced rather than remembered.</para>
/// </summary>
public struct CheckingFlags
{
    /// <summary>EC-ARGUMENT-FUNCTION — an intrinsic-function argument/domain error (§15.3).</summary>
    public bool ArgumentFunction;

    /// <summary>EC-DATA-CONVERSION — a conversion that loses information (§14.6.13.2).</summary>
    public bool DataConversion;

    /// <summary>EC-BOUND-OVERFLOW — a dynamic-capacity table receiving item grown past its expected bound.</summary>
    public bool BoundOverflow;

    /// <summary>EC-BOUND-SET — an explicit SET of a dynamic-capacity table's capacity whose new capacity exceeds the
    /// table's expected maximum capacity (§14.9.39.4 GR30, second arm; kb/Work PB460).</summary>
    public bool BoundSet;

    /// <summary>EC-RANGE-INVALID — an alphanumeric/national THROUGH range whose starting value collates after its
    /// ending value (§14.7.8 rule 2), in a level-88 VALUE THRU or an EVALUATE WHEN range.</summary>
    public bool RangeInvalid;

    /// <summary>EC-STORAGE-NOT-AVAIL — a <c>SET [SIZE OF] dynamic-length-item TO arithmetic-expression-5</c> whose
    /// value is not nonnegative or exceeds the item's maximum size (§14.9.39 Format 16 GR37/GR38).</summary>
    public bool StorageNotAvail;

    /// <summary>EC-BOUND-REF-MOD — an out-of-range reference modification (§8.4.3.3.4).</summary>
    public bool BoundRefMod;

    /// <summary>EC-RANGE-PERFORM-VARYING — a PERFORM VARYING/AFTER that initializes an index-name from a data
    /// item whose value is not positive (§14.9.28.4 GR3). The field name is the emitter's flag stem, not the
    /// exception-name; the two are paired in <c>EcEmitter</c>'s gate table and the pairing is asserted by
    /// <c>ExceptionRaiseHelperDriftTests</c>.</summary>
    public bool PerformVarying;

    /// <summary>EC-DATA-NOT-FINITE — a NaN/±Inf standard-float sending operand (§14.6.13.2 item 3).</summary>
    public bool FloatNotFinite;

    /// <summary>EC-DATA-OVERFLOW — a MOVE whose finite sending algebraic value is farther from zero than a
    /// standard-float receiver's usage can represent — "the algebraic value of the sending operand is farther from
    /// zero than is permitted by the usage specifications of the receiving data item" (§14.9.25.4 GR6 d)4.a, which
    /// is MOVE-specific). NOT EC-SIZE-OVERFLOW, which this doc named until kb/Work PB676.</summary>
    public bool FloatOverflow;

    /// <summary>EC-DATA-INCOMPATIBLE — a de-editing MOVE whose numeric-edited sender holds content that is not a
    /// possible result of any editing operation in that item (§14.6.13.2 rule 4; kb/Work PB66).</summary>
    public bool DataIncompatible;

    /// <summary>EC-DATA-PTR-NULL — a reference through a NULL data-address pointer (§13.18.5.4 GR3), or a NULL
    /// operand on SET pointer UP/DOWN BY (§14.9.39 Format 10 GR18).</summary>
    public bool DataPtrNull;

    /// <summary>EC-BOUND-PTR — a reference through an address that is not NULL and not a valid address of
    /// storage (§13.18.5.4 GR4).</summary>
    public bool BoundPtr;

    /// <summary>EC-SIZE-ADDRESS — a SET pointer UP/DOWN BY amount that does not evaluate to an integer
    /// (§14.9.39.4 GR19). ⛔ Its MAGNITUDE is a different rule and a different condition — see
    /// <see cref="RangePtr"/>.</summary>
    public bool SizeAddress;

    /// <summary>EC-RANGE-PTR — a SET pointer UP/DOWN BY whose new address falls outside the implementor range
    /// of data-pointer values (§14.9.39.4 GR20; the range is DOC-A.1-216). kb/Work PB465.</summary>
    public bool RangePtr;

    /// <summary>EC-PROGRAM-ARG-OMITTED — a reference to an omitted formal parameter outside the
    /// omitted-argument condition or an argument position (§14.9.4.4 GR12; kb/Work PB133 wave C).</summary>
    public bool ProgramArgOmitted;

    /// <summary>EC-FUNCTION-ARG-OMITTED — the same reference in an ACTIVATED FUNCTION (§8.4.3.2.4 GR8;
    /// kb/Work PB971). One condition per activated-element kind, never the program's name in a function.</summary>
    public bool FunctionArgOmitted;

    /// <summary>EC-OO-ARG-OMITTED — the same reference in an INVOKED METHOD (§14.9.23.4 GR10; kb/Work PB971).</summary>
    public bool OoArgOmitted;

    /// <summary>EC-OO-RESOURCE — the standard class BASE's New could not obtain the resources to create an object
    /// (§16.2.1.2 GR2; kb/Work PB1524).</summary>
    public bool OoResource;

    /// <summary>EC-BOUND-SUBSCRIPT — a subscript below 1 or above the highest permissible occurrence number
    /// (§8.4.2.3.4 GR2).</summary>
    public bool BoundSubscript;

    /// <summary>EC-BOUND-ODO — an OCCURS DEPENDING control value outside integer-1 through integer-2
    /// (§13.18.38.4 GR7).</summary>
    public bool BoundOdo;

    /// <summary>EC-RANGE-INDEX — a PERFORM VARYING, SEARCH or SET that creates a value for an index outside the
    /// implementor's range of allowed index values (§13.18.38.4 GR2; SET Format 1's GR2 a) 1. b / 2. a / 3. b and
    /// Format 2's GR4 a) name the same limit). kb/Work PB459.</summary>
    public bool RangeIndex;

    /// <summary>EC-RANGE-INSPECT-SIZE — an INSPECT REPLACING / CONVERTING operand pair of unequal size, or a
    /// CHARACTERS replacement that is not one character (§14.9.22.4 GR14 / GR15 / GR22; kb/Work PB1126).</summary>
    public bool RangeInspectSize;

    /// <summary>EC-OO-UNIVERSAL — the ACTIVATOR's half of §14.9.23.4 GR7c's "enabled in BOTH" gate. Set around
    /// an INVOKE by the emitted statement guard and read by the callee's <c>__CobolInvoke</c>, which is entered
    /// synchronously on the same run unit; the METHOD's half is a compile-time literal baked per method.</summary>
    public bool OoUniversal;

    /// <summary>EC-OO-CONFORMANCE — an object-view's run-time conformance check failed (§8.4.3.5.4 GR2–GR6; checked
    /// "if exception condition EC-OO-CONFORMANCE is enabled", §14.8.1 NOTE 3; kb/Work PB1425).</summary>
    public bool OoConformance;

    /// <summary>EC-EXTERNAL-FORMAT-CONFLICT — the ACTIVATING element's half of §14.8.4.1's "enabled in both the
    /// activating and activated runtime elements" for §14.8.4.3 (kb/Work PB1138). Set around a CALL, an INVOKE or a
    /// statement carrying an activation by the emitted statement guard; read by the activation boundary
    /// (<c>ProgramTable.CallProgram</c>, <c>ExternalStore.DescribeAtMethodActivation</c>) before the activated
    /// element's checking scope opens. The activated element's half is its compile-time before-Environment-division mask.</summary>
    public bool ExternalFormatConflict;

    /// <summary>EC-EXTERNAL-DATA-MISMATCH — the activating half of §14.8.4.1 for §14.8.4.2; see
    /// <see cref="ExternalFormatConflict"/>.</summary>
    public bool ExternalDataMismatch;

    /// <summary>EC-EXTERNAL-FILE-MISMATCH — the activating half of §14.8.4.1 for §14.8.4.4; see
    /// <see cref="ExternalFormatConflict"/>.</summary>
    public bool ExternalFileMismatch;

    /// <summary>EC-BOUND-TABLE-LIMIT — a dynamic-capacity table grown past the implementor maximum
    /// (§14.9.39.4 GR30).</summary>
    public bool BoundTableLimit;

    /// <summary>EC-FLOW-SEARCH — a capacity SET during a SEARCH of that same table (§14.9.39.4 GR31).</summary>
    public bool FlowSearch;

    /// <summary>EC-FLOW-REPORT — a GENERATE, INITIATE or TERMINATE executed within the range of a USE
    /// BEFORE REPORTING declarative procedure (§14.9.49.4 GR10).</summary>
    public bool FlowReport;

    /// <summary>EC-FLOW-USE — a statement executed during a USE procedure raised an exception condition that
    /// would cause the execution of a USE procedure "that had previously been activated and had not yet
    /// returned control to the activating entity" (§14.9.49.4 GR2; Table 13 Fatal).</summary>
    public bool FlowUse;

    /// <summary>EC-FLOW-GLOBAL-GOBACK — a GOBACK statement executed within the RANGE of a declarative procedure
    /// whose USE statement contains the GLOBAL phrase and is specified in the same program as the GOBACK
    /// (§14.9.18.4 GR6; Table 13 Fatal). ⚠ Its Table 13 neighbour EC-FLOW-GLOBAL-EXIT has NO flag because the
    /// standard states no general rule that sets it — see <c>ExceptionEngine.FlowGlobalGobackError</c>.</summary>
    public bool FlowGlobalGoback;

    /// <summary>EC-FLOW-RELEASE — a RELEASE executed other than within the range of an input procedure being
    /// executed by a SORT statement that references the file-name associated with its record-name-1
    /// (§14.9.32.4 GR1; kb/Work PB349).</summary>
    public bool FlowRelease;

    /// <summary>EC-FLOW-RETURN — a RETURN executed other than within the range of an output procedure being
    /// executed by a MERGE or SORT statement that references its file-name-1 (§14.9.34.4 GR1; kb/Work PB349).</summary>
    public bool FlowReturn;

    /// <summary>EC-SORT-MERGE-RETURN — a RETURN executed in the current output procedure after the at end
    /// condition for its file has occurred (§14.9.34.4 GR3; kb/Work PB349).</summary>
    public bool SortMergeReturn;

    /// <summary>EC-SORT-MERGE-ACTIVE — a MERGE, RETURN or format 1 SORT executed within the range of a SORT's
    /// input procedure, or a MERGE, RELEASE or format 1 SORT within the range of a SORT/MERGE output procedure
    /// (§14.9.40.4 GR10 / GR13, §14.9.24.4 GR8; kb/Work PB1036).</summary>
    public bool SortMergeActive;

    /// <summary>EC-SORT-MERGE-FILE-OPEN — a USING or GIVING file in an open mode when its SORT phase commences
    /// or when the MERGE statement starts (§14.9.40.4 GR9, §14.9.24.4 GR7 / GR12; kb/Work PB1036).</summary>
    public bool SortMergeFileOpen;

    /// <summary>EC-SORT-MERGE-RELEASE — a released record's size outside the sort-merge file's record range, by
    /// a RELEASE statement (§13.18.43.4 GR14 b) / GR19 b)) or by the implicit USING release (§14.9.40.4 GR12 b),
    /// §14.9.24.4 GR7 b); kb/Work PB1036).</summary>
    public bool SortMergeRelease;

    /// <summary>EC-SORT-MERGE-SEQUENCE — a MERGE USING file whose records are not ordered as the KEY phrases
    /// and the statement's collating sequence describe (§14.9.24.4 GR6; kb/Work PB1036).</summary>
    public bool SortMergeSequence;

    /// <summary>EC-REPORT-ACTIVE — an INITIATE of a report that is already in the active state
    /// (§14.9.21.4 GR2).</summary>
    public bool ReportActive;

    /// <summary>EC-REPORT-INACTIVE — a GENERATE (§14.9.16.4 GR7) or TERMINATE (§14.9.46.4 GR1) of a report
    /// that is not in the active state.</summary>
    public bool ReportInactive;

    /// <summary>EC-REPORT-FILE-MODE — an INITIATE whose report file connector is not open in the output or
    /// the extend mode (§14.9.21.4 GR3).</summary>
    public bool ReportFileMode;

    /// <summary>EC-REPORT-SUM-SIZE — a sum counter addition that is a size error (§13.18.54.4 GR3), and the
    /// presentation of a sum counter whose size error indicator is set (GR4); kb/Work PB1130.</summary>
    public bool ReportSumSize;

    /// <summary>EC-REPORT-VARYING — a report VARYING clause's FROM or BY arithmetic expression evaluated to a
    /// noninteger value (§13.18.64.4 GR5); kb/Work PB1305.</summary>
    public bool ReportVarying;

    /// <summary>EC-REPORT-LINE-OVERLAP — a report line printed on or above a line already printed on the page,
    /// other than a relative line with an integer-2 of zero (§13.18.35.4 GR3); kb/Work PB1247 / PB1130.</summary>
    public bool ReportLineOverlap;

    /// <summary>EC-REPORT-PAGE-LIMIT — a report line of a paged report placed beyond the page limit, i.e. a report
    /// group split between two pages (§13.18.35.4 GR2); kb/Work PB1188.</summary>
    public bool ReportPageLimit;

    /// <summary>EC-REPORT-COLUMN-OVERLAP — a printable item using a column position another printable item of the
    /// same line already uses (§13.18.14.4 GR4); kb/Work PB1188.</summary>
    public bool ReportColumnOverlap;

    /// <summary>EC-REPORT-PAGE-WIDTH — a printable item whose final column exceeds the page width (§13.18.14.4
    /// GR5); kb/Work PB1188.</summary>
    public bool ReportPageWidth;

    /// <summary>EC-ORDER-NOT-SUPPORTED — FUNCTION STANDARD-COMPARE naming a cultural ordering table this
    /// processor does not provide, or an ordering level the table does not define (§15.85.4 r2).</summary>
    public bool OrderNotSupported;

    /// <summary>EC-LOCALE-MISSING — a locale that is not available in this operating environment: SET LOCALE's
    /// locale-name-1 (§14.9.39.4 GR24), a named IS LOCALE collating sequence at use (§8.2.1; DESIGN-locale-facility L1).</summary>
    public bool LocaleMissing;

    /// <summary>EC-LOCALE-INVALID-PTR — SET LOCALE through a pointer that does not reference saved locale
    /// information (§14.9.39.4 GR21).</summary>
    public bool LocaleInvalidPtr;

    /// <summary>EC-LOCALE-INCOMPATIBLE — a locale comparison over an operand the locale's collating sequence does
    /// not order (§8.8.4.2.11; DETERMINATION L6 — an ill-formed UTF-16 operand).</summary>
    public bool LocaleIncompatible;

    /// <summary>EC-LOCALE-INVALID — an operation using a locale whose content is invalid or incomplete in this
    /// environment (§8.2.1): a LOCALE-DATE / -TIME over a locale with no LC_TIME culture data (T4; T5/T6 add LC_CTYPE /
    /// LC_MONETARY).</summary>
    public bool LocaleInvalid;

    /// <summary>EC-LOCALE-SIZE — locale editing of a PICTURE format-2 item truncated a character that is neither a
    /// zero nor a space caused by a suppressed zero (§13.18.40.5 r14 b); Table 13 Fatal; PB64 T6).</summary>
    public bool LocaleSize;

    /// <summary>The AMBIENT STATEMENT CONTEXT (<see cref="ExceptionEngine.EnterStatement"/>): the executing statement's
    /// Table 12 name and location, and the exception-names whose enabling TURN carried WITH LOCATION there — what a raise
    /// with no explicit pair answers for §15.32.3 r2 / §15.30.3 r2. It is part of THIS value, not a field beside it,
    /// because it is a property of the same source text as the flags (§7.3.25.4 GR6) and so has the same scope: every
    /// boundary that opens a checking BASELINE (<see cref="ExceptionEngine.PushAllCheckingOff"/> — a CALL or function
    /// activation, a method body, a USE procedure, a PERFORMed range or a nested statement list) clears it, and its
    /// restore brings the activating statement's context back (kb/Work PB2745: kept beside the flags, it leaked across
    /// those boundaries, and a raise in a CALLed program whose own TURN had no LOCATION answered the CALL statement's
    /// name and location where §15.32.3 r1 requires spaces). Internal: generated code reaches it only through
    /// <c>EnterStatement</c> / <c>ExitStatement</c>.</summary>
    internal (string? Name, string? Location, string[]? LocationNames) Statement;
}
