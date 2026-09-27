// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Common;
using CobolNet.Runtime;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>
/// The per-compilation EDITION context — the bind-side half of the four-compilers-in-one mission. As of
/// rearchitecture PHASE 02 this is a thin ADAPTER over the two things it always really was: an immutable
/// <see cref="Editions.EditionInfo"/> value (the SINGLE source of the targeted ISO edition — 85 / 2002 / 2014 /
/// 2023, the CLI's <c>--std</c> — and the strict/permissive <c>--permissive</c> axis) plus a stringly-typed
/// diagnostic sink (the <see cref="Diagnostics"/> / <see cref="Warnings"/> channels). Splitting the two ends the
/// triple-sourcing of <c>DialectLevel</c> (P5) and lets the ONE <see cref="Editions.ConstructRegistry.Check"/>
/// funnel — now living BELOW the frontend in <c>Cobol.Net.Editions</c> — report here through
/// <see cref="IDiagnosticSink"/> without the frontend and the compiler each re-encoding the severity policy.
/// The public surface is unchanged, so every existing call site compiles untouched (the ~290-site adapter
/// guarantee; the actual retirement of this adapter is deferred to P7).
/// </summary>
/// <remarks>
/// Two channels, one policy seam (VERSION_TEST_MATRIX_DESIGN "Phase-2 implementation plan" P2.1):
/// <list type="bullet">
/// <item><see cref="Diagnostics"/> is ERRORS-ONLY — <see cref="CompilerDriver"/> fails the compile on ANY entry,
/// so nothing below error severity may ever be appended there.</item>
/// <item><see cref="Warnings"/> is the non-failing channel (obsolete/archaic 0903 flags; removed constructs under
/// permissive); <see cref="Warning"/> is its only writer. The driver surfaces it on every
/// <see cref="CompilerDriver.Result"/>, success or not.</item>
/// <item><see cref="Removed"/> is the ONE severity decision for removed-construct gating: error when strict,
/// warning when permissive — one policy, several emit sites (feedback_one_mechanism_per_job). The layer-neutral twin
/// is <see cref="Editions.EditionSeverityPolicy"/>, which <see cref="Report"/> bridges back onto these channels.</item>
/// </list>
/// </remarks>
public sealed class EditionContext(int dialectLevel, bool permissive = false) : IDiagnosticSink
{
    /// <summary>The immutable edition value — the SINGLE source of the dialect year + the permissive axis (P5).
    /// Constructed with the non-validating record ctor to preserve the historical tolerance of any
    /// <paramref name="dialectLevel"/>; NEW code that needs the {85,2002,2014,2023} guard uses
    /// <see cref="Editions.EditionInfo.Of(int, bool)"/>.</summary>
    public EditionInfo Edition { get; } = new(dialectLevel, permissive);

    /// <summary>This context AS the structured report channel handed to
    /// <see cref="Editions.ConstructRegistry.Check"/> (it bridges to the legacy string channels via
    /// <see cref="Report"/>).</summary>
    public IDiagnosticSink Sink => this;

    /// <summary>The targeted ISO edition year (85 / 2002 / 2014 / 2023) — delegated to <see cref="Edition"/>.</summary>
    public int DialectLevel => Edition.Year;

    /// <summary>The severity axis (VERSION_TEST_MATRIX_DESIGN §10 #1): strict (default) rejects removed
    /// constructs; permissive accepts them with a warning and the pre-removal semantics (the migration mode).
    /// Introduction gating (a construct NEWER than the edition, 0900) is an error on BOTH axes.</summary>
    public bool Permissive => Edition.Permissive;

    /// <summary>Bind-time rejection diagnostics. Any entry fails the compilation (CompilerDriver → BindError).</summary>
    public List<string> Diagnostics { get; } = [];

    /// <summary>Non-failing edition diagnostics (obsolete/archaic flags; removed constructs under
    /// <see cref="Permissive"/>). Printed to stderr by the CLI, carried on <see cref="CompilerDriver.Result"/>;
    /// never fails a compile. <see cref="Warning"/> is the only writer.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>True when any failing diagnostic has been recorded (the CompilerDriver fail-fast gate).</summary>
    public bool HasErrors => Diagnostics.Count > 0;

    /// <summary>The fixed-point digit capacity of the targeted edition: 18 at COBOL-85, 31 at 2002+ (ISO
    /// §8.3.3.3.2 fixed-point literals 1–31 digits; the §14.7 composite-of-operands rules; PICTURE digit
    /// positions) — delegated to <see cref="Edition"/>.</summary>
    public int MaxDigits => Edition.MaxDigits;

