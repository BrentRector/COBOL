// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The run unit's USE BEFORE REPORTING RANGE (ISO/IEC 1989:2023 §14.9.49.4 GR10): "If a GENERATE, INITIATE,
/// or TERMINATE statement is executed within the range of a declarative procedure whose USE statement contains
/// the BEFORE REPORTING phrase, the EC-FLOW-REPORT exception condition is set to exist, the result of the
/// execution of the GENERATE, INITIATE, or TERMINATE statement is unsuccessful, and the state of the report is
/// unchanged."
///
/// <para>⛔ THE RANGE IS THE RUN UNIT'S — not one report's, and not one program's. It is DYNAMIC (the standard's
/// other flow rules read the same way: §14.9.32.4 GR1 makes a RELEASE legal only "within the range of an input
/// procedure being executed by a SORT statement"), and GR10 attaches NO element qualifier where its nearest
/// neighbour §14.9.18.4 GR6 attaches one explicitly for EC-FLOW-GLOBAL-GOBACK ("… and that USE statement is
/// specified in the same program as the GOBACK statement"). The standard says "the same program" when it means
/// it; GR10 does not. So a declarative on report R-1 that reaches a GENERATE of a SECOND report — or, through a
/// CALL, a report of another runtime element — is still inside the range, and a per-<see cref="CobolReport"/>
/// flag would be exactly the one-arm-of-two dispatch this project keeps rediscovering.</para>
///
/// <para>A DEPTH rather than a bool: §14.9.49.4 GR2's re-entrancy guard blocks re-invocation of an ACTIVE use
/// procedure, but two DIFFERENT groups' declaratives nest routinely (a control footing presented from inside a
/// detail group's presentation), and only a counter survives that.</para>
/// </summary>
public sealed class ReportFlowState
{
    private int _depth;

    /// <summary>True while control is anywhere inside a USE BEFORE REPORTING declarative procedure.</summary>
    public bool InBeforeReporting => _depth > 0;

    /// <summary>Enter a USE BEFORE REPORTING declarative (§14.9.49.4 GR8 — just before the group is produced).</summary>
    public void Enter() => _depth++;

    /// <summary>Leave a USE BEFORE REPORTING declarative. Called from a <c>finally</c>, so a fatal EC thrown out
    /// of the declarative cannot leave the run unit permanently inside the range.</summary>
    public void Exit()
    {
        if (_depth > 0) _depth--;
    }
}

/// <summary>The seven report group types (ISO/IEC 1989:2023 §13.18.57 TYPE clause Format 2). DETAIL,
/// CONTROL HEADING and CONTROL FOOTING are the BODY groups (§13.18.57.3 SR15) — the page-fit machinery applies
/// to them; the heading/footing groups are presented by the RWCS at fixed logical points.</summary>
public enum ReportGroupKind { ReportHeading, PageHeading, ControlHeading, Detail, ControlFooting, PageFooting, ReportFooting }

/// <summary>A report line's LINE clause form (ISO §13.18.35): absolute (<c>LINE n</c>), relative
/// (<c>LINE PLUS n</c>), or — for a repetition of a VERTICALLY repeating entry whose line is relative — the
/// STEP placement of §13.18.38.4 GR12c/GR12d. The <c>NEXT PAGE</c> phrase is not a kind: it is
/// <see cref="ReportGroupLine.NextPage"/> on an absolute (<c>integer-1 ON NEXT PAGE</c>) or relative (the bare
/// <c>ON NEXT PAGE</c> operand) first line.</summary>
public enum ReportLineKind
{
    /// <summary>LINE NUMBER integer-1 — the line stands at that page line number (ISO §13.18.35.4 GR5a/GR7a).</summary>
    Absolute,
    /// <summary>LINE PLUS integer-2 — integer-2 lines below LINE-COUNTER (GR5b/GR5c/GR7b).</summary>
    Relative,
    /// <summary>A later occurrence of a STEP'd vertically repeating entry: "report lines in successive
    /// occurrences are positioned integer-3 lines vertically beneath the line they occupy in the preceding
    /// occurrence" (ISO §13.18.38.4 GR12d, and GR12c for the one-line form). The datum is the line's ANCHOR —
    /// the page line the FIRST occurrence of this same line landed on — not LINE-COUNTER, because the lines
    /// between them belong to the intervening occurrences. An ABSOLUTE line needs no kind of its own: its
    /// displaced position is integer-1 + Σ ordinal × integer-3, a compile-time constant.</summary>
    Step,
}

/// <summary>One report line of a report group: its LINE clause and the generated COMPOSE method that renders the
/// line's printable items against the program's live state. Composition runs AT PRESENTATION TIME — after
/// LINE-COUNTER is set to the line's number (ISO §13.18.35.4 GR6) — which is what makes <c>SOURCE IS
/// LINE-COUNTER</c> print the line's OWN number and every SOURCE an implicit MOVE executed "when the line is
/// printed" (§13.18.53.4 GR1/GR3).</summary>
public sealed class ReportGroupLine(ReportLineKind kind, int value, Func<string> compose, Func<bool>? present = null,
    int anchor = 0, int relativeBase = 0, int trialInterval = 0, bool nextPage = false)
{
    /// <summary>The line's LINE clause carries the NEXT PAGE phrase (ISO §13.18.35.2 Format 1). The binder sets it
    /// only on a group's first report line (§13.18.35.3 SR7), and the engine reads it only on the group's first
    /// PRESENT line — "Which LINE clause is taken to be the first may depend on the current values of conditions in
    /// PRESENT WHEN clauses" (§13.18.35.4 GR4/GR5): a body group then declares its page fit unsuccessful without a
    /// test (GR4a), and a report footing begins on a new page (GR5a).</summary>
    public bool NextPage { get; } = nextPage;

    /// <summary>Absolute, relative, or a repeating entry's STEP placement (ISO §13.18.35 / §13.18.38.4 GR12).</summary>
    public ReportLineKind Kind { get; } = kind;

    /// <summary>integer-1 (absolute) or integer-2 (relative) of the LINE clause — or, for
    /// <see cref="ReportLineKind.Step"/>, the vertical displacement Σ ordinal × integer-3 from the ANCHOR.</summary>
    public int Value { get; } = value;

    /// <summary>The line's step-anchor slot, 0 for a line that neither seeds nor steps from one (ISO
    /// §13.18.38.4 GR12c/GR12d). A non-Step line with an anchor SEEDS it with the page line it lands on; a
    /// <see cref="ReportLineKind.Step"/> line READS it. One slot per (report line × undisplaced enclosing
    /// ordinals), so nested repeating entries cannot share a datum.</summary>
    public int Anchor { get; } = anchor;

    /// <summary>A Step line's own written integer-2 — used ONLY when its anchor was never seeded because the
    /// first occurrence of this line was absent under a PRESENT WHEN clause (§13.18.41.4 GR2b). There is then no
    /// "preceding occurrence" to measure from, so the first PRESENT occurrence places relatively and becomes the
    /// anchor instead.</summary>
    public int RelativeBase { get; } = relativeBase;

    /// <summary>What this line adds to the §13.18.35.4 GR4c page-fit trial sum. A relative line contributes its
    /// own integer-2 ("the trial sum is incremented by integer-2 for each subsequent LINE clause"); an absolute
    /// line contributes nothing (GR4b governs that test instead); a <see cref="ReportLineKind.Step"/> line
    /// contributes the amount the BINDER computed — its expected offset from the group's start minus the
    /// preceding line's — so that the sum over a group is exactly GR4c's "expected position of the last line of
    /// the report group" however the repetitions interleave. That is GR4c's next sentence discharged in the same
    /// arithmetic: "Wherever there is an OCCURS clause at the level of the LINE clause or above, the vertical
    /// interval between successive occurrences is added into the trial sum once for each occurrence beyond the
    /// first" — with STEP the interval is integer-3, and the offsets add it exactly once per occurrence.</summary>
    public int TrialInterval { get; } = kind == ReportLineKind.Relative ? value : trialInterval;

    /// <summary>The generated compose method — the §13.18.53.4 GR1 implicit MOVEs into one line image.</summary>
    public Func<string> Compose { get; } = compose;

    /// <summary>The line's PRESENT WHEN condition chain (ISO §13.18.41 Format 1), AND-composed by the emitter;
    /// null = unconditional. Evaluated ONCE per group presentation, before any LINE clause is processed (GR2);
    /// false ⇒ the line is processed as though its entry were omitted (GR2b).</summary>
    public Func<bool>? Present { get; } = present;
}

