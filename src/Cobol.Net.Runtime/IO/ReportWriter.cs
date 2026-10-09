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
public sealed class ReportGroupLine(ReportLineKind kind, int value, Func<string> compose, int present = -1,
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

    /// <summary>The line's slot in its group's PRESENCE SNAPSHOT (<see cref="ReportGroup.SetPresence"/>) — the
    /// line's PRESENT WHEN chain (ISO §13.18.41 Format 1) AND its enclosing OCCURS … DEPENDING counts (§13.18.38.4
    /// GR13), as the snapshot recorded them before any LINE clause was processed (§13.18.41.4 GR2); −1 =
    /// unconditional. An absent line is processed as though its entry were omitted (GR2b).</summary>
    public int PresentSlot { get; } = present;
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

    /// <summary>ISO §13.18.28.4 GR1 — the GROUP INDICATE condition of THIS detail group: true from an INITIATE
    /// (a), a page advance (b) or a control break (c) until the next GENERATE issued for this group has been
    /// processed. It is per group because the rule is per group — "true only on the first occasion that a
    /// GENERATE is issued for the current detail group after any of the following events" — so a GENERATE of
    /// another detail group neither reads nor consumes it (kb/Work PB1244). Only <see cref="CobolReport"/> writes
    /// it; a non-detail group never holds it, since §13.18.28.3 SR1 admits the clause only in a detail group.</summary>
    internal bool GroupIndicatePending { get; set; }

    /// <summary>⛔ THE ADDITIONS THIS GROUP'S PROCESSING PERFORMS — ROLLED TOTALS (ISO §13.18.54.4 GR7 a)/b), kb/Work
    /// PB1294). A SUM clause whose addend is data-name-1, an entry of THIS group, is added into its sum counter "when
    /// the report group description containing data-name-1 is processed" (a), or "during the processing of the current
    /// report group before any of the report group's lines are printed" (b) — one event, the start of the group's
    /// processing. The counter belongs to ANOTHER engine when the SUM entry is in a different report description
    /// (SR4 g)), so each addition carries its target engine; <see cref="CobolReport.AddRolled"/> registers it here and
    /// <see cref="CobolReport"/>'s group prologue performs it.</summary>
    internal List<RolledAddition> Rolled { get; } = [];

    /// <summary>One rolled addition: add <paramref name="Apply"/>'s result into counter <paramref name="SumId"/> of
    /// <paramref name="Target"/> when the group is processed — unless the addend entry is absent in this
    /// presentation's snapshot at <paramref name="PresentSlot"/> (§13.18.54.4 GR11; −1 = always present).</summary>
    internal readonly record struct RolledAddition(CobolReport Target, int SumId, int PresentSlot, Func<Int128, Int128> Apply);

    /// <summary>The group's NEXT GROUP clause (ISO §13.18.37; §13.15.3 SR6 — level 1 only), null when none.</summary>
    public ReportNextGroup? NextGroup { get; set; }

    /// <summary>A control heading written with the OR PAGE phrase (ISO §13.18.57.2): "The OR PAGE phrase causes the
    /// associated control heading to be printed in addition after each page advance, following any page heading"
    /// (§13.18.57.4 GR6 c)). Read by <see cref="CobolReport"/>'s page advance only; false for every other group.</summary>
    public bool OrPage { get; set; }

    /// <summary>⛔ ONE PRESENCE SNAPSHOT PER GROUP PRESENTATION (kb/Work PB1272). ISO §13.18.41.4 GR2: "condition-1
    /// of each PRESENT WHEN clause is evaluated before the processing of any LINE clauses for the report group",
    /// and §13.18.38.4 GR13 evaluates an OCCURS … DEPENDING data-name-1 "just before the processing for the first
    /// LINE clause of the report group". Every such test of the group — a line's, a printable item's, a SUM
    /// entry's — is therefore ONE answer per presentation, recorded here by the generated probe before the page-fit
    /// test and read by the placement, the compose and the sum reset alike. Only the LINE tests used to be
    /// snapshotted: a printable item's chain ran inside its compose, AFTER the page advance its own group's fit
    /// test had caused, so a page heading's USE BEFORE REPORTING declarative could change what the item's already-
    /// evaluated condition said; and a SUM entry's chain ran again at the end of the group, so the counter could
    /// be printed yet not reset, or reset yet not printed — two answers where GR3 g) has one.</summary>
    private Action<bool[]>? _presenceProbe;
    private bool[] _presence = [];

    /// <summary>Install the group's generated presence probe and its slot count (see <see cref="_presenceProbe"/>).</summary>
    public void SetPresence(int slots, Action<bool[]> probe)
    {
        _presence = new bool[slots];
        _presenceProbe = probe;
    }

    /// <summary>Take this presentation's snapshot (ISO §13.18.41.4 GR2 / §13.18.38.4 GR13).</summary>
    internal void SnapshotPresence() => _presenceProbe?.Invoke(_presence);

    /// <summary>Slot <paramref name="slot"/> of the current snapshot; −1 is the unconditional slot.</summary>
    internal bool IsPresent(int slot) => slot < 0 || _presence[slot];
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
    string name, string fileName, int lineWidth, int pageWidth, bool paged,
    int pageLimit, int heading, int firstDetail, int lastControlHeading, int lastDetail, int footing)
{
    /// <summary>The report-name (the RD entry's name).</summary>
    public string Name { get; } = name;

    private readonly string _fileName = fileName;   // the emit-qualified connector name ("PROG::FILE")
    private readonly int _lineWidth = lineWidth;

    /// <summary>The page width (ISO §13.18.39.4 GR2b/GR5 — the PAGE clause's integer-2, else 999), supplied by the
    /// binder: THE width §13.18.14.4 GR5 measures a printable item against, in one place.</summary>
    private readonly int _pageWidth = pageWidth;
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
    /// <summary>The <see cref="ReportGroup.Index"/> of the group whose USE BEFORE REPORTING declarative is running
    /// NOW (−1 = none) — the only group a §14.9.45 SUPPRESS can still inhibit (see <see cref="SuppressPrinting"/>).</summary>
    private int _hookGroup = -1;

    /// <summary>A SUPPRESS naming <see cref="_hookGroup"/> executed during that group's own hook (§14.9.45.4 GR1/GR2).</summary>
    private bool _hookSuppressed;
    private int _physLine;                 // physical line position on the current page (0 = top, nothing printed)

    // ── The CODE clause (ISO §13.18.12) ──────────────────────────────────────────────────────────────────────
    private Func<string>? _codeRead;       // literal-1 (a constant) or identifier-1 (read at each evaluation); null = no CODE clause
    private string _code = "";             // the characters in force — "used until the next evaluation" (GR3)

    /// <summary>Register the report's CODE clause (§13.18.12.2): <paramref name="read"/> yields literal-1, or the
    /// character image of identifier-1 each time it is called. The engine calls it at the points GR3 names
    /// (<see cref="EvaluateCode"/>); until the first evaluation the code is empty.</summary>
    public void SetCode(Func<string> read) => _codeRead = read;

    /// <summary>⛔ THE ONE EVALUATION OF THE CODE CLAUSE (§13.18.12.4 GR3 — "If identifier-1 is specified, it is evaluated
    /// at the start of the processing for each body group, either during page advance processing, as detailed in the
    /// GENERATE statement, or whenever page advance processing is not performed. The resultant value is used until the
    /// next evaluation."). It is called from exactly two places: <see cref="AdvancePage"/> at §14.9.16.4 GR6 c), and
    /// <see cref="PresentBody"/> for a body group whose presentation took NO page advance — plus the first GENERATE,
    /// whose report heading and page heading precede its first body group. A literal re-reads as the same constant.</summary>
    private void EvaluateCode() => _code = _codeRead?.Invoke() ?? "";

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

    /// <summary>Every group of the report, indexed by <see cref="ReportGroup.Index"/> (report-description order).</summary>
    private readonly List<ReportGroup> _groups = [];

    /// <summary>Slot <paramref name="slot"/> of group <paramref name="group"/>'s current presence snapshot — what a
    /// generated compose asks for a conditioned printable item (ISO §13.18.41.4 GR2; kb/Work PB1272). The compose
    /// runs only inside that group's presentation, after <see cref="BeginPresentation"/> took the snapshot.</summary>
    public bool IsPresent(int group, int slot) => _groups[group].IsPresent(slot);

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
    /// delegates over the program's typed storage. The character image is what §13.18.16.4 GR3's prior control
    /// SAVES and RESTORES (representation-faithful for every category); whether two images are EQUAL is the
    /// program's own comparison, <see cref="Equal"/>.</summary>
    private sealed class ControlEntry(bool isFinal, Func<string> get, Action<string> set, Func<string, string, bool>? equal)
    {
        public bool IsFinal { get; } = isFinal;
        public Func<string> Get { get; } = get;
        public Action<string> Set { get; } = set;

        /// <summary>⛔ THE BREAK TEST IS THE PROGRAM'S RELATION CONDITION, NOT AN ORDINAL IMAGE COMPARE (kb/Work
        /// PB1131). §13.18.16.4 GR3 tests each control data item "for equality with the corresponding prior
        /// control", and §12.3.6.4 GR11 c) names that comparison — "Implicitly specified by the presence of a
        /// CONTROL clause in a report description entry" — among those whose truth value the alphanumeric and
        /// national PROGRAM COLLATING SEQUENCES determine. The compiler supplies the comparison the item's category
        /// takes under the program's sequence (the collated <c>CobolString.Compare</c> a relation condition over
        /// the same item renders); null where that comparison IS code-unit equality of the two equal-length
        /// images — no program collating sequence in effect, or a numeric / boolean item (§8.8.4.2.4 / §8.8.4.2.8
        /// collate neither), whose image is a function of its value.</summary>
        public Func<string, string, bool>? Equal { get; } = equal;

        public string? Prior { get; set; }   // saved at the first GENERATE (§13.18.16.4 GR3); null until then

        /// <summary>§13.18.16.4 GR3 — has this control data item changed from its prior control?</summary>
        public bool ChangedFrom(string prior)
        {
            string current = Get();
            return !(Equal?.Invoke(current, prior) ?? string.Equals(current, prior, StringComparison.Ordinal));
        }
    }

    private readonly List<ControlEntry> _controls = [];   // major→minor (FINAL, if present, is index 0 — GR2)

    // ── The floating-point prior-control channel (ISO §13.18.16.4 GR3/GR4; kb/Work PB1234) ────────────────────
    // GR3 defines a prior control "having the same data description as the corresponding data item", and GR4 a)
    // stores the priors INTO the control items before the control footings print and restores the new current
    // values afterwards — a same-usage copy each way, which §14.9.25.4 GR6 c) makes a transfer "without change".
    // A CONTROL entry saves its item as a string key, so a floating-point item's key is its BIT PATTERN: the
    // character (DISPLAY) image is a rounded rendering, and restoring through it would hand the program back a
    // value an ulp away from the one it held. The break test compares VALUES (−0 equals +0, as the program's own
    // relation condition answers) and treats an identical bit pattern as equal (a NaN control item that has not
    // changed has not broken).

    /// <summary>The prior-control key of a binary32 control item: its exact bit pattern (8 hex digits).</summary>
    public static string FloatControlKey(float value) => BitConverter.SingleToInt32Bits(value).ToString("X8");

    /// <summary>The prior-control key of a binary64 control item: its exact bit pattern (16 hex digits).</summary>
    public static string FloatControlKey(double value) => BitConverter.DoubleToInt64Bits(value).ToString("X16");

    /// <summary>The binary32 value a <see cref="FloatControlKey(float)"/> key holds — the restore half.</summary>
    public static float FloatControlSingle(string key) =>
        BitConverter.Int32BitsToSingle(int.Parse(key, System.Globalization.NumberStyles.HexNumber));

    /// <summary>The binary64 value a <see cref="FloatControlKey(double)"/> key holds — the restore half.</summary>
    public static double FloatControlDouble(string key) =>
        BitConverter.Int64BitsToDouble(long.Parse(key, System.Globalization.NumberStyles.HexNumber));

    /// <summary>§13.18.16.4 GR3's "equality with the corresponding prior control" for two keys of ONE floating-
    /// point control item (both from the same <c>FloatControlKey</c> overload, so of one width): equal bit
    /// patterns, or equal values.</summary>
    public static bool FloatControlEqual(string a, string b) =>
        a == b || (a.Length == 8 ? FloatControlSingle(a) == FloatControlSingle(b) : FloatControlDouble(a) == FloatControlDouble(b));

    /// <summary>ONE <c>SUM … [UPON …]</c> GROUP of a SUM clause (ISO §13.18.54.3 SR1 — the SUM keyword may appear
    /// more than once, and §13.18.54.4 GR1 still gives the ENTRY one counter): the group's ONE ADDITION as the
    /// counter's new content — <paramref name="Apply"/> takes the counter's current content (unscaled, at its own
    /// scale) and returns its content after adding the group's addends "consistent with the general rules of the
    /// ADD statement" (GR3; GR9 sums a group's addends together), evaluated by the SAME arithmetic renderer and
    /// store funnel an ADD statement uses (kb/Work PB1686) — and the group's OWN UPON filter — GR7 c) 2)
    /// accumulates "whenever any GENERATE statement is executed for a detail referenced by the UPON phrase", and
    /// the phrase belongs to its group. Null = no UPON phrase (GR7 c) 1) — every GENERATE for this report).</summary>
    private readonly record struct SumTerm(Func<Int128, Int128> Apply, string[]? UponDetails)
    {
        /// <summary>HOW MANY TIMES this term accumulates on a GENERATE of <paramref name="detailName"/> (GR7 c)):
        /// once with no UPON phrase (GR7 c) 1.), else once per appearance of the detail in the phrase — GR7: "It is
        /// permissible for the UPON phrase to contain two or more instances of data-name-2 that specify the same
        /// detail. When a GENERATE statement for such a detail is executed, the adding takes place as many times as
        /// data-name-2 appears in the UPON phrase" (kb/Work PB1297). A membership test answered `UPON DET DET` with
        /// one addition.</summary>
        public int Fires(string? detailName)
        {
            if (UponDetails is null) return 1;
            if (detailName is null) return 0;
            int n = 0;
            foreach (var d in UponDetails)
                if (d.Equals(detailName, StringComparison.OrdinalIgnoreCase)) n++;
            return n;
        }
    }

    /// <summary>One SUM counter (ISO §13.18.54): an unscaled integer accumulation at the counter's scale (GR1 —
    /// digits derived from the entry's PICTURE), its SIZE ERROR INDICATOR, the clause's <see cref="Terms"/> over
    /// the program's typed storage, the RESET control level (GR2; −1 = reset where printed), and the group it
    /// prints in.</summary>
    private sealed class SumEntry(int resetLevel, ReportGroup printedIn, int presentSlot)
    {
        /// <summary>The counter's content, unscaled at its own scale. ⛔ THE CARRIER IS <see cref="Int128"/>, THE
        /// WIDEST NATIVE FIXED-POINT CARRIER THE COMPILER USES (the 19–38-digit tier of <c>PicInfo.ClrType</c>),
        /// NOT A <c>long</c> (kb/Work PB1509/PB1560/PB1666). GR1 derives the counter's digit count from the
        /// entry's PICTURE, and a numeric PICTURE may describe up to 31 digit positions (§13.18.40.3 SR14), so
        /// a <c>long</c> — 18 digits — wrapped a 20-digit total modulo 2^64 or reported a false size error.</summary>
        public Int128 Value;

        /// <summary>⛔ THE COUNTER'S CHARACTER CELL, held only while it is not its value's own image (kb/Work PB2553).
        /// The counter is a signed USAGE DISPLAY item (docs/CONFORMANCE.md A.4.11), so a procedure-division store of
        /// characters — through a reference modifier (§8.4.3.3.4 GR5: "a subset of the data item referenced by
        /// identifier-1"), or a pre-2023 figurative fill — puts THOSE characters in it, a non-digit included, exactly
        /// as it would in a stored item. <see cref="Value"/> alone cannot hold them, so they are kept here, with
        /// <see cref="Value"/> the image's lenient decode, until a numeric store replaces the content. A reference to
        /// the content while they are not a valid numeric image is §14.6.13.2 rule 2's incompatible data (<see cref="Current"/>).</summary>
        public string? Image;

        /// <summary>The counter's GR1 profile <see cref="Image"/> was stored under (the decode it is read back with).</summary>
        public NumProfile ImageProfile;

        /// <summary>⛔ ISO §13.18.54.4 GR1 — "Each entry containing a SUM clause establishes an independent sum
        /// counter AND SIZE ERROR INDICATOR" (kb/Work PB1130). Set by an addition that is a size error (GR3);
        /// unset with the counter's reset — at INITIATE (§14.9.21.4 GR1 a)) and at the end of the group that
        /// prints it or of its RESET level's control footing (GR2). While it is set the printable item is filled
        /// with spaces (GR4).</summary>
        public bool SizeError;

        /// <summary>ISO §13.18.54.4 GR3 — one addition "consistent with the general rules of the ADD statement with
        /// the ON SIZE ERROR phrase": <paramref name="apply"/> is the compiler's own ADD evaluation of the group's
        /// addends into THIS counter (the counter's content, then the addends, summed at the wider of their scales
        /// and stored ONCE at the counter's scale with its ROUNDED mode and its GR1 capacity — the digit count of
        /// the entry's PICTURE, enforced by the store through the counter's profile, kb/Work PB1130/PB1686). When
        /// that evaluation or store is a size error — a sum past the capacity, an intermediate past the Int128
        /// carrier, a PROHIBITED-inexact transfer — the counter is left unchanged (§14.7.5 1) — with the phrase,
        /// "the values of all of the resultant data items remain unchanged from the values they had at the start
        /// of the execution of the arithmetic statement") and the size error indicator is set. Returns false on a
        /// size error.</summary>
        public bool Add(Func<Int128, Int128> apply)
        {
            Int128 next;
            try { next = apply(Current()); }
            catch (Exception e) when (e is CobolSizeError or OverflowException) { SizeError = true; return false; }
            Value = next;
            Image = null;
            return true;
        }

        /// <summary>The counter's content as the addition's first operand (GR3: "consistent with the general rules of
        /// the ADD statement"): its value, or — when a store left characters that are not a numeric image — their
        /// decode through the ONE checked sending read, which sets EC-DATA-INCOMPATIBLE (§14.6.13.2 rule 2: "the
        /// content of a numeric sending item … would evaluate to false in a numeric class condition").</summary>
        private Int128 Current() => Image is null ? Value : CobolNum.ParseImageSending(Image, ImageProfile);

        /// <summary>The counter's character image under its GR1 <paramref name="profile"/>: the characters a store left
        /// (<see cref="Image"/>), else its value's DISPLAY image.</summary>
        public string ImageOf(in NumProfile profile) => Image ?? CobolNum.FormatImage(Value, profile);

        /// <summary>A procedure-division store of the counter's character image (GR12): a valid numeric image becomes the
        /// counter's value; the characters are kept as they were stored (<see cref="Image"/>) unless they ARE the value's
        /// own image, so a valid but non-canonical image (a plain digit in the sign position, a negative zero) reads
        /// back character for character (§8.4.3.3.4 GR5; train 1045 review).</summary>
        public void Store(string image, in NumProfile profile)
        {
            Value = CobolNum.ParseImage(image, profile);
            Image = image == CobolNum.FormatImage(Value, profile) ? null : image;
            ImageProfile = profile;
        }

        /// <summary>GR2 / §14.9.21.4 GR1 a) — the counter set to zero and its size error indicator unset.</summary>
        public void Reset()
        {
            Value = 0;
            Image = null;
            SizeError = false;
        }

        /// <summary>The clause's <c>SUM … [UPON …]</c> groups in written order, each with its own UPON filter.</summary>
        public List<SumTerm> Terms { get; } = [];
        public int ResetLevel { get; } = resetLevel;
        public ReportGroup PrintedIn { get; } = printedIn;

        /// <summary>The SUM entry's slot in <see cref="PrintedIn"/>'s presence snapshot — its full PRESENT WHEN
        /// chain (ISO §13.18.41.4 GR3 g) / §13.18.54.4 GR10); −1 = unconditional. The printable face reads its own
        /// slot of the SAME snapshot, so the entry is printed exactly when it is reset (kb/Work PB1272).</summary>
        public int PresentSlot { get; } = presentSlot;
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
        g.Index = _groups.Count;
        _groups.Add(g);
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

    /// <summary>Register one CONTROL operand, major→minor order (ISO §13.18.16.4 GR1/GR2; FINAL first).
    /// <paramref name="equal"/> is the program's equality comparison for the item (§12.3.6.4 GR11 c); see
    /// <see cref="ControlEntry.Equal"/>), null when that comparison is code-unit equality.</summary>
    public void AddControl(bool isFinal, Func<string> get, Action<string> set, Func<string, string, bool>? equal = null) =>
        _controls.Add(new ControlEntry(isFinal, get, set, equal));

    /// <summary>Register a SUM counter (ISO §13.18.54.4 GR1 — one per ENTRY containing a SUM clause).
    /// <paramref name="resetLevel"/> is the RESET control level (GR2; −1 = reset at the end of the group it
    /// prints in); <paramref name="presentSlot"/> is the entry's slot in <paramref name="printedIn"/>'s presence
    /// snapshot (§13.18.41.4 GR3 g) — absent suppresses the end-of-group reset; −1 = unconditional). The counter's
    /// capacity (GR1's digit count of the entry's PICTURE, past which an addition is the GR3 size error, kb/Work
    /// PB1130) is enforced by the store each term's addition ends in, through the counter's own profile.
    /// The addends arrive through <see cref="AddSumTerm"/>, one call per <c>SUM … [UPON …]</c> group.
    /// <para><paramref name="id"/> is the ENTRY OCCURRENCE's id within its report description (GR1 — the counter's
    /// identity is the entry, never its data-name, kb/Work PB882; a repeating entry has one counter per occurrence
    /// in one contiguous block, kb/Work PB1271 — see <see cref="SumOccurrence"/>). The compiler emits the registrations in
    /// ordinal order, so the call APPENDS; a gap would mean the emitter and the model disagree about which
    /// entry a counter belongs to, which is exactly the confusion this keying exists to prevent.</para></summary>
    public void AddSum(int id, int resetLevel, ReportGroup printedIn, int presentSlot = -1)
    {
        if (id != _sums.Count)
            throw new InvalidOperationException(
                $"report '{Name}': sum counter {id} registered out of order (expected {_sums.Count}) — the "
                + "counter's identity is its entry's ordinal (ISO §13.18.54.4 GR1)");
        _sums.Add(new SumEntry(resetLevel, printedIn, presentSlot));
    }

    /// <summary>Register one <c>SUM … [UPON …]</c> group of the counter <paramref name="id"/> (ISO §13.18.54.3
    /// SR1 — "the SUM keyword may appear more than once"). <paramref name="apply"/> is that group's ONE addition —
    /// the counter's current content in, its content after the ADD-consistent addition of the group's addends out
    /// (GR3, GR9); <paramref name="uponDetails"/> restricts its accumulation to the named details (GR7 c) 2); null =
    /// every GENERATE for this report, GR7 c) 1).</summary>
    public void AddSumTerm(int id, Func<Int128, Int128> apply, string[]? uponDetails) =>
        _sums[id].Terms.Add(new SumTerm(apply, uponDetails));

    /// <summary>⛔ A ROLLED TOTAL (ISO §13.18.54.3 SR4, §13.18.54.4 GR6/GR7 a)/b), kb/Work PB1294): register, on the
    /// report <paramref name="source"/> group that contains data-name-1, the addition of its value into counter
    /// <paramref name="sumId"/> of THIS report — "adding takes place when the report group description containing
    /// data-name-1 is processed" (a), "during the processing of the current report group before any of the report
    /// group's lines are printed" (b). <paramref name="apply"/> is the ADD-consistent addition of the addend's CURRENT
    /// value into the counter's content (GR3), exactly as <see cref="AddSumTerm"/>'s is; <paramref name="presentSlot"/>
    /// is the addend entry's slot in <paramref name="source"/>'s presence snapshot — an entry "declared to be absent as
    /// a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase … is not added into the sum
    /// counter during the processing of that instance of the report group" (GR11); −1 = unconditional. The source group
    /// may belong to ANOTHER report's engine (SR4 g)): the group carries its target.</summary>
    public void AddRolled(ReportGroup source, int sumId, int presentSlot, Func<Int128, Int128> apply) =>
        source.Rolled.Add(new ReportGroup.RolledAddition(this, sumId, presentSlot, apply));

    /// <summary>One SUM group's UPON on a detail of ANOTHER report (ISO §13.18.54.4 GR7 c) 2) — "whenever any GENERATE
    /// statement is executed for a detail referenced by the UPON phrase", the detail being of a different report
    /// description, SR7 permits only the report-name qualifier to say so): register, on THIS report's engine, whose
    /// GENERATE of <paramref name="detailName"/> is the event, the addition into counter <paramref name="sumId"/> of
    /// <paramref name="target"/>. A detail named n times in the phrase registers n times — "the adding takes place as
    /// many times as data-name-2 appears" (GR7).</summary>
    public void AddGenerateTrigger(string detailName, CobolReport target, int sumId, Func<Int128, Int128> apply) =>
        _generateTriggers.Add((detailName, target, sumId, apply));

    private readonly List<(string Detail, CobolReport Target, int SumId, Func<Int128, Int128> Apply)> _generateTriggers = [];

    /// <summary>The ONE ADDITION INTO A SUM COUNTER THAT IS NOT THIS REPORT'S OWN GENERATE LOOP (§13.18.54.4 GR3 — "Each
    /// addition is tested for size error; if a size error occurs, the EC-REPORT-SUM-SIZE exception condition is set to
    /// exist"): the same <see cref="SumEntry.Add"/> the GENERATE accumulation calls, so a rolled addition and a
    /// GENERATE-driven one fail identically.</summary>
    private void Accumulate(int sumId, Func<Int128, Int128> apply)
    {
        if (!_sums[sumId].Add(apply))
            ExceptionState.ReportSumSizeError($"report {Name}: an addition into sum counter {sumId + 1} is a size "
                + "error (ISO §13.18.54.4 GR3)");
    }

    /// <summary>Perform the additions <paramref name="group"/> owes when it is processed — its rolled totals — in the
    /// order they were registered (the binder orders a chain of rolled totals so a counter's own additions precede
    /// its being read, §13.18.54.4 GR6: "The additions necessary to compute its value are completed before the adding
    /// of the operand into the current sum counter"). An entry absent in THIS presentation's snapshot adds nothing.</summary>
    private static void ApplyRolled(ReportGroup group)
    {
        foreach (var r in group.Rolled)
            if (group.IsPresent(r.PresentSlot))
                r.Target.Accumulate(r.SumId, r.Apply);
    }

    /// <summary>A SUM counter's content as its CHARACTER IMAGE under its GR1 <paramref name="profile"/> — the counter is
    /// a signed USAGE DISPLAY item carried as its image (docs/CONFORMANCE.md A.4.11; kb/Work PB2553), read by the
    /// generated compose of the printable item the counter is the source of (ISO §13.18.54.4 GR4), by a rolled total,
    /// and by a procedure division statement that names the counter (GR5 + GR12), each through the ordinary
    /// image-carried numeric read (§14.6.13.2 rule 2's checked decode included).</summary>
    public string SumImage(int id, in NumProfile profile) => _sums[id].ImageOf(profile);

    /// <summary>The image of one OCCURRENCE of a repeating SUM entry's counter, selected by a procedure division
    /// subscript (kb/Work PB1271 — see <see cref="SumOccurrence"/>). An out-of-range subscript reads zero once the
    /// EC-BOUND-SUBSCRIPT condition has been raised, the counter twin of an ordinary table's scratch occurrence
    /// (<c>CobolTable.At</c>).</summary>
    public string SumImage(int baseId, ReadOnlySpan<int> extents, ReadOnlySpan<long> subscripts, in NumProfile profile) =>
        SumOccurrence(baseId, extents, subscripts) is int id and >= 0
            ? _sums[id].ImageOf(profile)
            : CobolNum.FormatImage(Int128.Zero, profile);

    /// <summary>Alter a SUM counter's content from the procedure division (ISO §13.18.54.4 GR12 — "It is
    /// permissible for procedure division statements to alter the content of sum counters"): the image the writing
    /// statement stored through the counter's GR1 profile, kept as stored (<see cref="SumEntry.Store"/>).</summary>
    public void SetSumImage(int id, string image, in NumProfile profile) => _sums[id].Store(image, profile);

    /// <summary>Alter one OCCURRENCE of a repeating SUM entry's counter (GR12 over <see cref="SumOccurrence"/>); a
    /// store through an out-of-range subscript is discarded once EC-BOUND-SUBSCRIPT has been raised.</summary>
    public void SetSumImage(int baseId, ReadOnlySpan<int> extents, ReadOnlySpan<long> subscripts, string image,
                            in NumProfile profile)
    {
        if (SumOccurrence(baseId, extents, subscripts) is int id and >= 0) _sums[id].Store(image, profile);
    }

    /// <summary>The repetition count of a report writer OCCURS … DEPENDING entry (ISO §13.18.38.4 GR13): "If the
    /// value of data-name-1 is not in the range integer-1 to (integer-2 - 1), the report group is processed as though
    /// the OCCURS clause had been written without the TO and DEPENDING phrases. If the value of data-name-1 is in the
    /// range integer-1 to (integer-2 - 1), the OCCURS clause has the same effect as an OCCURS clause with no TO or
    /// DEPENDING phrases and with an integer-2 equal to the current value of data-name-1." The ONE evaluation of that
    /// sentence: the per-repetition presence test and a table(ALL) argument over a repeating sum counter both call
    /// it (kb/Work PB1271).</summary>
    public static int DependingCount(int value, int minOccurs, int maxOccurs) =>
        value >= minOccurs && value <= maxOccurs - 1 ? value : maxOccurs;

    /// <summary>⛔ A REPEATING ENTRY'S SUM COUNTER IS A TABLE (kb/Work PB1271). ISO §13.18.54.4 GR8 a) adds each
    /// occurrence of a repeating addend "into the corresponding occurrence of the sum counter", so an entry that is
    /// subject to an OCCURS clause, a multiple LINE clause or a multiple COLUMN clause (§13.18.38 Format 3;
    /// §13.18.35.4 GR9 and §13.18.14.4 GR12 make the latter two simple OCCURS levels) has one counter per
    /// occurrence. The compiler registers a family's counters as one contiguous block, row-major over
    /// <paramref name="extents"/> (outermost level first), starting at <paramref name="baseId"/>; this answers the
    /// id of the occurrence the one-based <paramref name="subscripts"/> select, or −1 after raising
    /// EC-BOUND-SUBSCRIPT for a subscript outside 1..its level's extent (§8.4.2.3.4 GR2 — "If the value of the
    /// subscript is not a positive integer or is less than one or is greater than the highest permissible
    /// occurrence number, the EC-BOUND-SUBSCRIPT exception condition is set to exist"). Each level is tested on its
    /// own: an out-of-range inner subscript must not wrap into the next outer occurrence.</summary>
    private int SumOccurrence(int baseId, ReadOnlySpan<int> extents, ReadOnlySpan<long> subscripts)
    {
        int linear = 0;
        for (int k = 0; k < extents.Length; k++)
        {
            long s = subscripts[k];
            if (s < 1 || s > extents[k])
            {
                ExceptionState.SubscriptError($"report {Name}: sum counter subscript {s} is outside 1..{extents[k]} "
                    + "(ISO 8.4.2.3.4 GR2)");
                return -1;
            }
            linear = linear * extents[k] + (int)(s - 1);
        }
        return baseId + linear;
    }

    /// <summary>ISO §13.18.54.4 GR4 — may sum counter <paramref name="id"/> be moved to its printable item? True
    /// while its size error indicator is unset ("the content of the sum counter is moved, according to the general
    /// rules of the MOVE statement, to the printable item"). When it IS set, "an EC-REPORT-SUM-SIZE exception
    /// condition is set to exist and the printable item is filled with spaces": the raise happens here (>>TURN-
    /// gated, fatal), and the false answer makes the generated compose place spaces instead of the counter — an
    /// outcome GR4 states unconditionally, checking or not (kb/Work PB1130).</summary>
    public bool SumPresentable(int id)
    {
        if (!_sums[id].SizeError) return true;
        ExceptionState.ReportSumSizeError($"report {Name}: sum counter {id + 1} is presented with its size error "
            + "indicator set (ISO §13.18.54.4 GR4)");
        return false;
    }

    // ── VARYING (ISO §13.18.64.4) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>A VARYING FROM or BY value (ISO §13.18.64.4 GR3 a)/b)) as the INTEGER GR1 makes the counter —
    /// "an independent temporary integer data item that shall be large enough to contain the maximum expected
    /// value", so the carrier is <see cref="Int128"/>, the widest native fixed-point carrier (kb/Work PB1305). A
    /// fixed-point value arrives UNSCALED at <paramref name="scale"/>. When it has a nonzero fraction, GR5 applies:
    /// "the EC-REPORT-VARYING exception condition is set to exist, the execution of the INITIATE, GENERATE, or
    /// TERMINATE statement is unsuccessful, and the content of the print line is undefined" — raised here (fatal,
    /// >>TURN-gated); with checking off the value's integer part is returned, one of the contents GR5 leaves
    /// undefined.</summary>
    public static Int128 VaryingInteger(Int128 unscaled, int scale, string detail)
    {
        if (scale <= 0) return unscaled * Pow10.AsWide(-scale);
        Int128 unit = Pow10.AsWide(scale);
        if (unscaled % unit != 0) ExceptionState.ReportVaryingError(detail);
        return unscaled / unit;   // C# integer division truncates toward zero
    }

    /// <summary>The <see cref="VaryingInteger(Int128, int, string)"/> arm for a floating-point value (a VARYING
    /// expression over a float operand, or a transcendental function): GR5's noninteger test is on the value
    /// itself, and a non-finite value is not an integer either.</summary>
    public static Int128 VaryingInteger(double value, string detail)
    {
        if (!double.IsFinite(value)) { ExceptionState.ReportVaryingError(detail); return 0; }
        double whole = Math.Truncate(value);
        if (whole != value) ExceptionState.ReportVaryingError(detail);
        return (Int128)whole;
    }

    /// <summary>The <see cref="VaryingInteger(Int128, int, string)"/> arm for a standard-decimal intermediate
    /// (ARITHMETIC IS STANDARD-DECIMAL, §11.9.5.2 GR3): the value is an integer exactly when truncating it and
    /// rounding it away from zero land on the same integer.</summary>
    public static Int128 VaryingInteger(CobolDec value, string detail)
    {
        Int128 whole = value.ToUnscaledIntermediate(0, CobolRounding.Truncation);
        if (value.ToUnscaledIntermediate(0, CobolRounding.AwayFromZero) != whole) ExceptionState.ReportVaryingError(detail);
        return whole;
    }

    // ── INITIATE (ISO §14.9.21.4) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>INITIATE this report (ISO §14.9.21.4 GR1): sum counters ← 0 and their size error indicators
    /// unset (GR1a; kb/Work PB1130), LINE-COUNTER ← 0 (GR1b),
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
        foreach (var s in _sums) s.Reset();     // GR1a — "All sum counters and all size error indicators are set to zero"
        LineCounter = 0;                               // GR1b
        PageCounter = 1;                               // GR1c
        _active = true;                                // GR4
        _started = false;
        _firstBodySinceInitiate = true;
        _firstBodyOnPage = true;
        _rhOnThisPage = false;
        _pfOnThisPage = false;
        ArmGroupIndicate();                            // §13.18.28.4 GR1a — every detail's next GENERATE indicates
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
            EvaluateCode();   // §13.18.12.4 GR3 — the first body group's processing opens here: no page advance precedes it
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
            ArmGroupIndicate();   // §13.18.28.4 GR1c — a control break re-arms every detail group
            var current = new string[_controls.Count];
            for (int i = 0; i < _controls.Count; i++)
            {
                current[i] = _controls[i].Get();
                if (_controls[i].Prior is { } prior) _controls[i].Set(prior);
            }
            ProcessControlFootings(breakLevel);
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
        // Each addition is tested for size error (GR3): a size error leaves the counter as ADD ON SIZE ERROR leaves
        // it, sets the entry's indicator, and raises EC-REPORT-SUM-SIZE (kb/Work PB1130). A detail named n times
        // in a term's UPON phrase is added n times, each addition its own GR3 test (GR7; kb/Work PB1297).
        for (int k = 0; k < _sums.Count; k++)
            foreach (var t in _sums[k].Terms)
            {
                int times = t.Fires(detailName);
                if (times == 0) continue;
                // Each addition is its OWN ADD (GR3 — "Each addition is tested for size error"): the term's
                // addends are evaluated afresh against the counter's current content, summed at the wider of
                // their scales and stored once at the counter's scale, so an addend finer than the counter loses
                // its extra digits only when the SUM is stored — never one addend at a time (kb/Work PB1686). A
                // size error anywhere in that evaluation leaves the counter unchanged (§14.7.5 1)).
                for (int n = 0; n < times; n++)
                    Accumulate(k, t.Apply);
            }
        // …and for a SUM group whose UPON names THIS report's detail while its counter belongs to ANOTHER report
        // (§13.18.54.4 GR7 c) 2), SR4 g)'s twin for UPON): the event is this GENERATE, the counter the target's.
        if (detailName is not null)
            foreach (var (triggerDetail, target, sumId, apply) in _generateTriggers)
                if (triggerDetail.Equals(detailName, StringComparison.OrdinalIgnoreCase))
                    target.Accumulate(sumId, apply);

        // GR4d / GR5b: the specified detail — unless summary reporting (GR2).
        if (detailName is not null && _details.TryGetValue(detailName, out var detail))
        {
            PresentBody(detail);
            // §13.18.28.4 GR1 — this GENERATE WAS the "first occasion that a GENERATE is issued for the current
            // detail group" since the last event, whether or not the group printed: a SUPPRESS (§14.9.45) or an
            // all-absent PRESENT WHEN leaves the GENERATE issued all the same. Consumed only AFTER the
            // presentation, because the page advance of this group's own page-fit test (§13.18.35.4 GR4) is an
            // event b) that precedes its lines and must re-arm the condition they read.
            detail.GroupIndicatePending = false;
        }
    }

    /// <summary>⛔ THE ONE CONTROL-FOOTING SEQUENCE — a control break at <paramref name="breakLevel"/> (§14.9.16.4
    /// GR5 a)) and TERMINATE's "as though a control break has been sensed in the most major control data item"
    /// (§14.9.46.4 GR3 b), <paramref name="breakLevel"/> 0) both process the footings minor → break level. Each
    /// LEVEL is processed whether or not a control footing is DECLARED for it, because §13.18.54.4 GR2 resets a
    /// RESET ON counter "at the end of the processing of the control footing for the specified level of control.
    /// If no such control footing is defined, it is assumed to be present and to consist of a 01-level entry
    /// alone" (kb/Work PB1297): the reset belongs to the LEVEL, and used to be keyed on a presented CF group, so a
    /// level with no CF never reset its counters. §13.18.37.4 GR1 — only the footing AT the break level applies
    /// its NEXT GROUP clause ("no effect when it is specified in a control footing that is at a level other than
    /// the highest level at which the control break is detected").</summary>
    private void ProcessControlFootings(int breakLevel)
    {
        for (int i = _controls.Count - 1; i >= breakLevel; i--)
        {
            if (_controlFootings.TryGetValue(i, out var cf)) PresentBody(cf, applyNextGroup: i == breakLevel);
            // GR2 — the end of the processing of level i's control footing, declared or assumed. The RESET ON
            // entry's own PRESENT WHEN is not asked here: GR10's "not reset to zero for the current instance of the
            // report group" speaks of the group the entry is IN, and §13.18.54.3 SR8 keeps that group from being
            // this footing (an entry in a control footing: "its level of control shall be a lower level than that
            // of data-name-3").
            foreach (var s in _sums)
                if (s.ResetLevel == i) s.Reset();
        }
    }

    /// <summary>ISO §13.18.28.4 GR1 a/b/c — an INITIATE, a page advance or a control break makes the next
    /// GENERATE of EVERY detail group its "first occasion" again.</summary>
    private void ArmGroupIndicate()
    {
        foreach (var d in _details.Values) d.GroupIndicatePending = true;
    }

    /// <summary>The ISO §13.18.28.4 GR1 condition of the report group whose lines are being composed: true exactly
    /// when that group is a detail group presenting on its first GENERATE since an INITIATE, a page advance or a
    /// control break. A GROUP INDICATE printable item's generated compose reads it as its PRESENT WHEN condition
    /// (GR1: "the same effect as a PRESENT WHEN clause"), so an indicated item that is not presented is ABSENT
    /// (§13.18.41.4 GR2b) — it places nothing and moves no horizontal counter — rather than overwritten with
    /// spaces after the fact. Set by <see cref="PresentBody"/> before the group's first line is composed.</summary>
    public bool GroupIndicatePresent { get; private set; }

    /// <summary>The most-major control level whose CURRENT value differs from its prior (§13.18.16.4 GR3 —
    /// tested major→minor, the first change wins; FINAL never breaks mid-report, GR2), by the program's own
    /// comparison (<see cref="ControlEntry.ChangedFrom"/>, kb/Work PB1131). Null when no break.</summary>
    private int? DetectBreakLevel()
    {
        for (int i = 0; i < _controls.Count; i++)
        {
            if (_controls[i].IsFinal) continue;
            if (_controls[i].Prior is { } prior && _controls[i].ChangedFrom(prior))
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
            // ⛔ GR3 a) … d) IS ONE BRACKET, AND c) IS INSIDE IT (kb/Work PB1187). "a) The contents of any control
            // data items are changed to their prior values. b) Each control footing is printed … c) The report
            // footing is printed, if defined. d) The contents of any control data items are restored to the values
            // they had at the start of execution of the TERMINATE statement." The restore is the LAST action, so
            // the report footing — and the final page footing, which §13.18.57.4 GR6f prints immediately before it
            // — are composed while the control items still hold their PRIOR values. The restore used to close the
            // control-footing loop, so an RF / last PF that SOURCEd a control item printed the value the program
            // had moved in after its last GENERATE.
            string[]? current = null;
            if (_controls.Count > 0 && _controls[0].Prior is not null)
            {
                current = new string[_controls.Count];
                for (int i = 0; i < _controls.Count; i++)
                {
                    current[i] = _controls[i].Get();
                    if (_controls[i].Prior is { } prior) _controls[i].Set(prior);   // GR3a
                }
                // GR3b — minor → major, "as though a control break has been sensed in the most major control data item", so the
                // most major footing is the one §13.18.37.4 GR1 lets apply its NEXT GROUP clause.
                ProcessControlFootings(0);
            }
            // §13.18.57.4 GR6f: the page footing prints as the last report group on EACH page — including the
            // final page (exception GR6f 2: a last page occupied only by an RF on a page by itself — the RF's LINE
            // NEXT PAGE form, whose own page feed PresentHeadingFooting takes after this PF). When an RF not on a
            // page by itself follows, the PF is "immediately followed by" it.
            if (_pageFooting is not null) PresentPageFooting();
            if (_reportFooting is { } rf) PresentHeadingFooting(rf);   // GR3c
            if (current is not null)
                for (int i = 0; i < _controls.Count; i++) _controls[i].Set(current[i]);   // GR3d — LAST
            FlushPendingLine();   // the report's last line: nothing of this report can overprint it now
        }
        _active = false;   // GR6: the associated file stays open
        _started = false;
    }

    // ── SUPPRESS (ISO §14.9.45) ──────────────────────────────────────────────────────────────────────────────

    /// <summary>The SUPPRESS statement (ISO §14.9.45) of the USE BEFORE REPORTING procedure for the group whose
    /// <see cref="ReportGroup.Index"/> is <paramref name="groupIndex"/> — the group "named in the USE procedure
    /// within which the SUPPRESS statement appears" (GR1), fixed at bind from the statement's lexical declarative.
    /// <para>⛔ KEYED BY THE GROUP, AND HONOURED ONLY DURING THAT GROUP'S OWN HOOK (kb/Work PB1186). GR1 inhibits
    /// "only" that group and GR2 limits the effect to "the current instance of the report group", so a SUPPRESS
    /// takes effect exactly when the named group is the one about to be produced — i.e. while
    /// <see cref="RunBeforeReporting"/> runs ITS declarative. Reached any other way — a PERFORM of the declarative
    /// section from the main program or from another group's declarative, which §14.9.49.3 SR4 makes legal — no
    /// instance of the named group is current, and the statement has no effect. It used to set a per-report
    /// one-shot flag that the NEXT presentation of ANY group consumed, so a SUPPRESS performed from outside the
    /// hook suppressed a different group, or a later instance of its own.</para>
    /// SUPPRESS inhibits only printing, page advance, NEXT GROUP and LINE-COUNTER changes (GR3 a–d); it does NOT
    /// inhibit sum-counter accumulation (§13.18.54.4 GR7) or the end-of-group sum reset (GR2).</summary>
    public void SuppressPrinting(int groupIndex)
    {
        if (groupIndex == _hookGroup) _hookSuppressed = true;
    }

    /// <summary>Invoke a report group's USE BEFORE REPORTING declarative (ISO §14.9.49 Format 2 GR8, just before
    /// the group is produced) and report whether that declarative executed a SUPPRESS statement naming THIS group
    /// (§14.9.45.4 GR1/GR2). The suppression state is scoped to the hook: cleared on entry (no earlier SUPPRESS
    /// carries into this instance) and restored on exit (a nested presentation's hook cannot leak into, or erase,
    /// an enclosing one). A true result tells the caller to skip the PRINTING half of this presentation (GR3 a–d);
    /// the group's remaining PROCESSING — notably the end-of-group sum reset (§13.18.54.4 GR2) — is NOT skipped.</summary>
    private bool RunBeforeReporting(ReportGroup group)
    {
        if (_beforeReporting is not { } select) return false;   // no declarative ⇒ no SUPPRESS can name this instance
        var (outerGroup, outerSuppressed) = (_hookGroup, _hookSuppressed);
        _hookGroup = group.Index;
        _hookSuppressed = false;
        // §14.9.49.4 GR10 — THE ONE PLACE the BEFORE REPORTING range is entered. Every presentation path
        // (PresentBody / PresentPageHeading / PresentPageFooting / PresentHeadingFooting) funnels through
        // this method, so the range cannot be half-tracked. try/finally: the declarative can throw a fatal
        // EC out of the hook. The range is entered around the SELECTION as well as the run: a selection that
        // finds no qualifying declarative executes no statement, so the wider bracket observes nothing.
        var flow = RunUnit.Current.ReportFlow;
        flow.Enter();
        try
        {
            select(group.Index);
            return _hookSuppressed;
        }
        finally
        {
            flow.Exit();
            (_hookGroup, _hookSuppressed) = (outerGroup, outerSuppressed);
        }
    }

    // ── Group presentation (ISO §13.18.35.4 / §13.18.57.4 / §14.9.16.4 GR6) ──────────────────────────────────

    /// <summary>Present a BODY group (detail / CH / CF — §13.18.57.3 SR15): the §13.18.35.4 GR4 page-fit test
    /// (skipped for the chronologically first body group since INITIATE), a failed fit's §14.9.16.4 GR6 page
    /// advance, then each line per GR5 (first line) / GR7 (subsequent lines). <paramref name="reprint"/> is the
    /// OR PAGE reprint of a control heading (<see cref="PresentOrPageHeadings"/>): the first body group of a
    /// fresh page, so it takes no page-fit test, and it is not the heading's first printing, so it neither applies
    /// its NEXT GROUP clause nor resets the SUM counters it prints.</summary>
    private void PresentBody(ReportGroup group, bool applyNextGroup = true, bool reprint = false)
    {
        if (!BeginGroup(group, out int first, reprint)) return;
        var lines = group.Lines;

        // The first line's position when the preceding body group's absolute NEXT GROUP filled the save location
        // (§13.18.37.4 GR4a 3) — the ordinary GR5 placement otherwise.
        long? firstTarget = null;
        bool advanced = false;   // a page advance was processed for THIS group (its §14.9.16.4 GR6 c) evaluated the CODE)
        if (_nextGroupSave != 0)
        {
            firstTarget = PlaceAfterSavedNextGroup(group, first, LowerLimit(group));   // always advances first (GR4a)
            advanced = true;
        }
        else if (_paged && !_firstBodySinceInitiate && !reprint)
        {
            // §13.18.35.4 GR4b (absolute): fit iff integer-1 > LINE-COUNTER. GR4c (relative): trial =
            // LINE-COUNTER + Σ integer-2 over the group's relative LINE clauses; fit iff trial ≤ the group's
            // lower limit (§13.18.57.4 GR8: detail → LAST DETAIL; CH → LAST CH; CF → FOOTING). Which LINE
            // clause is "first" — and which contribute — depends on the PRESENT WHEN values (§13.18.35.4
            // GR4/§13.18.41.4 GR3a/GR3d: absent lines are disregarded by the page fit test).
            // ⚠ The 2023 GR4c wording — "incremented by integer-2 for each *subsequent* LINE clause" — is
            // ambiguous about the FIRST relative line's integer-2; the NIST goldens
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
                    if (group.IsPresent(lines[i].PresentSlot))
                        trial += lines[i].TrialInterval;
                fit = trial <= LowerLimit(group);
            }
            if (!fit)
            {
                AdvancePage(group);   // §13.18.35.4 GR4 tail → the §14.9.16.4 GR6 sequence
                advanced = true;
            }
        }
        // §13.18.12.4 GR3 — the CODE identifier is evaluated "either during page advance processing … or whenever page
        // advance processing is not performed": this is the second arm. (An OR PAGE reprint is the first body group of
        // a page whose advance just evaluated it.)
        if (!advanced && !reprint) EvaluateCode();

        GroupIndicatePresent = group.GroupIndicatePending;   // §13.18.28.4 GR1 — read AFTER this group's own page advance
        // §13.18.35.4 GR5 for the first PRESENT line — unless the preceding body group's absolute NEXT GROUP placed
        // it (§13.18.37.4 GR4a 3); §13.18.35.4 GR7 for the rest. An absent line leaves the next relative one on LINE-COUNTER.
        bool whole = PresentLines(group, firstTarget);
        _firstBodySinceInitiate = false;
        _firstBodyOnPage = false;
        if (!whole) return;   // a raised EC-REPORT-PAGE-LIMIT — resume at the next report group (PresentLine)
        if (reprint) return;
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
    private long? PlaceAfterSavedNextGroup(ReportGroup group, int first, int lowerLimit)
    {
        var lines = group.Lines;
        long saved = _nextGroupSave;
        _nextGroupSave = 0;
        AdvancePage(group);
        if (lines[first].Kind == ReportLineKind.Absolute)
        {
            LineCounter = saved;                                             // GR4a 1 (and GR4a 2 — see above)
            if (lines[first].Value <= LineCounter) AdvancePage(group);            // the re-applied §13.18.35.4 GR4b test
            return null;
        }
        // GR4a 3 — the first line at saved + 1; every later present line adds what it adds to a GR4c trial sum.
        long last = saved + 1;
        for (int i = first + 1; i < lines.Length; i++)
            if (group.IsPresent(lines[i].PresentSlot)) last += lines[i].TrialInterval;
        if (last <= lowerLimit) return saved + 1;
        AdvancePage(group);                                                       // the page devoid of body groups
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

    /// <summary>⛔ THE ONE PROLOGUE OF EVERY GROUP PRESENTATION — body, page heading, page footing, report heading
    /// and report footing alike. It runs the group's USE BEFORE REPORTING declarative (§14.9.49.4 GR8), takes the
    /// group's presence snapshot (<see cref="BeginPresentation"/>), and answers whether the group has anything to
    /// PRINT. When it has not, the group's processing still ENDS, so its end-of-group sum reset (§13.18.54.4 GR2)
    /// runs here:
    /// <list type="bullet">
    /// <item>a group with no present line — no LINE clause at all, or every line absent under PRESENT WHEN /
    /// OCCURS … DEPENDING. §13.18.41.4 GR2 b) omits the ENTIRE group only when "the entry is a level-01 entry"
    /// whose condition is false; an absent LINE entry is "as though the entry were omitted", which leaves a
    /// present 01-level entry alone — the very shape §13.18.54.4 GR2 still treats as a control footing that is
    /// processed (kb/Work PB1272). A counter whose own entry is absent — which includes every counter of an absent
    /// 01-level entry, since its chain carries the 01 condition — is not reset (§13.18.41.4 GR3 g)). No line
    /// prints, so nothing moves LINE-COUNTER or PAGE-COUNTER and no NEXT GROUP applies (§8.4.3.15.4 GR5 — a dummy
    /// report group);</item>
    /// <item>a group whose declarative executed SUPPRESS (§14.9.45.4 GR3 a)–d)): only the PRINTING half is
    /// inhibited — no page fit, no lines, no LINE-COUNTER movement, no NEXT GROUP — and the reset still runs.</item>
    /// </list>
    /// It used to return before the reset in the first case (and the page heading, page footing and report
    /// heading/footing paths never reset at all), so a control footing whose every line was absent left its
    /// ended group's total standing, and a page footing's page total never restarted.</summary>
    private bool BeginGroup(ReportGroup group, out int first, bool reprint = false)
    {
        bool suppressed = RunBeforeReporting(group);   // §14.9.49 GR8; true ⇒ a §14.9.45 SUPPRESS executed
        first = BeginPresentation(group);
        // §13.18.54.4 GR7 a)/b) — a rolled total is added when the group is PROCESSED, before any of its lines is
        // printed, whether or not anything prints (a SUPPRESSed group and a dummy group are still processed, and
        // SUPPRESS inhibits only printing, page advance, NEXT GROUP and LINE-COUNTER changes — see SuppressPrinting).
        // The snapshot §13.18.54.4 GR11 reads was just taken.
        // An OR PAGE reprint of a control heading is the same instance printed again, not a second processing
        // (the reset it skips, `PresentBody`), so it adds nothing.
        if (!reprint) ApplyRolled(group);
        if (first >= 0 && !suppressed) return true;
        EndOfGroupSumReset(group);
        return false;
    }

    /// <summary>Begin ONE group presentation: clear the per-presentation step anchors, then take the group's
    /// presence snapshot (ISO §13.18.41.4 GR2 / §13.18.38.4 GR13 — once per presentation, before any LINE clause
    /// is processed; <see cref="ReportGroup.SetPresence"/>). Returns the index of the first present line (−1 =
    /// none).</summary>
    private int BeginPresentation(ReportGroup group)
    {
        Array.Clear(_lineAnchors);
        group.SnapshotPresence();
        var lines = group.Lines;
        for (int i = 0; i < lines.Length; i++)
            if (group.IsPresent(lines[i].PresentSlot)) return i;
        return -1;
    }

    /// <summary>The body group's LOWER LIMIT for the page-fit test (ISO §13.18.57.4 GR8d/e/f).</summary>
    private int LowerLimit(ReportGroup group) => group.Kind switch
    {
        ReportGroupKind.ControlHeading => _lastControlHeading,   // GR8d
        ReportGroupKind.ControlFooting => _footing,              // GR8f
        _ => _lastDetail,                                        // GR8e — detail
    };

    /// <summary>The §14.9.16.4 GR6 page advance, in the GR's order: (a) the page footing, (b) the physical
    /// advance to the next page, (c) CODE evaluation (<see cref="EvaluateCode"/> — the new page's records carry
    /// the new value, and the page footing of step (a) was written with the old one), (d) PAGE-COUNTER + 1, or 1 after a NEXT GROUP NEXT PAGE WITH RESET, (e) LINE-COUNTER ←
    /// 0, (f) the page heading — and then the control headings written with OR PAGE (§13.18.57.4 GR6 c), see
    /// <see cref="PresentOrPageHeadings"/>). <paramref name="causing"/> is the body group whose presentation needed
    /// the advance: the headings reprinted depend on it.</summary>
    private void AdvancePage(ReportGroup causing)
    {
        if (_pageFooting is not null) PresentPageFooting();                  // GR6a
        PageFeed();                                                          // GR6b–e
        EvaluateCode();                                                      // GR6c — §13.18.12.4 GR3
        if (_pageHeading is not null) PresentPageHeading();                  // GR6f
        PresentOrPageHeadings(causing);                                      // §13.18.57.4 GR6 c)
    }

    /// <summary>⛔ ISO §13.18.57.4 GR6 c) — "The OR PAGE phrase causes the associated control heading to be printed
    /// in addition after each page advance, following any page heading, provided that the page advance did not
    /// take place just before the printing of a control footing at a lower control level." (kb/Work PB1298.)
    /// The headings are presented major → minor, like every run of control headings (GR6 c) 1.), each as the FIRST
    /// body group of the new page: no page-fit test (nothing fits better than the top of a page), no NEXT GROUP
    /// and no SUM reset (the heading is not being printed for the first time).
    /// <list type="bullet">
    /// <item>causing a CONTROL HEADING at level j: that heading prints itself on the new page, as does every
    /// heading below it in the same break, so only the headings ABOVE j (more major) are the "in addition" ones.</item>
    /// <item>causing a CONTROL FOOTING at level j: the headings at level j and every HIGHER level (more major) are
    /// reprinted, the headings BELOW j (more minor) are skipped. ⚠ The proviso's "lower control level" is elliptical
    /// and its referent is the HEADING's level: "not before a control footing at a lower control level" read with
    /// the footing as the referent would skip the headings of the groups the break leaves OPEN, which §13.18.57.4
    /// GR7 d) 4. contradicts by giving such a footing an upper limit after "the lowest-level control heading with
    /// an OR PAGE phrase at the same level as the control footing, or higher" (docs/CONFORMANCE.md A.4.11, kb/Work
    /// PB1927).</item>
    /// <item>causing a detail (or any other body group): every OR PAGE heading.</item>
    /// </list></summary>
    private void PresentOrPageHeadings(ReportGroup causing)
    {
        foreach (var ch in _controlHeadings.Values)
        {
            if (!ch.OrPage) continue;
            bool skip = causing.Kind switch
            {
                ReportGroupKind.ControlHeading => ch.ControlLevel >= causing.ControlLevel,
                ReportGroupKind.ControlFooting => ch.ControlLevel > causing.ControlLevel,
                _ => false,
            };
            if (!skip) PresentBody(ch, reprint: true);
        }
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
        ClaimDevice();
        FlushPendingLine();                       // no line on the NEW page can overprint the old page's last one
        // ⛔ BEFORE, not AFTER (kb/Work PB1667): the feed carries no record, so the empty image is presented FIRST and
        // the feed ends its (empty) line. An AFTER write would leave the connector standing on an "open" line — a
        // record presented after the feed — and the device is then NOT at the start of the new page's empty line 1,
        // which is what `_physLine == 0` tells PresentLine (CobolFile.DeviceOnOpenLine) and what makes a zero
        // advance on it an overprint return (§14.9.51.4 GR25 c)).
        CobolFile.WriteAdvancing(_fileName, "", -1, before: true, page: null);   // GR6b — form feed
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
        ArmGroupIndicate();                                                  // §13.18.28.4 GR1b — a page advance
    }

    /// <summary>Present the page heading (placement ISO §13.18.35.4 GR5b2: absolute → integer-1; relative with no
    /// report heading on the page → HEADING + integer-2 − 1, with one → LINE-COUNTER + integer-2). "First" =
    /// the first PRESENT line (§13.18.41.4 GR2/GR5); an all-absent PH prints nothing (GR2b).</summary>
    private void PresentPageHeading()
    {
        var ph = _pageHeading!;
        if (!BeginGroup(ph, out _)) return;
        if (PresentLines(ph))                                               // GR5b2 / GR7
            EndOfGroupSumReset(ph);                                         // §13.18.54.4 GR2
    }

    /// <summary>Place every PRESENT line of a heading or footing group: the first by the one §13.18.35.4 GR5 rule
    /// (<see cref="FirstLineTarget"/>, or <paramref name="firstTarget"/> when the caller owns that placement), the
    /// rest by GR7. An absent line is skipped as though its entry were omitted (§13.18.41.4 GR2 b)). False when a
    /// raised EC-REPORT-PAGE-LIMIT abandoned the rest of the group (<see cref="PresentLine"/>): the caller then
    /// resumes at the next report group, skipping the group's NEXT GROUP clause and its end-of-group reset.</summary>
    private bool PresentLines(ReportGroup group, long? firstTarget = null)
    {
        bool isFirst = true;
        foreach (var l in group.Lines)
        {
            if (!group.IsPresent(l.PresentSlot)) continue;                  // §13.18.41.4 GR2b
            long target = !isFirst ? SubsequentTarget(l) : firstTarget ?? FirstLineTarget(group, l);
            isFirst = false;
            if (!PresentLine(target, l)) return false;
        }
        return true;
    }

    /// <summary>Present the page footing (placement ISO §13.18.35.4 GR5b4: absolute → integer-1; relative →
    /// FOOTING + integer-2). "First" = the first PRESENT line (§13.18.41.4 GR2/GR5); an all-absent PF prints
    /// nothing (GR2b).</summary>
    private void PresentPageFooting()
    {
        var pf = _pageFooting!;
        if (!BeginGroup(pf, out _)) return;
        bool whole = PresentLines(pf);                                      // GR5b4 / GR7
        _pfOnThisPage = true;
        if (!whole) return;   // a raised EC-REPORT-PAGE-LIMIT — resume at the next report group
        ApplyNextGroup(pf);   // §13.18.37.4 GR5
        EndOfGroupSumReset(pf);   // §13.18.54.4 GR2 — a page total restarts with the page footing that printed it
    }

    /// <summary>Present the report heading or report footing in flow (placement ISO §13.18.35.4 GR5b1 for RH —
    /// relative → HEADING + integer-2 − 1; GR5b5 for RF — relative → FOOTING + integer-2 unless a page footing
    /// printed on the same page, then LINE-COUNTER + integer-2; absolute → integer-1 for both; in a report NOT
    /// divided into pages, relative → LINE-COUNTER + integer-2 for both, GR5c — all in <see cref="FirstLineTarget"/>). "First" = the
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
        if (!BeginGroup(group, out int first)) return;
        bool ownPage = _paged && group.Kind == ReportGroupKind.ReportFooting && group.Lines[first].NextPage;
        if (ownPage) PageFeed();                                                                   // GR5a
        // §13.18.57.4 GR7f for a relative first line on a page by itself; §13.18.35.4 GR5a / GR5b1 / GR5b5 / GR5c otherwise.
        bool whole = PresentLines(group, ownPage && group.Lines[first].Kind != ReportLineKind.Absolute ? _heading : null);
        if (group.Kind == ReportGroupKind.ReportHeading) _rhOnThisPage = true;
        if (!whole) return;   // a raised EC-REPORT-PAGE-LIMIT — resume at the next report group
        ApplyNextGroup(group);   // §13.18.37.4 GR3 (a report footing carries none — §13.18.37.3 SR4)
        EndOfGroupSumReset(group);   // §13.18.54.4 GR2 — a report footing's own counters (§13.18.54.3 SR4)
    }

    /// <summary>⛔ THE ONE §13.18.35.4 GR5 RULE — the line number of a report group's FIRST (present) line, for
    /// every group type (kb/Work PB1247). It was written out four times, once per presentation method, and the
    /// UNPAGED arm (GR5 c)) existed only in the body-group copy, so an unpaged report's report heading and report
    /// footing were placed by the PAGED formulas over the RD's zero page regions. The callers keep only what is
    /// genuinely theirs: a body group's §13.18.37.4 GR4a save-location placement and a report footing on a page by
    /// itself (§13.18.57.4 GR7f).</summary>
    private long FirstLineTarget(ReportGroup group, ReportGroupLine l)
    {
        if (l.Kind == ReportLineKind.Absolute) return l.Value;                           // GR5a — integer-1
        long relative = RelativeValue(l);
        if (!_paged) return LineCounter + relative;                                       // GR5c — not divided into pages
        return group.Kind switch
        {
            ReportGroupKind.ReportHeading => _heading + relative - 1,                     // GR5b1
            ReportGroupKind.PageHeading => _rhOnThisPage
                ? LineCounter + relative                                                  // GR5b2, an RH on this page
                : _heading + relative - 1,                                                // GR5b2
            ReportGroupKind.PageFooting => _footing + relative,                           // GR5b4
            ReportGroupKind.ReportFooting => _pfOnThisPage
                ? LineCounter + relative                                                  // GR5b5, a PF on this page
                : _footing + relative,                                                    // GR5b5
            _ => _firstBodyOnPage ? _firstDetail : LineCounter + relative,                // GR5b3 — a body group
        };
    }

    /// <summary>The most recent report line, composed but NOT YET WRITTEN, and the physical page line it will
    /// occupy. ISO §13.18.35.4 GR3: "the non-space characters of a relative line specified with an integer-2 of
    /// zero will overwrite the corresponding characters of the preceding line" — the print stream cannot rewrite a
    /// line it has already emitted, so the engine holds each line back until the NEXT line proves it is not an
    /// overprint (<see cref="PresentLine"/>), and writes it at every point after which no line can overprint it: a
    /// line elsewhere, a page feed, the end of a TERMINATE, and the close of the report file (whatever closes it —
    /// the drain is registered on the connector, <see cref="CobolFile.HoldLine"/>). Null = none held.</summary>
    private string? _pendingImage;
    private int _pendingAdvance;
    private long _pendingLine;

    /// <summary>The cached drain handed to the report file's connector (one allocation per engine), and whether
    /// the connector currently holds it.</summary>
    private Action? _drain;
    private bool _drainRegistered;

    /// <summary>The physical page line the device stands on once any held-back line is written (0 = nothing on
    /// this page yet) — where an overlap or an overprint is measured from.</summary>
    private long DeviceLine => _pendingImage is null ? _physLine : _pendingLine;

    /// <summary>Present ONE report line: LINE-COUNTER is set to the computed line number FIRST (ISO §13.18.35.4
    /// GR6 — load-bearing: a <c>SOURCE IS LINE-COUNTER</c> item prints THIS line's number), then the line is
    /// composed (§13.18.53.4 GR3 — the implicit MOVEs execute when the line is printed) and placed at the line's
    /// vertical position. The single method ordering makes the GR6-before-compose sequence impossible to reorder
    /// per group.
    /// <para>⛔ A LINE ON OR ABOVE ONE ALREADY PRINTED (kb/Work PB1247 / PB1130). §13.18.35.4 GR3's one legal
    /// case — a relative line with integer-2 zero — OVERWRITES the preceding line's characters: it is merged into
    /// the held-back image, so the page carries one line and LINE-COUNTER still names it (GR1 / §8.4.3.15.4 GR4).
    /// Every other such line violates GR3: EC-REPORT-LINE-OVERLAP is set to exist, and when it is RAISED (checking
    /// enabled) §14.9.16.4 GR8 / §14.9.46.4 GR5 resume "at the next report item, line, or report group" — this line
    /// is abandoned. With checking off GR3's "the results are undefined" stands: a line on the device's own line
    /// overprints, one above it prints on the next physical line, and <see cref="_physLine"/> records the line it
    /// really occupies. The engine used to force a one-line advance for BOTH cases and record the TARGET as the
    /// physical line, so a LINE PLUS 0 printed one line low and every later line of the page drifted with it.</para>
    /// <para>⛔ A LINE PAST THE PAGE LIMIT (kb/Work PB1188). §13.18.35.4 GR2: "A report group shall never be split
    /// between two pages. If this rule is violated the EC-REPORT-PAGE-LIMIT exception condition is set to exist and
    /// the results are undefined." A line of a paged report placed below the page's last line is exactly that
    /// split. When the condition is RAISED, §14.9.16.4 GR8 / §14.9.46.4 GR5 resume at the next unit "whichever
    /// follows in logical order", and the unit that cannot be printed on one page is the GROUP: the line and the
    /// rest of its group are abandoned (false is returned), so the group's remaining processing — its NEXT GROUP
    /// clause and its end-of-group sum reset — does not take place. With checking off GR2's undefined result is
    /// the line printed below the limit, as before.</para></summary>
    /// <returns>False when the rest of the group is abandoned (a raised EC-REPORT-PAGE-LIMIT).</returns>
    private bool PresentLine(long target, ReportGroupLine line)
    {
        if (target < 1) target = 1;
        if (_paged && target > _pageLimit
            && ExceptionState.ReportPageLimitError($"report {Name}: line {target} is past the page limit {_pageLimit} "
                + "- the report group is split between two pages (ISO §13.18.35.4 GR2)"))
            return false;                         // §14.9.16.4 GR8 / §14.9.46.4 GR5 — resume at the next report group
        // ISO §13.18.38.4 GR12c/GR12d: the datum a later occurrence of this same line steps from is the page
        // line THIS one landed on. A Step line re-anchors only when its own anchor was never seeded (the first
        // occurrence was absent, §13.18.41.4 GR2b) — otherwise its Value is the displacement FROM the seed, and
        // moving the seed would compound it.
        if (line.Anchor != 0 && (line.Kind != ReportLineKind.Step || Anchor(line.Anchor) == 0))
            SeedAnchor(line.Anchor, target - (line.Kind == ReportLineKind.Step ? line.Value : 0));
        long device = DeviceLine;
        bool overprint = false;
        if (device != 0 && target <= device)
        {
            bool zeroRelative = target == device && line.Kind == ReportLineKind.Relative && line.Value == 0;
            // An abandoned line is never printed, so LINE-COUNTER keeps naming the line that was (GR1 — "the line
            // number within the page of the most recent line to have been printed"): the raise precedes GR6's set.
            if (!zeroRelative
                && ExceptionState.ReportLineOverlapError($"report {Name}: line {target} is on or above line {device}, "
                    + "already printed on this page (ISO §13.18.35.4 GR3)"))
                return true;                        // §14.9.16.4 GR8 / §14.9.46.4 GR5 — resume at the next line
            overprint = target == device;
        }
        LineCounter = target;                     // §13.18.35.4 GR6 — BEFORE the compose
        // §13.18.12.4 GR1 — the CODE characters stand "in the first characters of each logical record written to the
        // report file for this report" (the line the compose returns is the report line alone, GR2).
        string image = _code + line.Compose();    // §13.18.53.4 GR1/GR3 — evaluated at presentation time
        if (overprint && _pendingImage is { } under)
        {
            _pendingImage = Overprint(under, image);   // GR3 — the non-space characters overwrite
            return true;
        }
        ClaimDevice();
        FlushPendingLine();
        // ⛔ A PAGE'S LINE 1 IS WHERE THE PRINT STREAM ALREADY RESTS, NOT ONE ADVANCE BELOW IT (kb/Work PB484).
        // §13.18.35.4 GR6: "the report's LINE-COUNTER is set equal to that line number and the line is now
        // printed on the page at that vertical location" — line number n IS page line n, and GR7's "Any
        // unoccupied lines on the page result in a blank line" fixes the count of blanks above it. The stream
        // starts each page (INITIATE §14.9.21.4 GR1b, and the §14.9.16.4 GR6b form feed) positioned AT line 1
        // with nothing written there, so a record emitted with NO advance occupies line 1 and the travel to
        // line `target` is target − 1 while the page is still empty — target − _physLine only once a line has
        // been printed. `_physLine == 0` IS that empty page, not a line zero to advance off; reading it as one
        // put every report line of every report one line too low.
        // ⛔ …BUT ONLY WHILE THE DEVICE IS NOT ON A LINE ALREADY (kb/Work PB1667). The connector owns "the open
        // line" (CobolFile.DeviceOnOpenLine); a report file that stays open after a TERMINATE stands on that run's
        // last line, and INITIATE's _physLine = 0 names an empty PAGE MODEL, not an empty device. The new page's
        // line 1 is then the line BELOW it, so the device stands at line 0 and the first line travels `target`
        // lines — one of which ends the open line. Reading it as "at line 1" gave a zero advance, which
        // §14.9.51.4 GR25 c) makes an overprint: the new run's first line landed ON the old run's last one.
        long from = _physLine != 0 ? _physLine : CobolFile.DeviceOnOpenLine(_fileName) ? 0 : 1;
        int advance = (int)(target - from);
        // An unchecked overlap (GR3 — "the results are undefined"), or an overprint whose preceding line was
        // already written out by a close: the line goes on the next physical line, and is recorded THERE.
        if (advance < 1 && _physLine != 0) advance = 1;
        _pendingImage = image;
        _pendingAdvance = advance;
        _pendingLine = from + advance;
        return true;
    }

    /// <summary>Take the report file's print device before writing to it: the connector holds at most ONE held-back
    /// line (<see cref="CobolFile.HoldLine"/>), so claiming it first writes out a line some OTHER producer on the
    /// same file still holds — its line precedes every line this report is about to write — and registers this
    /// report's drain for the close. A close or a claim invokes (and forgets) the drain, so it is re-registered
    /// after one, not per line.</summary>
    private void ClaimDevice()
    {
        if (_drainRegistered) return;
        CobolFile.HoldLine(_fileName, _drain ??= () => { _drainRegistered = false; FlushPendingLine(); });
        _drainRegistered = true;
    }

    /// <summary>Write the held-back line (see <see cref="_pendingImage"/>), if any.</summary>
    private void FlushPendingLine()
    {
        if (_pendingImage is not { } image) return;
        _pendingImage = null;
        CobolFile.WriteAdvancing(_fileName, image, _pendingAdvance, before: false, page: null);   // no LINAGE on a report FD (§13.4.5.2 Format 3)
        // _physLine is where the DEVICE stands, so it moves only when the write did. A refused write — a line
        // holding a character with no byte image in the file coded character set ('91', Annex A.1 item 159; owner
        // decision kb/Work R47), or a medium the host reports full ('34') — "does not take place" (§14.9.51.4
        // GR15), advance included; leaving _physLine behind makes the NEXT line's travel cover the refused line's
        // slot, so every later line of the page still lands on its own LINE-COUNTER line (kb/Work PB690).
        if (CobolFile.Status(_fileName) is [ '0', _ ]) _physLine = (int)_pendingLine;
    }

    /// <summary>ISO §13.18.35.4 GR3 — "the non-space characters of a relative line specified with an integer-2 of
    /// zero will overwrite the corresponding characters of the preceding line".</summary>
    private static string Overprint(string under, string over)
    {
        var merged = new char[Math.Max(under.Length, over.Length)];
        for (int i = 0; i < merged.Length; i++)
            merged[i] = i < over.Length && over[i] != ' ' ? over[i] : i < under.Length ? under[i] : ' ';
        return new string(merged);
    }

    /// <summary>Reset the SUM counters with no RESET phrase that <paramref name="group"/> prints: ISO §13.18.54.4
    /// GR2 — "reset to zero and the size error indicator is unset at the end of the processing of the report group
    /// in which it is printed". A RESET ON counter resets with its LEVEL instead (<see cref="ProcessControlFootings"/>).
    /// An entry absent in THIS presentation's snapshot is neither printed nor reset (§13.18.41.4 GR3 g) /
    /// §13.18.54.4 GR10): the compose reads the entry's printable face from the same snapshot, so the two halves
    /// cannot disagree (kb/Work PB1272).</summary>
    private void EndOfGroupSumReset(ReportGroup group)
    {
        foreach (var s in _sums)
            if (s.ResetLevel < 0 && ReferenceEquals(s.PrintedIn, group) && group.IsPresent(s.PresentSlot))
                s.Reset();
    }

    // ── Line-composition helpers (used by the generated compose methods) ──────────────────────────────────────

    /// <summary>A fresh, empty report line of the report's width, for one line's generated compose.</summary>
    public ReportLineImage NewLine() => new(_lineWidth);

    /// <summary>Place a printable item's image at COLUMN (1-based, ISO §13.18.14) — the image is already
    /// width-exact (the §13.18.53.4 GR1 implicit-MOVE result). The two COLUMN rules that constrain a printed line
    /// are tested here, where the item is placed (kb/Work PB1188):
    /// <list type="bullet">
    /// <item>§13.18.14.4 GR4 — "any given column position is used for only one printable item when the line is
    /// printed. If this rule is violated the EC-REPORT-COLUMN-OVERLAP exception condition is set to exist and the
    /// results are undefined." When it is RAISED, §14.9.16.4 GR8 / §14.9.46.4 GR5 resume "at the next report
    /// item": this item is not placed. With checking off it overwrites, as it always did. An ABSENT item never
    /// reaches here (its compose guard), which is §13.18.41.4 GR3 e) — the rule "not applied to items associated
    /// with absent data items".</item>
    /// <item>§13.18.14.4 GR5 — an item whose final column exceeds the page width sets EC-REPORT-PAGE-WIDTH, and
    /// "the report line is truncated, and the report line is printed" — that outcome is stated, so it holds
    /// whether or not the condition is raised.</item>
    /// </list>
    /// The line itself is also bounded by the report file's record (its width), where the image is cut.</summary>
    public void Place(ReportLineImage line, int column, string image)
    {
        int start = column >= 1 ? column - 1 : 0;
        if (line.Overlaps(start, image.Length)
            && ExceptionState.ReportColumnOverlapError($"report {Name}: the printable item at column {column} uses a "
                + "column position another printable item of the line already uses (ISO §13.18.14.4 GR4)"))
            return;                               // §14.9.16.4 GR8 / §14.9.46.4 GR5 — resume at the next report item
        if (start + image.Length > _pageWidth)
            ExceptionState.ReportPageWidthError($"report {Name}: the printable item at column {column} ends at column "
                + $"{start + image.Length}, past the page width {_pageWidth} (ISO §13.18.14.4 GR5)");
        line.Write(start, image, _pageWidth);
    }
}