    // ── The diagnostic cursor and the source-line map (kb/Work PB82) ──────────────────────────────────────────
    // A bind-time diagnostic used to be a bare "error CODE: message" — no file, no line, no column — because the
    // ~160 report sites pass no location and the channel accepted none. The fix is STRUCTURAL: the walkers
    // (StatementBinder per statement, DataBinder per entry, the parse-tree conformance arms per node) position the
    // cursor with `using var _ = edition.At(ctx)`, and Error/Warning stamp the prefix AUTOMATICALLY — no report
    // site changes and a new one cannot forget. The cursor is in RESULTANT-text space (the ANTLR token line after
    // COPY / REPLACE / continuation joins); the prefix maps it to the file and line the USER edits through
    // LineMap, the origin table the preprocessing chain built (Frontend.LineMap). SourceLineOf is the same map for
    // the bound tree's user-facing lines (DEBUG-LINE, EXCEPTION-LOCATION's third part).

    /// <summary>The RESULTANT-line → source-origin map of the compilation unit being bound; the driver sets it from
    /// the frontend. Null = the identity map on <see cref="SourceFile"/> (a unit-test bind without a frontend).</summary>
    public SourceLineMap? LineMap { get; set; }

    /// <summary>The main source file — the identity origin when no <see cref="LineMap"/> is set.</summary>
    public string SourceFile { get; set; } = "<source>";

    /// <summary>The compilation's DISPLAY over-punch convention — the CLI's <c>--sign-encoding</c> (kb/Work PB803,
    /// owner decision 2026-09-09; Annex A.1 items 177/178, ISO §13.18.52.4 GR4 / GR5 b). NOT a dialect axis: every
    /// edition leaves the representation to the implementor, so the option is orthogonal to <see cref="Edition"/>
    /// exactly as <see cref="LineMap"/> and <see cref="SourceFile"/> are. It rides here because this is the ONE
    /// per-compilation object <c>CompilerDriver</c> hands to bind, and <c>DataBinder.Edition</c> carries it on to
    /// the emitter (<c>EmitContext.SignEncoding</c>), which is where a <c>NumProfile</c> and a class-condition call
    /// are written. Defaults to <see cref="Runtime.SignEncoding.Ibm"/> — the documented default — so every direct
    /// test construction and every unflagged compile is unchanged.</summary>
    public SignEncoding SignEncoding { get; set; } = SignEncoding.Ibm;

    /// <summary>The <see cref="IDiagnosticSink.Cursor"/> — REAL here (the interface default is a no-op).</summary>
    public DiagnosticCursor Cursor { get; set; }

    /// <summary>The source origin (file, 1-based physical line) of RESULTANT line <paramref name="resultantLine"/>
    /// — the ONE mapping every user-facing consumer of a token line goes through.</summary>
    public SourceOrigin OriginOf(int resultantLine) =>
        LineMap?.Origin(resultantLine) ?? new SourceOrigin(SourceFile, resultantLine);

    /// <summary>The user's source line for RESULTANT line <paramref name="resultantLine"/> (the line in the file that
    /// physically holds it — the main source or a copybook).</summary>
    public int SourceLineOf(int resultantLine) => OriginOf(resultantLine).Line;

    /// <summary>The <c>file(line,col): </c> prefix for the current cursor — rendered by <see cref="SourceLocation"/>
    /// itself (the ONE format source; the parse-layer diagnostics print the same object), or empty when no cursor
    /// is set — a diagnostic about the compilation unit as a whole.</summary>
    private string Prefix() =>
        Cursor.IsSet ? OriginOf(Cursor.Line).ToLocation(Cursor.Column) + ": " : "";

    /// <summary>Record an edition-gating error (fails the compile).</summary>
    public void Error(string code, string message)
    {
        ErrorsRecorded++;
        AddOnce(Diagnostics, $"{Prefix()}error {code}: {message}");
    }