/// <summary>The three forms of the NEXT GROUP clause (ISO §13.18.37.2): <c>integer-1</c>, <c>{PLUS|+} integer-2</c>
/// and <c>NEXT PAGE [WITH RESET]</c> — "exactly one alternative shall be selected".</summary>
public enum ReportNextGroupKind
{
    /// <summary>NEXT GROUP integer-1 — an absolute line number (§13.18.37.3 SR1).</summary>
    Absolute,
    /// <summary>NEXT GROUP PLUS integer-2 — a relative vertical distance (SR1; SR2 — PLUS and + are synonyms).</summary>
    Relative,
    /// <summary>NEXT GROUP NEXT PAGE — the next group begins a new page.</summary>
    NextPage,
}

/// <summary>A report group's NEXT GROUP clause (ISO §13.18.37): its form, integer-1 / integer-2 (0 for NEXT PAGE),
/// and whether the NEXT PAGE form carries WITH RESET (GR6 — PAGE-COUNTER is set to 1 at the next page advance).
/// The engine applies it after the group's last line is printed (GR2), per the group's type (GR3–GR5).</summary>
public sealed record ReportNextGroup(ReportNextGroupKind Kind, int Value, bool Reset = false);

/// <summary>One report group (ISO §13.15 report group description entry): its TYPE, name (referenced by GENERATE
/// for a detail, §14.9.16 SR1), control level (CH/CF — index into the report's control hierarchy, −1 otherwise),
/// and its report lines in declaration order.</summary>
public sealed class ReportGroup(ReportGroupKind kind, string name, int controlLevel, ReportGroupLine[] lines)
{
    public ReportGroupKind Kind { get; } = kind;
    public string Name { get; } = name;

    /// <summary>The CH/CF control level — the index into the report's major→minor control list; −1 for
    /// non-control groups (ISO §13.18.16.4 GR1).</summary>
    public int ControlLevel { get; } = controlLevel;

    public ReportGroupLine[] Lines { get; } = lines;

    /// <summary>The group's ordinal within its report, in report-description order — assigned by
    /// <see cref="CobolReport.AddGroup"/>, which the generated construction calls in that order. It is the key a
    /// statement's USE BEFORE REPORTING selector is asked with (ISO §14.9.49.4 GR4/GR8).
    /// <para>⛔ THERE IS NO PER-GROUP HOOK. Which declarative runs before a group is produced is NOT a property of
    /// the group: §14.9.49.4 GR4 ("FORMATS 1 AND 2") selects it from the source element that contains the
    /// statement that caused the group to be produced, and then from the GLOBAL declaratives of its containers —
    /// so one group of a GLOBAL report (§13.18.27) runs different declaratives for a GENERATE written in the
    /// declaring program and for one written in a contained program. The selector therefore travels with the
    /// GENERATE / TERMINATE call (kb/Work PB369).</para></summary>
    public int Index { get; internal set; } = -1;

    /// <summary>The (column, width) spans of this group's GROUP INDICATE printable items (ISO §13.18.29): they
    /// print on the first presentation after an INITIATE / page advance / control break and are blanked on
    /// every other presentation.</summary>
    public List<(int Column, int Width)> IndicateFields { get; } = [];

    /// <summary>The group's NEXT GROUP clause (ISO §13.18.37; §13.15.3 SR6 — level 1 only), null when none.</summary>
    public ReportNextGroup? NextGroup { get; set; }
}

