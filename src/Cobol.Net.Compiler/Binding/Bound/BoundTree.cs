// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The COBOL.NET bound semantic tree (COBOLNET_DESIGN §2). The binder resolves every reference to a Place, every
//  literal to typed text, and every condition/expression to a bound node ONCE — so the backend (and future
//  desugar passes + the G4 PC dispatcher) walk this tree WITHOUT re-touching the ANTLR parse tree. No bound node
//  holds a raw parse context. Control-flow *emission* (sequential paragraph calls now, the dispatcher at G4) is the
//  backend's concern; this tree faithfully represents the program's paragraph/statement structure.
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>A bound program unit: its paragraphs in source order — declarative sections FIRST (sharing the one
/// pc space, COBOLNET_DESIGN §14.5), then the nondeclarative body starting at <paramref name="EntryPc"/>
/// (ISO §14.2.3 GR1 — execution begins with the first nondeclarative procedure). <paramref name="Declaratives"/>
/// carries the program's USE AFTER STANDARD ERROR/EXCEPTION sections (ISO §14.9.49; empty/null when none).</summary>
public sealed record BoundProgram(
    IReadOnlyList<BoundParagraph> Paragraphs,
    int EntryPc = 0,
    IReadOnlyList<BoundDeclarative>? Declaratives = null,
    EcFeatures? Ec = null,
    IReadOnlyList<BoundMethod>? Methods = null,
    IReadOnlyList<BoundDebugSubject>? DebugSubjects = null,
    // The exception-checking (Format-3) PERFORM handler pc-ranges (imp-2/3/4) appended above the main pc space
    // (ISO §14.9.28.4 GR17). F3HandlerBasePc is the first such pc (null when the unit has none — the byte-identity
    // gate: a non-F3 unit adds zero synthetic pcs and needs no fall-through wall); F3HandlerOwners[pc - base] is the
    // owning PERFORM's PerformId (the EXIT-PERFORM / handler-region context). Set by the pc-range synthesis wave.
    int? F3HandlerBasePc = null,
    IReadOnlyList<int>? F3HandlerOwners = null);

/// <summary>One <c>USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES</c> subject procedure (X3.23-1985 debug
/// module; deleted 2002, absent ISO 2023 — modeled only at <c>--std 85</c>, VCR Table 7 row 7.17). The emitter
/// injects, at <paramref name="SubjectPc"/>'s dispatcher entry, a call that space-fills DEBUG-ITEM, sets
/// DEBUG-LINE (<paramref name="SourceLine"/>) / DEBUG-NAME (<paramref name="SubjectName"/>) / DEBUG-CONTENTS (the
/// transfer cause), then runs the debugging declarative body over the bounded pc range
/// [<paramref name="SectionStartPc"/>..<paramref name="SectionEndPc"/>]. A subject is always a NONdeclarative
/// procedure (pc ≥ EntryPc): a debugging declarative is never debugged, matching ALL PROCEDURES's exclusion of
/// the debugging sections themselves (DB101A "USE PROCEDURE NOT EXECUTED"). The data-name / file-name / cd-name
/// subject kinds and the SORT/MERGE-procedure cause taxonomy are staged (rejected COBOLNET1571 at bind).</summary>
public sealed record BoundDebugSubject(
    int SubjectPc,
    string SubjectName,
    int SourceLine,
    int SectionStartPc,
    int SectionEndPc);

/// <summary>One bound METHOD of a class body (ISO §11.7; OO deep-dive — the emit-into-a-type spine): its
/// contiguous pc range in the class's ONE dispatch space. The emitted public method runs
/// <c>__Dispatch(EntryPc, EndPc)</c> — the exit bound IS the method's LAST paragraph, so falling off the end
/// is the implicit method return, never a run into a sibling method's paragraphs (the legacy trap-#4 guard,
/// ported from CilEmitter's exit-bounded ranges). <paramref name="EntryPc"/> &gt; <paramref name="EndPc"/> ⇔
/// an empty method body (emitted as an empty C# method — no dispatch call at all).</summary>
public sealed record BoundMethod(string CobolName, string CsName, int EntryPc, int EndPc);

/// <summary>What of the EC exception-condition model (ISO §14.6.13; COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN) a
/// bound program actually USES — the emitter's gating summary: every piece of EC machinery (the int-returning
/// <c>__RunUse</c>, <c>__EcDispatch</c>, <c>__IoCheckEc</c>, the entry-wrapper fatal catch, the
/// <c>CobolNet.Runtime.Exceptions</c> using) is emitted ONLY when the group uses the feature, so an EC-free
/// program's generated source is byte-identical to a pre-EC build (the zero-scaffolding invariant, SSOT §18.16).</summary>
public sealed record EcFeatures(
    bool HasChecked,      // any statement bound under enabled >>TURN checking (a BoundEcChecked exists)
    bool HasIoChecked,    // any I-O statement with an enabled EC-I-O name (needs the generated __IoCheckEc)
    bool HasRaise,        // a RAISE statement (§14.9.29)
    bool HasResume,       // a RESUME statement (§14.9.33)
    bool HasF3,           // a USE AFTER EXCEPTION CONDITION declarative (§14.9.49 F3 — needs __EcDispatch)
    bool HasEcFunctions,  // a FUNCTION EXCEPTION-STATUS/-LOCATION/-STATEMENT reference (§15.28–15.33)
    bool HasRaising,      // a GOBACK/EXIT … RAISING (§14.9.18 / §14.9.14)
    bool HasF3Perform = false)  // an exception-checking (Format-3) PERFORM (§14.9.28 F3 — installs __EcPerform + the frame stack)
{
    /// <summary>Any EC-model feature present (drives the group-level <c>_ecActive</c> gate). HasF3Perform is
    /// included so the int-returning <c>__RunUse</c> (the pc-range handler invoker the interceptor depends on) is
    /// emitted even for an F3 PERFORM whose imp-1 uses no OTHER EC feature (e.g. <c>PERFORM CONTINUE WHEN
    /// EC-BOUND-SUBSCRIPT CONTINUE END-PERFORM</c>) — §14.9.28.4.</summary>
    public bool Any => HasChecked || HasIoChecked || HasRaise || HasResume || HasF3 || HasEcFunctions || HasRaising || HasF3Perform;
}

/// <summary>One USE declarative section (ISO §14.9.49): the pc <paramref name="Range"/> its use procedure spans
/// and its trigger scope. Format 1 (AFTER STANDARD ERROR/EXCEPTION): file-scoped (GR3a/GR5,
/// <paramref name="Files"/> non-empty) or open-mode-scoped (GR3b/GR6b–e, <paramref name="ModeIndex"/> = the
/// runtime <c>FileOpenMode</c> ordinal). Format 2 (BEFORE REPORTING): <paramref name="ReportGroup"/> names the
/// report group the procedure runs just before (GR8 — wired into the report engine's per-group hook at emission).
/// <paramref name="Global"/> is parsed and recorded; cross-program dispatch (GR4) is the post-CALL wave.
/// <para>⛔ <b>THE RANGE IS THE SECTION — there is no second, shape-derived "handler end"</b> (kb/Work PB367).
/// §14.9.49.3 SR1 makes the use procedure "the remainder of the section", §14.4.2 ends that section only at the
/// next section header or END DECLARATIVES, and §14.9.14.4 GR7's NOTE places the USE return mechanism after the
/// section's LAST paragraph ("an unnamed empty paragraph immediately following the last paragraph of the current
/// section, preceding any return mechanisms for that section" — the NOTE names USE as one of them). A handler end
/// chosen from paragraph SHAPE — a trivial EXIT/CONTINUE paragraph followed by a STOP RUN tail — executed the
/// selected declarative only in part on EVERY invocation path, so it is gone: this is the section's own
/// <see cref="PcRange"/>, produced once by <c>SectionInfo.CloseAt</c>.</para></summary>
public sealed record BoundDeclarative(
    string SectionName,
    PcRange Range,
    IReadOnlyList<FileModel> Files,
    int? ModeIndex,
    bool Global,
    ReportGroupModel? ReportGroup = null,
    IReadOnlyList<(string Ec, FileModel? File)>? EcEntries = null,
    BoundEoOperand? Eo = null)
{
    /// <summary>Is <paramref name="pc"/> a paragraph of THIS declarative section — the "am I binding inside this
    /// declarative?" test (§14.9.49.4 GR2 / the EXCEPTION-STATUS and BEFORE REPORTING scope questions). A
    /// declarative section is a physically contiguous run of the pc space (§14.4.2: paragraphs flatten in source
    /// order and the section ends at the next section header or END DECLARATIVES), which is what makes the
    /// arithmetic test legal HERE and nowhere else — an arbitrary <see cref="PcRange"/> may be a legal INVERTED
    /// THRU range (§14.9.28.4 GR6) for which containment means nothing (kb/Work PB440).</summary>
    public bool Contains(int pc) => !Range.IsEmpty && pc >= Range.Start && pc <= Range.End;
}
// EcEntries: the Format-3 scope (ISO §14.9.49.2 — USE AFTER {EXCEPTION CONDITION | EC} {ec-name [FILE f]…}…):
// each pair is one (exception-name, optional file) selection entry, consumed by the generated __EcDispatch
// selector's GR3c–g tiers. Null for Format 1/2 declaratives; an F3 declarative has empty Files / null ModeIndex,
// so the F1 __IoCheck switches naturally exclude it.
// Eo: Format 4's ONE operand (USE AFTER {EXCEPTION OBJECT | EO} {object-class-name-1 | interface-name-1},
// §14.9.49.2 — the EC-OO wave). See BoundEoOperand: the two alternatives are ONE discriminated value, never
// two nullable fields, because §14.9.49.4 GR14 tests them in two SEPARATE passes and a shape that lets one arm
// be forgotten is the shape that lost SR17 for a year. Each alternative carries its RESOLVED COBOL SYMBOL, never
// a rendered C# type name: choosing the C# spelling — and how MANY spellings one COBOL name selects — is the
// emitter's job (kb/Work PB365 + PB366).

/// <summary>The single operand of a Format-4 USE statement (ISO §14.9.49.2): WHICH alternative of the format's
/// brace group was written, carrying that alternative's RESOLVED symbol. §14.9.49.4 GR14 gives the two
/// alternatives two DIFFERENT tests in two SEPARATE passes — a) every object-class-name-1 in source order, and
/// only "otherwise, all of the USE statements in the source element are analyzed again" for b) every
/// interface-name-1 — so the discriminator is load-bearing at EMIT, not decoration.
/// <para>Both alternatives render as a C# type test over the alternative's own census of emitted types (see the
/// two cases); the pass structure and the per-pass census are the emitter's, in <c>EcEmitter</c>.</para></summary>
public abstract record BoundEoOperand;

/// <summary>The object-class-name-1 alternative — ISO §14.9.49.3 SR16, selected by §14.9.49.4 GR14 a): the
/// exception object "is a factory object or instance object of object-class-name-1 or of a subclass of
/// object-class-name-1". ONE clause naming BOTH object kinds, and a COBOL class is emitted as TWO DISJOINT C#
/// hierarchies (the instance class and its sibling <c>…__FACTORY</c> singleton, each rooted at its base's
/// corresponding half) — so one COBOL class-name selects a SET of C# types, and the selector renders
/// <see cref="Compiler.Oo.OoClassSymbol.FactoryOrInstanceCsTypes"/>, the ONE census, as one C# or-pattern
/// (kb/Work PB366: testing only the instance half selected no declarative for any factory exception object,
/// silently).</summary>
public sealed record BoundEoClass(Compiler.Oo.OoClassSymbol Symbol) : BoundEoOperand;

/// <summary>The interface-name-1 alternative — ISO §14.9.49.3 SR17, selected by §14.9.49.4 GR14 b): the
/// exception object "is described with an IMPLEMENTS clause that references interface-name-1". §11.8.4 GR2
/// (instance objects) and §11.4.4 GR2 (factory objects) make that a CLOSURE — a) the object's own IMPLEMENTS,
/// b) "the instance object implements an interface that inherits intf-1", c) "the class containing the instance
/// object inherits a class whose instance object implements intf-1" — and the emitter renders each emitted half
/// of a class with exactly that closure as its C# interface list (a COBOL INTERFACE-ID emits ONE C# interface,
/// carrying its own INHERITS as C# bases), so one test against
/// <see cref="Compiler.Oo.OoInterfaceSymbol.ImplementedCsTypes"/> realizes all three legs for both halves
/// (kb/Work PB365: before it this alternative had no resolution path at all and died on SR16's diagnostic).</summary>
public sealed record BoundEoInterface(Compiler.Oo.OoInterfaceSymbol Symbol) : BoundEoOperand;

/// <summary>A bound paragraph: its COBOL name and its SENTENCES (each a statement list — the separator-period
/// boundaries are semantic: NEXT SENTENCE transfers to the point after the current sentence, ISO §14.9.19.4 GR4
/// (the THEN phrase) and GR6 (the ELSE phrase) — the SAME transfer, stated once per arm).
/// Its pc index is its position in <see cref="BoundProgram.Paragraphs"/> — the G4 PC dispatcher transfers control
/// by that index.</summary>
public sealed record BoundParagraph(string CobolName, IReadOnlyList<IReadOnlyList<BoundStatement>> Sentences,
    int SourceLine = 0)
{
    // SourceLine: the paragraph's LAST executable statement's source line — the X3.23-1985 DEBUG-LINE value when a
    // debug subject is reached by sequential FALL THROUGH (the causing statement is the one that completed and fell
    // through; DB101A "FALL-THROUGH-TEST" pins it to the preceding "MOVE 0 TO RESULT-FLAG.", :403-407). 0 when the
    // debug facility is inactive / the paragraph is empty (never read then). VCR Table 7 row 7.17.
    /// <summary>All statements in order (sentence boundaries flattened) — for consumers that don't care.</summary>
    public IEnumerable<BoundStatement> Statements => Sentences.SelectMany(s => s);
}

// ── Numeric expressions (scale-tracked at render time by the backend) ──────────────────────────────────────────

/// <summary>A bound numeric expression — a tree of resolved operands and operators (no parse context).</summary>
[BoundNode]
public abstract record BoundExpr;

/// <summary>A numeric literal, kept as raw source text (e.g. <c>"3.5"</c>, <c>"-12"</c>); the backend scales it.
/// <paramref name="Carrier"/> is set only where the value is a BINARY floating-point carrier value that the text
/// merely images — the §15.43 / §15.58 / §15.83 fold of a binary32 or binary64 usage's extreme (kb/Work PB1196).
/// The text is then a decimal that ROUNDS to that value (the binary64 maximum's exact expansion has 309 digits, and
/// the SDIDI carries 34), which round-to-nearest recovers and a directed §14.7.4.3 mode does not: stored into a
/// COMP-2 under the implied TRUNCATION, <c>1.7976931348623157E+308</c> lands one ulp below the maximum it names.
/// The backend therefore hands a floating-point resultant the carrier value itself.</summary>
public sealed record BoundNumLiteral(string Text, BinaryFloatCarrier? Carrier = null) : BoundExpr;

/// <summary>The ISO/IEC 60559 binary interchange format a <see cref="BoundNumLiteral"/>'s value is exactly a value
/// of (binary32: COMP-1 / FLOAT-SHORT / FLOAT-BINARY-32; binary64: the other binary float usages).</summary>
public enum BinaryFloatCarrier { Binary32, Binary64 }

/// <summary>A reference to a numeric data item.</summary>
public sealed record BoundNumRef(Place Place) : BoundExpr;

/// <summary>A binary arithmetic node (<c>+ - * /</c>).</summary>
public sealed record BoundBinary(BoundExpr Left, char Op, BoundExpr Right) : BoundExpr;

/// <summary>Arithmetic negation.</summary>
public sealed record BoundNegate(BoundExpr Operand) : BoundExpr;

/// <summary>Exponentiation (<c>base ** exp</c>).</summary>
public sealed record BoundPower(BoundExpr Base, BoundExpr Exp) : BoundExpr;

/// <summary>An INDEXED BY index-name read as its 1-based occurrence number (the C# <c>long</c> index field,
/// COBOLNET_DESIGN §3.5). Valid in SET senders, SEARCH, relation conditions, and subscripts (ISO §13.18.38).</summary>
public sealed record BoundIndexRef(string IndexField) : BoundExpr;

/// <summary>The LINAGE-COUNTER special register of <paramref name="File"/> (ISO §8.4.3.14): a READ-ONLY unsigned
/// integer the I-O control system alone modifies (§13.18.34 GR7b) — the current line within the page body. It is
/// runtime-sourced (the connector's counter, COBOLNET_DESIGN's register-attaches-to-its-subsystem rule), never a
/// synthesized storage item; SR2 bars it from receiving positions (receiving resolution already fails loud).</summary>
public sealed record BoundLinageCounterRef(FileModel File) : BoundExpr;

/// <summary>A report's LINE-COUNTER or PAGE-COUNTER register (ISO §8.4.3.15): an unsigned integer the Report
/// Writer Control System alone maintains (GR1–GR4) — runtime-sourced from the report's engine instance (the
/// register-attaches-to-its-subsystem rule, the <see cref="BoundLinageCounterRef"/> precedent), never a storage
/// item. SR3 bars LINE-COUNTER from receiving positions (receiving resolution rejects at bind).</summary>
public sealed record BoundReportCounterRef(ReportModel Report, bool IsPage, int Depth = 0) : BoundExpr;

// ⛔ `BoundReportSumRef` IS GONE (kb/Work PB840 × PB882), and this comment stands where it was so it is not
// re-added. It was a SECOND way to read a sum counter, alongside the ordinary identifier machinery, and it
// carried the counter's identity as a STRING — the entry's data-name — which is exactly how two entries that
// legally share a name came to share one counter. A sum counter is now `Model.ReportSumCounterPlace`: one
// identity (the entry's ordinal, §13.18.54.4 GR1), one accessor, and a RECEIVING side, which is what §13.18.54.4
// GR12 ("It is permissible for procedure division statements to alter the content of sum counters") needs and a
// read-only BoundExpr could never have.

/// <summary>A report VARYING counter read (ISO §13.18.64.4 GR3/GR4 — the per-repetition counter persists
/// through its occurrence and acts as a source item): a scale-0 integer, <paramref name="CsName"/> naming the
/// compose-local counter variable. Produced only by the report-section compose emission (a counter is
/// referable only within its entry, §13.18.64.3 SR2).</summary>
public sealed record BoundReportVaryingRef(string CsName) : BoundExpr;

/// <summary>An OCCURS DEPENDING table's CURRENT extent (ISO §13.18.38 GR8 — "the rules of the OCCURS clause for a
/// sending data item", which §15.50.4 r4b / §15.14.4 r2b invoke; kb/Work PB61): <paramref name="FixedWidth"/> plus
/// data-name-1's value × <paramref name="ElemWidth"/>, the value clamped to [<paramref name="MinOccurs"/>,
/// <paramref name="MaxOccurs"/>] with EC-BOUND-ODO set outside them (§13.18.38.4 GR7). A scale-0 integer,
/// runtime-sourced from <paramref name="Depending"/> (data-name-1's place). Produced only by the LENGTH /
/// BYTE-LENGTH variable-length-group builder; the backend renders <c>CobolTable.OdoExtent</c> — the SAME helper
/// the group's sending image slices with, so the two extents cannot disagree.</summary>
public sealed record BoundOdoExtent(Place Depending, int MinOccurs, int MaxOccurs, int FixedWidth, int ElemWidth) : BoundExpr
{
    /// <summary>The BASED entry's data-address field (<c>__addr_X</c>) when the group is a BASED record (kb/Work PB80):
    /// §15.50.4 r4a / §15.14.4 r2a — "if argument-1 is a based entry not associated with actual data … the length
    /// of argument-1 is determined in accordance with the rules of the OCCURS clause for a receiving data item", i.e.
    /// GR8b's MAXIMUM; only an ASSOCIATED entry reads data-name-1 (r4b). The renderer tests the pointer for null
    /// BEFORE anything under the entry is dereferenced (a null data address traps in <c>CobolPtr.Deref</c>).</summary>
    public string? BasedAddress { get; init; }
}

/// <summary>An operand the binder could not give a meaning to — the backend emits a loud runtime guard (§1.4).
/// ⛔ THE WRONG USE IS UNREPRESENTABLE (kb/Work PB1029): the constructor is private, and the node is obtainable
/// only as a <see cref="Refused"/> (the SOURCE is wrong and the site reported the rule — the one statement funnel
/// fails the compile with COBOLNET2362 if the statement drew no error) or as an <see cref="Unbuilt"/> shape
/// (COBOL.NET's gap, announced by that funnel as COBOLNET1756). Either way it can never compile SILENTLY into a
/// run-time abort, which is what a STRING sending ALL literal (§14.9.43.3 SR2) did before.</summary>
public sealed record BoundExprError : BoundExpr
{
    /// <summary>What could not be bound — the text of the run-time guard.</summary>
    public string Feature { get; }

    /// <summary>True for a shape COBOL.NET has not built; false for a refusal of the source.</summary>
    public bool IsUnbuilt { get; }

    private BoundExprError(string feature, bool unbuilt) { Feature = feature; IsUnbuilt = unbuilt; }

    /// <summary>The source is wrong and the caller has reported the rule it breaks.</summary>
    public static BoundExprError Refused(EditionContext edition, string feature)
    {
        edition.NoteRefusal(Editions.Diagnostics.DiagnosticCatalog.UnreportedRefusal, feature);
        return new(feature, unbuilt: false);
    }

    /// <summary>Report <paramref name="message"/> under <paramref name="rule"/> and return the refusal — the operand
    /// twin of <see cref="BoundRejected.Report(EditionContext, Editions.Diagnostics.DiagnosticDescriptor, string)"/>.</summary>
    public static BoundExprError Report(EditionContext edition, Editions.Diagnostics.DiagnosticDescriptor rule,
        string message, string feature)
    {
        edition.Error(rule, message);
        return Refused(edition, feature);
    }

    /// <summary>A legal shape COBOL.NET has not built — announced by the statement funnel (COBOLNET1756).</summary>
    public static BoundExprError Unbuilt(EditionContext edition, string feature)
    {
        edition.NoteUnbuilt(feature);
        return new(feature, unbuilt: true);
    }

    /// <summary>The SAME failure carried into another value channel — already on the ledger, so not re-noted.</summary>
    public static BoundExprError Carry(string feature, bool unbuilt) => new(feature, unbuilt);
}

/// <summary>The EXPRESSION twin of <see cref="BoundUdfEvaluated"/>: an operand carrying the function
/// <paramref name="Activations"/> its <paramref name="Inner"/> expression consumes, for an operand window the
/// statement evaluates REPEATEDLY — each activation runs once per evaluation of THAT window, not once per
/// statement (ISO §8.4.3.2.4 GR1/GR6a "the value … is determined when the function is referenced at runtime";
/// §8.8.4.13 r2). The windows that produce it today are §14.9.28.4 GR12's: "Item identification for
/// identifier-3, identifier-4, identifier-6, identifier-7, index-name-2, and index-name-4 is done each time the
/// content … is used in a setting or augmenting operation" — a PERFORM VARYING BY operand (per augment) and an
/// AFTER level's FROM operand (per outer-augment re-initialization). kb/Work PB437.
/// <para>THE CONSUMER EMITS THE ACTIVATIONS AS STATEMENTS, IT DOES NOT RENDER AN IIFE. <see cref="BoundUdfEvaluated"/>
/// must be an immediately-invoked <c>Func&lt;bool&gt;</c> because a condition sits in a C# loop HEADER where no
/// statement can precede it per iteration; the set/augment windows ARE statement positions, so the consumer
/// (<c>ControlFlowEmitter.RenderPerEvaluation</c>) emits each activation through the ONE statement emitter
/// immediately before the operation that reads the value — once per operation. Either way the activation is
/// <c>InExpression</c>, and a RESUME for a condition it propagates leaves through the carrying statement's
/// <see cref="BoundActivationSite"/> (kb/Work PB892).</para>
/// <para>Like its condition twin, <paramref name="Activations"/> is <see cref="BoundStatement"/> so it carries
/// BOTH pending pre-op kinds: a user-function activation and a §15.4 function-bearing-subscript temporary
/// store (kb/Work PB17). A rendering site that does NOT own the window fails LOUD rather than dropping the
/// activations (<c>NumericRenderer.Visit</c>).</para></summary>
public sealed record BoundUdfEvaluatedExpr(IReadOnlyList<BoundStatement> Activations, BoundExpr Inner) : BoundExpr;

/// <summary>A resolved intrinsic-function call (ISO §15; COBOLNET_INTRINSICS_DESIGN D2): the catalog row (already
/// category-resolved for the polymorphic MAX/MIN families) plus the typed bound arguments — table(ALL) expanded,
/// the §15.68.3 r3 default currency injected — never a pre-rendered C# fragment. <paramref name="Args"/> are
/// <see cref="BoundOperand"/>s, NOT <see cref="BoundExpr"/>s: the string-argument functions (NUMVAL, ORD,
/// LOWER-CASE …) take alphanumeric operands the expression tree cannot represent; numeric argument expressions
/// wrap as <see cref="BoundComputedOperand"/> (the documented deviation from the deep-dive's original sketch —
/// recorded there). <paramref name="Collate"/> marks a CHAR/ORD call bound under a NON-identity PROGRAM COLLATING
/// SEQUENCE (§15.15.4 r2 / §15.70.4) — the backend then passes its collating-weights table; when false the field
/// does not even exist (hazard H5).</summary>
public sealed record BoundIntrinsicCall(
    IntrinsicSig Sig, IReadOnlyList<BoundOperand> Args, PicCategory ResultCategory, bool Collate = false) : BoundExpr
{
    /// <summary>CHAR-NATIONAL / ORD-over-a-national-argument bound under a NON-native NATIONAL program collating
    /// sequence (an <c>ALPHABET … FOR NATIONAL</c> literal phrase; §15.16.4 / §15.70.4 r2) — the backend then
    /// passes the emitted <c>__COLLATE_NAT</c> table; when false the field does not even exist (the H5 twin).
    /// Never true together with <see cref="Collate"/> — each call reads exactly one class's sequence.</summary>
    public bool CollateNat { get; init; }

    /// <summary>A reference modification applied to this function's RESULT — <c>FUNCTION CURRENT-DATE (1:4)</c>,
    /// <c>FUNCTION UPPER-CASE("abc") (1:2)</c>, and their keyword-omitted twins (fix-queue PB8). ISO §8.4.3.1.2
    /// Format 3 composes an identifier from an identifier plus a reference-modifier, and §8.4.3.3.3 SR2 admits a
    /// function-identifier as identifier-1; §8.4.3.1.4 GR1 (f)→(g) fixes the order — the argument list binds to
    /// the name first, THEN the reference modifier applies to the identifier on its left.
    /// <para>⛔ A RIDER ON THIS NODE, NOT A WRAPPER AROUND IT, and that is a deliberate structural choice. The
    /// alphanumeric string channel is selected by pattern-matching <c>BoundComputedOperand { Expr:
    /// BoundIntrinsicCall }</c> at several sites (<c>OperandText.AsString</c>, the nested-argument visitor,
    /// <c>IntrinsicArgumentRules</c>, <c>EcBinder</c>); a wrapper node would have silently stopped matching at
    /// every one of them, and the failure mode is not a compile error but a DROPPED ref-mod — the exact silent
    /// wrong answer this fix exists to remove. The rider also keeps <see cref="ResultCategory"/> correct with no
    /// extra rule: §8.4.3.3.4 GR6 preserves class and category for the three categories SR2 admits (alphanumeric,
    /// boolean, national), so a ref-modified result has the SAME category as the unmodified one.</para>
    /// <para>Null for every unmodified call, which is nearly all of them. SR2 confines this to alphanumeric /
    /// boolean / national functions — all of which render through the ONE <c>IntrinsicRenderer.RenderString</c> —
    /// so exactly one emit site has to honour it, and a numeric-result call can never carry one.</para></summary>
    public RefModSpec? RefMod { get; init; }

    /// <summary>EXCEPTION-FILE / EXCEPTION-FILE-N with a file-connector-name argument (§15.28.4 r2 / §15.29.4 r2,
    /// COBOL-2023): the resolved FD <see cref="FileModel"/> the function reports the I-O status of. Non-null only
    /// for the arg form; the renderer passes its <c>FileKeyExpr</c> so the runtime reads the NAMED connector's
    /// status (not the last exception's). Null for the no-argument form and every other function.</summary>
    public FileModel? FileArg { get; init; }

    /// <summary>TRIM (§15.96.4): 0 = both leading and trailing (rule 3), 1 = LEADING (rule 1), 2 = TRAILING
    /// (rule 2) — the LEADING/TRAILING phrase keyword, extracted at bind time. Zero for every other function.</summary>
    public int TrimMode { get; init; }

    /// <summary>STANDARD-COMPARE (§15.85.2's optional <c>ordering-name-1</c>): the DECODED literal-9 of the
    /// SPECIAL-NAMES ORDER TABLE clause the name resolves to (§12.3.7.4 GR17 — literal-9 identifies the cultural
    /// ordering table), captured at bind time from <c>DataBinder.OrderTables</c>. <see langword="null"/> when no
    /// ordering-name was written, which §15.85.3 r5 defines as "the default ordering table 'ISO
    /// 14651_2020_TABLE1' … shall be used", and for every other function.
    /// <para>⚠ The NAME is not carried: §12.3.7.3 SR9 confines an ordering-name to this one function, so it
    /// identifies nothing outside the SPECIAL-NAMES paragraph, and a bound node carries complete semantics
    /// (the backend never asks the configuration model anything).</para></summary>
    public string? OrderingTable { get; init; }

    /// <summary>The LOCALE functions (§15.51–§15.54; kb/Work PB64 T4): the optional <c>locale-name-1</c> resolved at
    /// bind to the ONE <see cref="LocaleRef"/> — <see cref="LocaleRef.Current"/> when no name was written (§14.6.6 r7/r8:
    /// the locale current for LC_COLLATE / LC_TIME at use), the named symbol otherwise (its L1-normalized tag travels
    /// to the runtime; availability is decided at use — EC-LOCALE-MISSING). <see cref="LocaleRef.Current"/> for every
    /// other function.</summary>
    public LocaleRef Locale { get; init; } = LocaleRef.Current;

    /// <summary>NUMVAL-C / TEST-NUMVAL-C: the LOCALE KEYWORD was written (§15.68.2 — <c>argument-1 LOCALE
    /// [locale-name-1]</c>, the §15.68.3 r5 arm). ⛔ Needed because <see cref="Locale"/> DEFAULTS to
    /// <see cref="LocaleRef.Current"/> on every node, so a bare <c>LOCALE</c> (r5a's current-locale form) and NO
    /// phrase at all (the r3/r4 arm — a DIFFERENT accepted language) would reach the renderer identically
    /// (kb/Work PB64 T6). The case functions dodge this only because their phrase requires a name.</summary>
    public bool LocaleWritten { get; init; }

    /// <summary>FIND-STRING (§15.37.2): the <c>LAST</c> phrase keyword — seek the LAST occurrence of argument-2
    /// (rule 1) rather than the first. False for every other function.</summary>
    public bool FindLast { get; init; }

    /// <summary>FIND-STRING (§15.37.2 / .4 rule 4): the <c>ANYCASE</c> phrase keyword — case-insensitive matching
    /// (as if both arguments were lowered per LOWER-CASE). False for every other function.</summary>
    /// <summary>The ANYCASE phrase was present — FIND-STRING's case-folded search (§15.37.4 r4) and the
    /// NUMVAL-C / TEST-NUMVAL-C case-folded currency match (§15.68.3 r4f) share the ONE flag.</summary>
    public bool Anycase { get; init; }

    /// <summary>SUBSTITUTE (§15.87.2): one mode flag per (argument-2, argument-3) pair — bit 0 = FIRST (rule 3.a),
    /// bit 1 = LAST (rule 3.b), bit 2 = ANYCASE (rule 5); 0 = replace ALL occurrences. <see cref="Args"/> holds
    /// [argument-1, from₁, to₁, from₂, to₂, …]; this list has one entry per pair. Null for every other function.
    /// <para>When <see cref="SubstituteFlat"/> is set (kb/Work PB81 — a table(ALL) argument among the pairs, §15.3),
    /// the pairing is a RUN-TIME fact: <see cref="Args"/> holds [argument-1, part₁, part₂, …] where a part is a written
    /// operand or a table(ALL) enumeration, and this list has one flag PER PART — the keywords preceding it, which
    /// attach to the pair the part's FIRST element starts (a written operand is its own first element).</para></summary>
    public IReadOnlyList<int>? SubstituteModes { get; init; }

    /// <summary>SUBSTITUTE with a table(ALL) argument (kb/Work PB81): the argument-2 / argument-3 pairs are formed at run
    /// time from the enumerated elements (<c>CobolIntrinsics.SubstituteFlat</c>); see <see cref="SubstituteModes"/>.</summary>
    public bool SubstituteFlat { get; init; }

    /// <summary>FUNCTION LENGTH measured in BYTES over the argument's storage image (kb/Work PB61): §15.50.4 r6
    /// gives a DYNAMIC LENGTH elementary item its "current length … in bytes" — for a national item 2 × its
    /// character positions — where every other LENGTH runtime shape is a character-position count over the string
    /// image. The renderer's Length arm reads the storage channel when this is set.</summary>
    public bool LengthInBytes { get; init; }

    /// <summary>CONCAT (§15.18.4 r3): the returned value is ALPHABETIC — argument-1 is usage display and every
    /// argument is class alphabetic (a PIC A item, or a nested CONCAT carrying this same rider). The RESULT
    /// CATEGORY stays <see cref="PicCategory.Alphanumeric"/> — the deliberate PIC A fold (PicInfo) is not
    /// reopened, because this is per-CALL derived state, not a declarable category, and no §15.3 argument rule
    /// admits class alphabetic — so the rider carries Table 16's finer row to its ONE consumer
    /// (<c>Table16Operand</c>, whose IsAlphabetic axis already exists; fix-queue PB59 family 7 /
    /// RV-15.18.4-3). False for every other function.</summary>
    public bool ResultIsAlphabetic { get; init; }

    /// <summary>CONVERT (§15.19.2) source-format: 0 = ANY, 1 = ANUM/ALPHANUMERIC, 2 = HEX, 3 = NAT/NATIONAL.
    /// Zero for every other function (== ANY, unused there).</summary>
    public int ConvertSource { get; init; }

    /// <summary>CONVERT (§15.19.2) destination base-format: 1 = ANUM, 3 = NAT, 4 = BYTE. Zero for every other
    /// function.</summary>
    public int ConvertDest { get; init; }

    /// <summary>CONVERT (§15.19.2): the HEX destination modifier (§15.19.4 r2/r4). False for every other function.</summary>
    public bool ConvertDestHex { get; init; }

    /// <summary>MODULE-NAME (§15.65.2): the keyword selector — 0 = CURRENT (r7), 1 = ACTIVATING (r5/r6),
    /// 2 = NESTED (r8), 3 = STACK (r9), 4 = TOP-LEVEL (r10). Zero for every other function.</summary>
    public int ModuleNameKind { get; init; }
}

// ── General operands (DISPLAY / MOVE source / comparison) — render as string or number per context ─────────────

/// <summary>A bound operand usable where either a string image or a numeric value may be required.</summary>
[BoundNode]
public abstract record BoundOperand;

/// <summary>A non-numeric literal, already decoded to its character value. <paramref name="Category"/> carries
/// the literal's data category — Alphanumeric for a plain <c>"…"</c>, National for <c>N"…"</c> (§8.3.3.5),
/// Boolean for <c>B"…"</c> (§8.3.3.4) — the ONE literal node for all three (feedback_one_mechanism_per_job); the
/// category drives MOVE legality (§14.9.25.3 Table 16), relation-class checks, and store fills.</summary>
public sealed record BoundStringLiteral(string Value) : BoundOperand
{
    /// <summary>The literal's data category (default Alphanumeric — every pre-2002 site is untouched).</summary>
    public PicCategory Category { get; init; } = PicCategory.Alphanumeric;
}

/// <summary>A numeric literal operand, kept as raw source text.</summary>
public sealed record BoundNumericLiteral(string Text) : BoundOperand;

/// <summary>A reference to a data item (its category decides string-vs-numeric rendering).</summary>
public sealed record BoundFieldOperand(Place Place) : BoundOperand;

/// <summary>
/// THE CURRENT RECORD — the sending operand of the implicit MOVE a <c>READ … INTO</c> / <c>RETURN … INTO</c>
/// performs. ISO §14.9.30.4 GR4 b) and §14.9.34.4 GR5 b) say it in one sentence each: "The current record is
/// moved from the record area to the area specified by identifier-1 according to the rules for the MOVE
/// statement without the CORRESPONDING phrase. The size of the current record is determined by rules specified
/// in [for] the RECORD clause. If the file description entry contains a RECORD IS VARYING clause, the implied
/// move is an alphanumeric group move."
/// <para>⛔ THE SENDER IS THE CURRENT RECORD, NOT THE RECORD AREA. For a FORMAT 2 RECORD clause those differ:
/// §13.18.43.4 GR16 fixes the sending size at "the number of bytes in the current record that participate as the
/// sending operands in the implicit MOVE statement" — GR16 a) the content of data-name-1 when the DEPENDING ON
/// phrase is present, GR16 b) otherwise "the value that would have been moved into the data item referenced by
/// data-name-1 had data-name-1 been specified" (the just-read length, GR15). Sending the whole padded area
/// instead is invisible to a left-justified receiver (both space-fill on the right) and DESTROYS the data for a
/// JUSTIFIED one, whose §13.18.32.4 GR1 truncation takes the LEFTMOST characters — kb/Work PB339.</para>
/// <para>This node is STRUCTURAL (the G4 invariant): the record-area <paramref name="Area"/>, the file whose
/// connector or sort store holds the length, the resolved DEPENDING place when GR16 a) applies, and the GR4 b)
/// group-move designation. The backend renders the slice and the length expression
/// (<c>CodeGen.OperandText</c>); nothing here is C# text. A fixed-length (FORMAT 1) file needs no such node —
/// its current record IS the record area (§13.18.43.4 GR6) — so those sites keep a plain
/// <see cref="BoundFieldOperand"/>.</para>
/// </summary>
/// <param name="Area">The FD/SD record area the record was made available in (<c>FileModel.AreaRecord</c>'s
/// resolved place — the LARGEST record's view, §13.18.33.4 GR3 / §13.18.43.4 GR5 a)).</param>
/// <param name="File">The file/sort-file whose current-record length is read at execution: a connector's
/// last-read length, or — when <c>FileModel.IsSortMerge</c> — the sort store's last-returned length.</param>
/// <param name="Depending">The resolved <c>RECORD VARYING … DEPENDING ON</c> item, or <see langword="null"/>
/// when the phrase is absent. Present ⇒ §13.18.43.4 GR16 a); absent ⇒ GR16 b).</param>
/// <param name="AlphanumericGroupMove">§14.9.30.4 GR4 b) / §14.9.34.4 GR5 b)'s explicit designation — true iff
/// the file description entry contains a RECORD IS VARYING clause (FORMAT 2). It makes the implied move a
/// §14.9.25.4 GR4 group move WHATEVER the record area's own level structure: no conversion, no editing, the
/// receiving area "filled without consideration for the individual elementary or group items".</param>
public sealed record BoundCurrentRecord(Place Area, FileModel File, Place? Depending, bool AlphanumericGroupMove)
    : BoundOperand;

/// <summary>A computed numeric expression used as an operand (e.g. a comparison operand <c>A + B</c>).</summary>
public sealed record BoundComputedOperand(BoundExpr Expr) : BoundOperand;

/// <summary>A figurative constant operand (ISO §8.3.3.6). <paramref name="Kind"/> ∈ {Z=ZERO, S=SPACE, H=HIGH-VALUE,
/// L=LOW-VALUE, Q=QUOTE, N=NULL}; its value is materialized against the receiving / other operand's category and
/// width (a single occurrence in DISPLAY, the receiver width in MOVE, the other operand's width in a comparison).</summary>
public sealed record BoundFigurative(char Kind) : BoundOperand;

/// <summary>The figurative <c>ALL "literal"</c> (ISO §8.3.3.6.4 Format 6): the multi-character <paramref name="Literal"/>
/// repeated to the associated width (the receiver in a MOVE, the other operand in a comparison — GR2) or used once in a
/// length-unspecified context such as DISPLAY (GR3c).</summary>
public sealed record BoundAllLiteral(string Literal) : BoundOperand
{
    /// <summary>True when the literal is one or more digit characters — the shape of ISO §14.9.25.3 SR5's sole
    /// surviving figurative→numeric MOVE ("an ALL "literal" figurative constant (containing only digits) … to an
    /// integer numeric item"). The ONE definition both the binder's edition gates and the emitter's value/image
    /// split consult (feedback_one_mechanism_per_job).</summary>
    public bool IsDigitOnly => Literal.Length > 0 && Literal.All(c => c is >= '0' and <= '9');

    /// <summary>The literal's data category — literal-1's own (§8.3.3.6.3 SR2 admits an alphanumeric, boolean or
    /// national literal-1; §14.9.25.4 GR7 / Table 17 give the ALL figurative that category): Alphanumeric for
    /// <c>ALL "…"</c> / <c>ALL X"…"</c>, National for <c>ALL N"…"</c> / <c>ALL NX"…"</c>, Boolean for
    /// <c>ALL B"…"</c> / <c>ALL BX"…"</c>. Set through <see cref="Of"/> — the ONE constructor from source text.</summary>
    public PicCategory Category { get; init; } = PicCategory.Alphanumeric;

    /// <summary>False for a BARE symbolic-character figurative (§8.3.3.6.2 Format 7 without ALL — kb/Work PB110):
    /// its VALUE semantics are the ALL-literal fill (§8.3.3.6.4 GR2/GR10 — one node carries both), but the syntax
    /// screens that bar "a figurative constant that begins with the word ALL" (STRING/UNSTRING §14.9.43.3 SR2 /
    /// §14.9.48.3 SR5; INSPECT §14.9.22.3 SR3) key on THIS — the word, not the semantics.</summary>
    public bool BeginsWithAll { get; init; } = true;

    /// <summary>THE constructor from the literal's RAW source text (kb/Work PB71): the value through
    /// <see cref="CobolNet.Common.CobolLiteral.Decode"/>, the category through the ONE literal-class classifier
    /// (<see cref="CobolNet.Common.CobolLiteral.ClassOf"/>). Every producer of an ALL literal from raw VALUE text —
    /// INITIALIZE REPLACING, a Report Writer VALUE — builds through this, so a national or boolean literal-1 keeps
    /// its category everywhere; the raw text is already ONE literal (the VALUE reader folds a concatenated or
    /// constant-name literal-1 first, <c>DataBinder.RawValueOperandText</c>). The figurative-constant binder builds
    /// from the parse tree through the <see cref="ConcatFolder.Folded"/> overload.</summary>
    public static BoundAllLiteral Of(string rawLiteral) =>
        new(CobolNet.Common.CobolLiteral.Decode(rawLiteral))
        {
            Category = CobolNet.Common.CobolLiteral.ClassOf(rawLiteral) switch
            {
                CobolNet.Common.LiteralClass.National => PicCategory.National,
                CobolNet.Common.LiteralClass.Boolean => PicCategory.Boolean,
                _ => PicCategory.Alphanumeric,
            },
        };

    /// <summary>The ALL figurative over a literal-1 already folded by <see cref="CobolNet.Binding.ConcatFolder.FoldAllLiteral"/>
    /// — the figurative-constant binder's constructor, which carries the fold's class and decoded value as they are
    /// (a concatenated literal-1's class is the pairwise §8.8.3.3 GR1 fold, never its first operand's prefix).</summary>
    internal static BoundAllLiteral Of(CobolNet.Binding.ConcatFolder.Folded literal1) =>
        new(literal1.Value) { Category = literal1.Category };
}

/// <summary>An operand the binder could not give a meaning to — the backend emits a loud runtime guard (§1.4).
/// ⛔ THE WRONG USE IS UNREPRESENTABLE (kb/Work PB1029): the constructor is private, and the node is obtainable
/// only as a <see cref="Refused"/> (the SOURCE is wrong and the site reported the rule — the one statement funnel
/// fails the compile with COBOLNET2362 if the statement drew no error) or as an <see cref="Unbuilt"/> shape
/// (COBOL.NET's gap, announced by that funnel as COBOLNET1756). Either way it can never compile SILENTLY into a
/// run-time abort, which is what a STRING sending ALL literal (§14.9.43.3 SR2) did before.</summary>
public sealed record BoundOperandError : BoundOperand
{
    /// <summary>What could not be bound — the text of the run-time guard.</summary>
    public string Feature { get; }

    /// <summary>True for a shape COBOL.NET has not built; false for a refusal of the source.</summary>
    public bool IsUnbuilt { get; }

    private BoundOperandError(string feature, bool unbuilt) { Feature = feature; IsUnbuilt = unbuilt; }

    /// <summary>The source is wrong and the caller has reported the rule it breaks.</summary>
    public static BoundOperandError Refused(EditionContext edition, string feature)
    {
        edition.NoteRefusal(Editions.Diagnostics.DiagnosticCatalog.UnreportedRefusal, feature);
        return new(feature, unbuilt: false);
    }

    /// <summary>Report <paramref name="message"/> under <paramref name="rule"/> and return the refusal — the operand
    /// twin of <see cref="BoundRejected.Report(EditionContext, Editions.Diagnostics.DiagnosticDescriptor, string)"/>.</summary>
    public static BoundOperandError Report(EditionContext edition, Editions.Diagnostics.DiagnosticDescriptor rule,
        string message, string feature)
    {
        edition.Error(rule, message);
        return Refused(edition, feature);
    }

    /// <summary>A legal shape COBOL.NET has not built — announced by the statement funnel (COBOLNET1756).</summary>
    public static BoundOperandError Unbuilt(EditionContext edition, string feature)
    {
        edition.NoteUnbuilt(feature);
        return new(feature, unbuilt: true);
    }

    /// <summary>The SAME failure carried into another value channel — already on the ledger, so not re-noted.</summary>
    public static BoundOperandError Carry(string feature, bool unbuilt) => new(feature, unbuilt);
}

// ── Boolean expressions (ISO §8.8.2; Phase-4 track (a) increment 2) — a SEPARATE value channel from the numeric
//    BoundExpr and the DISPLAY/MOVE BoundOperand: a boolean value IS a '0'/'1' string (D-B1), combined by the
//    B-AND/B-OR/B-XOR/B-NOT operators. It never enters the numeric channel (NumericRenderer) or the string
//    channel (OperandText) — the emitter routes it through BooleanRenderer over the runtime CobolBool. ─────────

/// <summary>A bound boolean expression (COBOLNET_DESIGN §11 / ISO §8.8.2).</summary>
[BoundNode]
public abstract record BoundBoolExpr;

/// <summary>A boolean literal <c>B"1010"</c>, decoded to its '0'/'1' bit string.</summary>
public sealed record BoundBoolLiteral(string Bits) : BoundBoolExpr;

/// <summary>A reference to a category-boolean data item (including a static ref-mod of one).</summary>
public sealed record BoundBoolRef(Place Place) : BoundBoolExpr;

/// <summary>A BOOLEAN-result function reference as a boolean-expression operand (ISO §8.8.2 — "an identifier
/// referencing a boolean data item": §8.4.3.1.2 makes a function-identifier an identifier and §8.4.3.2.4 GR1 a
/// reference to a temporary data item, whose class/category is the function's type — §15.13.1 BOOLEAN-OF-INTEGER;
/// kb/Work PB68). Renders as the call's '0'/'1' string image through the ONE string channel.</summary>
public sealed record BoundBoolCall(BoundIntrinsicCall Call) : BoundBoolExpr;

/// <summary>The figurative <c>ALL B"…"</c> (and figurative ZERO, normalized to <c>ALL B"0"</c> at bind) — a
/// positionless pattern that materializes to the OTHER operand's length (ISO §8.3.3.6.4 GR2). <c>B-NOT ALL …</c>
/// constant-folds to the flipped pattern at bind (ALL is positionless).</summary>
/// <summary>A positionless boolean value — the figurative ZERO ([ALL] ZERO, §8.3.3.6.2 Format 1), the
/// Format-6 <c>ALL B"…"</c> literal, or a B-NOT fold of either. <paramref name="IsAllLiteral"/> is true ONLY
/// for a source-written Format-6 ALL literal — the operand §8.8.2 rules 4/5 and §14.9.8.3 SR3 restrict.
/// Figurative ZERO is a DISJOINT §8.8.2 alternative (its ALL is Format 1's optional word, §8.3.3.6.3 SR2
/// excludes figuratives from Format 6), and a fold result is no longer "the ALL literal" the source wrote —
/// conflating them rejected `COMPUTE B = ZERO` and `= ALL ZERO` and `= B-NOT ZERO` (kb/Work PB157).</summary>
public sealed record BoundBoolAll(string Bits, bool IsAllLiteral = false) : BoundBoolExpr;

/// <summary>A binary boolean operation (<paramref name="Op"/> ∈ <c>'&amp;'</c> B-AND / <c>'|'</c> B-OR /
/// <c>'^'</c> B-XOR), positionwise with rule-9 right-zero-extension and rule-10 result length (§8.8.2).</summary>
public sealed record BoundBoolBinary(BoundBoolExpr Left, char Op, BoundBoolExpr Right) : BoundBoolExpr;

/// <summary>Boolean negation (B-NOT) — length preserved (ISO §8.8.2 rule 10).</summary>
public sealed record BoundBoolNot(BoundBoolExpr Operand) : BoundBoolExpr;

/// <summary>The four boolean shift operators (ISO §8.8.2, COBOL-2023): logical (fill boolean 0) or circular
/// (rotate), left or right.</summary>
public enum BoolShiftKind { Left, Right, LeftCircular, RightCircular }

/// <summary>A boolean shift/rotate (ISO §8.8.2 rule 8, COBOL-2023): shift <paramref name="Operand"/> by the
/// integer <paramref name="Count"/>. The result length equals the operand's length (rule 9 — the count contributes
/// no positions). NOTE the rule-7b context-sensitive precedence (a shift inheriting a preceding B-OR/B-XOR's
/// precedence) is a documented refinement — the grammar binds the shift tighter than B-AND (the unmixed default
/// case, which the Annex A Table A.2 oracle exercises).</summary>
public sealed record BoundBoolShift(BoundBoolExpr Operand, BoolShiftKind Kind, BoundExpr Count) : BoundBoolExpr;

/// <summary>A boolean expression the binder could not give a meaning to — the backend emits a loud runtime guard (§1.4).
/// ⛔ THE WRONG USE IS UNREPRESENTABLE (kb/Work PB1029): the constructor is private, and the node is obtainable
/// only as a <see cref="Refused"/> (the SOURCE is wrong and the site reported the rule — the one statement funnel
/// fails the compile with COBOLNET2362 if the statement drew no error) or as an <see cref="Unbuilt"/> shape
/// (COBOL.NET's gap, announced by that funnel as COBOLNET1756). Either way it can never compile SILENTLY into a
/// run-time abort, which is what a STRING sending ALL literal (§14.9.43.3 SR2) did before.</summary>
public sealed record BoundBoolError : BoundBoolExpr
{
    /// <summary>What could not be bound — the text of the run-time guard.</summary>
    public string Feature { get; }

    /// <summary>True for a shape COBOL.NET has not built; false for a refusal of the source.</summary>
    public bool IsUnbuilt { get; }

    private BoundBoolError(string feature, bool unbuilt) { Feature = feature; IsUnbuilt = unbuilt; }

    /// <summary>The source is wrong and the caller has reported the rule it breaks.</summary>
    public static BoundBoolError Refused(EditionContext edition, string feature)
    {
        edition.NoteRefusal(Editions.Diagnostics.DiagnosticCatalog.UnreportedRefusal, feature);
        return new(feature, unbuilt: false);
    }

    /// <summary>Report <paramref name="message"/> under <paramref name="rule"/> and return the refusal — the operand
    /// twin of <see cref="BoundRejected.Report(EditionContext, Editions.Diagnostics.DiagnosticDescriptor, string)"/>.</summary>
    public static BoundBoolError Report(EditionContext edition, Editions.Diagnostics.DiagnosticDescriptor rule,
        string message, string feature)
    {
        edition.Error(rule, message);
        return Refused(edition, feature);
    }

    /// <summary>A legal shape COBOL.NET has not built — announced by the statement funnel (COBOLNET1756).</summary>
    public static BoundBoolError Unbuilt(EditionContext edition, string feature)
    {
        edition.NoteUnbuilt(feature);
        return new(feature, unbuilt: true);
    }

    /// <summary>The SAME failure carried into another value channel — already on the ledger, so not re-noted.</summary>
    public static BoundBoolError Carry(string feature, bool unbuilt) => new(feature, unbuilt);
}

/// <summary>A boolean expression used as a RELATION operand (ISO §8.8.4.2.2) — the ONE carrier that lets a
/// boolean expression sit in a <see cref="BoundRelational"/> beside another boolean operand (item↔item compares
/// ride the SAME BoundRelational + renderer branch, never a parallel node; feedback_one_mechanism_per_job).</summary>
public sealed record BoundBoolOperand(BoundBoolExpr Expr) : BoundOperand;

// ── Conditions ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound condition — a side-effect-free predicate tree (COBOLNET_DESIGN §11).</summary>
[BoundNode]
public abstract record BoundCondition;

/// <summary>A relational comparison <c>left op right</c> (<paramref name="Op"/> is the mapped C# operator).</summary>
public sealed record BoundRelational(BoundOperand Left, string Op, BoundOperand Right) : BoundCondition;

/// <summary>A logical combination (<c>&amp;&amp;</c> / <c>||</c> / <c>^</c>) of sub-conditions.</summary>
public sealed record BoundLogical(string Op, IReadOnlyList<BoundCondition> Operands) : BoundCondition;

/// <summary>Logical negation.</summary>
public sealed record BoundNot(BoundCondition Operand) : BoundCondition;

/// <summary>A simple boolean condition (ISO §8.8.4.3): a boolean expression of length 1 used as a condition —
/// true iff its value is boolean 1 (GR1). Negation composes via <see cref="BoundNot"/>.</summary>
public sealed record BoundBooleanCondition(BoundBoolExpr Expr) : BoundCondition;

/// <summary>A level-88 condition-name membership test over its (already-resolved) conditional variable place.</summary>
public sealed record BoundCondition88(Place Parent, Condition88 Condition, bool CheckRangeInvalid = false) : BoundCondition;

/// <summary>An alphanumeric/national THRU-range membership test (the EVALUATE WHEN <c>lo THRU hi</c> form, and the
/// carrier for EC-RANGE-INVALID checking): <c>Left</c> is within [<c>Lo</c>, <c>Hi</c>] in the effective collating
/// sequence. When <paramref name="CheckInvalid"/> and <c>lo</c> collates after <c>hi</c> (§14.7.8 rule 2) the nonfatal
/// EC-RANGE-INVALID is set and the range is treated as empty — realized by the runtime <c>CobolString.ThruMember</c>
/// (the empty behaviour is already emergent from the inclusive-bound test, so only the EC-set is added).
/// <para><paramref name="Alphabet"/> is alphabet-name-1 of the range's <c>IN</c> phrase AS WRITTEN, or null when
/// the phrase is absent. §14.7.8 rule 2 makes it the ordering outright — "the collating sequence used for range
/// evaluation is the collating sequence defined by that alphabet" — so it OVERRIDES the implementor default, which
/// for this compiler is the PROGRAM COLLATING SEQUENCE: a range under <c>IN STANDARD-1</c> collates natively even
/// inside a program whose PCS reorders the alphabet. A COBOL NAME rides here, never a generated field: the renderer
/// maps it through <c>DataBinder.RangeCollations</c>.</para>
/// <para>⛔ A RANGE WITH AN <c>IN</c> PHRASE ALWAYS BINDS TO THIS NODE, checked or not, because the alternative
/// lowering — a pair of <c>BoundRelational</c>s — has nowhere to put a per-range collating sequence: a relation's
/// sequence is derived from its operands' categories, which is exactly the rule the IN phrase overrides. The
/// unchecked rendering of this node is the same inclusive <c>Compare</c> pair that lowering produced.</para></summary>
public sealed record BoundRangeMembership(BoundOperand Left, BoundOperand Lo, BoundOperand Hi, bool CheckInvalid,
    string? Alphabet = null) : BoundCondition;

/// <summary>A sign condition: <paramref name="Expr"/> IS [NOT] {POSITIVE | NEGATIVE | ZERO}. <paramref
/// name="Format2Float"/> marks the ISO §8.8.4.7.3 Format 2 form — a bare (unparenthesized) standard
/// floating-point data-name — which tests the IEEE sign BIT (§8.8.4.7.4 GR2: +0.0 IS POSITIVE, −0.0 IS
/// NEGATIVE) rather than the Format-1 algebraic value.</summary>
public sealed record BoundSignCondition(BoundExpr Expr, char Kind, bool Negated, bool Format2Float = false) : BoundCondition;   // Kind: P/N/Z

/// <summary>The §8.8.4.8 omitted-argument condition (kb/Work PB133 wave C; the method arm kb/Work PB757):
/// <c>data-name-1 IS [NOT] OMITTED</c> over a formal parameter of THIS source element — rendered from the
/// formal's <see cref="OmittedProbe"/>, the ONE presence fact (a program/function formal's null carrier, a
/// method formal's presence parameter) that GR1c's transitive forwarding reads too.</summary>
public sealed record BoundOmittedCondition(OmittedProbe Probe, bool Negated) : BoundCondition;

/// <summary>A class condition: <paramref name="Operand"/> IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER |
/// ALPHABETIC-LOWER, BOOLEAN} (ISO §8.8.4.4). <paramref name="ClassKind"/> is one of
/// <see cref="CobolNet.Binding.ClassConditionModel"/>'s kind constants — N, A, U, L, B — which is also the tag
/// its §8.8.4.4.3 operand rules are keyed on. The class-name and alphabet-name alternatives have their own
/// bound nodes (<see cref="BoundUserClassCondition"/>, <see cref="BoundCodedSetClassCondition"/>).</summary>
public sealed record BoundClassCondition(BoundOperand Operand, char ClassKind, bool Negated) : BoundCondition;

/// <summary>A USER-DEFINED class condition (ISO §8.8.4.4 with a SPECIAL-NAMES class-name, §12.3.7): true when
/// the operand consists entirely of <paramref name="Members"/> (the clause's literals expanded at bind time).</summary>
public sealed record BoundUserClassCondition(BoundOperand Operand, string Members, bool Negated) : BoundCondition;

/// <summary>A class condition whose class is an ALPHABET-NAME (ISO §8.8.4.4.4 GR3 a; kb/Work PB109): true when
/// the operand's content consists entirely of characters in the CODED CHARACTER SET the alphabet identifies
/// (§12.3.7.4 GR7 Table 6 — never a LOCALE alphabet, §8.8.4.4.3 SR2). <paramref name="Kind"/> is the runtime
/// membership test (<c>CobolClass.CodedSetKind</c>): Ascii for STANDARD-1/2 (the 128 ISO/IEC 646 IRV characters);
/// ScalarValues for UCS-4 / UTF-8 (every character except an unpaired surrogate); AllNative for NATIVE / UTF-16 /
/// a literal-phrase alphabet (GR7 k4 puts every native character in the set, so the test is TRUE for any content —
/// deliberate, not vacuous: the SET is total even though the SEQUENCE is remapped).</summary>
public sealed record BoundCodedSetClassCondition(BoundOperand Operand, string Kind, bool Negated) : BoundCondition;

/// <summary>A condition the binder could not resolve — the backend emits a loud runtime guard (§1.4).</summary>
public sealed record BoundConditionError(string Feature) : BoundCondition;

/// <summary>A condition carrying the user-defined-function <paramref name="Activations"/> its
/// <paramref name="Inner"/> predicate consumes, for a CONDITIONALLY or REPEATEDLY evaluated window: each
/// activation runs ONCE PER EVALUATION of this condition — a function-identifier "references a temporary data
/// item whose value is determined when the function is referenced at runtime" and its arguments are evaluated
/// "at the beginning of the evaluation of the function-identifier" (ISO §8.4.3.2.4 GR1 :6963 / GR6a :6995), and
/// functions in conditions are evaluated "if and when the conditions containing them are evaluated"
/// (§8.8.4.13 r2). The binder attaches the statement-pending activations here instead of the once-per-statement
/// hoist at every such window: a PERFORM UNTIL / VARYING UNTIL condition (re-evaluated per iteration, §14.9.28),
/// a SEARCH / SEARCH ALL WHEN condition (per pass, §14.9.37), an EVALUATE selection-object term (per WHEN
/// consideration, §14.9.13), and a non-first AND/OR combined-condition operand (§8.8.4.13 r1 short-circuit).
/// The renderer emits an immediately-invoked <c>Func&lt;bool&gt;</c> so the activations execute exactly when the
/// C# condition text evaluates — including inside loop headers and short-circuited <c>&amp;&amp;</c>/<c>||</c>
/// chains. This is the ONE deliberate exception to the "side-effect-free predicate tree" contract above; the
/// activations are the SAME lowering the hoist uses (one mechanism).
/// <para>⚠ <paramref name="Activations"/> is <see cref="BoundStatement"/>, not <see cref="BoundCallProgram"/>,
/// because the statement-pending list it drains from carries BOTH pre-op kinds (see
/// <c>DataBinder.PendingPreOps</c>): a user-function activation AND a D18 function-bearing subscript's §15.4
/// temporary store (fix-queue PB17). The window rule is identical for both — §8.8.4.13 r2 governs "functions",
/// not one spelling of them — so widening the carrier is what makes the subscript case ride the machinery the
/// UDF case already proved, instead of a parallel one.</para></summary>
public sealed record BoundUdfEvaluated(IReadOnlyList<BoundStatement> Activations, BoundCondition Inner)
    : BoundCondition;

// ── Statements ─────────────────────────────────────────────────────────────────────────────────────────────────

/// <summary>A bound statement.</summary>
[BoundNode]
public abstract record BoundStatement;

/// <summary>⛔ A DEFERRAL, AND NOTHING ELSE: a statement whose construct COBOL.NET HAS NOT BUILT. The backend
/// emits a loud runtime guard (§1.4) and <see cref="StatementBinder.BindStatement"/> reports COBOLNET1756 at
/// compile time, so the gap is visible before the program is run.
/// <para>⛔ IT IS NOT THE CARRIER FOR AN ILL-FORMED OPERAND OR AN ILLEGAL PLACEMENT (kb/Work PB236). It used to
/// be all three, and the emitter could not tell them apart, so a violated syntax rule reached the programmer as
/// a claim that THE COMPILER is incomplete when in fact THE SOURCE was wrong — at run time, and on an
/// unexecuted path not at all. A statement whose operand or position the standard forbids reports its own
/// diagnostic (<see cref="Editions.Diagnostics.DiagnosticCatalog.StatementOperandRule"/> or the rule's own
/// descriptor, per ISO §4.2.2 ¶2's compile-time mechanism) and binds to <see cref="BoundRejected"/>; it does NOT
/// come here. If you are about to construct one of these while holding a §/SR citation for why the SOURCE is
/// wrong, that citation is telling you this is the wrong node — build a <see cref="BoundRejected"/> instead.
/// <c>BoundDeferralDriftTests</c> scans every construction site's message for the vocabulary of a violated rule
/// (a syntax-rule number, "shall", "extension", "general format") and fails the build on one (kb/Work PB909).
/// <para>An operand the reference resolver could not bind is the one case a SITE cannot classify — the null means
/// "reported" (an undefined name, a subscript on a non-table) or "a shape the resolver has not built" — so the
/// funnel classifies it: a <c>BoundUnsupported</c> from a statement whose bind already drew an error is a
/// refused statement and announces nothing.</para></summary>
public sealed record BoundUnsupported(string Feature) : BoundStatement;

/// <summary>⛔ A REFUSAL, AND NOTHING ELSE (kb/Work PB909): a statement the SOURCE got wrong — written in a shape no
/// general format prints, or with an operand its syntax rules exclude. It is the other half of the split that
/// <see cref="BoundUnsupported"/> used to carry alone, when an ungrammatical statement and an unbuilt feature both
/// compiled to the same COBOLNET1756 warning and the same run-time abort (INSPECT … REPLACING TRAILING and
/// SEARCH … NOT AT END shipped that way — a warning at compile time, a crash in production, and nothing in the
/// build output that stopped a release).
/// <para>The WRONG USE IS UNREPRESENTABLE: the constructor is private and <see cref="Report"/> is the only way to
/// obtain one, and it records the error (ISO §4.2.2 ¶2's compile-time indication) before it returns — so a refusal
/// can never be silent, and since any error fails the compile before code generation, a refusal can never be
/// compiled into a program either (<c>StatementEmitter</c> throws if one ever arrives).</para></summary>
public sealed record BoundRejected : BoundStatement
{
    /// <summary>The COBOLNET code the refusal was reported under — always an error; null when a CALLEE reported it
    /// (<see cref="Reported"/>).</summary>
    public string? Code { get; }

    private BoundRejected(string? code) => Code = code;

    /// <summary>A refusal whose diagnostic a CALLEE already reported — a file-name or procedure-name resolution, an
    /// operand screen, a placement rule (kb/Work PB1029: ~100 sites wrote <c>return new BoundNop();</c> after such a
    /// call, where nothing checked that the callee had in fact reported). The refusal is put on the ledger, and the
    /// one statement funnel fails the compile with COBOLNET2362 if the statement drew no error.
    /// <para>⛔ NOT for a null from the reference resolver: that null is either reported or a shape COBOL.NET has not
    /// built, and only the funnel can tell which — bind a <see cref="BoundUnsupported"/> there.</para></summary>
    public static BoundRejected Reported(EditionContext edition)
    {
        edition.NoteRefusal(Editions.Diagnostics.DiagnosticCatalog.UnreportedRefusal, "a statement");
        return new BoundRejected((string?)null);
    }

    /// <summary>Report <paramref name="message"/> under <paramref name="rule"/> and return the refusal node. A
    /// warning-severity descriptor is a programming error: a refusal that does not fail the compile would reach
    /// code generation with no meaning to emit.</summary>
    public static BoundRejected Report(EditionContext edition, Editions.Diagnostics.DiagnosticDescriptor rule,
        string message)
    {
        if (rule.Severity != Editions.EditionSeverity.Error)
            throw new InvalidOperationException(
                $"BoundRejected needs an error-severity descriptor; {rule.Code} is {rule.Severity} (kb/Work PB909).");
        edition.Error(rule, message);
        return new BoundRejected(rule.Code);
    }

    /// <summary>The same refusal for a code the site still writes as a bare <c>COBOLNETnnnn</c> literal (kb/Work
    /// PB1029 — the report-then-<see cref="BoundNop"/> sites migrated here keep their codes verbatim). A code the
    /// catalogue DOES carry is held to its descriptor's severity, exactly as the descriptor overload is.</summary>
    public static BoundRejected Report(EditionContext edition, string code, string message)
    {
        if (Editions.Diagnostics.DiagnosticCatalog.All.FirstOrDefault(d => d.Code == code) is { } rule)
            return Report(edition, rule, message);
        edition.Error(code, message);
        return new BoundRejected(code);
    }
}

/// <summary>The STOP RUN / GOBACK termination status phrase (ISO §14.9.42 / §14.9.18.2 — COBOL-2002 on STOP,
/// COBOL-2023 on GOBACK): <c>WITH {ERROR | NORMAL} STATUS [value]</c>. <paramref name="Error"/> selects the OS
/// error-vs-normal termination indication (§14.9.42.4 GR2/GR3 · §14.9.18.4 GR7/GR8); <paramref name="Value"/> is
/// the status VALUE passed to the operating system (§14.9.42.4 GR5 · §14.9.18.4 GR10), null when no <c>STATUS</c>
/// operand is given. On .NET the single observable is the process exit code (<see cref="Runtime.RunUnit.ExitStatus"/>
/// → <c>Environment.ExitCode</c>): the value wins when present, else ERROR ⇒ 1 / NORMAL ⇒ 0 (the documented
/// implementor mapping — <c>docs/CONFORMANCE.md</c> §4.2.16).</summary>
/// <remarks>⛔ <paramref name="Value"/> IS A <see cref="BoundOperand"/>, NOT A <see cref="BoundExpr"/> (kb/Work
/// PB169). §14.9.42.2 writes the operand as <c>{identifier-1 | literal-1}</c> — it is NOT an
/// arithmetic-expression position — and §14.9.42.3 SR2 admits "an integer data item or a data item with usage
/// display or usage national", while SR3's conditional ("If literal-1 IS numeric, it shall be an integer")
/// presupposes the non-numeric form. A <c>BoundExpr</c> carrier could hold none of that: the numeric funnel
/// rejected <c>STOP RUN WITH ERROR STATUS "ABEND"</c> and <c>STATUS &lt;PIC X item&gt;</c> alike with
/// COBOLNET0844, quoting §8.8.1.1 — a rule that does not govern this position — at a programmer who had broken
/// nothing. The operand carrier is the shape the general format actually has.
/// <para>⚠ The widening is EMIT-NEUTRAL for every program that compiled before: for an integer literal
/// <c>Visit(BoundNumericLiteral)</c> and <c>Visit(BoundNumLiteral)</c> both reduce to
/// <c>EmitText.UnscaledLit</c>, and <c>Visit(BoundFieldOperand)</c> and <c>Visit(BoundNumRef)</c> are both
/// <c>FieldNum(Place)</c> — the same text, by construction rather than by inspection.</para></remarks>
public sealed record TerminationStatus(bool Error, BoundOperand? Value);

/// <summary><c>STOP RUN [ WITH {NORMAL|ERROR} STATUS [value] ]</c> (ISO §14.9.42): terminate the run unit,
/// passing <see cref="Status"/> to the operating system as the process exit code. The edition gate
/// (StopRunStatus2002) reads the PARSE tree in the post-bind <see cref="Validation.VersionConformancePass"/>,
/// not this node.</summary>
public sealed record BoundStop(TerminationStatus? Status = null) : BoundStatement;

/// <summary>STOP literal (X3.23-1985 §14 Format 2, deleted 2002; edition-gated ≥2002 by the validator): "the
/// literal is communicated to the operator" and, on resume, "execution continues with the next executable
/// statement". The greenfield realization (implementor latitude on the operator interaction): write the
/// literal to the operator channel — stderr, never the program's output stream — and continue immediately.
/// Replaces the silent bind-as-STOP-RUN mis-bind (the DEVLOG-578 latent bug; P2.6).</summary>
public sealed record BoundStopLiteral(string Text) : BoundStatement;

/// <summary><c>DISPLAY</c> of a sequence of operands (each rendered as its display image). <paramref name="ToStdErr"/>
/// carries the UPON device routing (ISO §14.9.11.3 SR2 / §14.9.11.4 GR8): a mnemonic-name bound to the SYSERR
/// implementor device-name routes to the process standard-error stream; CONSOLE / SYSOUT and the no-UPON default use
/// the standard display device (standard output). Default false keeps the no-UPON emission byte-identical.</summary>
public sealed record BoundDisplay(IReadOnlyList<BoundOperand> Operands, bool NoAdvancing, bool ToStdErr = false) : BoundStatement;

/// <summary>The PHRASE whose own rules make a move — the <c>… FROM</c> of RELEASE / WRITE / REWRITE and the
/// <c>… INTO</c> of READ / RETURN — named so that every consumer of the resulting <see cref="BoundMove"/> (the
/// MOVE binder's syntax screens, <c>VersionConformancePass.GateMove</c>, and any rule written tomorrow) reports
/// the statement the programmer actually WROTE instead of a MOVE they never typed.
/// <para><paramref name="Cite"/> is the clause that imports the MOVE statement's rules into the phrase — ISO
/// §14.9.32.4 GR4 a) for RELEASE, §14.9.51.4 GR5 a) for WRITE, §14.9.30.4 GR4 b) for READ INTO, and so on. It is
/// carried, not re-derived, because the diagnostic has to name the rule that made the phrase a MOVE.</para></summary>
public sealed record ImplicitMovePhrase(string Statement, string Cite)
{
    /// <summary>True when the owning statement's OWN rule asks the §14.9.25.3 validity question for this move, in
    /// its own framing, so <c>MoveBinder.BindMoveOf</c> must not ask it again per receiver. Two statements:
    /// INITIALIZE — §14.9.20.3 SR4 asks it once per REPLACING category (a hypothetical MOVE into "an item of the
    /// specified category"), and the §14.9.20.4 GR6a/GR6c senders are valid by construction — and ACCEPT format 2,
    /// whose §14.9.1.3 SR3 names the refused receiver classes itself (kb/Work PB887). The move is still BOUND —
    /// frozen, classified and storage-marked — which is what building it in the emitter skipped (kb/Work PB880).</summary>
    public bool ValidityAskedByStatement { get; init; }

    /// <summary>INITIALIZE's implicit elementary MOVE — ISO §14.9.20.4 GR4, "Otherwise, the implicit statement is:
    /// MOVE sending-operand TO receiving-operand."</summary>
    public static readonly ImplicitMovePhrase Initialize =
        new("INITIALIZE", "ISO §14.9.20.4 GR4") { ValidityAskedByStatement = true };

    /// <summary>MOVE CORRESPONDING's per-pair move — ISO §14.9.25.4 GR11, "The results are the same as if the user
    /// had referred to each pair of corresponding identifiers in separate MOVE statements." The pair was selected
    /// by §14.7.6 rule 2's own MOVE-validity filter, so the screens pass by construction and are asked anyway.</summary>
    public static readonly ImplicitMovePhrase MoveCorresponding =
        new("MOVE CORRESPONDING", "ISO §14.9.25.4 GR11");

    /// <summary>GOBACK RETURNING's move into the procedure-division RETURNING item — an implementor extension to
    /// §14.9.18 (the ISO general format has no RETURNING phrase); ISO §14.9.18.4 GR2 makes that item's value "the
    /// result of the program activation", and the extension fills it by the MOVE rules.</summary>
    public static readonly ImplicitMovePhrase GobackReturning =
        new("GOBACK … RETURNING", "ISO §14.9.18.4 GR2 (implementor extension)");

    /// <summary>ACCEPT format 2's transfer — ISO §14.9.1.4 GR6, "The ACCEPT statement causes the information
    /// requested to be transferred to the data item specified by identifier-2 according to the rules for the MOVE
    /// statement", the sender being the GR7–GR12 conceptual unsigned integer item of usage display. The statement
    /// asks the validity question ITSELF (<c>AcceptDisplayBinder</c>, framed under §14.9.1.3 SR3 as COBOLNET0818),
    /// because SR3 is ACCEPT's own rule over the same receiver classes Table 16 refuses (kb/Work PB887).</summary>
    public static readonly ImplicitMovePhrase AcceptTemporal =
        new("ACCEPT … FROM DATE/DAY/DAY-OF-WEEK/TIME", "ISO §14.9.1.4 GR6") { ValidityAskedByStatement = true };
    /// <summary>UNSTRING's store of the examined characters into identifier-4 — ISO §14.9.48.4 GR11 c), "shall be
    /// moved into the current receiving area according to the rules for the MOVE statement" (kb/Work PB979). The
    /// validity question is the statement's own: §14.9.48.3 SR4 names the admissible receivers (and SR10 the
    /// groups), and the sender is the conceptual elementary alphanumeric / national item GR11 c) defines, so the
    /// MOVE screens are not asked a second time.</summary>
    public static readonly ImplicitMovePhrase UnstringInto =
        new("UNSTRING … INTO", "ISO §14.9.48.4 GR11 c)") { ValidityAskedByStatement = true };

    /// <summary>UNSTRING's store of the delimiting characters into identifier-5 — ISO §14.9.48.4 GR11 d), the same
    /// sentence for the DELIMITER IN phrase; §14.9.48.3 SR2 is the statement's own validity screen for it.</summary>
    public static readonly ImplicitMovePhrase UnstringDelimiterIn =
        new("UNSTRING … DELIMITER IN", "ISO §14.9.48.4 GR11 d)") { ValidityAskedByStatement = true };

    /// <summary>How ONE receiver of this move names itself in a diagnostic. The name is nullable because a
    /// receiver may be FILLER or an unnamed record area.</summary>
    public string Where(string? receiver) =>
        $"the implicit MOVE of {Statement} to {receiver ?? "the receiving item"} ({Cite})";

    /// <summary>The where-string for a move that may or may not be implicit — the explicit MOVE statement's own
    /// shape when <paramref name="phrase"/> is null. ONE spelling for both, so a screen written for the explicit
    /// statement reports correctly from an implicit phrase without an arm of its own.</summary>
    public static string WhereOf(ImplicitMovePhrase? phrase, string? receiver) =>
        phrase?.Where(receiver) ?? $"MOVE … TO {receiver}";

    /// <summary>The trailing clause a MOVE diagnostic ADDS when its message already names the receiver its own
    /// way — empty for the explicit MOVE statement. The second of this type's two renderings, and both live
    /// here so a screen cannot invent a third spelling of "the programmer did not write MOVE".</summary>
    public static string Via(ImplicitMovePhrase? phrase) =>
        phrase is null ? "" : $" — the implicit MOVE of {phrase.Statement} ({phrase.Cite})";

}

/// <summary>
/// ⛔ <b>THE RULES OF ONE <c>… INTO</c> PHRASE</b> — READ (ISO §14.9.30) and RETURN (§14.9.34), one row each.
/// It is the INTO twin of <c>FromPhraseRules</c>, and it exists for the same reason: each verb states the
/// phrase's syntax rules FOR ITSELF, in wording that DIFFERS, so a scalar column would reject legal source on
/// one arm or wave an illegal program through on the other.
///
/// <para><b>What the two verbs say, verbatim.</b> §14.9.30.3 SR1 admits the phrase <i>"a) If no record
/// description entry or only one record description is subordinate to the file description entry, or b) If the
/// data item referenced by identifier-1 and all record-names associated with file-name-1 describe an
/// alphanumeric group item or an elementary item of category alphanumeric or category national."</i>
/// §14.9.34.3 SR2 is the same rule for RETURN with arm a) reading <i>"If only one record description is
/// subordinate to the sort-merge file description entry"</i>. §14.9.30.3 SR2 adds, for a strongly-typed
/// identifier-1, <i>"there shall be at most one record area subordinate to the FD for file-name-1. This record
/// area, if specified, shall be a strongly-typed group item of the same type as identifier-1"</i>; §14.9.34.3
/// SR3 is its twin with <i>"exactly one record area subordinate to the SD"</i> and no <i>"if specified"</i>.</para>
///
/// <para><b>The ONE difference, and why it is one field rather than two.</b> Both of READ's rules admit a
/// RECORD-LESS description entry and both of RETURN's require a record; that is not two coincidences but one
/// fact about the description entry each verb reads — §13.4.5.3 SR3 permits an FD with no record description
/// entries (which is why <c>MaterializeImpliedRecord</c> exists, kb/Work PB345 / FILES design D18), while
/// §13.4.6.3 SR2 requires at least one under an SD and the binder already rejects an SD without one. So
/// <see cref="AdmitsRecordLessEntry"/> is a property of the ENTRY KIND, carried once and read by both rules.</para>
///
/// <para><b>What is NOT on this row.</b> The second sentence of SR2 / SR3 — <i>"shall be a strongly-typed group
/// item of the same type as identifier-1"</i> — is the same predicate over the same pair that §14.9.25.3 SR2
/// applies to this phrase's implicit-move SENDER, which IS the record area. It is checked once, by
/// <c>StatementValidation.CheckStrongMove</c> (COBOLNET1533), and writing it here too would be one rule in two
/// places (kb/Work PB337).</para>
/// </summary>
/// <param name="Statement">The phrase as the programmer wrote it, for every diagnostic.</param>
/// <param name="MoveRuleCite">The general rule that makes the phrase a MOVE — §14.9.30.4 GR4 b) / §14.9.34.4
/// GR5 b), which are the same sentence twice.</param>
/// <param name="AdmitsRecordLessEntry">True when the verb's description entry may legally have no record
/// description entry at all — an FD (§13.4.5.3 SR3) but not an SD (§13.4.6.3 SR2).</param>
/// <param name="AdmissibilityCite">The verb's own admissibility rule — §14.9.30.3 SR1 / §14.9.34.3 SR2.</param>
/// <param name="AdmissibilityArmA">That rule's arm a), quoted, because the two verbs word it differently and a
/// diagnostic that paraphrases it sends the reader to the wrong sentence.</param>
/// <param name="StrongReceiverCite">The verb's strongly-typed-receiver rule — §14.9.30.3 SR2 / §14.9.34.3 SR3.</param>
/// <param name="StrongReceiverCount">That rule's record-area count requirement, quoted.</param>
/// <param name="EntryFace">How the description entry names itself in a message — "FD" / "SD".</param>
public sealed record IntoPhraseRules(
    string Statement,
    string MoveRuleCite,
    bool AdmitsRecordLessEntry,
    string AdmissibilityCite,
    string AdmissibilityArmA,
    string StrongReceiverCite,
    string StrongReceiverCount,
    string EntryFace)
{
    /// <summary>The <see cref="ImplicitMovePhrase"/> every move of THIS verb's INTO phrase carries — built once
    /// per verb beside the two rows, exactly as <c>FromPhraseRules.Phrase</c> is. A phrase is a property of the
    /// VERB, not of an occurrence of it, so both are singletons.</summary>
    public ImplicitMovePhrase Phrase { get; } = new(Statement, MoveRuleCite);

    /// <summary>The record-area COUNT this verb's admissibility arm a) and strong-receiver rule admit: 1, or
    /// 0 and 1 where the description entry may be record-less. Read by both rules, never re-derived.</summary>
    public bool AdmitsRecordCount(int count) => count == 1 || (count == 0 && AdmitsRecordLessEntry);

    /// <summary><c>READ … INTO</c> — both organizations. ISO §14.9.30.4 GR4 b): "The current record is moved
    /// from the record area to the area specified by identifier-1 according to the rules for the MOVE statement
    /// without the CORRESPONDING phrase."</summary>
    public static readonly IntoPhraseRules Read = new(
        "READ … INTO", "ISO §14.9.30.4 GR4 b)", AdmitsRecordLessEntry: true,
        "ISO §14.9.30.3 SR1",
        "\"If no record description entry or only one record description is subordinate to the file description "
        + "entry\"",
        "ISO §14.9.30.3 SR2",
        "\"there shall be at most one record area subordinate to the FD for file-name-1\"",
        "FD");

    /// <summary><c>RETURN … INTO</c>. ISO §14.9.34.4 GR5 b) — the identical sentence, which is why the two
    /// phrases share one binder and one sender builder rather than one each.</summary>
    public static readonly IntoPhraseRules Return = new(
        "RETURN … INTO", "ISO §14.9.34.4 GR5 b)", AdmitsRecordLessEntry: false,
        "ISO §14.9.34.3 SR2",
        "\"If only one record description is subordinate to the sort-merge file description entry\"",
        "ISO §14.9.34.3 SR3",
        "\"there shall be exactly one record area subordinate to the SD for file-name-1\"",
        "SD");
}

/// <summary><c>MOVE source TO targets</c> (single sending operand) — the explicit MOVE statement, AND the
/// implicit move of every <c>… FROM</c> / <c>… INTO</c> phrase, which <see cref="ImplicitOf"/> identifies.
/// <para>⛔ EVERY BoundMove IS BUILT BY <c>MoveBinder</c>, AT BIND TIME (kb/Work PB348). The implicit ones used
/// to be synthesized in the EMITTER, downstream of every bind-time MOVE screen AND of the storage facts codegen
/// itself consumes — so <c>RELEASE R FROM WS-NUM</c> escaped the §14.9.25.3 Table-16 refusal its explicit twin
/// draws, and <c>RELEASE N FROM QUOTE</c> aborted the RUN because <c>MarkImageForced</c> had never seen the
/// receiver. <c>ImplicitMoveConstructionDriftTests</c> keeps the I-O and sort emitters free of
/// <c>new BoundMove</c>.</para></summary>
public sealed record BoundMove(BoundOperand Source, IReadOnlyList<Place> Targets) : BoundStatement
{
    /// <summary>Per-target stores — the §14.9.25.4 GR2/GR3 SENDING operand for that receiver and the dispatch
    /// kind it classifies to — computed ONCE at construction by the single authority
    /// (<see cref="MoveClassifier"/>, P7 Step 7; kb/Work PB425 added the sender half) — a computed record
    /// property, so EVERY construction (the explicit MOVE statement and every implicit phrase move alike)
    /// carries them; the emitter renders by store and re-derives nothing.
    /// <para>⛔ <see cref="Source"/> stays the WRITTEN sending operand and is what every SYNTAX screen reads
    /// (§14.9.25.3 SR5/SR6/SR7 in <c>MoveBinder</c> and <c>VersionConformancePass.GateMove</c>): GR2/GR3
    /// substitute a VALUE, not a written figurative constant, and gating on the substitution would reject
    /// <c>MOVE "" TO PIC 9(3)</c>, which the standard permits.</para></summary>
    public IReadOnlyList<MoveStore> Stores { get; } = MoveClassifier.Classify(Source, Targets);

    /// <summary>The phrase this move is the implicit move OF, or null for the explicit MOVE statement. It rides
    /// the node so a later consumer — a diagnostic, an edition gate — can name the statement without asking its
    /// parent, which a generic bound-tree walk cannot see.</summary>
    public ImplicitMovePhrase? ImplicitOf { get; init; }
}

// The arithmetic verbs, each a small explicit node: the source operands are bound numeric expressions, the
// receivers are resolved Places paired with a rounding mode (the ROUNDED phrase, ISO §14.7.4). The in-place forms
// (TO/FROM/BY/INTO) read+write each target; the GIVING forms only write. The backend renders the value at the
// target's scale and stores via CobolNum, rounding per the receiver's mode.

/// <summary>An arithmetic resultant identifier: the receiving <see cref="Place"/> and the rounding mode its ROUNDED
/// phrase selects (ISO §14.7.4 — no phrase → <see cref="CobolRounding.Truncation"/>; bare <c>ROUNDED</c> →
/// <see cref="CobolRounding.NearestAwayFromZero"/>; <c>ROUNDED MODE IS x</c> → the named mode).</summary>
public sealed record Receiver(Place Place, CobolRounding Rounding);

/// <summary>An ON SIZE ERROR phrase on an arithmetic statement (ISO §14.7.5): the imperative run when a size error
/// occurs (<paramref name="OnError"/>) and/or the imperative run when none does (<paramref name="NotOnError"/>);
/// either may be absent. A null <c>SizeError</c> on an arithmetic node means the statement has no phrase (the checked
/// path is not emitted — its behavior is unchanged).</summary>
public sealed record SizeErrorPhrase(IReadOnlyList<BoundStatement>? OnError, IReadOnlyList<BoundStatement>? NotOnError);

/// <summary>An ARITHMETIC statement (ISO §14.7.5's ADD / COMPUTE / DIVIDE / MULTIPLY / SUBTRACT, incl. CORRESPONDING)
/// — the statements that OWN their size error condition: <c>ArithmeticEmitter.EmitArith</c> wraps their evaluation
/// and stores in the §14.7.5 shape (the SIZE ERROR phrase, the EC-SIZE handling, the fatal default), so the generic
/// EC-SIZE statement guard skips them (kb/Work PB75 — otherwise a size error dispatched by the statement would be
/// dispatched again by the guard). A structural marker, not a name list: a new arithmetic statement declares it with
/// its <see cref="SizeError"/> and is excluded by construction (<c>EcSizeGuardDriftTests</c> holds the two in step).</summary>
public interface IArithmeticStatement
{
    SizeErrorPhrase? SizeError { get; }
}

/// <summary><c>ADD addends TO targets</c> — each target ← target + Σ addends.</summary>
public sealed record BoundAddTo(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>ADD addends GIVING targets</c> — each target ← Σ addends.</summary>
public sealed record BoundAddGiving(IReadOnlyList<BoundExpr> Addends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>SUBTRACT minuends FROM targets</c> — each target ← target − Σ minuends.</summary>
public sealed record BoundSubtractFrom(IReadOnlyList<BoundExpr> Minuends, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>SUBTRACT minuends FROM from GIVING targets</c> — each target ← from − Σ minuends.</summary>
public sealed record BoundSubtractGiving(IReadOnlyList<BoundExpr> Minuends, BoundExpr From, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>MULTIPLY a BY targets</c> — each target ← target × a.</summary>
public sealed record BoundMultiplyBy(BoundExpr A, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>MULTIPLY a BY b GIVING targets</c> — each target ← a × b.</summary>
public sealed record BoundMultiplyGiving(BoundExpr A, BoundExpr B, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>DIVIDE divisor INTO targets</c> — each target ← target ÷ divisor.</summary>
public sealed record BoundDivideInto(BoundExpr Divisor, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;
/// <summary><c>DIVIDE divisor INTO dividend GIVING targets</c> / <c>DIVIDE dividend BY divisor GIVING targets</c>
/// — each target ← dividend ÷ divisor.</summary>
public sealed record BoundDivideGiving(BoundExpr Dividend, BoundExpr Divisor, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;

/// <summary><c>DIVIDE … GIVING quotient REMAINDER remainder</c> (ISO §14.9.12 Formats 4–5): one quotient receiver;
/// the remainder = dividend − (intermediate quotient × divisor), where the intermediate quotient is TRUNCATED at
/// the quotient receiver's scale even when the stored quotient is ROUNDED (GR7).</summary>
public sealed record BoundDivideRemainder(
    BoundExpr Dividend, BoundExpr Divisor, Receiver Quotient, Place Remainder, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;

/// <summary><c>COMPUTE targets = rhs</c>.</summary>
public sealed record BoundCompute(BoundExpr Rhs, IReadOnlyList<Receiver> Targets, SizeErrorPhrase? SizeError) : BoundStatement, IArithmeticStatement;

/// <summary><c>COMPUTE boolean-targets = boolean-expression</c> (ISO §14.9.8 Format 2). Each receiver is an
/// elementary boolean item; the stored value is taken at the number of boolean positions in the LARGEST boolean
/// ITEM referenced in the expression (§14.9.8.4 GR3 — literal-only larger sides don't count), left-aligned /
/// right-zero-filled / right-truncated (§14.6.8.6). No ROUNDED, no SIZE ERROR (F2). The width is NOT a member:
/// an operand's length can be a run-time quantity (a reference-modified slice, a boolean function's temporary
/// item), so the emitter carries it at run time (<c>BooleanRenderer.RenderAtItemWidth</c>; kb/Work PB589).</summary>
public sealed record BoundComputeBoolean(BoundBoolExpr Rhs, IReadOnlyList<Place> Targets) : BoundStatement;

/// <summary><c>IF cond THEN then-stmts [ELSE else-stmts]</c>.</summary>
public sealed record BoundIf(
    BoundCondition Condition,
    IReadOnlyList<BoundStatement> Then,
    IReadOnlyList<BoundStatement> Else) : BoundStatement;

/// <summary>How a PERFORM repeats its body.</summary>
[BoundNode]
public abstract record BoundPerformControl;
/// <summary>Run the body once.</summary>
public sealed record PerformOnce : BoundPerformControl;
/// <summary>Run the body <paramref name="Count"/> times.</summary>
public sealed record PerformTimes(BoundOperand Count) : BoundPerformControl;
/// <summary>Run the body until <paramref name="Until"/> (TEST BEFORE → while; <paramref name="TestAfter"/> → do/while).</summary>
public sealed record PerformUntil(BoundCondition Until, bool TestAfter) : BoundPerformControl;
/// <summary>PERFORM … UNTIL EXIT (ISO §14.9.28.4 GR11, COBOL-2023): an unconditional infinite loop (a condition
/// that never becomes true → <c>while(true)</c>). Escape is the programmer's responsibility — an inline loop by
/// EXIT PERFORM, an out-of-line loop by GOBACK/STOP RUN (NOTE 4).</summary>
public sealed record PerformForever : BoundPerformControl;

/// <summary>The COBOL KIND of a varying-phrase FROM/BY operand — the brace group §14.9.28.2's varying-phrase
/// prints: <c>{identifier-3 | index-name-2 | literal-1}</c> for FROM, <c>{identifier-4 | literal-2}</c> for BY
/// (rendered from the printed page 683 / PDF 713).
/// <para>⛔ IT EXISTS SO NO LATER PASS RE-DERIVES THE KIND FROM A BOUND-NODE C# TYPE (kb/Work PB439). The
/// §14.9.28.4 GR3 EC-RANGE-PERFORM-VARYING guard used to ask <c>From is BoundNumRef</c> — one identifier SHAPE
/// where the rule says "an identifier" — so a FUNCTION-IDENTIFIER FROM operand took the unchecked path and a
/// FATAL exception condition was never set to exist. The kind is decided ONCE where the operand is bound, and
/// every consumer (the §14.9.28.3 SR4/SR5/SR6 screens and the GR3 guard) asks the same question.</para></summary>
public enum VaryingOperandKind
{
    /// <summary>literal-1 / literal-2 (§14.9.28.3 SR3 — numeric; a leading sign is part of the literal,
    /// §8.3.3.3.2 rule 2; the figurative ZERO is one, §8.3.3.6.3 SR1).</summary>
    Literal,
    /// <summary>index-name-2 / index-name-4 — legal in the FROM slot only; §14.9.28.4 GR12 takes its OCCURRENCE
    /// NUMBER as the initialization value, and §14.9.28.4 GR3 is expressly NOT about it.</summary>
    IndexName,
    /// <summary>identifier-3 / identifier-4 — EVERY §8.4.3.1.2 identifier format: a (qualified, subscripted,
    /// reference-modified) data reference, a function-identifier, and a user-function result.</summary>
    Identifier,
    /// <summary>A shape the brace group does not print (a compound arithmetic expression, a signed identifier) —
    /// COBOLNET2119, or already diagnosed upstream.</summary>
    Invalid,
}

/// <summary>One VARYING/AFTER level of a PERFORM Format 4 (ISO §14.9.28): the induction variable (an index-name or
/// data item — SET-style target), its FROM initialization, BY augment (1 when the phrase is omitted, GR12), and
/// UNTIL condition. FROM/BY stay EXPRESSIONS — they are re-evaluated at every setting/augmenting operation and the
/// conditions at every test (GR12 item identification; changes inside the body have immediate effect).
/// <para><paramref name="FromKind"/> is the FROM operand's §14.9.28.2 brace-group kind, decided by the binder so
/// the emitter's §14.9.28.4 GR3 guard asks the rule's question instead of a node's C# type (kb/Work PB439).</para></summary>
public sealed record VaryingLevel(BoundSetTarget Var, BoundExpr From, BoundExpr By, BoundCondition Until,
    VaryingOperandKind FromKind);

/// <summary><c>PERFORM … VARYING v FROM f BY b UNTIL c [AFTER …]…</c> (ISO §14.9.28, the VARYING phrase of
/// Formats 1 and 2 — §14.9.28.4 GR13): nested
/// induction loops, leftmost level outermost.</summary>
public sealed record PerformVarying(IReadOnlyList<VaryingLevel> Levels, bool TestAfter,
    bool CheckIndexRange = false) : BoundPerformControl;

/// <summary>An inline <c>PERFORM … END-PERFORM</c> (a real loop over a bound body).</summary>
public sealed record BoundInlinePerform(BoundPerformControl Control, IReadOnlyList<BoundStatement> Body) : BoundStatement;

/// <summary>An out-of-line <c>PERFORM p [THRU q] [control]</c> — the resolved procedure <paramref name="Range"/>
/// (ISO §14.9.28.4 GR4's specified set of statements), run per the <paramref name="Control"/> phrase via the G4
/// dispatcher (a recursive bounded <c>Dispatch(Range.Start, Range.End)</c>). The control phrase is carried in
/// EVERY case, the EMPTY range included: an empty specified set (a zero-paragraph section, §14.4.2) still runs
/// GR13 a)'s initialization, GR9's count and GR10's tests — it just has no body and no transfer of control
/// (GR5), so the emitter emits the scaffold and never calls the dispatcher (kb/Work PB440).</summary>
// SourceLine (on the transfer nodes below): the source line of the transferring statement — the X3.23-1985
// DEBUG-LINE value when the transfer reaches a debug subject (VCR Table 7 row 7.17; the causing statement, DB101A —
// PERF-ITERATION-TEST pins the PERFORM line :611-617, GO-TO-TEST the GO TO line :482-489, on every iteration). 0
// when the debug facility is inactive (never read then).
/// <para><paramref name="EntersDeclarative"/> — this PERFORM is written in the NONDECLARATIVE portion and its
/// range is a declarative procedure (§14.9.49.3 SR4 admits exactly this reference). It is then the landing of
/// §14.9.33.4 GR2 b): "If the declarative was not executed because of an exception condition but was executed
/// instead by a PERFORM statement in the nondeclarative portion of the source element that referenced the
/// declarative procedure, the implicit CONTINUE statement immediately follows the last statement of the terminating
/// procedure referenced in that PERFORM statement" — the other arm of the ONE resume signal, whose exception arm
/// lands in <c>__RunUse</c> (kb/Work PB892).</para>
public sealed record BoundOutOfLinePerform(PcRange Range, BoundPerformControl Control, int SourceLine = 0,
    bool EntersDeclarative = false) : BoundStatement;

/// <summary><c>GO TO p</c> — set the program counter to <paramref name="TargetPc"/> (ISO §14.9.17 Format 1;
/// §14.9.17.4 GR1 "control is transferred to procedure-name-1"). (§14.9.20 — carried here and on the DEPENDING
/// node — is the INITIALIZE statement.)</summary>
public sealed record BoundGoTo(int TargetPc, int SourceLine = 0) : BoundStatement;

/// <summary><c>GO TO p1 p2 … DEPENDING ON sel</c> — transfer to <c>Targets[sel-1]</c>; out-of-range falls through
/// to the next statement (ISO §14.9.17 Format 2; §14.9.17.4 GR2).</summary>
public sealed record BoundGoToDepending(BoundOperand Selector, IReadOnlyList<int> Targets, int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT PARAGRAPH</c> — transfer to the end of the current paragraph (fall through to the next).</summary>
public sealed record BoundExitParagraph(int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT SECTION</c> (ISO §14.9.14 Format 4, GR7) — transfer control to the unnamed empty paragraph
/// following the LAST paragraph of the current section (pc <paramref name="SectionEndPc"/> + 1), "preceding any
/// return mechanisms for that section". When the enclosing bounded dispatch was entered with its exit AT the section
/// end (a PERFORM SECTION / PERFORM … THRU the section end / SORT-or-USE procedure / the top-level end wall), that
/// section return mechanism fires — realized in the emitter by an explicit <c>return</c> when <c>__exitPc</c> equals
/// the section end (the mid-section fall-through the bounded dispatch's own <c>__atExit</c> tail-check cannot see).</summary>
public sealed record BoundExitSection(int SectionEndPc, int SourceLine = 0) : BoundStatement;

/// <summary><c>EXIT PERFORM [CYCLE]</c> — break (or continue, when CYCLE) the nearest inline PERFORM loop.</summary>
public sealed record BoundExitPerform(bool Cycle) : BoundStatement;

/// <summary>A no-op statement: bare <c>EXIT</c>, <c>CONTINUE</c>, or <c>EXIT PROGRAM</c> in the main program.</summary>
public sealed record BoundNop : BoundStatement;

/// <summary>CONTINUE AFTER arithmetic-expression-1 SECONDS (ISO §14.9.9, COBOL-2023): a timed pause.
/// <paramref name="Seconds"/> is the interval; a value below zero is forced to 0 (GR1a) and, when
/// <paramref name="CheckLessThanZero"/> (EC-CONTINUE-LESS-THAN-ZERO checking was enabled at this statement),
/// sets the nonfatal EC-CONTINUE-LESS-THAN-ZERO (GR1b) before continuing; otherwise execution suspends for the
/// interval (GR1). Fractional seconds truncate (implicit COMPUTE without ROUNDED).</summary>
public sealed record BoundContinueAfter(BoundExpr Seconds, bool CheckLessThanZero) : BoundStatement;

/// <summary>A fixed pre/main/post statement group emitted in order — the carrier for bind-time desugars
/// that wrap ONE source statement in synthesized neighbors (first client: object-property references,
/// ISO §8.4.3.9.4 GR1–GR3 — the pre-GET / statement / post-SET triple over a compiler temp; deep-dive
/// D-P2). NOT a control-flow construct: no pc identity of its own, both backends render the children
/// consecutively; a child that transfers control (GO TO/EXIT) behaves exactly as if written in line.</summary>
public sealed record BoundSequence(IReadOnlyList<BoundStatement> Steps) : BoundStatement;

/// <summary>⛔ ONE WRITTEN STATEMENT THAT *IS* N SEPARATE STATEMENTS — the multi-operand rule the standard states
/// SEVEN times, once per verb, in identical words: CLOSE (ISO §14.9.6.4 GR10), FREE (§14.9.15.4 GR2), INITIALIZE
/// (§14.9.20.4 GR3), INITIATE (§14.9.21.4 GR5), OPEN (§14.9.27.4 GR20), TERMINATE (§14.9.46.4 GR4) and VALIDATE
/// (§14.9.50.4 GR3) each read "the result of executing this … statement is the same as if a separate … statement
/// had been written for each … in the same order as specified", and each then adds the sentence THIS NODE EXISTS
/// FOR: "If an implicit … statement results in the execution of a declarative procedure that executes a RESUME
/// statement with the NEXT STATEMENT phrase, processing resumes at the next implicit … statement, if any."
/// <para>That second sentence is what §14.9.33.4 GR2 a) leaves room for — "the implicit CONTINUE statement
/// immediately follows the end of the statement that was executing when control was transferred to the exception
/// processing procedure UNLESS GENERAL RULES ASSOCIATED WITH THE APPLICABLE STATEMENT SPECIFY OTHERWISE" — so a
/// multi-operand verb's resumption point is a PER-OPERAND boundary, never the end of the written statement. The
/// compiler's resume protocol addresses STATEMENT SITES (a declarative's <c>-2</c> action falls out of the
/// statement's own EC guard, EcEmitter's dispatch-result protocol), so the boundary exists exactly when each
/// operand owns a site — and this node is that and nothing else. <c>EcBinder.EcWrap</c> DISTRIBUTES the
/// <see cref="BoundEcChecked"/> wrapper over <paramref name="Members"/>, which is what makes each member a site
/// the <c>-2</c> action lands AFTER rather than past the whole verb (kb/Work PB419: a declarative that resumed
/// NEXT STATEMENT out of the FIRST identifier-1 of <c>INITIALIZE A B</c> skipped B entirely — a silent wrong
/// answer, measured).</para>
/// <para>⛔ A ONE-OPERAND STATEMENT NEVER BUILDS ONE — <see cref="Of"/> returns the lone member. Every one of the
/// seven rules is conditioned on "more than one", and the zero-scaffolding invariant (design SSOT §18.16) holds:
/// with EC checking OFF no wrapper is built at all, so the series emits its members consecutively and the text is
/// byte-identical to the former flat run (with no wrapper there is no <c>-2</c> to land).</para>
/// <para>NOT a <see cref="BoundSequence"/>, and the difference is the whole point: a sequence is ONE statement
/// made of steps — a desugar's hoisted pre-ops belong to the statement they decorate, and a RESUME out of one
/// abandons the whole thing — while a series is N statements. <see cref="Rewrap"/> is how a desugar wrap keeps
/// the series outermost.</para></summary>
public sealed record BoundImplicitSeries(IReadOnlyList<BoundStatement> Members) : BoundStatement
{
    /// <summary>The ONE constructor a multi-operand verb binder calls: the series when the standard's "more than
    /// one" premise holds, the bare member otherwise (and an empty series for the operand-less recovery shapes a
    /// screen may leave behind, which emits nothing).</summary>
    public static BoundStatement Of(IReadOnlyList<BoundStatement> members) =>
        members.Count == 1 ? members[0] : new BoundImplicitSeries(members);

    /// <summary>Apply a bind-time desugar wrap (the UDF activation hoist, the OO property pre-op triple) to a
    /// statement that may be a series, keeping the SERIES OUTERMOST so <c>EcWrap</c> still sees it. The wrap lands
    /// on the FIRST implicit statement, which is where the hoisted operands are evaluated and therefore which
    /// implicit statement they belong to; wrapping the series itself would put the boundary back inside a single
    /// site and silently restore the PB419 defect for exactly the statements that carry a function-identifier or
    /// a property reference. Returns <paramref name="node"/> untouched when the wrap was a no-op.</summary>
    public static BoundStatement Rewrap(BoundStatement node, Func<BoundStatement, BoundStatement> wrap)
    {
        if (node is not BoundImplicitSeries ser || ser.Members.Count == 0) return wrap(node);
        var first = wrap(ser.Members[0]);
        return ReferenceEquals(first, ser.Members[0]) ? ser : new BoundImplicitSeries([first, .. ser.Members.Skip(1)]);
    }
}

/// <summary>COMMIT / ROLLBACK (ISO §14.9.7 / §14.9.36; kb/Work PB137): the transaction facility is the
/// documented A.3 items-6/7 non-support, and with no APPLY COMMIT clause GR1 makes each statement
/// CONTINUE-equivalent — but the node carries its IDENTITY, so §14.9.7.3/§14.9.36.3 SR2's SORT/MERGE
/// procedure ban is enforceable in the cross-pass where the old payload-free BoundNop made the statement
/// indistinguishable from CONTINUE forever.</summary>
public sealed record BoundCommitRollback(bool IsCommit) : BoundStatement;

/// <summary><c>NEXT SENTENCE</c> (ISO §14.9.19.4 GR4 in an IF's THEN phrase and GR6 in its ELSE phrase — ONE
/// node for both, since the two rules state the same transfer and nothing downstream may diverge on the arm; also
/// §14.9.37 as a SEARCH AT END/WHEN phrase — archaic per Annex F.1, legal at every edition): transfer to the
/// implicit CONTINUE following the current sentence's separator period.</summary>
public sealed record BoundNextSentence(int SourceLine = 0) : BoundStatement;

/// <summary><c>SET {{ condition-name-1 } … TO { TRUE | FALSE }} …</c> (ISO §14.9.39 Format 4) — each names a
/// level-88 whose selected VALUE literal is stored into its (already-resolved) parent place.
/// <para><paramref name="ToTrue"/> selects WHICH literal §14.9.39.4 places there, and nothing else: GR6 takes
/// "<i>the literal in the VALUE clause</i>" (the FIRST, when the clause writes several) and GR7 "<i>the literal
/// in the FALSE phrase of the VALUE clause</i>" (§13.18.63.4 GR20's literal-4), both "<i>according to the rules
/// for the VALUE clause</i>" with the same group-length and zero-length provisos. One node, one store path —
/// kb/Work PB555, which is also why the FALSE arm exists at all.</para>
/// <para>⛔ <c>ToTrue</c> IS PER STORE, NOT PER STATEMENT (kb/Work PB450). §14.9.39.2 Format 4 prints an OUTER
/// repetition around the whole <c>{ condition-name-1 } … TO { TRUE | FALSE }</c> unit, so one statement may
/// carry several groups and they need not agree; §14.9.39.4 GR8 — "<i>If multiple condition-names are
/// specified, the results are the same as if a separate SET statement had been written for each
/// condition-name-1</i>" — is what lets the groups flatten into ONE ordered list here instead of a node per
/// group.</para></summary>
public sealed record BoundSetConditions(
    IReadOnlyList<(Place Parent, Condition88 Condition, bool ToTrue)> Sets) : BoundStatement;

/// <summary>ONE receiving operand of SET Format 7, as §14.9.39.2's printed brace writes it —
/// <c>{ ADDRESS OF data-name-1 | identifier-5 }</c> — carrying its OWN kind (kb/Work PB450).
/// <para>Exactly one member is non-null. <paramref name="Pointer"/> is identifier-5, the data-pointer item
/// §14.9.39.3 SR17 screens and §14.9.39.4 GR12 stores the address INTO; <paramref name="Based"/> is
/// data-name-1, the based item SR18 screens and GR13 assigns the address TO (its implicit data-address
/// pointer, §8.6.5). The two spellings are independent per operand and may be mixed in one statement, because
/// the printed `…` sits OUTSIDE the brace — so the KIND belongs to the operand and never to the
/// statement.</para></summary>
public sealed record BoundPointerReceiver(Place? Pointer, DataItem? Based);

/// <summary>SET data-pointer assignment (ISO §14.9.39 Format 7): the address identified by identifier-6 is
/// stored in / assigned to EACH receiving operand IN THE ORDER SPECIFIED (§14.9.39.4 GR12 and GR13 — one loop
/// over <paramref name="Receivers"/>, which is why the receivers are one list and not two node shapes).
/// <para><paramref name="ToNull"/> ⇔ the sender is the predefined address NULL (SR19; renders
/// <c>ManagedPointer.Null</c>); <paramref name="Address"/> ⇔ the sender is a §8.4.3.11 data-address-identifier
/// <c>ADDRESS OF identifier</c>; otherwise <paramref name="Source"/> is the data-pointer sender. The sender is
/// ONE operand outside the printed repetition and is therefore evaluated ONCE.</para>
/// <para>⛔ ONE NODE FOR ONE PRINTED FORMAT. A separate <c>BoundSetAddressOfBased</c> used to carry the
/// data-name-1 half with arity one, and a Format-7 statement that mixed the two spellings could not be
/// expressed at all — the shape kb/Work PB450 measured as four COBOL0001 cascades.</para></summary>
public sealed record BoundSetPointer(
    IReadOnlyList<BoundPointerReceiver> Receivers, Place? Source, bool ToNull,
    BoundAddressOf? Address = null) : BoundStatement;

/// <summary><c>ADDRESS OF identifier</c> as a pointer VALUE (ISO §8.4.3.11 GR1; Phase-4b increment 2): for a
/// BASED item the value IS its implicit data-address pointer (§8.6.5 :8791); for a cell-backed record the
/// emitter renders <c>ManagedPointer.At(cell, classOffset)</c> over the item's forced/EXTERNAL storage cell.
/// <paramref name="OccursDisplacement"/> carries a SUBSCRIPTED operand's occurrence displacement —
/// <c>(idx − 1) × width [+ …]</c> character positions added to the item's class offset (the occurrences lie
/// end-to-end in the ONE cell image, §8.4.3.11 GR1 — the address OF THE OCCURRENCE); null for an
/// unsubscripted operand. It is the D10 transitional rendered-index carrier (see
/// <c>AccessPath</c>/<c>FixedTableSegment</c>) — a <c>BoundExpr</c> when PHASE 15 removes SUBSCRIPT mode.</summary>
public sealed record BoundAddressOf(DataItem Item, string? OccursDisplacement = null);

/// <summary>A bound §8.4.3.13 PROGRAM-ADDRESS-IDENTIFIER (<c>ADDRESS OF PROGRAM …</c>) as a pointer VALUE: the
/// program named by a compile-time <paramref name="NameLiteral"/> (literal-1, or program-prototype-name-1's
/// EXTERNALIZED name — §8.4.3.13.4 GR2) or by the run-time content of <paramref name="NamePlace"/>
/// (identifier-1, GR1a). <paramref name="Prototype"/> is set exactly for the program-prototype-name-1 arm, which
/// GR3 makes a program-pointer RESTRICTED to that prototype. Produced by the ONE operand binder
/// (<c>SetBinder.BindProgramAddressOperand</c>) for both surfaces that take it — the SET Format-9 sender and the
/// CALL argument (kb/Work PB239).</summary>
public sealed record BoundProgramAddress(string? NameLiteral, Place? NamePlace, string? Prototype);

/// <summary>A bound ADDRESS-IDENTIFIER used as an ordinary OPERAND — ISO §8.4.3.1.2 identifier FORMAT 9, exactly
/// one of its two arms set: <paramref name="Data"/> (§8.4.3.11, category data-pointer — §8.4.3.11.4 GR1) or
/// <paramref name="Program"/> (§8.4.3.13, category program-pointer — §8.4.3.13.4 GR1). Each arm "creates a unique
/// data item of class pointer", which is why it is a <see cref="BoundOperand"/> of its own rather than a
/// <see cref="BoundFieldOperand"/>: the item has no storage of the program's, so there is no <see cref="Place"/>
/// to read or write, and §8.4.3.11.3 SR5 / §8.4.3.13.3 SR4 forbid it as a receiving operand.
/// <para>Produced by the ONE binder of the rule (<c>PtrBinder.BindAddressIdentifier</c>) for every surface that
/// takes the identifier as an operand — the relation condition (§8.8.4.2.2 Format 3) and the INVOKE argument
/// (§14.9.23.3 SR9/SR19; kb/Work PB1021). The CALL argument carries the same two arms on
/// <c>BoundCallArg.DataAddress</c> / <c>ProgramAddress</c> (kb/Work PB239).</para></summary>
public sealed record BoundAddressOperand(BoundAddressOf? Data, BoundProgramAddress? Program) : BoundOperand
{
    /// <summary>The §8.5.2.1 Table 2 category of the unique data item the identifier creates.</summary>
    public PicCategory Category => Data is not null ? PicCategory.Pointer : PicCategory.ProgramPointer;
}

/// <summary><c>SET program-pointer… TO {NULL | program-pointer}</c> (ISO §14.9.39 Format 9; SR21 — both sides
/// category program-pointer; P10 Step 7): a straight carrier copy, the data-pointer Format-7 twin.</summary>
public sealed record BoundSetProgramPointer(IReadOnlyList<Place> Targets, Place? Source, bool ToNull) : BoundStatement;

/// <summary><c>SET program-pointer… TO ENTRY {literal | identifier}</c> (ISO §14.9.39 Format 9 with the
/// §8.4.3.13 program-address-identifier sender): resolve the named OUTERMOST program through the run-unit
/// ProgramTable at statement time. Exactly one of <paramref name="NameLiteral"/> (the decoded literal) /
/// <paramref name="NamePlace"/> (a runtime name read, GR1a) is set. Not locatable → GR4: the targets take
/// NULL and EC-PROGRAM-NOT-FOUND is set to exist (the emitter's checking-gated block).</summary>
public sealed record BoundSetEntry(IReadOnlyList<Place> Targets, string? NameLiteral, Place? NamePlace) : BoundStatement;

/// <summary><c>SET function-pointer… TO {NULL | function-pointer}</c> (ISO §14.9.39.2 Format 8; §14.9.39.3 SR20
/// — both sides category function-pointer and the associated function-prototypes of the same signature, checked
/// at BIND; §14.9.39.4 GR14 — "The address identified by identifier-13 is stored in each data item referenced by
/// identifier-12 in the order specified"): a straight carrier copy, the Format-9 twin. kb/Work PB452 + PB817.
/// <para>GR14's run-time EC-FUNCTION-PTR-INVALID screen is NOT emitted for this node, and that is derived rather
/// than skipped: SR20 has already proven at compile time that the sender's prototype and every receiver's
/// prototype have the same signature, and §13.18.60.4 GR26 makes the sender's own content NULL-or-same-signature
/// by construction, so the GR14 condition cannot arise on this shape. It CAN arise on
/// <see cref="BoundSetFunctionAddress"/>, whose sender names a function at RUN time.</para></summary>
public sealed record BoundSetFunctionPointer(IReadOnlyList<Place> Targets, Place? Source, bool ToNull) : BoundStatement;

/// <summary><c>SET function-pointer… TO ADDRESS OF FUNCTION {function-prototype-name-1 | identifier-1}</c>
/// (ISO §14.9.39.2 Format 8 with the §8.4.3.12 function-address-identifier sender): resolve the named function
/// through the run-unit ProgramTable at statement time (§8.4.3.12.4 GR2 — the externalized function-name).
/// Exactly one of <paramref name="PrototypeName"/> (GR1 b, the compile-time prototype form) /
/// <paramref name="NamePlace"/> (GR1 a, a run-time name read) is set. Not locatable → GR4: the targets take NULL
/// and EC-FUNCTION-NOT-FOUND is set to exist. A located function whose signature does not match
/// <paramref name="ExpectedFormals"/> → §14.9.39.4 GR14: EC-FUNCTION-PTR-INVALID, "no data items are changed,
/// and the execution of the SET statement is terminated". kb/Work PB452.</summary>
/// <param name="ExpectedFormals">The receiving items' declared function-prototype's formal count — the run-time
/// signature the located function is screened against. Every receiver carries the SAME one: SR20 requires it.</param>
public sealed record BoundSetFunctionAddress(IReadOnlyList<Place> Targets, string? PrototypeName, Place? NamePlace,
    int ExpectedFormals) : BoundStatement;

/// <summary><c>SET pointer… {UP|DOWN} BY integer</c> (ISO §14.9.39 Format 10; 2002+): the address moves by
/// bytes (GR20 — character positions in this model); NULL → EC-DATA-PTR-NULL at runtime (GR18).</summary>
public sealed record BoundSetPointerUpDown(IReadOnlyList<Place> Targets, BoundExpr Amount, bool Down) : BoundStatement;

/// <summary><c>ALLOCATE</c> (ISO §14.9.3): form 1 — <paramref name="Chars"/> characters (GR1 rounds a
/// fractional request UP; GR2 ≤0 ⇒ NULL, no EC) RETURNING <paramref name="Returning"/> (SR2 — required with
/// CHARACTERS); form 2 — storage sized for the BASED <paramref name="Based"/> (GR3), its implicit pointer set
/// (GR4a) and <paramref name="Returning"/> also set when present (GR4b). <paramref name="Initialized"/>: GR6
/// binary-zero fill (form 1) / the GR7 INITIALIZE lowering (form 2).</summary>
public sealed record BoundAllocate(
    DataItem? Based, BoundExpr? Chars, bool Initialized, Place? Returning) : BoundStatement;

/// <summary><c>FREE pointer…</c> (ISO §14.9.15): per-operand left to right (GR2); each operand runs the GR1
/// three-way (release-and-null / NULL no-op / EC-STORAGE-NOT-ALLOC nonfatal, reported through the
/// TurnState-gated status block).</summary>
public sealed record BoundFree(IReadOnlyList<Place> Operands) : BoundStatement;

// ── SET index assignment / arithmetic (ISO §14.9.39 Formats 1–2; COBOLNET_DESIGN §3.5/§12.3) ──────────────────

/// <summary>A SET receiving operand, dispatched by kind (the design's §12.3 rule).</summary>
[BoundNode]
public abstract record BoundSetTarget;
/// <summary>An INDEXED BY index-name receiver — its C# <c>long</c> occurrence-number field.</summary>
public sealed record SetIndexTarget(string IndexField) : BoundSetTarget;
/// <summary>A data-item receiver: an index data item (USAGE INDEX — receives the value unchanged, §14.9.39 GR2b)
/// or an integer data item (receives the occurrence number via its own PICTURE store, GR2c).</summary>
public sealed record SetPlaceTarget(Place Place) : BoundSetTarget;

/// <summary><c>SET receivers… TO value</c> (ISO §14.9.39 Format 1): the sender (an occurrence number — in the
/// §3.5 model an index IS its 1-based occurrence number) is determined ONCE (GR2), then stored per receiver kind.</summary>
public sealed record BoundSetTo(IReadOnlyList<BoundSetTarget> Targets, BoundExpr Value) : BoundStatement;

/// <summary><c>SET index-name… {UP|DOWN} BY amount</c> (ISO §14.9.39 Format 2): the amount is determined ONCE
/// (GR3), then each index is incremented/decremented by it (GR4).</summary>
public sealed record BoundSetUpDown(IReadOnlyList<BoundSetTarget> Targets, BoundExpr Amount, bool Down) : BoundStatement;

/// <summary>How SET Format 14 changes a dynamic table's current capacity (ISO §13.18.38 Format 4 / §14.9.39 GR29;
/// data-model D9).</summary>
public enum SetCapacityKind { To, UpBy, DownBy }

/// <summary>THE ONE spelling of a Format-14 capacity phrase (§14.9.39.2 Format 14), shared by the binder's
/// receiver diagnostics and by the emitter's GR29 exception detail — the two had a private copy each.</summary>
public static class SetCapacityKinds
{
    /// <inheritdoc cref="SetCapacityKinds"/>
    public static string Text(SetCapacityKind kind) =>
        kind switch { SetCapacityKind.To => "TO", SetCapacityKind.UpBy => "UP BY", _ => "DOWN BY" };
}

/// <summary><c>SET dynamic-capacity-register {TO | UP BY | DOWN BY} amount</c> (ISO §14.9.39 SET Format 14; the
/// COBOL-2014 OCCURS DYNAMIC feature, data-model D9). The register is a VIEW over its owning table, so the emitter
/// calls the table's <c>SetCapacity</c>/<c>CapacityUpBy</c>/<c>CapacityDownBy</c> (via <paramref name="Table"/>, the
/// whole-table access path) — raising or lowering the current capacity, seeding new occurrences (§8.5.1.9.5), clamped
/// to the minimum, and raising EC-FLOW-SEARCH if a SEARCH of that same table is active (GR31).</summary>
public sealed record BoundSetCapacity(AccessPath Table, BoundExpr Amount, SetCapacityKind Kind) : BoundStatement;

/// <summary>SET [SIZE OF] data-name TO n (ISO §14.9.39 Format 16, COBOL-2023): set the current length of the
/// dynamic-length elementary item at <paramref name="Target"/> to <paramref name="Amount"/> characters. Growing
/// space-fills the added positions (GR39); shrinking drops the trailing ones; a value above <paramref name="Limit"/>
/// (the LIMIT character count, −1 = unbounded) clamps, a negative value yields 0 (GR37/GR38). The clamp/negative
/// legs also raise the nonfatal EC-STORAGE-NOT-AVAIL — through the runtime's ambient
/// (EC-STORAGE-NOT-AVAIL → StorageNotAvailChecking) pair, which the statement's own EC wrapper arms, so the raise
/// runs the §14.6.13.1.4 #3 declarative selection like every other ambient-gated condition. (This node used to
/// carry a bind-time <c>CheckStorage</c> bool passed POSITIONALLY to the runtime; that second enablement
/// mechanism is what kept the raise outside the selection — kb/Work PB367b.) A
/// self-identifying node — the VersionConformancePass bound-tree arm gates it (SetDynLengthSize2023) for both the
/// explicit SIZE OF form and the bare re-routed form.</summary>
public sealed record BoundSetSize(Place Target, BoundExpr Amount, int MaxSize) : BoundStatement;

/// <summary>One receiving operand of a Format-15 SET, with the content computed FOR IT (ISO §14.9.39.4 GR32–GR36
/// each say "the content of identifier-14 is set …", and every value they name is a property of that receiver's own
/// data description — so a statement with several receivers stores a DIFFERENT value into each).</summary>
/// <param name="Target">One occurrence of identifier-14.</param>
/// <param name="Store">The implicit elementary MOVE that deposits GR32/GR36's extreme of <paramref name="Target"/>'s
/// own description (already carrying its sign, GR32 c / GR36 c) into it — the ONE store path, so conversion,
/// truncation and an unsigned receiver's sign drop are the ordinary store rules and not a second copy of them.
/// ⛔ BUILT AT BIND TIME like every other <see cref="BoundMove"/> (kb/Work PB348): a move constructed in the
/// emitter is downstream of the storage facts codegen itself consumes. Null exactly when <paramref name="Ieee"/>
/// is set.</param>
/// <param name="Ieee">WHICH canonical value GR33/GR34/GR35 names — an infinity, a quiet NaN or a signaling NaN.
/// None of the three can be spelled by any COBOL numeric literal, so this arm writes the receiver's carrier
/// directly instead of routing through a numeric store; §14.9.39.3 SR32 has already confined that carrier to a
/// standard floating-point usage, i.e. to the ISO/IEC 60559:2020 basic interchange format the rules name.
/// ⛔ The ENUM rides here, not the backend expression: a bound node states what the standard requires, and how
/// to spell an infinity is the code generator's question (a second backend behind <c>ICodeGenBackend</c> spells
/// it differently). <c>RuntimeApi.FloatFromBits</c> does the spelling, over <c>IeeeSpecials.Bits</c>. Null exactly when <paramref name="Store"/> is
/// set.</param>
/// <param name="NegativeSign">GR33/GR34/GR35's own last sentence — the SIGN phrase's sign, false when the phrase
/// is absent ("otherwise the sign is positive"). Meaningless, and always false, on a <paramref name="Store"/>
/// arm, whose sign is already inside the literal the binder computed.</param>
public sealed record SetContentStore(Place Target, BoundMove? Store, IeeeSpecial? Ieee, bool NegativeSign = false);

/// <summary><c>SET CONTENT OF identifier-14 … TO { FARTHEST-FROM-ZERO | NEAREST-TO-ZERO } [IN-ARITHMETIC-RANGE]
/// [SIGN …]</c> / <c>… TO { FLOAT-INFINITY | FLOAT-NOT-A-NUMBER | FLOAT-NOT-A-NUMBER-SIGNALING } [SIGN …]</c>
/// (ISO §14.9.39.2 Format 15, numeric-content; COBOL-2014 — kb/Work PB452). Every value is a compile-time property
/// of the receiver's data description and the arithmetic mode, so the whole statement resolves at BIND time to one
/// store per receiver; a self-identifying node whose edition gate fires on RECOGNITION in the ParseArm
/// (set-numeric-content-2014), not here.</summary>
public sealed record BoundSetContent(IReadOnlyList<SetContentStore> Stores) : BoundStatement;

// ── SEARCH (ISO §14.9.37 — Format 1 serial, Format 2 SEARCH ALL) ───────────────────────────────────────────────

/// <summary>One WHEN arm of a SEARCH: its condition and imperative statements (evaluated in source order; the
/// first true arm runs and ends the search — ISO §14.9.37.4 GR1 a), "the search operation is terminated
/// immediately").</summary>
public sealed record BoundSearchWhen(BoundCondition Condition, IReadOnlyList<BoundStatement> Statements);

/// <summary><c>SEARCH table [VARYING …] [AT END …] WHEN…</c> (ISO §14.9.37): a scan of the table over
/// <paramref name="IndexField"/> — the table's first index, or the VARYING same-table index that GR3 c) 1. makes
/// the search index. Each probe tries the WHEN conditions in order; none true → the scan advances.
/// <para><paramref name="IsAll"/> marks <c>SEARCH ALL</c> (Format 2), and the two formats are LOWERED SEPARATELY
/// because GR4 and GR9 disagree about the index: Format 1 advances and then judges the new value, so an
/// unsuccessful serial search ends one past the table and carries <paramref name="AlsoVaried"/> (a different
/// table's index or a data item, GR3 b)/c)) with it; Format 2 ignores the initial setting and, per GR9, is "At no
/// time … set to a value that exceeds the value that corresponds to the last element of the table", so its index
/// never leaves the table (kb/Work PB447). Format 2 has no VARYING phrase, so
/// <paramref name="AlsoVaried"/> is null there.</para></summary>
public sealed record BoundSearch(
    string IndexField, long Count, BoundSetTarget? AlsoVaried,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundSearchWhen> Whens,
    bool IsAll = false, Place? DependItem = null, string? DynTable = null,
    bool CheckSearchIndex = false, bool CheckSearchNoMatch = false) : BoundStatement;

// ── File I/O (ISO §14.9; COBOLNET_DESIGN §8) ───────────────────────────────────────────────────────────────────

/// <summary>How a file is opened (ISO §14.9.25). Maps 1:1 to the runtime <c>FileOpenMode</c>.</summary>
public enum BoundOpenMode { Input, Output, Extend, IO }

/// <summary>The written form of a CLOSE statement (ISO §14.9.6) — the four rows of Table 14 (§14.9.6.4 GR3)
/// plus <c>WITH LOCK</c>, which is not a Table 14 row but the plain form with a reopen prohibition. The runtime
/// looks the effect up in <c>Table14</c> from the form and the file's §14.9.6.4 GR2 category; nothing here
/// decides behaviour. (The enum's doc used to cite §14.9.7, which is the COMMIT statement — kb/Work PB235.)</summary>
public enum BoundCloseKind
{
    Normal,
    WithLock,
    /// <summary>REEL/UNIT — Table 14's <c>CLOSE UNIT</c> row (§14.9.6.3 SR2: "The words REEL and UNIT are
    /// equivalent"). On the Non-unit medium the cell is symbol e, the '07' that leaves the file open.</summary>
    ReelUnit,
    /// <summary>REEL/UNIT FOR REMOVAL — Table 14's own <c>CLOSE UNIT FOR REMOVAL</c> row. Its Non-unit cell
    /// equals ReelUnit's, but the (b)/(c) cells add symbol d (unit removal), so the two forms stay separate
    /// members: folding them was what left <c>opt.REMOVAL()</c> with no consumer (kb/Work PB235).</summary>
    ReelUnitForRemoval,
    /// <summary>WITH NO REWIND — Table 14's Non-unit cell is c,g: the file IS closed AND the status is '07'
    /// (§9.1.13.2 item 6). Previously folded into Normal, reporting '00' (kb/Work PB141).</summary>
    NoRewind,
}

/// <summary>The LOCK-RETENTION phrase on a READ/WRITE/REWRITE — the printed `[ WITH LOCK | WITH NO LOCK ]`
/// bracket of ISO §14.9.30.2 / §14.9.51.2 / §14.9.35.2: explicit WITH LOCK, WITH NO LOCK (never lock), or None
/// = the file's effective LOCK MODE governs (AUTOMATIC locks on READ, MANUAL does not — §14.9.30.4 GR11c/d).
/// <para>⛔ IGNORING LOCK IS NOT A MEMBER, AND MUST NOT BECOME ONE (kb/Work PB331). It is an alternative of the
/// OTHER, INDEPENDENT bracket — see <c>CobolIO.g4#readLockContentionPhrase</c> and
/// <c>BoundRead.IgnoringLock</c> — and §5.2.6.1 lets a READ select from both brackets at once, so
/// `IGNORING LOCK WITH NO LOCK` is one legal statement that a single enum cannot represent. Folding contention
/// and retention back together is what made that spelling a syntax error.</para></summary>
public enum BoundRecordLock { None, WithLock, WithNoLock }

/// <summary>A RETRY phrase (ISO §14.7.9): retry a locked operation N TIMES (§14.7.9.3 GR1), FOR N SECONDS
/// (GR2), or FOREVER (GR3). ⛔ THE THREE FORMS ARE NOT INTERCHANGEABLE AND THIS ENUM MUST STAY THREE-VALUED —
/// GR1's count is a bounded loop over the connector registry, GR2's timeout period is clamped to this
/// implementation's maximum meaningful value of ZERO (Annex A.1 item 166) so it makes no further attempt, and
/// GR3's unbounded wait is the ONE form whose failure this implementation reports as the §9.1.13.8 item 2
/// deadlock. Which of them is which is decided in exactly one place, <c>FileRegistry.RetryLoop</c>
/// (docs/COBOLNET_FILES_DESIGN.md "D8"); nothing upstream may restate the outcome, because a comment that does
/// is what drifted when the SECONDS/FOREVER arms were split (kb/Work PB346). GR4 a) — "the result of the
/// evaluation of arithmetic-expression-1 or arithmetic-expression-2 is negative or zero" — is screened there
/// too, which is why <see cref="RetrySpec.Amount"/> travels for SECONDS as well as for TIMES.</summary>
public enum RetryKind { Times, Seconds, Forever }
public sealed record RetrySpec(RetryKind Kind, BoundExpr? Amount);

/// <summary><c>UNLOCK file [RECORD[S]]</c> (ISO §14.9.47): release all record locks this connector holds on the
/// file; always succeeds when the file is open (else I-O status 42).</summary>
public sealed record BoundUnlock(FileModel File, bool Records) : BoundStatement;

/// <summary>ONE file-name of an OPEN statement, carrying the mode and phrases of the repeated group it was
/// written in AND the phrase the general format writes per file-name (ISO §14.9.27.2). The general format's
/// OUTER braces enclose <c>{open-mode} [sharing-phrase] [retry-phrase] {file-name-1 [WITH NO REWIND]} …</c> and
/// carry the trailing ellipsis — verified against the printed page — so the two phrases are scoped to their
/// group beside the mode, never to the statement, while the REWIND phrase is the FILE-NAME's own. This record
/// IS §14.9.27.4 GR20's own normal form: "If more than one file-name is specified in an OPEN statement, the
/// result of executing this OPEN statement is the same as if a separate OPEN statement had been written for
/// each file-name in the same order as specified in the OPEN statement. These separate OPEN statements would
/// each have the same open mode specification, the sharing-phrase, retry-phrase, and REWIND phrase as specified
/// in the OPEN statement." — the binder flattens the groups into exactly the separate OPENs the rule names, so
/// no consumer can re-broaden a phrase's scope (kb/Work PB316).
/// <para><see cref="Sharing"/> is this group's OPEN SHARING phrase or null; null means §14.9.27.4 GR23's other
/// arm — "If there is no SHARING phrase on the OPEN statement, then file sharing is completely specified in the
/// file control entry" — so a null here shall reach the runtime as the file-control clause, NOT as a sibling
/// group's phrase. <see cref="Retry"/> is this group's RETRY phrase (§14.7.9) or null.</para>
/// <para><see cref="Tape"/> = the ONE per-file-name TAPE PHRASE written for THIS file-name — §14.9.27.2's
/// <c>{ file-name-1 [ WITH NO REWIND ] } …</c> group, which at COBOL-85 printed <c>[ REVERSED | WITH NO REWIND ]</c>
/// (VERSION_CHANGE_REFERENCE row 7.12 records REVERSED's deletion at 2002) and which the superset grammar still
/// parses as one alternation, <c>openFileSpec : dataReference (REVERSED | WITH? NO REWIND)?</c>. It is an ENUM and
/// not one bool per phrase because the grammar makes the two MUTUALLY EXCLUSIVE, and a second bool is a second
/// place for the next tape phrase to be forgotten: the tuple this record replaced could not carry a phrase at all,
/// which is why NO REWIND parsed and was then dropped (kb/Work PB317), and REVERSED was still being dropped after
/// PB317 gave NO REWIND a field of its own (kb/Work PB668 — the two-arm dispatch with one arm fixed, one layer up).
/// <see cref="BoundClose"/>'s per-file <see cref="BoundCloseKind"/> is the same shape for the CLOSE half.</para>
/// <para>An unsupported organization carries a loud <see cref="Unsupported"/> reason so the file opens to a
/// runtime not-implemented guard.</para>
/// <para>A <c>readonly record struct</c>, not a class: it is the per-file-name PAYLOAD of one statement node,
/// never a polymorphic bound node, and it replaced a <c>ValueTuple</c> in the same list — so the value shape
/// keeps the binder's allocation profile at one list rather than one object per opened file-name.</para></summary>
public readonly record struct BoundOpenFile(FileModel File, BoundOpenMode Mode, SharingMode? Sharing,
    RetrySpec? Retry, BoundOpenTapePhrase Tape, string? Unsupported);

/// <summary>The per-file-name TAPE PHRASE of one OPEN group — the alternation
/// <c>[ REVERSED | WITH NO REWIND ]</c> that COBOL-85's OPEN general format printed beside each file-name and
/// that the superset grammar still parses as <c>openFileSpec : dataReference (REVERSED | WITH? NO REWIND)?</c>.
/// ⛔ ONE enum, never one bool per phrase: the grammar makes the alternatives mutually exclusive, and the
/// per-phrase syntax screens and the per-phrase run-time effect are then a TABLE over these members rather than
/// a chain of independent tests each of which can be forgotten (kb/Work PB668).
/// <para><see cref="NoRewind"/> survives into COBOL-2023 (§14.9.27.2, §14.9.27.4 GR11/GR12);
/// <see cref="Reversed"/> was deleted by ISO 2002 and is legal source only at <c>--std 85</c>, where
/// <c>VersionConformancePass</c>'s <c>open-reversed-removed-2002</c> gate admits it
/// (VERSION_CHANGE_REFERENCE row 7.12).</para></summary>
public enum BoundOpenTapePhrase { None, NoRewind, Reversed }

/// <summary><c>OPEN {INPUT|OUTPUT|I-O|EXTEND} [sharing] [retry] {file [WITH NO REWIND]} … …</c> (ISO §14.9.27) —
/// the statement's repeated groups flattened to §14.9.27.4 GR20's per-file-name normal form. The statement
/// itself carries NO phrase state: every phrase lives on the <see cref="BoundOpenFile"/> whose group wrote it,
/// and the REWIND phrase on the entry for the file-name that wrote it.</summary>
public sealed record BoundOpen(IReadOnlyList<BoundOpenFile> Files) : BoundStatement;

/// <summary><c>CLOSE file [WITH LOCK | REEL/UNIT] …</c> (ISO §14.9.7).</summary>
public sealed record BoundClose(IReadOnlyList<(FileModel File, BoundCloseKind Kind)> Files) : BoundStatement
{
    /// <summary>§14.9.6.4 GR5: EC-REPORT-NOT-TERMINATED checking is enabled (>>TURN) at this statement — the
    /// emitter then guards a report file's close on its reports' active state (kb/Work PB141).</summary>
    public bool ReportNotTerminatedCheck { get; init; }
}

/// <summary>The ONE <c>[BEFORE] [AFTER] ADVANCING {n LINES | PAGE}</c> phrase of a WRITE (ISO §14.9.51.2
/// Format 1): print-control output. <paramref name="Page"/> = ADVANCING PAGE (a form feed); otherwise
/// <paramref name="Lines"/> is the line count (a literal or data-name, default 1) — ONE amount, because the
/// printed format carries ONE operand however many of the two words precede it.
/// <para><paramref name="Before"/> is the §14.9.51.4 GR25 e)/f) PLACEMENT of that one advance: true = the record
/// is presented and the medium then advances (the BEFORE phrase, alone or together with AFTER — GR25 f) puts the
/// combined form's advance "after the line was presented as specified in General rule 25e"); false = the medium
/// advances and the record is then presented (a lone AFTER). The binder decides it once, from the source words,
/// so no backend re-derives it (kb/Work PB712).</para></summary>
public sealed record BoundAdvancing(bool Before, bool Page, BoundOperand? Lines);

/// <summary><c>WRITE record [FROM x] [ADVANCING …]</c> (ISO §14.9.51): <paramref name="Record"/> is the record area
/// place (its image is written); <paramref name="FromMove"/> is the BOUND implicit MOVE of the FROM phrase
/// (§14.9.51.4 GR5 a) — built by <c>MoveBinder.BindFromPhrase</c> under the MOVE statement's own syntax rules
/// (§14.9.51.3 SR6), null when no FROM phrase is written. <paramref name="Advancing"/> null = a
/// plain (data) WRITE. <paramref name="Unsupported"/> set (loud) when the owning file's organization is unsupported.
/// <paramref name="AtEop"/>/<paramref name="NotAtEop"/> are the END-OF-PAGE / NOT END-OF-PAGE imperatives (ISO
/// §14.9.51 GR27b/GR28 — run after the SUCCESSFUL write, branching on the end-of-page condition; SR19 requires
/// the file to have a LINAGE clause).</summary>
public sealed record BoundWrite(FileModel File, Place Record, BoundMove? FromMove, BoundAdvancing? Advancing,
    string? Unsupported, IReadOnlyList<BoundStatement>? AtEop = null, IReadOnlyList<BoundStatement>? NotAtEop = null) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.51 Format 1 — WITH LOCK / WITH NO LOCK), or None;
    /// on a sharing-active file WITH LOCK locks the record written (GR11), single locking releases the
    /// connector's prior lock (GR10).</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.51 GR16), or null.</summary>
    public RetrySpec? Retry { get; init; }
    // ⛔ `AfterAdvancing` LIVED HERE AND IS GONE (kb/Work PB712). It held the SECOND ADVANCING phrase of a
    // `WRITE … BEFORE ADVANCING n AFTER ADVANCING m`, a spelling §14.9.51.2 Format 1 never prints: the printed
    // phrase's choice indicators enclose the two WORDS and there is ONE `ADVANCING` with ONE operand. The pair of
    // words is a placement, not a second amount (§14.9.51.4 GR25 e)/f)), and `Advancing.Before` now carries it.
    /// <summary>The <c>INVALID KEY</c> / <c>NOT INVALID KEY</c> pair, which ISO §14.9.51.3 SR2 FORBIDS on a
    /// sequential-organization WRITE ("If the organization of the write file is sequential, format 1 shall be
    /// specified", and Format 1 of §14.9.51.2 carries no INVALID KEY bracket) — so it is non-null ONLY under
    /// <c>--permissive</c>, where the COBOLNET1720 screen warns and the bind stands (kb/Work PB691). It is
    /// carried rather than dropped because §9.1.14's final rule item 2 gives the NOT INVALID KEY imperative a
    /// LIVE meaning here — it runs on a successful completion — while the INVALID arm is provably dead: every
    /// invalid-key status (§9.1.13.5, '21'–'24') names a relative or indexed file. Null = the legal Format-1
    /// WRITE, which is every WRITE the strict compiler accepts on this organization.</summary>
    public KeyedInvalidKey? InvalidKey { get; init; }
}

/// <summary><c>READ file [NEXT|PREVIOUS] [INTO x] [AT END …][NOT AT END …]</c> on a SEQUENTIAL or LINE
/// SEQUENTIAL file (ISO §14.9.30 Format 1): a sequential read that distributes the record image into the FD
/// record (and, with INTO, MOVEs it to the receiver through <paramref name="IntoMove"/> — the BOUND implicit
/// MOVE of §14.9.30.4 GR4 b), whose sender is the §13.18.43.4 GR16 slice of the record area). The AT END /
/// NOT AT END imperatives branch on the at-end condition.
/// <para>⛔ EVERY PHRASE <c>readStatement</c> CAN PRODUCE HAS A MEMBER HERE. Three of them did not, and the
/// binder therefore never called their accessors: the record carried no direction (so <c>READ … PREVIOUS</c>
/// was a forward read at every edition AND skipped its 2002 gate), no KEY phrase (so §14.9.30.3 SR10 was
/// enforced on the keyed arm only) and no INVALID KEY phrase (so a <c>NOT INVALID KEY</c> block was compiled
/// away) — kb/Work PB334. The drift test that keeps this true for the next phrase is PB340.</para></summary>
public sealed record BoundRead(
    FileModel File, BoundMove? IntoMove, IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd, string? Unsupported) : BoundStatement, IBoundRead
{
    /// <summary>The §14.9.30.4 GR19 read kind — <see cref="ReadKind.Next"/> (written or implied by §14.9.30.3
    /// SR8) or <see cref="ReadKind.Previous"/>. <see cref="ReadKind.Random"/> cannot occur here: §12.4.5.5.2 SR2
    /// bars ACCESS MODE RANDOM/DYNAMIC on a sequential file, so SR8 always implies NEXT when no direction is
    /// written, and GR19 therefore always yields a Format-1 read on this arm.</summary>
    public ReadKind Kind { get; init; } = ReadKind.Next;
    /// <summary>The INVALID KEY / NOT INVALID KEY phrase, or null. ⛔ Format 1 HAS NO SUCH PHRASE
    /// (§14.9.30.2), so on this arm it is never conforming source and <c>SequentialIoBinder.BindRead</c>
    /// reports COBOLNET1720 for it; the carrier exists because that report routes through
    /// <c>EditionContext.Removed</c>, which under <c>--permissive</c> WARNS and leaves the bind in place. The
    /// phrase then has to MEAN something, and §14.9.30.4 GR13c is what it means — a successful READ transfers
    /// control to the NOT INVALID KEY imperative. The INVALID arm stays unrendered: a sequential-organization
    /// READ raises no '2x' status (§9.1.13.5), so the invalid key condition cannot exist for it.</summary>
    public KeyedInvalidKey? InvalidKey { get; init; }
    /// <summary>The lock-RETENTION phrase — bracket 2 of §14.9.30.2 (WITH LOCK / WITH NO LOCK); the GR7–GR12
    /// lock rules are ALL-FORMATS rules, so they bind on the sequential organization too. None = LOCK MODE
    /// governs.</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.30 GR9) — bracket 1 of §14.9.30.2, or null.</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>ADVANCING ON LOCK (§14.9.30 GR22) — bracket 1: skip-scan records locked by another connector.</summary>
    public bool AdvancingOnLock { get; init; }
    /// <summary>IGNORING LOCK (§14.9.30 GR12) — bracket 1: "the requested record is made available, even if it
    /// is locked". INDEPENDENT of <see cref="Lock"/> (§5.2.6.1), so `IGNORING LOCK WITH NO LOCK` sets both.</summary>
    public bool IgnoringLock { get; init; }
}

/// <summary><c>REWRITE record [FROM x]</c> (ISO §14.9.35): replace the last-read record with the record area's
/// image. <paramref name="FromMove"/> is the BOUND implicit MOVE of the FROM phrase, screened under the MOVE
/// statement's own syntax rules (§14.9.35.3 SR6) by <c>MoveBinder.BindFromPhrase</c>; null with no FROM.</summary>
public sealed record BoundRewrite(FileModel File, Place Record, BoundMove? FromMove, string? Unsupported) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.35 — WITH LOCK / WITH NO LOCK; the GR11/GR12 lock
    /// rules are ALL-FORMATS rules), or None.</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9 / §14.9.35 GR11), or null.</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>The <c>INVALID KEY</c> / <c>NOT INVALID KEY</c> pair, which ISO §14.9.35.3 SR2's FIRST arm
    /// forbids on a sequential-organization REWRITE — so it is non-null ONLY under <c>--permissive</c>, where
    /// the COBOLNET1720 screen warns and the bind stands. PB144 landed the screen but still DROPPED the phrase,
    /// on the reasoning that a sequential REWRITE has no '2x' condition for it to carry; that is true of the
    /// INVALID arm and false of the NOT INVALID arm, which §9.1.14's final rule item 2 runs on a SUCCESSFUL
    /// completion. Carrying the pair is the same correction PB691 made to <see cref="BoundWrite"/>, one method
    /// away in the same binder.</summary>
    public KeyedInvalidKey? InvalidKey { get; init; }
}

// ── Report Writer verbs (ISO §14.9.21 / §14.9.16 / §14.9.46; COBOLNET_REPORT_WRITER_DESIGN §5) ────────────────

/// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each report's counters/sum counters reset and the
/// report becomes active (GR1/GR4); a multi-name statement unrolls in written order (GR5).</summary>
public sealed record BoundInitiate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

/// <summary><c>GENERATE {detail | report-name}</c> (ISO §14.9.16): detail reporting prints one instance of
/// <paramref name="Detail"/> after control-break/page-fit processing (GR1); a null detail is SUMMARY reporting
/// (GR2 — the report-name form, same processing with no detail printed).</summary>
public sealed record BoundGenerate(ReportModel Report, ReportGroupModel? Detail) : BoundStatement;

/// <summary><c>SUPPRESS PRINTING</c> (ISO §14.9.45): inhibit the PRINTING of the current instance of the report
/// group named by the lexically-enclosing USE BEFORE REPORTING procedure (GR1/SR1). <paramref name="Report"/> is
/// that group's owning report, resolved at bind time (the target group is a static, lexical property — GR1). The
/// per-instance suppression itself is a RUNTIME effect (GR2): the emitted call sets a one-shot flag the report
/// engine consumes at the next group presentation, inhibiting print lines, page advance, NEXT GROUP, and
/// LINE-COUNTER changes (GR3 a–d) — but NOT sum-counter accumulation (GR7) or the end-of-group sum reset (GR2;
/// only PRESENT WHEN / OCCURS DEPENDING absence skips the reset, §13.18.54.4 GR10).</summary>
public sealed record BoundSuppress(ReportModel Report) : BoundStatement;

/// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46): final control footings + report footing, report →
/// inactive (GR3); unrolls in written order (GR4); does NOT close the file (GR6).</summary>
public sealed record BoundTerminate(IReadOnlyList<ReportModel> Reports) : BoundStatement;

// ── The EC exception-condition model (ISO §14.6.13 / §14.9.29 / §14.9.33 / §14.9.49 F3;
//    COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN D9–D12) ─────────────────────────────────────────────────────────────

/// <summary>The per-statement EC checking decision, computed at BIND time from the compile-time TurnState
/// (deep-dive D10 — bound nodes carry no parse context, so the line-anchored TURN fold happens in the binder and
/// its RESULT travels on the bound tree; the emitter renders guards from this record only).</summary>
/// <param name="Enabled">The enabled (level-3 exception-name, file, with-location) triples RELEVANT to this
/// statement's kind — EC-SIZE-* for arithmetic, EC-I-O-* per referenced file connector, EC-OVERFLOW-* for
/// STRING/UNSTRING, EC-PROGRAM-* for CALL/CANCEL, EC-ARGUMENT-FUNCTION for intrinsic-bearing statements. Never
/// empty (an empty decision binds NO wrapper — the zero-scaffolding rule). ⛔ <b>WithLocation is PER
/// (name, file) PAIR, never per statement</b> — §15.32.3 r1 / §15.30.3 r1 key the 63-spaces/one-space answer on
/// "the TURN directive … that enabled checking for THE EXCEPTION CONDITION associated with the last exception
/// status": one condition enabled WITH LOCATION must not make a sibling condition's raise record location
/// information (kb/Work R06 — the former per-statement bool did exactly that).</param>
/// <param name="StatementName">The uppercase statement name (§15.32.3 r2, Table 12).</param>
/// <param name="Location">The pre-rendered §15.30.3 r2 location string ("element; para[ OF section]; line").</param>
public sealed record EcStatementInfo(
    IReadOnlyList<(string Ec, FileModel? File, bool WithLocation)> Enabled,
    string StatementName,
    string Location);

/// <summary>A statement bound under ENABLED exception-condition checking (>>TURN … CHECKING ON in scope at its
/// line, §7.3.25.4 GR6): the emitter sets the statement EC context, emits <paramref name="Inner"/> with the
/// per-raise-point guards, and clears it. Absent wherever checking is off — checking-off emits NOTHING new.</summary>
public sealed record BoundEcChecked(BoundStatement Inner, EcStatementInfo Info) : BoundStatement;

/// <summary>A statement that ACTIVATES another runtime element — CALL, INVOKE (typed or universal), and the
/// lowered user-defined-function reference — and therefore owns ISO §14.9.18.4 GR1 b)'s question: a condition a
/// <c>GOBACK / EXIT … RAISING</c> stages in the activated element "is raised in the activating runtime element
/// if checking for that exception condition is enabled in the ACTIVATING runtime element".
///
/// <para>The activated element cannot answer that — under separate compilation it cannot even see this element —
/// and the propagated name is not known until run time (<c>RAISING LAST EXCEPTION</c>), so the answer travels as
/// the activating STATEMENT's own §7.3.25 checking state and is consulted by the site's propagation pickup.
/// Implementing this interface is the one thing that obliges a bound node to carry it; before kb/Work PB408 the
/// test was taken in the callee at bind time and the pickup was gated on nothing finer than the compilation
/// group's EC participation.</para></summary>
public interface IActivatingStatement
{
    /// <summary>The §7.3.25 checking state governing THIS statement in THIS source element
    /// (<c>TurnState.ProfileAt</c>), queryable at run time for a name chosen at run time.</summary>
    CobolNet.Runtime.Exceptions.EcCheckingProfile ActivatorChecking { get; }

    /// <summary>Return this node carrying <paramref name="profile"/>. It exists so ONE site — the
    /// <c>BindStatement</c> exit every statement funnels through — stamps every activating node, including the
    /// hoisted ones inside a desugar sequence: a new activating node inherits the stamp by implementing the
    /// interface, never by remembering to set a property at its own construction site.</summary>
    BoundStatement WithActivatorChecking(CobolNet.Runtime.Exceptions.EcCheckingProfile profile);

    /// <summary>True when this activation is not a statement of its own but an operand of one — a user-defined
    /// function reference, an inline method invocation, or an object-property accessor lowered onto an activation
    /// (kb/Work PB892). ISO §14.9.33.4 GR2 a) 2.: "for an inline invocation or a function invocation, it is the
    /// statement in which the inline invocation or function invocation was specified" that a RESUME AT NEXT
    /// STATEMENT resumes after — so its propagation pickup leaves THROUGH that carrying statement's
    /// <see cref="BoundActivationSite"/> rather than falling back into the statement it is an operand of.</summary>
    bool InExpression { get; }

    /// <summary>Return this node marked <see cref="InExpression"/>. Set at the ONE place an operand activation
    /// leaves the binder's pending lists for its carrier (<c>UdfBinder.DrainPending</c>,
    /// <c>OoBinder.OoWrapPropertyOps</c>).</summary>
    BoundStatement AsExpressionActivation();
}

/// <summary>⛔ THE STATEMENT AN OPERAND ACTIVATION BELONGS TO (kb/Work PB892) — the lowest-level statement whose
/// operands carry a function reference, an inline method invocation or an object-property accessor, wherever in
/// that statement the activation sits: hoisted before it (a <see cref="BoundSequence"/> step), or evaluated
/// per-evaluation inside one of its conditions or operands (<see cref="BoundUdfEvaluated"/> /
/// <see cref="BoundUdfEvaluatedExpr"/>).
/// <para>It is the landing of the transfer ISO §14.9.33.4 GR2 a) 2. and 3. prescribe for a condition that such an
/// activation propagates (§14.9.18.4 GR1 b)): "for an inline invocation or a function invocation, it is the
/// statement in which the inline invocation or function invocation was specified", and "the lowest level
/// statement, not the containing statement". The activation's pickup cannot express that transfer itself — an
/// expression-position activation runs inside a C# expression, where no <c>goto</c> can leave, and a hoisted one
/// runs BEFORE the statement, where falling through re-enters it — so it throws
/// <c>RaiseResumeSignal</c> and this node catches it: RESUME AT procedure-name transfers (GR3); RESUME AT NEXT
/// STATEMENT continues after <paramref name="Inner"/>.</para>
/// <para>Created by <c>StatementBinder.BindStatement</c> exactly when the statement drained an operand
/// activation, so a statement without one binds byte-identically.</para></summary>
public sealed record BoundActivationSite(BoundStatement Inner) : BoundStatement;

/// <summary><c>RAISE EXCEPTION exception-name-1</c> (ISO §14.9.29; SR1 — level-3 only, validated at bind).
/// The TURN decision is baked in at bind time (§14.6.13.1.1: an exception condition is raised only when checking
/// is enabled): <paramref name="Enabled"/> false + nonfatal ⇒ the statement is a no-op (§14.6.13.1.4 first
/// sentence — "execution continues as if the exception did not occur"); false + fatal ⇒ the implementor-defined
/// §14.6.13.1.3 #8 case — this implementation terminates loudly (§1.4). The RAISE identifier-1 (exception object)
/// form binds loud until the OO wave.</summary>
public sealed record BoundRaise(
    string EcName, bool Fatal, bool Enabled, bool WithLocation, string Location) : BoundStatement;

/// <summary><c>RESUME AT {NEXT STATEMENT | procedure-name}</c> (ISO §14.9.33): unwinds the active declarative
/// via the runtime ResumeSignal; <paramref name="TargetPc"/> is the resolved NONdeclarative pc (SR3), or the
/// NextStatement sentinel (−2) — the raise site then falls through past the raising statement (GR2).</summary>
public sealed record BoundResume(int TargetPc) : BoundStatement;

/// <summary><c>SET LAST EXCEPTION TO OFF</c> (ISO §14.9.39 Format 13): clears the run-unit last exception
/// status (§14.6.13.1.1).</summary>
public sealed record BoundSetLastException : BoundStatement;

/// <summary>What a <c>SET LOCALE … TO</c> operand names (ISO §14.9.39.2 format 11's TO brace).</summary>
public enum LocaleSetSource
{
    /// <summary><c>locale-name-1</c> — a SPECIAL-NAMES LOCALE clause's name (§14.9.39.3 SR26; GR23a / GR22).</summary>
    LocaleName,
    /// <summary><c>identifier-10</c> — a data-pointer holding a saved locale (SR27; GR21 / GR23a / GR22).</summary>
    SavedPointer,
    /// <summary><c>USER-DEFAULT</c> (GR23b).</summary>
    UserDefault,
    /// <summary><c>SYSTEM-DEFAULT</c> (GR23c).</summary>
    SystemDefault,
}

/// <summary><c>SET LOCALE {category… | USER-DEFAULT} TO {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT}</c>
/// (ISO §14.9.39 Format 11, set-locale; kb/Work PB64 T1): <paramref name="SetsUserDefault"/> for the USER-DEFAULT-first
/// form (§14.9.39.4 GR22 — the user default is set; SR25 — then the source is a locale-name or a pointer), else the
/// categories to switch (<paramref name="Categories"/> — a SET, per the format's choice indicators; GR23 — taken from the
/// source, GR25 — until another SET names them). Exactly one of <paramref name="Locale"/> (LocaleName) /
/// <paramref name="SavedPointer"/> (SavedPointer) is set; the defaults carry neither. GR24 (EC-LOCALE-MISSING) and GR21
/// (EC-LOCALE-INVALID-PTR) are runtime outcomes of <c>LocaleState</c>.</summary>
public sealed record BoundSetLocale(LocaleCategorySet Categories, bool SetsUserDefault, LocaleSetSource Source,
    LocaleSymbol? Locale, Place? SavedPointer) : BoundStatement;

/// <summary><c>SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT}</c> (ISO §14.9.39 Format 12, save-locale; kb/Work PB64
/// T1): the current locale (GR26) or the user default (GR27, <paramref name="UserDefault"/>) is saved and a reference to
/// it — a <c>SavedLocalePointer</c> handle (DETERMINATION L4) — is placed into the data-pointer (SR28).</summary>
public sealed record BoundSaveLocale(Place Target, bool UserDefault) : BoundStatement;

/// <summary>The bound RAISING phrase of GOBACK / EXIT PROGRAM (ISO §14.9.18.2 / §14.9.14.2 Format 2): either a
/// level-3 <paramref name="EcName"/> (with its catalog <paramref name="Fatal"/>ity and the bind-time TURN
/// <paramref name="Enabled"/> decision at the statement's line) or <paramref name="IsLast"/> (RAISING LAST
/// EXCEPTION — re-stages the current last exception status). The identifier (exception-object) form binds loud
/// until the OO wave.</summary>
public sealed record BoundRaising(string? EcName, bool IsLast, bool Fatal,
    Place? ObjectSource = null,
    // kb/Work R07 — the RAISING statement's §15.32.3 r2 / §15.30.3 r2 operands, per-condition like BoundRaise's
    // (§7.3.25.4 GR7 keys them on the TURN governing THIS name at THIS line). StatementName is the Table 12
    // name: GOBACK, or EXIT (EXIT PROGRAM / FUNCTION / METHOD are formats of the EXIT statement).
    bool WithLocation = false, string? StatementName = null, string? Location = null,
    // The LAST leg's §14.9.18.4 GR1b3a input: the containing source element's PROCEDURE DIVISION header RAISING
    // list. A level-3 EC-USER condition NOT in it propagates as EC-RAISING-NOT-SPECIFIED instead — decidable only
    // at run time, because the LAST arm's name is.
    IReadOnlyList<string>? PdRaising = null);
// ObjectSource: the GOBACK/EXIT … RAISING identifier-1 leg (§14.9.18.3 SR4; the EC-OO wave) — exactly one
// of EcName / IsLast / ObjectSource is set. Objects are NOT TURN-gated (§7.3.25 takes names only), so the
// Fatal field is meaningless on this leg (the §14.6.13.1.5 activator rules decide fatality).
//
// ⛔ THERE IS DELIBERATELY NO `Enabled` FIELD (kb/Work PB408, and ExceptionCheckingProfileDriftTests keeps it
// that way). §14.9.18.4 GR1 b) asks whether checking is enabled in the ACTIVATING runtime element, and the
// activator is not known when this node is bound — it is not even in the compilation group under separate
// compilation. The enablement test lives at the activating statement, which carries its own
// <see cref="CobolNet.Runtime.Exceptions.EcCheckingProfile"/>; staging here is unconditional.

/// <summary>RAISE identifier-1 (ISO §14.9.29; §14.6.13.1.5 — the EC-OO wave): raise an exception OBJECT.
/// <paramref name="Source"/> null ⇔ SELF (renders <c>this</c>). NEVER fatal by itself (GR2): the F4
/// declarative runs if one matches, else execution continues with the next statement.</summary>
public sealed record BoundRaiseObject(Place? Source) : BoundStatement;