    // ── ⛔ THE REFUSAL LEDGER (kb/Work PB1029) ─────────────────────────────────────────────────────────────────
    // A bound node that stands for something the binder could NOT give a meaning to — a refused statement
    // (BoundRejected), a refused or unbuilt operand (BoundExprError / BoundOperandError / BoundBoolError), a
    // refused condition (BoundConditionError) — is lowered by the emitter to a run-time abort. Such a node is
    // harmless only when the compile FAILS (a refusal: its error was reported) or when the gap is ANNOUNCED (an
    // unbuilt shape: COBOLNET1756). Before this ledger nothing tied the node to either: ~140 sites reported and
    // then returned an ordinary no-op, and an operand error could be built with no report at all (a STRING
    // sending ALL literal, §14.9.43.3 SR2, compiled clean and aborted the run unit). Every such node is now
    // obtainable only through a factory that records itself here, and StatementBinder.BindStatement — the ONE
    // funnel every statement passes — reads the ledger: a refusal with no error during the statement fails the
    // compile (COBOLNET2362), an unbuilt operand in a statement that drew no error is announced (COBOLNET1756).

    /// <summary>Every <see cref="Error(string, string)"/> call, a de-duplicated repeat INCLUDED — so a
    /// parameterized expansion that re-binds a line and re-finds its error (kb/Work PB759, where the text is
    /// dropped as the same fact) still counts as "this statement drew an error".</summary>
    public int ErrorsRecorded { get; private set; }

    /// <summary>Refusal nodes bound so far (kb/Work PB1029) — see <see cref="NoteRefusal"/>.</summary>
    public int RefusalsBound { get; private set; }

    private readonly List<string> _unbuilt = [];

    /// <summary>Record that a REFUSAL node was bound — the source is wrong and the site says it reported why.
    /// The per-statement check is the funnel's; this one is the backstop for a node built outside any statement:
    /// with no error recorded at all, the compile is failed under <paramref name="unreported"/> (an internal-error
    /// descriptor) rather than let the node reach the generated program.</summary>
    internal void NoteRefusal(DiagnosticDescriptor unreported, string feature)
    {
        RefusalsBound++;
        if (ErrorsRecorded == 0) Error(unreported, UnreportedRefusalMessage(feature));
    }

    /// <summary>The ONE wording of the internal-error net, shared by <see cref="NoteRefusal"/> and the funnel.</summary>
    internal static string UnreportedRefusalMessage(string feature) =>
        $"{feature} was refused without a diagnostic naming the rule it breaks; this is a WiseOwl COBOL internal error — "
        + "please report it with the source that produced it";

    /// <summary>Record an operand shape WiseOwl COBOL has not BUILT (not a source error) — announced once per
    /// statement by the funnel as COBOLNET1756, exactly as a statement-level <c>BoundUnsupported</c> is.</summary>
    internal void NoteUnbuilt(string feature) => _unbuilt.Add(feature);

    /// <summary>The unbuilt-operand ledger's high-water mark, taken by the funnel before a statement binds.</summary>
    internal int UnbuiltMark => _unbuilt.Count;

    /// <summary>The first unbuilt operand recorded since <paramref name="mark"/> (or null), and the ledger cut back
    /// to <paramref name="mark"/> — a statement answers for its own operands exactly once.</summary>
    internal string? TakeUnbuiltSince(int mark)
    {
        if (_unbuilt.Count <= mark) return null;
        string first = _unbuilt[mark];
        _unbuilt.RemoveRange(mark, _unbuilt.Count - mark);
        return first;
    }

    /// <summary>A diagnostic is a FACT about a source position, and the same fact reported twice is one fact.
    /// The case that makes this reachable by construction is a PARAMETERIZED class or interface (kb/Work PB759):
    /// each EXPANSION re-binds the definition's own source lines (§12.3.8.4 GR5 — the expansion IS the definition
    /// with its formals replaced), so a defect in a line no formal touches is found once per expansion, at the
    /// same position with the same words. A diagnostic that DOES depend on the actual parameters differs in its
    /// text and is kept for each expansion — which is exactly the per-expansion report a user needs.</summary>
    private void AddOnce(List<string> into, string line)
    {
        if (_reported.Add(line)) into.Add(line);
    }
    private readonly HashSet<string> _reported = new(StringComparer.Ordinal);

    /// <summary>Record a diagnostic keyed by a catalogue <see cref="DiagnosticDescriptor"/> (P2.10 — the
    /// first-class registry replacing bare <c>COBOLNETnnnn</c> string literals). Emits the descriptor's
    /// <see cref="DiagnosticDescriptor.Code"/> with the site-composed <paramref name="message"/> — byte-identical
    /// to the former bare-code call — while binding the site to a stable, documented, suppressible identity
    /// (<see cref="DiagnosticDescriptor.Id"/>). All P2-catalogued descriptors reachable here are error-severity;
    /// warning-severity descriptors route through <see cref="Warning"/> at their own sites.</summary>
    public void Error(DiagnosticDescriptor descriptor, string message) => Error(descriptor.Code, message);