/// <summary>One report line under composition (ISO §13.18.14): its characters and, per column position, whether a
/// printable item already uses it — the occupancy §13.18.14.4 GR4 is stated over ("any given column position is
/// used for only one printable item when the line is printed"). Built by <see cref="CobolReport.NewLine"/>, written
/// only by <see cref="CobolReport.Place"/>, and read once by the compose as its text.</summary>
public sealed class ReportLineImage
{
    private readonly char[] _chars;
    private bool[] _used;

    internal ReportLineImage(int width)
    {
        _chars = new char[width];
        Array.Fill(_chars, ' ');
        _used = new bool[width];
    }

    /// <summary>Does any column in [<paramref name="start"/>, <paramref name="start"/> + <paramref name="length"/>)
    /// already belong to a printable item? The occupancy reaches as far as the page width lets an item go
    /// (<see cref="Write"/>), not as far as the record: GR4 is stated over the report line's column positions, so two
    /// items overlapping past the record's cut overlap all the same (kb/Work PB1934).</summary>
    internal bool Overlaps(int start, int length)
    {
        int end = Math.Min(start + length, _used.Length);
        for (int i = start; i < end; i++)
            if (_used[i]) return true;
        return false;
    }

    /// <summary>Write <paramref name="image"/> from column index <paramref name="start"/>. Each column up to
    /// <paramref name="limit"/> (the page width, §13.18.14.4 GR5) is marked used, because the line a printable item
    /// belongs to is bounded by the page width and not by the record (GR4's occupancy); only the TEXT is cut at the
    /// line's width, the record the report writes (§13.18.43.4 GR6/GR7/GR18; Annex A.1 159), docs/CONFORMANCE.md).</summary>
    internal void Write(int start, string image, int limit)
    {
        int reach = Math.Min(start + image.Length, limit);
        if (reach > _used.Length) Array.Resize(ref _used, reach);
        int cut = Math.Min(reach, _chars.Length);
        for (int i = start; i < reach; i++)
        {
            if (i < cut) _chars[i] = image[i - start];
            _used[i] = true;
        }
    }

    /// <summary>The composed line.</summary>
    public override string ToString() => new(_chars);
}