/// <summary>
/// The per-report Report Writer Control System engine (ISO/IEC 1989:2023 §14.9.16 GENERATE / §14.9.21 INITIATE /
/// §14.9.46 TERMINATE over the §13.18 report description clauses; COBOLNET_REPORT_WRITER_DESIGN). ONE mechanism
/// composes every report line: a generated compose delegate invoked at presentation time (§13.18.53.4 GR3 —
/// the implicit MOVE executes when the line is printed), after LINE-COUNTER is set to the line's number
/// (§13.18.35.4 GR6). There is no byte plan, no registration kinds — the typed-native singular pattern.
/// Physical output goes through the report file's connector via <see cref="CobolFile.WriteAdvancing"/>
/// (a print-control stream); <see cref="_physLine"/> tracks the physical position independently of
/// LINE-COUNTER, because a NEXT GROUP clause moves LINE-COUNTER without printing (§8.4.3.15.4 GR4, §13.18.37.4).
/// </summary>
public sealed class CobolReport(
    string name, string fileName, int lineWidth, bool paged,
    int pageLimit, int heading, int firstDetail, int lastControlHeading, int lastDetail, int footing)
{
    /// <summary>The report-name (the RD entry's name).</summary>
    public string Name { get; } = name;

    private readonly string _fileName = fileName;   // the emit-qualified connector name ("PROG::FILE")
    private readonly int _lineWidth = lineWidth;
    private readonly bool _paged = paged;           // PAGE clause present (§13.18.39.4 GR2a — absent ⇒ one page of indefinite length)

    // Page regions (§13.18.39.4 GR2, binder-supplied GR3 defaults).
    private readonly int _pageLimit = pageLimit;
    private readonly int _heading = heading;
    private readonly int _firstDetail = firstDetail;
    private readonly int _lastControlHeading = lastControlHeading;
    private readonly int _lastDetail = lastDetail;
    private readonly int _footing = footing;

    /// <summary>The report's LINE-COUNTER (ISO §8.4.3.15): an unsigned integer; 0 after INITIATE (GR3), set to
    /// each line's number as it is printed (§13.18.35.4 GR6), reset to 0 at every page advance (GR3).</summary>
    public long LineCounter { get; private set; }

    /// <summary>The report's PAGE-COUNTER (ISO §8.4.3.15): 1 after INITIATE (GR2), +1 at each page advance.
    /// ⛔ SET BY THE PROCEDURE DIVISION TOO — see <see cref="SetPageCounter"/>.</summary>
    public long PageCounter { get; private set; }

    /// <summary>Assign PAGE-COUNTER from the procedure division (ISO §8.4.3.15.3 SR1 — "In the procedure
    /// division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an integer data item may
    /// appear", and SR3 subtracts only LINE-COUNTER from the receiving side; §13.18.37.4 GR6's parenthetical
    /// "(unless procedurally altered)" is the standard saying a program assigning page numbers is the intended
    /// use). §8.4.3.15.4 GR1 makes the counter an UNSIGNED integer, so the binder's receiving place carries an
    /// unsigned profile and the value reaching here is already the stored one (kb/Work PB429).</summary>
    public void SetPageCounter(long value) => PageCounter = value;

    private bool _active;                  // INITIATE…TERMINATE state (§14.9.21.4 GR4)

    /// <summary>The report is in the ACTIVE state — INITIATEd and not yet TERMINATEd (§14.9.21.4 GR4). Read
    /// by the emitted CLOSE of the report's file: §14.9.6.4 GR5 completes the CLOSE and sets
    /// EC-REPORT-NOT-TERMINATED to exist when any associated report is still active (kb/Work PB141).</summary>
    public bool IsActive => _active;
    private bool _started;                 // a GENERATE has executed since INITIATE (§14.9.46.4 GR2/GR3)
    private bool _firstBodySinceInitiate;  // the §13.18.35.4 GR4 page-fit exemption
    private bool _firstBodyOnPage;         // the §13.18.35.4 GR5b3 FIRST DETAIL placement
    private bool _rhOnThisPage;            // a report heading printed on the current page (GR5b2)
    private bool _pfOnThisPage;            // a page footing printed on the current page (GR5b5)
    private bool _indicateFresh;           // GROUP INDICATE freshness (§13.18.29 — run / page / control-group start)
    private bool _suppressCurrent;         // §14.9.45 — a SUPPRESS executed in the presenting group's USE BEFORE REPORTING
    private int _physLine;                 // physical line position on the current page (0 = top, nothing printed)

    /// <summary>The NEXT GROUP SAVE LOCATION (ISO §13.18.37.4 GR4a): integer-1 of a body group's absolute NEXT
    /// GROUP clause that LINE-COUNTER had already reached; 0 = empty (an absolute integer-1 is ≥ FIRST DETAIL ≥ 1,
    /// SR6b). While it is set, LINE-COUNTER holds the FOOTING integer and the next non-dummy body group is placed
    /// by GR4a 1–3 instead of the ordinary §13.18.35.4 GR4/GR5 rules.</summary>
    private int _nextGroupSave;

    /// <summary>LINE-COUNTER as the group that filled <see cref="_nextGroupSave"/> left it — restored when a
    /// TERMINATE is the next statement for the report, because GR4a says the clause then has "no effect at
    /// all".</summary>
    private long _lineCounterBeforeSave;

    /// <summary>ISO §13.18.37.4 GR6 — a NEXT GROUP NEXT PAGE WITH RESET was processed: the next page advance sets
    /// PAGE-COUNTER to 1 instead of incrementing it (§14.9.16.4 GR6d, GR4a).</summary>
    private bool _resetPageCounterAtAdvance;

    private int _groupCount;               // the next ReportGroup.Index AddGroup assigns

    /// <summary>The executing statement's USE BEFORE REPORTING selector (ISO §14.9.49.4 GR4/GR8): asked with a
    /// group's <see cref="ReportGroup.Index"/> just before that group is produced, it runs the qualifying
    /// declarative — the one in the source element containing the GENERATE / TERMINATE, else a GLOBAL one of a
    /// container — and answers whether one ran. Set for the duration of one GENERATE or TERMINATE and cleared
    /// after it; null when no declarative anywhere in the statement's scope names a group of this report.</summary>
    private Func<int, bool>? _beforeReporting;

    private ReportGroup? _reportHeading, _pageHeading, _pageFooting, _reportFooting;
    private readonly Dictionary<string, ReportGroup> _details = new(StringComparer.OrdinalIgnoreCase);
    private readonly SortedDictionary<int, ReportGroup> _controlHeadings = [];   // by control level (0 = most major)
    private readonly SortedDictionary<int, ReportGroup> _controlFootings = [];

    /// <summary>One CONTROL operand (ISO §13.18.16): FINAL or a data item reached through generated get/set
    /// delegates over the program's typed storage (the character image is the break-compare key — §13.18.16.4
    /// GR3's prior-control save/compare, representation-faithful for every category).</summary>
    private sealed class ControlEntry(bool isFinal, Func<string> get, Action<string> set)
    {
        public bool IsFinal { get; } = isFinal;
        public Func<string> Get { get; } = get;
        public Action<string> Set { get; } = set;
        public string? Prior { get; set; }   // saved at the first GENERATE (§13.18.16.4 GR3); null until then
    }

    private readonly List<ControlEntry> _controls = [];   // major→minor (FINAL, if present, is index 0 — GR2)

    /// <summary>ONE <c>SUM … [UPON …]</c> GROUP of a SUM clause (ISO §13.18.54.3 SR1 — the SUM keyword may appear
    /// more than once, and §13.18.54.4 GR1 still gives the ENTRY one counter): the group's addend total, already
    /// at the counter's scale (GR9 sums a group's addends together), and the group's OWN UPON filter — GR7 c) 2)
    /// accumulates "whenever any GENERATE statement is executed for a detail referenced by the UPON phrase", and
    /// the phrase belongs to its group. Null = no UPON phrase (GR7 c) 1) — every GENERATE for this report).</summary>
    private readonly record struct SumTerm(Func<long> Addend, string[]? UponDetails)
    {
        /// <summary>Whether this term accumulates on a GENERATE of <paramref name="detailName"/> (GR7 c)).</summary>
        public bool Fires(string? detailName) =>
            UponDetails is null
            || (detailName is not null
                && Array.FindIndex(UponDetails, d => d.Equals(detailName, StringComparison.OrdinalIgnoreCase)) >= 0);
    }

    /// <summary>One SUM counter (ISO §13.18.54): an unscaled integer accumulation at the counter's scale (GR1 —
    /// digits derived from the entry's PICTURE), the clause's <see cref="Terms"/> over the program's typed
    /// storage, the RESET control level (GR2; −1 = reset where printed), and the group it prints in.</summary>
    private sealed class SumEntry(int resetLevel, ReportGroup printedIn, Func<bool>? present)
    {
        public long Value;

        /// <summary>The clause's <c>SUM … [UPON …]</c> groups in written order, each with its own UPON filter.</summary>
        public List<SumTerm> Terms { get; } = [];
        public int ResetLevel { get; } = resetLevel;
        public ReportGroup PrintedIn { get; } = printedIn;

        /// <summary>The SUM entry's PRESENT WHEN chain (ISO §13.18.41.4 GR3g / §13.18.54.4 GR10): when false at
        /// a presentation the counter is neither printed (the printable face carries the same chain in its
        /// compose) nor reset. Null = unconditional.</summary>
        public Func<bool>? Present { get; } = present;
    }

    /// <summary>⛔ THE SUM COUNTERS, KEYED BY THE ENTRY — NEVER BY A SPELLING (kb/Work PB882). ISO §13.18.54.4
    /// GR1: "Each entry containing a SUM clause establishes an independent sum counter and size error
    /// indicator." The identity is therefore the ENTRY, and the compiler hands it over as that entry's ORDINAL
    /// within its report description. It was a <c>Dictionary&lt;string, SumEntry&gt;</c> keyed by
    /// <c>ReportSumModel.Id</c> = the entry's data-name, so two entries that legally share a data-name — GR5
    /// names the COUNTER, and an unreferenced declaration engages no §8.4.2.2.1 uniqueness requirement — shared
    /// one counter and the second registration DESTROYED the first: `0022  0022` printed where the standard owes
    /// `0011  0022`. A list indexed by the ordinal makes that collision structurally impossible, and drops a
    /// case-insensitive string hash off the per-presentation compose path.</summary>
    private readonly List<SumEntry> _sums = [];

    // ── Registration (generated by the compiler in __Activate, once per program instance) ─────────────────────

    /// <summary>Register a report group into its slot (TYPE-driven; §13.18.57.3 SR13/SR14 cap each slot at one,
    /// diagnosed at bind).</summary>
    public void AddGroup(ReportGroup g)
    {
        g.Index = _groupCount++;
        switch (g.Kind)
        {
            case ReportGroupKind.ReportHeading: _reportHeading = g; break;
            case ReportGroupKind.PageHeading: _pageHeading = g; break;
            case ReportGroupKind.PageFooting: _pageFooting = g; break;
            case ReportGroupKind.ReportFooting: _reportFooting = g; break;
            case ReportGroupKind.ControlHeading: _controlHeadings[g.ControlLevel] = g; break;
            case ReportGroupKind.ControlFooting: _controlFootings[g.ControlLevel] = g; break;
            default: _details[g.Name] = g; break;
        }
    }

    /// <summary>Register one CONTROL operand, major→minor order (ISO §13.18.16.4 GR1/GR2; FINAL first).</summary>
    public void AddControl(bool isFinal, Func<string> get, Action<string> set) =>
        _controls.Add(new ControlEntry(isFinal, get, set));

    /// <summary>Register a SUM counter (ISO §13.18.54.4 GR1 — one per ENTRY containing a SUM clause).
    /// <paramref name="resetLevel"/> is the RESET control level (GR2; −1 = reset at the end of the group it
    /// prints in); <paramref name="present"/> is the entry's PRESENT WHEN chain (§13.18.41.4 GR3g — false
    /// suppresses the end-of-group reset; null = unconditional). The addends arrive through
    /// <see cref="AddSumTerm"/>, one call per <c>SUM … [UPON …]</c> group.
    /// <para><paramref name="id"/> is the ENTRY's ordinal within its report description (GR1 — the counter's
    /// identity is the entry, never its data-name; kb/Work PB882). The compiler emits the registrations in
    /// ordinal order, so the call APPENDS; a gap would mean the emitter and the model disagree about which
    /// entry a counter belongs to, which is exactly the confusion this keying exists to prevent.</para></summary>
    public void AddSum(int id, int resetLevel, ReportGroup printedIn, Func<bool>? present = null)
    {
        if (id != _sums.Count)
            throw new InvalidOperationException(
                $"report '{Name}': sum counter {id} registered out of order (expected {_sums.Count}) — the "
                + "counter's identity is its entry's ordinal (ISO §13.18.54.4 GR1)");
        _sums.Add(new SumEntry(resetLevel, printedIn, present));
    }

    /// <summary>Register one <c>SUM … [UPON …]</c> group of the counter <paramref name="id"/> (ISO §13.18.54.3
    /// SR1 — "the SUM keyword may appear more than once"). <paramref name="addend"/> yields that group's addend
    /// total, already at the counter's scale (GR9); <paramref name="uponDetails"/> restricts its accumulation to
    /// the named details (GR7 c) 2); null = every GENERATE for this report, GR7 c) 1).</summary>
    public void AddSumTerm(int id, Func<long> addend, string[]? uponDetails) =>
        _sums[id].Terms.Add(new SumTerm(addend, uponDetails));

    /// <summary>A SUM counter's current value (unscaled, at the counter's scale) — read by the generated compose
    /// of the printable item the counter is the source of (ISO §13.18.54.4 GR4), and by a procedure division
    /// statement that names the counter (GR5 + GR12).</summary>
    public long SumValue(int id) => _sums[id].Value;

    /// <summary>Alter a SUM counter's content from the procedure division (ISO §13.18.54.4 GR12 — "It is
    /// permissible for procedure division statements to alter the content of sum counters"). The value is
    /// unscaled, at the counter's own scale (GR1 — derived from the entry's PICTURE).</summary>
    public void SetSumValue(int id, long value) => _sums[id].Value = value;

    // ── INITIATE (ISO §14.9.21.4) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>INITIATE this report (ISO §14.9.21.4 GR1): sum counters ← 0 (GR1a; size-error indicators are the
    /// EC-REPORT-SUM-SIZE seam — checking default-off, COBOLNET_DESIGN §18.16), LINE-COUNTER ← 0 (GR1b),
    /// PAGE-COUNTER ← 1 (GR1c); the report becomes active (GR4). GR2: an INITIATE of an ACTIVE report raises
    /// EC-REPORT-ACTIVE and has no other effect. GR3: the file is NOT opened here — it must ALREADY be open in the
    /// output or the extend mode, and when it is not, EC-REPORT-FILE-MODE is raised and no action is taken on the
    /// report. §14.9.49.4 GR10 outranks both: inside a USE BEFORE REPORTING range the statement is unsuccessful
    /// (EC-FLOW-REPORT) and the report's state is unchanged. All three raises are >>TURN-gated; all three RETURNS
    /// are unconditional, because the standard states each lenient outcome outright (kb/Work PB326).</summary>
    public void Initiate()
    {
        // §14.9.49.4 GR10 — inside a USE BEFORE REPORTING range the statement is unsuccessful and the state of
        // the report is unchanged. The RETURN is UNCONDITIONAL: GR10 states that outcome outright, so it holds
        // whether or not EC-FLOW-REPORT checking is enabled; only the RAISE is gated (§14.6.13.1.1).
        if (RunUnit.Current.ReportFlow.InBeforeReporting)
        {
            ExceptionState.FlowReportError($"INITIATE {Name}: executed within the range of a USE BEFORE "
                + "REPORTING declarative procedure (ISO §14.9.49.4 GR10)");
            return;
        }
        if (_active)   // §14.9.21.4 GR2 — "the execution of the INITIATE statement has no other effect"
        {
            ExceptionState.ReportActiveError($"INITIATE {Name}: the report is already in the active state "
                + "(ISO §14.9.21.4 GR2)");
            return;
        }
        // §14.9.21.4 GR3 — "the INITIATE statement may be executed only if the corresponding file connector is
        // open in the extend mode or the output mode. If the file connector is not open in the output or extend
        // mode, the EC-REPORT-FILE-MODE exception condition is set to exist and no action is taken on the
        // report." This is the DETECTION half of §14.9.27.4 GR7 ("The OPEN statement for a report file connector
        // shall be executed before the execution of an INITIATE statement that references a report-name that is
        // associated with file-name-1"); the other half — that nothing opens a report file connector implicitly
        // — holds by construction. OpenModeIfOpen, NOT OpenModeOf: the §14.9.49.4 GR6b view also answers with the
        // ATTEMPTED mode of a FAILED open, and an INITIATE after an unsuccessful OPEN OUTPUT must not proceed.
        if (CobolFile.OpenModeIfOpen(_fileName) is not (FileOpenMode.Output or FileOpenMode.Extend))
        {
            ExceptionState.ReportFileModeError($"INITIATE {Name}: the report's file connector is not open in "
                + "the output or the extend mode (ISO §14.9.21.4 GR3 / §14.9.27.4 GR7)");
            return;   // "no action is taken on the report" — no counter resets, no activation
        }
        foreach (var s in _sums) s.Value = 0;   // GR1a
        LineCounter = 0;                               // GR1b
        PageCounter = 1;                               // GR1c
        _active = true;                                // GR4
        _started = false;
        _firstBodySinceInitiate = true;
        _firstBodyOnPage = true;
        _rhOnThisPage = false;
        _pfOnThisPage = false;
        _indicateFresh = true;                         // §13.18.29 — the run's first presentation indicates
        _physLine = 0;
        _nextGroupSave = 0;                            // §13.18.37.4 — no NEXT GROUP carries across an INITIATE
        _resetPageCounterAtAdvance = false;
        foreach (var c in _controls) c.Prior = null;   // priors are saved by the first GENERATE (§13.18.16.4 GR3)
    }

    // ── GENERATE (ISO §14.9.16.4) ─────────────────────────────────────────────────────────────────────────────

    /// <summary>GENERATE one detail (<paramref name="detailName"/>) or a summary instance (null — §14.9.16.4 GR2,
    /// same processing with no detail printed). First GENERATE (GR4): RH once → PH → CHs major→minor → detail.
    /// Subsequent (GR5): on a control break, CFs minor→break then CHs break→minor (GR5a / §13.18.16.4 GR4), then
    /// the detail. Body groups page-fit per §13.18.35.4 GR4 (the chronologically first since INITIATE exempt);
    /// an unsuccessful fit page-advances per GR6 (PF → physical advance → PAGE-COUNTER → LINE-COUNTER ← 0 → PH).
    /// GR7: a GENERATE for an INACTIVE report raises EC-REPORT-INACTIVE and does nothing; §14.9.49.4 GR10: one
    /// executed inside a USE BEFORE REPORTING range raises EC-FLOW-REPORT, is unsuccessful, and leaves the state
    /// of the report unchanged (kb/Work PB326).</summary>
    public void Generate(string? detailName, Func<int, bool>? beforeReporting = null)
    {
        var saved = _beforeReporting;   // restored, not cleared: a GR10-refused nested call must not clobber it
        _beforeReporting = beforeReporting;
        try { GenerateCore(detailName); }
        finally { _beforeReporting = saved; }
    }

    private void GenerateCore(string? detailName)
    {
        // §14.9.49.4 GR10 — see Initiate: unsuccessful, report state unchanged, the raise gated by checking.
        if (RunUnit.Current.ReportFlow.InBeforeReporting)
        {
            ExceptionState.FlowReportError($"GENERATE for report {Name}: executed within the range of a USE "
                + "BEFORE REPORTING declarative procedure (ISO §14.9.49.4 GR10)");
            return;
        }
        // §14.9.16.4 GR7 — "shall be in the active state. If it is not, the EC-REPORT-INACTIVE exception
        // condition is set to exist, if it is enabled."
        if (!_active)
        {
            ExceptionState.ReportInactiveError($"GENERATE for report {Name}: the report is not in the active "
                + "state (ISO §14.9.16.4 GR7)");
            return;
        }
        if (!_started)
        {
            _started = true;
            // GR4a: the report heading, exactly once. An RH whose NEXT GROUP clause is NEXT PAGE is on a page by
            // itself, and its page advance happens inside the presentation (ApplyNextGroup, §13.18.37.4 GR3c).
            if (_reportHeading is { } rh) PresentHeadingFooting(rh);
            // GR4b / GR6: the page heading precedes the chronologically first body group.
            if (_pageHeading is not null) PresentPageHeading();
            // §13.18.16.4 GR3: the first GENERATE saves each control item in its prior control.
            foreach (var c in _controls) c.Prior = c.Get();
            // GR4c: control headings, major → minor.
            foreach (var ch in _controlHeadings.Values) PresentBody(ch);
        }
        else if (_controls.Count > 0 && DetectBreakLevel() is { } breakLevel)
        {
            // §14.9.16.4 GR5a / §13.18.16.4 GR4a: save current values, restore the PRIOR values so the ending
            // groups' CFs (and any reference to a control item while they print) see the pre-break contents,
            // print CFs minor→break, then restore the new current values and print CHs break→minor.
            _indicateFresh = true;   // §13.18.29 — a control break starts a new group instance
            var current = new string[_controls.Count];
            for (int i = 0; i < _controls.Count; i++)
            {
                current[i] = _controls[i].Get();
                if (_controls[i].Prior is { } prior) _controls[i].Set(prior);
            }
            // §13.18.37.4 GR1 — a control footing's NEXT GROUP clause "has no effect when it is specified in a
            // control footing that is at a level other than the highest level at which the control break is
            // detected": only the footing AT the break level applies it.
            for (int i = _controls.Count - 1; i >= breakLevel; i--)
                if (_controlFootings.TryGetValue(i, out var cf)) PresentBody(cf, applyNextGroup: i == breakLevel);
            for (int i = 0; i < _controls.Count; i++)
            {
                _controls[i].Set(current[i]);
                _controls[i].Prior = current[i];   // GR4a tail — new current values become the priors
            }
            for (int i = breakLevel; i < _controls.Count; i++)
                if (_controlHeadings.TryGetValue(i, out var ch)) PresentBody(ch);
        }

        // SUM accumulation (§13.18.54.4 GR7c): on every GENERATE for the report (GR7c1) or, with UPON, on a
        // GENERATE of a named detail (GR7c2) — AFTER the control-break processing, so a control footing printed
        // above showed the ended group's total (its reset happened at the end of its printing, GR2).
        foreach (var s in _sums)
            foreach (var t in s.Terms)
                if (t.Fires(detailName)) s.Value += t.Addend();

        // GR4d / GR5b: the specified detail — unless summary reporting (GR2).
        if (detailName is not null && _details.TryGetValue(detailName, out var detail))
            PresentBody(detail);
    }

    /// <summary>The most-major control level whose CURRENT value differs from its prior (§13.18.16.4 GR3 —
    /// tested major→minor, the first change wins; FINAL never breaks mid-report, GR2). Null when no break.</summary>
    private int? DetectBreakLevel()
    {
        for (int i = 0; i < _controls.Count; i++)
        {
            if (_controls[i].IsFinal) continue;
            if (_controls[i].Prior is { } prior && !string.Equals(_controls[i].Get(), prior, StringComparison.Ordinal))
                return i;
        }
        return null;
    }

    // ── TERMINATE (ISO §14.9.46.4) ────────────────────────────────────────────────────────────────────────────

    /// <summary>TERMINATE this report (ISO §14.9.46.4). GR1: inactive → EC-REPORT-INACTIVE, the statement is
    /// unsuccessful. §14.9.49.4 GR10: inside a USE BEFORE REPORTING range → EC-FLOW-REPORT, unsuccessful, the
    /// state of the report unchanged (kb/Work PB326).
    /// GR2: with NO GENERATE since INITIATE, no report group is processed at all — the sole effect is
    /// active→inactive. GR3: otherwise the control items revert to their prior values (GR3a), each control
    /// footing prints minor→major as though a most-major break occurred (GR3b), the page footing of the last
    /// page prints (§13.18.57.4 GR6f — every page's last group; "immediately followed by the report footing"),
    /// the report footing prints (GR3c), and the control items are restored (GR3d). GR6: the file is NOT closed.</summary>
    public void Terminate(Func<int, bool>? beforeReporting = null)
    {
        var saved = _beforeReporting;   // see Generate
        _beforeReporting = beforeReporting;
        try { TerminateCore(); }
        finally { _beforeReporting = saved; }
    }

    private void TerminateCore()
    {
        // §14.9.49.4 GR10 — see Initiate: unsuccessful, report state unchanged, the raise gated by checking.
        if (RunUnit.Current.ReportFlow.InBeforeReporting)
        {
            ExceptionState.FlowReportError($"TERMINATE {Name}: executed within the range of a USE BEFORE "
                + "REPORTING declarative procedure (ISO §14.9.49.4 GR10)");
            return;
        }
        if (!_active)   // GR1 — "the execution of the statement is unsuccessful"
        {
            ExceptionState.ReportInactiveError($"TERMINATE {Name}: the report is not in the active state "
                + "(ISO §14.9.46.4 GR1)");
            return;
        }
        if (_started)           // GR2 — no GENERATE ⇒ no group processing of any kind
        {
            // §13.18.37.4 GR4a — an absolute NEXT GROUP whose integer-1 went into the save location "will have no
            // effect at all if a TERMINATE is next executed for the report": the save location is discarded and
            // LINE-COUNTER is what the group's own last line left it, so the control footings below neither take
            // the forced page advance nor the save-location placement.
            if (_nextGroupSave != 0)
            {
                _nextGroupSave = 0;
                LineCounter = _lineCounterBeforeSave;
            }
            if (_controls.Count > 0 && _controls[0].Prior is not null)
            {
                var current = new string[_controls.Count];
                for (int i = 0; i < _controls.Count; i++)
                {
                    current[i] = _controls[i].Get();
                    if (_controls[i].Prior is { } prior) _controls[i].Set(prior);   // GR3a
                }
                // GR3b — minor → major, "as though a control break has been sensed in the most major control data item", so the
                // most major footing is the one §13.18.37.4 GR1 lets apply its NEXT GROUP clause.
                for (int i = _controls.Count - 1; i >= 0; i--)
                    if (_controlFootings.TryGetValue(i, out var cf)) PresentBody(cf, applyNextGroup: i == 0);
                for (int i = 0; i < _controls.Count; i++) _controls[i].Set(current[i]);   // GR3d
            }
            // §13.18.57.4 GR6f: the page footing prints as the last report group on EACH page — including the
            // final page (exception GR6f 2: a last page occupied only by an RF on a page by itself — the RF's LINE
            // NEXT PAGE form, whose own page feed PresentHeadingFooting takes after this PF). When an RF not on a
            // page by itself follows, the PF is "immediately followed by" it.
            if (_pageFooting is not null) PresentPageFooting();
            if (_reportFooting is { } rf) PresentHeadingFooting(rf);   // GR3c
        }
        _active = false;   // GR6: the associated file stays open
        _started = false;
    }

    // ── SUPPRESS (ISO §14.9.45) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The SUPPRESS statement (ISO §14.9.45), executed within a USE BEFORE REPORTING declarative: inhibit
    /// the PRINTING of the CURRENT instance of the associated report group (GR1). The effect is one-shot — GR2
    /// limits it to the current instance — so the flag is consumed by the very next group presentation (the
    /// declarative that set it runs during that presentation's §14.9.49 GR8 hook, immediately before the group is
    /// produced). SUPPRESS inhibits only printing, page advance, NEXT GROUP and LINE-COUNTER changes (GR3 a–d); it
    /// does NOT inhibit sum-counter accumulation (§13.18.54.4 GR7) or the end-of-group sum reset (GR2).</summary>
    public void SuppressPrinting() => _suppressCurrent = true;

    /// <summary>Invoke a report group's USE BEFORE REPORTING declarative (ISO §14.9.49 Format 2 GR8, just before
    /// the group is produced) and report whether that declarative executed a SUPPRESS statement (§14.9.45). The
    /// suppression flag is consumed here every time (GR2 — current instance only). A true result tells the caller
    /// to skip the PRINTING half of this presentation (GR3 a–d); the group's remaining PROCESSING — notably the
    /// end-of-group sum reset (§13.18.54.4 GR2) — is NOT skipped.</summary>
    private bool RunBeforeReporting(ReportGroup group)
    {
        if (_beforeReporting is { } select)
        {
            // §14.9.49.4 GR10 — THE ONE PLACE the BEFORE REPORTING range is entered. Every presentation path
            // (PresentBody / PresentPageHeading / PresentPageFooting / PresentHeadingFooting) funnels through
            // this method, so the range cannot be half-tracked. try/finally: the declarative can throw a fatal
            // EC out of the hook. The range is entered around the SELECTION as well as the run: a selection that
            // finds no qualifying declarative executes no statement, so the wider bracket observes nothing.
            var flow = RunUnit.Current.ReportFlow;
            flow.Enter();
            try { select(group.Index); }
            finally { flow.Exit(); }
        }
        if (!_suppressCurrent) return false;
        _suppressCurrent = false;
        return true;
    }

    // ── Group presentation (ISO §13.18.35.4 / §13.18.57.4 / §14.9.16.4 GR6) ──────────────────────────────────

    /// <summary>Present a BODY group (detail / CH / CF — §13.18.57.3 SR15): the §13.18.35.4 GR4 page-fit test
    /// (skipped for the chronologically first body group since INITIATE), a failed fit's §14.9.16.4 GR6 page
    /// advance, then each line per GR5 (first line) / GR7 (subsequent lines).</summary>
    private void PresentBody(ReportGroup group, bool applyNextGroup = true)
    {
        bool suppressed = RunBeforeReporting(group);   // §14.9.49 GR8; true ⇒ a §14.9.45 SUPPRESS executed
        var lines = group.Lines;
        if (lines.Length == 0) return;     // a dummy group affects no counters (§8.4.3.15.4 GR5)

        // §13.18.41.4 GR2: every PRESENT WHEN condition is evaluated ONCE, before the processing of any LINE
        // clauses for the report group. An absent line is processed as though its entry were omitted (GR2b);
        // when EVERY line is absent the effect is as though the entire description were omitted — no fit test,
        // no printing, no counter movement, no sum reset (the dummy-group shape above).
        var (present, first) = BeginPresentation(lines);
        if (first < 0) return;

        // §14.9.45.4 GR3 a–d: a SUPPRESSed group inhibits only the PRINTING half — no page-fit/advance, no line
        // printing, no LINE-COUNTER movement, no NEXT GROUP, and none of the page/indicate state updates below.
        // Its end-of-group sum reset (§13.18.54.4 GR2) STILL runs: SUPPRESS does not inhibit the reset (only
        // PRESENT WHEN / OCCURS DEPENDING absence does, §13.18.54.4 GR10), so a suppressed control footing's
        // totals stay correct. The addends were already accumulated in Generate/Terminate (§13.18.54.4 GR7).
        if (suppressed) { EndOfGroupSumReset(group); return; }

        // The first line's position when the preceding body group's absolute NEXT GROUP filled the save location
        // (§13.18.37.4 GR4a 3) — the ordinary GR5 placement otherwise.
        long? firstTarget = null;
        if (_nextGroupSave != 0)
            firstTarget = PlaceAfterSavedNextGroup(lines, present, first, LowerLimit(group));
        else if (_paged && !_firstBodySinceInitiate)
        {
            // §13.18.35.4 GR4b (absolute): fit iff integer-1 > LINE-COUNTER. GR4c (relative): trial =
            // LINE-COUNTER + Σ integer-2 over the group's relative LINE clauses; fit iff trial ≤ the group's
            // lower limit (§13.18.57.4 GR8: detail → LAST DETAIL; CH → LAST CH; CF → FOOTING). Which LINE
            // clause is "first" — and which contribute — depends on the PRESENT WHEN values (§13.18.35.4
            // GR4/§13.18.41.4 GR3a/GR3d: absent lines are disregarded by the page fit test).
            // ⚠ The 2023 GR4c wording — "incremented by integer-2 for each *subsequent* LINE clause" — is
            // ambiguous about the FIRST relative line's integer-2; the NIST goldens and the legacy oracle
            // resolve it as the sum over ALL relative lines (RW103A overflows exactly at LINE-COUNTER 25 with
            // LAST DETAIL 25 and one PLUS 1 line: 25+1 > 25), and GR5b3 then IGNORES the first line's relative
            // value anyway (first body group on the new page lands at FIRST DETAIL). Encoded as Σ over all.
            bool fit;
            if (lines[first].NextPage)
                fit = false;   // GR4a — "no page fit test takes place and the page fit is declared unsuccessful"
            else if (lines[first].Kind == ReportLineKind.Absolute)
                fit = lines[first].Value > LineCounter;
            else
            {
                long trial = LineCounter;
                for (int i = 0; i < lines.Length; i++)
                    if (present is null || present[i])
                        trial += lines[i].TrialInterval;
                fit = trial <= LowerLimit(group);
            }
            if (!fit) AdvancePage();   // §13.18.35.4 GR4 tail → the §14.9.16.4 GR6 sequence
        }

        bool isFirst = true;
        for (int i = 0; i < lines.Length; i++)
        {
            if (present is not null && !present[i]) continue;   // §13.18.41.4 GR2b — the next relative line re-anchors on LINE-COUNTER
            long target;
            if (isFirst)
            {
                // §13.18.35.4 GR5a: absolute → integer-1. GR5b3 (paged, relative): the FIRST body group on the
                // page lands at FIRST DETAIL (the relative value is IGNORED); otherwise LINE-COUNTER + integer-2.
                // GR5c (unpaged, relative): LINE-COUNTER + integer-2. "First" = the first PRESENT line (GR5).
                target = firstTarget ?? (lines[i].Kind == ReportLineKind.Absolute ? lines[i].Value
                    : _paged && _firstBodyOnPage ? _firstDetail
                    : LineCounter + RelativeValue(lines[i]));
                isFirst = false;
            }
            else
                target = SubsequentTarget(lines[i]);
            PresentLine(target, lines[i], group);
        }
        _firstBodySinceInitiate = false;
        _firstBodyOnPage = false;
        if (group.Kind == ReportGroupKind.Detail) _indicateFresh = false;   // §13.18.29 — repeats now suppress
        if (applyNextGroup) ApplyNextGroup(group);   // §13.18.37.4 GR2 — after the group's last line is printed
        EndOfGroupSumReset(group);
    }

    /// <summary>⛔ ISO §13.18.37.4 GR4a — THE NEXT NON-DUMMY BODY GROUP AFTER A SAVED ABSOLUTE NEXT GROUP. The
    /// preceding body group's integer-1 was not below LINE-COUNTER, so it went into the save location and
    /// LINE-COUNTER was set to the FOOTING integer, "causing a page advance to take place just before any other
    /// non-dummy body group is printed for the report" — the advance is the GR's stated effect, so it is taken
    /// here unconditionally rather than re-derived from a page-fit test. Then:
    /// <list type="number">
    /// <item>a first LINE clause that is absolute: "the save location is moved to LINE-COUNTER and the page fit
    /// test is re-applied before the first line of the body group is printed" (GR4a 1; the returned null lets
    /// the ordinary absolute placement stand);</item>
    /// <item>a first LINE clause that is absolute WITH the NEXT PAGE phrase: "a page advance takes place, the save
    /// location is moved to LINE-COUNTER and a new page fit test and subsequent processing take place as for an
    /// identical report group without the NEXT PAGE phrase" (GR4a 2). ⚠ The advance GR4a 2 names is the one the
    /// save location already forces — not a second one: where §13.18.37.4 means a second advance it says so
    /// ("a second page advance takes place, resulting in a page devoid of body groups", GR4a 3), and a second
    /// one here would leave a page with no report group on it at all. So GR4a 2 IS GR4a 1, and the phrase has
    /// no further effect on this path (docs/CONFORMANCE.md, the LINE NEXT PAGE block);</item>
    /// <item>only relative LINE clauses: "its first line will be printed on the next line following the line
    /// number in the save location, unless this will result in some line of this body group being printed
    /// beyond its lower permitted limit. In the latter case, a second page advance takes place, resulting in a
    /// page devoid of body groups, and the next body group is printed on the following page with no reference to
    /// the save location" (GR4a 3).</item>
    /// </list>
    /// A dummy group (no lines, or every line absent under PRESENT WHEN) and a SUPPRESSed one never reach here,
    /// so they leave the save location for the next non-dummy group, as the GR requires.</summary>
    private long? PlaceAfterSavedNextGroup(ReportGroupLine[] lines, bool[]? present, int first, int lowerLimit)
    {
        long saved = _nextGroupSave;
        _nextGroupSave = 0;
        AdvancePage();
        if (lines[first].Kind == ReportLineKind.Absolute)
        {
            LineCounter = saved;                                             // GR4a 1 (and GR4a 2 — see above)
            if (lines[first].Value <= LineCounter) AdvancePage();            // the re-applied §13.18.35.4 GR4b test
            return null;
        }
        // GR4a 3 — the first line at saved + 1; every later present line adds what it adds to a GR4c trial sum.
        long last = saved + 1;
        for (int i = first + 1; i < lines.Length; i++)
            if (present is null || present[i]) last += lines[i].TrialInterval;
        if (last <= lowerLimit) return saved + 1;
        AdvancePage();                                                       // the page devoid of body groups
        return null;                                                         // FIRST DETAIL, no save reference
    }

    /// <summary>⛔ THE ONE PLACE A NEXT GROUP CLAUSE TAKES EFFECT (ISO §13.18.37.4), called after the group's
    /// last line is printed (GR2 — "modifies the value of the current report's LINE-COUNTER after the printing of
    /// the last line, if any, of the report group in whose description the clause appears"). Every presentation
    /// path that can carry the clause reaches it — body groups (GR4), the report heading (GR3) and the page
    /// footing (GR5); §13.18.37.3 SR4 keeps it out of a page heading and a report footing, which the binder
    /// enforces. A dummy group and a SUPPRESSed one return before this call (§8.4.3.15.4 GR5: neither affects
    /// LINE-COUNTER or PAGE-COUNTER; §14.9.45.4 GR3 names NEXT GROUP among what SUPPRESS inhibits).</summary>
    private void ApplyNextGroup(ReportGroup group)
    {
        if (group.NextGroup is not { } ng) return;
        switch (group.Kind)
        {
            case ReportGroupKind.ReportHeading:
                switch (ng.Kind)
                {
                    case ReportNextGroupKind.Absolute: LineCounter = ng.Value; break;    // GR3a
                    case ReportNextGroupKind.Relative: LineCounter += ng.Value; break;   // GR3b
                    default:
                        // GR3c — "the report heading is printed on the first page of the report as the only report
                        // group on that page and LINE-COUNTER is then set equal to zero"; §14.9.16.4 GR4a — "an
                        // advance is made to the next physical page, and PAGE-COUNTER is either incremented by 1
                        // or, if the report heading's NEXT GROUP clause has the WITH RESET phrase, set to 1". No
                        // page footing closes that page (§13.18.57.4 GR6f 1 — "on the first page, if it is
                        // occupied only by a report heading group") and the page heading follows through the
                        // ordinary GENERATE flow (GR4b).
                        _resetPageCounterAtAdvance = ng.Reset;
                        PageFeed();
                        break;
                }
                break;
            case ReportGroupKind.PageFooting:
                // GR5 — the clause "affects any report footing defined in the current report using only relative
                // LINE clauses": the footing is placed from LINE-COUNTER (§13.18.35.4 GR5b5), so moving it here IS
                // the effect. (SR5 forbids NEXT PAGE in a page footing.)
                if (ng.Kind == ReportNextGroupKind.Absolute) LineCounter = ng.Value;         // GR5a
                else if (ng.Kind == ReportNextGroupKind.Relative) LineCounter += ng.Value;   // GR5b
                break;
            case ReportGroupKind.ControlHeading or ReportGroupKind.Detail or ReportGroupKind.ControlFooting:
                switch (ng.Kind)
                {
                    case ReportNextGroupKind.Absolute:                                       // GR4a
                        if (LineCounter < ng.Value) LineCounter = ng.Value;
                        else
                        {
                            _nextGroupSave = ng.Value;
                            _lineCounterBeforeSave = LineCounter;
                            LineCounter = _footing;
                        }
                        break;
                    case ReportNextGroupKind.Relative:                                       // GR4b
                        // An unpaged report has no FOOTING integer to clamp against (§13.18.39.4 GR2a — one page of
                        // indefinite length), so the relative distance is simply added there.
                        LineCounter = !_paged || LineCounter + ng.Value < _footing ? LineCounter + ng.Value : _footing;
                        break;
                    default:                                                                 // GR4c
                        LineCounter = _footing;
                        if (ng.Reset) _resetPageCounterAtAdvance = true;                     // GR6
                        break;
                }
                break;
        }
    }

    /// <summary>⛔ THE ONE PLACEMENT RULE FOR A SUBSEQUENT LINE OF A REPORT GROUP (ISO §13.18.35.4 GR7 with
    /// §13.18.38.4 GR12c/GR12d). All four group presentations (body, page heading, page footing, report
    /// heading/footing) reach it, so the STEP arm cannot be live in one of them and missing in the others —
    /// the two-arm dispatch this repo keeps paying for was here as FOUR copies of
    /// <c>LineCounter + l.Value</c>.</summary>
    private long SubsequentTarget(ReportGroupLine l) => l.Kind switch
    {
        ReportLineKind.Absolute => l.Value,                                 // GR7a
        // GR12c/GR12d — integer-3 lines beneath the line this one occupies in the preceding occurrence. An
        // unseeded anchor means that occurrence was absent (GR2b), and RelativeValue then re-anchors here.
        ReportLineKind.Step when Anchor(l.Anchor) > 0 => Anchor(l.Anchor) + l.Value,
        _ => LineCounter + RelativeValue(l),                                // GR7b
    };

    /// <summary>The relative operand a line places by when it is measured from LINE-COUNTER: its own integer-2,
    /// or — for a <see cref="ReportLineKind.Step"/> line whose anchor was never seeded — the integer-2 written
    /// on the entry (see <see cref="ReportGroupLine.RelativeBase"/>).</summary>
    private static int RelativeValue(ReportGroupLine l) =>
        l.Kind == ReportLineKind.Step ? l.RelativeBase : l.Value;

    /// <summary>The step anchors of the presentation in progress (ISO §13.18.38.4 GR12), indexed by
    /// <see cref="ReportGroupLine.Anchor"/>; 0 = not yet seeded (a page line number is always ≥ 1). Cleared at
    /// the start of every group presentation, because each presentation re-places every line.</summary>
    private long[] _lineAnchors = [];

    private long Anchor(int id) => (uint)id < (uint)_lineAnchors.Length ? _lineAnchors[id] : 0;

    private void SeedAnchor(int id, long value)
    {
        if (id >= _lineAnchors.Length) Array.Resize(ref _lineAnchors, id + 1);
        _lineAnchors[id] = value;
    }

    /// <summary>Begin ONE group presentation: clear the per-presentation step anchors, then evaluate the lines'
    /// PRESENT WHEN conditions (ISO §13.18.41.4 GR2 — once per presentation, before any LINE processing).
    /// Returns a null flag array when every line is unconditional (the untouched fast path) and the index of the
    /// first present line (−1 = all absent).</summary>
    private (bool[]? Present, int First) BeginPresentation(ReportGroupLine[] lines)
    {
        Array.Clear(_lineAnchors);
        return EvaluatePresent(lines);
    }

    private static (bool[]? Present, int First) EvaluatePresent(ReportGroupLine[] lines)
    {
        bool[]? present = null;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].Present is not null)
            {
                present = new bool[lines.Length];
                for (int j = 0; j < lines.Length; j++) present[j] = lines[j].Present?.Invoke() ?? true;
                break;
            }
        if (present is null) return (null, lines.Length > 0 ? 0 : -1);
        return (present, Array.IndexOf(present, true));
    }

    /// <summary>The body group's LOWER LIMIT for the page-fit test (ISO §13.18.57.4 GR8d/e/f).</summary>
    private int LowerLimit(ReportGroup group) => group.Kind switch
    {
        ReportGroupKind.ControlHeading => _lastControlHeading,   // GR8d
        ReportGroupKind.ControlFooting => _footing,              // GR8f
        _ => _lastDetail,                                        // GR8e — detail
    };

    /// <summary>The §14.9.16.4 GR6 page advance, in the GR's order: (a) the page footing, (b) the physical
    /// advance to the next page, (c) CODE re-evaluation — the CODE clause is staged loud at bind, so this point
    /// is a cited no-op — (d) PAGE-COUNTER + 1, or 1 after a NEXT GROUP NEXT PAGE WITH RESET, (e) LINE-COUNTER ←
    /// 0, (f) the page heading.</summary>
    private void AdvancePage()
    {
        if (_pageFooting is not null) PresentPageFooting();                  // GR6a
        PageFeed();                                                          // GR6b–e
        if (_pageHeading is not null) PresentPageHeading();                  // GR6f
    }

    /// <summary>The PAGE FEED itself — §14.9.16.4 GR6 b) to e), shared by the body-group page advance above and
    /// by the report heading that stands on a page by itself (§14.9.16.4 GR4a / §13.18.37.4 GR3c), which takes
    /// the feed without a page footing or a page heading around it.</summary>
    private void PageFeed()
    {
        // ⛔ page: null. A REPORT file has NO LINAGE clause to supply one — ISO §13.4.5.2 Format 3 (report) is
        // the file description entry format for a file with a REPORT clause and its clause list carries no
        // linage-clause at all (only Format 1, sequential, does). The Report Writer owns this file's page model
        // through the RD PAGE clause instead (§13.16 / PAGE-COUNTER + LINE-COUNTER above), so there is nothing
        // for §13.18.34 GR6 to evaluate here (kb/Work PB673).
        CobolFile.WriteAdvancing(_fileName, "", -1, before: false, page: null);   // GR6b — form feed
        _physLine = 0;
        // GR6d — "If the page advance was preceded by the printing of a group whose description has a NEXT GROUP
        // clause with the NEXT PAGE and WITH RESET phrases, PAGE-COUNTER is set to 1; otherwise PAGE-COUNTER is
        // incremented by 1" (§13.18.37.4 GR6 — "immediately after the page feed caused by the next page advance").
        PageCounter = _resetPageCounterAtAdvance ? 1 : PageCounter + 1;
        _resetPageCounterAtAdvance = false;
        LineCounter = 0;                                                     // GR6e
        _firstBodyOnPage = true;
        _rhOnThisPage = false;
        _pfOnThisPage = false;
        _indicateFresh = true;                                               // §13.18.29 — a new page indicates
    }

    /// <summary>Present the page heading (placement ISO §13.18.35.4 GR5b2: absolute → integer-1; relative with no
    /// report heading on the page → HEADING + integer-2 − 1, with one → LINE-COUNTER + integer-2). "First" =
    /// the first PRESENT line (§13.18.41.4 GR2/GR5); an all-absent PH prints nothing (GR2b).</summary>
    private void PresentPageHeading()
    {
        var ph = _pageHeading!;
        if (RunBeforeReporting(ph)) return;   // §14.9.49 GR8; §14.9.45 SUPPRESS ⇒ inhibit this instance (no sum reset in a PH)
        var (present, first) = BeginPresentation(ph.Lines);
        if (first < 0) return;
        bool isFirst = true;
        for (int i = 0; i < ph.Lines.Length; i++)
        {
            if (present is not null && !present[i]) continue;               // §13.18.41.4 GR2b
            var l = ph.Lines[i];
            long target = !isFirst ? SubsequentTarget(l)                      // GR7
                : l.Kind == ReportLineKind.Absolute ? l.Value
                : _rhOnThisPage ? LineCounter + RelativeValue(l)              // GR5b2 second form
                : _heading + RelativeValue(l) - 1;                            // GR5b2 first form
            isFirst = false;
            PresentLine(target, l, ph);
        }
    }

    /// <summary>Present the page footing (placement ISO §13.18.35.4 GR5b4: absolute → integer-1; relative →
    /// FOOTING + integer-2). "First" = the first PRESENT line (§13.18.41.4 GR2/GR5); an all-absent PF prints
    /// nothing (GR2b).</summary>
    private void PresentPageFooting()
    {
        var pf = _pageFooting!;
        if (RunBeforeReporting(pf)) return;   // §14.9.49 GR8; §14.9.45 SUPPRESS ⇒ inhibit this instance (no sum reset in a PF)
        var (present, first) = BeginPresentation(pf.Lines);
        if (first < 0) return;
        bool isFirst = true;
        for (int i = 0; i < pf.Lines.Length; i++)
        {
            if (present is not null && !present[i]) continue;               // §13.18.41.4 GR2b
            var l = pf.Lines[i];
            long target = !isFirst ? SubsequentTarget(l)                      // GR7
                : l.Kind == ReportLineKind.Absolute ? l.Value
                : _footing + RelativeValue(l);                                // GR5b4
            isFirst = false;
            PresentLine(target, l, pf);
        }
        _pfOnThisPage = true;
        ApplyNextGroup(pf);   // §13.18.37.4 GR5
    }

    /// <summary>Present the report heading or report footing in flow (placement ISO §13.18.35.4 GR5b1 for RH —
    /// relative → HEADING + integer-2 − 1; GR5b5 for RF — relative → FOOTING + integer-2 unless a page footing
    /// printed on the same page, then LINE-COUNTER + integer-2; absolute → integer-1 for both). "First" = the
    /// first PRESENT line (§13.18.41.4 GR2/GR5); an all-absent group prints nothing (GR2b).
    /// <para>A report footing whose first present line carries the NEXT PAGE phrase is ON A PAGE BY ITSELF:
    /// "the first line is printed beginning on a new page" (§13.18.35.4 GR5a). The page feed is the bare
    /// §14.9.16.4 GR6 b)–e) one — no page footing after it (§13.18.57.4 GR6f 2, "on the last page, if it is
    /// occupied only by a report footing group") and no page heading before the footing (GR6b, "except when the
    /// report group about to be printed is a report footing on a page by itself"). Its first line is integer-1
    /// (GR5a); the bare <c>ON NEXT PAGE</c> form, which writes no integer, starts at the upper limit of a report
    /// footing on a page by itself, the HEADING integer (§13.18.57.4 GR7f — ⚠ a determination,
    /// docs/CONFORMANCE.md).</para></summary>
    private void PresentHeadingFooting(ReportGroup group)
    {
        if (RunBeforeReporting(group)) return;   // §14.9.49 GR8; §14.9.45 SUPPRESS ⇒ inhibit this instance (no sum reset in an RH/RF)
        var (present, first) = BeginPresentation(group.Lines);
        if (first < 0) return;
        bool ownPage = _paged && group.Kind == ReportGroupKind.ReportFooting && group.Lines[first].NextPage;
        if (ownPage) PageFeed();                                                                   // GR5a
        bool isFirst = true;
        for (int i = 0; i < group.Lines.Length; i++)
        {
            if (present is not null && !present[i]) continue;               // §13.18.41.4 GR2b
            var l = group.Lines[i];
            long target = !isFirst ? SubsequentTarget(l)                                           // GR7
                : l.Kind == ReportLineKind.Absolute ? l.Value
                : ownPage ? _heading                                                               // §13.18.57.4 GR7f
                : group.Kind == ReportGroupKind.ReportHeading ? _heading + RelativeValue(l) - 1    // GR5b1
                : _pfOnThisPage ? LineCounter + RelativeValue(l)                                   // GR5b5
                : _footing + RelativeValue(l);                                                     // GR5b5
            isFirst = false;
            PresentLine(target, l, group);
        }
        if (group.Kind == ReportGroupKind.ReportHeading) _rhOnThisPage = true;
        ApplyNextGroup(group);   // §13.18.37.4 GR3 (a report footing carries none — §13.18.37.3 SR4)
    }

    /// <summary>Present ONE report line: LINE-COUNTER is set to the computed line number FIRST (ISO §13.18.35.4
    /// GR6 — load-bearing: a <c>SOURCE IS LINE-COUNTER</c> item prints THIS line's number), then the line is
    /// composed (§13.18.53.4 GR3 — the implicit MOVEs execute when the line is printed) and physically written
    /// at the line's vertical position. The single method ordering makes the GR6-before-compose sequence
    /// impossible to reorder per group. On a page that has already printed a line, a target at/above the current
    /// physical line advances one line — the §13.18.35.4 GR3 overlap rule's EC-REPORT-LINE-OVERLAP seam (checking
    /// default-off, §18.16); on an empty page there is no printed line to overlap and line 1 is where the stream
    /// already is (see the travel computation below).</summary>
    private void PresentLine(long target, ReportGroupLine line, ReportGroup group)
    {
        if (target < 1) target = 1;
        // ISO §13.18.38.4 GR12c/GR12d: the datum a later occurrence of this same line steps from is the page
        // line THIS one landed on. A Step line re-anchors only when its own anchor was never seeded (the first
        // occurrence was absent, §13.18.41.4 GR2b) — otherwise its Value is the displacement FROM the seed, and
        // moving the seed would compound it.
        if (line.Anchor != 0 && (line.Kind != ReportLineKind.Step || Anchor(line.Anchor) == 0))
            SeedAnchor(line.Anchor, target - (line.Kind == ReportLineKind.Step ? line.Value : 0));
        LineCounter = target;                     // §13.18.35.4 GR6 — BEFORE the compose
        string image = line.Compose();            // §13.18.53.4 GR1/GR3 — evaluated at presentation time
        if (group.Kind == ReportGroupKind.Detail && !_indicateFresh && group.IndicateFields.Count > 0)
        {
            // GROUP INDICATE (ISO §13.18.29): on a repeated presentation the indicated items present as spaces.
            var chars = image.ToCharArray();
            foreach (var (col, width) in group.IndicateFields)
                for (int i = 0; i < width && col - 1 + i < chars.Length; i++)
                    chars[col - 1 + i] = ' ';
            image = new string(chars);
        }
        // ⛔ A PAGE'S LINE 1 IS WHERE THE PRINT STREAM ALREADY RESTS, NOT ONE ADVANCE BELOW IT (kb/Work PB484).
        // §13.18.35.4 GR6: "the report's LINE-COUNTER is set equal to that line number and the line is now
        // printed on the page at that vertical location" — line number n IS page line n, and GR7's "Any
        // unoccupied lines on the page result in a blank line" fixes the count of blanks above it. The stream
        // starts each page (INITIATE §14.9.21.4 GR1b, and the §14.9.16.4 GR6b form feed) positioned AT line 1
        // with nothing written there, so a record emitted with NO advance occupies line 1 and the travel to
        // line `target` is target − 1 while the page is still empty — target − _physLine only once a line has
        // been printed. `_physLine == 0` IS that empty page, not a line zero to advance off; reading it as one
        // put every report line of every report one line too low.
        int advance = (int)(target - (_physLine == 0 ? 1 : _physLine));
        if (advance < 1 && _physLine != 0) advance = 1;   // EC-REPORT-LINE-OVERLAP seam (§13.18.35.4 GR3)
        CobolFile.WriteAdvancing(_fileName, image, advance, before: false, page: null);   // no LINAGE on a report FD (§13.4.5.2 Format 3)
        // _physLine is where the DEVICE stands, so it moves only when the write did. A refused write — a line
        // holding a character with no byte image in the file coded character set ('91', Annex A.1 item 159; owner
        // decision kb/Work R47), or a medium the host reports full ('34') — "does not take place" (§14.9.51.4
        // GR15), advance included; leaving _physLine behind makes the NEXT line's travel cover the refused line's
        // slot, so every later line of the page still lands on its own LINE-COUNTER line (kb/Work PB690).
        if (CobolFile.Status(_fileName) is [ '0', _ ]) _physLine = (int)target;
    }

    /// <summary>Reset the SUM counters whose reset point is the END of <paramref name="group"/>'s processing
    /// (ISO §13.18.54.4 GR2): no RESET phrase → the group the counter prints in; RESET ON level → the control
    /// footing of that level.</summary>
    private void EndOfGroupSumReset(ReportGroup group)
    {
        foreach (var s in _sums)
        {
            // An entry absent under its PRESENT WHEN chain is neither printed nor reset for this instance of
            // the report group (ISO §13.18.41.4 GR3g / §13.18.54.4 GR10) — the print half falls out of the
            // compose (the printable face carries the same chain); the reset half is suppressed here.
            if (s.Present?.Invoke() == false) continue;
            bool reset = s.ResetLevel >= 0
                ? group.Kind == ReportGroupKind.ControlFooting && group.ControlLevel == s.ResetLevel
                : ReferenceEquals(s.PrintedIn, group);
            if (reset) s.Value = 0;
        }
    }

    // ── Line-composition helpers (used by the generated compose methods) ──────────────────────────────────────

    /// <summary>A fresh space-filled report line buffer of the report's width.</summary>
    public static char[] NewLine(int width)
    {
        var line = new char[width];
        for (int i = 0; i < width; i++) line[i] = ' ';
        return line;
    }

    /// <summary>Place a printable item's image at COLUMN (1-based, ISO §13.18.14) — the image is already
    /// width-exact (the §13.18.53.4 GR1 implicit-MOVE result), truncated only at the line-width edge.</summary>
    public static void Place(char[] line, int column, string image)
    {
        int start = column >= 1 ? column - 1 : 0;
        for (int i = 0; i < image.Length && start + i < line.Length; i++)
            line[start + i] = image[i];
    }
}