    /// <summary>Record a non-failing edition diagnostic (the 0903 obsolete/archaic flags; removed constructs
    /// under <see cref="Permissive"/> via <see cref="Removed"/>).</summary>
    public void Warning(string code, string message) => AddOnce(Warnings, $"{Prefix()}warning {code}: {message}");

    /// <summary>The <see cref="Warning"/> twin of the descriptor-keyed <see cref="Error(DiagnosticDescriptor,string)"/>.
    /// ⛔ ITS ABSENCE WAS A DEFECT, not a gap: a descriptor-carrying site that wanted a warning had no descriptor
    /// overload to reach for, so <c>--permissive</c> sites passed <c>descriptor.Id</c> to the string overload
    /// instead and emitted the kebab SLUG in the code position. That shipped in COBOLNET1627 (PB1) and
    /// COBOLNET1628 (PB6) — both documented under codes they never printed. Warning and Error must offer the
    /// SAME shape, or the asymmetry writes the bug for the next caller.
    /// <see cref="DiagnosticEmitFormDriftTests"/> keeps it from returning.</summary>
    public void Warning(DiagnosticDescriptor descriptor, string message) => Warning(descriptor.Code, message);

    /// <summary>THE severity seam for removed-construct gating (P2.1): a construct the targeted edition REMOVED
    /// is an error under strict, a warning (with the pre-removal semantics preserved) under
    /// <see cref="Permissive"/>. Every removed/reserved-word emit site routes its severity decision through
    /// here — never a local strictness test.</summary>
    // NOTE (the OVERRIDE/FINAL wave, DEVLOG 605): this seam now also carries DOCUMENTED-DIALECT-LENIENCY
    // gating (e.g. §11.7 SR4a redefinition-without-OVERRIDE, 0837 — error strict, warning + the pre-wave
    // name-match inference under --permissive), not only removed-construct gating: ONE policy seam, never a
    // parallel Lenient() method or a local Permissive test (feedback_one_mechanism_per_job).
    public void Removed(string code, string message)
    {
        if (Permissive) Warning(code, message);
        else Error(code, message);
    }

    /// <summary>⛔ THE ONE SEAM for a DECLINED OPTIONAL/PROCESSOR-DEPENDENT ELEMENT — every Annex A.3/A.4
    /// facility this implementation does not claim is named through here, at bind, once per occurrence.
    /// <para>The DESCRIPTOR's <see cref="DiagnosticDescriptor.Severity"/> chooses the disposition, so the
    /// policy lives in the catalogue (where it is documented and drift-tested) and never in a local test at
    /// the site: <see cref="EditionSeverity.Warning"/> ⇒ ACCEPT-INERT (the facility is additive — the program
    /// compiles, runs, and the facility does nothing: COBOLNET1578/1579/1778, satisfying §4.2.6 ¶3's
    /// mandatory compile-time warning mechanism for their Annex A.3 elements, and COBOLNET1580, whose A.4.14
    /// VALIDATE is instead a §4.2.7 element this implementation documents rather than is obliged to warn about
    /// — kb/Work PB709; the descriptor's <c>Annex</c> datum is what tells the two apart);
    /// <see cref="EditionSeverity.Error"/> ⇒ REFUSE (an inert
    /// compile would change the ANSWER, so Annex A.4.1's "shall accept the syntax … only when support … is
    /// claimed" applies to an optional element, and §4.2.6 ¶3's "The implementor is not required to produce
    /// executable code when unsupported processor-dependent language elements are used" to a
    /// processor-dependent one: COBOLNET1560/1705/1706/1707/1954).</para>
    /// <para>⛔ THE BAND SPLITS ON WHETHER AN INERT READING EXISTS, NOT ON WHICH ANNEX LISTS THE ELEMENT, and
    /// that is why there is ONE method and not two: a facility whose absence leaves every program-visible value
    /// unchanged is accepted-and-named, and one whose absence would change the ANSWER is refused-and-named.
    /// ⚠ This paragraph read "an Annex A.3 processor-dependent facility is accepted-and-warned under §4.2.6,
    /// while an Annex A.4 optional module that is Not claimed is refused" until 2026-09-09, which described a
    /// COINCIDENCE in the rows that existed — every refusal happened to be A.4 — as if it were the rule.
    /// COBOLNET1954 (kb/Work PB358) refutes it: the SOURCE phrase of RECORD KEY / ALTERNATE RECORD KEY is
    /// Annex A.3 item 40 and is REFUSED, because a record-key-name (§8.3.2.2.24) names no data item and an
    /// inert compile would leave the file with no prime key. What the annex still decides is the POSTURE
    /// CLAUSE the message cites — <see cref="DiagnosticDescriptor.Annex"/> → <c>PostureClause</c>.
    /// Both dispositions are "name the facility, then bind to a no-op or refuse"; only the severity differs, so
    /// the severity lives in the descriptor and every declined site — A.3 and A.4, data surface and statement
    /// surface — calls THIS, never a local <c>if (severity == …)</c>
    /// and never a second Report/Declined twin (feedback_one_mechanism_per_job).</para>
    /// <para>⛔ NOT routed through <see cref="Removed"/>: that is the strict/permissive migration seam for
    /// constructs an edition REMOVED. A declined optional element has no pre-removal semantics to preserve —
    /// but SOME declines do have an INERT reading, and whether this one does is the descriptor's
    /// <see cref="DiagnosticDescriptor.PermissiveInert"/> fact, read here and nowhere else. The axis is the
    /// same one that splits Warning from Error: a decline whose sibling STATEMENT is already accept-inert
    /// (the A.4.14 VALIDATE clauses beside COBOLNET1580, the A.4.3 APPLY COMMIT clause beside COBOLNET1579,
    /// and a declined module's exception-names, which raise nothing either way) can be tolerated under
    /// <c>--permissive</c> and bound to a no-op — nothing observable changes. A decline that would REBIND the
    /// source to a different construct cannot: an inert A.4.2 screen ACCEPT is re-read as the device ACCEPT
    /// and moves the wrong data (COBOLNET1560/1707), an inert FORMAT clause changes which bytes reach the
    /// medium (1705), an inert WRITE FILE writes the wrong record (1706). Those four are
    /// <c>PermissiveInert: false</c> and <c>--permissive</c> does not move them in either direction — which
    /// is also, measurably, how the accept-inert rows have always behaved.</para>
    /// <para><paramref name="seen"/> is the site's naming of WHICH element of the module was written (the
    /// module owns the code, the site owns the spelling); omit it and the descriptor's own
    /// <see cref="DiagnosticDescriptor.Title"/> is the whole message — the byte-identical shape the
    /// accept-inert sites emitted before this seam existed.</para></summary>
    public void Declined(DiagnosticDescriptor descriptor, string? seen = null)
    {
        string message = seen is null ? descriptor.Title : $"{seen}: {descriptor.Title}";
        bool refuse = descriptor.Severity == EditionSeverity.Error
                      && !(descriptor.PermissiveInert && Permissive);
        if (refuse) Error(descriptor.Code, message);
        else Warning(descriptor.Code, message);
    }

    /// <summary>The <see cref="IDiagnosticSink"/> bridge: render a structured <see cref="EditionDiagnostic"/>
    /// back onto the legacy string channels with byte-identical text — an <see cref="EditionSeverity.Error"/>
    /// goes to <see cref="Error"/> (fails the compile), a <see cref="EditionSeverity.Warning"/> to
    /// <see cref="Warning"/>. This is how the layer-neutral <see cref="Editions.ConstructRegistry.Check"/>
    /// produces exactly the diagnostics the old direct <c>edition.Error/Removed/Warning</c> calls did.</summary>
    public void Report(in EditionDiagnostic d)
    {
        if (d.Severity == EditionSeverity.Error) Error(d.Code, d.Message);
        else Warning(d.Code, d.Message);
    }

    /// <summary>Check a fixed-point digit-position count against the edition cap (ISO §8.3.3.3.2 / §13.18.40):
    /// 19–31 digits require COBOL-2002+; more than 31 is invalid at every edition.</summary>
    public void CheckDigitCapacity(int digits, string what)
    {
        if (digits <= MaxDigits) return;
        if (digits > 31)
            Error("COBOLNET0801", $"{what} has {digits} digit positions; ISO/IEC 1989 limits fixed-point items "
                + "and literals to 31 digits (ISO §8.3.3.3.2)");
        else
            Error("COBOLNET0802", $"{what} has {digits} digit positions; COBOL-85 limits fixed-point items and "
                + $"literals to 18 digits — 19–31 digits require --std 2002 or later (targeting COBOL-{DialectLevel})");
    }
}
