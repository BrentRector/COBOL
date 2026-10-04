// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;

namespace CobolNet.Editions.Diagnostics;

/// <summary>
/// The first-class compiler-diagnostic catalogue (rearch PHASE 02, P2.10). ONE home, below both the frontend and
/// the compiler, for the diagnostics that used to be bare <c>COBOLNETnnnn</c> string literals: the edition band
/// (single-sourced from <see cref="EditionCodes"/>), the digit-capacity codes, the reused <c>COBOLNET1533</c>
/// strong-type rules split by ISO §, and — the bulk — the <c>COBOLNET0899</c> "recognized but not implemented"
/// catch-all, split into one addressable descriptor per deferred feature / per validation rule.
/// </summary>
/// <remarks>
/// <para>
/// <b>The 0899 split (fixes P8).</b> <c>COBOLNET0899</c> was emitted from ~44 sites conflating two categories:
/// (a) LEGAL-but-not-yet-implemented features (national data, several Report Writer clauses, OO refinements) and
/// (b) genuine SEMANTIC VALIDATION errors that happen to share the code (an unresolved Report Writer CONTROL /
/// SOURCE / SUM operand, a receiving-side LINE-COUNTER, …). Every site now references a descriptor here. The
/// emitted CODE stays <c>COBOLNET0899</c> (byte-stable — goldens and the differential net pin the text), but each
/// site is now addressable by a stable <see cref="DiagnosticDescriptor.Id"/>. The DEFERRALS carry the shared
/// <see cref="RecognizedNotImplemented"/> suppress family so a developer can mute all "not implemented yet"
/// diagnostics at once WITHOUT also muting the validation errors; the validation descriptors keep the default
/// (suppress-by-code) family. Reclassifying the validation errors onto proper per-rule codes (out of the 0899
/// bucket) is a tracked follow-on (P3 validator work) — it changes emitted codes, so it re-baselines goldens
/// under review and is out of P2's byte-stable scope.
/// </para>
/// <para>
/// <b>Reused-code disambiguation.</b> <c>COBOLNET1533</c> is emitted for THREE distinct strong-type rules
/// (§14.9.25.3 SR2 MOVE, §8.8.4.4.3 SR1 class condition, §8.8.4.2.3 SR1 comparison). Each now has its own
/// descriptor / <see cref="DiagnosticDescriptor.Id"/> while keeping the shared emitted code.
/// </para>
/// <para>
/// <b>Scope.</b> P2 catalogues the edition band + the 0899 split + the 1533 reuse — the P8-named problems. The
/// broader "every one of the ~163 compiler codes → a descriptor + a <c>sink.Report</c>" migration (and unifying
/// the frontend's parse-layer descriptor catalogue + its three-value <c>DiagnosticSeverity</c> down into this
/// home) is the P7 follow-on. <see cref="All"/> reflects the public descriptor fields, so a new descriptor is
/// picked up by <c>docs/DIAGNOSTICS.md</c> generation and the drift test without a hand-maintained list.
/// </para>
/// </remarks>
public static class DiagnosticCatalog
{
    /// <summary>The shared emitted code for the "recognized but not implemented" bucket (kept byte-stable).</summary>
    private const string NotImplemented = "COBOLNET0899";

    /// <summary>The shared emitted code for the strong-type rules (§8.5 strong typing; reused across 3 rules).</summary>
    private const string StrongType = "COBOLNET1533";

    /// <summary>The <c>--suppress</c> family grouping every LEGAL-but-deferred-feature diagnostic (the honest
    /// half of the old 0899 catch-all) so it can be muted as a group during development.</summary>
    public const string RecognizedNotImplemented = "recognized-not-implemented";

    /// <summary>The <c>--suppress</c> family grouping every refusal of an Annex A.4 OPTIONAL element whose
    /// support this implementation does not claim (§4.2.7 / A.4.1). One key, because a migrating program that
    /// wants to see past ONE declined module almost always wants to see past all of them; the per-module
    /// identity stays in the message, which names the module and its A.4 item.</summary>
    public const string DeclinedOptionalElement = "declined-optional-element";

    // ── Edition band (single-sourced from EditionCodes) ──────────────────────────────────────────────
    public static readonly DiagnosticDescriptor EditionIntroduction = new(
        EditionCodes.Introduction, "edition-introduction", EditionSeverity.Error,
        "A construct is used below the edition that introduced it (requires a newer --std).", "ISO §ann. per construct");
    public static readonly DiagnosticDescriptor EditionReservedWord = new(
        EditionCodes.ReservedWord, "edition-reserved-word", EditionSeverity.Error,
        "A word reserved in the targeted edition is used as a user-defined word.", "ISO §8.9");
    public static readonly DiagnosticDescriptor EditionRemovedConstruct = new(
        EditionCodes.RemovedConstruct, "edition-removed-construct", EditionSeverity.Error,
        "A construct removed by the targeted edition is used (error strict / warning permissive).", "ISO ann. E.2");
    public static readonly DiagnosticDescriptor EditionObsoleteFlag = new(
        EditionCodes.ObsoleteFlag, "edition-obsolete-flag", EditionSeverity.Warning,
        "An obsolete/archaic element is used (still conforming; flagged).", "ISO §4.2.12/§4.2.13, ann. F.2");

    // ── Digit-capacity band (emitted by EditionContext.CheckDigitCapacity) ───────────────────────────
    public static readonly DiagnosticDescriptor DigitCapacityOver31 = new(
        "COBOLNET0801", "digit-capacity-over-31", EditionSeverity.Error,
        "A fixed-point item/literal exceeds the 31-digit ISO limit.", "ISO §8.3.3.3.2");
    // ── The §8.3.2.1 word-length ceiling — ONE rule (CobolWordRule), reported from the tree-walk funnel
    //    (VersionConformancePass.VisitCobolWord) AND the directive stages (>>TURN operands, >>DEFINE names),
    //    which never reach the tree walk (kb/Work R05's sweep). ─────────────────────────────────────────
    public static readonly DiagnosticDescriptor WordLengthExceeded = new(
        "COBOLNET1567", "word-length-exceeded", EditionSeverity.Error,
        "A COBOL word exceeds the edition's length ceiling: 63 characters at COBOL-2023 (Annex E.3.3 item 11 — "
        + "a relaxation, so a 32..63-character word below 2023 is a length error, not an introduction gate), "
        + "31 at 2002/2014, 30 at 1985.", "ISO §8.3.2.1");
    public static readonly DiagnosticDescriptor DigitCapacityOver18Pre2002 = new(
        "COBOLNET0802", "digit-capacity-over-18-pre-2002", EditionSeverity.Error,
        "A fixed-point item/literal exceeds the 18-digit COBOL-85 limit (19–31 need --std 2002+).", "ISO §8.3.3.3.2");

    // ── Compiler-directing facility band (emitted by the frontend directive processors). Registered by the
    //    P13 plan-vs-spec remediation (review finding C1's recurrence guard): every emitted code must be a
    //    catalog descriptor so the next-free allocation scan and DIAGNOSTICS.md see it — these four were bare
    //    frontend literals, the same channel that shipped the COBOLNET1573 collision. ─────────────────────────
    public static readonly DiagnosticDescriptor TurnDirectiveMalformed = new(
        "COBOLNET0718", "turn-directive-malformed", EditionSeverity.Error,
        "A >>TURN directive is malformed: the format is '>>TURN {exception-name [file-name]…}… CHECKING "
        + "{ON [WITH LOCATION] | OFF}' — an unexpected word, a missing CHECKING phrase, or a repeated "
        + "exception-name/file-name combination is rejected (ISO §7.3.25.2 / §7.3.25.3 SR1, SR3).",
        "ISO §7.3.25.2 / §7.3.25.3 SR1/SR3");
    public static readonly DiagnosticDescriptor TurnFileNameNonIo = new(
        "COBOLNET0719", "turn-file-name-non-io", EditionSeverity.Error,
        "A >>TURN file-name may follow only an exception-name beginning 'EC-I-O' (ISO §7.3.25.3 SR4).",
        "ISO §7.3.25.3 SR4");
    // ⛔ COBOLNET0875 ("turn-directive-below-2002") is RETIRED — NEVER REALLOCATE IT. It was >>TURN's own
    //    hand-rolled `if (dialectLevel < 2002)`, one of THREE mechanisms implementing the single rule "a
    //    compiler directive is rejected below its introducing edition". kb/Work PB725 reconciled all three onto
    //    the ONE ConstructRegistry funnel, so >>TURN below 2002 is now COBOLNET0900 like every other directive
    //    (registry row turn-directive-2002). The COBOLNET1518 precedent applies: a retired code stays retired.
    // ── Written exception-name resolution — ONE funnel (EcNameResolution; kb/Work R05). The unknown-name and
    //    introduction-gate texts existed as four verbatim copies each before the funnel. ────────────────────────
    public static readonly DiagnosticDescriptor EcNameUnknown = new(
        "COBOLNET0711", "ec-name-unknown", EditionSeverity.Error,
        "A written exception-name is neither in the §14.6.13.1 catalog nor a valid EC-USER- name (suffix of basic "
        + "letters/digits/hyphen/underscore, not ending in hyphen or underscore). EC-IMP-suffix names are defined by "
        + "the implementor, and this implementation defines none.",
        "ISO §14.6.13.1.1");
    public static readonly DiagnosticDescriptor EcNameIntroducedLater = new(
        "COBOLNET0878", "ec-name-introduced-later", EditionSeverity.Error,
        "An exception-name belongs to a family introduced by a later edition than the targeted one (the "
        + "2023-only families — VERSION_CHANGE_REFERENCE rows 40/61).", "ISO §14.6.13.1");
    public static readonly DiagnosticDescriptor EcNameWiderThanStatus = new(
        "COBOLNET1636", "ec-name-wider-than-exception-status", EditionSeverity.Warning,
        "A level-3 exception-name is longer than the 31-character value FUNCTION EXCEPTION-STATUS returns "
        + "(§15.33.3 r1 fixes the width while COBOL-2023 words run to 63 characters, §8.3.2.1, and the "
        + "§14.6.13.1.1 open-family suffixes are unbounded) — this name and any other sharing its first 31 "
        + "characters are indistinguishable through that one function. Checking, declarative selection, and "
        + "WHEN matching use the full name. See COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN §15.33.",
        "ISO §15.33.3 r1 / §8.3.2.1 / §14.6.13.1.1");
    // ⛔ COBOLNET0883 ("propagate-directive") is RETIRED — NEVER REALLOCATE IT. It was >>PROPAGATE's own
    //    malformed-operand code, one of SIX spellings of the single rule "a directive's operand shall be one the
    //    directive's general format admits" (ISO §7.3.3 SR6). kb/Work PB794 made that rule DATA — the
    //    `directiveOperand` column of constructs.json — checked once at the point a >> word is recognized, so
    //    >>PROPAGATE MAYBE is now COBOLNET1911 like every other closed-operand directive, and the seven
    //    directives that had no such code at all stopped compiling malformed lines in silence. PB725 had already
    //    taken this descriptor's edition half onto COBOLNET0900; this is the other half. The COBOLNET0875 and
    //    COBOLNET1518 precedents apply: a retired code stays retired.

    // ── COBOLNET0814 — the literal LENGTH rule (§8.3.3.2.3 SR1 / §8.3.3.4.3 SR1 / §8.3.3.5.3 SR1) ──────
    // kb/Work PB1393. The code predates the catalog (the Phase-4a "0814 band"), and was written as a bare string at
    // the two procedure-operand funnels that checked it — the national and boolean ones only, so an over-long
    // alphanumeric literal, and ANY literal in a VALUE clause, CONSTANT entry or ALL figurative, compiled clean. It
    // is reported by exactly one site now: LiteralScreenPass, which sees every literal token in the unit.
    public static readonly DiagnosticDescriptor LiteralTooLong = new(
        "COBOLNET0814", "literal-too-long", EditionSeverity.Error,
        "A literal shall be at most 8,191 character positions of its class, excluding the separators that delimit "
        + "it: alphanumeric (§8.3.3.2.3 SR1, the X\"…\" format included), boolean (§8.3.3.4.3 SR1, a BX\"…\" "
        + "digit counting as four boolean positions) or national (§8.3.3.5.3 SR1).",
        "ISO §8.3.3.2.3 SR1 / §8.3.3.4.3 SR1 / §8.3.3.5.3 SR1");

    // ── COBOLNET1540/1541/1545 — concatenation expressions, one code per rule (§8.8.3) ───────────────
    public static readonly DiagnosticDescriptor ConcatClassMismatch = new(
        "COBOLNET1540", "concat-class-mismatch", EditionSeverity.Error,
        "Both operands of a concatenation expression shall be of the same class — alphanumeric, boolean, or "
        + "national (a figurative constant takes the other operand's class).", "ISO §8.8.3.2 SR1");
    public static readonly DiagnosticDescriptor ConcatAllFigurative = new(
        "COBOLNET1541", "concat-all-figurative", EditionSeverity.Error,
        "Neither operand of a concatenation expression shall be a figurative constant that begins with the "
        + "word ALL.", "ISO §8.8.3.2 SR1");
    public static readonly DiagnosticDescriptor ConcatResultTooLong = new(
        "COBOLNET1545", "concat-result-too-long", EditionSeverity.Error,
        "The value resulting from concatenation shall be at most 8,191 character positions (alphanumeric, "
        + "boolean, or national).", "ISO §8.8.3.2 SR2–SR4");
    /// <summary>kb/Work PB1406: a word written as a concatenation operand that stands for no literal.</summary>
    public static readonly DiagnosticDescriptor ConcatOperandNotLiteral = new(
        "COBOLNET2473", "concat-operand-not-literal", EditionSeverity.Error,
        "An operand of a concatenation expression is a literal (ISO §8.8.3.1 — literal-1 & literal-2) or a figurative "
        + "constant (§8.8.3.2 SR1). The only words that stand for one are a constant-name (§13.10.3 SR2) and a "
        + "symbolic-character, which is a figurative constant (§12.3.7.4 GR11); a data-name or any other word is not.",
        "ISO §8.8.3.1 · §13.10.3 SR2 · §12.3.7.4 GR11");
    /// <summary>kb/Work PB1406: §12.3.7.3 SR11 asked of a concatenation operand inside SPECIAL-NAMES.</summary>
    public static readonly DiagnosticDescriptor ConcatSymbolicCharacterInSpecialNames = new(
        "COBOLNET2474", "concat-symbolic-character-in-special-names", EditionSeverity.Error,
        "ISO §12.3.7.3 syntax rule 11: \"Literal-1, literal-2, literal-3, literal-4, literal-5, literal-6, and "
        + "literal-9 shall specify neither a symbolic-character figurative constant nor a zero-length literal.\" A "
        + "concatenation expression written in one of those positions shall not have a symbolic-character operand.",
        "ISO §12.3.7.3 SR11");

    // ── COBOLNET1547/1548/1549 — constant entries + CONSTANT RECORD, one code per rule family (§13.10 /
    //    §13.18.15; P10 Step 15). 1547 = the §13.10 constant-entry syntax rules; 1548 = the receiving-operand
    //    rejection (a constant substitutes a LITERAL — §13.10.3 SR2/GR1 — and §13.18.15.3 SR2 forbids storing
    //    into a structured constant); 1549 = the CONSTANT RECORD structural rules (§13.18.15.3 SR1 +
    //    §13.16.3 SR3/SR6/SR13). 1540–1546 taken; 1550/1551/1552 are unallocated mid-band holes (the PHASE-12 earmark expired unused); 1560 = the Annex A.4.2 screen-handling refusal (below). ──
    public static readonly DiagnosticDescriptor ConstantEntryRule = new(
        "COBOLNET1547", "constant-entry-rule", EditionSeverity.Error,
        "A constant entry violates a §13.10.3 syntax rule (figurative operand SR6; duplicate constant-name SR9; "
        + "ANY-LENGTH / dynamic-length LENGTH operand SR10/SR12; non-integer constant where an integer is "
        + "required SR2; non-literal / exponentiation / division-by-zero in the compile-time expression SR7, "
        + "per §7.3.6.2).",
        "ISO §13.10.3 / §7.3.6.2");
    public static readonly DiagnosticDescriptor ConstantAsReceiver = new(
        "COBOLNET1548", "constant-as-receiver", EditionSeverity.Error,
        "A constant-name or a data item of a CONSTANT RECORD shall not be specified as a receiving operand — a "
        + "constant substitutes a literal, and a structured constant's content cannot be modified.",
        "ISO §13.10.3 SR2 / §13.18.15.3 SR2");
    public static readonly DiagnosticDescriptor ConstantRecordRule = new(
        "COBOLNET1549", "constant-record-rule", EditionSeverity.Error,
        "A CONSTANT RECORD clause violates a structural rule: WS/LS sections only (SR1); level-01 only, no "
        + "REDEFINES, and no ANY LENGTH / BASED / BLANK WHEN ZERO / SYNCHRONIZED / TYPEDEF on the record or any "
        + "subordinate (§13.16.3 SR3/SR6/SR13); no format 2 OCCURS or DEPENDING phrase in a subordinate item and no "
        + "dynamic-capacity table on the record's items (§13.18.38.3 SR19/SR23/SR33).",
        "ISO §13.18.15.3 / §13.16.3 / §13.18.38.3");

    // ── COBOLNET0895 — a numeric literal's decimal separator against the DECIMAL-POINT mode (§12.3.7.4 GR14a). It
    //    was a bare code at its three emit sites (the literal chokepoint and the constant-entry evaluator's router)
    //    with no descriptor, so docs/DIAGNOSTICS.md never listed it (kb/Work PB1231's semgrep sweep).
    public static readonly DiagnosticDescriptor NumericLiteralDecimalSeparator = new(
        "COBOLNET0895", "numeric-literal-decimal-separator", EditionSeverity.Error,
        "A numeric literal's decimal separator does not match the DECIMAL-POINT mode: under DECIMAL-POINT IS COMMA "
        + "the decimal separator written in numeric literals shall be the comma, and without it only the period is "
        + "a decimal point.", "ISO §12.3.7.4 GR14a / §8.3.3.3.2");

    // ── COBOLNET0899 — a file's IMPLICITLY shared record area of a shape the storage model cannot share (kb/Work PB836)
    /// <summary>An FD/SD's level-1 records (ISO §13.18.33.4 GR3) or a record-area SAME clause's files' records
    /// (§12.4.6.4.4 GR2) share ONE area, and the typed-native storage model cannot yet carry one of them in that
    /// shared area. No syntax rule forbids the source; this is recognized-not-implemented debt, NOT the
    /// §13.18.44.3 REDEFINES-clause rejection the binder used to borrow for it (the source contains no REDEFINES
    /// clause). ⚠ Since kb/Work PB981 a dynamic-length, variable-length-group or pointer-class record is NOT such a
    /// shape — it is an OUT-OF-LINE record (determination D-FRA, docs/CONFORMANCE.md §3) and compiles. What remains
    /// is a character-window record with a byte-window residue (<c>DataBinder.ByteWindowResidueOf</c>). (The
    /// run-unit EXTERNAL area of a file with an out-of-line record left this diagnostic at kb/Work PB1026: each
    /// out-of-line record is a run-unit cell of its own.)</summary>
    public static readonly DiagnosticDescriptor ImplicitRecordAreaShape = new(
        NotImplemented, "implicit-record-area-shape", EditionSeverity.Error,
        "The records of one file description (ISO §13.18.33.4 GR3), or of the files of one record-area SAME "
        + "clause (§12.4.6.4.4 GR2), share one storage area, and one of them is of a shape the storage model "
        + "cannot yet carry in that shared area: a shared record area of that shape is recognized but not yet "
        + "implemented.", "ISO §13.18.33.4 GR3 / §12.4.6.4.4 GR2",
        RecognizedNotImplemented);

    // ── COBOLNET0899 — a constant's LENGTH OF / BYTE-LENGTH OF operand described later, whose description cannot be
    //    bound ahead of the reference that needs the constant (kb/Work PB1231, PB1226)
    /// <summary>A constant entry's length phrase may name a data item described after it (§13.10.3 SR4 forbids only a
    /// length that depends on the constant). The binder serves that by binding the operand's RECORD out of source
    /// order when the value is first needed. Two shapes it cannot bind ahead are recognized-not-implemented debt,
    /// never a misreported undefined name: part of a record whose earlier entries are being bound
    /// (<c>01 R. 05 A PIC X(K). 05 W PIC X(7).</c> with <c>01 K CONSTANT AS LENGTH OF W.</c>), and an elementary
    /// report item of a report group the report binder has not reached (§13.10.3 SR11's operand).</summary>
    public static readonly DiagnosticDescriptor ConstantLengthOperandBoundLater = new(
        NotImplemented, "constant-length-operand-bound-later", EditionSeverity.Error,
        "A constant entry's LENGTH OF / BYTE-LENGTH OF operand is described later, and the constant is referenced "
        + "before that description can be bound (inside a record that is still being bound, or in a report group the "
        + "REPORT SECTION has not reached): measuring it out of source order is recognized but not yet implemented.",
        "ISO §13.10.3 SR4 / §13.10.4 GR5-GR6", RecognizedNotImplemented);

    // ⛔ The two 0899-staged constant-entry legs are GONE. ConstantFromCompilationVariable (kb/Work PB1368): the
    // >>DEFINE table now reaches the binder as a timeline (DirectiveResults.CompilationVariables), and an undefined
    // FROM name is the permanent §13.10.3 SR8 rejection under ConstantEntryRule. ConstantByteLength (kb/Work PB1227):
    // its premise — "the §15.14 intrinsic itself is a Deferred catalog row" — had been false since FUNCTION BYTE-LENGTH
    // landed; BYTE-LENGTH OF data-name-1 binds through BindConstantLength beside LENGTH OF.

    // ⛔ The 0899-staged CallAsPrototypeName is GONE (kb/Work PB237). It said "§12.3.8.2's program-specifier has no
    // repositoryEntry alternative, so no source can declare one"; the alternative now exists, the registry resolves,
    // and a prototype-name that is not declared is a permanent §14.9.4.3 SR16 conformance rejection — COBOLNET1760
    // below — not recognized-not-implemented debt. Same retirement shape as CallAsNestedNeedsLiteral before it.
    // The ONE §14.9.4.3 SR15 diagnostic (kb/Work PB131) — both sentences: literal-1 required, and the name
    // resolved at BIND time against the containment tree (a directly contained program, or a visible common
    // program per §8.4.6.3). Replaced the 0899-staged CallAsNestedNeedsLiteral: this is a permanent
    // conformance rejection, not recognized-not-implemented debt.
    // The §13.18.44.3 SR12/SR14 REDEFINES class screen (kb/Work PB179 — the Step D Tier-D arm's bind
    // half): a permanent conformance rejection of the ENTRY-level shapes the rules name. The NESTED
    // pointer/object-leaf shape is NOT these rules' letter and takes ComputeTier's staged-loud arm instead.
    // The L1–L3 phrase-placement leniency family (kb/Work PB144), finally gated. THREE syntax rules across
    // READ/REWRITE/DELETE close a phrase out of a particular access mode or organization, and all three were
    // bound unconditionally with a "CCVS-lenient" comment and NO strict arm — so strict `--std 2023` accepted
    // source the standard forbids and said nothing. One reusable descriptor rather than three near-identical
    // ones, on the COBOLNET1694 precedent: the SHAPE is one rule ("a phrase is written where this statement's
    // syntax rules forbid it") and each site's message quotes its own §/SR. Emitted through
    // StatementValidation.ScreenForbiddenPhrase → EditionContext.Removed, so it is an ERROR under strict and a
    // WARNING with an UNCHANGED bind under --permissive, which is what keeps the CCVS-85 corpus compiling.
    // kb/Work PB691 added the WRITE arm, whose defect was the OTHER shape: the phrase was parsed and then
    // DROPPED, so neither arm existed to keep. The screen is the same one; the --permissive bind had to be
    // built (BoundWrite.InvalidKey), because §9.1.14's final rule item 2 gives the NOT INVALID arm a live
    // meaning on a successful WRITE even where the INVALID arm is unreachable.
    public static readonly DiagnosticDescriptor IoPhraseForbiddenHere = new(
        "COBOLNET1720", "io-phrase-forbidden-here", EditionSeverity.Error,
        "An input-output statement specifies a phrase its syntax rules exclude for that file's organization or "
        + "access mode. ISO §14.9.10.3 syntax rule 2: \"The INVALID KEY and the NOT INVALID KEY phrases shall "
        + "not be specified for a DELETE RECORD statement that references a file that is in sequential access "
        + "mode.\" ISO §14.9.35.3 syntax rule 2: \"Neither the INVALID KEY phrase nor the NOT INVALID KEY "
        + "phrase shall be specified for a REWRITE statement that references a file with sequential "
        + "organization or a file with relative organization and sequential access mode.\" ISO §14.9.51.3 "
        + "syntax rule 2: \"If the organization of the write file is sequential, format 1 shall be "
        + "specified.\" — and Format 1 of §14.9.51.2 carries no INVALID KEY bracket, so the phrase is "
        + "excluded from a sequential-organization WRITE. Its mirror, syntax rule 3: \"If the organization of "
        + "the write file is indexed or relative, format 2 shall be specified.\" — and Format 2 carries neither "
        + "the ADVANCING nor the END-OF-PAGE phrase, so those are excluded from a relative or indexed WRITE in "
        + "BOTH lanes (Format 2 gives a print-control phrase no meaning to bind under --permissive). ISO §14.9.30.3 "
        + "syntax rule 6: \"None of the phrases ADVANCING, AT END, NEXT, NOT AT END, or PREVIOUS shall be "
        + "specified if ACCESS MODE RANDOM is specified in the file control entry for file-name-1.\" "
        + "ISO §14.9.30.3 syntax rule 7: \"The phrase PREVIOUS shall not be specified if FILE ORGANIZATION "
        + "LINE SEQUENTIAL is specified in the file control entry for file-name-1.\" And the READ general "
        + "format itself: §14.9.30.2 Format 1 has no INVALID KEY bracket, and §12.4.5.5.2 SR2 + §14.9.30.3 SR8 "
        + "+ §14.9.30.4 GR19 make every READ of a sequential-organization file a Format-1 read, so the phrase "
        + "has no format to belong to there. "
        + "Under --permissive the phrase is tolerated and BOUND with the meaning ISO §9.1.14 gives it: the "
        + "INVALID arm is dead in the status-first branches wherever the forbidding condition also excludes "
        + "the '2x' family (never silently rerouted), while §9.1.14's final rule item 2 still runs the NOT "
        + "INVALID arm on a successful completion. That leniency is what the CCVS-85 corpus depends on; "
        + "dropping the phrase instead of binding it was kb/Work PB691. On a sequential READ the same "
        + "obligation is §14.9.30.4 GR13c, which transfers control to NOT INVALID KEY on a successful read "
        + "(kb/Work PB334).",
        "ISO §14.9.10.3 SR2 · §14.9.35.3 SR2 · §14.9.51.3 SR2 · §14.9.51.3 SR3 · §14.9.30.3 SR6 · §14.9.30.3 SR7 "
        + "· §14.9.30.2 Format 1");
    // The MIRROR of COBOLNET1720, and the falsely-PERMISSIVE twin of the OCR's falsely-restrictive bias
    // (kb/Work PB350). §5.2.6.2 makes BRACKETS the only thing that lets a portion of a general format be
    // omitted, and §5.2.2 makes an underlined keyword required subject to those conventions — so a phrase
    // line printed with NO brackets is MANDATORY, and a grammar that writes it `phrase?` under-rejects.
    // The rule is a property of the printed FORMAT, so one descriptor carries the shape and each call site
    // quotes its own §, exactly as COBOLNET1694/COBOLNET1720 do. It is screened at BIND time, not in the
    // grammar, on purpose: the grammar already builds a tree the binder can read, and a bind-time
    // diagnostic NAMES the rule and the omitted phrase where a parse error can only say "extraneous input".
    // MEASURED SWEEP (2026-09-05, scripts/spec sweep over every `General format` <pre> block in
    // specs/ISO_COBOL.md): §14.9.34.2's `AT END imperative-statement-1` is the ONLY unbracketed conditional
    // phrase line in any §14.9 statement format. SEARCH's two `WHEN` groups are the only other mandatory
    // phrases and the grammar already writes them `+`. RequiredFormatPhraseDriftTests re-derives that.
    public static readonly DiagnosticDescriptor FormatRequiredPhraseOmitted = new(
        "COBOLNET1850", "format-required-phrase-omitted", EditionSeverity.Error,
        "A statement omits a phrase its general format prints WITHOUT brackets, and is therefore incomplete. "
        + "ISO §5.2.6.2: brackets \"enclosing a portion of a general format indicate that the syntax element "
        + "contained within the brackets or one of the alternatives contained within the brackets may be "
        + "explicitly specified or that portion of the general format may be omitted\" — nothing else in the "
        + "format conventions makes a portion omissible. ISO §5.2.2: an underlined keyword is \"required in "
        + "order to select the functionality associated with that keyword, subject to the conventions "
        + "specified in 5.2.6, Options\". ISO §14.9.34.2 prints RETURN's `AT END imperative-statement-1` line "
        + "with no brackets while bracketing `[ NOT AT END … ]` and `[ END-RETURN ]` on the lines around it, "
        + "so the AT END phrase is mandatory on EVERY RETURN — including the §14.9.34.3 SR4 reversed order, "
        + "which permits reversal of the two phrases \"when specified\" and does not make either omissible. "
        + "Until kb/Work PB350 the grammar wrote the phrase `returnAtEndPhrase?` and the reversed alternative "
        + "made its AT END half optional too, so a RETURN with no AT END compiled clean at all four editions "
        + "and, at end of data, §14.9.34.4 GR3's \"the contents of the record area … are undefined\" branch "
        + "ran with control falling THROUGH the statement: a loop written on RETURN could never terminate "
        + "from the statement, and re-displayed the previous record instead.",
        "ISO §14.9.34.2 · §5.2.2 · §5.2.6.2");

    public static readonly DiagnosticDescriptor RedefinesPointerObject = new(
        "COBOLNET1697", "redefines-pointer-object", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 12: \"The REDEFINES clause shall not be specified for a data item of "
        + "class object, message-tag, or pointer or a strongly-typed group item\"; syntax rule 14: "
        + "\"Data-name-2 shall not be of class object, message-tag, or pointer, a strongly-typed group "
        + "item, or an item subordinate to a strongly-typed group item.\"",
        "ISO §13.18.44.3 SR12/SR14");
    // The §13.18.44.3 SR17 REDEFINES screen (kb/Work PB177 arm C — the decision-free half). SR17 is a
    // SYMMETRIC rule naming BOTH sides of the entry, and only its dynamic-CAPACITY half was screened (as
    // COBOLNET1525, and under SR5's number — SR5's own sentence is "Neither the original definition nor the
    // redefinition shall include an occurs-depending table", a DIFFERENT rule, which COBOLNET0855 already
    // enforces). The dynamic-LENGTH half was not merely unscreened but SILENTLY MIS-MODELLED: StorageFormPass
    // classifies IsDynamicLength BEFORE the Tier-B view arm, so `01 A PIC X(8). 01 B REDEFINES A. 05 D PIC X
    // DYNAMIC LENGTH.` gave the view its OWN disjoint native string — two storages for one shared area, no
    // diagnostic (measured: MOVE "ZZ" TO D left A unchanged, violating §13.18.44.4 GR1). "Variable-length
    // group" is §8.5.1.12.1's defined term: a group with a dynamic-length elementary item or a
    // dynamic-capacity table subordinate.
    public static readonly DiagnosticDescriptor RedefinesVariableLength = new(
        "COBOLNET1698", "redefines-variable-length", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 17: \"Neither data-name-2 nor the subject of the entry shall be a "
        + "variable-length group or a dynamic-length elementary item.\" A variable-length group is "
        + "\"a group item whose data description has at least one dynamic-length elementary item or "
        + "dynamic-capacity table as a subordinate item\" (ISO §8.5.1.12.1).",
        "ISO §13.18.44.3 SR17");

    // ── The program-prototype registry's two conformance rejections (kb/Work PB237; §12.3.8.2 program-specifier) ──
    // ONE code per rule FAMILY, message-differentiated, the COBOLNET1720/1707 shape: 1760 answers "is this word a
    // declared program-prototype-name?" for every reference site (CALL Format 2, CANCEL), 1761 screens the
    // DECLARATION itself (§12.3.8.3's ALL-SPECIFIERS rules 1 and 2, restricted to the program-specifier).
    public static readonly DiagnosticDescriptor ProgramPrototypeUndeclared = new(
        "COBOLNET1760", "program-prototype-undeclared", EditionSeverity.Error,
        "A program-prototype-name shall be declared by a program-specifier in the REPOSITORY paragraph "
        + "(ISO §12.3.8.2: \"PROGRAM program-prototype-name-1 [AS literal-3]\"). §14.9.4.3 syntax rule 16: "
        + "\"Program-prototype-name-1 shall be specified in a program-specifier in the REPOSITORY paragraph.\" "
        + "§14.9.5.3 syntax rule 3: \"Program-prototype-name-1 shall be a program prototype specified in the "
        + "REPOSITORY paragraph.\" §8.4.6.8 admits one further spelling — the program-name of a CONTAINING "
        + "program definition — and this check accepts that too.",
        "ISO §14.9.4.3 SR16 / §14.9.5.3 SR3 / §12.3.8.2 / §8.4.6.8");
    public static readonly DiagnosticDescriptor RepositoryProgramSpecifier = new(
        "COBOLNET1761", "repository-program-specifier", EditionSeverity.Error,
        "A REPOSITORY specifier's declaration rules (ISO §12.3.8.3, ALL SPECIFIERS) — asked of the class, interface, "
        + "program, property and user-defined-function specifiers alike (kb/Work PB974). Rule 1: \"If "
        + "any object-class-name-1, interface-name-2, program-prototype-name-1, function-prototype-name-1, "
        + "intrinsic-function-name-1 or property-name-1 is specified more than once in the REPOSITORY paragraph, "
        + "all the specifications for that name shall be identical.\" Rule 2: \"Literal-1, literal-2, literal-3, "
        + "literal-4, and literal-5 shall be alphanumeric literals or national literals and shall be neither "
        + "figurative constants nor zero-length literals.\"",
        "ISO §12.3.8.3 SR1/SR2");

    // ── The identification-division AS externalized-name phrase (kb/Work PB303) ─────────────────────
    /// <summary>COBOLNET1794 — the AS-phrase literal's own syntax rule, in whichever id paragraph carries the
    /// phrase. ONE code because it is ONE rule restated by five clauses; the message names the paragraph and
    /// cites that clause, so a reader is never sent to the wrong one.</summary>
    public static readonly DiagnosticDescriptor ExternalizedNameLiteral = new(
        "COBOLNET1794", "externalized-name-literal", EditionSeverity.Error,
        "The literal of an identification-division AS phrase. ISO §11.10.3 syntax rule 1 (PROGRAM-ID), §11.5.3 "
        + "syntax rule 1 (FUNCTION-ID), §11.6.3 syntax rule 1 (INTERFACE-ID) and §11.7.3 syntax rule 1 "
        + "(METHOD-ID) all read: \"Literal-1 shall be an alphanumeric literal or a national literal and shall be "
        + "neither a figurative constant nor a zero-length literal.\" §11.3.3 syntax rule 1 (CLASS-ID) states "
        + "the same rule WITHOUT the zero-length exclusion — \"shall be an alphanumeric literal or a national "
        + "literal and shall not be a figurative constant\" — and that asymmetry is honoured exactly.",
        "ISO §11.10.3 SR1 / §11.5.3 SR1 / §11.3.3 SR1 / §11.6.3 SR1 / §11.7.3 SR1");

    /// <summary>COBOLNET2642 — an AS literal's leading or trailing spaces are removed when the externalized name is
    /// formed (§8.3.2.2 2) leaves formation to the implementor; DOC-A.1-68 fixes it; kb/Work PB1539).</summary>
    public static readonly DiagnosticDescriptor ExternalizedNameSpaces = new(
        "COBOLNET2642", "externalized-name-spaces", EditionSeverity.Warning,
        "An AS phrase's literal has leading or trailing spaces. ISO §8.3.2.2 rule 2 makes the literal's content a "
        + "name externalized to the operating environment and leaves its formation rules to the implementor; "
        + "WiseOwl COBOL forms every externalized name — an AS literal, a CALL, CANCEL or program-address target, an "
        + "INVOKE method name — by removing leading and trailing spaces (docs/CONFORMANCE.md DOC-A.1-68), so the "
        + "name the entity is known by differs from the literal as written.",
        "ISO §8.3.2.2 rule 2 / DOC-A.1-68");

    /// <summary>COBOLNET2648 — a procedure division header's formal parameter or RETURNING item breaks §14.2.2 SR1 /
    /// SR5 / SR6 or the §14.2.1 using-phrase format (kb/Work PB1145). <c>ProcedureHeaderScreen</c> is its only reporter,
    /// called by the program / function arm and the method arm alike.</summary>
    public static readonly DiagnosticDescriptor ProcedureHeaderParameter = new(
        "COBOLNET2648", "procedure-header-parameter", EditionSeverity.Error,
        "A procedure division header's USING parameter or RETURNING item is not admissible. ISO §14.2.2 SR1: "
        + "\"A particular user-defined word shall not appear more than once as data-name-1.\" ISO §14.2.2 SR1: "
        + "\"The data description entry for data-name-1 shall not contain a BASED clause or a REDEFINES clause.\" "
        + "ISO §14.2.2 SR5: \"The data description entry for data-name-2 shall not contain a BASED clause or a "
        + "REDEFINES clause.\" ISO §14.2.2 SR6: \"Data-name-2 shall not be the same as data-name-1.\" ISO §14.2.1 "
        + "prints OPTIONAL only in the BY REFERENCE alternative of the using-phrase.",
        "ISO §14.2.2 SR1/SR5/SR6, §14.2.1");

    /// <summary>COBOLNET2647 — an OCCURS DYNAMIC FROM / TO value exceeds the implementor's maximum (kb/Work PB1264).
    /// <c>DataBinder.DynamicBoundWithinMaximum</c> is its only reporter.</summary>
    public static readonly DiagnosticDescriptor OccursDynamicBoundAboveMaximum = new(
        "COBOLNET2647", "occurs-dynamic-bound-above-maximum", EditionSeverity.Error,
        "The FROM or TO value of an OCCURS DYNAMIC clause is above this implementation's maximum. ISO §13.18.38.3 SR29: "
        + "\"The implementor shall specify a maximum permissible value for integer-4 and integer-5. Their values "
        + "shall not exceed this maximum value.\" The maximum is 1,073,741,823, the highest permissible occurrence "
        + "number of a dynamic-capacity table (docs/CONFORMANCE.md DOC-A.1-60).",
        "ISO §13.18.38.3 SR29");

    /// <summary>COBOLNET2646 — a table is nested under seven others, so a reference to it would need eight
    /// subscripts (kb/Work PB1260). <c>DataBinder.OdoResolve</c> is its only reporter.</summary>
    public static readonly DiagnosticDescriptor TableNestingTooDeep = new(
        "COBOLNET2646", "table-nesting-too-deep", EditionSeverity.Error,
        "An OCCURS entry is subordinate to seven other OCCURS entries, so a reference to it requires eight subscripts. "
        + "ISO §13.18.38.3 SR10: an OCCURS clause may be subordinate to an entry containing another OCCURS clause "
        + "\"as long as the number of subscripts required does not exceed seven\", and §8.4.2.3.3 SR3 states the same "
        + "limit: \"This allows a maximum of seven subscripts to be specified.\"",
        "ISO §13.18.38.3 SR10 / §8.4.2.3.3 SR3");

    /// <summary>COBOLNET2645 — the ASSIGN clause's literal-1 is not an alphanumeric literal, or is a figurative constant
    /// or a zero-length literal (kb/Work PB1078). <c>AssignTargetRule.Text</c> is its only reporter.</summary>
    public static readonly DiagnosticDescriptor AssignLiteralShape = new(
        "COBOLNET2645", "assign-literal-shape", EditionSeverity.Error,
        "The literal-1 of an ASSIGN clause is not admissible. ISO §12.4.5.2 SR4: \"Literal-1 shall be an alphanumeric "
        + "literal and shall be neither a figurative constant nor a zero-length literal.\" The hexadecimal-"
        + "alphanumeric literal and a concatenation expression of alphanumeric operands are alphanumeric literals "
        + "(§8.3.3.2.2; §8.8.3.3 GR3), so they are admitted; a national or boolean literal, a figurative constant and "
        + "the zero-length literal are not.",
        "ISO §12.4.5.2 SR4");

    /// <summary>COBOLNET2644 — an OO definition's environment division carries a part its kind may not (kb/Work
    /// PB1076 + PB813). ONE code for the one placement table, <c>OoEnvironmentRules.Screen</c>, its only reporter.</summary>
    public static readonly DiagnosticDescriptor OoEnvironmentPlacement = new(
        "COBOLNET2644", "oo-environment-placement", EditionSeverity.Error,
        "The environment division of a method, factory, instance, interface or class definition carries a part that "
        + "kind of definition may not: \"The configuration section shall not be specified in a method definition.\" "
        + "(ISO §12.3.3 SR2); \"The SOURCE-COMPUTER, OBJECT-COMPUTER, and REPOSITORY paragraphs shall not be specified "
        + "in a factory definition or an instance definition.\" (ISO §12.3.3 SR3); \"The input-output section "
        + "shall not be specified within an interface definition.\" (ISO §12.4.3 SR1); \"In a factory "
        + "definition or instance definition, the only clauses that may be specified are the CURSOR and CRT STATUS "
        + "clauses.\" (ISO §12.3.7.3 SR2); \"In an interface definition, the ALPHABET clause, the CURRENCY clause, the "
        + "DECIMAL-POINT clause, and the LOCALE clause are the only permitted clauses.\" (ISO §12.3.7.3 SR3). Within a "
        + "class definition the input-output section is admitted only in a factory definition or an instance "
        + "definition, so it is refused in a method definition and in the class definition's own environment "
        + "division too (ISO §12.4.3 SR1).",
        "ISO §12.3.3 SR2/SR3, §12.4.3 SR1, §12.3.7.3 SR2/SR3");

    /// <summary>COBOLNET1519 — a data-division section a class-body definition may not carry (kb/Work PB1251, the
    /// DATA-division twin of <see cref="OoEnvironmentPlacement"/>). ONE code for the one placement table,
    /// <c>OoDefinitionRules.Screen</c>, its only reporter: "Within a class definition, the file section may be
    /// specified only in a factory definition or an instance definition, but not in a method definition." (ISO
    /// §13.4.3 SR1; the same sentence for the working-storage, report and screen sections, §13.5.3 SR1, §13.8.3 SR1,
    /// §13.9.3 SR1); "The local-storage section may be specified in a program definition or function definition or
    /// in a method definition contained in a class definition." (§13.6.3 SR1); "The linkage section may be
    /// specified in a program definition, function definition, method definition, program prototype definition, or
    /// function prototype definition." (§13.7.3 SR1).</summary>
    public static readonly DiagnosticDescriptor OoDataDivisionPlacement = new(
        "COBOLNET1519", "oo-data-division-placement", EditionSeverity.Error,
        "The data division of a method, factory or instance definition carries a section that kind of definition may "
        + "not: within a class definition the file, working-storage, report and screen sections are admitted only in a "
        + "factory definition or an instance definition, never in a method definition (ISO §13.4.3 SR1, §13.5.3 SR1, "
        + "§13.8.3 SR1, §13.9.3 SR1); the local-storage section is admitted in a method definition but not in a "
        + "factory or instance definition (§13.6.3 SR1); the linkage section is admitted in a method definition but "
        + "not in a factory or instance definition (§13.7.3 SR1).",
        "ISO §13.4.3 SR1, §13.5.3 SR1, §13.6.3 SR1, §13.7.3 SR1, §13.8.3 SR1, §13.9.3 SR1");

    /// <summary>COBOLNET2643 — an AS literal of spaces only forms the zero-length externalized name (kb/Work
    /// PB1539). Refused wherever the clause itself refuses a zero-length literal — every AS clause but CLASS-ID.</summary>
    public static readonly DiagnosticDescriptor ExternalizedNameAllSpaces = new(
        "COBOLNET2643", "externalized-name-all-spaces", EditionSeverity.Error,
        "An AS phrase's literal consists of spaces only. Externalized names are formed without leading and trailing "
        + "spaces (ISO §8.3.2.2 rule 2 leaves formation to the implementor; docs/CONFORMANCE.md DOC-A.1-68), so the "
        + "literal forms the zero-length name, which no CALL, CANCEL, program-address or INVOKE target can name. The "
        + "clauses that refuse a zero-length literal (§11.10.3 SR1, §11.5.3 SR1, §11.6.3 SR1, §11.7.3 SR1, §12.3.8.3 "
        + "SR2, §13.18.22.3 SR3) therefore refuse this literal too; CLASS-ID, whose §11.3.3 SR1 admits a zero-length "
        + "literal, forms the zero-length name without a diagnostic.",
        "ISO §8.3.2.2 rule 2 / DOC-A.1-68");

    /// <summary>COBOLNET1795 — §11.10.3 syntax rule 2, the PROGRAM-ID Format-1 placement rule. A contained
    /// program is not externalized at all (§8.3.2.2 2) externalizes <q>program-names of outermost programs</q>), so
    /// the phrase that names its externalized form is illegal there.</summary>
    public static readonly DiagnosticDescriptor ExternalizedNameContained = new(
        "COBOLNET1795", "externalized-name-contained", EditionSeverity.Error,
        "ISO §11.10.3 syntax rule 2 (PROGRAM-ID, FORMAT 1): \"Literal-1 shall not be specified in a program "
        + "that is contained within another program.\" §8.3.2.2 externalizes \"program-names of outermost "
        + "programs\" only, and §8.4.6.3 scopes a contained program-name to its container, so a contained "
        + "program has no externalized name for the AS phrase to give.",
        "ISO §11.10.3 SR2");

    public static readonly DiagnosticDescriptor CallAsNestedScope = new(
        "COBOLNET1676", "call-as-nested-scope", EditionSeverity.Error,
        "CALL … AS NESTED names its program by literal-1. ISO §14.9.4.3 syntax rule 15: \"If the NESTED phrase "
        + "is specified, literal-1 shall be specified. Literal-1 shall be the same as the program-name specified "
        + "in a PROGRAM-ID paragraph of a common program as specified in 8.4.6.3, Scope of program-names, or of "
        + "a program that is directly contained in the calling program.\"",
        "ISO §14.9.4.3 SR15");
    // ── The PB132 CALL operand screens (kb/Work PB132): each predicate existed in the tree and was
    // unreached from CALL — these are the cited rejections for the shapes the binder silently accepted. ──
    public static readonly DiagnosticDescriptor CallOperandSection = new(
        "COBOLNET1677", "call-operand-section", EditionSeverity.Error,
        "A CALL operand shall reference a data item defined in the file, working-storage, local-storage, or "
        + "linkage section — ISO §14.9.4.3 syntax rule 3 for an argument, rule 7 for the RETURNING item. A "
        + "SCREEN SECTION entry is not such an item (and the SCREEN SECTION is an unsupported optional "
        + "facility — COBOLNET1560).",
        "ISO §14.9.4.3 SR3/SR7");
    public static readonly DiagnosticDescriptor CallByReferenceObjectData = new(
        "COBOLNET1678", "call-by-reference-object-data", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 3: \"If the BY REFERENCE phrase is specified or implied, identifier-2 "
        + "shall not be defined in the working-storage or file section of a factory or an instance object.\" "
        + "Object data is shared state; only BY CONTENT or BY VALUE may carry it out of a method's CALL.",
        "ISO §14.9.4.3 SR3");
    public static readonly DiagnosticDescriptor CallByReferenceOperandKind = new(
        "COBOLNET1679", "call-by-reference-operand-kind", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 10 (Format 1): \"If the BY REFERENCE phrase is specified or implied for "
        + "an identifier-2, that identifier shall be neither a strongly-typed group item nor a data item of "
        + "class object or pointer.\" A prototype-less callee cannot preserve the type discipline these items "
        + "carry; Format 2 admits them under §14.8.2's conformance rules.",
        "ISO §14.9.4.3 SR10");
    public static readonly DiagnosticDescriptor CallVariableLengthGroup = new(
        "COBOLNET1680", "call-variable-length-group", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 12 (Format 1): \"Identifier-2 shall not reference a variable-length "
        + "group.\" A group with a DYNAMIC LENGTH elementary item or a dynamic-capacity table subordinate to "
        + "it (§8.5.1.12.1) has no prototype-less byte image to pass.",
        "ISO §14.9.4.3 SR12 / §8.5.1.12.1");
    public static readonly DiagnosticDescriptor CallTargetCategory = new(
        "COBOLNET1681", "call-target-category", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 1: \"Identifier-1 shall be defined as an alphanumeric, national, or "
        + "program-pointer data item.\" Any other identifier target (numeric, boolean, alphabetic, index, "
        + "object) is not a program name carrier.",
        "ISO §14.9.4.3 SR1");
    public static readonly DiagnosticDescriptor CallAsNestedContext = new(
        "COBOLNET1682", "call-as-nested-context", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 13 (Format 2): \"The NESTED phrase may be specified only in a program "
        + "definition.\" A function, method, or interface definition contains no programs, so AS NESTED has "
        + "nothing to name there.",
        "ISO §14.9.4.3 SR13");
    public static readonly DiagnosticDescriptor CallBitAlignment = new(
        "COBOLNET1683", "call-bit-alignment", EditionSeverity.Error,
        "A bit data item passed BY REFERENCE (ISO §14.9.4.3 syntax rule 6) or used as the CALL RETURNING "
        + "item (rule 8) shall be aligned on a byte boundary, and its subscripts and reference-modification "
        + "leftmost position shall consist of only fixed-point numeric literals or all-literal arithmetic "
        + "expressions without exponentiation — the referenced address must be statically byte-aligned.",
        "ISO §14.9.4.3 SR6/SR8 / §8.5.1.6.3");
    public static readonly DiagnosticDescriptor CallArgumentCount = new(
        "COBOLNET1684", "call-argument-count", EditionSeverity.Error,
        "ISO §14.8.2.1: \"The number of arguments in the activating element shall be equal to the number of "
        + "formal parameters in the activated element, with the exception of trailing formal parameters that "
        + "are specified with an OPTIONAL phrase in the procedure division header of the activated element and "
        + "omitted from the list of arguments of the activating element.\" With AS NESTED the callee's header "
        + "is known at compile time, so the mismatch is a diagnostic here rather than a run-time "
        + "EC-PROGRAM-ARG-MISMATCH.",
        "ISO §14.8.2.1");
    public static readonly DiagnosticDescriptor CallOmittedNeedsOptional = new(
        "COBOLNET1685", "call-omitted-needs-optional", EditionSeverity.Error,
        "ISO §14.9.4.3 syntax rule 24: \"If the OMITTED phrase is specified, the OPTIONAL phrase shall be "
        + "specified for the corresponding formal parameter in the procedure division header.\"",
        "ISO §14.9.4.3 SR24");
    public static readonly DiagnosticDescriptor InvokeOmittedNeedsOptional = new(
        "COBOLNET2237", "invoke-omitted-needs-optional", EditionSeverity.Error,
        "ISO §14.9.23.3 syntax rule 18: \"If an OMITTED phrase is specified, an OPTIONAL phrase shall be "
        + "specified for the corresponding formal parameter in the procedure division header.\" The INVOKE twin "
        + "of COBOLNET1685 (CALL, §14.9.4.3 SR24); checked at compile time for a typed receiver, and at runtime "
        + "through a universal one (§14.9.23.4 GR7c, EC-OO-UNIVERSAL).",
        "ISO §14.9.23.3 SR18");
    public static readonly DiagnosticDescriptor FunctionOmittedNeedsOptional = new(
        "COBOLNET2238", "function-omitted-needs-optional", EditionSeverity.Error,
        "ISO §8.4.3.2.3 syntax rule 9: \"If the word OMITTED is specified, the OPTIONAL phrase shall be specified "
        + "for the corresponding formal parameter.\" The user-defined-function twin of COBOLNET1685 (CALL) and "
        + "COBOLNET2237 (INVOKE).",
        "ISO §8.4.3.2.3 SR9");
    /// <summary>The function-identifier twin of COBOLNET1688 (kb/Work PB1418 / PB1115): the SAME §14.8.2 argument
    /// conformance rules, imported by a different syntax rule, asked of the same shared routine
    /// (<c>ParameterConformance.CheckArgument</c>).</summary>
    public static readonly DiagnosticDescriptor FunctionArgumentConformance = new(
        "COBOLNET2470", "function-argument-conformance", EditionSeverity.Error,
        "An argument of a function-identifier that names a function prototype or a function pointer does not "
        + "conform to its corresponding formal parameter. ISO §8.4.3.2.3 syntax rule 13: \"If "
        + "function-prototype-name-1 or function-pointer-name-1 is specified, the rules for conformance specified "
        + "in 14.8.2, Parameters and 14.8.3, Returning items, apply.\" BY REFERENCE, §14.8.2.3.2 rule 2 (\"a "
        + "function\" is one of the activated elements it lists) requires the same ALIGN, BLANK WHEN ZERO, DYNAMIC "
        + "LENGTH, JUSTIFIED, PICTURE, SIGN and USAGE clauses and the object-reference rules; BY CONTENT or BY "
        + "VALUE, §14.8.2.3.3 applies the COMPUTE / SET / MOVE rules.",
        "ISO §8.4.3.2.3 SR13 · §14.8.2");
    /// <summary>The function-identifier twin of COBOLNET1683 (kb/Work PB1418).</summary>
    public static readonly DiagnosticDescriptor FunctionBitAlignment = new(
        "COBOLNET2471", "function-bit-alignment", EditionSeverity.Error,
        "A bit data item passed as the argument of a function-identifier whose corresponding formal parameter is "
        + "BY REFERENCE shall be aligned on a byte boundary, and its subscripts and reference-modification "
        + "leftmost position shall consist of only numeric literals or all-literal arithmetic expressions without "
        + "exponentiation (ISO §8.4.3.2.3 syntax rule 14).",
        "ISO §8.4.3.2.3 SR14");
    public static readonly DiagnosticDescriptor OmittedConditionOperand = new(
        "COBOLNET1686", "omitted-condition-operand", EditionSeverity.Error,
        "ISO §8.8.4.8 syntax rule 1: \"Data-name-1 shall be a formal parameter defined in the source element "
        + "in which this condition is specified.\" The omitted-argument condition asks whether an argument was "
        + "provided to THIS program, function, or method — an ordinary data item has no such property.",
        "ISO §8.8.4.8.3 SR1");
    public static readonly DiagnosticDescriptor CallArgumentMode = new(
        "COBOLNET1687", "call-argument-mode", EditionSeverity.Error,
        "The argument's passing mode does not match its corresponding formal parameter's. ISO §14.9.4.3 "
        + "syntax rule 19: with BY CONTENT or BY REFERENCE specified or implied for an argument, BY REFERENCE "
        + "shall be specified or implied for the corresponding formal parameter; rule 21: with BY VALUE "
        + "specified for an argument, BY VALUE shall be specified for the corresponding formal parameter. With "
        + "AS NESTED the callee's header is known at compile time, so the mismatch is a diagnostic here.",
        "ISO §14.9.4.3 SR19/SR21");
    public static readonly DiagnosticDescriptor CallArgumentConformance = new(
        "COBOLNET1688", "call-argument-conformance", EditionSeverity.Error,
        "A BY REFERENCE argument does not conform to its corresponding formal parameter. ISO §14.8.2.3.2 "
        + "(elementary, the NESTED-call regime): the definitions \"shall have the same ALIGN, BLANK WHEN ZERO, "
        + "DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses\"; §14.8.2.2 (groups): an alphanumeric "
        + "group or elementary alphanumeric formal of the same or smaller byte count, strongly-typed pairs of "
        + "the same type. The violation is EC-PROGRAM-ARG-MISMATCH at run time; with AS NESTED it is a "
        + "diagnostic at compile time.",
        "ISO §14.8.2.2 / §14.8.2.3.2");
    public static readonly DiagnosticDescriptor CallReturningConformance = new(
        "COBOLNET1736", "call-returning-conformance", EditionSeverity.Error,
        "A Format-2 CALL's RETURNING item does not conform to the activated program's. ISO §14.9.4.3 syntax "
        + "rule 25 makes \"the rules for conformance specified in 14.8.2, Parameters and 14.8.3, Returning "
        + "items\" apply, and §14.8.3.1 requires a returning item in the activating statement \"if and only "
        + "if a returning item is specified in the procedure division header of the activated element\". "
        + "§14.8.3.2 then asks the two group operands for the same length, the same type when strongly typed, "
        + "and — when either is a VARIABLE LENGTH GROUP — compatibility as described in §8.5.1.12; §14.8.3.3 "
        + "gives the elementary rules. With AS NESTED the callee's header is known at compile time, so the "
        + "mismatch is a diagnostic here instead of a run-time EC-PROGRAM-ARG-MISMATCH.",
        "ISO §14.9.4.3 SR25 / §14.8.3");
    public static readonly DiagnosticDescriptor ArithmeticFormatOperand = new(
        "COBOLNET1689", "arithmetic-format-operand", EditionSeverity.Error,
        "An arithmetic statement's operand does not fit the format its phrases select. The GIVING forms of "
        + "ADD / SUBTRACT / MULTIPLY / DIVIDE print ONE sending `{identifier | literal}` TO/FROM/BY/INTO "
        + "operand with no ROUNDED; the non-GIVING forms print receiving identifiers only, so a literal, a "
        + "function-identifier (§8.4.3.2.3 SR1) or an inline method invocation (§8.4.3.4.3 SR1) there is "
        + "illegal; and every BY form of DIVIDE prints GIVING. The old binders silently dropped the extra "
        + "operands, the ROUNDED or an inline-invocation receiver, or crashed.",
        "ISO §14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2 / §8.4.3.2.3 SR1 / §8.4.3.4.3 SR1");
    public static readonly DiagnosticDescriptor CommitRollbackContext = new(
        "COBOLNET1690", "commit-rollback-context", EditionSeverity.Error,
        "A COMMIT or ROLLBACK statement in a context its syntax rules ban: \"This statement shall not be "
        + "specified in a recursive source element\" (ISO §14.9.7.3 SR1 / §14.9.36.3 SR1 — and a function or a "
        + "method always is one, ISO §8.6.6), and ISO §14.9.7.3 SR2 — \"shall not be specified in the input or "
        + "output procedure of a MERGE or file SORT statement\"; ISO §14.9.36.3 SR2 states the same rule for "
        + "ROLLBACK, with SORT unqualified.",
        "ISO §14.9.7.3 / §14.9.36.3");
    public static readonly DiagnosticDescriptor AcceptVariableLengthGroup = new(
        "COBOLNET1691", "accept-variable-length-group", EditionSeverity.Error,
        "An ACCEPT receiver references a variable-length group: a group with a DYNAMIC LENGTH elementary item "
        + "or a dynamic-capacity table subordinate to it at any depth (ISO §8.5.1.12). \"Neither identifier-1 "
        + "nor identifier-2 shall reference a variable-length group\" — both the device and the temporal "
        + "format exclude it.",
        "ISO §14.9.1.3");
    public static readonly DiagnosticDescriptor SortMergeFileInIoStatement = new(
        "COBOLNET1692", "sd-file-io-statement", EditionSeverity.Error,
        "An input-output statement names a sort-merge (SD) file. \"A sort-merge file is referenced only by a "
        + "SORT, MERGE, RELEASE, or RETURN statement\" (and a SORT/MERGE USING/GIVING) — CLOSE, DELETE, DELETE "
        + "FILE, OPEN, READ, REWRITE, START, UNLOCK and WRITE all reject it at compile time. The old posture "
        + "let CLOSE and the DELETE forms compile and run against an unregistered connector, whose fail-open "
        + "status read '00'.",
        "ISO §13.4.6.3");
    public static readonly DiagnosticDescriptor ClosePhraseOrganization = new(
        "COBOLNET1693", "close-phrase-organization", EditionSeverity.Error,
        "A CLOSE statement's NO REWIND, REEL, or UNIT phrase on a file whose organization is not sequential: "
        + "\"The NO REWIND, REEL, and UNIT phrases may be used only with files that are of sequential "
        + "organization.\" The WITH LOCK phrase is not organization-restricted. The old acceptance degraded at "
        + "run time to a stale FILE STATUS value with no defined meaning. Its OPEN twin — the identically "
        + "worded ISO §14.9.27.3 SR5 — is open-norewind-organization.",
        "ISO §14.9.6.3");
    public static readonly DiagnosticDescriptor OpenNoRewindOrganization = new(
        "COBOLNET1802", "open-norewind-organization", EditionSeverity.Error,
        "An OPEN statement's WITH NO REWIND phrase on a file whose organization is not sequential: \"The NO "
        + "REWIND phrase may be specified only for sequential files.\" The EXACT twin of the CLOSE rule "
        + "close-phrase-organization reports (ISO §14.9.6.3 SR1) — one rule the standard writes once per "
        + "statement, of which only the CLOSE spelling had a screen. It is also what keeps §14.9.27.4 GR11 "
        + "answerable: GR11 keys on the storage medium, and a relative or indexed file is §14.9.6.4 GR2 "
        + "category (d), for which neither GR11 nor GR12 defines a NO REWIND effect.",
        "ISO §14.9.27.3");
    public static readonly DiagnosticDescriptor OpenNoRewindOpenMode = new(
        "COBOLNET1803", "open-norewind-open-mode", EditionSeverity.Error,
        "An OPEN statement's WITH NO REWIND phrase in an I-O or EXTEND group: \"The NO REWIND phrase may be "
        + "specified only when the INPUT or OUTPUT phrase is specified.\" §14.9.27.4 GR12 a) corroborates the "
        + "rule by naming only EXTEND as the mode that suppresses the beginning-of-file positioning the phrase "
        + "talks about. The phrase used to be parsed and dropped, so every mode accepted it silently.",
        "ISO §14.9.27.3");
    public static readonly DiagnosticDescriptor OpenReportFileMode = new(
        "COBOLNET2371", "open-report-file-mode", EditionSeverity.Error,
        "An OPEN statement opens a REPORT FILE (an FD carrying a REPORT clause) in the INPUT or I-O mode: \"The "
        + "OPEN statement for a report file shall not contain the INPUT phrase or the I-O phrase.\" (ISO "
        + "§14.9.27.3 SR1). §13.18.46.3 SR3 states the same boundary from the file's side — the subject of such "
        + "an FD may be referenced in the procedure division only by USE, the WHEN phrase of a PERFORM, CLOSE, "
        + "or \"the OPEN statement with the OUTPUT or EXTEND phrase\". A report file is written only by the "
        + "report writer's own implicit output, so neither mode has anything to read.",
        "ISO §14.9.27.3");
    public static readonly DiagnosticDescriptor OpenExtendAccessLinage = new(
        "COBOLNET2372", "open-extend-access-linage", EditionSeverity.Error,
        "An OPEN statement's EXTEND phrase names a file whose access mode is not sequential, or whose file "
        + "description entry carries a LINAGE clause: \"The EXTEND phrase shall be specified only if the access "
        + "mode of the file connector referenced by file-name-1 is sequential and the LINAGE clause is not "
        + "specified in the file description entry for file-name-1.\" (ISO §14.9.27.3 SR2). The predicate is the "
        + "ACCESS MODE, not the organization: EXTEND stays legal on a relative or indexed file whose ACCESS MODE "
        + "IS SEQUENTIAL. The site names which of the two conjuncts the statement violated.",
        "ISO §14.9.27.3");
    public static readonly DiagnosticDescriptor OpenReversedOrganization = new(
        "COBOLNET2210", "open-reversed-organization", EditionSeverity.Error,
        "An OPEN statement's REVERSED phrase on a file whose organization is not RECORD sequential. REVERSED "
        + "is the COBOL-85 tape phrase ISO 2002 deleted (VERSION_CHANGE_REFERENCE row 7.12), so it is legal "
        + "source only at --std 85, where its general format writes it beside WITH NO REWIND in the repeated "
        + "file-name group of the OPEN statement. The phrase positions the file at its end and makes every "
        + "subsequent READ retrieve the PREVIOUS record, which is the backward walk ISO §14.9.30.4 GR21 c) "
        + "defines and §14.9.30.3 SR7 denies to LINE SEQUENTIAL organization — so the phrase is admitted for "
        + "record sequential organization alone, one narrower step than its sibling WITH NO REWIND "
        + "(open-norewind-organization, §14.9.27.3 SR5, which §9.1.7.2 lets reach both sequential kinds).",
        "ISO §14.9.30.3");
    public static readonly DiagnosticDescriptor OpenReversedOpenMode = new(
        "COBOLNET2211", "open-reversed-open-mode", EditionSeverity.Error,
        "An OPEN statement's REVERSED phrase in an OUTPUT, I-O or EXTEND group. REVERSED is the COBOL-85 tape "
        + "phrase ISO 2002 deleted (VERSION_CHANGE_REFERENCE row 7.12); COBOL-85's OPEN general format writes "
        + "it in the INPUT group only, and the phrase's whole effect is a retrieval direction, which the three "
        + "other modes have no READ to apply it to. The sibling rule for WITH NO REWIND is "
        + "open-norewind-open-mode (ISO §14.9.27.3 SR6, INPUT or OUTPUT).",
        "VCR Table 7 row 7.12 (X3.23-1985 OPEN … REVERSED)");
    public static readonly DiagnosticDescriptor ReadIgnoringWithLock = new(
        "COBOLNET1818", "read-ignoring-with-lock", EditionSeverity.Error,
        "A READ statement specifies both the IGNORING LOCK phrase and the LOCK phrase: \"The LOCK phrase shall "
        + "not be specified in the same READ statement as the IGNORING LOCK phrase.\" The two are alternatives "
        + "of DIFFERENT printed brackets (§14.9.30.2 prints [ADVANCING ON LOCK | IGNORING LOCK | retry-phrase] "
        + "and [WITH LOCK | WITH NO LOCK] as two independent brackets), so nothing but this rule stops them "
        + "combining. ⚠ WITH NO LOCK is NOT \"the LOCK phrase\" — §14.9.30.4 GR11 b) names \"the NO LOCK "
        + "phrase\" and GR11 d) \"the LOCK phrase\" as different things in this same statement — so IGNORING "
        + "LOCK WITH NO LOCK is legal and is accepted. Until kb/Work PB331 the forbidden pair was a raw parse "
        + "error only because the grammar had merged the two brackets into one slot, which ALSO rejected the "
        + "legal pair; splitting the brackets is what made this check load-bearing.",
        "ISO §14.9.30.3");
    public static readonly DiagnosticDescriptor BasedRecordSubstrate = new(
        "COBOLNET1695", "based-record-substrate", EditionSeverity.Error,
        "A BASED record has a subordinate item the shared storage area cannot carry. ⛔ NO SUCH SHAPE EXISTS "
        + "TODAY: this is the loud DEFAULT of the ALLOW-list gate DataBinder.ByteWindowResidueOf, kept so that "
        + "a data category added to the model with no pinned representation is REFUSED by name instead of "
        + "silently laid out as a zero-width alias. Every leaf kind it once refused now rides the area — every "
        + "NUMERIC usage on its pinned byte form (DISPLAY, BINARY, PACKED-DECIMAL, COMP-5, the IEEE float "
        + "family, USAGE INDEX; kb/Work PB164); a USAGE BIT run on its §8.5.1.6.3 sub-byte packing; a NATIONAL "
        + "leaf on its §13.18.60.4 GR8 two-bytes-per-position extent, transcoded UTF-16BE (RESIDUE-11); and a "
        + "POINTER-CLASS leaf on the area's MANAGED SLOT at the byte offset its reserved bytes occupy, which "
        + "is where §14.9.3.4 GR9's null-seeding writes — the three thirds of kb/Work PB231. Each of those "
        + "drew this diagnostic while its byte-identical REDEFINES spelling compiled and ran, because the cell "
        + "gate and the REDEFINES gate were two hand-written lists that had drifted; they are ONE predicate "
        + "now. The message interpolates the residue clause that gate produced, so a future refusal names "
        + "itself. ⚠ The two NONCONFORMING shapes this diagnostic used to reject for the wrong reason now draw "
        + "the rules that actually bar them: COBOLNET1797 (§13.18.5.3 SR1, a BASED subject of class object) "
        + "and COBOLNET1796 (§13.18.22.3 SR4, an EXTERNAL item of class object or pointer).",
        "ISO §13.18.5 / §14.9.3");
    public static readonly DiagnosticDescriptor CancelTargetCategory = new(
        "COBOLNET1696", "cancel-target-category", EditionSeverity.Error,
        "ISO §14.9.5.3 syntax rule 1: \"Identifier-1 shall be defined as an alphanumeric or national data "
        + "item.\" CANCEL's admitted list is NARROWER than CALL's (§14.9.4.3 SR1 also admits a "
        + "program-pointer item — COBOLNET1681's rule); any other identifier target is not a program-name "
        + "carrier (kb/Work PB154: CANCEL of a numeric item compiled clean and no-opped silently).",
        "ISO §14.9.5.3 SR1");
    public static readonly DiagnosticDescriptor OperandClassExcluded = new(
        "COBOLNET1694", "operand-class-excluded", EditionSeverity.Error,
        "A statement operand is of a class its syntax rules exclude. The recurring shape (kb/Work PB148): "
        + "\"Identifier-1 shall not reference a data item of class …\" closes a class list per statement "
        + "(DISPLAY's §14.9.11.3 SR1 excludes message-tag, object and pointer — where 'class pointer' spans "
        + "the data-, function- and program-pointer categories), and §13.18.60.3 SR10 closes the reference "
        + "contexts for an index DATA item. The reusable gate also rejects the word NULL in such slots — NULL "
        + "is the predefined object reference/address (§8.4.3.7/§8.4.3.10), not a §8.3.3.6.2 figurative "
        + "constant. The old fall-throughs printed a pointer's CLR carrier text, an object's ToString, an "
        + "index item's EMPTY zero-digit image, and U+0000 for NULL.",
        "ISO §14.9.11.3 / §13.18.60.3");
    public static readonly DiagnosticDescriptor CallContentOperandFormat = new(
        NotImplemented, "call-content-operand-format", EditionSeverity.Error,
        "This BY CONTENT operand belongs to a different CALL format. ISO §14.9.4.2 Format 1's BY CONTENT admits "
        + "\"{ identifier-2 } …\" and nothing else; the expression operands (arithmetic-expression-1, "
        + "boolean-expression-1) are Format 2's, which the AS phrase selects. A boolean expression is additionally "
        + "not yet carried across a CALL boundary — the INVOKE side has BoundInvokeArg.ContentBool and the CALL "
        + "argument model has no counterpart.",
        "ISO §14.9.4.2 Formats 1 and 2", RecognizedNotImplemented);

    // ── COBOLNET0899 — the staged-loud standard-arithmetic leg (P10 Step 12) ─────────────────────────────
    public static readonly DiagnosticDescriptor ArithmeticStandardIntrinsic = new(
        NotImplemented, "arithmetic-standard-intrinsic",  EditionSeverity.Error,
        "Under ARITHMETIC IS STANDARD / STANDARD-DECIMAL, ISO §15.4.1 r1 requires this function's returned "
        + "value to EQUAL its equivalent arithmetic expression evaluated in the standard-decimal intermediate "
        + "(SDIDI, §8.8.1.5) — the ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION equivalent "
        + "expressions carry inexact divisions, and the native IEEE-double engine (§15.4.1's native-arithmetic "
        + "approximation license) cannot honor that equality; staged loud until the CobolDec evaluations land "
        + "so a program depending on standard-decimal function results never silently gets native ones.",
        "ISO §15.4.1 / §8.8.1.5.1", RecognizedNotImplemented);

    // ── COBOLNET1958 — the prototype-pointer RESTRICTION OPERAND, one rule over two carriers ─────────────
    // ⛔ THIS REPLACED TWO 0899 STAGED-LOUD DESCRIPTORS (`usage-function-pointer`, `program-pointer-restricted`,
    // P10 Step 7), which are DELETED rather than kept inert: both asserted that the construct was "not yet
    // implemented", and both constructs are implemented as of kb/Work PB452 + PB817. A staged-loud descriptor
    // that outlives its stage is a green test's way of holding a gap open
    // (feedback_green_test_can_hold_a_gap_open).
    /// <summary>COBOLNET1958 — the <c>TO {function|program}-prototype-name-1</c> phrase of ISO §13.18.60.2's
    /// USAGE general format: PRESENCE (FUNCTION-POINTER's operand is unbracketed, so the phrase is required) and
    /// SCOPE (§8.4.6.6 for a function-prototype-name, §8.4.6.8 for a program-prototype-name — the same sentence
    /// over two namespaces). ONE code because it is one rule over two carriers: §13.18.60.4 GR25 and GR26 differ
    /// only in the namespace, and so do their §14.9.39.3 SR20/SR22 consumers (kb/Work PB817).
    /// <para>The SHAPE rule that is NOT here is §13.18.60.3 SR18/SR19's TYPEDEF requirement — that one is the
    /// 0881 declaration band beside the other USAGE-clause compatibility screens, and it has no function-pointer
    /// arm at all (§13.18.60.3 carries no function-prototype-name sentence).</para></summary>
    public static readonly DiagnosticDescriptor PrototypePointerRestriction = new(
        "COBOLNET1958", "prototype-pointer-restriction", EditionSeverity.Error,
        "The TO phrase of a USAGE FUNCTION-POINTER / PROGRAM-POINTER clause shall name a function-prototype or "
        + "program-prototype in the source element's scope, and FUNCTION-POINTER's phrase is not optional.",
        "ISO §13.18.60.2 / §8.4.6.6 / §8.4.6.8");

    /// <summary>COBOLNET1959 — ISO §14.9.39.3 SR20 and SR22, which are ONE rule over two carriers: "The
    /// function-prototypes associated with identifier-12 and identifier-13 shall have the same signature" and
    /// "the program-prototypes associated with identifier-7 and identifier-8 shall have the same signature".
    /// ONE code, because <c>PrototypeSignatures.Same</c> is one test (kb/Work PB817).</summary>
    public static readonly DiagnosticDescriptor PrototypePointerSignature = new(
        "COBOLNET1959", "prototype-pointer-signature", EditionSeverity.Error,
        "A SET between restricted pointers requires the associated function-prototypes or program-prototypes to "
        + "have the same signature.", "ISO §14.9.39.3 SR20 / SR22");

    /// <summary>COBOLNET1960 — ISO §8.4.3.12.3 SR1/SR2, the function-address-identifier's operand: the braced
    /// choice is <c>function-prototype-name-1 | identifier-1</c>, the first "a function prototype specified in
    /// the REPOSITORY paragraph" and the second "of category alphanumeric or national". A word that is neither
    /// names no function. (There is no literal-1 arm — §8.4.3.13's PROGRAM twin has one and §8.4.3.12 does
    /// not, measured on the printed folio 141.)</summary>
    public static readonly DiagnosticDescriptor FunctionAddressOperand = new(
        "COBOLNET1960", "function-address-operand", EditionSeverity.Error,
        "The operand of ADDRESS OF FUNCTION shall be a function-prototype-name declared in the REPOSITORY "
        + "paragraph or an identifier of category alphanumeric or national.",
        "ISO §8.4.3.12.3 SR1 / SR2");

    // ── COBOLNET1533 — strong typing, split by rule (§8.5) ───────────────────────────────────────────
    public static readonly DiagnosticDescriptor StrongMoveMismatch = new(
        StrongType, "strong-move-mismatch", EditionSeverity.Error,
        "MOVE to/from a strongly-typed group requires a group of the same type.", "ISO §14.9.25.3 SR2");
    public static readonly DiagnosticDescriptor StrongClassCondition = new(
        StrongType, "strong-class-condition", EditionSeverity.Error,
        "A strongly-typed group item may not appear in a class condition.", "ISO §8.8.4.4.3 SR1");
    public static readonly DiagnosticDescriptor StrongCompareMismatch = new(
        StrongType, "strong-compare-mismatch", EditionSeverity.Error,
        "A strongly-typed group may be compared only with a group of the same type.", "ISO §8.8.4.2.3 SR1");

    // ── COBOLNET1535 — the strong-group ordering rule (§8.8.4.2.3 SR4). It once also named the staged
    //    RENAMES-in-TYPEDEF refusal; that stage is implemented and its descriptor deleted (kb/Work PB1304, PB608). ───
    public static readonly DiagnosticDescriptor StrongCompareOrdering = new(
        "COBOLNET1535", "strong-compare-ordering", EditionSeverity.Error,
        "A strongly-typed group whose elementary items include class boolean, message-tag, object, or pointer "
        + "may be compared only for equality or inequality — an ordering relation on such a group is a syntax "
        + "error.", "ISO §8.8.4.2.3 SR4");
    // ── COBOLNET1555/1556/1557 — the SAME AS clause, one code per rule family (§13.18.49 / §13.16.3;
    //    P10 Step 16). 1555 = the SUBJECT-entry rules (what the SAME AS entry itself may look like);
    //    1556 = the REFERENCED-entry rules (what data-name-1 may be); 1557 = the cycle rules.
    //    1550/1551/1552 are unallocated holes (the PHASE-12 earmark expired unused); 1553/1554 taken; 1558 = EXTERNAL type declarations. ──
    public static readonly DiagnosticDescriptor SameAsEntryRule = new(
        "COBOLNET1555", "same-as-entry-rule", EditionSeverity.Error,
        "A SAME AS entry violates a subject-entry rule: no clause other than CONSTANT RECORD, entry-name, "
        + "EXTERNAL, GLOBAL, level-number, and OCCURS may share the entry (§13.16.3 SR12); the entry shall not "
        + "be immediately followed by a subordinate or level-88 entry (§13.18.49.3 SR2); a level-77 subject "
        + "requires an elementary data-name-1 (SR8); no group containing the subject may carry a GROUP-USAGE, "
        + "SIGN, or USAGE clause (SR9).", "ISO §13.18.49.3 / §13.16.3 SR12");
    public static readonly DiagnosticDescriptor SameAsReferencedEntry = new(
        "COBOLNET1556", "same-as-referenced-entry", EditionSeverity.Error,
        "A SAME AS reference violates a data-name-1 rule: the target shall resolve to exactly one elementary "
        + "item or level-1 group item of the file/working-storage/local-storage/linkage section (§13.18.49.3 SR7); "
        + "it shall not be subject to any OCCURS clause (SR1) nor itself carry one (SR5); it shall not carry a "
        + "CONSTANT RECORD clause (SR10); in the file section its description shall not contain a USAGE OBJECT "
        + "REFERENCE item (SR6).", "ISO §13.18.49.3");
    public static readonly DiagnosticDescriptor SameAsCycle = new(
        "COBOLNET1557", "same-as-cycle", EditionSeverity.Error,
        "A SAME AS reference is cyclic: neither data-name-1's description nor any subordinate of the subject "
        + "may directly or indirectly reference the subject or a group it is subordinate to, via SAME AS (SR3) "
        + "or a TYPE clause (SR4).", "ISO §13.18.49.3 SR3/SR4");

    // ── COBOLNET1558 — EXTERNAL type declarations (§13.18.22 / §13.18.58; P10 Step 16) ───────────────
    // ── COBOLNET1559 — the report-group PRESENT WHEN / VARYING / multiple-COLUMN syntax rules, one code for
    //    the rule family (§13.15.3 / §13.18.64.3; P10 Step 13 — the SameAsEntryRule bundling precedent).
    //    COBOLNET1560 is NO LONGER an earmark: it is the Annex A.4.2 screen-handling refusal, declared with its
    //    procedure-division twin COBOLNET1707 at the end of this file (kb/Work PB260). ──
    public static readonly DiagnosticDescriptor ReportGroupClauseRule = new(
        "COBOLNET1559", "report-group-clause-rule", EditionSeverity.Error,
        "A report group description entry violates a PRESENT WHEN / VARYING syntax rule: a PRESENT WHEN "
        + "condition shall not reference a sum counter, LINE-COUNTER, PAGE-COUNTER, or another report section "
        + "data item (§13.15.3 SR16); GROUP INDICATE shall not share an entry with PRESENT WHEN (§13.15.3 "
        + "SR17); a VARYING entry shall also contain an OCCURS clause or a multiple LINE or multiple COLUMN "
        + "clause (§13.18.64.3 SR1); its data-name shall not be defined elsewhere in the source element (SR2) "
        + "nor referenced in arithmetic-expression-1 of the same clause (SR3).",
        "ISO §13.15.3 SR16/SR17 / §13.18.64.3 SR1–SR3");

    // ── COBOLNET2021 — the report-writer OCCURS (§13.18.38 format 3) syntax-rule family, one code for the
    //    family (the COBOLNET1559 bundling precedent); kb/Work PB565. ──
    public static readonly DiagnosticDescriptor ReportOccursFormat3Rule = new(
        "COBOLNET2021", "report-occurs-format3-rule", EditionSeverity.Error,
        "A repeating entry in a report group description violates a syntax rule of the report-writer OCCURS "
        + "clause (§13.18.38.2 format 3 — OCCURS [integer-1 TO] integer-2 TIMES [DEPENDING ON data-name-1] "
        + "[STEP integer-3]): the DYNAMIC, KEY and INDEXED BY phrases belong to formats 1, 2 and 4; the clause "
        + "shall not appear on an 01-level entry (SR1a); integer-1 shall be ≥ 0 and integer-2 > integer-1 "
        + "(SR16); data-name-1 shall describe an integer (SR17); TO and DEPENDING are both absent or both "
        + "present (SR24); the STEP phrase shall be specified when the entry contains or has subordinate to it "
        + "an absolute COLUMN or LINE clause (SR25); integer-3 shall be sufficient to prevent two consecutive "
        + "repetitions overlapping (SR26); an entry with DEPENDING may be followed within its report group only "
        + "by entries subordinate to it (SR27); and an OCCURS may nest inside another only without DEPENDING "
        + "(SR10).", "ISO §13.18.38.3 SR1/SR10/SR16/SR17/SR24–SR27");

    // ── COBOLNET2199 — the LINE clause (§13.18.35 Format 1) syntax-rule family, one code for the family (the
    //    COBOLNET2021 bundling precedent); kb/Work PB565. ──
    public static readonly DiagnosticDescriptor ReportLineClauseRule = new(
        "COBOLNET2199", "report-line-clause-rule", EditionSeverity.Error,
        "A LINE clause in a report group description violates one of its syntax rules. ISO §13.18.35.3 SR10 "
        + "governs the MULTIPLE LINE clause — more than one integer-1 or integer-2 operand: a) \"The NEXT PAGE "
        + "phrase, if specified, shall appear only with the first operand\"; b) \"All absolute operands, if "
        + "present, shall precede all relative operands, if present\"; c) \"The occurrences of integer-1, if "
        + "present, shall be in ascending numerical order\"; d) \"An OCCURS clause shall not also be present in "
        + "the same entry\". SR4 forbids a LINE clause in an entry subordinate to one that also contains a LINE "
        + "clause. SR3: \"Neither integer-1 nor integer-2 shall exceed the page limit, or 9999 if the report is not "
        + "divided into pages.\" SR5: \"If the report is not divided into pages, all its LINE clauses shall be "
        + "relative.\" SR7: \"Within a given report group description, a NEXT PAGE phrase, if present, shall be "
        + "specified only in the first LINE clause.\" SR8: \"The NEXT PAGE phrase may appear only in the description "
        + "of a body group or a report footing.\" SR6 governs the set of lines of one report group: a) absolute "
        + "lines not in increasing numerical order, b) lines that overlap, and e) an absolute line described after a "
        + "relative one each need a different PRESENT WHEN clause; c) \"no line appears above the upper limit or below "
        + "the lower limit allowed for the report group\", the limits being those of the TYPE clause's general rules; "
        + "d) relative lines after unconditional absolute ones shall not carry the group past its lower limit.",
        "ISO §13.18.35.3 SR3/SR4/SR5/SR6/SR7/SR8/SR10");

    // ── COBOLNET2247 — the §13.15.3 CLAUSE-PRESENCE family of a report group description entry, one code for
    //    the family (the COBOLNET2021 / COBOLNET2199 bundling precedent); kb/Work PB853. ──
    /// <summary>COBOLNET2247 — a report group description entry carries, or lacks, a clause in violation of
    /// §13.15.3's clause-presence rules. SR10 had NO site: an elementary entry with a COLUMN clause and no
    /// SOURCE, VALUE or SUM clause compiled clean and the binder FABRICATED a figurative SPACE operand for it,
    /// which printed a made-up image (<c>000</c> under <c>PIC 999</c>) or aborted the run unit; SR11, SR13 and
    /// SR15 had no site either, so a group entry's PICTURE/VALUE and a column-less VALUE, JUSTIFIED or BLANK WHEN
    /// ZERO entry were dropped in silence. SR5 (the TYPE clause only in, and in every, level 1 entry), SR9 (a
    /// COLUMN entry subordinate to a LINE entry), SR12 (a PICTURE in every elementary SOURCE or SUM entry) and SR14
    /// (a PICTURE only omitted for an alphanumeric, boolean or national VALUE literal) joined it when
    /// `report-column-without-line` and `report-item-missing-picture` — two genuine errors that had been staged
    /// on the shared COBOLNET0899 "not implemented" code — were folded in (kb/Work PB1224, PB1288). Screened ONCE
    /// per written entry, over the flat entry array, so a §13.18.38 Format 3 subtree replay cannot multiply
    /// it.</summary>
    public static readonly DiagnosticDescriptor ReportEntryClausePresence = new(
        "COBOLNET2247", "report-entry-clause-presence", EditionSeverity.Error,
        "A report group description entry violates a clause-presence syntax rule: the TYPE clause may be specified "
        + "only in a level 1 entry and shall be specified in every level 1 entry (§13.15.3 SR5); every elementary "
        + "entry with a COLUMN clause but no LINE clause shall be subordinate to an entry with a LINE clause (SR9; "
        + "§13.18.14.3 SR3); every elementary entry with a COLUMN clause shall also contain either a SOURCE, VALUE "
        + "or SUM clause (SR10); the PICTURE, COLUMN, SOURCE, VALUE, SUM, and GROUP INDICATE clauses may be written "
        + "only in an elementary entry (SR11); a PICTURE clause shall be specified in every elementary entry that "
        + "has a SOURCE or SUM clause (SR12), and may be omitted only for an alphanumeric, boolean or national "
        + "VALUE literal (SR14); a COLUMN clause shall be specified in each elementary entry that has a VALUE "
        + "clause (SR13); and if BLANK WHEN ZERO or JUSTIFIED is specified, a COLUMN clause shall also be "
        + "specified (SR15).",
        "ISO §13.15.3 SR5/SR9/SR10/SR11/SR12/SR13/SR14/SR15");

    /// <summary>COBOLNET2519 — a GROUP INDICATE clause outside the placement §13.18.28.3 SR1 admits: a report
    /// group that is not a DETAIL, or an elementary entry lacking the COLUMN clause or the SOURCE-or-VALUE clause
    /// that makes it a printed item. Only the rule's elementary half (§13.15.3 SR11, COBOLNET2247) had a site, so
    /// the clause in a control heading, or on a column-less entry, compiled in silence (kb/Work PB1245).</summary>
    public static readonly DiagnosticDescriptor ReportGroupIndicatePlacement = new(
        "COBOLNET2519", "report-group-indicate-placement", EditionSeverity.Error,
        "A GROUP INDICATE clause is specified outside a detail report group description, or in an elementary "
        + "entry without a COLUMN clause or without a SOURCE or VALUE clause. ISO §13.18.28.3 SR1: \"The GROUP "
        + "INDICATE clause may be specified only within a detail report group description, in an elementary entry "
        + "that also contains a COLUMN clause and a SOURCE or VALUE clause.\"", "ISO §13.18.28.3 SR1");

    /// <summary>A NEXT GROUP clause (ISO §13.18.37) that a syntax rule forbids where it is written (kb/Work PB957):
    /// outside a level 1 entry (§13.15.3 SR6); an integer beyond the page limit, or 9999 when the report is not
    /// divided into pages (§13.18.37.3 SR1); an absolute or NEXT PAGE form in a report that is not divided into
    /// pages (SR3); in a page heading or a report footing (SR4); NEXT PAGE in a page footing (SR5); or an
    /// integer outside the region SR6 (absolute) or SR7 (relative) fixes for the group's type.</summary>
    public static readonly DiagnosticDescriptor ReportNextGroupClauseRule = new(
        "COBOLNET2284", "report-next-group-clause-rule", EditionSeverity.Error,
        "A NEXT GROUP clause violates one of its syntax rules: it may be specified only in a level 1 entry "
        + "(§13.15.3 SR6); integer-1 and integer-2 shall not exceed the page limit, or 9999 if the report is not "
        + "divided into pages (§13.18.37.3 SR1); if the report is not divided into pages, only the relative form "
        + "may be specified (SR3); the clause shall not be specified in a page heading or report footing (SR4); "
        + "the NEXT PAGE phrase shall not be specified in a page footing (SR5); and the absolute and relative "
        + "integers shall lie within the bounds SR6 and SR7 set for a report heading, a body group and a page "
        + "footing.", "ISO §13.18.37.3 SR1/SR3–SR7; §13.15.3 SR6");

    /// <summary>COBOLNET2708 — a report description entry followed by no report group description entry. ISO
    /// §13.8.4: "An RD entry shall be followed by one or more report group description entries." The general
    /// format (§13.8.2) prints the entries after an RD as the brace group <c>{ constant-entry |
    /// report-group-description-entry } …</c>, and a constant entry describes no report, so a constant entry alone
    /// does not satisfy the rule. The sort-merge file description's "one or more record description entries"
    /// (§13.4.6.3 SR2) is the same shape and reports COBOLNET1837.</summary>
    public static readonly DiagnosticDescriptor ReportDescriptionWithoutGroup = new(
        "COBOLNET2708", "report-description-without-group", EditionSeverity.Error,
        "A report description (RD) entry is followed by no report group description entry. ISO §13.8.4: \"An RD "
        + "entry shall be followed by one or more report group description entries. The RD entry and the report "
        + "group description entries that follow fully describe one report.\" A constant entry written after the RD "
        + "(§13.8.2) describes no report group, so it does not satisfy the rule. Describe at least one report "
        + "group (a level-01 entry with a TYPE clause) under the RD.",
        "ISO §13.8.4");

    /// <summary>COBOLNET2709 — a PAGE clause that writes one of its five subdividing phrases (HEADING, FIRST
    /// DETAIL, LAST CONTROL HEADING, LAST DETAIL, FOOTING) without integer-1, the page limit. ISO §13.18.39.3 SR3;
    /// the phrases divide the page (§13.18.39.4 GR2c–GR2g) and a report with no page limit has no page.</summary>
    public static readonly DiagnosticDescriptor ReportPagePhraseWithoutLimit = new(
        "COBOLNET2709", "report-page-phrase-without-limit", EditionSeverity.Error,
        "A PAGE clause writes a HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL or FOOTING phrase but not "
        + "integer-1. ISO §13.18.39.3 SR3: \"The HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, or "
        + "FOOTING phrase may be specified only if integer-1 is specified.\" Integer-1 is the page limit "
        + "(§13.18.39.4 GR2a) and the phrases subdivide the page it bounds, so a PAGE clause that gives only the "
        + "page width (integer-2 COLUMNS) cannot carry them. Write the page limit, or drop the phrases.",
        "ISO §13.18.39.3 SR3");

    /// <summary>COBOLNET2744 — a PAGE clause whose integers break §13.18.39.3 SR5 or SR6: integer-1 (the page limit)
    /// over 9999, or the HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL and FOOTING integers and the page
    /// limit not in ascending order (equality allowed) where they are specified. The "greater than zero" half of SR6
    /// is COBOLNET2386; the phrases-need-a-limit rule is COBOLNET2709 (kb/Work PB1270).</summary>
    public static readonly DiagnosticDescriptor ReportPageClauseRule = new(
        "COBOLNET2744", "report-page-clause-rule", EditionSeverity.Error,
        "A PAGE clause violates a syntax rule on its integers. ISO §13.18.39.3 SR5: \"Integer-1 shall not exceed "
        + "9999.\" SR6: \"Integer-3, integer-4, integer-5, integer-6, integer-7, and integer-1 shall be greater than "
        + "zero. Wherever specified, they shall be in ascending order, with equality allowed.\" The order is HEADING, "
        + "FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, FOOTING, then the page limit.",
        "ISO §13.18.39.3 SR5/SR6");

    /// <summary>COBOLNET2745 — a report line's COLUMN clauses break an ARRANGEMENT rule of ISO §13.18.14.3: SR7
    /// (absolute items not in increasing numerical order), SR8 a) (overlapping items), SR8 b) (an absolute item's
    /// rightmost column past the page width), SR8 c) (a relative tail past the page width), SR10 a) (a multiple
    /// COLUMN clause with an OCCURS clause in the same entry) or SR10 b) (a multiple COLUMN clause whose integer-1
    /// occurrences are not in increasing order of magnitude). SR7, SR8 a) and SR8 c) are the rules that excuse
    /// items "subject to a different PRESENT WHEN clause"; SR10 has no such excuse (kb/Work PB1222).</summary>
    public static readonly DiagnosticDescriptor ReportColumnClauseRule = new(
        "COBOLNET2745", "report-column-clause-rule", EditionSeverity.Error,
        "A report line's COLUMN clauses violate an arrangement rule. ISO §13.18.14.3 SR7: \"Within a given report "
        + "line, any two or more absolute items defined using column numbers that are not in increasing numerical "
        + "order shall be subject to a different PRESENT WHEN clause.\" SR8: a) overlapping items shall each be "
        + "subject to a different PRESENT WHEN clause; b) the rightmost column positions of all absolute items shall "
        + "not exceed the page width; c) a report line that ends in relative items shall not exceed the page width "
        + "unless each of them is subject to a different PRESENT WHEN clause. SR10: a multiple COLUMN clause (more "
        + "than one operand) shall not have an OCCURS clause in the same entry (a), and all the occurrences of "
        + "integer-1 shall be in increasing order of magnitude (b).",
        "ISO §13.18.14.3 SR7/SR8/SR10");

    /// <summary>COBOLNET2749 — the report groups of one report description break a census rule of ISO §13.18.57.3: SR13
    /// (a REPORT HEADING, PAGE HEADING, REPORT FOOTING or PAGE FOOTING written more than once), SR14 (more than one
    /// CONTROL HEADING or CONTROL FOOTING for one control data item or FINAL) or SR15 (no body group). Each rule is
    /// about the SET of groups, not any one group's clauses (kb/Work PB1299).</summary>
    public static readonly DiagnosticDescriptor ReportGroupSetRule = new(
        "COBOLNET2749", "report-group-set-rule", EditionSeverity.Error,
        "The report groups of a report description break a rule on the set of groups. ISO §13.18.57.3 SR13: "
        + "\"REPORT HEADING, PAGE HEADING, REPORT FOOTING, and PAGE FOOTING may each appear no more than once in any "
        + "given report description.\" SR14: \"At most one CONTROL HEADING and at most one CONTROL FOOTING may be "
        + "defined for each control data item or FINAL of the CONTROL clause for any given report.\" SR15: \"Each "
        + "report description shall include at least one body group\", a DETAIL, CONTROL HEADING or CONTROL FOOTING "
        + "group.",
        "ISO §13.18.57.3 SR13/SR14/SR15");

    /// <summary>COBOLNET2750 — a report group entry that contains a SUM clause whose PICTURE category cannot receive a
    /// numeric sender. ISO §13.18.54.3 SR2: "The category of the subject of the entry shall be valid as the category
    /// of a receiving operand in a MOVE statement for a sending operand of the category numeric." The question is
    /// §14.9.25.3 SR10, Table 16: a numeric operand does not move to an alphabetic or a boolean item (kb/Work PB1295).</summary>
    public static readonly DiagnosticDescriptor ReportSumEntryCategory = new(
        "COBOLNET2750", "report-sum-entry-category", EditionSeverity.Error,
        "A report group entry with a SUM clause has a PICTURE of a category that cannot receive a numeric sender. ISO "
        + "§13.18.54.3 SR2: \"The category of the subject of the entry shall be valid as the category of a receiving "
        + "operand in a MOVE statement for a sending operand of the category numeric.\" Table 16 (§14.9.25.3 SR10) "
        + "makes a numeric to alphabetic move and a numeric to boolean move invalid. Describe the entry as numeric, "
        + "numeric-edited, alphanumeric or national.",
        "ISO §13.18.54.3 SR2");

    /// <summary>COBOLNET2751 — a report section subscript that is a sum counter or the LINE-COUNTER or PAGE-COUNTER
    /// identifier. ISO §8.4.2.3.3 SR8: "In the report section, neither a sum counter nor the LINE-COUNTER and
    /// PAGE-COUNTER identifiers may be used as a subscript." Screened once per written report group entry, over
    /// every clause that can carry a subscripted reference except PRESENT WHEN, whose §13.15.3 SR16 refuses every
    /// report section reference (kb/Work PB1474).</summary>
    public static readonly DiagnosticDescriptor ReportSubscriptCounter = new(
        "COBOLNET2751", "report-subscript-counter", EditionSeverity.Error,
        "A report group description entry uses a sum counter, LINE-COUNTER or PAGE-COUNTER as a subscript. ISO "
        + "§8.4.2.3.3 SR8: \"In the report section, neither a sum counter nor the LINE-COUNTER and PAGE-COUNTER "
        + "identifiers may be used as a subscript.\" Subscript with a data item described outside the report section.",
        "ISO §8.4.2.3.3 SR8");

    /// <summary>COBOLNET2710 — a COLUMN clause operand larger than the page width. ISO §13.18.14.3 SR6: "Neither
    /// integer-1 nor integer-2 shall exceed the page width." The page width is the PAGE clause's integer-2, or 999
    /// when it is omitted (§13.18.39.4 GR5). The run-time twin for a printed item whose FINAL column passes the
    /// width is EC-REPORT-PAGE-WIDTH (§13.18.14.4 GR5).</summary>
    public static readonly DiagnosticDescriptor ReportColumnBeyondPageWidth = new(
        "COBOLNET2710", "report-column-beyond-page-width", EditionSeverity.Error,
        "A COLUMN clause operand exceeds the page width. ISO §13.18.14.3 SR6: \"Neither integer-1 nor integer-2 "
        + "shall exceed the page width.\" The page width is integer-2 of the PAGE clause (§13.18.39.2, \"COLUMNS\") "
        + "or, when that is omitted, 999 (§13.18.39.4 GR5). Lower the column, or widen the page with "
        + "`PAGE … integer-2 COLUMNS`.",
        "ISO §13.18.14.3 SR6");

    /// <summary>COBOLNET2711 — a COLUMN clause that writes LEFT, CENTER or RIGHT and also a relative operand. ISO
    /// §13.18.14.3 SR9: "If LEFT, CENTER, or RIGHT is specified, all the operands shall be absolute." (kb/Work
    /// PB1220.)</summary>
    public static readonly DiagnosticDescriptor ReportColumnAlignmentNotAbsolute = new(
        "COBOLNET2711", "report-column-alignment-not-absolute", EditionSeverity.Error,
        "A COLUMN clause writes LEFT, CENTER or RIGHT and a relative (PLUS) operand. ISO §13.18.14.3 SR9: \"If LEFT, "
        + "CENTER, or RIGHT is specified, all the operands shall be absolute.\" Write absolute column numbers, or drop "
        + "the alignment word (LEFT is then assumed for an absolute operand).",
        "ISO §13.18.14.3 SR9");

    /// <summary>COBOLNET2712 — a RIGHT or CENTER aligned absolute operand whose leftmost column falls before column
    /// 1. ISO §13.18.14.4 GR6 c)/d) derive the leftmost column from integer-1 and the printable-size, and a report
    /// line has no column before 1 (GR6 a: "Integer-1 specifies an absolute column number"; a column number is a
    /// positive integer, GR2's correspondence between a column and a character). The standard states no outcome
    /// for it, so it is a compile-time error rather than a clamp that would make GR9's horizontal counter lie
    /// (docs/CONFORMANCE.md §3 "COLUMN alignment").</summary>
    public static readonly DiagnosticDescriptor ReportColumnLeftOfLine = new(
        "COBOLNET2712", "report-column-left-of-line", EditionSeverity.Error,
        "A RIGHT or CENTER aligned COLUMN operand places the printable item's leftmost column before column 1. ISO "
        + "§13.18.14.4 GR6 c)/d) derive the leftmost column from integer-1 and the printable-size, and the line "
        + "starts at column 1. Raise the column number or shorten the item.",
        "ISO §13.18.14.4 GR6 c)/d)");

    public static readonly DiagnosticDescriptor ExternalTypeRule = new(
        "COBOLNET1558", "external-type-rule", EditionSeverity.Error,
        "An EXTERNAL type declaration is misused: a data description containing an EXTERNAL type shall be at "
        + "level-number 1 (§13.18.22.4 GR2), and an external record whose type declaration is strongly typed "
        + "requires that type declaration to be external too (§13.18.22.3 SR5).", "ISO §13.18.22.3 SR5 / §13.18.22.4 GR2/GR3");

    // ── COBOLNET0899 — a file record whose leaf has no byte image ────────────────────────
    // ⛔ NOT A NATIONAL STAGE ANY MORE, AND THE NAME IS THE LAST TRACE OF ONE (kb/Work PB646). Every national
    // shape this descriptor was written for is LIVE: the category and the national-edited form (PB492), the
    // byte surfaces (PB231 + PB327), the SORT/MERGE keys (PB678), and — with PB646 — §13.18.60.3 SR12's
    // national-form numeric, numeric-edited and boolean. ONE caller remains,
    // DataBinder.GateFileRecordByteSurface, and its residue clause now names only what
    // DataBinder.ByteWindowResidueOf still refuses: a leaf with no bound representation (the staged §13.18.60
    // USAGE band — USAGE FUNCTION-POINTER / MESSAGE-TAG, which gain no PicInfo at all). The id stays
    // "national-data" because it is a PUBLISHED diagnostic key; the TEXT says what it reports.
    public static readonly DiagnosticDescriptor NationalData = new(
        NotImplemented, "national-data", EditionSeverity.Error,
        "An FD/SD record cannot be laid out because one of its leaves has no byte image — the byte-window "
        + "carriage gate (DataBinder.ByteWindowResidueOf) refuses it. What that means today is a leaf of the "
        + "staged §13.18.60 USAGE band, which gains no representation at all. Every NATIONAL shape this "
        + "diagnostic once staged now RIDES: the category and national-edited (kb/Work PB492), a national leaf "
        + "in an FD/SD record, an EXTERNAL/BASED/ADDRESS-OF cell, a REDEFINES overlay and an INDEXED record "
        + "key (PB231 + PB327 — the record codec lays a national position out as its two UTF-16BE bytes, "
        + "§13.18.60.4 GR8 / D-N1), a national SORT/MERGE key in either SORT format (PB678 — §14.9.40.4 GR5 / "
        + "§14.9.24.4 GR5 resolve an alphanumeric AND a national sequence and each key takes the one its CLASS "
        + "names), and §13.18.60.3 SR12's national-form numeric, numeric-edited and boolean items (PB646 — "
        + "design D-N7).",
        "ISO §8.5 / §13.18.60", RecognizedNotImplemented);
    // ⛔ `NationalThroughRange` (`national-through-range`) IS GONE — kb/Work PB761, discharged in landing train
    // 18, and this comment stands where it was so it is not re-added. It staged a condition-name THROUGH range
    // over a national conditional variable as "recognized but not yet implemented (Phase 4a residue)" on the
    // citation §13.18.63 SR31 — a clause that governs when the IN alphabet-name-1 phrase may be written and
    // says nothing about implementability. §14.7.8 rule 2 governs the range, ConditionRenderer has emitted it
    // under __COLLATE_NAT since PB678, and it was MEASURED correct for the elementary and the group shape alike
    // (both flip with the national alphabet). The stage was refusing conforming source. The SR29 boolean ban
    // (COBOLNET0898) is the only THROUGH refusal a condition-name entry now draws.

    // ── COBOLNET0899 — PICTURE/USAGE staging ─────────────────────────────────────────────────────────
    public static readonly DiagnosticDescriptor UsageKeywordUnmappedInternal = new(
        NotImplemented, "usage-keyword-unmapped-internal", EditionSeverity.Error,
        "Internal: a grammar-accepted USAGE keyword has no ParseUsage mapping (a compiler defect).",
        "ISO §13.18.60", RecognizedNotImplemented);
    public static readonly DiagnosticDescriptor ConstructStagedNotImplemented = new(
        NotImplemented, "construct-staged-not-implemented", EditionSeverity.Error,
        "A registry-recognized construct is available at this edition but not yet implemented (staged loud).",
        "COMPLETION_ROADMAP_COUNCIL", RecognizedNotImplemented);

    // ── COBOLNET0899 — Report Writer, deferred features ──────────────────────────────────────────────
    // ⛔ `ReportGlobalClause` (`report-global-clause`) LIVED HERE AND IS GONE (kb/Work PB369): the GLOBAL clause on
    // a report description entry (§13.18.27.3 SR1 e)) is implemented — contained-program visibility of the
    // report-name, its groups and its sum counters, and the §14.9.49.4 GR4 Format-2 declarative selection.
    // ⛔ `ReportCodeClause` (`report-code-clause`) LIVED HERE AND IS GONE (kb/Work PB1129): the RD CODE clause
    // (§13.18.12) is implemented — the code prefixes every logical record the report writes, an identifier is
    // evaluated per body group, and its three syntax rules report as `ReportCodeClauseRule` (COBOLNET2713).
    /// <summary>COBOLNET2713 — an RD CODE clause that breaks one of its three syntax rules (ISO §13.18.12.3): SR1 literal-1
    /// shall be an alphanumeric literal; SR2 identifier-1 shall reference an alphanumeric data item that is not an
    /// occurs-depending-on group, a variable-length group or a dynamic-length elementary item; SR3 the clause is
    /// specified for every report of a report file or for none. The message names the rule.</summary>
    public static readonly DiagnosticDescriptor ReportCodeClauseRule = new(
        "COBOLNET2713", "report-code-clause-rule", EditionSeverity.Error,
        "A CODE clause breaks a syntax rule of ISO §13.18.12.3: literal-1 shall be an alphanumeric literal (SR1); "
        + "identifier-1 shall reference an alphanumeric data item that is not an occurs-depending-on group item, a "
        + "variable-length group, or a dynamic-length elementary item (SR2); and if the CODE clause is specified for "
        + "any report it shall be specified for each report associated with the same report file (SR3).",
        "ISO §13.18.12.3 SR1–SR3");
    // ⛔ `ReportLineNextPage` (`report-line-next-page`) LIVED HERE AND IS GONE (kb/Work PB1001), and this comment
    // stands where it did so it is not re-added. It refused the LINE clause's NEXT PAGE phrase (§13.18.35.2 Format
    // 1 — `integer-1 ON NEXT PAGE` and the bare `ON NEXT PAGE` operand) at every edition, and orphaned the
    // group's COLUMN entries into a second, cascading error. The phrase is now LIVE: a flag on the group's first
    // report line (DataBinder.Reports RepeatedLine) that the engine reads as §13.18.35.4 GR4a (body group — the
    // page fit is declared unsuccessful) and GR5a (report footing — it begins on a new page). Its syntax rules
    // SR3/SR5/SR7/SR8 report through the LINE clause family code, COBOLNET2199 (ScreenReportLineClauses).
    // ⛔ `ReportNextGroupClause` (`report-next-group-clause`) LIVED HERE AND IS GONE (kb/Work PB957), and this
    // comment stands where it did so it is not re-added. It refused the NEXT GROUP clause (§13.18.37) by name at
    // every edition; the clause is now LIVE — bound by DataBinder.Reports BindNextGroupClauses and applied by the
    // report engine's ApplyNextGroup after a group's last line (§13.18.37.4 GR2–GR6). Its syntax rules report
    // through ReportNextGroupClauseRule (COBOLNET2284). The id is retired, never reallocated.
    // ⛔ `ReportOccursInGroup` (`report-occurs-in-group`) AND `ReportMultipleLine` (`report-multiple-line`) LIVED
    // HERE AND ARE GONE (kb/Work PB565), and this comment stands where they did so neither is re-added. They
    // staged the VERTICAL repetition of a report group description entry — an OCCURS clause on an entry that
    // contains or has subordinate to it a LINE clause (§13.18.38.4 GR10c/GR10d, GR12c/GR12d), and its
    // §13.18.35.4 GR9 twin the multiple LINE clause. Both axes are now LIVE: the subtree replay binds either
    // one, GR12's integer-3 displaces on the entry's own axis, and the syntax rules each report through the
    // family code that owns them (COBOLNET2021 for the OCCURS clause, COBOLNET2199 for the LINE clause). The
    // ids are retired, never reallocated.
    // ⛔ `ReportVaryingCounterInExpression` (`report-varying-counter-in-expression`) LIVED HERE AND IS GONE (kb/Work
    // PB1306), and this comment stands where it did so it is not re-added. It staged the §13.18.64.3 SR3-legal
    // reference to a VARYING counter in arithmetic-expression-2 of its own clause. A counter is now a named, scoped
    // data item — one compose-local variable per counter, in scope within its entry and every subordinate entry
    // (SR2) — so a FROM, a BY, a SOURCE and a subscript all resolve it through the ONE expression binder, and a BY
    // that names a counter of its own entry is the running recurrence GR3 b) describes.
    public static readonly DiagnosticDescriptor ReportConditionFunction = new(
        NotImplemented, "report-condition-function", EditionSeverity.Error,
        "A FUNCTION reference inside a report PRESENT WHEN condition is not yet implemented.",
        "ISO §13.18.41", RecognizedNotImplemented);
    // ⛔ `ReportIndicateRelativeColumn` (`report-indicate-relative-column`, COBOLNET0899) LIVED HERE AND IS GONE
    // (kb/Work PB1244), and this comment stands where it did so it is not re-added. It refused GROUP INDICATE on an
    // entry with a relative (PLUS) COLUMN operand — legal source (§13.18.28.3 SR1 asks only for "a COLUMN clause") —
    // because the engine blanked a fixed column span after composing the line. GROUP INDICATE is now what
    // §13.18.28.4 GR1 says it is, a PRESENT WHEN on the item, so the relative operand needs nothing of its own.
    // COBOLNET0899 is the shared recognized-not-implemented code and stays; only this NAME is retired, never reused.
    // ⛔ `ReportNonDisplayItem` LIVED HERE AND IS GONE (kb/Work PB541). It staged a "not supported" refusal of
    // every non-DISPLAY printable item under a §13.15 citation that says no such thing, which refused the
    // NATIONAL half of §13.18.60.3 SR7 along with the usages the rule really excludes. The rule now has its own
    // code and its own descriptor, ReportUsageNotDisplayOrNational; the id is retired, never reallocated.
    public static readonly DiagnosticDescriptor ReportUsageNotDisplayOrNational = new(
        "COBOLNET2198", "report-usage-not-display-or-national", EditionSeverity.Error,
        "A USAGE clause associated with a report group item specifies neither DISPLAY nor NATIONAL. ISO "
        + "§13.18.60.3 SR7: \"Only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause "
        + "associated with a report group item.\" The same code carries SR2's group/subordinate agreement "
        + "(\"the same usage shall be specified in both entries\"), which is what makes §13.18.60.4 GR1's "
        + "inheritance of a group entry's usage unambiguous.", "ISO §13.18.60.3 SR7 / SR2");
    // SUPPRESS PRINTING (§14.9.45) syntax-rule violation: the statement may appear ONLY in a USE BEFORE
    // REPORTING procedure (§14.9.45.3 SR1), which fixes the affected report group (§14.9.45.4 GR1). Written
    // anywhere else there is no group to inhibit — a genuine user error, not a non-support.
    public static readonly DiagnosticDescriptor ReportSuppressContext = new(
        "COBOLNET1581", "report-suppress-context", EditionSeverity.Error,
        "A SUPPRESS statement may appear only in a USE BEFORE REPORTING procedure.", "ISO §14.9.45.3 SR1");
    // §12.4.5.7 file-control COLLATING SEQUENCE (INDEXED record-key collating).
    // ⛔ THE NON-INDEXED ARM IS §12.4.5.2 SR8, NOT §12.4.5.7.1. §12.4.5.1 prints the collating-sequence-clause
    // in Format 1 (indexed) and in no other format, so writing it SPECIFIES Format 1 and SR8 — "Format 1 shall
    // be specified only for an indexed file" — is the sentence that forbids it elsewhere. The citation shipped
    // here was §12.4.5.7.1, the clause's descriptive General paragraph, which states no obligation; repaired in
    // kb/Work PB742, which landed SR8's key-clause operands as rows of FileControlKeyRules.
    public static readonly DiagnosticDescriptor FileCollatingKey = new(
        "COBOLNET1582", "file-collating-key", EditionSeverity.Error,
        "A file-control COLLATING SEQUENCE clause is malformed: at most one file-level clause is allowed "
        + "(§12.4.5.7.3 SR3), and every key-level name shall be a declared RECORD/ALTERNATE RECORD KEY of the "
        + "entry (SR4/SR5), named in at most one clause (SR8). It also refuses the clause on a NON-INDEXED file: "
        + "§12.4.5.1 prints the collating-sequence-clause in Format 1 (indexed) and in no other format, so "
        + "writing it specifies Format 1, and §12.4.5.2 SR8 — \"Format 1 shall be specified only for an indexed "
        + "file\" — is the sentence that forbids it elsewhere. That rejection cited §12.4.5.7.1, the clause's "
        + "descriptive General paragraph, which states no obligation; repaired in kb/Work PB742, which added "
        + "SR8's key-clause operands as rows of FileControlKeyRules.",
        "ISO §12.4.5.7.3 SR3-SR8 / §12.4.5.2 SR8");
    public static readonly DiagnosticDescriptor FileCollatingAlphabet = new(
        "COBOLNET1583", "file-collating-alphabet", EditionSeverity.Error,
        "A file-control COLLATING SEQUENCE clause names an alphabet that is not declared in SPECIAL-NAMES or that "
        + "defines no collating sequence of the class the rule requires: alphabet-name-1 alphanumeric (SR1), "
        + "alphabet-name-2 national (SR2), alphabet-name-3 the class of each key it names (SR7). Screened on the "
        + "clause as written, whatever keys it ends up reaching.", "ISO §12.4.5.7.3 SR1/SR2/SR7");
    // kb/Work PB1074 — REFUSED, not accepted inert: the band's axis (EditionContext.Declined) is whether an inert
    // reading exists, and ignoring the alphabet would order the key — and judge its uniqueness (§12.4.5.12.4 GR1)
    // — by the native national sequence instead of the one written, which is a different ANSWER. The descriptor
    // said Warning "orders natively" while its only site raised an Error; the site now asks Declined, and the
    // severity lives here.
    public static readonly DiagnosticDescriptor FileCollatingNationalUnsupported = new(
        "COBOLNET1584", "file-collating-national-unsupported", EditionSeverity.Error,
        "a national collating sequence defined by literals or by the LOCALE phrase, applied to a national record "
        + "key of an indexed file (ISO §12.4.5.7.4 GR3/GR6), is a processor-dependent capability (§4.2.6; Annex "
        + "A.3 item 41) that this implementation does not provide. The clause is REFUSED rather than accepted "
        + "inert, because an inert compile would order the key and judge its uniqueness by the native national "
        + "sequence instead of the one written. A NATIVE or UCS-4 national alphabet, and every alphanumeric "
        + "alphabet on an alphanumeric key, is supported. See docs/CONFORMANCE.md §2 rows 41-42.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 41 / §12.4.5.7.4", RecognizedNotImplemented, Annex: DeclinedAnnex.A3);
    public static readonly DiagnosticDescriptor DefineNoOverrideRedefinition = new(
        "COBOLNET1618", "define-no-override-redefinition", EditionSeverity.Error,
        "A >>DEFINE directive redefines a compilation variable to a different value without the OVERRIDE phrase "
        + "(the previous definition was neither OFF nor the same value).", "ISO §7.3.11.3 SR2");
    public static readonly DiagnosticDescriptor DirectiveExpressionViolation = new(
        "COBOLNET1619", "directive-expression-violation", EditionSeverity.Error,
        "A compiler-directive expression is malformed — a syntax error, or a formation-rule violation such as a "
        + "floating-point literal / figurative constant / concatenation in a directive (§7.3.3 SR10), a non-literal "
        + "or wrong-category operand, an exponentiation or division-by-zero in a compile-time arithmetic expression, "
        + "or a category-mismatched / non-numeric-ordering constant-conditional relation.",
        "ISO §7.3.6 / §7.3.7 / §7.3.8");
    /// <summary>COBOLNET2649 — a conditional-compilation directive breaks the STRUCTURE its general format writes
    /// (kb/Work PB1363): an <c>&gt;&gt;ELSE</c> / <c>&gt;&gt;END-IF</c> / <c>&gt;&gt;WHEN</c> / <c>&gt;&gt;END-EVALUATE</c>
    /// with no open directive of its kind, a second <c>&gt;&gt;ELSE</c> or <c>&gt;&gt;WHEN OTHER</c>, a
    /// <c>&gt;&gt;WHEN</c> after <c>&gt;&gt;WHEN OTHER</c>, an <c>&gt;&gt;EVALUATE</c> with no <c>&gt;&gt;WHEN</c>,
    /// text between <c>&gt;&gt;EVALUATE</c> and its first <c>&gt;&gt;WHEN</c>, a directive left open at the end of its
    /// library text or source text, or phrases of one directive written in different library texts.
    /// <c>ConditionalCompilationProcessor</c>'s frame stack is its only reporter.</summary>
    public static readonly DiagnosticDescriptor DirectiveStructureViolation = new(
        "COBOLNET2649", "directive-structure-violation", EditionSeverity.Error,
        "A conditional-compilation directive breaks the structure of its general format. ISO §7.3.16.2: "
        + ">> IF, an optional >> ELSE, then >> END-IF; §7.3.13.2: >> EVALUATE, one or more >> WHEN, an optional final "
        + ">> WHEN OTHER, then >> END-EVALUATE. The phrases of one IF directive (§7.3.16.3 SR7) and of one EVALUATE "
        + "directive (§7.3.13.3 SR9) are all in the same library text or all in source text. The message names the "
        + "violated phrase.",
        "ISO §7.3.16.2 / §7.3.16.3 SR7 / §7.3.13.2 / §7.3.13.3 SR9");
    /// <summary>COBOLNET2650 — a compilation-variable-name is a compiler-directive word (kb/Work PB1366): the name of a
    /// DEFINE directive or of a defined condition. <c>CompilerDirectiveWords</c> is the one §8.12 representation and
    /// the DEFINE arm of <c>ConditionalCompilationProcessor</c> and the defined-condition evaluation are its callers.</summary>
    public static readonly DiagnosticDescriptor DirectiveWordAsName = new(
        "COBOLNET2650", "directive-word-as-compilation-variable-name", EditionSeverity.Error,
        "A compilation-variable-name is the same as a compiler-directive word. ISO §7.3.11.3 SR1: "
        + "\"Compilation-variable-name-1 shall not be the same as a compiler-directive word.\" ISO §7.3.8.4.3 SR1 says "
        + "the same of a defined condition. ISO §7.3.3 SR7: a compiler-directive word is reserved within the context of "
        + "a directive that specifies it, and §8.12 lists the words — and reserves every exception-name of §14.6.13.1 "
        + "in the context of compiler directives. §7.3.3 SR9 reserves IMP for the implementor.",
        "ISO §7.3.11.3 SR1 / §7.3.8.4.3 SR1 / §7.3.3 SR7, SR9 / §8.12");

    /// <summary>COBOLNET2651 — a DEFINE directive does not match its general format (kb/Work PB1367): no
    /// compilation-variable-name, a name that is not a COBOL word, no operand, OVERRIDE attached to OFF, or words after
    /// OFF. The DEFINE arm of <c>ConditionalCompilationProcessor</c> is its only reporter.</summary>
    public static readonly DiagnosticDescriptor DefineDirectiveMalformed = new(
        "COBOLNET2651", "define-directive-malformed", EditionSeverity.Error,
        "A >>DEFINE directive does not match its general format (ISO §7.3.11.2): >>DEFINE compilation-variable-name-1 "
        + "[AS] { { arithmetic-expression-1 | boolean-expression-1 | literal-1 | PARAMETER } [OVERRIDE] | OFF }. "
        + "OVERRIDE qualifies the value alternative only; OFF is an alternative of its own. The message names the "
        + "violated part.", "ISO §7.3.11.2");
    // §7.3.14 / §7.3.15 migration-flagging directives — the warning channel (one code per directive; each emit
    // carries the specific option's message + GR4/Annex-E citation). Warning: a flag NEVER fails a compile.
    public static readonly DiagnosticDescriptor Flag02Warning = new(
        "COBOLNET1620", "flag-02-incompatibility", EditionSeverity.Warning,
        "A construct is flagged by an active >>FLAG-02 option — a 2002-to-2014 incompatibility potentially "
        + "affecting existing programs (the specific option + change is named in the message).", "ISO §7.3.14");
    public static readonly DiagnosticDescriptor Flag14Warning = new(
        "COBOLNET1621", "flag-14-incompatibility", EditionSeverity.Warning,
        "A construct is flagged by an active >>FLAG-14 option — a 2014-to-2023 incompatibility potentially "
        + "affecting existing programs (the specific option + change is named in the message).", "ISO §7.3.15");
    public static readonly DiagnosticDescriptor FlagDirectiveMalformed = new(
        "COBOLNET1622", "flag-directive-malformed", EditionSeverity.Error,
        "A >>FLAG-02 / >>FLAG-14 directive is malformed — an unknown option word, no option or ALL named, ALL "
        + "combined with individual options, or (FLAG-14) a missing ON/OFF phrase.", "ISO §7.3.14.2 / §7.3.15.2");
    // §7.3.10 COBOL-WORDS directive — a malformed directive or a syntax-rule violation. Error: an ill-formed or
    // rule-violating word-modification would silently mis-shape the reserved/context/function word tables.
    public static readonly DiagnosticDescriptor CobolWordsDirectiveInvalid = new(
        "COBOLNET1623", "cobol-words-directive-invalid", EditionSeverity.Error,
        "A >>COBOL-WORDS directive is malformed or violates a syntax rule — a missing/unknown option word, a "
        + "missing WITH/BY, a non-plain-alphanumeric literal (SR2), a word used in more than one directive (SR5), "
        + "an existing word that is not a reserved / "
        + "context-sensitive / intrinsic-function word (SR3), or a new word that is not a valid user-defined word "
        + "or is itself reserved/context/intrinsic (SR4). The message names the specific rule.",
        "ISO §7.3.10.2 / §7.3.10.3");
    // ⛔ `ReportSourceOtherReportCounter` (`report-source-other-report-counter`) AND `ReportSourceSubscripted`
    // (`report-source-subscripted`) LIVED HERE AND ARE GONE (kb/Work PB1456, PB1292), and this comment stands where
    // they did so neither is re-added. They staged two shapes of a SOURCE identifier-1 — another report's LINE-COUNTER
    // / PAGE-COUNTER (§8.4.2.2.3 SR9/SR10) and a subscripted or reference-modified identifier (§8.4.3.1.2) — because
    // the identifier arm looked a NAME up in storage. identifier-1 is now bound in the procedure phase by the one
    // operand binder a MOVE's sending operand takes, which knows both.
    // ⛔ `ReportSumRolledTotal` (`report-sum-rolled-total`), `ReportSumCrossReport` (`report-sum-cross-report`) AND
    // `ReportSumUponCrossReport` (`report-sum-upon-cross-report`) LIVED HERE AND ARE GONE (kb/Work PB1294): all three
    // staged one mechanism — a sum counter fed by something that is not a GENERATE of its own report. A SUM data-name-1
    // (a rolled total, §13.18.54.4 GR6), its report-name-qualified spelling (SR4 g)) and an UPON naming a detail of
    // another report (GR7 c) 2)) are now registered with the engine whose group or GENERATE triggers the addition
    // (`CobolReport.AddRolled`, `AddGenerateTrigger`), so the report that owns the counter and the report that fires it
    // may be different. The ids are retired, never reallocated.

    /// <summary>COBOLNET2730 — a SUM addend written as data-name-1 (a report section item, §13.18.54.3 SR4) breaks the
    /// rule that governs it: it shall name ONE numeric data item with a value (a SUM, SOURCE or VALUE entry) in the
    /// report section, written without subscripting; the UPON phrase shall not be written with it (SR4 a)); and its
    /// repetition shall fit the subject's — a repeating item when it shares the subject's report group description
    /// with at least one more level (SR4 b)), at least the subject's levels when it does not (SR4 c)), equal maximum
    /// repetitions at each corresponding level (SR4 d)) — and the chain of references shall terminate (SR4 e)).</summary>
    public static readonly DiagnosticDescriptor ReportSumDataNameRule = new(
        "COBOLNET2730", "report-sum-data-name-rule", EditionSeverity.Error,
        "§13.18.54.3 SR4: \"Data-name-1 shall be the name of a numeric data item in the report section. If it is "
        + "associated with an OCCURS clause, it shall be specified without the subscripting normally required. When "
        + "data-name-1 is specified, the following rules also apply: a) The UPON phrase shall not be specified. b) If "
        + "data-name-1 is specified in the same report group description as the subject of the entry, data-name-1 "
        + "shall be a repeating item … and subject to at least one more level of repetition than the subject of the "
        + "entry. c) If data-name-1 is specified in a different report group description than the subject of the "
        + "entry, data-name-1 either shall not reference a repeating item or shall reference a repeating item that "
        + "is subject to at least the same number of levels of repetition as the subject of the entry. d) The "
        + "maximum number of repetitions of data-name-1 and the subject of the entry shall be equal at each "
        + "corresponding level taken in order beginning with the lowest level of nesting. e) Any chain of reference "
        + "shall terminate at an entry that does not contain a SUM clause referring to a data-name-1 defined in the "
        + "report section.\" The value added is that of the entry's sum counter, or of the operand of its SOURCE or "
        + "VALUE clause (§13.18.54.4 GR6), so an entry with none of the three has no value to add.",
        "ISO §13.18.54.3 SR4");

    /// <summary>COBOLNET2731 — the combination of report group TYPEs of a SUM entry and the entry its data-name-1
    /// names is not one §13.18.54.3 SR4 f) permits within a report description (SR4 g) lifts the restriction across
    /// report descriptions).</summary>
    public static readonly DiagnosticDescriptor ReportSumDataNameGroups = new(
        "COBOLNET2731", "report-sum-data-name-groups", EditionSeverity.Error,
        "§13.18.54.3 SR4 f): \"If data-name-1 specifies an entry in a report group description other than the current "
        + "report group description, only the following combinations of report types of each report group are "
        + "permitted: The current report group may be a control footing and data-name-1 may be defined in a detail or "
        + "in a control footing associated with a lower level of control. The current report group may be a detail "
        + "and data-name-1 may be defined in a different detail. The current report group may be a report footing and "
        + "data-name-1 may be defined in any other report group other than a report heading. The current report "
        + "group may be a page footing and data-name-1 may be defined in any body group.\" SR4 g) — \"If data-name-1 "
        + "specifies an entry in a different report description, there are no restrictions\" — is the exception.",
        "ISO §13.18.54.3 SR4 f)");
    // ⛔ `ReportMultipleOnFile` (`report-multiple-on-file`) LIVED HERE AND IS GONE (kb/Work PB1050): a file description
    // entry may name several reports (§13.18.46.2 `REPORTS ARE report-name-1 …`; GR1 "more than one report may be
    // written to the file"); each report has its own engine over the one file connector.
    // ⛔ `ReportPageCounterReceiving` LIVED HERE AND IS GONE (kb/Work PB429). PAGE-COUNTER as a receiving
    // operand is ISO §8.4.3.15.3 SR1's plain reading — the descriptor's own text said "legal" — and it is now
    // implemented as a receiving-capable place (`Model.ReportPageCounterPlace`), so there is nothing left to
    // stage. The id is retired, never reallocated.

    // ── COBOLNET0899 — Report Writer, semantic validation (genuine errors on the shared code) ─────────
    public static readonly DiagnosticDescriptor ReportGroupBefore01 = new(
        NotImplemented, "report-group-before-01", EditionSeverity.Error,
        "A report group entry appears before any 01-level entry.", "ISO §13.15");
    public static readonly DiagnosticDescriptor ReportPageTypeRequiresPage = new(
        NotImplemented, "report-page-type-requires-page", EditionSeverity.Error,
        "A PAGE HEADING/FOOTING group, or a control heading with the OR PAGE phrase, requires a PAGE clause defining "
        + "the page limit.", "ISO §13.18.57.3 SR12");
    /// <summary>COBOLNET2714 — the report-name ↔ REPORT-clause correspondence is broken (kb/Work PB1285): a REPORT clause
    /// names a report with no report description entry (ISO §13.18.46.3 SR1), a report is named in more than one
    /// REPORT clause (§13.18.46.3 SR2), or a report description entry is named in none (§13.14.3 SR1 — "one and only
    /// one REPORT clause"). It replaces `report-not-in-file`, which carried the not-implemented code for the third arm.</summary>
    public static readonly DiagnosticDescriptor ReportClauseNameRule = new(
        "COBOLNET2714", "report-clause-name-rule", EditionSeverity.Error,
        "A REPORT clause and the report description entries disagree: each report-name-1 of a REPORT clause shall be the "
        + "subject of a report description entry (ISO §13.18.46.3 SR1), may appear in only one REPORT clause (SR2), and "
        + "every report description entry shall be named by one and only one REPORT clause (§13.14.3 SR1).",
        "ISO §13.18.46.3 SR1/SR2; §13.14.3 SR1");
    // ⛔ CITATION REPAIRED (kb/Work PB177 arm C). This descriptor and its two message sites all cited
    // "ISO §13.18.16.3 SR3" for an UNRESOLVABLE CONTROL operand — a REAL clause answering a DIFFERENT
    // question: SR3 is "Data-name-1 shall not be subject to any OCCURS clauses." Nothing in §13.18.16.3
    // governs resolution failure; that is ordinary name resolution, §8.4.2.1. `cite.py --check` on the
    // clause NUMBER alone would have passed either way — the exact failure mode CLAUDE.md rule 1 names.
    public static readonly DiagnosticDescriptor ReportControlOperandUnresolved = new(
        NotImplemented, "report-control-operand-unresolved", EditionSeverity.Error,
        "A CONTROL operand does not resolve to a data item.", "ISO §8.4.2.1");
    // The §13.18.16.3 CONTROL-operand SYNTAX RULES over data-name-1 (kb/Work PB177 arm C, extended by PB205) —
    // ONE screen, one arm per rule, so the next rule on this clause drops in beside them instead of
    // becoming a fourth place the clause is enforced. Every arm was unscreened and MEASURED so: SR3 (an
    // operand subject to an OCCURS clause) and SR5 (an operand with an occurs-depending table subordinate)
    // compiled and ran silently; SR7 (a variable-length group) compiled and staged a RUNTIME Tier-C loud,
    // where a SYNTAX rule requires a compile-time rejection. PB205 added the rules about the WRITTEN
    // REFERENCE rather than about the item's shape — SR2 (rejected, but as an unresolved name under §8.4.2.1:
    // a real clause answering a different question), SR4's integer-literal restriction and SR6's uniqueness,
    // the last two unscreenable while the capture dropped the reference modification they are about — plus
    // the subscript for which SR3 and §8.4.2.3.3 SR2 between them leave no legal spelling.
    public static readonly DiagnosticDescriptor ReportControlOperandShape = new(
        "COBOLNET1699", "report-control-operand-shape", EditionSeverity.Error,
        "ISO §13.18.16.3 syntax rule 2: \"Data-name-1 shall not be defined in the report section.\"; "
        + "syntax rule 3: \"Data-name-1 shall not be subject to any OCCURS clauses.\" (with §8.4.2.3.3 SR2, "
        + "which bars a subscript on an item having no OCCURS clause, so no subscripted control operand is "
        + "legal); syntax rule 4: \"Data-name-1 may be reference-modified. If it is, leftmost-position and "
        + "length shall be integer literals.\"; syntax rule 5: \"The entry specified by data-name-1 shall not "
        + "have an occurs-depending table subordinate to it.\"; syntax rule 6: \"Data-name-1 shall be unique "
        + "in any given CONTROL clause.\"; syntax rule 7: \"Data-name-1 shall not reference a variable-length "
        + "group.\"",
        "ISO §13.18.16.3 SR2/SR3/SR4/SR5/SR6/SR7");
    // An INDEX CONTROL operand is illegal for a reason that lives OUTSIDE §13.18.16.3, which is why it gets its
    // own code rather than riding COBOLNET1699's clause list (kb/Work PB177 arm C follow-up): §13.18.60.3 SR10
    // closes the set of contexts in which an index data item may be referenced explicitly, and §8.4.5 makes a
    // data-division clause naming a data item exactly such an explicit reference. It was a RUNTIME loud —
    // ReportWriterEmitter's §13.18.16.4 GR3 "no character image" backstop — where a syntax rule requires the
    // compile-time rejection. (The FLOAT operand of the same runtime guard is NOT this: no syntax rule bars it,
    // and it stays loud because the RESTORE half has no float channel — see that guard's own comment.)
    public static readonly DiagnosticDescriptor ReportControlOperandIndex = new(
        "COBOLNET1700", "report-control-operand-index", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 10: \"An index data item may be referenced explicitly only in a SEARCH or "
        + "SET statement, a relation condition, an intrinsic function argument, an inline method invocation "
        + "argument, the USING phrase of a procedure division header, or the USING phrase of a CALL or INVOKE "
        + "statement.\" A CONTROL clause naming the item is an explicit reference (§8.4.5).",
        "ISO §13.18.60.3 SR10 / §8.4.5");
    // ⛔ §13.18.44.3 SR5 SENTENCE 1, RESTORED AS THE OBJECT-SIDE AUTHORITY (kb/Work PB177 arm C follow-up).
    // SR5 is FOUR sentences and each side of this family quotes a different one. Sentence 4 ("Neither the
    // original definition nor the redefinition shall include an occurs-depending table") is COBOLNET0855's, and
    // reading only that one produced the claim that the OCCURS-bearing data-name-2 "NO syntax rule literally
    // names" — which is false: sentence 1 is "The data description entry for data-name-2 shall not contain an
    // OCCURS clause", and OCCURS DYNAMIC is Format 4 OF THE OCCURS CLAUSE (§13.18.38), so sentence 1 covers the
    // dynamic-capacity object as squarely as the fixed one. That claim was true only of the SUBJECT side, which
    // keeps COBOLNET1525's §13.18.44.4 GR1 / §8.5.1.9.1 storage-model reasoning.
    // Measured before this screen: `05 T PIC X(3) OCCURS 4. 05 R REDEFINES T PIC X(12).` compiled CLEAN and ran.
    public static readonly DiagnosticDescriptor RedefinesTargetOccurs = new(
        "COBOLNET1701", "redefines-target-occurs", EditionSeverity.Error,
        "ISO §13.18.44.3 syntax rule 5, sentence 1: \"The data description entry for data-name-2 shall not "
        + "contain an OCCURS clause.\" (Sentence 2 permits data-name-2 to be SUBORDINATE to such an item; that "
        + "is a different shape and is not screened here. Sentence 4's occurs-depending prohibition is "
        + "COBOLNET0855's, and owns the entries where data-name-2 IS an occurs-depending table.)",
        "ISO §13.18.44.3 SR5");
    // ⛔ THE GROUP-LEVEL VALUE SUBORDINATE SCREEN (kb/Work PB184). §13.18.63.3 SR13/SR14 restrict what may sit
    // UNDER an entry that carries a group-level VALUE, and NONE of the three restrictions existed: measured on
    // 8ca74a3d, `01 GV VALUE "40537". 05 GB PIC 9 COMP-5 OCCURS 5.` compiled CLEAN and left every occurrence
    // ZERO; so did a JUSTIFIED / SYNCHRONIZED subordinate and a subordinate carrying its own VALUE.
    // ⚠ PB184 was registered as a §13.18.63.4 GR5 DISTRIBUTION gap — "the group area is initialized without
    // consideration for the individual elementary or group items contained within this group", so the byte-form
    // leaves should take their slice of the literal's BYTES. That premise is refuted by SR14: a COMP-5 leaf
    // under an alphanumeric group item carrying a VALUE is not usage DISPLAY, so the program is NOT CONFORMING
    // and there is no area to distribute. GR5 is already implemented for the population SR14 admits (every
    // subordinate usage DISPLAY, where the character image IS the byte image). The missing half was the
    // DIAGNOSTIC on the complement, never a widening of the distributor — the
    // [[validate_the_premise_not_only_the_rule]] shape.
    // SR14 is TWO conjuncts in one sentence and SR13's second sentence is the third restriction on the same
    // subject; all three live here so a future case is automatic rather than a fourth unscreened arm.
    public static readonly DiagnosticDescriptor GroupValueSubordinate = new(
        "COBOLNET1702", "group-value-subordinate", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 14: \"If a VALUE clause is specified at the group level, subordinate items "
        + "within that group shall not be described with a JUSTIFIED or SYNCHRONIZED clause, and all data items "
        + "subordinate to an alphanumeric group item shall be explicitly or implicitly described with usage "
        + "DISPLAY.\"; syntax rule 13, sentence 2: \"The VALUE clause shall not be specified at subordinate "
        + "levels within this group.\" (Syntax rule 16 applies both to the format 2 (table) VALUE.)",
        "ISO §13.18.63.3 SR13/SR14");
    // The SUBJECT half of the same screen. SR1 restricts what the VALUE-carrying entry may BE, and it is the
    // rule that keeps SR14's second conjunct honest: §13.18.29.4 GR3 makes a group an ALPHANUMERIC group item
    // only when it is "not strongly typed and is not a variable-length group", so without SR1 those two shapes
    // reach the SR14 usage arm and are rejected under a rule that does not govern them. Measured (probe, this
    // landing): `01 ST IS TYPEDEF STRONG VALUE "ABCD". 05 SY PIC 9(4) COMP.` and `01 GV VALUE "ABCDE".
    // 05 GB PIC 9(4) COMP OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 5.` each drew COBOLNET1702 naming SR14,
    // while the rule they actually violate — SR1 — was unenforced.
    public static readonly DiagnosticDescriptor GroupValueSubjectShape = new(
        "COBOLNET1703", "group-value-subject-shape", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 1: \"The subject of the entry shall not be a strongly-typed group item or "
        + "a variable-length group.\" (A variable-length group is §8.5.1.12.1's — a group with a dynamic-length "
        + "elementary item or a dynamic-capacity table subordinate to it.)",
        "ISO §13.18.63.3 SR1");
    // ── The FORMAT 2 (table) VALUE GEOMETRY band (kb/Work PB505; DataBinder.ResolveTableValues). SR18, SR20's
    //    and SR21's count sentences, and SR23 had NO site at all: one staged "not implemented" refusal stood in
    //    for the whole population, so the shapes SR18 expressly PERMITS were rejected alongside the ones SR20/
    //    SR22/SR23 forbid, and a user reading the diagnostic was told the compiler was incomplete rather than
    //    that the program was wrong. SR20/SR21's ceiling sentences keep COBOLNET1586/1587 and SR22 keeps
    //    COBOLNET1588 — those three were written down (one dimension deep) and their codes stay byte-stable.
    public static readonly DiagnosticDescriptor TableValueWithoutOccurs = new(
        "COBOLNET1944", "table-value-without-occurs", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 18: \"A data description entry that contains the VALUE clause shall contain "
        + "an OCCURS clause or be subordinate to a data description entry that contains an OCCURS clause.\"",
        "ISO §13.18.63.3 SR18");
    public static readonly DiagnosticDescriptor TableValueSubscriptCount = new(
        "COBOLNET1945", "table-value-subscript-count", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 20: \"In one FROM phrase, there shall be one subscript-1 specified for each "
        + "OCCURS clause for the subject of the entry or superordinate to that entry, specified in the same order "
        + "as a subscripted reference to the subject of the entry would be specified.\"; syntax rule 21, sentence "
        + "1, states the same requirement for subscript-2 in a TO phrase.",
        "ISO §13.18.63.3 SR20/SR21");
    public static readonly DiagnosticDescriptor TableValueDynamicSpanLevels = new(
        "COBOLNET1946", "table-value-dynamic-span-levels", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 23: \"If the TO phrase is specified and an OCCURS clause with a DYNAMIC "
        + "phrase but no TO phrase is specified in the same entry or in any superordinate entry, the values of "
        + "subscript-1 and subscript-2 corresponding to all levels higher than that of the OCCURS clause, if "
        + "applicable, shall be equal\"",
        "ISO §13.18.63.3 SR23");
    // ⛔ NO `BitGroupLevelValue` DESCRIPTOR ANY MORE (kb/Work PB207, landed). A group-level VALUE on a group
    // with a USAGE BIT descendant was staged loud here as recognized-not-implemented, on the ground that
    // §13.18.63.4 GR5 had no area to deposit into. It has one, in the OTHER UNIT: §13.18.29.4 GR1b makes a bit
    // group an as-if `PICTURE 1(m)` item, so its area is m BOOLEAN POSITIONS from the §8.5.1.6.3 walk, and
    // GroupValueSlicer.AreaOf composes it for both initializer lanes. The other half of the refused population
    // — an ALPHANUMERIC group with a USAGE BIT subordinate — was never a compiler gap either: it violates
    // §13.18.63.3 SR14 and is reported as GroupValueSubordinate (COBOLNET1702) now that this descriptor no
    // longer sits in front of that rule. Do not reintroduce the name.
    // ⛔ THE TERMINATION-STATUS PHRASE HAS ITS OWN SYNTAX RULES, and none of them existed (kb/Work PB169). The
    // operand was bound through the ARITHMETIC funnel, so §8.8.1.1 — a rule that does not govern this position —
    // rejected the two shapes the position's own rules explicitly admit: measured on 9a89fbd1, both
    // `STOP RUN WITH ERROR STATUS "ABEND"` (SR3's conditional presupposes the non-numeric literal; SR4 bars only
    // a zero-length one) and `STOP RUN WITH ERROR STATUS WS-CODE` with `WS-CODE PIC X(3)` (SR2 admits a data item
    // "with usage display") drew COBOLNET0844. Meanwhile the position's ACTUAL rules went unenforced in both
    // directions: `STATUS ""`, `STATUS 1.5` and `STATUS <index-name>` all compiled clean. This code carries the
    // rules; the §8.8.1.1 screen no longer reaches the site at all.
    public static readonly DiagnosticDescriptor TerminationStatusOperand = new(
        "COBOLNET1704", "termination-status-operand", EditionSeverity.Error,
        "ISO §14.9.42.3 syntax rule 2: \"Identifier-1 shall reference an integer data item or a data item with "
        + "usage display or usage national.\"; syntax rule 3: \"If literal-1 is numeric, it shall be an "
        + "integer.\"; syntax rule 4: \"Literal-1 shall not be a zero-length literal.\" (GOBACK's §14.9.18.3 "
        + "SR6/SR7/SR8 are the same three rules for the same shared phrase, over identifier-2 — GOBACK's "
        + "identifier-1 is the RAISING object.) NULL written in the phrase is COBOLNET2576, the one §8.4.3.10.3 "
        + "SR1 context screen.",
        "ISO §14.9.42.3 SR2/SR3/SR4");
    // ── The §13.18.60.3 USAGE DECLARATION-PLACEMENT family (kb/Work PB183) ────────────────────────────────
    // Three syntax rules about WHERE a usage phrase may be written, none of which existed anywhere in the
    // compiler. SR14 is the headline: measured on 2acbd842, `01 G. 05 P USAGE POINTER.` compiled and ran, as
    // did a pointer member of a WEAK typedef template and a `05 Q SAME AS P` copy of a level-1 pointer. The
    // rule was verified against the PRINTED page (folio 505, PDF 535) before the screen was written — the
    // reading is restrictive enough that the falsely-restrictive-OCR hazard had to be excluded, and it was:
    // the transcription is character-for-character the printed rule.
    //
    // ⛔ SR14's list OMITS INDEX while the neighbouring SR4 INCLUDES it. That is deliberate drafting, not an
    // oversight to "unify": `05 IX USAGE INDEX.` inside an ordinary group is LEGAL and a positive golden pins
    // it. The two rules take two descriptors and two predicates for exactly that reason.
    public static readonly DiagnosticDescriptor UsageDeclarationPlacement = new(
        "COBOLNET1724", "usage-declaration-placement", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 14: \"A USAGE clause with the MESSAGE-TAG, OBJECT REFERENCE, POINTER, "
        + "FUNCTION-POINTER, or PROGRAM-POINTER phrase may be specified only for an elementary data item at "
        + "level 1 or an elementary data item subordinate to a type declaration that includes the STRONG "
        + "phrase.\" (A level-77 entry satisfies the first arm: §13.11.1 makes the level-1 and level-77 "
        + "spellings ALTERNATIVES for one data element that \"bear[s] no hierarchical relationship to any "
        + "other data item\", and §8.5.1.3.2 puts a 77 entry outside the level system altogether — \"three "
        + "types of entries exist for which there is no true concept of level\". The group form is reached "
        + "through §13.18.60.4 GR1, which applies a group's usage \"only to each elementary item in the "
        + "group\" — at that item's own level.)",
        "ISO §13.18.60.3 SR14");
    public static readonly DiagnosticDescriptor UsageObjectReferenceFileSection = new(
        "COBOLNET1725", "usage-object-reference-file-section", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 15: \"The USAGE OBJECT REFERENCE clause shall not be specified in the "
        + "file section.\" (The SAME AS twin — §13.18.49.3 SR6, a file-section SAME AS whose data-name-1 "
        + "description contains an object reference — is COBOLNET1556; this is the DIRECT declaration arm, "
        + "which had no screen at all.)",
        "ISO §13.18.60.3 SR15");
    public static readonly DiagnosticDescriptor UsageConstantRecord = new(
        "COBOLNET1726", "usage-constant-record", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 4: \"The INDEX, MESSAGE-TAG, OBJECT REFERENCE, POINTER, FUNCTION-POINTER, "
        + "and PROGRAM-POINTER phrases shall not be specified in a data item described with the CONSTANT "
        + "RECORD clause, or in any item subordinate to a data item described with the CONSTANT RECORD "
        + "clause.\" (SIX phrases — INDEX is in THIS rule's list and NOT in SR14's.)",
        "ISO §13.18.60.3 SR4");
    // ⛔ THE TWO SCREENS THE POINTER-CLASS STORAGE LANDING MADE NECESSARY (kb/Work PB231). Until it, a
    // pointer-class leaf under BASED / EXTERNAL / ADDRESS OF drew the byte-window residue diagnostic
    // COBOLNET1695 ("recognized but not yet implemented"), which happened to reject these two NONCONFORMING
    // shapes as well — for the wrong reason. Opening the gate would therefore have turned each into a silent
    // UNDER-REJECTION, so the rules that actually bar them are enforced in the same change set.
    public static readonly DiagnosticDescriptor ExternalPointerOrObjectItem = new(
        "COBOLNET1796", "external-pointer-or-object-item", EditionSeverity.Error,
        "ISO §13.18.22.3 syntax rule 4: \"The EXTERNAL clause shall not be specified for a data item of class "
        + "object or pointer.\" (The rule names the ITEM's own class, so a strongly-typed EXTERNAL group with a "
        + "pointer MEMBER is not barred by it — that group is class alphanumeric, and §13.18.22.3 SR5 requires "
        + "only that its type declaration also be external. Class pointer covers the data-pointer and "
        + "program-pointer categories; class object is the object-reference category.)",
        "ISO §13.18.22.3 SR4");
    public static readonly DiagnosticDescriptor BasedSubjectOfClassObject = new(
        "COBOLNET1797", "based-subject-of-class-object", EditionSeverity.Error,
        "ISO §13.18.5.3 syntax rule 1: \"The subject of the entry shall not be of class object.\" (Only class "
        + "OBJECT — a BASED data-pointer or program-pointer entry is class POINTER and is legal, which is the "
        + "asymmetry with the EXTERNAL clause's §13.18.22.3 SR4, whose list carries both classes. SR2's shapes, "
        + "a dynamic-length elementary item and a variable-length group, have their own screens.)",
        "ISO §13.18.5.3 SR1");
    // The OPTIONS INITIALIZE clause's ONE syntax rule (kb/Work PB152). ⛔ "hexadecimal-alphanumeric literal" is a
    // DEFINED TERM, not loose wording for "alphanumeric or hex": §8.3.3.2.2 gives the alphanumeric literal exactly
    // two formats — format 1 `"…"` / `'…'` and format 2 `X"…"` / `X'…'` — and "hexadecimal-alphanumeric" names
    // format 2. So `INITIALIZE ALL TO "Z"` is NOT a conforming spelling; `X"5A"` is. Measured on 2acbd842, the
    // decoder took `raw[0]` for ANY shape, so `INITIALIZE ALL TO "AB"` silently became 'A' — a fill character the
    // program never asked for, from a literal the standard does not admit. (PB151's own golden was written with
    // the format-1 spelling and is repaired to X"51" by this landing.)
    public static readonly DiagnosticDescriptor OptionsInitializeFillLiteral = new(
        "COBOLNET1727", "options-initialize-fill-literal", EditionSeverity.Error,
        "ISO §11.9.10.3 syntax rule 1: \"Literal-1 shall specify a one-byte hexadecimal-alphanumeric literal.\" "
        + "(A hexadecimal-alphanumeric literal is §8.3.3.2.2's FORMAT 2 — X\"…\" or X'…' — whose "
        + "hex-character-sequence \"shall be composed of hexadecimal digits\" (§8.3.3.2.3 SR5). One byte is "
        + "exactly two hexadecimal digits.)",
        "ISO §11.9.10.3 SR1");
    // ⛔ THE THREE SCREENS THAT NARROW THE USAGE CLAUSE'S FLOAT FORMAT PHRASES (kb/Work PB174). The grammar
    // parses `floatFormatPhrase*` after ANY usageKeyword — the established binarySign / noSignPhrase posture, a
    // superset parse the binder narrows (DESIGN-version-conformance-pipeline's parse-wide/bind-narrow direction).
    // The general format is what scopes each phrase: §13.18.60.2 prints the endianness-phrase ONLY on
    // FLOAT-BINARY-32/-64/-128 and FLOAT-DECIMAL-16/-34, and the encoding-phrase ONLY on FLOAT-DECIMAL-16/-34
    // (verified against the PRINTED page, PDF p.533 = printed 503). GR19c/d and GR20c corroborate by naming the
    // usage families the OPTIONS clauses supply the IMPLIED phrase for.
    public static readonly DiagnosticDescriptor UsageEndiannessPhraseScope = new(
        "COBOLNET1716", "usage-endianness-phrase-scope", EditionSeverity.Error,
        "ISO §13.18.60.2 general format: the endianness-phrase is written only with the standard floating-point "
        + "usages — FLOAT-BINARY-32, FLOAT-BINARY-64, FLOAT-BINARY-128, FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34. "
        + "§13.18.60.4 general rule 19 scopes its meaning the same way: c) \"For the standard binary "
        + "floating-point usages, if neither the HIGH-ORDER-LEFT phrase nor the HIGH-ORDER-RIGHT phrase is "
        + "specified, 11.9.8, FLOAT-BINARY clause, specifies which of these phrases is implied.\" and d) its "
        + "standard-decimal twin. The implementor-defined float usages (COMP-1/COMP-2/FLOAT-SHORT/-LONG/"
        + "-EXTENDED) are outside both — GR13/GR21 leave their representation to the implementor, and WiseOwl COBOL "
        + "pins them big-endian (Annex A.1 item 48).",
        "ISO §13.18.60.2 / §13.18.60.4 GR19c-d");
    public static readonly DiagnosticDescriptor UsageEncodingPhraseScope = new(
        "COBOLNET1717", "usage-encoding-phrase-scope", EditionSeverity.Error,
        "ISO §13.18.60.2 general format: the encoding-phrase is written only with the standard DECIMAL "
        + "floating-point usages, FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34 — it is absent from every other line of "
        + "the figure, the standard BINARY float usages included. §13.18.60.4 general rule 20a says the same in "
        + "prose: \"The BINARY-ENCODING phrase specifies that the encoding of the information in a data item "
        + "described with any standard decimal floating-point usage is the binary encoding as specified in "
        + "ISO/IEC 60559:2020, 3.5.\"",
        "ISO §13.18.60.2 / §13.18.60.4 GR20");
    public static readonly DiagnosticDescriptor UsageFloatFormatPhraseRepeated = new(
        "COBOLNET1718", "usage-float-format-phrase-repeated", EditionSeverity.Error,
        "ISO §5.2.6.4, Choice indicators: \"When enclosed by brackets, zero or more of the alternatives "
        + "contained within the choice indicators shall be specified, but any single alternative may be "
        + "specified only once.\" — the FLOAT-DECIMAL-16/-34 phrase group of the §13.18.60.2 general format is "
        + "exactly such a bracketed choice-indicator group over { encoding-phrase, endianness-phrase }, so each "
        + "phrase may appear at most once (in either order, per the same clause: \"The alternatives may be "
        + "specified in any order.\"). The standard BINARY float usages carry a single bracketed "
        + "endianness-phrase, which is likewise at most one.",
        "ISO §5.2.6.4 / §13.18.60.2");
    // ⛔ THE EXPRESSION FORMATION TABLES (kb/Work PB158). §8.8.1.2 Table 3 and §8.8.2 Table 4 each state which
    // ordered pairs of adjacent symbols an expression may contain; a '—' cell is an invalid pair. Most cells are
    // excluded structurally by the expression tiers — MEASURED, one probe per cell: ten of Table 3's thirteen are
    // already hard parse errors and two more cannot form at all, because COBOL reads a '(' after an identifier as
    // a subscript. The tiers admit exactly one cell from each table, and this code carries both, because they are
    // one rule ("an invalid adjacent pair") over two tables. The arithmetic cell CANNOT be closed in the grammar:
    // §8.3.3.3.2 rule 2 makes an ADJACENT sign part of the numeric literal, so `- -2` is the permissible
    // (unary, literal) pair while `- - 2` is the invalid (unary, unary) one — and in the default lexer mode both
    // emit MINUS MINUS INTEGERLIT, so only the TOKEN POSITIONS separate them. A tier rejecting both would reject
    // legal source, the worse failure.
    public static readonly DiagnosticDescriptor ExpressionFormationPair = new(
        "COBOLNET1719", "expression-formation-pair", EditionSeverity.Error,
        "ISO §8.8.1.2 Table 3, Combinations of symbols in arithmetic expressions: \"The letter 'P' indicates a "
        + "permissible pair of symbols. The character '—' indicates an invalid pair.\" Row \"Unary + or −\" × "
        + "column \"Unary + or −\" is '—', so a unary operator may not be immediately followed by another unary "
        + "operator. §8.8.2 Table 4 is the boolean counterpart: its B-NOT row × B-NOT column is likewise '—', and "
        + "§8.8.4.11.3's Table 5 NOTE states the same restriction for conditions outright — \"the pair 'NOT NOT' "
        + "is not permissible\". The sign-adjacency carve-out is §8.3.3.3.2 rule 2: a numeric literal is a "
        + "character-string and \"If a sign is used, it shall appear as the leftmost character of the literal\", "
        + "so a sign written against the digits belongs to the literal and forms the PERMISSIBLE (unary, literal) "
        + "pair instead. Table 4's boolean-shift row permits only an identifier or literal after B-SHIFT-L / "
        + "B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC (§8.8.2 rule 5: \"The second operand shall be an integer operand\"), "
        + "so a parenthesized, separately-signed or compound shift count is the same kind of invalid pair.",
        "ISO §8.8.1.2 Table 3 / §8.8.2 Table 4");
    // ⛔ THE LEVEL-NUMBER SETS (kb/Work PB485). §13.18.33.3 states FOUR sets, one per DATA DIVISION section, and
    // they are NOT the same set: 77 is legal in working-storage/local-storage/linkage (SR5) and illegal in a
    // record area (SR2), and the report (SR4) and screen (SR6) arms admit no special level at all. ONE code for
    // the family — the shape COBOLNET1720 and COBOLNET1707 already use — because it is one rule ("a level-number
    // outside the set its section permits") over four sections; the MESSAGE names the section, its permitted set
    // and its syntax-rule number, so the four arms stay distinguishable without four codes. Not edition-gated
    // (the sets are identical in 1985/2002/2014/2023) and not dialect-gated (a level ISO never defined is not a
    // ConstructAvailability.Removed construct, so --permissive has no arm for it).
    public static readonly DiagnosticDescriptor LevelNumberOutOfRange = new(
        "COBOLNET1746", "level-number-out-of-range", EditionSeverity.Error,
        "ISO §13.18.33.1: \"Level numbers 1 through 49 indicate the position of a data item or screen item within "
        + "the hierarchical structure described by a data description entry, a report group description entry, or "
        + "a screen description entry. In addition, level numbers 66, 77, and 88 are used to identify special "
        + "entries.\" §13.18.33.3 then bounds the permitted set PER SECTION: SR2, entries subordinate to an FD or "
        + "SD entry, \"66, 88, or 1 through 49\"; SR4, report group description entries subordinate to an RD "
        + "entry, \"1 through 49\"; SR5, entries in the working-storage, local-storage and linkage sections, "
        + "\"66, 77, 88, or 1 through 49\"; SR6, screen description entries, \"1 through 49\". SR3 (\"A "
        + "level-number in the range of 1 through 9 may be specified as 01 through 09\") is a spelling permission, "
        + "so the screen tests the VALUE and the 01–09 forms pass. §4.2.2 is why this is a COMPILE-time "
        + "diagnostic: an implementation \"shall provide a warning mechanism that optionally may be invoked by the "
        + "user at compile time to indicate violations of the general formats and the explicit syntax rules\". "
        + "The common cause is the MicroFocus/GnuCOBOL level-78 constant, which ISO does not define — the "
        + "conforming spelling is the §13.10 CONSTANT entry, `01 name CONSTANT AS literal.`",
        "ISO §13.18.33.3");
    // The SECOND level-number axis: the entry FORMAT. §13.18.33.3 above bounds the level-number by the SECTION;
    // §13.18.33.4 GR2 and §13.16.3 bound it by the general format the entry is WRITTEN in, and the two are
    // independent — 78 in working-storage is a section violation, `05 R RENAMES A THRU B.` is a format one and its
    // level is perfectly in range. Kept a separate code because the user action differs: 1746 says "this
    // level-number may not appear here at all", 1747 says "this level-number is fine, but the entry under it is
    // not the format it requires". The format axis was the one that reached the EMITTER: a renames body at level
    // 05 produced uncompilable C# rather than any diagnostic (kb/Work PB485).
    public static readonly DiagnosticDescriptor LevelNumberEntryFormat = new(
        "COBOLNET1747", "level-number-entry-format", EditionSeverity.Error,
        "ISO §13.18.33.4 GR2 assigns the special level-numbers to particular general formats and permits them "
        + "nowhere else: (b) \"Level-number 66 is assigned to identify RENAMES entries and may be used only as "
        + "described by the renames format of the data description entry\"; (c) \"Level-number 88 may be used only "
        + "as described by the condition-name format or the validation format of the data description entry\". "
        + "§13.16.2 gives those formats their shapes — format 2 is `66 data-name-1 RENAMES …`, formats 3 and 4 "
        + "are `88 [condition-name] value-clause .` — and §13.16.3 SR1 closes the set for everything else: "
        + "\"Level-number may be 77 or 1 through 49.\" §13.16.3 SR2 adds the one obligation that runs the other "
        + "way: \"The data-name format of the entry-name clause shall be specified if level-number is 77\", which "
        + "SR4 extends to an OMITTED entry-name (\"it is as though the filler format … were specified\"). Formats 2 "
        + "and 3 print their names UNBRACKETED and carry no entry-name clause, so a nameless or FILLER 66 entry and "
        + "a FILLER 88 entry are neither format either; only format 4 brackets its condition-name (kb/Work PB849). This is "
        + "the FORMAT axis; COBOLNET1746 is the SECTION axis, and a level-number can violate either alone.",
        "ISO §13.18.33.4 GR2 / §13.16.3");
    // §13.18.57.3 SR10 governs the whole WRITTEN reference, not just its name (kb/Work PB205): the operand
    // "may be qualified and reference-modified", the ref-mod's positions "shall be integer literals", and the
    // reference "shall be the same as one of the operands of the CONTROL clause" — so CX(4:3) is not CX(1:3),
    // which a name-only comparison could not see.
    public static readonly DiagnosticDescriptor ReportControlTypeOperand = new(
        NotImplemented, "report-control-type-operand", EditionSeverity.Error,
        "A TYPE CH/CF operand is not the same as one of the operands of the CONTROL clause, or its reference "
        + "modification is not written with integer literals.", "ISO §13.18.57.3 SR10/SR11");
    // ⛔ `ReportSourceOperandUnresolved` (`report-source-operand-unresolved`) LIVED HERE AND IS GONE (kb/Work PB1292). A
    // SOURCE identifier-1 that names nothing is the ordinary unresolved-name diagnostic of the one operand binder now,
    // and one that names a report section item which is no counter is `ReportExpressionOperandSection` (§13.18.53.3 SR4).
    public static readonly DiagnosticDescriptor ReportSumAddendUnresolved = new(
        NotImplemented, "report-sum-addend-unresolved", EditionSeverity.Error,
        "A SUM addend does not resolve to a data item outside the report section.", "ISO §13.18.54.3 SR5");
    // §13.18.54.3 SR8 is §13.18.57.3 SR10's twin over the SAME written reference (kb/Work PB205): data-name-3
    // "may be qualified and reference-modified", "shall be an operand of the CONTROL clause of the current
    // report description", and its ref-mod positions "shall be integer literals".
    public static readonly DiagnosticDescriptor ReportResetNotControlOperand = new(
        NotImplemented, "report-reset-not-control-operand", EditionSeverity.Error,
        "A RESET ON operand is not an operand of the CONTROL clause, or its reference modification is not "
        + "written with integer literals.", "ISO §13.18.54.3 SR8");
    // ⛔ ITS OWN CODE, NOT THE 0899 BAND (kb/Work PB429). §8.4.3.15.3 SR3 is a RULE the program broke, and a
    // rule-rejection wearing the "recognized but not implemented" code tells the reader their legal program is
    // unsupported by this compiler when in fact their program is illegal by the standard — indistinguishable,
    // to a reader and to a selector, from the PAGE-COUNTER defect this note actually fixed.
    public static readonly DiagnosticDescriptor ReportLineCounterReceiving = new(
        "COBOLNET2197", "report-line-counter-receiving", EditionSeverity.Error,
        "LINE-COUNTER shall not be referenced as a receiving operand.", "ISO §8.4.3.15.3 SR3");
    // ⛔ `ReportCounterQualifierNotReport` (`report-counter-qualifier-not-report`) AND `ReportCounterNoReport`
    // (`report-counter-no-report`) LIVED HERE AND ARE GONE (kb/Work PB1049, PB1456), and this comment stands where
    // they did so neither is re-added. Both reported a PROGRAM that breaks a qualification rule under the
    // recognized-but-not-implemented code (COBOLNET0899), which tells the reader a legal program is unsupported when
    // it is illegal by the standard. They are ONE rule — which report a LINE-COUNTER / PAGE-COUNTER names — and ONE
    // code, `ReportCounterQualification`, written beside the one resolution of a counter to its report.
    /// <summary>COBOLNET2729 — a LINE-COUNTER or PAGE-COUNTER reference breaks the qualification rules, or names no
    /// report (ISO §8.4.2.2.3 SR9/SR10, §8.4.3.15.3 SR2, §8.4.3.15.1): the qualifier shall name a report description
    /// entry; a counter shall be qualified in the procedure division when the source element has more than one RD;
    /// the counters exist only for a report that is described. In the report section an unqualified counter is
    /// qualified implicitly by the enclosing RD, so it never draws this diagnostic there. The message names the rule.</summary>
    public static readonly DiagnosticDescriptor ReportCounterQualification = new(
        "COBOLNET2729", "report-counter-qualification", EditionSeverity.Error,
        "A LINE-COUNTER or PAGE-COUNTER reference breaks the qualification rules of ISO §8.4.2.2.3 SR9/SR10 — the "
        + "qualifier shall name a report description entry, and a counter shall be qualified in the procedure "
        + "division when the source element has more than one report description entry — or the source element "
        + "describes no report at all (§8.4.3.15.1).", "ISO §8.4.2.2.3 SR9/SR10; §8.4.3.15.3 SR2");
    public static readonly DiagnosticDescriptor ReportGenerateNeedsControl = new(
        NotImplemented, "report-generate-needs-control", EditionSeverity.Error,
        "GENERATE report-name requires a CONTROL clause in the report description.", "ISO §14.9.16.3 SR2");
    public static readonly DiagnosticDescriptor ReportGenerateNotDetail = new(
        NotImplemented, "report-generate-not-detail", EditionSeverity.Error,
        "GENERATE names a report group that is not a DETAIL group.", "ISO §14.9.16.3 SR1");

    /// <summary>COBOLNET1920 — the §8.4.2.2 uniqueness rule as it applies to a REPORT-GROUP reference
    /// (kb/Work PB365). The same 01-level group name may be described in two report description entries of one
    /// source unit; §8.4.2.2.1 then requires the reference to be qualified by a report-name, and §8.4.2.2.3 SR1
    /// makes it a syntax rule. Emitted from the ONE funnel (<c>ReportGroupResolution.Resolve</c>) for both of
    /// its consumers — <c>GENERATE data-name-1</c> (§14.9.16.3 SR1) and <c>USE BEFORE REPORTING identifier-1</c>
    /// (§14.9.49.3 SR9) — which before PB365 each silently bound the FIRST report that carried the name.</summary>
    public static readonly DiagnosticDescriptor ReportGroupReferenceAmbiguous = new(
        "COBOLNET1920", "report-group-reference-ambiguous", EditionSeverity.Error,
        "A report-group reference names a group described in more than one report and is not qualified by a "
        + "report-name.", "ISO §8.4.2.2.1 / §8.4.2.2.3 SR1");

    // ── COBOLNET0899 — object-oriented refinements (deferred) ────────────────────────────────────────
    /// <summary>COBOLNET0859 — the USE Format-4 operand does not name a class or interface the referring
    /// SOURCE ELEMENT may reference (ISO §14.9.49.3 SR16/SR17, scoped by §8.4.6.4). The code predates
    /// kb/Work PB365 and is kept byte-stable (goldens pin it); what changed is the SET the operand is resolved
    /// against — the source element's REPOSITORY, not the whole compilation group — and that BOTH alternatives
    /// of the format's brace group now resolve, where only the class arm used to.</summary>
    public static readonly DiagnosticDescriptor UseExceptionObjectName = new(
        "COBOLNET0859", "use-exception-object-name", EditionSeverity.Error,
        "A USE AFTER EXCEPTION OBJECT operand does not name a class or interface in scope.",
        "ISO §14.9.49.3 SR16/SR17 / §8.4.6.4");

    // ⛔ `oo-factory-object-reference` (COBOLNET0899, "USAGE OBJECT REFERENCE FACTORY OF is recognized but not
    // yet implemented") was DELETED by kb/Work PB496's golden round once kb/Work PB389 made the phrase compile:
    // the whole §13.18.60.2 general format — [FACTORY OF] ACTIVE-CLASS and [FACTORY OF] object-class-name-1
    // [ONLY] — is carried by `ObjectRefDescriptor`, so no site could raise it and `docs/DIAGNOSTICS.md` would
    // have documented a code the compiler cannot produce. COBOLNET0899 itself is the shared
    // recognized-not-implemented CODE and stays: ~40 other descriptors emit it. Only this NAME is retired, and
    // the name is never to be reused for anything else.
    // ⛔ `oo-based-in-class` (COBOLNET0899, "BASED data / ADDRESS OF in a class definition's data division is not
    // yet implemented") was DELETED by kb/Work PB956: the OO type-halves now render the SAME BASED bridges and
    // ADDRESS-OF cells the program class does (OoEmitter.EmitPointerBackings), so no site can raise it. The NAME is
    // retired and never reused.
    // ⛔ `oo-external-method-working-storage` (COBOLNET0899, "EXTERNAL on a method WORKING-STORAGE item is not yet
    // implemented") was DELETED by kb/Work PB1308 (decision R62): a method's WORKING-STORAGE SECTION is illegal at
    // every edition (COBOLNET1519, ISO §13.5.3 SR1), so no item of one can carry EXTERNAL and no site can raise it. The
    // NAME is retired and never reused.
    public static readonly DiagnosticDescriptor OoInterfacePropertyPrototype = new(
        NotImplemented, "oo-interface-property-prototype", EditionSeverity.Error,
        "A GET/SET PROPERTY prototype in an interface is not yet implemented.", "ISO §10.6.2", RecognizedNotImplemented);
    // ⛔ `oo-method-declaratives` is DELETED, not disabled (kb/Work PB1010): ISO §14.2.2 SR10 admits the declaratives
    // format in a method definition, and a method's declaratives now bind and dispatch as its own (the per-method
    // declarative table + the method-local selection machinery) — never reallocate the id.
    // ⛔ `oo-method-raising-last` is DELETED, not disabled (kb/Work PB410): the method arm no longer decides
    // §14.9.18.3 SR5 at all — it asks the same PlacementRules screen the program arm asks, so RAISING LAST in a
    // method's PERFORM WHEN phrase is ACCEPTED and one outside either admitted position is refused by
    // COBOLNET2103, with the same ordinal, whichever arm bound it.
    // ⛔ `oo-group-valued-property` is DELETED, not disabled (kb/Work PB1448): §8.4.3.9.4 GR1-GR3 give the property's
    // temp the accessor's whole data description, and a group description is one — the temp is now built over it,
    // so no site can raise the name. Never reallocate the id.
    // ⛔ `any-length-returning` is DELETED, not disabled (kb/Work PB1167): §13.18.2.3 SR3 b) admits an ANY LENGTH
    // RETURNING item of a contained program or a method, and §13.18.2.4 GR1 b) makes its length n "the length of the
    // corresponding argument or returning item of the activating runtime element" — the activator's receiver length now
    // reaches the activated element (CobolArgAdapt.ReturningSeed for a program, the method's trailing __retLen for an
    // INVOKE), so no site can raise it. Never reallocate the name.

    // ── COBOLNET0899 — inter-program header-formal deferrals (P10 Step 10) ──────────────────────────
    // ⛔ `by-value-formal-carrier` (COBOLNET0899, "a method's BY VALUE formal is not yet implemented on the INVOKE
    // channel") was DELETED by kb/Work PB1051: a method's BY VALUE formal is carried (OoFormal.ByValue — the activator
    // hands a detached value, the method never copies it back, §14.2.3 GR10), so no site can raise it. The NAME is
    // retired and never reused. (The program/function arm's was deleted by kb/Work PB1114.)

    // ── COBOLNET0899 — miscellaneous deferrals ───────────────────────────────────────────────────────
    public static readonly DiagnosticDescriptor ExternalRecordNotCellBacked = new(
        NotImplemented, "external-record-not-cell-backed", EditionSeverity.Error,
        "An EXTERNAL record cannot be cell-backed. ⛔ NO SUCH SHAPE EXISTS TODAY — the EXTERNAL twin of "
        + "COBOLNET1695, kept for the same reason: both route through the ONE allow-list gate "
        + "DataBinder.ByteWindowResidueOf, so a category added to the model with no pinned representation is "
        + "refused by name on BOTH surfaces rather than on one (kb/Work PB231's collapse). A nonconforming "
        + "EXTERNAL pointer/object item is COBOLNET1796's (§13.18.22.3 SR4), not this.",
        "ISO §13.18.22", RecognizedNotImplemented);
    // (RecursiveContainedWs was DELETED by kb/Work PB1133, which implemented the composition it staged: a
    // RECURSIVE program that contains programs now owns its static WORKING-STORAGE / FILE SECTION, and the
    // containees' GLOBAL bridges reach a static member through the container's CLASS name
    // (BoundUnit.AnchorOf). It carried the shared 0899 recognized-not-implemented code, so no number is freed.)
    // (RefModBitGroupSlice was DELETED by kb/Work PB173, which implemented the model it deferred: a bit group's
    // reference modification is a BitImagePlace over the UNPACKED boolean string, so the boolean channel's
    // BOOLEAN positions and the substrate's positions are the same positions — §8.4.3.3.4 GR5a. It carried the
    // shared 0899 recognized-not-implemented code, so no number is freed and none is reallocated. Verified
    // before removal: no `.err` fixture in the corpus expected it, so no green test was pinning the gap open.)
    // (RecursiveWsPointerBacked — `recursive-working-storage-pointer-backed` — was DELETED by kb/Work PB234, which
    // implemented the model it deferred: an ADDRESS-OF-taken record of a RECURSIVE unit's static WS routes its
    // StorageCell onto the static channel (DataBinder.StaticAddressableCells) and __ResetStatics re-seeds it in
    // place; a level-01 REDEFINES of a BASED record rides the based root's static bridge. It carried the shared
    // 0899 recognized-not-implemented code, so no number is freed and none is reallocated. Verified before
    // removal: no `.err` fixture in the corpus expected it.)
    // kb/Work PB206 — the THIRD ARM of §13.18.63.3's VALUE-literal SIZE rule. SR4, SR5 and SR10 are the same
    // sentence pair written once per category ("… shall not exceed the size indicated by an explicit PICTURE
    // clause" for an elementary item; "… shall not exceed the size of the group item" for a group one); the
    // national and boolean arms were implemented (the COBOLNET0898 band) and the ALPHANUMERIC arm had no
    // implementation anywhere, so both of its sentences silently TRUNCATED. Measured on 1d949007:
    // `01 E1 PIC X(2) VALUE "ABCD".` displayed `AB` and `01 GZ VALUE "ABCDEF". 05 O1 PIC X(2). 05 O2 PIC X(2).`
    // displayed `ABCD`, neither with a diagnostic. Not dialect-gated — SR4 carries this sentence at every
    // edition, and Annex E.2 item 27's 2023 change is scoped to NUMERIC-EDITED items (COBOLNET1570), not to this.
    // A FIGURATIVE constant is exempt by rule, not by omission: §8.3.3.6.4 GR2 repeats it to the subject's size
    // and truncates from the right, naming the VALUE clause as the context that specifies the length.
    public static readonly DiagnosticDescriptor ValueLiteralOversize = new(
        "COBOLNET1740", "value-literal-oversize", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 4: \"Alphanumeric literals in the VALUE clause of an elementary item shall "
        + "not exceed the size indicated by an explicit PICTURE clause. Alphanumeric literals in the VALUE clause "
        + "of an alphanumeric group item shall not exceed the size of the group item.\" (The national and boolean "
        + "twins of the same sentence pair — SR5 and SR10 — report in the COBOLNET0898 band.)",
        "ISO §13.18.63.3 SR4");
    public static readonly DiagnosticDescriptor ValueNumericEditedOversize = new(
        "COBOLNET1570", "value-numeric-edited-oversize", EditionSeverity.Error,
        "At COBOL-2023 an alphanumeric edited-image literal in the VALUE clause of a numeric-edited item is checked "
        + "against the PICTURE size (ISO §13.18.63.3 SR4/SR5) — a literal longer than the edited width is rejected "
        + "(before 2023 it was stored truncated). Under --permissive the check is a warning (a removed-capability "
        + "posture); the national/alphanumeric class mismatch is the separate COBOLNET0898 check.",
        "ISO §13.18.63.3 SR4/SR5 / Annex E.2 item 27 (VCR row 34)");
    public static readonly DiagnosticDescriptor DebugSubFacilityStaged = new(
        "COBOLNET1571", "debug-sub-facility-staged", EditionSeverity.Error,
        "The X3.23-1985 USE FOR DEBUGGING ON procedure-name / ALL PROCEDURES trigger leg + the DEBUG-ITEM special "
        + "register are modeled at --std 85; the data-name (incl. ALL REFERENCES OF), file-name, and cd-name subject "
        + "kinds and the SORT/MERGE INPUT/OUTPUT-procedure DEBUG-CONTENTS cause are staged — rejected loud rather "
        + "than compiled with a missing/stale trigger.",
        "VCR Table 7 row 7.17 (X3.23-1985 debug module)");
    public static readonly DiagnosticDescriptor MergeInSortMergeProc = new(
        "COBOLNET1572", "merge-in-sort-merge-proc", EditionSeverity.Error,
        "At COBOL-2023 a MERGE statement is prohibited in the output procedure of another MERGE, or the input or "
        + "output procedure of a file-format SORT (the prior standard allowed it with conflicting rules; SORT "
        + "already disallowed it). A bind-time procedure-range cross-pass rejects it at --std 2023; below 2023 the "
        + "runtime EC-SORT-MERGE-ACTIVE raise (CobolSort, when checking is enabled) is the dynamic net.",
        "ISO §14.9.24 / Annex E.2 item 20 (VCR row 27)");
    public static readonly DiagnosticDescriptor ExceptionFileArgumentNotFile = new(
        "COBOLNET1574", "exception-file-argument-not-file", EditionSeverity.Error,
        "The argument of FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N shall be the name of a file connector specified "
        + "in an FD statement (ISO §15.28.3 rule 1 / §15.29.3) — the given name does not resolve to a declared file.",
        "ISO §15.28.3 rule 1 / §15.29.3 (VCR rows 68/69)");
    public static readonly DiagnosticDescriptor ExternalFileStatusConsistency = new(
        "COBOLNET1573", "external-file-status-consistency", EditionSeverity.Error,
        "At COBOL-2023, for an external file all corresponding file control entries in the run unit shall specify the "
        + "FILE STATUS clause naming the same corresponding external data item (ISO §12.4.5.3 GR1(i); §14.8.4.2; Annex "
        + "E.2 item 12) — a corresponding SELECT omitting FILE STATUS, or naming a non-external / different external "
        + "item, is rejected. Below 2023 the requirement did not exist.",
        "ISO §12.4.5.3 GR1(i) / §14.8.4.2 / Annex E.2 item 12 (VCR row 18)");
    public static readonly DiagnosticDescriptor ExternalRelativeKeyConsistency = new(
        "COBOLNET1575", "external-relative-key-consistency", EditionSeverity.Error,
        "At COBOL-2023, for an external relative file all corresponding file control entries in the run unit shall "
        + "specify the RELATIVE KEY clause naming the same corresponding external data item (ISO §12.4.5.3 GR1(h); "
        + "§14.8.4.2; Annex E.2 item 24) — a corresponding SELECT omitting RELATIVE KEY, or naming a non-external / "
        + "different external item, is rejected. Below 2023 the requirement did not exist.",
        "ISO §12.4.5.3 GR1(h) / §14.8.4.2 / Annex E.2 item 24 (VCR row 31)");
    public static readonly DiagnosticDescriptor ExternalFileItemNotExternal = new(
        "COBOLNET1624", "external-file-item-not-external", EditionSeverity.Error,
        "At COBOL-2023, for an external file connector the FILE STATUS, RELATIVE KEY and LINAGE data items shall "
        + "themselves be external data items (ISO §14.8.4.2; Annex E.2 item 9) — a file/relative-key/linage clause "
        + "naming a non-external item is rejected. Enforced per connector regardless of describer count. Below 2023 "
        + "the requirement did not exist.",
        "ISO §14.8.4.2 / Annex E.2 item 9");
    // 1625 — the fixed-point-numeric VALUE range/sign syntax rules (§13.18.63.3 SR2/SR3; the 08xx value/picture band
    // is fully allocated, so the next-free scan lands this in the 16xx band alongside the 1570 numeric-edited VALUE
    // check). Edition-invariant (present 85/2002/2014/2023).
    public static readonly DiagnosticDescriptor ValueNumericOutOfRange = new(
        "COBOLNET1625", "value-numeric-out-of-range", EditionSeverity.Error,
        "A fixed-point numeric VALUE literal is not a permissible value in the range the PICTURE indicates: it is "
        + "not representable in the subject without truncation of a leading or trailing nonzero digit (SR2), or a "
        + "negative literal seeds an unsigned subject (SR3). A syntax-rule violation, rejected at bind time rather "
        + "than silently mis-stored as an out-of-range native value.",
        "ISO §13.18.63.3 SR2/SR3");
    // 1626 — the character-operand USAGE syntax rules of INSPECT / STRING / UNSTRING (DA7). These constructs were
    // already REJECTED correctly; the defect was the STAGE. Each violation was reported as a run-time
    // NotImplementedCobolFeatureException, so an illegal program compiled clean and then crashed when control
    // reached the statement — where the standard promises a compile-time error. Edition-invariant: all three rules
    // are present unchanged at 85/2002/2014/2023, so this is NOT edition-gated and needs no introduction axis.
    public static readonly DiagnosticDescriptor CharacterOperandUsage = new(
        "COBOLNET1626", "character-operand-usage", EditionSeverity.Error,
        "An INSPECT, STRING or UNSTRING operand that must be a character item is not one. INSPECT identifier-1 "
        + "shall be an alphanumeric/national GROUP item or an ELEMENTARY item of usage display or national "
        + "(§14.9.22.3 SR1 — note the rule admits a group outright and constrains only an elementary operand); "
        + "STRING's identifiers other than the POINTER shall be usage display or national (§14.9.43.3 SR1); and "
        + "UNSTRING's INTO receiver shall be usage display with category alphabetic/alphanumeric/numeric, or usage "
        + "national with category national/numeric (§14.9.48.3 SR4). A binary, packed, float, index or pointer "
        + "ELEMENTARY operand has no character image and is rejected at bind time. ⛔ A GROUP receiver is NOT "
        + "rejected: §14.9.43.4 GR3a transfers into STRING's receiver \"in accordance with the MOVE statement rules "
        + "for alphanumeric-to-alphanumeric moves\", so a group takes whatever an alphanumeric MOVE may deposit, "
        + "including a group holding a BINARY/PACKED leaf (V59 gave those leaves a byte image).",
        "ISO §14.9.22.3 SR1 / §14.9.43.3 SR1 / §14.9.48.3 SR4");
    // 1627 — the ISO §15.3 intrinsic ARGUMENT-CLASS screen (fix-queue PB1). IntrinsicCatalog declared an
    // ArgKinds class code on all 79 rows and IntrinsicSig.ArgKind had ZERO callers, so no §15 argument rule was
    // enforced from the table built for it: FUNCTION REVERSE(<PIC 9(4)>) and FUNCTION ABS(<PIC X(4)>) both
    // compiled clean and produced garbage. Edition-invariant — §15.3's argument types are unchanged across
    // 85/2002/2014/2023 — so this is NOT edition-gated and needs no introduction axis. The --permissive leniency
    // mirrors DA6/COBOLNET0844, which settled the sibling §8.8.1.1 question for ARITHMETIC operands; one
    // mechanism for one question.
    public static readonly DiagnosticDescriptor IntrinsicArgumentClass = new(
        "COBOLNET1627", "intrinsic-argument-class", EditionSeverity.Error,
        "An intrinsic-function argument is not of the class its argument rule requires. ISO §15.3 defines the "
        + "argument types: type 10 Numeric admits \"an arithmetic expression or a numeric data item\", type 6 "
        + "Integer admits an integer-valued arithmetic expression or an integer data item, and types 1/2/9 admit "
        + "the alphabetic/alphanumeric/national family (a strongly-typed group item counting as alphanumeric). "
        + "The MAX/MIN/ORD-MAX/ORD-MIN family instead carries a NEGATIVE rule — §15.71.3 r1 and siblings exclude "
        + "class boolean, message-tag, object and pointer. ⛔ CLASS, not category: §8.5.2.1 Table 2 puts "
        + "NUMERIC-EDITED under class ALPHANUMERIC when its usage is display, so PIC ZZ9.99 is not a legal "
        + "numeric argument however numeric it looks. An operand whose class is not statically decidable is "
        + "never rejected. --permissive downgrades this to a warning and proceeds with the existing coercion.",
        "ISO §15.3 / §8.5.2.1 Table 2 / §4.2.2 para 3 (which leaves the determination to the implementor)");
    // 1628 — CALL … USING BY VALUE operand class (ISO §14.9.4.3 SR22). Surfaced by the pre-merge GnuCOBOL
    // differential: two AGREE_ACCEPT→WE_REJECT flips traced to DA6's §8.8.1.1 arithmetic screen firing on a BY
    // VALUE operand, because the grammar production is named arithmeticExpression and the binder took that at
    // its word. The VERDICT was right — SR22 does exclude an alphanumeric operand — but COBOLNET0844 quoted a
    // rule about arithmetic expressions at a programmer who had broken a CALL rule. Edition-invariant in
    // SUBSTANCE, but BY VALUE itself is a COBOL-2002 introduction, so it is unreachable below --std 2002 and
    // needs no separate introduction axis of its own.
    public static readonly DiagnosticDescriptor CallByValueOperandClass = new(
        "COBOLNET1628", "call-by-value-operand-class", EditionSeverity.Error,
        "A CALL … USING BY VALUE operand is not of a class the standard permits to be passed by value. ISO "
        + "§14.9.4.3 SR22: \"If identifier-4 or its corresponding formal parameter is specified with a BY VALUE "
        + "phrase, identifier-4 shall be of class numeric, object, or pointer.\" An alphanumeric, national or "
        + "boolean operand is therefore rejected — GnuCOBOL accepts one as an extension, assuming BY CONTENT, "
        + "which is a different passing mode and not what the source asked for. --permissive accepts it with a "
        + "warning. ⛔ CLASS, not category (§8.5.2.1 Table 2): a numeric-edited item is class ALPHANUMERIC when "
        + "its usage is display, so it is excluded however numeric it looks.",
        "ISO §14.9.4.3 SR22 / §8.5.2.1 Table 2");
    /// <summary>COBOLNET2741 — an INVOKE … USING BY VALUE identifier-5 (or a bare identifier whose corresponding formal is
    /// BY VALUE) is not of a class the standard permits to be passed by value (kb/Work PB1051). ISO §14.9.23.3 SR15: "If
    /// identifier-5 or its corresponding formal parameter is specified with the BY VALUE phrase, identifier-5 shall be of
    /// class message-tag, numeric, object or pointer." The INVOKE twin of <see cref="CallByValueOperandClass"/>
    /// (§14.9.4.3 SR22); there is no vendor extension to accept here (GnuCOBOL has no object facility), so --permissive
    /// does not relax it. CLASS, not category (§8.5.2.1 Table 2): a numeric-edited DISPLAY item is class alphanumeric.</summary>
    public static readonly DiagnosticDescriptor InvokeByValueOperandClass = new(
        "COBOLNET2741", "invoke-by-value-operand-class", EditionSeverity.Error,
        "An INVOKE … USING BY VALUE operand is not of a class the standard permits to be passed by value. ISO "
        + "§14.9.23.3 SR15: \"If identifier-5 or its corresponding formal parameter is specified with the BY VALUE "
        + "phrase, identifier-5 shall be of class message-tag, numeric, object or pointer.\" An alphanumeric, national "
        + "or boolean operand is therefore rejected. CLASS, not category (§8.5.2.1 Table 2): a numeric-edited item is "
        + "class ALPHANUMERIC when its usage is display, so it is excluded however numeric it looks.",
        "ISO §14.9.23.3 SR15 / §8.5.2.1 Table 2");
    /// <summary>COBOLNET2746 — the procedure division references a LINKAGE SECTION data item that §13.7.3 SR4 does not
    /// make referenceable (kb/Work PB1249). ISO §13.7.3 SR4: "a data item defined in the linkage section of a source
    /// element may be referenced within the procedure division of that source element if, and only if, it satisfies one
    /// of the following conditions: a) It is an operand of the USING phrase or the RETURNING phrase of the procedure
    /// division header. b) It is subordinate to an operand of the USING phrase or the RETURNING phrase of the procedure
    /// division header. c) It is defined with a REDEFINES or RENAMES clause, the object of which satisfies one of the
    /// above conditions. d) It is subordinate to any item that satisfies the condition in subrule c. e) It is a
    /// condition-name or index-name associated with a data item that satisfies one of the above conditions", and "a
    /// based data item may be referenced as described in 13.18.5". An unreferenceable item has no storage association
    /// at all (§14.2.3 GR8 gives a formal the caller's storage), so the compiler used to give it callee-local storage
    /// and let the program read zeros.</summary>
    public static readonly DiagnosticDescriptor LinkageItemNotReferenceable = new(
        "COBOLNET2746", "linkage-item-not-referenceable", EditionSeverity.Error,
        "The procedure division references a LINKAGE SECTION item that is not referenceable. ISO §13.7.3 SR4: a "
        + "linkage item may be referenced in the procedure division \"if, and only if\" it is an operand of the USING "
        + "or RETURNING phrase of the procedure division header, subordinate to such an operand, defined with a "
        + "REDEFINES or RENAMES clause whose object satisfies one of those conditions, subordinate to such a "
        + "redefinition, or a condition-name or index-name associated with such an item — or it is a BASED item "
        + "(§13.18.5). Name the item in USING (or RETURNING), or describe it BASED.",
        "ISO §13.7.3 SR4");
    /// <summary>COBOLNET2747 — a formal parameter of a FUNCTION is used as a receiving operand (kb/Work PB1250). ISO
    /// §13.7.3 SR5: "A formal parameter of a function shall not be used as a receiving operand." The prohibition is
    /// over the operand ROLE, so it is one arm of <c>ExpressionBinder.ReceivingPlaceBar</c> and every statement's
    /// receiver inherits it. The parameter is barred in any reference form (whole, or a reference-modified part); an
    /// item subordinate to a group parameter is not "a formal parameter" (the same clause's SR4 b) says "subordinate to"
    /// when it means it), and the function's RETURNING item stays a legal receiver.</summary>
    public static readonly DiagnosticDescriptor FunctionFormalReceiving = new(
        "COBOLNET2747", "function-formal-receiving", EditionSeverity.Error,
        "A formal parameter of a FUNCTION is written where the statement MODIFIES the operand. ISO §13.7.3 SR5: \"A "
        + "formal parameter of a function shall not be used as a receiving operand.\" The prohibition covers the "
        + "parameter in any reference form (whole, or a reference-modified part) and every statement's receiving "
        + "position (MOVE, arithmetic, STRING INTO, READ INTO, SET, INITIALIZE, a BY REFERENCE argument …). Compute "
        + "into the RETURNING item, or into a WORKING-STORAGE copy of the parameter.",
        "ISO §13.7.3 SR5");
    /// <summary>ISO §14.9.4.3 SR23 — literal-2 shall be a NUMERIC literal when it, or its corresponding
    /// formal parameter, carries the BY VALUE phrase (kb/Work PB238). A SEPARATE code from SR22's
    /// COBOLNET1628 because it is a separate rule about a separate subject: SR22 screens identifier-4's CLASS,
    /// SR23 screens literal-2's KIND, and one code reporting both would name the wrong rule for one of them.
    /// <para>Before this, SR23 was enforced only as a side effect of the expression grammar — the spine bottoms
    /// out at <c>numericLiteral | ZERO_ARITH | functionCall | dataReference | ( … )</c>, so a non-numeric
    /// literal after BY VALUE could not be written at all and the standard's verdict was delivered as a raw
    /// ANTLR "no viable alternative" naming no rule; and the rule's SECOND subject (a keyword-less literal-2
    /// whose FORMAL carries the phrase) had no reachable arm at all.</para></summary>
    public static readonly DiagnosticDescriptor CallByValueLiteralKind = new(
        "COBOLNET1762", "call-by-value-literal-kind", EditionSeverity.Error,
        "A CALL … USING literal argument is passed BY VALUE but is not a numeric literal. ISO §14.9.4.3 SR23: "
        + "\"If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase, "
        + "literal-2 shall be a numeric literal.\" The phrase may be WRITTEN on the argument or DERIVED from "
        + "the corresponding formal parameter through §14.9.4.4 GR9 b), and both spellings are screened. The "
        + "figurative ZERO is admitted: §8.3.3.6.3 SR1a makes it a numeric literal wherever a literal is "
        + "restricted to numeric. A constant-name argument is screened on the literal it SUBSTITUTES "
        + "(§13.10.4 GR1 — \"as if literal-1 … were written where constant-name-1 is written\" — and GR2 for "
        + "its class and category), not on the name. Edition-invariant in substance; BY VALUE is a COBOL-2002 "
        + "introduction, so it is unreachable below --std 2002 and needs no introduction axis of its own.",
        "ISO §14.9.4.3 SR23 / §8.3.3.6.3 SR1a / §13.10.4 GR1");
    // 1629 — reference-modifying a FUNCTION result whose function is not alphanumeric/boolean/national (ISO
    // §8.4.3.3.3 SR2). Opened by fix-queue PB8, which made the shape PARSE for the first time: before it, every
    // ref-modified function-identifier died at COBOL0001 and the class question could not even be asked. The
    // rule is edition-invariant — reference modification of a function-identifier is in the 1989 intrinsic
    // amendment's model and unchanged since — so no introduction axis of its own.
    public static readonly DiagnosticDescriptor RefModFunctionResultClass = new(
        "COBOLNET1629", "ref-mod-function-result-class", EditionSeverity.Error,
        "Reference modification is applied to the result of a function that is not of a class the standard "
        + "permits to be reference-modified. ISO §8.4.3.3.3 SR2: \"If identifier-1 is a function-identifier, it "
        + "shall reference an alphanumeric, boolean, or national function.\" A NUMERIC or INTEGER function "
        + "(FUNCTION PI, FUNCTION MAX over numerics, FUNCTION LENGTH …) is therefore rejected: §15.2 gives it a "
        + "numeric temporary, and §8.4.3.3.4 GR1 has no character positions to number in one. ⛔ This is the "
        + "FUNCTION's declared type (§15.2), not the shape of its arguments — FUNCTION MAX over alphanumeric "
        + "arguments IS an alphanumeric function and is legal here. --permissive does NOT relax it: unlike a "
        + "removed-construct leniency there is no defined value to fall back on.",
        "ISO §8.4.3.3.3 SR2 / §15.2");
    // 1630 — reference-modifying a reference modification (ISO §8.4.3.3.3 SR3). Found by the PB8 sibling sweep
    // (CLAUDE.md rule 4), NOT by PB8's own repro: the grammar's `dataReferenceSuffix*` admits unlimited
    // refModParts, and ReferenceResolver kept only the FIRST of each carrier via `??=` while the DEFAULT-mode
    // form outranked the SUBSCRIPT-mode one — so `MOVE A (3:4)(2:2)` compiled clean and silently returned
    // A(2:2), neither the composition nor the rejection the standard requires. Closes the traceability row
    // SR-8.4.3.3.3-3, which stood at state GAP with an empty code-location.
    public static readonly DiagnosticDescriptor RefModOfRefMod = new(
        "COBOLNET1630", "ref-mod-of-ref-mod", EditionSeverity.Error,
        "A reference modification is applied to something that is already reference-modified. ISO §8.4.3.3.3 "
        + "SR3: \"Identifier-1 shall not be a reference-modification format identifier.\" §8.4.3.3.4 GR5 numbers "
        + "positions within the item identifier-1 references, and a ref-mod result is a NEW unique data item "
        + "(GR5), so a second modifier has no defined base to count from. Write the composed positions directly: "
        + "A (3:4)(2:2) is A (4:2). ⛔ A SUBSCRIPT followed by a reference modification — T(I) (2:3) — is a "
        + "different and entirely legal shape (§8.4.3.1.4 GR1 a→g) and is not affected; only a SECOND reference "
        + "modification is rejected.",
        "ISO §8.4.3.3.3 SR3 / §8.4.3.3.4 GR5");
    // 1631 — a FORMATTED-*/INTEGER-OF-FORMATTED-DATE/… format argument that is not one of the §15.3.1–§15.3.3
    // formats, or is the wrong KIND for the function (fix-queue PB11). Before this, the format was validated
    // character-wise only, so any string assembled from legal subfields was accepted and the function
    // FABRICATED a value — `FORMATTED-DATE("hhmmss" …)` returned "000000". Edition-invariant in substance; the
    // functions themselves are 2014+ (§15.39–§15.41), so it is unreachable below that.
    public static readonly DiagnosticDescriptor DateTimeFormatKindMismatch = new(
        "COBOLNET1631", "date-time-format-kind", EditionSeverity.Error,
        "A date/time FORMAT argument is not a format the standard defines, or is the wrong KIND for the "
        + "function. ISO §15.3.1.1 fixes SIX date formats (basic and extended, for calendar, ordinal and week "
        + "dates), §15.3.2 twelve time formats (four common-time shapes × local / UTC / offset), and §15.3.3.7 "
        + "makes a combined format a date format, an uppercase T, and a time format. §15.39.3 r2 requires a "
        + "DATE format, §15.41.3 r2 a TIME format and §15.40.3 r2 a COMBINED one. ⛔ BASIC AND EXTENDED NEVER "
        + "MIX: `YYYY-MMDD` and `YYYY-MM-DDThhmmss` are built entirely from legal subfields and are still not "
        + "formats, which is why membership is tested rather than each field in isolation.",
        "ISO §15.3.1.1 / §15.3.2 / §15.3.3.7 / §15.39.3 r2 / §15.40.3 r2 / §15.41.3 r2");
    // PB10's INSPECT half. Deliberately NOT an INSPECT-specific code: §8.4.3.2.3 SR1 is ONE rule about
    // function-identifiers in RECEIVING positions, and PB10's remaining positions plus PB17 want the same
    // verdict. Naming it after the rule rather than the statement is what stops the next site minting a second
    // code for the same sentence (feedback_one_rule_one_place).
    public static readonly DiagnosticDescriptor FunctionIdentifierReceiving = new(
        "COBOLNET1632", "function-identifier-receiving", EditionSeverity.Error,
        "A function-identifier is written where the statement MODIFIES the operand. ISO §8.4.3.2.3 SR1: \"A "
        + "function-identifier shall not be specified as a receiving operand.\" A function returns a temporary "
        + "value (§15.4), so there is nothing for the statement to store into. ⛔ THIS IS POSITION-SPECIFIC, NOT "
        + "STATEMENT-SPECIFIC: §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, so every "
        + "identifier-N SENDING position admits one — INSPECT identifier-1 is legal in Format 1 (TALLYING), "
        + "where §14.9.22.4 GR1 treats it as sending, and barred in Formats 2/3/4 (REPLACING / "
        + "TALLYING-and-REPLACING / CONVERTING), where GR7 replaces its characters and GR20 makes format 4 "
        + "execute as a format 2 over the same identifier-1. Move the function result into a data item first.",
        "ISO §8.4.3.2.3 SR1 (with §14.9.22.4 GR1/GR7/GR20 for INSPECT's per-format split)");
    // PB11's VALUE half. Decidable at BIND time and therefore a diagnostic rather than a run-time check:
    // §15.40.3 r1 / §15.41.3 r1 make argument-1 a LITERAL, so the format's zone is known at compile time, and the
    // offset argument's PRESENCE is syntactic. Before this the argument was accepted and silently DISCARDED.
    public static readonly DiagnosticDescriptor DateTimeOffsetArgumentNotPermitted = new(
        "COBOLNET1633", "datetime-offset-argument-not-permitted", EditionSeverity.Error,
        "An offset-from-UTC argument is supplied for a format whose time portion is a LOCAL time — it carries no "
        + "place to put an offset. ISO §15.41.3 r5: \"Argument-3 shall not be specified if the time portion of "
        + "the format in argument-1 is neither a UTC format nor an offset format\"; §15.40.3 r6 says the same of "
        + "FORMATTED-DATETIME's argument-4. A UTC format ends in 'Z' and an offset format carries an explicit "
        + "'+hhmm' / '+hh:mm' subformat (§15.3.3.4–§15.3.3.6); a plain 'hhmmss' / 'hh:mm:ss' is local. ⚠ The "
        + "converse is NOT an error: omitting the argument for a UTC or offset format is explicitly legal and "
        + "evaluates as though 0 were specified (§15.40.3 r7 / §15.41.3 r6).",
        "ISO §15.40.3 r6 / §15.41.3 r5 (zone per §15.3.3.4–§15.3.3.6)");
    // PB47. A SYNTAX RULE violation, so a compile-time diagnostic — and that is the whole point of the entry:
    // an invalid pairing used to compile clean and reach a RUN-TIME NotImplementedCobolFeatureException reading
    // "a COBOL feature that is not yet implemented", which is doubly wrong (it IS implemented; the source is
    // inadmissible) and is coverage-shaped, since a WHEN branch that never executes never reported at all.
    public static readonly DiagnosticDescriptor EvaluateOperandCombinationInvalid = new(
        "COBOLNET1634", "evaluate-operand-combination-invalid", EditionSeverity.Error,
        "This EVALUATE selection subject and selection object may not be paired. ISO §14.9.13.3 syntax rule 10: "
        + "\"The permissible combinations of selection subject and selection object operands are indicated in "
        + "Table 15, Combination of operands in the EVALUATE statement\", and the cell for this pairing is blank "
        + "— \"a space indicates an invalid combination\". Most often this is a value object written against a "
        + "TRUE/FALSE subject, whose only permissible objects are a condition, TRUE/FALSE, or ANY.",
        "ISO §14.9.13.3 SR10 (Table 15)");
    // R03. A SYNTAX RULE violation that used to produce a SILENT WRONG ANSWER rather than any diagnostic: the hex
    // decoders answer "" for a malformed digit count and every caller took that as the literal's value, so
    // `FUNCTION LENGTH(X"414")` said 1. Three formats state the grouping rule and they differ — §8.3.3.2.3 r6
    // (alphanumeric, pairs), §8.3.3.5.3 r5 (national, four for the D-N1 UTF-16 unit) — while §8.3.3.4.3 r3
    // states NO grouping rule for BX at all, because §8.3.3.4.4 GR5 maps each digit on its own. The check is
    // therefore per-format, and BX is deliberately unscreened.
    public static readonly DiagnosticDescriptor HexLiteralDigitGrouping = new(
        "COBOLNET1635", "hex-literal-digit-grouping", EditionSeverity.Error,
        "A hexadecimal literal's digits do not form whole characters. ISO §8.3.3.2.3 rule 6 (X\"…\") and "
        + "§8.3.3.5.3 rule 5 (NX\"…\") each require every hexadecimal character sequence to consist of the number "
        + "of digits that map to one character — two for an alphanumeric character, four for a national one. A "
        + "hexadecimal-BOOLEAN literal (BX\"…\") has no such rule: §8.3.3.4.4 GR5 maps each digit independently "
        + "to four boolean characters, so any digit count is well formed there.",
        "ISO §8.3.3.2.3 r6 / §8.3.3.5.3 r5");
    // kb/Work R16 (ledger F11). A SYNTAX RULE violation that used to compile clean and abort at RUN TIME
    // ("computed expression in a string context") — DISPLAY IX, MOVE IX TO an alphanumeric, STRING IX — or,
    // worse, silently compute (MOVE IX TO a numeric item), against the SAME judgment the W2 review already
    // landed for class-index DATA ITEMS (COBOLNET0809 rejects a MOVE of one at every edition). §13.18.38.3 r7
    // is a closed LIST, so the diagnostic names it and the legal spelling.
    public static readonly DiagnosticDescriptor IndexNameContext = new(
        "COBOLNET1637", "index-name-context", EditionSeverity.Error,
        "An index-name is written where the statement needs an identifier. An index-name is not an identifier "
        + "(ISO §8.4.3.1.2 defines the identifier formats and an index-name is none of them), and "
        + "§13.18.38.3 r7 closes the list of contexts that may reference one: a subscript, the VARYING phrase "
        + "of PERFORM or SEARCH, the SET statement, and a relation-condition operand. To use the index's "
        + "occurrence number elsewhere, SET a data item to it first (SET data-item TO index-name, §14.9.39) "
        + "and reference the data item.",
        "ISO §13.18.38.3 r7 / §8.4.3.1.2");
    // kb/Work R19 (ledger F18). The OMITTED arm one line above it reports (COBOLNET1544) while this arm
    // returned a silent BoundOperandError — the program compiled with zero diagnostics and threw
    // NotImplementedCobolFeatureException at run time. §4.2.2 ¶3 obliges the warning mechanism to indicate a
    // syntactically-distinguishable violation, and a reserved phrase word in an argument slot of a function
    // that takes no phrase is exactly that. Generic to every catalogued function — reported at the ONE arm,
    // never per function.
    public static readonly DiagnosticDescriptor IntrinsicArgumentNotAValue = new(
        "COBOLNET1638", "intrinsic-argument-not-a-value", EditionSeverity.Error,
        "An intrinsic-function argument position holds a reserved phrase word (LEADING, TRAILING, LAST, "
        + "FIRST, ANY, START, AFTER, ALPHANUMERIC, NATIONAL) or a literal form the position does not admit. "
        + "The phrase words belong to the few functions whose §15.x.2 general format names them (FIND-STRING, "
        + "SUBSTITUTE, CONVERT, TRIM, MODULE-NAME); every other function's format admits only its arguments — "
        + "§15.3's argument types are identifiers, literals, and expressions, never a bare reserved word.",
        "ISO §15.3 / §4.2.2 ¶3 (the per-function §15.x.2 general format decides)");
    // kb/Work R30. Found while probing R22's name-collision control: MOVE NO-SUCH-NAME TO X and DISPLAY
    // NO-SUCH-NAME compiled with zero diagnostics, exit 0, and threw NotImplementedCobolFeatureException at
    // run time — in EVERY reference position measured (sender, receiver, arithmetic operand, condition,
    // STRING, CALL USING, subscript, PERFORM UNTIL). ReferenceResolver.Resolve staged the unresolved
    // fallthrough as a runtime loud; §4.2.2 ¶3 obliges the indication, and a typo is never a feature gap.
    // Reported at the ONE chokepoint (Resolve's undefined arm, deduped per source reference); the
    // type-discriminating probe sites with a legal alternative on failure (the SET format sniffs, INVOKE's
    // class-name receiver, EXCEPTION-OBJECT, the boolean/float reroutes) read the silent Probe form, so a
    // legal alternative reading never draws it. Unsupported-SHAPE nulls of a name that DID resolve keep the
    // documented references-then-fail-loud staging — that debt is a different register entry than a typo.
    // kb/Work R34. The differential's syn_copy:630 ("COPY: recursive replacement") exposed the shape: GnuCOBOL
    // extends COPY REPLACING to reach nested-COPY text; ISO forbids the COMBINATION outright — §7.2.3.4 GR10
    // "If the REPLACING phrase is specified, the library text shall not contain a COPY statement" (GR12 permits
    // nesting, ≥5 levels, only WITHOUT replacing). Our preprocessor recursed OUTSIDE the replacement scope, so
    // the illegal source produced arbitrary partial text and a misleading downstream COBOLNET1639 on whatever
    // name failed to materialize. The verdict (reject) was right by accident; this descriptor makes the REASON
    // right. Emitted by CopyProcessor.ResolveOneCopy at the outer COPY's line. The GnuCOBOL replacement-reaches-
    // nested-text semantics remain a vendor-dialect-axis candidate, adjudicated separately.
    // kb/Work R39 (found by R36's adjudication probes): `REPLACE LEADING "PREFIX-" BY SPACES` — the GCOS/ACU
    // vendor spelling — was silently HALF-PARSED: no diagnostic on the statement, nothing applied, and the
    // failure surfaced downstream as COBOLNET1639 on the never-replaced name. REPLACE's operands were NEVER
    // literals in any ISO edition (unlike COPY's, which 2023 removed — COBOLNET0902's territory): the
    // §7.2.4.2 general format admits pseudo-text/partial-word operands only, and §7.2.4.3 SR7 bars literals
    // as partial-words explicitly. Emitted by CopyProcessor.ApplyReplaceStatements via the same
    // NoteNonPseudoText hook the COPY gate rides — one detector, two rules, each cited at its own site.
    // §12.3.3 SR1 — a contained program has NO configuration section of its own: the container's applies to it
    // (§12.3.4 GR1), which is why DataBinder.InheritConfiguration copies the whole configuration-derived state
    // into every containee before it binds (kb/Work PB60 / AR-15.67.3-5 — a contained program under
    // DECIMAL-POINT IS COMMA parsed NUMVAL("123,45") as 0 and NUMVAL("123.45") as 123.45).
    // §15.68.3 r3 — NUMVAL-C / TEST-NUMVAL-C without argument-2 in a unit whose SPECIAL-NAMES paragraph specifies
    // two or more distinct currency strings (kb/Work PB60 / AR-15.68.3-3: the former single-symbol model
    // injected whichever clause bound last, silently).
    // (The former SubstituteAllSubscript stage — SUBSTITUTE with a table(ALL) argument — landed as the run-time
    // pairing CobolIntrinsics.SubstituteFlat, kb/Work PB81, 2026-08-18.)
    // §15.3 — the ALL subscript in an intrinsic argument (kb/Work PB62): admissible only "when the definition of a
    // function permits an argument to be repeated a variable number of times"; the former bind-time expansion ran
    // for every function and let `FUNCTION MOD(E(ALL) B)` bind over a one-occurrence table.
    // §14.9.28.3 SR2 — the PERFORM … TIMES count "shall be an integer" (kb/Work PB86): a non-integer data item was
    // accepted and its UNSCALED digits iterated (PIC 9V9 VALUE 1.2 → 12 times); a non-integer function was a
    // parse error in one spelling and ran once in the other.
    // §8.4.3.3.3 SR1 — the identifier-1 a reference modification may name (kb/Work PB70): every excluded shape
    // (a strongly-typed / variable-length group, a numeric item of a non-DISPLAY usage, an edited or numeric item
    // subordinate to a strongly-typed group, an index / pointer / object reference) used to fall to a run-time
    // NotImplemented — and a receiving one to a silent drop.
    // §8.3.3.6.3 SR2 — the ALL figurative's literal-1 "shall be neither a figurative constant nor a zero-length
    // literal" (kb/Work PB71): `MOVE ALL "" TO X` compiled and stored spaces.
    public static readonly DiagnosticDescriptor AllLiteralZeroLength = new(
        "COBOLNET1648", "all-literal-zero-length", EditionSeverity.Error,
        "The literal-1 of the figurative constant ALL literal-1 is a zero-length literal. ISO §8.3.3.6.3 SR2: "
        + "\"Literal-1 shall be an alphanumeric, boolean, or national literal … The literal shall be neither a "
        + "figurative constant nor a zero-length literal.\" Write at least one character.", "ISO §8.3.3.6.3 SR2");
    // §8.3.3.6.3 SR2's first sentence — literal-1 "shall be an alphanumeric, boolean, or national literal" (kb/Work
    // PB1627). A quoted literal-1 is one of the three by construction of the grammar; the one spelling that can be of
    // another class is a constant-name (§13.10.3 SR2 lets it stand for a literal of its OWN class), so `ALL K` with K
    // a numeric constant is refused by this rule rather than read as a symbolic-character.
    // §8.5.1.12.1 over a RELATION (kb/Work PB1467): a variable-length group "may not undergo a comparison … unless the
    // other operand is a compatible group". The MOVE twin is §14.9.25.3 SR9's COBOLNET1931; both ask the one
    // VariableLengthCompatibility.PairRefusal. Before this an incompatible pair compiled clean and aborted the run
    // unit on a whole-group image such a group does not have.
    public static readonly DiagnosticDescriptor VariableLengthGroupComparison = new(
        "COBOLNET2492", "variable-length-group-comparison", EditionSeverity.Error,
        "A relation compares a variable-length group (a group with a dynamic-length elementary item or a "
        + "dynamic-capacity table subordinate to it) with an operand that is not a compatible group. ISO §8.5.1.12.1: "
        + "\"a variable-length group is not equivalent to an alphanumeric data item and may not undergo a comparison "
        + "or a move operation, in either direction, explicitly or otherwise, unless the other operand is a compatible "
        + "group.\" Compare it with a group whose dynamic-length items and dynamic-capacity tables correspond "
        + "(§8.5.1.12.2) and match (§8.5.1.12.3), or compare its elementary items.", "ISO §8.5.1.12.1");
    // §8.8.4.2.1's CLOSED "Comparisons are defined for the following:" list over the general relation (kb/Work
    // PB1468), asked of the ONE table RelationComparability at the ONE relation checkpoint. Before it, a COMP /
    // packed / float / non-integer / arithmetic-expression operand against an alphanumeric one compiled clean and ran
    // as an undocumented character comparison, and an index-name against an alphanumeric operand compiled clean and
    // aborted the run unit ("computed expression in a string context").
    public static readonly DiagnosticDescriptor RelationNumericNotInteger = new(
        "COBOLNET2532", "relation-numeric-not-integer", EditionSeverity.Error,
        "A relation compares a numeric operand with an operand of class alphanumeric, alphabetic or national, and the "
        + "numeric operand is not an integer literal or an integer numeric data item of usage display or national. "
        + "ISO §8.8.4.2.1 defines that pair only as item 6, \"Two operands where one is a numeric integer and the other "
        + "is class alphanumeric or national\", and §8.8.4.2.5: \"The numeric integer operand shall be an integer "
        + "literal or an integer numeric data item of usage display or national.\" A non-integer, COMPUTATIONAL, "
        + "packed-decimal or floating-point operand, an arithmetic expression, or a NUMERIC intrinsic function has no "
        + "defined comparison with a character operand. MOVE the value to a data item of the character operand's class "
        + "first, or compare it with a numeric operand. --permissive accepts it with a warning as the character "
        + "comparison of the value's text.", "ISO §8.8.4.2.5");
    public static readonly DiagnosticDescriptor RelationIndexPair = new(
        "COBOLNET2533", "relation-index-pair", EditionSeverity.Error,
        "A relation compares an index-name or an index data item with an operand ISO §8.8.4.2.13 does not pair it "
        + "with. \"Relation tests may be made only between 1) two index-names … 2) an index-name and a numeric data "
        + "item or numeric literal … 3) an index data item and an index-name or another index data item.\" An "
        + "index-name compared with an alphanumeric operand or an arithmetic expression, and an index data item "
        + "compared with a numeric literal, a numeric data item or a figurative constant, are none of these. SET a "
        + "numeric data item from the index first and compare that.", "ISO §8.8.4.2.13");
    // kb/Work PB1427 — the ONE §8.4.3.10.3 SR1 context screen for the predefined NULL. It replaced the per-statement
    // NULL arms (STOP/GOBACK status 1704, DISPLAY 1694, MOVE 0809 …) that each re-stated the rule for one verb while
    // every verb nobody had written an arm for ran NULL as LOW-VALUE (STRING, INSPECT, UNSTRING, FUNCTION LENGTH).
    // Reported only through CobolNet.Binding.PredefinedNullRule (PredefinedNullContextDriftTests).
    public static readonly DiagnosticDescriptor PredefinedNullContext = new(
        "COBOLNET2576", "predefined-null-context", EditionSeverity.Error,
        "NULL is written where ISO §8.4.3.10.3 syntax rule 1 does not admit it. NULL is an identifier — the "
        + "predefined address of class pointer (§8.4.3.10.1), the predefined content of class message-tag, or the "
        + "null object reference of class object (§8.4.3.7.3 SR2) — never a literal or a figurative constant, and "
        + "\"it may be used only as a sending operand in an INITIALIZE or a SET statement; as an argument in a "
        + "program-prototype format CALL statement, a function-prototype format function activation, or a method "
        + "invocation; or in a pointer-or-object-reference relation condition\" (SR1 a; SR1 b adds the message-tag "
        + "relation condition). A MOVE, DISPLAY, STRING, UNSTRING or INSPECT operand, an arithmetic operand, an "
        + "intrinsic-function argument, a VALUE clause operand, a constant entry's AS operand, an externalized name and "
        + "a termination status are none of these. SET a pointer or "
        + "object-reference data item TO NULL and use that item instead.", "ISO §8.4.3.10.3 SR1");
    // kb/Work PB1171 — the report-file reference restriction, asked where a procedure-division file-name resolves
    // (StatementValidation.ResolveFile / ScreenReportFileReference) and at WRITE / REWRITE of such a file's record.
    public static readonly DiagnosticDescriptor ReportFileReference = new(
        "COBOLNET2577", "report-file-reference", EditionSeverity.Error,
        "A statement references a REPORT FILE — a file whose file description entry specifies a REPORT clause — "
        + "where the standard forbids it. \"The subject of a file description entry that specifies a REPORT clause "
        + "may be referenced in the procedure division only by the USE statement, the WHEN phrase of a PERFORM "
        + "statement, the CLOSE statement, or the OPEN statement with the OUTPUT or EXTEND phrase\" (ISO §13.4.5.3 "
        + "SR9; §13.18.46.3 SR3). The statements restate it for their own operands: SORT §14.9.40.3 SR8 and MERGE "
        + "§14.9.24.3 SR9 (a USING or GIVING file \"not for a report file\"), WRITE §14.9.51.3 SR12 and REWRITE "
        + "§14.9.35.3 SR11. A report file is written only by INITIATE, GENERATE and TERMINATE; use an ordinary "
        + "sequential file for any other input-output.", "ISO §13.4.5.3 SR9");
    public static readonly DiagnosticDescriptor ReportFileRecordEntry = new(
        "COBOLNET2578", "report-file-record-entry", EditionSeverity.Error,
        "A file description entry that specifies a REPORT clause has record description entries or constant "
        + "entries subordinate to it: \"Format 3 is the file description entry for a report file. No record "
        + "description entries or constant entries shall be associated with the file description entry for a report "
        + "file.\" (ISO §13.4.5.3 SR8). The report file's lines are composed by the report writer from the report "
        + "description entries of the REPORT SECTION; remove the entries, or describe a separate file for records.",
        "ISO §13.4.5.3 SR8");
    public static readonly DiagnosticDescriptor FileDescriptionClauseFormat = new(
        "COBOLNET2604", "file-description-clause-format", EditionSeverity.Error,
        "A clause is written in a file description entry whose general format does not contain it. ISO §13.4.5.2 "
        + "prints three closed formats and §13.4.5.3 binds each entry to exactly one of them: \"Format 1 is the file "
        + "description entry for a sequential file\" (SR5), \"Format 2 is the file description entry for a relative "
        + "file or an indexed file\" (SR7), \"Format 3 is the file description entry for a report file\" (SR8). "
        + "Format 1 admits IS EXTERNAL, IS GLOBAL, FORMAT, BLOCK CONTAINS, the RECORD clause, the LINAGE clause and "
        + "CODE-SET; Format 2 only IS EXTERNAL, IS GLOBAL, BLOCK CONTAINS and the RECORD clause; Format 3 IS EXTERNAL, "
        + "IS GLOBAL, BLOCK CONTAINS, the RECORD clause, CODE-SET and the REPORT clause. The ORGANIZATION, ACCESS "
        + "MODE, RECORD KEY, ALTERNATE RECORD KEY and FILE STATUS clauses are clauses of the file control entry "
        + "(§12.4.5.1) and belong in the SELECT entry, never in the FD.", "ISO §13.4.5.2; §13.4.5.3 SR5/SR7/SR8");
    public static readonly DiagnosticDescriptor LineSequentialBlockOrRecordContains = new(
        "COBOLNET2605", "line-sequential-block-record-contains", EditionSeverity.Error,
        "The file description entry of a LINE SEQUENTIAL file specifies a BLOCK CONTAINS clause or a RECORD CONTAINS "
        + "clause. ISO §13.4.5.3 SR4: \"If the LINE SEQUENTIAL phrase of the ORGANIZATION clause of the sequential "
        + "format of the 12.4.5, File control entry is specified neither the BLOCK CONTAINS clause nor the RECORD "
        + "CONTAINS clause shall be specified.\" A line sequential record's length is set by its line delimiter "
        + "(§9.1.7.2). The variable-length RECORD IS VARYING clause is not a RECORD CONTAINS clause and remains "
        + "available (§14.9.51.4 GR22 describes a line sequential file whose RECORD clause has the DEPENDING phrase).",
        "ISO §13.4.5.3 SR4");
    public static readonly DiagnosticDescriptor AllLiteralClass = new(
        "COBOLNET2491", "all-literal-class", EditionSeverity.Error,
        "The literal-1 of the figurative constant ALL literal-1 is a constant-name that stands for a numeric literal. "
        + "ISO §8.3.3.6.3 SR2: \"Literal-1 shall be an alphanumeric, boolean, or national literal, any of which may be "
        + "a concatenation expression.\"", "ISO §8.3.3.6.3 SR2");
    // §8.3.2.1 rule 5 — an intrinsic-function-name "identified in a function-specifier in the REPOSITORY paragraph"
    // shall not be used as a user-defined word (kb/Work PB65 FMT-15.43.2 / FMT-15.58.2): under `REPOSITORY. FUNCTION
    // HIGHEST-ALGEBRAIC INTRINSIC.` a table named HIGHEST-ALGEBRAIC compiled clean and `HIGHEST-ALGEBRAIC(A1)`
    // silently read the table element where §15.43.4 requires +999.
    public static readonly DiagnosticDescriptor RepositoryIntrinsicNameAsUserWord = new(
        "COBOLNET1649", "repository-intrinsic-name-as-user-word", EditionSeverity.Error,
        "A user-defined word (a data-name, condition-name, index-name, file-name, paragraph or section name) spells "
        + "an intrinsic-function-name that the REPOSITORY paragraph identifies in a function-specifier — FUNCTION "
        + "name INTRINSIC, or FUNCTION ALL INTRINSIC. ISO §8.3.2.1 rule 5: intrinsic-function-names may be used as "
        + "user-defined words except for LENGTH, RANDOM, SIGN, SUM and \"intrinsic function names identified in a "
        + "function-specifier in the REPOSITORY paragraph\" — that identification is what lets a reference omit the "
        + "word FUNCTION (§8.4.3.2.3 SR2), so the same word cannot also name a data item. Rename the item, or take "
        + "the function out of the REPOSITORY and write FUNCTION name(…) at each reference.", "ISO §8.3.2.1 rule 5");
    // ⛔ COBOLNET1650 ("leap-second-directive-syntax") is RETIRED — NEVER REALLOCATE IT. It was >>LEAP-SECOND's own
    // PLACEMENT code (§7.3.17.3 SR1 "shall not be specified within a compilation unit"; kb/Work PB65) after kb/Work
    // PB794 had taken its operand half onto COBOLNET1911. kb/Work PB1378 found the rule written for LEAP-SECOND
    // alone (and wrongly — it latched at the first unit and rejected a directive BETWEEN two sibling units) while
    // PROPAGATE's twin SR1 was unchecked, so both directives' placement is now DATA on their constructs.json rows
    // (directivePlacement) judged by ONE pass — COBOLNET2652 (DirectivePlacementViolation) below.
    // §14.9.43.3 / §14.9.48.3 — the STRING and UNSTRING operand rules that are not about USAGE (those are
    // COBOLNET1626): a reference-modified, edited, JUSTIFIED or strongly-typed STRING receiver (SR4/SR5/SR6), a
    // POINTER / COUNT IN / TALLYING item that is not an integer without P (STRING SR7, UNSTRING SR5/SR6), an
    // UNSTRING sender that is not category alphanumeric or national (SR2), DELIMITER IN / COUNT IN without
    // DELIMITED BY (UNSTRING SR7), a variable-length group operand (STRING SR11 / UNSTRING SR10). kb/Work PB88:
    // each was a `BoundUnsupported` — the program compiled clean and died at the statement.
    public static readonly DiagnosticDescriptor StringUnstringOperandRule = new(
        "COBOLNET1651", "string-unstring-operand-rule", EditionSeverity.Error,
        "A STRING or UNSTRING operand violates one of the statement's syntax rules: STRING's INTO receiver shall not "
        + "be reference-modified (§14.9.43.3 SR4), edited or JUSTIFIED (SR5), a strongly-typed group (SR6) or a "
        + "variable-length group (SR11), and its POINTER shall be an elementary integer without P (SR7); UNSTRING's "
        + "sender shall be category alphanumeric or national (§14.9.48.3 SR2), DELIMITER IN / COUNT IN need a "
        + "DELIMITED BY phrase (SR7), COUNT IN / TALLYING IN / POINTER items are integers without P (SR5/SR6), and no "
        + "operand may be a variable-length group (SR10). Rejected at bind — the statement is not run.",
        "ISO §14.9.43.3 / §14.9.48.3");
    // kb/Work PB78: the OBJECT-COMPUTER paragraph's clauses (§12.3.6.2's bracketed choice indicators — §5.2.6.4:
    // zero or more, EACH AT MOST ONCE, any order) are a list since computer-name-1 became optional in the grammar.
    public static readonly DiagnosticDescriptor ObjectComputerDuplicateClause = new(
        "COBOLNET1652", "object-computer-duplicate-clause", EditionSeverity.Error,
        "An OBJECT-COMPUTER clause (PROGRAM COLLATING SEQUENCE, CHARACTER CLASSIFICATION) is specified more than once "
        + "in the paragraph. ISO §12.3.6.2's format encloses the clauses in choice indicators within brackets, which "
        + "§5.2.6.4 defines as zero or more of the alternatives, each at most once, in any order. Keep one.",
        "ISO §12.3.6.2 / §5.2.6.4");
    // kb/Work PB79 (data-model design D20): the GROUP-USAGE clause's syntax rules — SR1 (a group item that is not
    // strongly typed and not a variable-length group), SR2/SR3 (no explicit USAGE on the subject; every subordinate
    // group the same GROUP-USAGE). The subordinate LEAF conformance rides the shared §13.18.60.4 GR1 usage-inheritance
    // check (COBOLNET0881 / the national-form staged 0899 legs), never a second copy here.
    public static readonly DiagnosticDescriptor GroupUsageRule = new(
        "COBOLNET1653", "group-usage-rule", EditionSeverity.Error,
        "A GROUP-USAGE clause violates one of its syntax rules: it is specified on an entry that is not a group item, "
        + "on a strongly-typed group, or on a variable-length group (SR1); a USAGE clause is explicitly specified on "
        + "the same entry (SR2/SR3 — the usage is implied); or a subordinate group declares the other GROUP-USAGE "
        + "(SR2/SR3 — every subordinate group is the same, explicitly or implicitly).",
        "ISO §13.18.29.3");
    // kb/Work PB93: a REDEFINES data-name-2 / RENAMES data-name-2/-3 that names no preceding entry was accepted
    // SILENTLY (the program-scope REDEFINES miss left RedefinesTargetName set against a null RedefinesTarget, so the
    // layout consumers disagreed; the RENAMES miss just `continue`d). §8.4.2.1: a statement shall contain a reference
    // that uniquely identifies the resource — a name that identifies nothing is an error at every edition.
    public static readonly DiagnosticDescriptor RedefinesTargetUnresolved = new(
        "COBOLNET1654", "redefines-target-unresolved", EditionSeverity.Error,
        "The REDEFINES clause's data-name-2 does not name a preceding data description entry in the same scope. "
        + "ISO §13.18.44.3 SR4/SR7/SR10 place data-name-2 among the entries preceding the subject at the same level; "
        + "§8.4.2.1 requires every reference to identify a resource. Name the entry that defines the area.",
        "ISO §13.18.44.3 / §8.4.2.1");
    public static readonly DiagnosticDescriptor RenamesOperandUnresolved = new(
        "COBOLNET1655", "renames-operand-unresolved", EditionSeverity.Error,
        "A RENAMES clause's data-name-2 or data-name-3 does not name an elementary item or group of elementary items in "
        + "the same record (ISO §13.18.45.3 SR4; §8.4.2.1 requires every reference to identify a resource). Name an "
        + "item of the record the level-66 entry follows.",
        "ISO §13.18.45.3 / §8.4.2.1");
    // kb/Work PB93 (sweep): SR7 — "Multiple redefinitions of the same storage area shall each specify as data-name-2
    // the data-name of the entry that originally defined the area" — a REDEFINES naming a REDEFINER is illegal ISO
    // source that GnuCOBOL/IBM accept as a chain (the anchor is chased); error strict, warning + the chain semantics
    // under --permissive (the documented-dialect-leniency seam, EditionContext.Removed).
    public static readonly DiagnosticDescriptor RedefinesOfRedefinition = new(
        "COBOLNET1656", "redefines-of-redefinition", EditionSeverity.Error,
        "A REDEFINES clause names an entry that is itself a redefinition. ISO §13.18.44.3 SR7 requires each of multiple "
        + "redefinitions of one storage area to name the entry that ORIGINALLY defined the area. Name the original "
        + "entry; under --permissive the chain is accepted with this warning (the anchor is the original entry).",
        "ISO §13.18.44.3 SR7");
    // kb/Work PB94: §13.18.63.3 SR2 ("If the category of the subject of the entry is numeric, all literals in the VALUE
    // clause shall be numeric") and SR4 ("If the item is of category alphabetic, alphanumeric, or alphanumeric-edited
    // literals in the VALUE clause shall be alphanumeric literals") were unenforced — `PIC 9 VALUE "abc"` reached the
    // C# backend (CS0103), `PIC 99 VALUE "7"` and `PIC X(2) VALUE 12` compiled silently. Error strict at every
    // edition; under --permissive the REPRESENTABLE vendor leniency (a digits-only alphanumeric literal on a numeric
    // item is that number; a numeric literal on an alphanumeric item is its digits, left-justified; a character
    // figurative on a numeric item is ZERO — a native numeric holds no character fill) is a warning. The national /
    // boolean halves of the same family (SR5 / SR10) are the pre-catalog COBOLNET0898 band.
    public static readonly DiagnosticDescriptor ValueLiteralClass = new(
        "COBOLNET1657", "value-literal-class", EditionSeverity.Error,
        "A VALUE clause literal's class does not match the subject's category: a numeric item takes numeric literals "
        + "(or figurative ZERO) only (ISO §13.18.63.3 SR2); an alphabetic, alphanumeric or alphanumeric-edited item "
        + "takes alphanumeric literals only (SR4). Under --permissive a representable value is stored with this "
        + "warning; a value no numeric item can hold is an error on both axes. So is a numeric literal on a GROUP "
        + "subject (SR13 sentence 1 — kb/Work PB206): the leniency promises the literal's digits are stored as MOVE "
        + "would store them, and §13.18.63.4 GR5's area deposit is over the operand's CHARACTERS, which a numeric "
        + "literal has none of — the measured result was a group seeded with SPACES.",
        "ISO §13.18.63.3 SR2 / SR4 / SR13");
    // kb/Work PB66 (data-model design D21): the floating-point numeric-edited PICTURE (the symbol E) — its syntax rules.
    public static readonly DiagnosticDescriptor PictureFloatEdited = new(
        "COBOLNET1658", "picture-float-edited", EditionSeverity.Error,
        "A floating-point numeric-edited PICTURE (a significand and an exponent separated by the symbol E) violates its "
        + "form: E and the decimal point may appear only once (ISO §13.18.40.3 SR12 b); the exponent shall be +9, +99, "
        + "+999 or +9999 (§13.18.40.4 GR13 b); the significand admits only 9, B, 0, /, comma, the point and one leading "
        + "+ or − (§13.18.40.6 Table 10 row E — no floating insertion, no zero suppression, no S/V/P/CR/DB/currency, "
        + "no EDITING character); the significand carries 1 to 36 digit positions (§13.18.40.3 SR15).",
        "ISO §13.18.40.3 / §13.18.40.4 GR13 b / §13.18.40.6");
    // kb/Work PB66 / PB97: §13.18.63.3 SR6 — a numeric-edited item's numeric VALUE literal shall be of the item's FORM:
    // a fixed-point numeric-edited item takes a fixed-point literal, a floating-point numeric-edited item a
    // floating-point literal; ZERO and the zero forms are legal for either. Reaches the item VALUE and the level-88 set.
    public static readonly DiagnosticDescriptor ValueEditedLiteralForm = new(
        "COBOLNET1659", "value-edited-literal-form", EditionSeverity.Error,
        "The form of a numeric VALUE literal for a numeric-edited item shall match the item's form (ISO §13.18.63.3 SR6): "
        + "a fixed-point numeric-edited item takes a fixed-point literal, a floating-point numeric-edited item a "
        + "floating-point literal (a mantissa and an exponent, e.g. 1.5E+3); the figurative constant ZERO and the "
        + "integer / decimal literal zero are legal for either.",
        "ISO §13.18.63.3 SR6");
    // kb/Work PB66: §15.43.4 r1 / §15.58.4 r1 — the well-formedness of a floating-point numeric-edited argument-1 of
    // HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC: its extreme shall pass an IN-ARITHMETIC-RANGE test, i.e. lie within the
    // intermediate data item's range for the arithmetic mode in effect (§8.8.4.4.4 GR3 l).
    public static readonly DiagnosticDescriptor AlgebraicFloatEditedRange = new(
        "COBOLNET1660", "algebraic-float-edited-range", EditionSeverity.Error,
        "FUNCTION HIGHEST-ALGEBRAIC / LOWEST-ALGEBRAIC: the floating-point numeric-edited argument-1's data description "
        + "entry shall be such that its value farthest from zero (all-nines significand at the maximum exponent) would "
        + "pass an IN-ARITHMETIC-RANGE test under the arithmetic mode in effect — the intermediate data item cannot hold "
        + "it (ISO §15.43.4 r1 / §15.58.4 r1; §8.8.4.4.4 GR3 l). Narrow the exponent, or change ARITHMETIC.",
        "ISO §15.43.4 r1 / §15.58.4 r1");
    // kb/Work PB99: §8.3.3.3.3 SR2/SR3/SR4 — the floating-point literal's form — and r3's implementor-defined exponent
    // range (binary64 for a procedure-division literal, D16; the receiver's binary form for a VALUE on a FLOAT item).
    public static readonly DiagnosticDescriptor FloatingLiteral = new(
        "COBOLNET1661", "floating-point-literal", EditionSeverity.Error,
        "A floating-point numeric literal shall have a significand of 1 to 36 digits with a decimal point, an exponent of at "
        + "most four digits, and — when the significand is zero — a zero exponent and no negative sign (ISO §8.3.3.3.3 "
        + "SR2/SR3/SR4); its value shall lie within the implementor-defined exponent range (r3): the IEEE binary64 range "
        + "(about 4.9E-324 to 1.8E+308) for a literal that evaluates in an arithmetic expression or seeds a FLOAT-LONG / "
        + "FLOAT-BINARY-64 item, the binary32 range for FLOAT-SHORT / FLOAT-BINARY-32; a floating-point numeric-edited "
        + "VALUE keeps the exact value.",
        "ISO §8.3.3.3.3 SR2/SR3/SR4/r3");
    // kb/Work PB101 (DESIGN-locale-facility §4.9, increment T7): the SPECIAL-NAMES ORDER TABLE clause and
    // FUNCTION STANDARD-COMPARE. §12.3.7.4 GR17 leaves the allowable content of literal-9 to the implementor, so a
    // literal this implementation's collation engine cannot resolve is NOT a syntax error — the program is legal
    // and §15.85.4 r2 defines its runtime outcome (EC-ORDER-NOT-SUPPORTED at every reference). A WARNING is
    // therefore the only conforming severity, and saying nothing would leave a program whose every
    // STANDARD-COMPARE is inoperative looking clean at compile time.
    public static readonly DiagnosticDescriptor OrderTableUnresolved = new(
        "COBOLNET1662", "order-table-unresolved", EditionSeverity.Warning,
        "The SPECIAL-NAMES ORDER TABLE clause's literal-9 does not name a cultural ordering table this "
        + "implementation provides. ISO §12.3.7.4 GR17 leaves the allowable content of literal-9 to the "
        + "implementor; WiseOwl COBOL accepts the default table 'ISO 14651_2020_TABLE1' (case-insensitive, the space "
        + "and the underscore interchangeable) and, as an implementor extension, a CLDR locale tag naming a "
        + "tailored collation. Every FUNCTION STANDARD-COMPARE reference to this ordering-name sets "
        + "EC-ORDER-NOT-SUPPORTED at run time (§15.85.4 r2). The clause itself stays legal.",
        "ISO §12.3.7.4 GR17 / §15.85.4 r2");
    public static readonly DiagnosticDescriptor StandardCompareArgument = new(
        "COBOLNET1663", "standard-compare-argument", EditionSeverity.Error,
        "A FUNCTION STANDARD-COMPARE argument violates ISO §15.85.3: ordering-name-1 shall be a name associated "
        + "with a cultural ordering table in the ORDER TABLE clause of the SPECIAL-NAMES paragraph (r5; §15.3 "
        + "argument type 12, and §12.3.7.3 SR9 makes this function the only place such a name may be specified), "
        + "and argument-4 shall be a positive nonzero integer (r6). The §15.85.2 general format admits at most one "
        + "of each, so a second ordering-name or a second level violates the format.",
        "ISO §15.85.3 r5 / r6 / §15.85.2");
    // kb/Work PB64 T1 (DESIGN-locale-facility §7 rules a–e): the syntax rules of the locale facility that become
    // reachable once the SPECIAL-NAMES LOCALE clause and SET formats 11/12 are ACCEPTED rather than refused by name.
    public static readonly DiagnosticDescriptor LocaleNameUndeclared = new(
        "COBOLNET1664", "locale-name-undeclared", EditionSeverity.Error,
        "A locale-name is referenced that no SPECIAL-NAMES LOCALE clause in scope declares — the ONE diagnostic for every "
        + "locale-name reference site, which names the rule it cites: the ALPHABET clause's IS LOCALE locale-name-2 "
        + "(ISO §12.3.7.3 SR24), SET LOCALE's locale-name-1 (§14.9.39.3 SR26), and the later increments' LOCALE phrases "
        + "(PICTURE §13.18.40.3 SR37; LOCALE-COMPARE / -DATE / -TIME / UPPER-CASE / LOWER-CASE; CHARACTER CLASSIFICATION "
        + "§12.3.6.3 SR3). Declare it: LOCALE locale-name IS external-locale-name | literal.",
        "ISO §12.3.7.3 SR24 / §14.9.39.3 SR26 / §12.3.7.4 GR1");
    public static readonly DiagnosticDescriptor LocaleNameDuplicate = new(
        "COBOLNET1665", "locale-name-duplicate", EditionSeverity.Error,
        "The same locale-name is declared by more than one SPECIAL-NAMES LOCALE clause of one paragraph. A user-defined "
        + "word of one type is unique within its scope (ISO §8.3.2.2); the LOCALE clause is repeatable (§12.3.7.2) so "
        + "several LOCALES may be declared, each under its own name.", "ISO §8.3.2.2 / §12.3.7.2");
    public static readonly DiagnosticDescriptor SetLocaleCategories = new(
        "COBOLNET1666", "set-locale-categories", EditionSeverity.Error,
        "The first operand of SET LOCALE (ISO §14.9.39.2 format 11) is malformed: a category is specified more than "
        + "once (the category brace carries choice indicators — §5.2.6.4: one or more of LC_ALL, LC_COLLATE, LC_CTYPE, "
        + "LC_MESSAGES, LC_MONETARY, LC_NUMERIC, LC_TIME, each at most once, in any order), a word that is not a locale "
        + "category appears in the list, or USER-DEFAULT is combined with a category (the outer brace is a plain "
        + "alternation — categories OR USER-DEFAULT).", "ISO §14.9.39.2 format 11 / §5.2.6.4");
    public static readonly DiagnosticDescriptor SetLocaleUserDefaultSource = new(
        "COBOLNET1667", "set-locale-user-default-source", EditionSeverity.Error,
        "SET LOCALE USER-DEFAULT TO USER-DEFAULT / SYSTEM-DEFAULT: if USER-DEFAULT is specified as the first operand, "
        + "identifier-10 or locale-name-1 shall be specified in the TO phrase (ISO §14.9.39.3 SR25) — the user default "
        + "is set FROM a named or saved locale (§14.9.39.4 GR22), never from itself or the system default.",
        "ISO §14.9.39.3 SR25 / §14.9.39.4 GR22");
    public static readonly DiagnosticDescriptor SetLocalePointerCategory = new(
        "COBOLNET1668", "set-locale-pointer-category", EditionSeverity.Error,
        "The identifier of SET LOCALE … TO identifier-10 (ISO §14.9.39.3 SR27) or of SET identifier-11 TO LOCALE (SR28) "
        + "shall reference an elementary data item of category data-pointer (USAGE POINTER) — the saved-locale handle "
        + "of §14.9.39.4 GR26/GR27 is a pointer value, stored and read only through such an item.",
        "ISO §14.9.39.3 SR27 / SR28");
    public static readonly DiagnosticDescriptor LocaleAlphabetNotACharacterSet = new(
        "COBOLNET1669", "locale-alphabet-not-a-charset", EditionSeverity.Error,
        "An alphabet defined with the LOCALE phrase (ALPHABET … [FOR NATIONAL] IS LOCALE [locale-name]) is referenced "
        + "where a CODED CHARACTER SET is required — a class condition's alphabet-name-1 (ISO §8.8.4.4.3 SR2: 'shall not "
        + "reference an alphabet associated with a locale'; the same rule governs the IN phrases of SYMBOLIC CHARACTERS "
        + "and CLASS, §12.3.7.3 SR16g / SR17d, and the CODE-SET clause, §13.18.13.3 SR1 / SR2). A LOCALE alphabet defines "
        + "a collating sequence only (§12.3.7.4 GR7, Table 6) — it names no set of characters. Name a coded-character-set "
        + "alphabet (NATIVE, STANDARD-1/2, UCS-4, UTF-8, UTF-16) instead.",
        "ISO §8.8.4.4.3 SR2 / §12.3.7.3 SR16g, SR17d");
    public static readonly DiagnosticDescriptor SymbolicCharactersViolation = new(
        "COBOLNET1670", "symbolic-characters-clause", EditionSeverity.Error,
        "A SYMBOLIC CHARACTERS clause violates one of ISO §12.3.7.3 SR16's sub-rules (the message names which): a) a "
        + "given symbolic-character-1 may be specified only once within the paragraph's SYMBOLIC CHARACTERS clauses; "
        + "b/c) the names pair with the integers by position, one-to-one; e/f) the ordinal position shall exist in the "
        + "native character set of the clause's class or, under IN, in the character set referenced by alphabet-name-3, "
        + "which shall define a set of that class. (A LOCALE alphabet under IN is COBOLNET1669 — SR16 g.)",
        "ISO §12.3.7.3 SR16");
    public static readonly DiagnosticDescriptor ClassClauseViolation = new(
        "COBOLNET1671", "class-clause", EditionSeverity.Error,
        "A SPECIAL-NAMES CLASS clause violates one of ISO §12.3.7.3 SR17's class-dependent sub-rules (the message names "
        + "which; b applies when the ALPHANUMERIC phrase is specified or implied, c under NATIONAL): 1) the IN phrase's "
        + "alphabet-name-4 shall define a character set of the clause's class; 2) a numeric literal shall be an unsigned "
        + "integer from one through the number of characters in the native set or, under IN, in the set referenced by "
        + "alphabet-name-4 (the ordinal resolves in THAT set — §12.3.7.4 GR12 a); 3) each noninteger literal shall be "
        + "of the clause's class; 4) each literal of a THROUGH phrase shall be one character; 5) the characters "
        + "specified shall not outnumber that set. (A LOCALE alphabet under IN is COBOLNET1669 — SR17 d.)",
        "ISO §12.3.7.3 SR17");
    public static readonly DiagnosticDescriptor CodeSetClauseViolation = new(
        "COBOLNET1672", "code-set-clause", EditionSeverity.Error,
        "An FD CODE-SET clause violates one of ISO §13.18.13.3's syntax rules (the message names which): SR1/SR2 — "
        + "alphabet-name-1 / alphabet-name-2 shall reference an alphabet defining an alphanumeric / national coded "
        + "character set (a LOCALE alphabet is COBOLNET1669; a class mismatch or an undeclared name is named here); "
        + "each class at most once (§5.2.6.4) — or names a coded character set whose on-medium representation differs "
        + "from the native encoding, which this processor does not provide (Annex A §A.3 item 27 — the CODE-SET clause "
        + "is dependent upon a device capable of supporting the specified code; documented non-support, CONFORMANCE.md "
        + "§2 row 27: NATIVE and the identity-correspondence sets STANDARD-1 / STANDARD-2 / UTF-16 are supported).",
        "ISO §13.18.13.3; Annex A §A.3 item 27");
    public static readonly DiagnosticDescriptor PictureLocaleFormat2Violation = new(
        "COBOLNET1673", "picture-locale-format2", EditionSeverity.Error,
        "A format 2 (LOCALE) PICTURE clause violates one of ISO §13.18.40.3's syntax rules — the message names "
        + "which: SR32 — not in (or subordinate to) a CONSTANT RECORD item; SR33 — character-string-1 shall "
        + "contain at least one 'Z' or '9'; SR34 — each of '+', '.', the currency symbol at most once; SR35 — 1 "
        + "through 31 digit positions; SR36 — the currency symbol and '+' only left of the decimal point "
        + "position; the §13.18.40.6 Table 11 precedence (the symbols are ONLY '+', the currency symbol, 'Z', "
        + "'9', '.' — '+' first, the currency symbol before every digit, no '9' before any 'Z', so the legal "
        + "shape is [+] [cs] Z… 9… [. Z…|9…]); or an EDITING phrase beside the LOCALE phrase (format 2 has no "
        + "EDITING phrase — it is format 1's).",
        "ISO §13.18.40.3 SR32-SR37 / §13.18.40.6 Table 11");
    public static readonly DiagnosticDescriptor SignClauseWithLocalePicture = new(
        "COBOLNET1674", "sign-clause-with-locale-picture", EditionSeverity.Error,
        "A SIGN clause is specified for a data item whose PICTURE clause carries the LOCALE phrase (format 2). "
        + "ISO §13.16.3 SR19 (data description) / §13.17.3 SR9 (screen description): \"If the LOCALE phrase of "
        + "the PICTURE clause is specified, the SIGN clause shall not be specified\" — a locale-edited item's "
        + "sign representation comes from the locale (§13.18.40.5 rule 13), never from a SIGN clause. (A report "
        + "group description entry carries NO such rule — §13.15.3 — and the pair is legal there.)",
        "ISO §13.16.3 SR19 / §13.17.3 SR9");
    public static readonly DiagnosticDescriptor RefModIdentifierNotPermitted = new(
        "COBOLNET1647", "ref-mod-identifier-not-permitted", EditionSeverity.Error,
        "Reference modification is applied to an item ISO §8.4.3.3.3 SR1 does not admit as identifier-1: a boolean, "
        + "national, alphanumeric or alphabetic item, an alphanumeric group item, an edited item or a numeric item of "
        + "usage DISPLAY or NATIONAL (each not subordinate to a strongly-typed group), or a group that is neither "
        + "strongly-typed nor variable-length (§8.5.1.12). Reference-modify a permitted item, or REDEFINES the storage "
        + "with a character item.", "ISO §8.4.3.3.3 SR1");
    // The receiving-side residue (kb/Work PB70): a data reference that RESOLVED to a declared item but whose shape
    // this compiler does not implement as a receiving operand. It used to be dropped from the receiver list by
    // .OfType<Place>() — `MOVE "Z" TO OK1 TB(2:1) OK2` moved into OK1 and OK2 and silently skipped TB.
    public static readonly DiagnosticDescriptor ReceivingReferenceNotImplemented = new(
        NotImplemented, "receiving-reference-shape-not-implemented", EditionSeverity.Error,
        "A receiving operand names a declared item in a reference shape WiseOwl COBOL does not yet implement as a receiver "
        + "(COBOLNET_DESIGN §1.4: an unsupported shape fails loud, never silently). The statement is rejected rather "
        + "than run with the receiver dropped.", "COBOLNET_DESIGN §1.4",
        RecognizedNotImplemented);
    // kb/Work PB1030: the SENDING twin — a reference the resolver DEFERS, in a position whose binder has no node that
    // can carry a deferral (a pointer / object-reference operand, a helper answering "place or reported").
    public static readonly DiagnosticDescriptor ReferenceShapeNotImplemented = new(
        NotImplemented, "reference-shape-not-implemented", EditionSeverity.Error,
        "An operand names a declared item in a reference shape WiseOwl COBOL does not yet implement in this position "
        + "(COBOLNET_DESIGN §1.4: an unsupported shape fails loud, never silently). The statement is rejected.",
        "COBOLNET_DESIGN §1.4",
        RecognizedNotImplemented);
    // kb/Work PB128: arithmetic RESULTANTS never had a compile-time category screen — COMPUTE X-item = 1
    // compiled and died in StoreArith's run-time loud, where §4.2.2 requires a compile-time mechanism.
    public static readonly DiagnosticDescriptor ArithmeticResultantCategory = new(
        "COBOLNET1675", "arithmetic-resultant-category", EditionSeverity.Error,
        "An arithmetic statement's resultant identifier is not of a category its syntax rule admits. The in-place "
        + "receivers (ADD TO / SUBTRACT FROM / MULTIPLY BY / DIVIDE INTO) shall reference elementary NUMERIC data "
        + "items; the GIVING resultants, DIVIDE's REMAINDER and COMPUTE's identifier-1 admit elementary numeric or "
        + "NUMERIC-EDITED items. A group, an alphanumeric/alphabetic/boolean item, an index data item "
        + "(§13.18.60.3 SR10's closed reference list), or a reference-modified slice (§8.4.3.3.4 GR6c makes it "
        + "category alphanumeric) is not a legal resultant.",
        "ISO §14.9.2.3 SR2/SR4 · §14.9.8.3 SR1 · §14.9.12.3 SR1/SR2 · §14.9.26.3 SR1/SR2 · §14.9.44.3");
    // kb/Work PB1413 — the INTEGER half of §8.8.2 rule 5 for a boolean shift count. Its SHAPE half (Table 4's single
    // identifier or literal) is COBOLNET1719's, so this code fires only on a well-formed count that is not an integer.
    public static readonly DiagnosticDescriptor BooleanShiftCountNotInteger = new(
        "COBOLNET2513", "boolean-shift-count-not-integer", EditionSeverity.Error,
        "The second operand of a boolean shift operator (B-SHIFT-L / B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC) is not an "
        + "integer operand. ISO §8.8.2 rule 5: \"The second operand shall be an integer operand.\" §5.5 2) makes that "
        + "an integer literal (\"a fixed-point numeric literal that contains no decimal point\", §8.3.3.3.2), an "
        + "integer intrinsic function, or a fixed-point numeric data item with no digit positions to the right of the "
        + "radix point; §8.4.3.2.3 SR11 bars a numeric function even when its value is integral. Write an integer count.",
        "ISO §8.8.2 rule 5");
    public static readonly DiagnosticDescriptor PerformTimesCountNotInteger = new(
        "COBOLNET1646", "perform-times-count-not-integer", EditionSeverity.Error,
        "The PERFORM … TIMES count is not an integer. ISO §14.9.28.3 SR2: \"Each identifier shall reference a numeric "
        + "elementary item described in the data division. Identifier-1 shall be an integer.\" — an integer data item "
        + "(category numeric, no fraction digits, not USAGE INDEX), an integer literal, or a function-identifier whose "
        + "type is integer (§15.2 type 5). Write an integer count.",
        "ISO §14.9.28.3 SR2");
    public static readonly DiagnosticDescriptor AllSubscriptNotRepeatable = new(
        "COBOLNET1645", "all-subscript-not-repeatable-argument", EditionSeverity.Error,
        "A table(ALL) argument is written to an intrinsic function whose general format does not repeat an argument. "
        + "ISO §15.3: \"When the definition of a function permits an argument to be repeated a variable number of "
        + "times, a table may be referenced by specifying the data-name and any qualifiers that identify the table, "
        + "followed immediately by subscripting where one or more of the subscripts is the word ALL.\" The ALL "
        + "subscript stands for every occurrence, so it belongs only where the format is `{ argument } …` (MAX, MIN, "
        + "SUM, MEAN, MEDIAN, MIDRANGE, RANGE, ORD-MAX, ORD-MIN, VARIANCE, STANDARD-DEVIATION, PRESENT-VALUE, CONCAT, "
        + "SUBSTITUTE, TRIM's argument-2). Write the occurrence you mean.",
        "ISO §15.3 (the ALL subscript)");
    public static readonly DiagnosticDescriptor NumvalCAmbiguousCurrency = new(
        "COBOLNET1644", "numval-c-ambiguous-currency", EditionSeverity.Error,
        "FUNCTION NUMVAL-C or TEST-NUMVAL-C is written without argument-2 (and without LOCALE) in a compilation "
        + "unit whose SPECIAL-NAMES paragraph specifies two or more distinct currency strings. ISO §15.68.3 rule 3: "
        + "\"If neither argument-2 nor the LOCALE keyword is specified, there shall be only one currency string for "
        + "the compilation unit, either the default currency sign or a currency string specified in the "
        + "SPECIAL-NAMES paragraph.\" Name the intended currency string as argument-2.",
        "ISO §15.68.3 rule 3 (via §15.94.3 rule 1 for TEST-NUMVAL-C)");
    public static readonly DiagnosticDescriptor ConfigurationSectionInContainedProgram = new(
        "COBOLNET1643", "configuration-section-in-contained-program", EditionSeverity.Error,
        "A CONFIGURATION SECTION is specified in a program that is contained within another program. ISO "
        + "§12.3.3 SR1: \"The configuration section shall not be specified in a program that is contained within "
        + "another program\" — the containing program's configuration section (SPECIAL-NAMES, OBJECT-COMPUTER, "
        + "SOURCE-COMPUTER, REPOSITORY) applies to every directly or indirectly contained program (§12.3.4 GR1). "
        + "Move the entries to the outermost program.",
        "ISO §12.3.3 SR1 / §12.3.4 GR1");
    public static readonly DiagnosticDescriptor ReplaceOperandNotPseudoText = new(
        "COBOLNET1641", "replace-operand-not-pseudo-text", EditionSeverity.Error,
        "A REPLACE statement operand is not pseudo-text. REPLACE's general format (§7.2.4.2) admits "
        + "==pseudo-text== (and ==partial-word== under LEADING/TRAILING) operands only — a bare literal, "
        + "identifier or word is not a REPLACE operand in any ISO edition, and §7.2.4.3 SR7 explicitly bars "
        + "alphanumeric, boolean and national literals as partial-words. Write ==operand== (empty "
        + "==== deletes under LEADING/TRAILING).",
        "ISO §7.2.4.2 / §7.2.4.3 SR7");
    public static readonly DiagnosticDescriptor CopyReplacingNestedCopy = new(
        "COBOLNET1640", "copy-replacing-nested-copy", EditionSeverity.Error,
        "A COPY statement with the REPLACING phrase names library text that itself contains a COPY statement. "
        + "ISO §7.2.3.4 GR10: \"If the REPLACING phrase is specified, the library text shall not contain a "
        + "COPY statement\"; nesting (at least 5 levels) is permitted only without REPLACING (GR12). Flatten "
        + "the copybook, or drop the REPLACING phrase.",
        "ISO §7.2.3.4 GR10 / GR12");
    /// <summary>COBOLNET2449 — a COPY or REPLACE statement that does not follow its general format (kb/Work PB1354).
    /// Both statements are parsed over §7.2.2.5 text-words in general-format order (COPY: text-name-1/literal-1, the
    /// OF/IN phrase, SUPPRESS [PRINTING], REPLACING operands, the separator period); a word out of place, a missing BY,
    /// an empty REPLACING phrase, an unterminated pseudo-text or a missing separator period is this error. Before, a
    /// hand scan read the header positionally and skipped everything else to the next '.', so
    /// <c>COPY t SUPPRESS REPLACING …</c> silently lost its REPLACING phrase and every malformed header compiled.</summary>
    public static readonly DiagnosticDescriptor TextManipulationStatementSyntax = new(
        "COBOLNET2449", "text-manipulation-statement-syntax", EditionSeverity.Error,
        "A COPY or REPLACE statement does not follow its general format: a word is out of place, a phrase is "
        + "incomplete (REPLACING with no operands, an operand with no BY, an unterminated ==pseudo-text==), or the "
        + "statement has no terminating separator period.",
        "ISO §7.2.3.2 / §7.2.4.2 / §7.2.3.4 GR6");

    /// <summary>COBOLNET2495 — a floating comment indicator <c>*&gt;</c> written directly after a character (kb/Work
    /// PB1493). It is recognized in ONE place, the §6.5 logical conversion, which still takes the rest of the line as
    /// the comment; before, both the fixed-form strip and the lexer took <c>X*&gt; …</c> as a comment silently.</summary>
    public static readonly DiagnosticDescriptor FloatingCommentNotSeparated = new(
        "COBOLNET2495", "floating-comment-not-separated", EditionSeverity.Error,
        "A floating comment indicator *> is written directly after another character. ISO §6.2.3.2 SR2: \"The "
        + "floating comment indicator of an inline comment shall be preceded by a separator space\". Write a space "
        + "before the *>.", "ISO §6.2.3.2 SR2");

    /// <summary>COBOLNET2496 — a multiple-character floating indicator split across a fixed-form continued line and
    /// its continuation line (kb/Work PB1493): the continued line ends with the indicator's first character and the
    /// continuation line's program text begins with the rest, so the join spells it without its being written.</summary>
    public static readonly DiagnosticDescriptor FloatingIndicatorSplit = new(
        "COBOLNET2496", "floating-indicator-split", EditionSeverity.Error,
        "A continuation line completes a floating indicator (*> or >>) begun at the end of the line it continues. "
        + "ISO §6.2.3.2 SR3: \"All the characters forming a multiple-character floating indicator shall be specified "
        + "on the same line.\" Write the whole indicator on one line.", "ISO §6.2.3.2 SR3");

    /// <summary>COBOLNET2616 — the indicator area of a fixed-form line holds a character that is not a fixed indicator
    /// (kb/Work PB1494). §6.2.2 lists <c>*</c>, <c>/</c>, <c>-</c> and space (and COBOL-85's debugging indicator
    /// <c>D</c>); the NIST CCVS column-7 conventions (S/Y debugging, P/J/H/E/U excluded alternates, any other letter a
    /// primary-configuration line) are honored under <c>--nist</c> only. Before, every compilation silently dropped a
    /// line marked E and silently compiled one marked X. The line is still read as source after the diagnostic.</summary>
    public static readonly DiagnosticDescriptor FixedIndicatorInvalid = new(
        "COBOLNET2616", "fixed-indicator-invalid", EditionSeverity.Error,
        "The indicator area (column 7) of a fixed-form line holds a character that is not a fixed indicator. ISO "
        + "§6.2.2 lists * and / (comment line), - (continuation line) and space (source line); write one of them, or "
        + "compile a NIST CCVS program with --nist.", "ISO §6.2.2 / §6.3.3");

    /// <summary>COBOLNET2653 — a free-form line longer than 255 character positions (kb/Work PB1496). §6.1 3) a) bounds
    /// the number of positions on a free-form line at 255; only a fixed-form line logically converted to free form may
    /// be longer (§6.1 2) b)). Asked on the EXPANDED line — a tab counts the positions it advances over
    /// (DOC-A.1-157) — once per offending line; the line is still read in full after the diagnostic.</summary>
    public static readonly DiagnosticDescriptor FreeFormLineTooLong = new(
        "COBOLNET2653", "free-form-line-too-long", EditionSeverity.Error,
        "A free-form line has more than 255 character positions. ISO §6.1 3) a): \"The number of character positions "
        + "on a line may vary from line to line, ranging from a minimum of 0 to a maximum of 255.\" Break the line "
        + "(the floating literal continuation indicator continues a literal) or split the statement.",
        "ISO §6.1 3) a)");

    /// <summary>COBOLNET2684 — a continuation line of a literal whose first nonblank character is not the quotation
    /// symbol of the literal's opening delimiter (kb/Work PB1492, PB1359). One check for the fixed continuation
    /// indicator and the floating one, in both reference formats: before, a fixed-form continuation line with no
    /// quotation symbol, or the OTHER one, was spliced into the literal silently.</summary>
    public static readonly DiagnosticDescriptor LiteralContinuationQuote = new(
        "COBOLNET2684", "literal-continuation-quote", EditionSeverity.Error,
        "The first nonblank character of a continuation line of a literal is not the quotation symbol used in the "
        + "literal's opening delimiter. ISO §6.2.3.2 SR6: \"the first nonblank character of each continuation line "
        + "shall be the quotation symbol used in the opening delimiter of the literal\". Begin the continuation "
        + "line with the same quotation symbol (a quotation mark for a literal opened with one, an apostrophe for "
        + "one opened with an apostrophe).", "ISO §6.2.3.2 SR6 / §6.3.5 2) / §6.4.2");

    /// <summary>COBOLNET2685 — a national literal continued by the fixed continuation indicator (kb/Work PB1492).
    /// §6.3.5 2): "National literals may be continued only with a floating literal continuation indicator."</summary>
    public static readonly DiagnosticDescriptor NationalLiteralFixedContinuation = new(
        "COBOLNET2685", "national-literal-fixed-continuation", EditionSeverity.Error,
        "A national literal is continued with the fixed continuation indicator (a hyphen in column 7). ISO §6.3.5 "
        + "2): \"National literals may be continued only with a floating literal continuation indicator.\" End the "
        + "continued line with the literal's quotation symbol followed by a hyphen, and begin the continuation line "
        + "with the quotation symbol.", "ISO §6.3.5 2)");

    /// <summary>COBOLNET2686 — a floating literal continuation indicator on a line that holds the fixed continuation
    /// indicator (kb/Work PB1359). §6.2.3.2 SR5.</summary>
    public static readonly DiagnosticDescriptor FloatingContinuationOnFixedContinuation = new(
        "COBOLNET2686", "floating-continuation-on-fixed-continuation", EditionSeverity.Error,
        "A line with the fixed continuation indicator (a hyphen in column 7) ends in a floating literal continuation "
        + "indicator. ISO §6.2.3.2 SR5: \"A floating literal continuation indicator shall not be specified on a line "
        + "that contains a fixed literal continuation indicator.\" Continue the literal with one form only.",
        "ISO §6.2.3.2 SR5");

    /// <summary>COBOLNET2687 — a literal continued with both the fixed and the floating form (kb/Work PB1359).
    /// §6.2.3.2 SR4, second sentence.</summary>
    public static readonly DiagnosticDescriptor LiteralContinuedTwoForms = new(
        "COBOLNET2687", "literal-continued-two-forms", EditionSeverity.Error,
        "A literal is continued with more than one form of continuation (the fixed indicator, a hyphen in column 7, "
        + "and the floating indicator, a quotation symbol followed by a hyphen). ISO §6.2.3.2 SR4: \"A given literal "
        + "shall not be continued with more than one form of continuation.\" Use the floating indicator on every "
        + "line of the literal.", "ISO §6.2.3.2 SR4");

    /// <summary>COBOLNET2688 — a fixed-form continuation line that completes a multiple-character separator or
    /// invocation operator (<c>==</c>, <c>::</c>) begun at the end of the line it continues (kb/Work PB1492). The
    /// floating INDICATORS <c>*&gt;</c> and <c>&gt;&gt;</c> keep <see cref="FloatingIndicatorSplit"/>.</summary>
    public static readonly DiagnosticDescriptor MultipleCharacterTokenSplit = new(
        "COBOLNET2688", "multiple-character-token-split", EditionSeverity.Error,
        "A continuation line completes a multiple-character separator or operator (== or ::) begun at the end of the "
        + "line it continues. ISO §6.3.5 2): \"All characters composing any multiple-character separator or "
        + "multiple-character indicator shall be specified on the same line. All characters forming an invocation "
        + "operator shall be specified on the same line.\" Write all its characters on one line.", "ISO §6.3.5 2)");

    /// <summary>COBOLNET2689 — an inline comment on a line that ends in a floating literal continuation indicator
    /// (kb/Work PB1359). §6.3.7.3 / §6.4.4.3: an inline comment may be written on any line except one that contains
    /// a floating literal continuation indicator.</summary>
    public static readonly DiagnosticDescriptor FloatingContinuationComment = new(
        "COBOLNET2689", "floating-continuation-comment", EditionSeverity.Error,
        "A line that contains a floating literal continuation indicator also holds an inline comment. ISO §6.3.7.3 "
        + "and §6.4.4.3: \"An inline comment may be written on any line of a compilation group except on a line that "
        + "contains a floating literal continuation indicator.\" Move the comment to its own line.",
        "ISO §6.3.7.3 / §6.4.4.3");

    /// <summary>COBOLNET2690 — a free-form line of a continued literal that holds no literal content (kb/Work PB1359).
    /// §6.4.2.</summary>
    public static readonly DiagnosticDescriptor LiteralContinuationPartEmpty = new(
        "COBOLNET2690", "literal-continuation-part-empty", EditionSeverity.Error,
        "The continued line or a continuation line of a literal holds no literal content. ISO §6.4.2: \"At least one "
        + "alphanumeric character, national character, or hexadecimal digit of the literal content shall be specified "
        + "on the continued line and on each continuation line.\" Move content onto the line, or end the literal "
        + "on the previous one.", "ISO §6.4.2");

    /// <summary>COBOLNET2691 — a compiler directive written after program text on its line (kb/Work PB1690).
    /// §7.3.3 SR2. Before, the line stayed program text and the parser rejected the <c>&gt;&gt;</c> as a stray
    /// character (COBOL0001) without naming the directive or the rule.</summary>
    public static readonly DiagnosticDescriptor DirectiveAfterProgramText = new(
        "COBOLNET2691", "directive-after-program-text", EditionSeverity.Error,
        "A compiler directive is written after program text on its line. ISO §7.3.3 SR2: \"A compiler directive "
        + "shall be preceded only by zero, one, or more space characters.\" Write the directive on a line of its own.",
        "ISO §7.3.3 SR2");

    /// <summary>COBOLNET2692 — one user-defined word used as two TYPES of user-defined word within a source element
    /// (kb/Work PB990). §8.3.2.2: "Within a source element, a given user-defined word may be used as only one type
    /// of user-defined word" — with three exceptions the compiler holds as data (<c>UserWordKinds.MayBeOneWord</c>): a
    /// compilation-variable-name, a level-number beside a paragraph- or section-name, and the constant-name /
    /// data-name / property-name / record-key-name / record-name group. Decided at the ONE declaration funnel
    /// (<c>DataBinder.DeclareUserWord</c>), so every declaring construct is covered.</summary>
    public static readonly DiagnosticDescriptor UserWordTypeConflict = new(
        "COBOLNET2692", "user-word-type-conflict", EditionSeverity.Error,
        "A user-defined word is used as two different types of user-defined word in one source element — for example "
        + "an alphabet-name that is also a data-name, a paragraph-name that is also a file-name, or a section-name "
        + "that is also a condition-name. ISO §8.3.2.2: \"Within a source element, a given user-defined word may be "
        + "used as only one type of user-defined word\"; the exceptions are a compilation-variable-name, a "
        + "level-number that matches a paragraph-name or section-name, and the one group of constant-name, "
        + "data-name, property-name, record-key-name and record-name. Rename one of the declarations.",
        "ISO §8.3.2.2");

    /// <summary>COBOLNET2693 — a user-defined word with no letter (kb/Work PB1403). §8.3.2.2: "With the exception of
    /// section-names, paragraph-names, and level-numbers, each user-defined word shall contain at least one basic
    /// letter or extended letter." The lexer must admit a letterless token (<c>1-2</c> is a legal paragraph-name), so
    /// the rule is asked at the ONE declaration funnel (<c>DataBinder.DeclareUserWord</c>), for every type but
    /// those three.</summary>
    public static readonly DiagnosticDescriptor UserWordWithoutLetter = new(
        "COBOLNET2693", "user-word-without-letter", EditionSeverity.Error,
        "A user-defined word that is not a section-name, paragraph-name or level-number contains no letter — for "
        + "example the data-name 1-2. ISO §8.3.2.2: \"With the exception of section-names, paragraph-names, and "
        + "level-numbers, each user-defined word shall contain at least one basic letter or extended letter.\" "
        + "Add a letter to the name.", "ISO §8.3.2.2");

    /// <summary>COBOLNET2696 — a compiler directive continued onto a following line (kb/Work PB1360). §7.3.3 SR1: every
    /// directive is specified on ONE line (the IF and EVALUATE directives' own lines too, §7.3.16.3 SR1 / §7.3.13.3 SR2),
    /// so a fixed continuation line or a floating-continued literal that follows a directive line is not part of it.
    /// Before, the §6.5 join appended the line to the directive and its operand grew past what the author wrote.</summary>
    public static readonly DiagnosticDescriptor DirectiveContinued = new(
        "COBOLNET2696", "directive-continued", EditionSeverity.Error,
        "A compiler directive is continued onto a following line. ISO §7.3.3 SR1: \"A compiler directive shall be "
        + "specified on one line, except for the EVALUATE and the IF directives for which specific rules are specified\" "
        + "(and each of those is itself specified entirely on its line). Write the whole directive on one line.",
        "ISO §7.3.3 SR1");

    /// <summary>COBOLNET2697 — a compiler directive written within a COPY or REPLACE statement (kb/Work PB1384).
    /// §7.3.3 SR8 b). The merged text-manipulation driver ends its text block at every directive line, so the
    /// statement's later words used to reach COPY resolution truncated (no text-name, or a missing period) or the REPLACE
    /// scan unseen; the directive itself was accepted.</summary>
    public static readonly DiagnosticDescriptor DirectiveWithinTextManipulationStatement = new(
        "COBOLNET2697", "directive-within-text-manipulation-statement", EditionSeverity.Error,
        "A compiler directive is specified within a COPY or REPLACE statement. ISO §7.3.3 SR8: \"A compiler directive "
        + "may be specified anywhere in a compilation group, in source text or in library text, except ... b) within "
        + "a source text manipulation statement\". Finish the statement with its separator period first, or move "
        + "the directive before it.", "ISO §7.3.3 SR8 b)");

    /// <summary>COBOLNET2698 — a DISPLAY directive's UPON phrase (kb/Work PB807): a word that is no OUTPUT-capable
    /// device-name of the implementor-name table and is not LISTING (§7.3.12.4 GR5 b), DOC-A.1-54), or an UPON phrase
    /// that writes LISTING twice or two separate runs of devices (§7.3.12.2's braces carry §5.2.6.4 choice indicators:
    /// each alternative at most once).</summary>
    public static readonly DiagnosticDescriptor DisplayDirectiveUpon = new(
        "COBOLNET2698", "display-directive-upon", EditionSeverity.Error,
        "The UPON phrase of a DISPLAY directive names a device this implementation does not make available as "
        + "compile-time-device-1, or repeats an alternative. ISO §7.3.12.4 GR5: data is transferred to the same device as "
        + "source listings (LISTING) or to the device the implementor defines for compile-time-device-1 (an output "
        + "device-name: CONSOLE, SYSOUT or SYSERR); §5.2.6.4 admits each choice-indicator alternative at most once.",
        "ISO §7.3.12.2 / §7.3.12.4 GR5");

    /// <summary>COBOLNET2450 — COPY literal-1 / literal-2 that is not a plain alphanumeric literal (kb/Work PB1354):
    /// a concatenation expression or figurative constant (§7.2.3.3 SR4), or a national or boolean literal (SR5
    /// "Literal-1 and literal-2 shall be alphanumeric literals").</summary>
    public static readonly DiagnosticDescriptor CopyLiteralForm = new(
        "COBOLNET2450", "copy-literal-form", EditionSeverity.Error,
        "COPY literal-1 or literal-2 is a concatenation expression, a figurative constant, or a literal that is not "
        + "alphanumeric; the library text is named by a text-name or a single alphanumeric literal.",
        "ISO §7.2.3.3 SR4 / SR5");

    /// <summary>COBOLNET2451 — a COPY statement in a place §7.2.3.3 forbids (kb/Work PB1354): within another COPY
    /// statement (SR1 — in its header or its REPLACING operands, where it would otherwise have been spliced and
    /// expanded), or not preceded by a space (SR2).</summary>
    public static readonly DiagnosticDescriptor CopyStatementPlacement = new(
        "COBOLNET2451", "copy-statement-placement", EditionSeverity.Error,
        "A COPY statement appears within a COPY statement, or is not preceded by a space.",
        "ISO §7.2.3.3 SR1 / SR2");

    /// <summary>COBOLNET2572 — a COPY REPLACING or REPLACE operand whose CONTENT breaks the operand syntax rules
    /// (kb/Work PB1353). The rules are written twice, word for word — §7.2.3.3 SR6/SR9/SR10/SR11/SR12/SR13 (COPY) and
    /// §7.2.4.3 SR3/SR9/SR10/SR5/SR6/SR7 (REPLACE) — and checked by ONE screen (CopyProcessor.ScreenOperandPair).
    /// Before, an empty or comma-only pseudo-text-1 was silently dropped, a multi-word partial-word used only its first
    /// word, and a literal partial-word, a directive line inside pseudo-text or an over-long text-word compiled.</summary>
    public static readonly DiagnosticDescriptor ReplacingOperandContent = new(
        "COBOLNET2572", "replacing-operand-content", EditionSeverity.Error,
        "A REPLACING operand of a COPY or REPLACE statement breaks an operand syntax rule: pseudo-text-1 holds no "
        + "text-word other than a separator comma or semicolon; partial-word-1 is not exactly one text-word, or "
        + "partial-word-2 more than one; a partial-word is an alphanumeric, boolean or national literal; a compiler "
        + "directive line is written inside an operand; or a text-word is longer than 65,535 characters.",
        "ISO §7.2.3.3 SR6 / SR9–SR13; §7.2.4.3 SR3 / SR5–SR7 / SR9 / SR10");

    /// <summary>COBOLNET2573 — a pseudo-text delimiter that is not separated as §8.3.5 6) requires (kb/Work PB1353):
    /// an opening <c>==</c> not immediately preceded by a space, or a closing <c>==</c> not immediately followed by a
    /// space, comma, semicolon or period (<c>==XX==BY</c>, <c>BY==YY==</c>).</summary>
    public static readonly DiagnosticDescriptor PseudoTextDelimiterPlacement = new(
        "COBOLNET2573", "pseudo-text-delimiter-placement", EditionSeverity.Error,
        "A pseudo-text delimiter is not separated from its neighbour: an opening == shall be immediately preceded by a "
        + "space, and a closing == immediately followed by a space, comma, semicolon or period.",
        "ISO §8.3.5 6) / 8) c)");

    /// <summary>COBOLNET2574 — a replacing action that produces text its statement's rule forbids (kb/Work PB1356):
    /// §7.2.3.4 GR13 (COPY: a COPY statement, a SOURCE FORMAT directive, a comment or a blank line) and §7.2.4.4 GR9
    /// (REPLACE: the same, and a REPLACE statement). Checked on the text each match produces — a pseudo-text-2 once
    /// per operand pair, a LEADING/TRAILING result word per match (CopyProcessor.ForbiddenIn). A blank line cannot be
    /// produced: logical conversion discarded every blank line before text manipulation (§6.5 2)). Before, such text
    /// was spliced and processed (or silently lost downstream).</summary>
    public static readonly DiagnosticDescriptor ReplacingResultForbidden = new(
        "COBOLNET2574", "replacing-result-forbidden", EditionSeverity.Error,
        "The replacing action of a COPY or REPLACE statement produces a COPY statement, a REPLACE statement (REPLACE "
        + "only), a SOURCE FORMAT directive, or a comment.",
        "ISO §7.2.3.4 GR13 / §7.2.4.4 GR9");

    // ── The INVOKE twins of the PB132 CALL operand screens (kb/Work PB1137): the rules are written in the same
    // words for both activation statements and are checked by ONE screen (ActivationOperandScreen); each statement
    // keeps its own code because each has its own clause, exactly as COBOLNET2237 twins COBOLNET1685. ──
    /// <summary>COBOLNET2452 — an INVOKE BY REFERENCE argument (identifier-3) or RETURNING item (identifier-4)
    /// that is not a data item of the four storage sections (kb/Work PB1137).</summary>
    public static readonly DiagnosticDescriptor InvokeOperandSection = new(
        "COBOLNET2452", "invoke-operand-section", EditionSeverity.Error,
        "An INVOKE argument passed BY REFERENCE shall be an address-identifier or reference a data item defined in "
        + "the file, working-storage, local-storage, or linkage section (ISO §14.9.23.3 syntax rule 9), and the "
        + "RETURNING item shall reference such a data item (rule 11). A special register (PAGE-COUNTER, "
        + "EXCEPTION-OBJECT) is not one; an object property is admitted by §8.4.3.9.3 SR5/SR6. A keyword-less "
        + "argument that is not such an item is passed BY CONTENT instead (§14.9.23.4 GR6 a) 2.) — except through a "
        + "universal object reference, where BY REFERENCE is the only mode (SR6).",
        "ISO §14.9.23.3 SR9/SR11");

    /// <summary>COBOLNET2453 — a misaligned bit data item as an INVOKE BY REFERENCE argument or RETURNING item
    /// (kb/Work PB1137; the INVOKE twin of COBOLNET1683).</summary>
    public static readonly DiagnosticDescriptor InvokeBitAlignment = new(
        "COBOLNET2453", "invoke-bit-alignment", EditionSeverity.Error,
        "A bit data item referenced by an INVOKE BY REFERENCE argument (identifier-3) or RETURNING item "
        + "(identifier-4) shall be aligned on a byte boundary, and its subscripts and reference-modification "
        + "leftmost position shall consist of only fixed-point numeric literals or all-literal arithmetic "
        + "expressions without exponentiation — the referenced address must be statically byte-aligned.",
        "ISO §14.9.23.3 SR12 / §8.5.1.6.3");

    /// <summary>COBOLNET2454 — a zero-length literal-2 INVOKE argument (kb/Work PB1137).</summary>
    public static readonly DiagnosticDescriptor InvokeArgumentZeroLengthLiteral = new(
        "COBOLNET2454", "invoke-argument-zero-length-literal", EditionSeverity.Error,
        "ISO §14.9.23.3 syntax rule 17: \"Literal-2 shall not be a zero-length literal.\" It binds every INVOKE "
        + "argument literal and, through §8.4.3.4.3 SR3, every argument of an inline method invocation.",
        "ISO §14.9.23.3 SR17");

    public static readonly DiagnosticDescriptor UndefinedReference = new(
        "COBOLNET1639", "undefined-reference", EditionSeverity.Error,
        "A statement or clause references a name that no declaration in the source element defines, or that the "
        + "written qualifiers (or an unqualified ambiguity) leave unidentified. ISO §8.4.2.1: \"In order to use "
        + "a resource, a statement shall contain a reference that uniquely identifies that resource\"; §8.4.2.2 "
        + "requires qualification to establish uniqueness when spellings collide. Two sites report it: the ONE "
        + "procedure-division chokepoint (ReferenceResolver.Resolve — kb/Work R30), and the VALUE clause's "
        + "literal-position screen, where the only names the position admits are a constant-name (§13.10.3 SR2) "
        + "and a symbolic-character (§8.3.3.6.2 Format 7), so a word that is neither identifies no resource "
        + "(kb/Work PB732). It also carries the PROCEDURE-NAME space (kb/Work PB390): a word that names no "
        + "paragraph or section of this source element identifies no procedure, at PERFORM (ISO §14.9.28.3 "
        + "SR12 / SR13, quoted by the message), GO TO in both formats, ALTER, RESUME AT and the SORT/MERGE "
        + "INPUT and OUTPUT PROCEDURE phrases — ONE reporting step, "
        + "ProcedureTableBuilder.ResolveProcedureOperand, since §8.4.6.1 makes paragraph-names and "
        + "section-names referenceable only in the source element that declares them; and the SET Format-4 "
        + "operand that names no condition-name at all (§14.9.39.3 SR6). It also carries the CLASS CONDITION's "
        + "user-defined-word position (kb/Work PB590): §8.8.4.4.2 offers exactly two there — alphabet-name-1 "
        + "and class-name-1 — so a word that declares neither identifies no resource. That arm used to fall to "
        + "a loud RUN-TIME stage, so `IF X IS NOSUCHCLASS` compiled with no diagnostic at all.",
        "ISO §8.4.2.1 / §8.4.2.2");
    // ⛔ THE TWO PROCEDURE-NAME UNIQUENESS RULES (kb/Work PB466). COBOLNET1639 above is "identifies NO
    // resource"; these two are "identifies MORE THAN ONE", and they are separate codes because the two rules
    // have different repairs: rule-1 ambiguity is cured by writing the qualifier the standard asks for, and an
    // SR7 in-section duplicate cannot be qualified at all — the section-name is the only qualifier a
    // paragraph-name takes, and both declarations already carry it, so one of them has to be renamed.
    public static readonly DiagnosticDescriptor AmbiguousProcedureName = new(
        "COBOLNET2121", "ambiguous-procedure-name", EditionSeverity.Error,
        "An explicitly referenced procedure-name does not identify exactly one procedure. ISO §8.4.2.2.1: "
        + "\"Identical user-defined names may be specified in a source unit; however, uniqueness shall be "
        + "established through qualification for each user-defined name explicitly referenced, except as "
        + "specified in rules 2 through 6.\" Rule 1 — \"No other name has the identical spelling\" — is false "
        + "for the reference, and rule 6, the only excuse that reaches a paragraph-name (\"The name is a "
        + "paragraph-name and the section containing the reference also contains the named paragraph\"), does "
        + "not apply; §8.4.2.2.3 SR1 then requires \"a sequence of qualifiers that precludes any ambiguity of "
        + "reference\". Write the qualifier (paragraph-name IN section-name — §8.4.2.2.2 format 4), or rename "
        + "one of the duplicated procedures. Duplicated SECTION-names cannot be qualified at all, since a "
        + "section-name takes no qualifier, so those shall be renamed. Reported at every statement whose "
        + "general format prints procedure-name — PERFORM (both operands of a THRU range), GO TO in both "
        + "formats, ALTER, RESUME AT and the SORT/MERGE INPUT and OUTPUT PROCEDURE phrases — from the ONE "
        + "resolution step, ProcedureTableBuilder.ResolveProcedureOperand.",
        "ISO §8.4.2.2.1 / §8.4.2.2.3 SR1");
    public static readonly DiagnosticDescriptor ParagraphNameDuplicatedInSection = new(
        "COBOLNET2122", "paragraph-name-duplicated-in-section", EditionSeverity.Error,
        "A paragraph-name that is explicitly referenced is declared more than once within one section. ISO "
        + "§8.4.2.2.3 SR7: \"If explicitly referenced, a paragraph-name shall not be duplicated within a "
        + "section.\" Qualification cannot repair this one — §8.4.2.2.2 format 4 offers a paragraph-name only "
        + "its section-name as a qualifier and both declarations carry the same one — so one of the two "
        + "paragraphs shall be renamed. A duplicated paragraph-name that is never referenced is legal and is "
        + "not reported: SR7 is conditioned on the explicit reference, which is why the check lives at the "
        + "reference and not at the declaration.",
        "ISO §8.4.2.2.3 SR7");
    // ⛔ COBOLNET1576 ("ref-mod-zero-length-malformed-operand") is RETIRED — NEVER REALLOCATE IT. It was
    //    >>REF-MOD-ZERO-LENGTH's own copy of "a directive's operand shall be one its general format admits"
    //    (ISO §7.3.3 SR6), one of six such codes; kb/Work PB794 made the rule DATA on the directive's
    //    constructs.json row and gave the whole family ONE producer, COBOLNET1911. (It had itself been
    //    renumbered from a bare-literal "COBOLNET1573" that collided with ExternalFileStatusConsistency above —
    //    the P13 plan-vs-spec review finding C1, DEVLOG 907 — because the frontend emit bypassed this catalog;
    //    the central producer ends that class of collision for every directive at once.) The COBOLNET0875 and
    //    COBOLNET1518 precedents apply: a retired code stays retired.
    // 1577 renumbered FROM a bare-literal "COBOLNET1518" in DataBinder that collided with the A.4.9 locale-module
    // non-support meaning (the P13 review batch-3 finding V11 — the THIRD collision of the class; 1518 stays
    // solely = locale non-support as CONFORMANCE.md item 25 documents).
    public static readonly DiagnosticDescriptor MethodRedefinesScope = new(
        "COBOLNET1577", "method-redefines-scope", EditionSeverity.Error,
        "A method data item's REDEFINES target shall be a preceding item in the SAME method scope — a method "
        + "item may not redefine object or program data (ISO §13.18.44.3).",
        "ISO §13.18.44.3");
    // ── THE DECLINED-OPTIONAL-ELEMENT BAND (§4.2.6 ¶3 / §4.2.7 / §4.2.13 / Annex A.3–A.4) ─────────────────
    //    ONE mechanism, TWO dispositions, and the descriptor's own Severity is what chooses between them —
    //    every site routes through `EditionContext.Declined(descriptor, seen)`, never a local strictness test
    //    and never a second helper (feedback_one_mechanism_per_job).
    //
    //    • ACCEPT-INERT (Warning) — COBOLNET1578 / 1579 / 1580 / 1778. These facilities are ADDITIVE: the
    //      program still means what it says with them absent (no message I-O, COMMIT behaves as
    //      CONTINUE, VALIDATE validates nothing, a RECORD DELIMITER selects a framing §12.4.5.11.4 GR1
    //      forbids the program from seeing), so the program COMPILES, RUNS, and the facility is inert, with
    //      §14.6.13.1.1 licensing NO exception conditions. Before this band these constructs produced a
    //      GENERIC parse error, which satisfied neither obligation below nor the "never a silent wrong
    //      answer" rule.
    //      ⛔ TWO DIFFERENT LICENCES REACH THIS ONE DISPOSITION, and conflating them is kb/Work PB709's
    //      defect — the descriptor's `Annex` datum is what tells them apart now, and `PostureClause` derives
    //      the § from it so that no site writes the clause by hand:
    //        · 1578 (MCS, Annex A.3 item 4), 1579 (commit and rollback, A.3 items 6-7) and 1778 (RECORD
    //          DELIMITER, A.3 item 26) are PROCESSOR-DEPENDENT, and §4.2.6 ¶3 makes the warning MANDATORY:
    //          "shall provide a warning mechanism at compile time to indicate use of syntactically-detectable
    //          processor-dependent language elements not supported". There the warning IS the obligation.
    //        · 1580 (VALIDATE) is NOT in Annex A.3 at all — it is Annex A.4.14, OPTIONAL, and §4.2.7 carries
    //          no warning-mechanism sentence to mandate anything. What §4.2.7 requires is that the
    //          non-support be identified in user documentation (docs/CONFORMANCE.md §4 item 3 and §5 row
    //          A.4.14); naming the statement instead of failing the compile is this implementation's CHOICE,
    //          taken because the facility is additive and §4.2.13 additionally makes it obsolete at
    //          COBOL-2023. Citing §4.2.6 for it would claim a mandate the standard does not give AND
    //          misdescribe the posture: a decline under the processor-dependent clause reads "we could not",
    //          a decline under the optional clause reads "we chose not to, and documented it".
    //
    //    • REFUSE (Error) — COBOLNET1560 / 1705 / 1706 / 1707 / 1954. These facilities are NOT additive:
    //      compiled inert they change the ANSWER (which bytes reach the medium; which record description entry
    //      is selected; a whole-record-area write standing in for a §14.9.51.4 GR8 implicit record; a screen
    //      ACCEPT re-read as the device format, which transfers the wrong data; a prime record key that names
    //      no data item at all). A.4.1's first sentence is the licence to refuse an OPTIONAL element: "An
    //      implementation shall accept the syntax and provide the functionality for an optional element only
    //      when support for that language element is claimed by the implementor."
    //      Unclaimed ⇒ the syntax is not accepted. The strictness axis does NOT move these: --permissive is
    //      the REMOVED-construct / documented-leniency migration seam (EditionContext.Removed) and there is no
    //      "pre-removal semantics" to preserve here — matching the accept-inert rows above, which --permissive
    //      likewise does not move.
    //      ⛔ THE DISPOSITION SPLITS ON WHETHER AN INERT READING EXISTS, **NOT** ON WHICH ANNEX LISTS THE
    //      ELEMENT, and 1954 is what proves it (kb/Work PB358, 2026-09-09). Until it landed, every REFUSE row
    //      happened to be Annex A.4 and every ACCEPT-INERT row Annex A.3, and this header and
    //      EditionContext.Declined both wrote that coincidence down as if it were the rule ("an Annex A.3
    //      processor-dependent facility is accepted-and-warned while an Annex A.4 optional module is
    //      refused"). COBOLNET1954 is an **A.3** element (item 40, the SOURCE phrase of RECORD KEY /
    //      ALTERNATE RECORD KEY) that must be REFUSED, because a record-key-name declared by a SOURCE phrase
    //      names no data item — accept it inert and the file has no prime key, which is a wrong answer and not
    //      an absent facility. §4.2.6 ¶3 licenses exactly that in its own closing sentence: "The implementor is
    //      not required to produce executable code when unsupported processor-dependent language elements are
    //      used." The annex still decides the POSTURE CLAUSE the message cites (DiagnosticDescriptor.Annex →
    //      PostureClause: §4.2.6 for A.3, §4.2.7 for A.4); it does not decide the severity.
    public static readonly DiagnosticDescriptor McsFacilityUnsupported = new(
        "COBOLNET1578", "mcs-facility-unsupported", EditionSeverity.Warning,
        "The asynchronous messaging facility (SEND/RECEIVE, ISO §14.9.31/§14.9.38) is a processor-dependent "
        + "element (§4.2.6; Annex A.3 item 4) that is not supported — the statement is accepted but performs no "
        + "message I-O, and no EC-MCS-* condition is raised (§14.6.13.1.1). See docs/CONFORMANCE.md §4.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 4 / §14.9.31 / §14.9.38", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A3);
    public static readonly DiagnosticDescriptor CommitRollbackUnsupported = new(
        "COBOLNET1579", "commit-rollback-unsupported", EditionSeverity.Warning,
        "The commit and rollback facility (COMMIT/ROLLBACK, ISO §14.9.7/§14.9.36) is a processor-dependent "
        + "element (§4.2.6; Annex A.3 items 6-7) that is not supported — the statement is accepted but performs "
        + "no transaction control and behaves as CONTINUE, and no EC-FLOW-COMMIT/ROLLBACK condition is raised "
        + "(§14.6.13.1.1). See docs/CONFORMANCE.md §4.",
        "ISO §4.2.6 ¶3 / §4.2.7 / Annex A.3 items 6-7 / Annex A.4.3 items 4-5 / §14.9.7 / §14.9.36",
        RecognizedNotImplemented, Annex: DeclinedAnnex.A3 | DeclinedAnnex.A4);
    public static readonly DiagnosticDescriptor ValidateFacilityUnsupported = new(
        "COBOLNET1580", "validate-facility-unsupported", EditionSeverity.Warning,
        "The VALIDATE facility (ISO §14.9.50) is an OPTIONAL element (§4.2.7; Annex A.4.14) and, at COBOL-2023, "
        + "additionally OBSOLETE (§4.2.13; Annex F.2 item 5) — it is not supported. The statement is accepted "
        + "but performs no content validation, and no EC-VALIDATE-* condition is raised (§14.6.13.1.1). Fires at "
        + "2002/2014/2023 (the facility exists from 2002); at --std 85 VALIDATE is a user word, not a statement. "
        + "See docs/CONFORMANCE.md §4.",
        "ISO §4.2.7 / Annex A.4.14 / §4.2.13 / Annex F.2 item 5 / §14.9.50", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A4);
    // ⛔ ONE CODE FOR THE WHOLE CLAUSE, BOTH ARMS, and that is the point (kb/Work PB292). The general format
    //    (§12.4.5.11.2, rendered from the printed page) is a plain required choice — `RECORD DELIMITER IS
    //    { STANDARD-1 | feature-name-1 }` — and the two alternatives are declined on DIFFERENT grounds that
    //    reach the SAME disposition, so splitting them into two codes would put one rule in two places
    //    (feedback_one_rule_one_place) and invite the two-arm defect this note was opened for: before this
    //    descriptor existed, `recordDelimiterClause` parsed and NOTHING read it, at any edition, on either arm.
    //    · STANDARD-1 is PROCESSOR-DEPENDENT — Annex A.3 item 26, "The STANDARD-1 phrase of the RECORD
    //      DELIMITER clause is dependent upon a reel type of device", and §12.4.5.11.4 GR2 spells the
    //      dependency as a requirement on the run: "If STANDARD-1 is specified, the external medium shall be a
    //      tape drive." WiseOwl COBOL provides no reel device (docs/CONFORMANCE.md §2 rows 28-30, 33-34), so
    //      §12.4.5.11.4 GR3's ISO/IEC 1001:2012 7.1.2 framing cannot be reached.
    //    · feature-name-1 is IMPLEMENTOR-DEFINED and OPTIONAL — Annex A.1 item 150, "This item is optional",
    //      whose licence is A.1's preamble ("Optional: The element may be provided at the implementor's
    //      option"). §12.4.5.11.3 SR2 makes the available names the implementor's to specify and this
    //      implementation specifies NONE (docs/CONFORMANCE.md §7, A.1 item 150), so §12.4.5.11.4 GR4 has no
    //      method to associate with any spelling.
    //    ACCEPT-INERT (Warning), not refuse, and §12.4.5.11.4 GR1 is why the accept is safe where an inert
    //    FORMAT clause (1705) is not: "Any method used shall not be reflected in the record area or the record
    //    size used within the function, method, or program" — the framing is invisible to the program's data,
    //    so a declined RECORD DELIMITER leaves every program-visible value identical to the conforming one and
    //    the file still round-trips through §12.4.5.11.4 GR5's implementor method (the 4-byte length prefix,
    //    A.1 item 151, docs/CONFORMANCE.md §7). §4.2.6 ¶3 then makes the warning MANDATORY rather than a house
    //    style: "An implementation shall provide a warning mechanism at compile time to indicate use of
    //    syntactically-detectable processor-dependent language elements not supported by that implementation."
    //    ⚠ §4.2.6 ¶3's next sentence — "it is not required to diagnose syntax errors within this unsupported
    //    syntax" — is the express licence for NOT enforcing §12.4.5.11.3 SR1 (the clause "may be specified only
    //    for variable-length records") inside the declined clause. That is a decision, not an omission: SR1's
    //    own NOTE makes variable-length-ness partly an implementor determination (A.1 items 147-148, still
    //    open), and a line-sequential file's records are variable by §9.1.7.2 with no RECORD clause at all, so
    //    an SR1 test keyed on FileModel.Varying would REJECT LEGAL SOURCE to enforce a rule the standard
    //    excuses.
    public static readonly DiagnosticDescriptor RecordDelimiterUnsupported = new(
        "COBOLNET1778", "record-delimiter-unsupported", EditionSeverity.Warning,
        "the RECORD DELIMITER clause (ISO §12.4.5.11) selects the method of determining a variable-length "
        + "record's length on the external medium, and neither alternative of its required choice is supported. "
        + "STANDARD-1 is a processor-dependent element (§4.2.6; Annex A.3 item 26) whose §12.4.5.11.4 GR2 "
        + "medium is a tape drive, and WiseOwl COBOL provides no reel device; no feature-name is available either "
        + "(§12.4.5.11.3 SR2 leaves the names to the implementor and Annex A.1 item 150 makes providing them "
        + "optional — this implementation provides none). The clause is ACCEPTED and has no effect: every "
        + "variable-length record is framed by the §12.4.5.11.4 GR5 implementor method (the 4-byte length "
        + "prefix, Annex A.1 item 151), and §12.4.5.11.4 GR1 keeps that framing out of the record area and the "
        + "record size, so no program-visible value changes. See docs/CONFORMANCE.md §2 row 26 and §7.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 26 / Annex A.1 items 150-151 / §12.4.5.11", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A3);
    // ⛔ ONE CODE FOR **BOTH CLAUSES**, and that is the point (kb/Work PB358 / PB293). Annex A.3 item 40 names
    //    the RECORD KEY clause and the ALTERNATE RECORD KEY clause in ONE sentence — "The capability of
    //    specifying the SOURCE phrase of the RECORD KEY clause and ALTERNATE RECORD KEY clause is dependent on
    //    the capabilities of the processor" — and §12.4.5.12.2 and §12.4.5.6.2 print the SAME brace group
    //    (`{ data-name-1 | record-key-name-1 SOURCE IS { data-name-2 } … }`). One rule, one place
    //    (feedback_one_rule_one_place); a second code would invite the two-arm defect this note was opened for,
    //    which is exactly how the construct got here — the original finding named only the prime key.
    // ⛔ REFUSE, not accept-inert, and the difference from COBOLNET1778 above is NOT the annex (both are A.3)
    //    but whether an inert reading exists. §12.4.5.12.4 GR2 / §12.4.5.6.4 GR2: "Record-key-name-1 defines a
    //    record key consisting of the concatenation of all occurrences of data-name-2 in the order specified."
    //    record-key-name-1 is its own name class (§8.3.2.2.24, scoped by §8.4.6.2.4) and names no data item, so
    //    there is nothing for an inert compile to use as the key: the file would have NO prime key, every
    //    keyed READ/START/WRITE would be resolved against nothing, and the program would produce a wrong
    //    answer rather than merely lack a facility. §4.2.6 ¶3's closing sentence is the licence: "The
    //    implementor is not required to produce executable code when unsupported processor-dependent language
    //    elements are used."
    // ⛔ WHAT PROVIDING IT WOULD COST is recorded in kb/Work PB293, NOT as a proposal: a record key would
    //    become a LIST of byte windows end-to-end — FileModel's key model, KeyedIoEmitter.EmitRegistration,
    //    RuntimeApi.FileRegisterIndexed/FileAddAlternateKey, IndexedConnector's single `(Off, Len)` slice,
    //    RecordLayout.KeyIndexOfKeyItem (which matches by STORAGE POSITION and cannot express a
    //    concatenation), FixedFileAttributes.KeyDescriptor and therefore the framed store's on-disk header
    //    (docs/COBOLNET_FILES_DESIGN.md D10 — a second owner-visible file-format break), plus the §12.4.5.7
    //    Format-2 per-key COLLATING SEQUENCE seam. The standard makes the element optional; this
    //    implementation declines it and says so.
    // EVERY EDITION. SOURCE is reserved at 85/2002/2014/2023 (tests/version-matrix/reserved-words.json), the
    //    element is provided at none of them, and a construct that never compiles has no edition window — which
    //    is why there is no tests/version-matrix/constructs.json row (that file models `introducedIn` /
    //    `removedIn`), matching COBOLNET1778's precedent.
    public static readonly DiagnosticDescriptor RecordKeySourcePhraseUnsupported = new(
        "COBOLNET1954", "record-key-source-phrase-unsupported", EditionSeverity.Error,
        "the SOURCE phrase of the RECORD KEY / ALTERNATE RECORD KEY clause (ISO §12.4.5.12.2 / §12.4.5.6.2) "
        + "declares a record-key-name whose key is the concatenation of one or more data-name-2 operands "
        + "(§12.4.5.12.4 GR2 / §12.4.5.6.4 GR2). It is a processor-dependent element (§4.2.6; Annex A.3 item "
        + "40 — \"The capability of specifying the SOURCE phrase of the RECORD KEY clause and ALTERNATE RECORD "
        + "KEY clause is dependent on the capabilities of the processor\") that this implementation does not "
        + "provide: a record key here is one contiguous byte window of the record, never a concatenation of "
        + "several. The clause is REFUSED rather than accepted inert, because record-key-name-1 is its own name "
        + "class (§8.3.2.2.24) and names no data item — an inert compile would leave the file with no key at "
        + "all. Write the single-item form instead: RECORD KEY IS data-name-1. See docs/CONFORMANCE.md §2 row "
        + "40.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 40 / §12.4.5.12.2 / §12.4.5.6.2", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A3);
    // ── 1560 / 1705 / 1706 / 1707 — the REFUSE half of the band (see the header above). One code per A.4
    //    MODULE (A.4.2 takes two, split at the division boundary — see the header below it), not per
    //    clause: the module is the unit §4.2.7 makes the implementor document and Annex A.4 makes optional, so
    //    a user who reads CONFORMANCE.md §5 finds exactly one row per code. The `seen` half of the message is
    //    composed at the site and names WHICH element of the module was written.
    public static readonly DiagnosticDescriptor FormatSelectWhenUnclaimed = new(
        "COBOLNET1705", "a48-format-select-when-unclaimed", EditionSeverity.Error,
        "the FORMAT clause (ISO §13.18.24) and the SELECT WHEN clause (ISO §13.18.51) are the two items of "
        + "Annex A.4.8, an OPTIONAL language element (§4.2.7) for which this implementation claims NO support "
        + "(docs/CONFORMANCE.md §5, row A.4.8) — Annex A.4.1: an implementation shall accept the syntax for an "
        + "optional element ONLY when support for it is claimed, so the clause is refused rather than accepted "
        + "inert. Refused at EVERY edition: an inert FORMAT changes which bytes reach the medium "
        + "(§13.18.24.4 GR1) and an inert SELECT WHEN selects the wrong record description entry "
        + "(§13.18.51.4 GR1) with an I-O status 45 path (§9.1.13.7 rule 5) — a wrong answer, not a missing "
        + "facility, which is why this is an Error and not the COBOLNET1578/1579/1580 accept-inert band. "
        + "At --std 85 the FORMAT clause additionally cannot be written at all: §8.9 reserves FORMAT only from "
        + "2002, so there the word is a user-defined name.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.8 items 1-2 / §13.18.24 / §13.18.51",
        Annex: DeclinedAnnex.A4);
    public static readonly DiagnosticDescriptor WriteRewriteFileUnclaimed = new(
        "COBOLNET1706", "a413-write-rewrite-file-unclaimed", EditionSeverity.Error,
        "the FILE phrase of the WRITE statement (ISO §14.9.51) and of the REWRITE statement (ISO §14.9.35) — "
        + "`WRITE FILE file-name-1 FROM …` / `REWRITE FILE file-name-1 RECORD FROM …` — are the two items of "
        + "Annex A.4.13, an OPTIONAL language element (§4.2.7) for which this implementation claims NO support "
        + "(docs/CONFORMANCE.md §5, row A.4.13) — Annex A.4.1: an implementation shall accept the syntax for an "
        + "optional element ONLY when support for it is claimed. The two STATEMENTS are mandatory and fully "
        + "supported; only this one alternative of `{ record-name-1 | FILE file-name-1 }` is declined "
        + "(A.4.1 NOTE 1: the higher-level cross-referenced construct is not optional), so plain "
        + "`WRITE record-name-1` / `REWRITE record-name-1` are unaffected. Refused at EVERY edition and in "
        + "BOTH printed formats (§14.9.51.2 Format 1 sequential and Format 2 random): the declined phrase has "
        + "its own implicit-record semantics (§14.9.51.4 GR8, §14.9.35.4 GR9) that a whole-record-area write "
        + "does not implement, so accepting it inert would be a wrong answer.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.13 items 1-2 / §14.9.51 / §14.9.35",
        Annex: DeclinedAnnex.A4);
    // ── The A.4 DECLINED-OPTIONAL-ELEMENT band (§4.2.7 / Annex A.4.1). ⛔ A DIFFERENT OBLIGATION FROM THE
    //    WAVE-H BAND ABOVE, and the severity difference is the whole point. §4.2.6 ¶3 covers PROCESSOR-DEPENDENT
    //    elements (Annex A.3): we may accept them and must WARN — 1578/1579 do exactly that. Annex A.4.1 covers
    //    OPTIONAL elements: "An implementation shall accept the syntax and provide the functionality for an
    //    optional element ONLY when support for that language element is claimed by the implementor", so for a
    //    module docs/CONFORMANCE.md §5 records as Not claimed, ACCEPTING the syntax is itself the
    //    non-conformance — the element has to be REFUSED BY NAME. Severity therefore routes through the ONE
    //    EditionContext.Removed seam: Error under strict, Warning under --permissive (the migration mode), the
    //    shape kb/Work PB100 established for the locale module. Naming the module in the message is the point:
    //    MEASURED before this band, every one of these constructs drew a bare COBOL0001/COBOL0307 parse error
    //    (or, for the declined modules' EXCEPTION-NAMES, no diagnostic at all — they were ACCEPTED against
    //    families with zero setting sites), so a user learned their syntax was bad rather than that this
    //    implementation does not provide the facility.
    public static readonly DiagnosticDescriptor ValidateDataDivisionClauseUnsupported = new(
        "COBOLNET1708", "validate-data-division-clause-unsupported", EditionSeverity.Error,
        "A data-division clause of the VALIDATE facility (the §13.16.2 validation-clauses group — CLASS, "
        + "DEFAULT, DESTINATION, INVALID, PRESENT WHEN format 2, VARYING's validation leg, "
        + "VALIDATE-STATUS/VAL-STATUS — or the §13.18.63 format-5 content-validation entry) is written. The "
        + "VALIDATE facility is an OPTIONAL element (Annex A.4.14) whose support WiseOwl COBOL does not claim "
        + "(docs/CONFORMANCE.md §4 item 3, §5), and at COBOL-2023 it is additionally OBSOLETE (§4.2.13; Annex "
        + "F.2 item 5); Annex A.4.1 admits the syntax only when support IS claimed, so the clause is refused by "
        + "name. Every clause of the group exists from COBOL-2002. CLASS is the one whose leading word is "
        + "reserved BELOW that edition too (§12.3.7's SPECIAL-NAMES CLASS clause, continuous since 1985), so "
        + "at --std 85 `CLASS IS NUMERIC` in a data description entry is an ordinary syntax error rather than "
        + "this decline; for the others the word is a user-defined word there (§8.9). Annex A.4 does not list "
        + "CLASS among A.4.14's items — it reaches the module through §13.16.2's validation-clauses group, "
        + "which opens with `[ class-clause ]` (owner decision, kb/Work PB375, 2026-09-02).",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.14 / §13.16.2 / §4.2.13", DeclinedOptionalElement,
        PermissiveInert: true, Annex: DeclinedAnnex.A4);
    public static readonly DiagnosticDescriptor ApplyCommitClauseUnsupported = new(
        "COBOLNET1709", "apply-commit-clause-unsupported", EditionSeverity.Error,
        "The I-O-CONTROL paragraph's APPLY COMMIT clause (ISO §12.4.6.3) is written. The commit and rollback "
        + "facility is an OPTIONAL element (Annex A.4.3 item 2) and processor-dependent (Annex A.3 items 6-7) "
        + "whose support WiseOwl COBOL does not claim (docs/CONFORMANCE.md §4 item 2, §5) — there is no transaction "
        + "manager — so Annex A.4.1 makes refusing the clause by name the conforming posture. With no clause "
        + "accepted, no APPLY COMMIT clause is ever active, which is exactly the state §14.9.7.4 GR1 / "
        + "§14.9.36.4 GR1 make COMMIT and ROLLBACK behave as CONTINUE in. Introduced by COBOL-2023 (Annex E.3.2 "
        + "item 2); below that APPLY and COMMIT are user-defined words (§8.9 / §8.10).",
        "ISO §4.2.6 / §4.2.7 / Annex A.4.1 / Annex A.4.3 item 2 / Annex A.3 items 6-7 / §12.4.6.3 / Annex E.3.2 item 2",
        DeclinedOptionalElement, PermissiveInert: true, Annex: DeclinedAnnex.A3 | DeclinedAnnex.A4);
    public static readonly DiagnosticDescriptor DeclinedModuleExceptionName = new(
        "COBOLNET1710", "declined-module-exception-name", EditionSeverity.Error,
        "A written exception-name (>>TURN, RAISE, EXIT/GOBACK RAISING, a USE declarative, or an "
        + "exception-checking PERFORM's WHEN phrase) belongs to an OPTIONAL module whose support WiseOwl COBOL does "
        + "not claim, so no statement in this implementation can ever set that condition to exist. Annex A.4.1 "
        + "makes the module's exception conditions optional WITH it (\"Any associated syntax rules, general "
        + "rules, other rules, exception conditions, and I-O status values are also optional, even if not "
        + "explicitly listed\"), and §14.6.13.1.1 licenses raising nothing for an unimplemented optional "
        + "element — but neither licenses accepting the NAME, which would let a program check, declare and "
        + "match a condition that cannot occur. The message names the module.",
        "ISO §4.2.7 / Annex A.4.1 / §14.6.13.1.1", DeclinedOptionalElement, PermissiveInert: true,
        Annex: DeclinedAnnex.A4);
    // ⛔ `StrongGroupOrderingSignedLeaf` ("strong-group-ordering-signed-leaf") LIVED HERE AND IS GONE (kb/Work
    // PB1469). It staged an ordering over strongly-typed groups with a SIGNED numeric leaf because the relation was
    // a whole-group image comparison; ISO §8.8.4.2.12 is now implemented element by element
    // (`StrongGroupComparison`), so every leaf kind orders by its own rule and there is nothing left to stage.
    // What remains unbuilt is a MEMBER SHAPE the element walk cannot reach, not a leaf kind:
    public static readonly DiagnosticDescriptor StrongGroupComparisonMember = new(
        NotImplemented, "strong-group-comparison-member", EditionSeverity.Error,
        "A relation between strongly-typed group items reaches a member this implementation cannot yet compare "
        + "element by element (ISO §8.8.4.2.12 — \"each elementary item of the first operand is compared with the "
        + "corresponding elementary item of the second operand\"): a dynamic-capacity table (whose occurrences are "
        + "a run-time count per operand), an OCCURS DEPENDING ON object that is itself inside a table, or a member "
        + "in a REDEFINES storage tier the element walk has no access path to.", "ISO §8.8.4.2.12",
        RecognizedNotImplemented);

    // ── Annex A.4.2 — ACCEPT and DISPLAY SCREEN HANDLING, the largest DECLINED optional module (kb/Work PB260).
    //
    //    ⛔ WHY AN ERROR AND NOT THE §4.2.6 RECOGNIZE-AND-WARN OF 1578/1579/1580. Those three name Annex A.3
    //    PROCESSOR-DEPENDENT elements, whose licence is §4.2.6 — "the decision to provide support … is within an
    //    implementor's discretion" plus a mandatory compile-time WARNING mechanism, i.e. accept-and-flag. Screen
    //    handling is an Annex A.4 OPTIONAL module, and A.4.1's licence reads the other way: "An implementation
    //    shall accept the syntax and provide the functionality for an optional element ONLY WHEN support for that
    //    language element is claimed by the implementor." docs/CONFORMANCE.md §5 already documents the consequence
    //    — for a Not-claimed module "a parse error or a named error is the conforming posture". A.4.1 ¶2 extends
    //    the licence from the 27 named elements to every syntax rule, general rule and exception condition hanging
    //    off them, which is what carries the screen description entry's clauses and the EC-SCREEN family.
    //
    //    ⛔ WHY TWO CODES. A Format-3 ACCEPT, a Format-2 DISPLAY and a Format-6 SET can only name a screen-name,
    //    and a screen-name can only be declared in a SCREEN SECTION — so EVERY statement witness necessarily
    //    carries the data-division surface too and would draw 1560 whether or not the STATEMENT was diagnosed.
    //    One code would make every procedure-division witness pass for the wrong reason
    //    (feedback_green_gates_arent_evidence). Splitting the module at the division boundary makes each `.err`
    //    an actual observation: 1560 can never be produced by a statement site, 1707 never by a data site.
    /// <summary>COBOLNET1560 — the A.4.2 DATA/ENVIRONMENT surface: the SCREEN SECTION header (§13.9), every
    /// screen description entry clause (§13.17 / §13.18.x), and the SPECIAL-NAMES CURSOR and CRT STATUS clauses
    /// (§12.3.7). The emitted message NAMES the construct seen — <see cref="Binding.ScreenFacility"/> is the one
    /// funnel and derives the clause name and its ISO § from the parse-tree rule.</summary>
    public static readonly DiagnosticDescriptor ScreenFacilityUnsupported = new(
        "COBOLNET1560", "screen-facility-unsupported", EditionSeverity.Error,
        "A SCREEN SECTION construct (the section header §13.9, a screen description entry §13.17, one of its "
        + "clauses §13.18.x, or the SPECIAL-NAMES CURSOR / CRT STATUS clause §12.3.7) is part of the OPTIONAL "
        + "screen handling module (§4.2.7; Annex A.4.2), for which WiseOwl COBOL claims no support — A.4.1 admits "
        + "an optional element's syntax only when support is claimed, so it is refused by name rather than "
        + "silently accepted and dropped. The facility exists from COBOL-2002. See docs/CONFORMANCE.md §5.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.2 / §13.9 / §13.17 / §12.3.7", Annex: DeclinedAnnex.A4);
    /// <summary>COBOLNET1707 — the A.4.2 PROCEDURE-division surface: ACCEPT format 3 (screen, §14.9.1), DISPLAY
    /// format 2 (screen, §14.9.11 — A.4.2 item 9 misprints the cross-reference as 14.9.10, which is DELETE),
    /// SET format 6 (attribute, §14.9.39), and the EC-SCREEN exception-names in the six contexts A.4.2 item 10
    /// names (RAISING on EXIT / GOBACK / the procedure division header, USE, the PERFORM WHEN phrase, RAISE, and
    /// the &gt;&gt;TURN directive).</summary>
    public static readonly DiagnosticDescriptor ScreenStatementUnsupported = new(
        "COBOLNET1707", "screen-statement-unsupported", EditionSeverity.Error,
        "A screen-handling STATEMENT or exception-name — ACCEPT format 3 (§14.9.1), DISPLAY format 2 (§14.9.11), "
        + "SET format 6 ATTRIBUTE (§14.9.39), or an EC-SCREEN exception-name in a RAISING phrase, a USE "
        + "statement, a PERFORM WHEN phrase, a RAISE statement or a >>TURN directive — is part of the OPTIONAL "
        + "screen handling module (§4.2.7; Annex A.4.2 items 1, 9, 10, 24), for which WiseOwl COBOL claims no "
        + "support. A.4.1 admits an optional element's syntax only when support is claimed; a screen ACCEPT or "
        + "DISPLAY silently re-read as its device format would transfer the wrong data, and a catalogued "
        + "EC-SCREEN name with no raise site reads as implemented to every consumer that can see it. "
        + "See docs/CONFORMANCE.md §5.",
        "ISO §4.2.7 / Annex A.4.1 / Annex A.4.2 items 1, 9, 10, 24 / §14.9.1 / §14.9.11 / §14.9.39",
        Annex: DeclinedAnnex.A4);

    // ── THE THREE BAND CODES THAT HAD NO DESCRIPTOR AT ALL (kb/Work PB175) ────────────────────────────
    //    COBOLNET0869 / 0881 / 1529 were emitted from 38 BARE STRING LITERALS across five binders and from
    //    nowhere else — no catalogue row, so no `docs/DIAGNOSTICS.md` row, no drift test, and nothing for
    //    `session-probe`'s next-free scan or the suppress-key machinery to see. They are BANDS: one code over
    //    a family of neighbouring rules, the shape COBOLNET1720 (`io-phrase-forbidden-here`, three syntax
    //    rules) and COBOLNET1707 (four statement formats) already use, so the descriptor's Title states the
    //    BAND and each site composes the rule it caught. Converting the sites changes no emitted byte:
    //    `Error(DiagnosticDescriptor, string)` forwards to `Error(descriptor.Code, string)`.
    /// <summary>COBOLNET0869 — the POINTER / ADDRESS OPERAND band: what may be written where a pointer,
    /// an address or an object reference is expected, and what a restricted data-pointer narrows that to.
    /// Covers ISO §14.9.39 (SET Format 1 index-assignment — an index-name receiver; Format 5
    /// object-reference-assignment; Format 7 data-pointer-assignment — SET ADDRESS OF / a pointer receiver),
    /// §8.4.3.11 and §8.4.3.13 (the ADDRESS OF and LENGTH OF special registers as operands), §8.8.4.2.16
    /// (the pointer relation condition), §14.9.3 (ALLOCATE's RETURNING operand) and Annex D.9.2.2 (the
    /// restricted data-pointer, whose target type constrains every one of the above).</summary>
    public static readonly DiagnosticDescriptor PointerOperandShape = new(
        "COBOLNET0869", "pointer-operand-shape", EditionSeverity.Error,
        "A pointer, address or object-reference OPERAND is not of a shape its statement admits — the SET statement's pointer formats (ISO §14.9.39), the ADDRESS OF / LENGTH OF special registers (§8.4.3.11, §8.4.3.13), the pointer relation condition (§8.8.4.2.16), the ALLOCATE statement's RETURNING operand (§14.9.3), or the target-type restriction a RESTRICTED data-pointer (Annex D.9.2.2) puts on all of them. The site names the rule it caught.",
        "ISO §14.9.39 / §8.4.3.11 / §8.4.3.13 / §8.8.4.2.16 / §14.9.3 / Annex D.9.2.2");
    /// <summary>COBOLNET0868 — the OBJECT-REFERENCE RELATION band: the fourth bare-string code of the kb/Work
    /// PB175 sweep above, missed because its only site was inside <c>ConditionBinder</c>'s relation arm rather
    /// than a verb binder (kb/Work PB399 moved that site to the ONE relation checkpoint and found it). It is
    /// the class-OBJECT half of ISO §8.8.4.2.2's Format 3 (message-tag-object-or-pointer-reference relation
    /// condition): that format prints only <c>IS [NOT] EQUAL TO</c> / <c>=</c> / <c>&lt;&gt;</c>, and
    /// §8.8.4.2.3 SR5 requires both operands to be of class message-tag, object or pointer and of the same
    /// category. The predefined object reference NULL rides (§8.4.3.9; §8.4.3.10.3 SR1).</summary>
    public static readonly DiagnosticDescriptor ObjectRelationShape = new(
        "COBOLNET0868", "object-relation-shape", EditionSeverity.Error,
        "An object-reference relation condition is not of a shape ISO §8.8.4.2.2 Format 3 admits — an ordering operator (that format prints only [NOT] EQUAL / '=' / '<>'), an operand that is neither of class message-tag, object or pointer nor the predefined NULL, or a pair whose two operands are of different categories (§8.8.4.2.3 SR5). The site names the rule it caught.",
        "ISO §8.8.4.2.1 / §8.8.4.2.2 Format 3 / §8.8.4.2.3 SR5 / §8.4.3.9 / §8.4.3.10.3");
    /// <summary>COBOLNET0881 — the USAGE-CLAUSE COMPATIBILITY band: which other data description clauses may
    /// share an entry with which usage. Covers ISO §13.18.60.3 (the USAGE clause's own syntax rules, incl.
    /// SR18's restricted-pointer TYPEDEF requirement) and §13.18.60.4, plus the clauses those rules exclude —
    /// PICTURE (§13.18.40.3), VALUE (§13.18.63), BLANK WHEN ZERO, JUSTIFIED, SIGN and SYNCHRONIZED.</summary>
    public static readonly DiagnosticDescriptor UsageClauseCompatibility = new(
        "COBOLNET0881", "usage-clause-compatibility", EditionSeverity.Error,
        "A data description entry combines a USAGE with a clause its syntax rules exclude, or omits one they require (ISO §13.18.60.3 / §13.18.60.4) — a PICTURE (§13.18.40.3) or VALUE (§13.18.63) clause on a usage that admits neither, a restricted `USAGE POINTER TO type-name` whose subject carries no TYPEDEF clause (SR18), and the neighbouring clause exclusions. The site names the rule it caught.",
        "ISO §13.18.60.3 / §13.18.60.4 / §13.18.40.3 / §13.18.63");
    /// <summary>COBOLNET1529 — the TYPE DECLARATION band: the shape a TYPEDEF entry and a TYPE reference must
    /// have. Covers ISO §13.18.58 (the TYPEDEF clause), §13.18.57.3 (the TYPE clause's syntax rules) and
    /// §8.5.3.1 / §8.5.3.3 (type declarations and strong typing — including SR1's prohibition on an
    /// ELEMENTARY type definition carrying the STRONG phrase).</summary>
    // ── THE THREE JOBS OF THE OLD `BoundUnsupported` CARRIER (kb/Work PB236) ──────────────────────────────
    //    One bound node used to answer three different questions — "WiseOwl COBOL has not built this" (a DEFERRAL),
    //    "your OPERAND is ill-formed" and "this statement is ILLEGAL HERE" — and StatementEmitter rendered all
    //    three as the same run-time `NotImplemented.Run(...)`. The two user-error jobs therefore reached the
    //    programmer as a claim that THE COMPILER is incomplete when in fact THE SOURCE is wrong, at run time,
    //    and on an unexecuted path not at all: `RELEASE <non-SD record>` behind a GO TO compiled clean AND ran
    //    to normal completion with no message at any time. ISO §4.2.2 ¶2: "An implementation shall provide a
    //    warning mechanism that optionally may be invoked by the user at compile time to indicate violations of
    //    the general formats and the explicit syntax rules of standard COBOL." These two descriptors are the
    //    separation: 1757 is where a violated syntax rule / general format now lands, and 1756 is the DEFERRAL
    //    announcing ITSELF at compile time so the carrier's one remaining job can never again be silent.
    /// <summary>COBOLNET1757 — an operand a statement's own syntax rules or general format do not admit,
    /// refused at BIND time. The rules that reach it today (each named in full by the site's message): the
    /// CORRESPONDING group-operand rule in its three spellings — MOVE §14.9.25.3 SR12 ("Identifier-3 and
    /// identifier-4 shall specify group data items and shall not be reference-modified"), ADD §14.9.2.3 SR6 and
    /// SUBTRACT §14.9.44.3 SR6 ("Identifier-4 and identifier-5 shall be alphanumeric group items, national group
    /// items, variable-length groups, or strongly-typed group items and shall not be described with
    /// level-number 66") — the record-name-1 operand rule in its three spellings, RELEASE §14.9.32.3 SR1
    /// ("Record-name-1 shall be the name of a logical record in a sort-merge file description entry"), WRITE
    /// §14.9.51.3 SR5 and REWRITE §14.9.35.3 SR1 ("Record-name-1 is the name of a logical record in the file
    /// section of the data division"), together with its corollary that a record-name is a user-defined word
    /// (§8.3.2.2.25) whose §5.2.4 operand type admits qualification and subscripting but never reference
    /// modification (§8.4.3.3.3 SR5) — RETURN §14.9.34.3 SR1 ("File-name-1 shall be described by a
    /// sort-merge file description entry in the data division"), and MERGE's §14.9.24.2 general format
    /// (at least two USING file-names; one of OUTPUT PROCEDURE or GIVING) with SORT's table-key rules
    /// (§14.9.40.3). The multi-rule shape follows COBOLNET1651's precedent: the CODE is the identity of the
    /// MECHANISM (a bind-time operand refusal), the MESSAGE carries the rule — which is why kb/Work PB347
    /// brought WRITE's and REWRITE's record-name-1 here rather than claiming a code of its own.</summary>
    public static readonly DiagnosticDescriptor StatementOperandRule = new(
        "COBOLNET1757", "statement-operand-rule", EditionSeverity.Error,
        "A statement operand is one the statement's own syntax rules or general format do not admit, so the "
        + "statement has no meaning: a CORRESPONDING operand that is not a group item or is a level-66 RENAMES "
        + "entry (ISO §14.9.25.3 SR12 / §14.9.2.3 SR6 / §14.9.44.3 SR6), a WRITE, REWRITE or RELEASE "
        + "record-name-1 that is not a logical record of a file description entry — a subordinate item of one, "
        + "or a reference-modified record, is not a record-name (§14.9.51.3 SR5 / §14.9.35.3 SR1 / §14.9.32.3 "
        + "SR1, with §5.2.4 and §8.4.3.3.3 SR5) — a RELEASE record-name whose file is described by an FD rather "
        + "than an SD (§14.9.32.3 SR1), a RETURN file-name not described by an SD (§14.9.34.3 SR1), or a "
        + "SORT/MERGE operand list the general format does not print (§14.9.24.2 / §14.9.40.3), a "
        + "CORRESPONDING operand that is REFERENCE-MODIFIED (§14.9.25.3 SR12's second half, with "
        + "§8.4.3.3.4 GR6 — the result is an elementary data item, so it is not a group item), or a SET "
        + "operand the statement's own format does not admit: a switch-status condition-name in Format 4 "
        + "(§14.9.39.3 SR6 — condition-name-1 shall be associated with a conditional variable) or a "
        + "Format-3 name that is no external-switch mnemonic (SR5), an INITIATE or TERMINATE report-name that "
        + "no report description entry defines (§14.9.21.3 SR1 / §14.9.46.3 SR1), a GENERATE operand that "
        + "names neither a detail report group nor a report (§14.9.16.3 SR1 / SR2), or a table-SORT key "
        + "described with, or subordinate to, an OCCURS inside data-name-2 (§14.9.40.3 SR14 e). Rejected at "
        + "bind — the statement is not run.",
        "ISO §14.9.2.3 / §14.9.16.3 / §14.9.21.3 / §14.9.25.3 / §14.9.32.3 / §14.9.34.3 / §14.9.35.3 / "
        + "§14.9.40.3 / §14.9.44.3 / §14.9.46.3 / §14.9.51.3");
    /// <summary>COBOLNET2700 — a SORT or MERGE statement written where its syntax rules bar it (kb/Work PB1139). The
    /// FILE-format SORT "shall not appear in imperative-statement-1 of an exception-checking PERFORM statement, in an
    /// input or output procedure, or in a declarative procedure" (§14.9.40.3 SR3, FORMAT 1 — a table SORT is not
    /// covered), and a MERGE "may appear anywhere in the procedure division except … in a declarative procedure"
    /// (§14.9.24.3 SR1). This code names two of the three regions for the two verbs together — a DECLARATIVE
    /// procedure (both verbs, every edition) and a SORT inside an INPUT or OUTPUT procedure of another file SORT or of
    /// a MERGE (every edition). The third region has its own codes: the exception-checking PERFORM is COBOLNET1615
    /// (MERGE) and COBOLNET1617 (SORT), and MERGE inside a sort-merge procedure is the COBOL-2023 removal
    /// COBOLNET1572 (Annex E.2 item 20), which the prior editions allowed. Rejected at bind.</summary>
    public static readonly DiagnosticDescriptor SortMergePlacement = new(
        "COBOLNET2700", "sort-merge-placement", EditionSeverity.Error,
        "A SORT or MERGE statement appears where its syntax rules do not allow it. ISO §14.9.40.3 SR3 (SORT Format 1): "
        + "\"A SORT statement shall not appear in imperative-statement-1 of an exception-checking PERFORM statement, in "
        + "an input or output procedure, or in a declarative procedure.\" ISO §14.9.24.3 SR1 (MERGE): \"A MERGE "
        + "statement may appear anywhere in the procedure division except in imperative-statement-1 of an "
        + "exception-checking PERFORM statement, or in an output procedure of another MERGE statement, or an input or "
        + "output procedure of a file format SORT statement, or in a declarative procedure.\" This diagnostic covers a "
        + "declarative procedure (SORT Format 1 and MERGE, every edition) and a file SORT inside an input or output "
        + "procedure of another SORT or MERGE; the exception-checking PERFORM has COBOLNET1615/1617 and MERGE in a "
        + "sort-merge procedure is the COBOL-2023 removal COBOLNET1572. A Format-2 table SORT is not subject to SR3. "
        + "Move the statement out of the region, or perform it from outside.",
        "ISO §14.9.40.3 SR3 / §14.9.24.3 SR1");

    /// <summary>COBOLNET1756 — the DEFERRAL announcing itself. A statement the grammar accepted but this
    /// compiler binds to <c>BoundUnsupported</c> is staged to a loud run-time refusal (COBOLNET_DESIGN §1.4);
    /// before kb/Work PB236 that staging was invisible at compile time, so a program carrying an unimplemented
    /// statement on an unexecuted path looked identical to one that compiled cleanly. The
    /// <see cref="EditionSeverity.Warning"/> severity is the point: the deferral is the COMPILER's gap and not
    /// the source's error, so it must not fail a compile that the standard gives a meaning — it must only stop
    /// being silent. Reported ONCE per statement from the single <c>StatementBinder.BindStatement</c> funnel,
    /// which already positions the diagnostic cursor (kb/Work PB82), so no construction site can forget it and
    /// every future one inherits it.</summary>
    public static readonly DiagnosticDescriptor StatementNotImplemented = new(
        "COBOLNET1756", "statement-not-implemented", EditionSeverity.Warning,
        "A statement this compiler has not implemented was accepted and staged to a loud run-time refusal "
        + "(COBOLNET_DESIGN §1.4): the compilation succeeds and every other statement runs, but reaching this "
        + "one aborts the run unit with NotImplementedCobolFeatureException. This is a gap in WiseOwl COBOL, not an "
        + "error in the source — the warning exists so the gap is visible before the program is run. It is "
        + "never raised for a statement whose bind already drew an error: a refused statement makes no claim "
        + "about the compiler.",
        "COBOLNET_DESIGN §1.4", "statement-not-implemented");
    /// <summary>COBOLNET2269 — a statement written in a SHAPE none of its general formats prints (kb/Work PB909).
    /// The grammar accepts some shapes the standard does not print — vendor extensions (INSPECT … TRAILING,
    /// SEARCH … NOT AT END, the ENTRY statement) and the defensive residue of a union-parsed rule — and each used
    /// to reach the binder's DEFERRAL carrier, so a program the standard gives no meaning to compiled with a
    /// COBOLNET1756 warning claiming WiseOwl COBOL was incomplete, and aborted the run unit when the statement ran.
    /// ISO §4.2.2 ¶1 fixes what may be accepted — "An implementation shall accept the syntax and provide the
    /// functionality for all standard language elements" — and ¶2 requires the compile-time indication of
    /// "violations of the general formats and the explicit syntax rules of standard COBOL". This
    /// implementation declares no vendor dialect under which an extension could be admitted (the COBOLNET1941 /
    /// COBOLNET1970 posture), so the shape is refused at every edition and every strictness. The CODE is the
    /// mechanism; the MESSAGE names the statement and the general format it departs from. Two emit layers
    /// (kb/Work PB446): the binder, for a shape the grammar parses (and for a NEXT SENTENCE written anywhere but
    /// the whole of an IF Format-2 or SEARCH WHEN phrase — <c>StatementBinder.IsNextSentenceArm</c>), and the
    /// parse-layer twin in <c>DiagnosticDescriptors</c>, for a SEARCH phrase the grammar now refuses (NOT AT END,
    /// a KEY phrase, a second Format-2 WHEN).</summary>
    public static readonly DiagnosticDescriptor StatementFormatShape = new(
        "COBOLNET2269", "statement-format-shape", EditionSeverity.Error,
        "A statement is written in a shape that none of its ISO general formats prints, so the standard gives "
        + "it no meaning: for example INSPECT … TALLYING … FOR FIRST or FOR TRAILING and INSPECT … REPLACING "
        + "TRAILING (§14.9.22.2 prints only CHARACTERS / ALL / LEADING in a tallying-phrase and CHARACTERS / "
        + "ALL / LEADING / FIRST in a replacing-phrase), SEARCH or SEARCH ALL with a NOT AT END phrase "
        + "(§14.9.37.2 prints AT END alone), a KEY phrase or a second WHEN phrase on SEARCH ALL (§14.9.37.2 "
        + "Format 2 prints one WHEN and no KEY phrase), NEXT SENTENCE anywhere but the whole of an IF Format-2 "
        + "THEN/ELSE phrase or a SEARCH WHEN phrase (§14.9.19.2, §14.9.37.2), or the ENTRY statement (ISO/IEC 1989 defines none). Refused at "
        + "every edition and every strictness: §4.2.2 makes a general format the definition of what may be "
        + "written, and this implementation declares no vendor dialect under which an extension could be "
        + "admitted. Rewrite the statement in a printed format — e.g. an INSPECT … REPLACING TRAILING becomes "
        + "a reference-modified INSPECT … REPLACING ALL over the trailing span, and SEARCH … NOT AT END "
        + "becomes a WHEN branch.",
        "ISO §4.2.2");
    // ── The ASSIGN … USING operand screens (kb/Work PB324): §12.4.5.2 SR7's two halves, checked post-build once
    // the data forest is indexed. Two codes, not one, because the halves have different causes and different
    // repairs — a wrong category is a declaration to change, a subordinate operand is a whole design to move. ──
    /// <summary>COBOLNET1810 — the CATEGORY half of §12.4.5.2 SR7. Also fires when data-name-1 names nothing:
    /// a reference that resolves to no data item cannot reference an alphanumeric one, and staying silent would
    /// leave the connector permanently unassociated (its OPENs failing '31' with no compile-time reason given).</summary>
    public static readonly DiagnosticDescriptor AssignUsingNotAlphanumeric = new(
        "COBOLNET1810", "assign-using-not-alphanumeric", EditionSeverity.Error,
        "ISO §12.4.5.2 syntax rule 7, first half: \"Data-name-1 shall reference an alphanumeric data item…\" — "
        + "data-name-1 is the operand of the ASSIGN clause's USING phrase, whose content identifies the physical "
        + "file at every OPEN, SORT or MERGE (§12.4.5.3 GR3 b; §9.1.21, Dynamic file assignment, states the same "
        + "requirement in the concepts: \"The USING phrase references an alphanumeric data item whose content at "
        + "the time an OPEN, SORT, or MERGE statement for that file is executed uniquely identifies the specific "
        + "physical file to be accessed\"). An alphanumeric GROUP item qualifies (§13.18.29.4 GR3); a "
        + "category-alphabetic, numeric, edited, national, boolean or pointer item does not.",
        "ISO §12.4.5.2 SR7");

    /// <summary>COBOLNET1811 — the SUBORDINATION half of §12.4.5.2 SR7. The dangerous half: an operand inside the
    /// file's own record area is overwritten by every READ of that file, so the name the next OPEN would read is
    /// file data.</summary>
    public static readonly DiagnosticDescriptor AssignUsingInOwnRecord = new(
        "COBOLNET1811", "assign-using-in-own-record", EditionSeverity.Error,
        "ISO §12.4.5.2 syntax rule 7, second half: \"…and shall not be subordinate to the file description entry "
        + "for file-name-1.\" The ASSIGN … USING operand names the physical file the connector is associated with "
        + "at each OPEN, SORT or MERGE (§12.4.5.3 GR3 b), so it cannot live in the record area that file's own "
        + "READ overwrites — nor is that area available at all while the connector is closed (§9.1.2).",
        "ISO §12.4.5.2 SR7");

    // ── The record-description-less file description entry (kb/Work PB345): §13.4.5.3 SR3 a) EXPRESSLY
    // contemplates an FD with no record description entries, so the two rules that BOUND that shape are the only
    // thing standing between legal source and a file connector with no record area. Two codes, not one, because
    // the repairs are opposite — 1836 says "state the size", 1837 says "describe the record". ──
    /// <summary>COBOLNET1836 — the size half. §14.9.30.4 GR6's implied record description is "of the maximum size
    /// established by the RECORD clause", so a record-description-less FD whose RECORD clause establishes no
    /// maximum has no record area at all; the connector would register a zero-width record and every READ INTO
    /// would deliver nothing.</summary>
    public static readonly DiagnosticDescriptor RecordLessFdNoRecordSize = new(
        "COBOLNET1836", "record-less-fd-no-record-size", EditionSeverity.Error,
        "ISO §13.4.5.3 syntax rule 3 a): \"When no record description entries are specified: a RECORD clause "
        + "shall be specified in the file description entry\" — restated on the clause itself by §13.18.43.3 "
        + "syntax rule 1, \"If no record description entries are specified in a file description entry for a file "
        + "that is not a report file, the RECORD clause shall be specified.\" The clause has to establish a "
        + "MAXIMUM, because §14.9.30.4 GR6 sizes the implied record description by \"the maximum size established "
        + "by the RECORD clause\": format 1's integer-1 and format 3's integer-5 do, and so does format 2 when "
        + "integer-3 is written, but a bare RECORD IS VARYING falls to §13.18.43.4 GR10 — \"the greatest number "
        + "of bytes described for a record in that file\" — and this file describes none. A report file is "
        + "exempt: §13.4.5.3 SR8 forbids it record description entries and §13.18.43.3 SR1 excludes it by name.",
        "ISO §13.4.5.3 SR3 / §13.18.43.3 SR1");

    /// <summary>COBOLNET1837 — the description half: the two entry kinds whose own syntax rules withdraw the
    /// §13.4.5.3 SR3 permission. Both spell the SAME requirement, so they share one code and the message names
    /// the arm that caught it.</summary>
    public static readonly DiagnosticDescriptor FileDescriptionRecordRequired = new(
        "COBOLNET1837", "file-description-record-required", EditionSeverity.Error,
        "ISO §13.4.5.3 syntax rule 7 (indexed): \"For an indexed file, one or more record description entries "
        + "shall be associated with the file description entry.\" — and §13.4.6.3 syntax rule 2 (sort-merge): "
        + "\"One or more record description entries shall be associated with the sort-merge file description "
        + "entry.\" §13.4.5.3 SR3's permission to omit them is a FORMATS 1 AND 2 rule and these two arms take it "
        + "back, because both entry kinds are keyed and a key is located IN a record: §12.4.5.12.3 SR2 requires "
        + "the RECORD KEY operand to \"reference a data item … within a record description entry associated with "
        + "the file-name specified in this file control entry\", and §14.9.40.3 SR6 a) requires that \"the data "
        + "items identified by key data-names shall be described in records associated with file-name-1\". "
        + "Neither can be located in a record nothing describes.",
        "ISO §13.4.5.3 SR7 / §13.4.6.3 SR2");

    /// <summary>COBOLNET0862 — the I-O STATEMENT OPERAND/PHRASE band: a START or WRITE statement writes an
    /// operand or a phrase its own syntax rules do not admit. Covers ISO §14.9.41.3 (START — the access mode,
    /// the relational operator, the WITH LENGTH phrase, the FIRST/LAST requirement on a sequential-organization
    /// file, and what data-name-1 may name on each organization) and §14.9.51.3 with §14.9.51.4 (WRITE — the
    /// ADVANCING phrase's operand and the malformed BEFORE/AFTER pair). Two statements, one shape: the operand
    /// or phrase is syntactically present and semantically inadmissible, so the diagnosis is a rule name.</summary>
    public static readonly DiagnosticDescriptor IoStatementOperandRule = new(
        "COBOLNET0862", "io-statement-operand-rule", EditionSeverity.Error,
        "A START or WRITE statement writes an operand or a phrase its own syntax rules do not admit — START's "
        + "access mode, relational operator, WITH LENGTH phrase, the FIRST/LAST requirement §14.9.41.3 syntax "
        + "rule 2 puts on a sequential-organization file (\"If the organization of the file referenced by "
        + "file-name-1 is sequential, either the FIRST or the LAST phrase shall be specified.\"), and what "
        + "data-name-1 may name on each organization; or WRITE's ADVANCING operand and its malformed "
        + "BEFORE/AFTER pair (§14.9.51.3 / §14.9.51.4). The site names the rule it caught.",
        "ISO §14.9.41.3 / §14.9.51.3 / §14.9.51.4");

    /// <summary>COBOLNET0863 — the FILE-CONTROL KEY-CLAUSE band: a RECORD KEY, ALTERNATE RECORD KEY or RELATIVE
    /// KEY clause (or the LINAGE-COUNTER qualifier that reads one of the same file description entries) violates
    /// its own rule. Covers ISO §12.4.5.1 (the file control entry's required clauses), §12.4.5.2 SR10 (RELATIVE
    /// KEY required for DYNAMIC/RANDOM access), §12.4.5.12.3 (RECORD KEY), §12.4.5.6.3 (ALTERNATE RECORD KEY),
    /// §12.4.5.13.3 (RELATIVE KEY) and §8.4.3.14 / §13.18.34 GR7 a) (LINAGE-COUNTER). The three key clauses state
    /// the SAME OCCURS ban in the same words, which is why they share one code: a rule set with one member written
    /// down is where the missing members hide (kb/Work PB354).
    /// <para>⛔ THE KEY-CLAUSE ARM IS ONE TABLE, <c>FileControlKeyRules</c>, run from
    /// <c>DataBinder.ResolveFiles</c> over EVERY declared file. It used to run from <c>KeyedIoBinder</c> on the
    /// first keyed VERB naming the file, so these entry rules were silent for a file the program only OPENed and
    /// CLOSEd (kb/Work PB699). Two citations were repaired in the same move: the required-clause rule is
    /// §12.4.5.2 SR10, not §12.4.5.13 (which has no syntax rules at all — they live in §12.4.5.13.3), and the
    /// unsigned-integer and not-in-a-record rules are §12.4.5.13.3 SR2 and SR3.</para></summary>
    public static readonly DiagnosticDescriptor FileKeyClauseRule = new(
        "COBOLNET0863", "file-key-clause-rule", EditionSeverity.Error,
        "A file control entry's key clause — RECORD KEY (ISO §12.4.5.12.3), ALTERNATE RECORD KEY (§12.4.5.6.3) "
        + "or RELATIVE KEY (§12.4.5.13.3) — is absent where the organization requires it (§12.4.5.1 Format 1) or "
        + "the access mode does (§12.4.5.2 SR10), names a data item that is not within a record description entry "
        + "associated with the file (§12.4.5.12.3 SR2 / §12.4.5.6.3 SR2), or breaks one of the clause's other "
        + "syntax rules: all three state \"Data-name-1 and data-name-2 shall not be subject to any OCCURS "
        + "clauses\" (§12.4.5.12.3 syntax rule 1 and its twins), and RELATIVE KEY adds the unsigned-integer "
        + "(§12.4.5.13.3 SR2) and not-in-a-record-of-this-file (SR3) rules. A key clause may also be written on a "
        + "file whose organization does not carry it at all — a RECORD KEY or ALTERNATE RECORD KEY clause on a "
        + "non-indexed file, or a RELATIVE KEY clause on a non-relative one (§12.4.5.2 SR8 / SR9, first "
        + "sentence) — or name an operand that is not of category alphanumeric or national (§12.4.5.12.3 SR2 / "
        + "§12.4.5.6.3 SR2, the other half of the same sentence). The LINAGE-COUNTER qualifier "
        + "(§8.4.3.14 / §13.18.34.4 GR7 a) names a file description entry the same way and lands here too. "
        + "The site names the rule it caught.",
        "ISO §12.4.5.1 / §12.4.5.2 / §12.4.5.12.3 / §12.4.5.6.3 / §12.4.5.13.3 / §8.4.3.14 / §13.18.34");

    /// <summary>COBOLNET1900 — §12.4.5.2 SR8 and SR9's SECOND sentence, which both print verbatim: "The
    /// associated file description entry shall not be a sort-merge file description entry" (kb/Work PB742).
    /// <para>SEPARATE FROM <see cref="FileKeyClauseRule"/> because the subject is DIFFERENT: not a key clause's
    /// own rule but the ENTRY's format against the kind of file description entry that describes it, and the
    /// remedy is a different edit (describe the file with an FD, or stop writing the format's clauses). The
    /// format may be specified by the ORGANIZATION clause with no key clause present at all, so a key-clause code
    /// could not carry it. ONE code for both rules: the sentence is the same sentence, and the message names
    /// which format the entry specified and by which clause.</para></summary>
    public static readonly DiagnosticDescriptor FileControlFormatOnSortMerge = new(
        "COBOLNET1900", "file-control-format-on-sort-merge", EditionSeverity.Error,
        "A file control entry specifies ISO §12.4.5.1's Format 1 (indexed) or Format 2 (relative) — by an "
        + "ORGANIZATION IS INDEXED / RELATIVE clause, a RECORD KEY, ALTERNATE RECORD KEY or COLLATING SEQUENCE "
        + "clause, or a RELATIVE KEY clause — for a file that is described by a SORT-MERGE file description "
        + "entry (an SD). §12.4.5.2 SR8 and SR9 both close with \"The associated file description entry shall "
        + "not be a sort-merge file description entry\"; §12.4.5.1 Format 4 is the only format a sort-merge file "
        + "may be written in, and it carries no key clause and only the SEQUENTIAL organization phrase.",
        "ISO §12.4.5.2 SR8 / SR9 / §12.4.5.1 Format 4");

    /// <summary>COBOLNET1858 — §12.4.5.5.2 SR2, the ACCESS MODE clause's own organization rule, checked on the
    /// FILE CONTROL ENTRY (kb/Work PB692). One descriptor, not one per phrase: DYNAMIC and RANDOM are two
    /// spellings of a single prohibition and the message names the one that was written.</summary>
    public static readonly DiagnosticDescriptor AccessModeNotSequentialOnSequentialFile = new(
        "COBOLNET1858", "access-mode-not-sequential-on-sequential-file", EditionSeverity.Error,
        "ISO §12.4.5.5.2 syntax rule 2: \"The DYNAMIC and RANDOM phrases shall not be specified for a sequential "
        + "file.\" The general format says the same structurally — the Format-3 (sequential) file control entry "
        + "admits only [ ACCESS MODE IS SEQUENTIAL ]. A file is sequential when its ORGANIZATION clause says "
        + "RECORD SEQUENTIAL or LINE SEQUENTIAL — §12.4.5.10.3 GR2/GR3 put both phrases in that clause and "
        + "§12.4.5.2 SR11 makes the Format-3 entry carrying it one \"for a sequential file or a report file\" — "
        + "and ALSO when the clause is omitted, because §12.4.5.10.3 GR6 implies \"sequential organization with "
        + "the RECORD SEQUENTIAL phrase\". Accepting the combination "
        + "makes a keyless connector reachable under the keyed access rules: with no NEXT implied (§14.9.30.3 "
        + "SR8/SR9), §14.9.30.4 GR19 reads the file as a Format-2 random read on a file that has no keys.",
        "ISO §12.4.5.5.2 SR2");

    /// <summary>COBOLNET1910 — §14.9.51.3 SR17, the ONE rule that restricts the COBOL-2023 combined
    /// <c>BEFORE AFTER ADVANCING</c> phrase (kb/Work PB712). The pair itself is legal (Annex §E.3.3 item 2);
    /// what SR17 forbids is the pair TOGETHER WITH the PAGE operand, and §14.9.51.4 GR25 g)/h) say why — they
    /// place the record "before or after (depending on the phrase used) the device is repositioned", which has
    /// no answer when both words are written. Every other malformed shape the old two-phrase grammar could
    /// build (two BEFOREs, two AFTERs, two ADVANCING operands) is now unspellable: §5.2.6.4 lets each
    /// choice-indicator alternative appear only once and Format 1 prints one operand.</summary>
    public static readonly DiagnosticDescriptor WriteBeforeAfterAdvancingPage = new(
        "COBOLNET1910", "write-before-after-advancing-page", EditionSeverity.Error,
        "ISO §14.9.51.3 syntax rule 17: \"The BEFORE and AFTER phrases shall not both be specified if the PAGE "
        + "phrase is specified.\" The combined form is otherwise legal from COBOL-2023 (Annex §E.3.3 item 2), and "
        + "§14.9.51.4 GR25 f) gives it a defined advance — the page is advanced after the record was presented. "
        + "PAGE is the one operand it cannot carry: GR25 g) and h) position the record \"before or after "
        + "(depending on the phrase used)\" the device is repositioned to the next page, and with both words "
        + "written there is no phrase to depend on.",
        "ISO §14.9.51.3 SR17");

    /// <summary>
    /// COBOLNET1911 — THE malformed compiler-directive operand, for the whole §7.3 family (kb/Work PB794).
    /// ISO §7.3.3 SR6 composes compiler-instruction "as specified in the syntax of each directive", and for the
    /// directives whose syntax is a closed word set that specification is the <c>directiveOperand</c> column of
    /// <c>constructs.json</c>, checked once by <see cref="CompilerDirectiveCatalog.CheckOperand"/> at the point
    /// the directive word is recognized. ONE descriptor, not one per directive: the rule had been written six
    /// times with six codes (0883 PROPAGATE, 1650 LEAP-SECOND, 1576 REF-MOD-ZERO-LENGTH, 1622 FLAG-14,
    /// 1623 COBOL-WORDS, 0718 TURN) and the seven directives nobody had written it for — SOURCE FORMAT, LISTING,
    /// PUSH, POP, DISPLAY, CALL-CONVENTION — compiled malformed lines in silence. The three whose operand is a
    /// closed word set now answer here; the structured ones (TURN's exception-name list, the FLAG option lists,
    /// COBOL-WORDS' entries) keep their own codes because their operands are not word sets.
    /// </summary>
    public static readonly DiagnosticDescriptor DirectiveMalformedOperand = new(
        "COBOLNET1911", "directive-malformed-operand", EditionSeverity.Error,
        "A compiler directive's operand is not one the directive's own general format admits. ISO §7.3.3 syntax "
        + "rule 6: \"Compiler-instruction is composed of compiler-directive words, system-names, and user-defined "
        + "words as specified in the syntax of each directive.\" The message names the directive, what was "
        + "written, and the admissible set — which is read from the directive's constructs.json row, so every "
        + "directive whose operand is a closed word set is checked by construction rather than by remembering.",
        "ISO §7.3.3 SR6 / each directive's general format");

    /// <summary>COBOLNET1914 — the FUNCTION-IDENTIFIER class restriction the <c>… FROM</c> phrase of RELEASE,
    /// WRITE and REWRITE each state for themselves (kb/Work PB348). ONE descriptor because it is one rule with
    /// a per-verb admitted SET, and the site names the verb, the clause and the category it saw.</summary>
    public static readonly DiagnosticDescriptor FromPhraseFunctionClass = new(
        "COBOLNET1914", "from-phrase-function-class", EditionSeverity.Error,
        "A function-identifier written as identifier-1 of a FROM phrase references a function whose result class "
        + "that phrase's own syntax rule does not admit. The rule is stated once per verb and the admitted set "
        + "DIFFERS: ISO §14.9.32.3 syntax rule 2 (RELEASE) and §14.9.51.3 syntax rule 4 (WRITE) each say \"If "
        + "identifier-1 is a function-identifier, it shall reference an alphanumeric or national function\", "
        + "while §14.9.35.3 syntax rule 9 (REWRITE, the FILE phrase not specified) admits \"an alphanumeric, "
        + "boolean, or national function\". The FILE-phrase readings (§14.9.51.3 SR10, §14.9.35.3 SR8) are not "
        + "reachable: the FILE arm is Annex A.4.13 item 2 and is declined earlier, COBOLNET1706. The class is "
        + "the function's §15.2 TYPE, so an integer function such as FUNCTION LENGTH is refused in every one of "
        + "these positions even though its value would move.",
        "ISO §14.9.32.3 SR2 / §14.9.51.3 SR4 / §14.9.35.3 SR9");

    /// <summary>COBOLNET1915 — ISO §14.9.32.3 SR4, RELEASE's own zero-length-literal rule (kb/Work PB348). It is
    /// RELEASE's alone: neither §14.9.51.3 nor §14.9.35.3 states it for the FROM phrase, so it rides the verb's
    /// rules row rather than every FROM phrase.</summary>
    public static readonly DiagnosticDescriptor FromPhraseZeroLengthLiteral = new(
        "COBOLNET1915", "from-phrase-zero-length-literal", EditionSeverity.Error,
        "ISO §14.9.32.3 syntax rule 4: \"Literal-1 shall not be a zero-length literal.\" A RELEASE statement "
        + "writes literal-1 as the sending operand of the FROM phrase, and a zero-length alphanumeric literal "
        + "there has no characters to move into record-name-1. The rule is stated for RELEASE only — the FROM "
        + "phrases of WRITE (§14.9.51.3) and REWRITE (§14.9.35.3) carry no such sentence, so this diagnostic "
        + "does not reach them.",
        "ISO §14.9.32.3 SR4");

    /// <summary>COBOLNET1924 — ISO §13.18.60.3 syntax rule 16, the placement of the ACTIVE-CLASS phrase of a
    /// USAGE OBJECT REFERENCE clause (kb/Work PB389). The phrase names "the same class as the object that was
    /// used to invoke the method in which this data description entry is specified" (§13.18.60.4 GR22 e)), so it
    /// is meaningless where there is no such class: SR16 admits it only "in a factory definition, an instance
    /// definition, or the linkage or local-storage section of a method definition". Both excluded shapes reach
    /// this one descriptor — outside a class definition altogether, and inside a METHOD definition's
    /// WORKING-STORAGE (a method's other sections are already refused by §13.4.3 SR1 and its siblings) — and the
    /// site says which.</summary>
    public static readonly DiagnosticDescriptor ObjectReferenceActiveClassPlacement = new(
        "COBOLNET1924", "object-reference-active-class-placement", EditionSeverity.Error,
        "ISO §13.18.60.3 syntax rule 16: \"The ACTIVE-CLASS phrase may be specified only in a factory "
        + "definition, an instance definition, or the linkage or local-storage section of a method definition.\" "
        + "USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS binds the item to the class of the object that "
        + "invoked the containing method, so outside a class definition — or in a method's WORKING-STORAGE, "
        + "which is per-class static storage rather than per-activation — there is no such class to bind to.",
        "ISO §13.18.60.3 SR16 / §13.18.60.4 GR22 e)");

    /// <summary>COBOLNET1925 — the FACTORY OF or ONLY phrase written on the interface-name-1 alternative of the
    /// USAGE OBJECT REFERENCE general format (ISO §13.18.60.2; kb/Work PB389). The printed format stacks THREE
    /// alternatives in one bracket pair — <c>interface-name-1</c>, <c>[FACTORY OF] ACTIVE-CLASS</c> and
    /// <c>[FACTORY OF] object-class-name-1 [ONLY]</c> — and only the third carries FACTORY and ONLY together.
    /// The grammar cannot separate an interface-name from an object-class-name (both are one user-defined word),
    /// so it parses the superset and the binder makes this rejection once the name resolves.</summary>
    public static readonly DiagnosticDescriptor ObjectReferenceInterfacePhrase = new(
        "COBOLNET1925", "object-reference-interface-phrase", EditionSeverity.Error,
        "The USAGE OBJECT REFERENCE general format (ISO §13.18.60.2) gives interface-name-1 its OWN alternative, "
        + "written bare: FACTORY OF belongs to the ACTIVE-CLASS and object-class-name-1 alternatives and ONLY to "
        + "object-class-name-1 alone. §13.18.60.4 general rule 22 c) states the interface reading with no "
        + "subordinate rules — \"If interface-name-1 is specified, the object referenced by this data item shall "
        + "implement interface-1\" — while the FACTORY and ONLY readings (22 d) and 22 e)) are stated only for a "
        + "class or the active class.",
        "ISO §13.18.60.2 / §13.18.60.4 GR22 c)");

    public static readonly DiagnosticDescriptor TypeDeclarationShape = new(
        "COBOLNET1529", "type-declaration-shape", EditionSeverity.Error,
        "A TYPEDEF entry or a TYPE reference is malformed (ISO §13.18.58 TYPEDEF, §13.18.57.3 TYPE, §8.5.3.1 / §8.5.3.3 type declarations and strong typing) — a type declaration at the wrong level or under another entry, an unnamed (FILLER) one, TYPEDEF combined with a clause it excludes, or an ELEMENTARY type definition carrying the STRONG phrase, which §8.5.3.1 forbids. The site names the rule it caught.",
        "ISO §13.18.58 / §13.18.57.3 / §8.5.3.1 / §8.5.3.3");

    public static readonly DiagnosticDescriptor AlphabetClauseViolation = new(
        "COBOLNET1906", "alphabet-clause", EditionSeverity.Error,
        "An ALPHABET clause specified with a literal phrase violates one of ISO §12.3.7.3 SR14's sub-rules (the "
        + "message names which): a) a given character shall not be specified more than once in that ALPHABET "
        + "clause; b1/c1) each numeric literal shall be an unsigned integer with a value from one through the "
        + "maximum number of characters in the native alphanumeric / national character set; b2/c2) each "
        + "noninteger literal shall be an alphanumeric / a national literal; b3/c3) each such literal, when a "
        + "THROUGH or ALSO phrase is specified, shall be one character in length. (b4/c4 — the character count "
        + "shall not exceed the native set's — follows from a): distinct characters of a set cannot outnumber "
        + "the set. An unsupported code-name is COBOLNET1907, SR15; a form violation of the clause itself is "
        + "the COBOLNET0898 band.)",
        "ISO §12.3.7.3 SR14");
    public static readonly DiagnosticDescriptor AlphabetCodeNameUnsupported = new(
        "COBOLNET1907", "alphabet-code-name", EditionSeverity.Error,
        "An ALPHABET clause names a code-name-1 (alphanumeric) or code-name-2 (national) this implementation does "
        + "not support. ISO §12.3.7.3 SR15 leaves the supported names to the implementor — 'if any' — and this one "
        + "supports ASCII and EBCDIC as code-name-1 and NO code-name-2 (owner decision, kb/Work PB793; the set is "
        + "the table CobolNet.Binding.ImplementorCodeNames, which is also what the message's own list is generated "
        + "from, and CONFORMANCE.md §7 items 183/184/185 publish it with what each name means). The OTHER words "
        + "that may stand alone in an alphabet definition are the general format's own keywords (NATIVE, "
        + "STANDARD-1, STANDARD-2 for the alphanumeric branch; NATIVE, UCS-4, UTF-8, UTF-16 for the national one; "
        + "LOCALE for either) and the figurative constants of a one-operand literal phrase (§12.3.7.4 GR10). A word "
        + "that is none of those used to be silently reinterpreted as the CHARACTERS OF ITS OWN SPELLING — "
        + "ALPHABET A IS ASCII built an alphabet whose first four positions were A, S, C, I — which every "
        + "downstream reference then read (kb/Work PB770 leg e).",
        "ISO §12.3.7.3 SR15");

    // kb/Work PB732. The VALUE clause resolves its own operands and never enters the R30 chokepoint
    // (ReferenceResolver.Resolve), so its literal-position screen is DataBinder.IsLiteralValueOperand. A word
    // there draws COBOLNET1639 (the position admits a constant-name or a symbolic-character, and it named
    // neither — §8.4.2.1); a shape that is not a word and not a literal draws THIS code, because nothing is
    // undefined and the violation is the general format itself.
    public static readonly DiagnosticDescriptor ValueOperandNotALiteral = new(
        "COBOLNET1902", "value-operand-not-a-literal", EditionSeverity.Error,
        "A VALUE clause operand is not a literal. Every general format of the VALUE clause (ISO §13.18.63.2) "
        + "writes literal-n in every operand position — Format 1 literal-1, Format 2 {literal-1}…, Format 3 "
        + "literal-2 [THROUGH literal-3] and its second position [WHEN SET TO FALSE IS literal-4], Format 4 "
        + "{literal-1}…, Format 5 literal-5 [THROUGH literal-6] — and "
        + "no format admits an identifier or an expression. A figurative constant (§8.3.3.6.3 SR1), a "
        + "constant-name (§13.10.3 SR2) and a symbolic-character (§8.3.3.6.2 Format 7) stand where a literal "
        + "stands, each written as a BARE word; an arithmetic expression, a function-identifier, and a "
        + "qualified, subscripted or reference-modified reference do not.",
        "ISO §13.18.63.2 / §4.2.2");

    // kb/Work PB393. §14.9.25.3 SR9 is the MOVE statement's own application of the §8.5.1.12 compatibility
    // relation, and it is the SCREEN half of one hole with two rims: without it an INCOMPATIBLE program
    // compiled clean and aborted at run time inside a Tier-C guard, while a COMPATIBLE one compiled clean and
    // aborted in the same guard. The relation is the ONE VariableLengthCompatibility module (kb/Work PB204);
    // this code reports its verdict for MOVE, including for every implicit move a phrase defines.
    public static readonly DiagnosticDescriptor MoveVariableLengthIncompatible = new(
        "COBOLNET1931", "move-variable-length-incompatible", EditionSeverity.Error,
        "ISO §14.9.25.3 syntax rule 9: \"If identifier-1 or identifier-2 references a variable-length group "
        + "then these groups shall be compatible groups as specified in 8.5.1.12, Variable-length groups.\" A "
        + "variable-length group is a group with a DYNAMIC LENGTH elementary item or an OCCURS DYNAMIC table "
        + "subordinate to it (§8.5.1.12.1), and §8.5.1.12.1 states the prohibition over the OTHER operand — "
        + "such a group \"may not undergo a comparison or a move operation, in either direction, explicitly or "
        + "otherwise, unless the other operand is a compatible group\". So a literal, a figurative constant, a "
        + "function result, a reference-modified operand and an elementary item are each refused outright, and "
        + "two groups are refused when their variable-length components do not correspond by relative byte "
        + "position (§8.5.1.12.2) or do not match (§8.5.1.12.3). INITIALIZE is the statement that fills such a "
        + "group without a compatible sender (§14.9.20.4 GR7/GR10).",
        "ISO §14.9.25.3 SR9 / §8.5.1.12");
    // kb/Work PB495. ISO §13.18.60.4 GR1 lets a group's USAGE clause reach an elementary item that never wrote
    // one; §13.18.60.3 SR2 is the rule that governs what happens when the item DID write one — the two clauses
    // are permitted to coexist, but not to disagree. The compiler had no site for SR2 at all, so the nearer
    // clause simply won and the outer one was discarded without a word, which changed the item's representation
    // and the group's width from what either written clause asked for.
    public static readonly DiagnosticDescriptor UsageGroupContradiction = new(
        "COBOLNET1927", "usage-group-contradiction", EditionSeverity.Error,
        "A data description entry and a group item it is subordinate to both write a USAGE clause, and the two "
        + "name different usages. ISO §13.18.60.3 SR2: \"If the USAGE clause is written in the data description "
        + "entry for a group item, it may also be written in the data description entry for any subordinate "
        + "elementary item or group item, but the same usage shall be specified in both entries.\" The comparison "
        + "is between USAGES, not words — COMP, COMPUTATIONAL and BINARY are one usage in this implementation "
        + "(§13.18.60.3 SR6 makes COMP the abbreviation; §13.18.60.4 GR4/GR6 leave both representations to the "
        + "implementor), and COMP-3 and PACKED-DECIMAL are likewise one. The violation is reported against the "
        + "SUBORDINATE entry, against the NEAREST enclosing entry that wrote a clause, so a chain of "
        + "contradictions reports each link once.",
        "ISO §13.18.60.3 SR2");
    // kb/Work PB528 (data-model design D24). The COMPOSITION of a Format-1 PICTURE character-string, which
    // ISO §13.18.40.3 SR2 states in two halves: the symbols shall be picture symbols (MEMBERSHIP — COBOLNET0808)
    // and they shall form "an allowable COMBINATION", whose allowable combinations "are specified in 13.18.40.6,
    // Precedence rules". The two codes split that the way a reader does: a named syntax rule versus the table.
    public static readonly DiagnosticDescriptor PictureComposition = new(
        "COBOLNET1934", "picture-composition", EditionSeverity.Error,
        "A Format-1 PICTURE character-string breaks one of ISO §13.18.40.3's COMPOSITION syntax rules — the "
        + "message names which: SR12 a) — it shall contain at least one of 'A', 'N', 'X', 'Z', '1', '9', '*', or "
        + "at least two occurrences of one of character-1, 'X', '+', '-', the currency symbol; SR12 b) — each of "
        + "'CR', 'DB', 'E', 'S', 'V' and the decimal separator at most once; SR16 — 'P' only as a CONTINUOUS "
        + "string at the leftmost or rightmost digit positions; SR17 — 'P' and the decimal separator are "
        + "mutually exclusive; SR18 — 'S' shall be the FIRST symbol; SR19 — 'V' shall immediately precede the "
        + "first 'P' or immediately follow the last; SR20 — 'V' and the decimal separator are mutually "
        + "exclusive; SR21 — 'Z' and '*' are mutually exclusive; SR22 — neither 'S' nor '*' beside a BLANK WHEN "
        + "ZERO clause; SR23 — '+', '-', 'CR', 'DB' are mutually exclusive; SR24 — one currency symbol and one "
        + "editing sign control symbol as FIXED insertion; SR29 — for floating insertion, at least one insertion "
        + "symbol to the LEFT of the decimal point position. Under DECIMAL-POINT IS COMMA every rule written for "
        + "the period reads for the comma and vice versa (SR13), so the message names the separator in force.",
        "ISO §13.18.40.3 SR12-SR29");
    public static readonly DiagnosticDescriptor PicturePrecedence = new(
        "COBOLNET1935", "picture-precedence", EditionSeverity.Error,
        "A Format-1 PICTURE character-string is not an ALLOWABLE COMBINATION of picture symbols: ISO "
        + "§13.18.40.3 SR2 requires one, and §13.18.40.6's Table 10 (Format 1 picture symbol order of "
        + "precedence) specifies which combinations are allowable. An 'x' at an intersection means the column's "
        + "symbol may precede — not necessarily immediately — the row's symbol in character-string-1, so a BLANK "
        + "cell is a prohibition that binds every ordered pair, adjacent or not. Eight symbols occupy two rows "
        + "and columns apiece because their precedence depends on where they stand: the fixed currency symbol "
        + "(first/second versus last/penultimate), the non-floating '+'/'-' (first versus last), and 'P', the "
        + "floating currency symbol, 'Z'/'*' and the floating '+'/'-' (left versus right of the decimal point "
        + "position). This is also how SR25 and SR26 bind — the leading-sign row is empty, so nothing may "
        + "precede a leading sign; the trailing-sign and 'CR'/'DB' columns are empty, so nothing may follow one. "
        + "Under DECIMAL-POINT IS COMMA the precedence rules for comma and period are interchanged.",
        "ISO §13.18.40.3 SR2 / §13.18.40.6 Table 10");
    // ── SET statement Format 15 (numeric-content), ISO §14.9.39.3 SR31/SR32 — kb/Work PB452 ────────────────
    public static readonly DiagnosticDescriptor SetContentNotNumeric = new(
        "COBOLNET1938", "set-content-not-numeric", EditionSeverity.Error,
        "ISO §14.9.39.3 syntax rule 31: \"If FARTHEST-FROM-ZERO or NEAREST-TO-ZERO is specified, identifier-14 "
        + "shall reference a numeric data item.\" A NUMERIC-EDITED item is NOT one — §8.5.2.13 gives it its own "
        + "category — which is where this rule is narrower than the equivalent §15.43 HIGHEST-ALGEBRAIC / §15.58 "
        + "LOWEST-ALGEBRAIC intrinsics Annex D.32 offers as alternatives: their §15.x.3 rule 1 admits \"category "
        + "numeric or numeric-edited\" and this one does not. An index item, a group, an alphanumeric item and a "
        + "reference-modified item are refused here for the same reason: none of them is a numeric data item "
        + "under §8.5.2.12.",
        "ISO §14.9.39.3 SR31");
    public static readonly DiagnosticDescriptor SetContentSignRequired = new(
        "COBOLNET1939", "set-content-sign-required", EditionSeverity.Error,
        "ISO §14.9.39.3 syntax rule 31 a)/b): when identifier-14 describes a SIGNED numeric item whose positive "
        + "and negative extremes have DIFFERENT absolute values, the SIGN phrase shall be specified — a) for "
        + "FARTHEST-FROM-ZERO, b) for NEAREST-TO-ZERO. The rule exists because \"the value farthest away from "
        + "zero permitted by the specifications of identifier-14\" (GR32 a) is then ambiguous, and it bites "
        + "exactly the two's-complement containers of §13.18.60.4 GR12: a PIC S9(4) COMP-5 item spans "
        + "−32768..32767, so FARTHEST-FROM-ZERO alone would have to choose between two different magnitudes. "
        + "Write SIGN POSITIVE or SIGN NEGATIVE. A DIGIT-COUNT item (PIC S9(4) DISPLAY, ±9999) is symmetric and "
        + "needs no phrase.",
        "ISO §14.9.39.3 SR31 a)/b)");
    public static readonly DiagnosticDescriptor SetContentNotStandardFloat = new(
        "COBOLNET1940", "set-content-not-standard-float", EditionSeverity.Error,
        "ISO §14.9.39.3 syntax rule 32: \"If FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, or FLOAT-NOT-A-NUMBER-SIGNALING "
        + "is specified, identifier-14 shall reference a data item described with a standard floating-point "
        + "usage.\" That term is defined narrowly and does NOT mean \"any floating-point item\": §3.166 names the "
        + "standard BINARY usages float-binary-32/-64/-128 and §3.167 the standard DECIMAL usages "
        + "float-decimal-16/-34, while §8.5.1.6.2 separately calls FLOAT-SHORT, FLOAT-LONG and FLOAT-EXTENDED "
        + "floating-point numeric data items WITHOUT making them standard ones. The three value words name "
        + "ISO/IEC 60559:2020 Clause 3 canonical encodings of a BASIC INTERCHANGE FORMAT (GR33/GR34/GR35), which "
        + "only a standard floating-point usage pins; COMP-1, COMP-2, FLOAT-SHORT, FLOAT-LONG and FLOAT-EXTENDED "
        + "carry an implementor-defined representation (§13.18.60.4 GR13) and so cannot. Declare the item "
        + "FLOAT-BINARY-32 or FLOAT-BINARY-64.",
        "ISO §14.9.39.3 SR32 / §3.166 / §3.167");
    // ── COBOLNET1941/1942/1943 — the CLOSED §13.16.2 Format-1 data-description clause list (kb/Work PB487).
    //    The general format used to end in a vendor-extension catch-all (`genericDataClause`) that swallowed any
    //    trailing word sequence, so an unrecognized word, the never-implemented ALIGNED clause, and the bare
    //    `01 M MESSAGE-TAG.` were all accepted and DISCARDED. 1941 names the unrecognized word; 1942 is ALIGNED's
    //    own §13.18.1.3 SR1; 1943 refuses the declined MESSAGE-TAG usage by name. The §13.16.3 permitted-set
    //    rules those three unblocked keep their existing codes (1555 SR12, 1549 SR13, 1542 SR17, 1563 SR18). ──

    /// <summary>COBOLNET1941 — a word at the tail of a data description entry that is not one of §13.16.2
    /// Format 1's clauses. It replaces a SILENT DISCARD, which is the whole point: the entry used to bind with a
    /// data description the programmer did not write.</summary>
    public static readonly DiagnosticDescriptor DataClauseUnrecognized = new(
        "COBOLNET1941", "data-clause-unrecognized", EditionSeverity.Error,
        "A word in a data description entry is not a clause of ISO §13.16.2 Format 1, whose general format is a "
        + "CLOSED list: level-number, entry-name, REDEFINES, IS TYPEDEF [STRONG], ALIGNED, ANY LENGTH, BASED, "
        + "BLANK WHEN ZERO, CONSTANT RECORD, DYNAMIC LENGTH, IS EXTERNAL [AS], IS GLOBAL, GROUP-USAGE, "
        + "JUSTIFIED, occurs-clause, picture-clause, PROPERTY, SAME AS, select-when-clause, SIGN, SYNCHRONIZED, "
        + "TYPE, usage-clause, validation-clauses and value-clause. The grammar used to end this list in a "
        + "vendor-extension catch-all matching any run of words, so a misspelled clause word, an undefined "
        + "COMP-n, and every clause the compiler had not implemented were accepted and SILENTLY DISCARDED — the "
        + "program then compiled and ran against a data description its author did not write. Refused at every "
        + "edition and every strictness: §4.2.2 makes a general format's syntax the definition of what may be "
        + "written, and this implementation declares no vendor dialect under which an extension clause could be "
        + "admitted. A vendor extension is admitted only under the dialect that owns it, never by a catch-all.",
        "ISO §13.16.2 / §4.2.2");

    /// <summary>COBOLNET1942 — ISO §13.18.1.3 SR1, the ALIGNED clause's one syntax rule. ALIGNED had no grammar
    /// rule at all until kb/Work PB487 and was swallowed by the catch-all, so `05 B2 PIC 1(4) USAGE BIT ALIGNED.`
    /// laid out at the WRONG bit offset in silence — the exact silent-wrong-layout case §13.18.1.4 GR1 exists to
    /// prevent.</summary>
    public static readonly DiagnosticDescriptor AlignedClauseSubject = new(
        "COBOLNET1942", "aligned-clause-subject", EditionSeverity.Error,
        "ISO §13.18.1.3 syntax rule 1: \"The ALIGNED clause may be specified only for a bit group item or an "
        + "elementary bit data item.\" A bit group item is a group carrying (or inheriting, §13.16.4 GR1) a "
        + "GROUP-USAGE BIT clause; an elementary bit data item is an elementary item of category boolean whose "
        + "usage is BIT (§13.18.60.4 GR5). On any other subject the clause has no defined effect — §13.18.1.4 "
        + "GR1 states its effect purely in bits (\"aligned on the first bit of the first available byte "
        + "boundary\", with implicit filler bits per §8.5.1.6.3) — so it is refused rather than accepted inert.",
        "ISO §13.18.1.3 SR1");

    /// <summary>COBOLNET1943 — the MESSAGE-TAG usage, refused by name. It is the DATA half of the same Annex A.3
    /// item 4 asynchronous-messaging facility whose STATEMENTS are accept-inert under COBOLNET1578; the data item
    /// cannot be accept-inert for the same reason COBOLNET1705's clause cannot be — there is no inert reading, so
    /// an accepted MESSAGE-TAG item would have to bind as some OTHER class and every reference to it would then
    /// be a wrong answer.</summary>
    public static readonly DiagnosticDescriptor MessageTagUsageUnsupported = new(
        "COBOLNET1943", "message-tag-usage-unsupported", EditionSeverity.Error,
        "the MESSAGE-TAG usage (ISO §13.18.60) is the data-item half of the asynchronous messaging facility, a "
        + "processor-dependent element (§4.2.6; Annex A.3 item 4) that is not supported — see "
        + "docs/CONFORMANCE.md §4 item 1, where SEND/RECEIVE are accepted inert under COBOLNET1578. The DATA "
        + "item is refused instead of accepted inert because there is no inert reading of it: §13.18.60.4 GR9 "
        + "makes the class and category of a message-tag data item message-tag, so an accepted item would have "
        + "to bind as some other class and every reference to it — a MOVE, a comparison, a length function — "
        + "would silently give a wrong answer. MESSAGE-TAG is a COBOL-2023 addition (Annex E.2 item 25); below "
        + "--std 2023 the word is a user-defined word and the entry draws the ordinary introduction gate. "
        + "⛔ Both spellings are refused: §13.18.60.2 prints [ USAGE IS ] as OPTIONAL, so bare "
        + "`01 M MESSAGE-TAG.` is the same clause as `01 M USAGE MESSAGE-TAG.`",
        "ISO §4.2.6 ¶3 / Annex A.3 item 4 / §13.18.60.4 GR9", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A3);

    /// <summary>COBOLNET1955 — a PICTURE EDITING phrase's literal is written in the wrong LITERAL CLASS for the
    /// subject of the entry. ISO §13.18.40.3 SR9 binds in BOTH directions, and neither half was asked while
    /// category national-edited could not be defined at all (kb/Work PB492) — the decoded literal reached the
    /// analyzer as a bare string with its class discarded.</summary>
    public static readonly DiagnosticDescriptor PictureEditingLiteralClass = new(
        "COBOLNET1955", "picture-editing-literal-class", EditionSeverity.Error,
        "ISO §13.18.40.3 SR9, first sentence: \"If USAGE IS NATIONAL is specified for the subject of the entry "
        + "or if character-string-1 contains the symbol 'N', literal-1, literal-2, and literal-3 shall be "
        + "national literals. Otherwise, literal1, literal-2, and literal-3 shall be alphanumeric literals.\" "
        + "The two conditions that make the subject national are the same two that make it a category-national "
        + "or category-national-edited item (§13.18.40.4 GR9/GR10 with §13.18.60.4 SR13a), so the insertion "
        + "characters a PICTURE EDITING phrase supplies are of the same class as the positions they occupy "
        + "(GR2: \"When the usage of the item being edited is national, the value is the national character "
        + "representation\"). Both halves are refused: a national literal in `PIC XXTXX EDITING \"T\" IS N\":\"` "
        + "and an alphanumeric literal in `PIC NNTNN EDITING \"T\" IS \":\"`. SR9's second sentence — 50 "
        + "characters — is COBOLNET1594.",
        "ISO §13.18.40.3 SR9");
    /// <summary>COBOLNET1961 — an index-name subscripting a table it is not INDEXED BY (§8.4.2.3.3 SR4).
    /// kb/Work PB459: §14.9.39.4 GR1 exists so the rest of the SET clause can say "that table", and the
    /// association was built (<c>DataItem.IndexNames</c>) but consumed by nothing outside SEARCH — so
    /// <c>MOVE 77 TO E2(IX1)</c>, with IX1 the index of a DIFFERENT table, compiled clean and wrote through an
    /// unrelated table's occurrence number.</summary>
    public static readonly DiagnosticDescriptor IndexNameNotInTable = new(
        "COBOLNET1961", "index-name-not-in-table", EditionSeverity.Error,
        "ISO §8.4.2.3.3 syntax rule 4: \"Index-name-1 shall correspond to a data description entry in the "
        + "hierarchy of the table being referenced that contains an INDEXED BY phrase specifying that "
        + "index-name.\" The index-name written in this subscript is declared in the INDEXED BY phrase of a "
        + "DIFFERENT table, so it does not identify an occurrence of the table being referenced. Subscript with "
        + "an index-name of this table's own hierarchy, or with an integer data item / arithmetic expression "
        + "(§8.4.2.3.2). Refused in BOTH dialect lanes: the reference has a computable occurrence number, so "
        + "accepting it would silently read or write the wrong table element rather than fail — there is no "
        + "leniency to offer that is not a wrong answer.",
        "ISO §8.4.2.3.3 SR4");

    // ── COBOLNET1964–1966 — the SEARCH ALL Format-2 operand rules (ISO §14.9.37.3 SR7–SR13) ──────────────────
    //    SEVEN consecutive syntax rules that all read the SAME two things — the OCCURS KEY phrase of identifier-1
    //    in its own order, and the WHEN's decomposed operands — and none of which existed as a model, so all
    //    seven were unenforced together (kb/Work PB445). The CODES divide the rules by the OPERAND each is
    //    about, because that is what a user has to go and change: the TABLE's declaration (1964), the KEY side
    //    of a WHEN comparison (1965), or the SENDING side (1966). A WHEN whose SHAPE is not Format 2 at all —
    //    an ordering operator, OR, NOT, a class/sign condition, an abbreviated relation — is the EXISTING
    //    COBOLNET1757 (an operand the statement's own general format does not admit) and gets no fourth code.

    /// <summary>COBOLNET1964 — SEARCH ALL of a table whose OCCURS clause has no KEY phrase (§14.9.37.3 SR7).
    /// Reported alone: every other Format-2 rule is written about the key phrase, so continuing would report
    /// each WHEN operand as "not a key" for a table that declares no keys at all.</summary>
    public static readonly DiagnosticDescriptor SearchAllTableNoKeyPhrase = new(
        "COBOLNET1964", "search-all-table-no-key-phrase", EditionSeverity.Error,
        "ISO §14.9.37.3 syntax rule 7: \"The OCCURS clause associated with identifier-1 shall contain the KEY "
        + "phrase.\" SEARCH ALL names a table whose OCCURS clause declares no ASCENDING/DESCENDING KEY, so the "
        + "statement has no key to compare and the §14.9.37.4 GR5 a) sequencing precondition the form is built "
        + "on cannot even be stated. Add the KEY phrase to the OCCURS clause, or use the serial Format 1 "
        + "(SEARCH without ALL), whose WHEN admits any conditional expression (SR6).",
        "ISO §14.9.37.3 SR7");

    /// <summary>COBOLNET1965 — the KEY side of a Format-2 WHEN comparison: data-name-1 / data-name-2 or a
    /// condition-name, and the subscript it is written with (§14.9.37.3 SR8, SR9, SR11, and SR12's data-name
    /// arm).</summary>
    public static readonly DiagnosticDescriptor SearchAllWhenKeyOperand = new(
        "COBOLNET1965", "search-all-when-key-operand", EditionSeverity.Error,
        "The four ISO §14.9.37.3 syntax rules about the KEY side of a Format-2 WHEN phrase. SR8: data-name-1 "
        + "and all repetitions of data-name-2 \"shall be subscripted by the first index-name associated with "
        + "identifier-1 along with any subscripts required to uniquely identify the data item, and shall be "
        + "referenced in the KEY phrase in the OCCURS clause associated with identifier-1. The index-name "
        + "subscript shall not be followed by a '+' or a '–'.\" SR9 states the same two requirements for a "
        + "condition-name and adds that it \"shall be defined as having only a single value\" and that its "
        + "associated data-name \"shall be specified in the KEY phrase\". SR11: when a key is referenced, \"all "
        + "preceding data-names in that KEY phrase or their associated condition-names shall also be "
        + "referenced\" — the phrase's order is its order of significance (§13.18.38.4 GR3), so a search that "
        + "skips a more significant key is not a search of an ordered table. SR12 forbids a variable-length "
        + "group as data-name-1 or data-name-2.",
        "ISO §14.9.37.3 SR8, SR9, SR11, SR12");

    /// <summary>COBOLNET1966 — the SENDING side of a Format-2 WHEN comparison: identifier-3 / identifier-4, the
    /// identifiers inside arithmetic-expression-1 / -2, and literal-1 / literal-2 (§14.9.37.3 SR10, SR13, and
    /// SR12's identifier arm).</summary>
    public static readonly DiagnosticDescriptor SearchAllWhenSendingOperand = new(
        "COBOLNET1966", "search-all-when-sending-operand", EditionSeverity.Error,
        "The ISO §14.9.37.3 syntax rules about the SENDING side of a Format-2 WHEN phrase. SR10: "
        + "\"Identifier-3, identifier-4, identifiers specified in arithmetic-expression-1, and identifiers "
        + "specified in arithmetic-expression-2 shall be neither referenced in the KEY phrase of the OCCURS "
        + "clause associated with identifier-1 nor subscripted by the first index-name associated with "
        + "identifier-1.\" The sending operand is what the key is compared AGAINST, so an operand that moves "
        + "with the search index — or that is itself a key — makes the comparison a function of the probe "
        + "rather than a test of it, and the binary search has nothing to converge on. SR12 forbids a "
        + "variable-length group as identifier-3 or identifier-4; SR13: \"Neither literal-1 nor literal-2 shall "
        + "be zero-length literals.\"",
        "ISO §14.9.37.3 SR10, SR12, SR13");

    // ── COBOLNET2075 — the SEARCH identifier-1 rules, both formats (ISO §14.9.37.3 SR1–SR3) ───────────────────
    //    ONE code for ONE operand position, the division COBOLNET1964–1966 above already established: SR1, SR2
    //    and SR3 are three predicates over the SAME written reference, and what a user has to go and change is
    //    always identifier-1 itself. Before kb/Work PB443 the binder read only its BASE WORD, so none of the
    //    three could be asked at all and a QUALIFIED identifier-1 searched whichever same-named table was
    //    declared first — a silent wrong answer on legal COBOL.

    /// <summary>COBOLNET2075 — identifier-1 of a SEARCH or SEARCH ALL statement (§14.9.37.3 SR1, SR2, SR3).</summary>
    public static readonly DiagnosticDescriptor SearchIdentifier1Operand = new(
        "COBOLNET2075", "search-identifier-1-operand", EditionSeverity.Error,
        "The three ISO §14.9.37.3 ALL-FORMATS syntax rules about SEARCH's identifier-1. SR1: \"Identifier-1 "
        + "shall not be reference-modified\" — identifier-1 names a TABLE, and a character slice of one is not "
        + "a table. SR2: \"The data description of identifier-1 shall contain an OCCURS clause with an INDEXED "
        + "phrase and identifier-1 shall not be subscripted at the level for which the SEARCH is applicable\" — "
        + "the statement varies the first index associated with identifier-1 (§14.9.37.4 GR1), so it needs an "
        + "index to vary and supplies the searched occurrence itself. SR3: \"Identifier-1 may be contained "
        + "within one or more other tables, for which the subscripting is still required\" — a PERMISSION, "
        + "not a demand on identifier-1's written form, because §14.9.37.4 GR1 puts that obligation "
        + "elsewhere: \"The subscript that is used to determine the occurrence of each superordinate table to "
        + "search is specified by the user in the WHEN phrases.\" A nested table's identifier-1 may therefore "
        + "be written bare OR with one subscript per enclosing table (outermost first, and never one for the "
        + "searched level); this diagnostic asks for no MINIMUM count, and a screen that did would reject "
        + "legal source — the CCVS suite writes the bare form throughout (kb/Work PB443).",
        "ISO §14.9.37.3 SR1, SR2, SR3");

    /// <summary>COBOLNET2409 — ISO §14.9.37.3 SR4, both SEARCH formats: END-SEARCH and a NEXT SENTENCE WHEN arm
    /// in one statement (kb/Work PB444). Before, the pair compiled and was given a MEANING — NEXT SENTENCE jumped
    /// past the terminator to the separator period — with no message beyond the unrelated archaic-feature
    /// warning, and none at all below COBOL-2023.</summary>
    public static readonly DiagnosticDescriptor SearchEndSearchNextSentence = new(
        "COBOLNET2409", "search-end-search-next-sentence", EditionSeverity.Error,
        "A SEARCH or SEARCH ALL statement specifies both the END-SEARCH phrase and a WHEN phrase whose body is "
        + "NEXT SENTENCE. ISO §14.9.37.3 SR4: \"If the END-SEARCH phrase is specified, the NEXT SENTENCE phrase "
        + "shall not be specified.\" With END-SEARCH the statement no longer runs to the separator period that "
        + "NEXT SENTENCE targets (§14.9.37.4 GR1 a)), so the standard gives the pair no meaning. Write CONTINUE in "
        + "the WHEN phrase (control then goes to the end of the SEARCH statement), or drop END-SEARCH and end the "
        + "sentence with a period.",
        "ISO §14.9.37.3 SR4");

    // ── COBOLNET1976–1977 — the level-88 condition-name association rules (ISO §13.16.3 SR24) ──────────

    /// <summary>COBOLNET1976 — the entry a level-88 condition-name entry follows is one ISO §13.16.3 SR24
    /// EXCLUDES from being a conditional variable. kb/Work PB488: <c>BindCondition</c> was a VALUE decoder with
    /// no eligibility screen at all, so seven of the rule's eight lettered exclusions were unenforced and the
    /// eighth (a) held only by construction — <c>01 IX USAGE INDEX. 88 C VALUE 1.</c> compiled AND EVALUATED,
    /// and the pointer spelling leaked a raw Roslyn <c>CS0103</c> at the user.</summary>
    public static readonly DiagnosticDescriptor ConditionNameVariableExcluded = new(
        "COBOLNET1976", "condition-name-variable-excluded", EditionSeverity.Error,
        "ISO §13.16.3 syntax rule 24: \"A condition-name may be associated with any data description entry that "
        + "contains a level-number except the following: a) Another level 88 entry. b) A level 66 entry. c) An "
        + "alphanumeric group containing items with a usage other than display. d) A group containing items "
        + "described with a JUSTIFIED or SYNCHRONIZED clause. e) A data item of the class index, message-tag, "
        + "object, or pointer. f) A data item described with the ANY LENGTH clause. g) A type declaration "
        + "described with the STRONG phrase, or a group item subordinate to such a type declaration. h) A "
        + "variable-length group.\" The message names the lettered exclusion that applies. Exclusion e) is the "
        + "same prohibition as §13.18.60.3 SR11 (\"An elementary data item of class index, message-tag, object, "
        + "or pointer shall not be a conditional variable\") and both are enforced by this one screen. A syntax "
        + "rule is a \"shall\", so the conforming response is a compile-time diagnostic; there is no permissive "
        + "lane, because every one of these shapes either evaluates a condition the standard gives no meaning "
        + "(c, d, e-index, f, g) or reaches code generation with no representation to compare (e-pointer, h).",
        "ISO §13.16.3 SR24");

    /// <summary>COBOLNET1977 — a level-88 entry with no conditional variable: it does not immediately follow a
    /// data description entry describing a data item. kb/Work PB488 measured the silent drop — <c>BindEntries</c>
    /// bound the entry only <c>if (stack.Count > 0)</c> and said nothing otherwise, so a compile-time syntax rule
    /// surfaced, if at all, as a RUN-TIME <c>NotImplementedCobolFeatureException</c> on the first reference.</summary>
    public static readonly DiagnosticDescriptor ConditionNameNoConditionalVariable = new(
        "COBOLNET1977", "condition-name-no-conditional-variable", EditionSeverity.Error,
        "ISO §13.16.3 syntax rule 24: \"The condition-name entries for a particular conditional variable shall "
        + "immediately follow the entry describing the item with which the condition-name is associated.\" This "
        + "level-88 entry follows no such entry — it is the first entry of its section, or the entry before it "
        + "is a constant entry (§13.10), which describes no data item. A condition-name with no conditional "
        + "variable has nothing to test, so it is refused at compile time rather than bound to whatever entry "
        + "happens to precede it.",
        "ISO §13.16.3 SR24");

    // ── COBOLNET1970/1971 — the CLOSED general formats OUTSIDE the data description entry (kb/Work PB829).
    //    ONE grammar rule — `genericClause : IDENTIFIER (IDENTIFIER|literal)*`, described in the grammar as a
    //    "vendor/extension hook" — was wired into SIX sites spanning EIGHT closed general formats and swallowed
    //    any word run the format did not define, at every edition and every strictness, with no diagnostic
    //    anywhere. kb/Work PB487 closed ONE of them (§13.16.2, COBOLNET1941); these two close the other seven.
    //    1970 is for the formats whose list is a list of CLAUSES, 1971 for the two whose list is a list of
    //    PARAGRAPHS — the distinction is not cosmetic: it decides which subclause the reader is sent to. ──

    /// <summary>COBOLNET1970 — a word run inside a closed general format's CLAUSE list that the format does not
    /// define: the file description entry (§13.4.5.2), the sort-merge file description entry (§13.4.6.2), the
    /// file control entry (§12.4.5.1), the I-O-CONTROL paragraph (§12.4.6.2) and the SPECIAL-NAMES paragraph
    /// (§12.3.7.2). Every one of those formats used to accept <c>WIBBLE WOBBLE</c> and run.</summary>
    public static readonly DiagnosticDescriptor ClosedFormatUnrecognizedClause = new(
        "COBOLNET1970", "closed-format-unrecognized-clause", EditionSeverity.Error,
        "A word in this entry is not a clause of the ISO general format named in the message, whose printed "
        + "syntax diagram is a CLOSED list: it enumerates the clauses that may be specified and admits nothing "
        + "else. The grammar used to end five such lists in a shared vendor-extension catch-all matching any run "
        + "of words, so a misspelled clause word, a clause written in the wrong paragraph, and every clause the "
        + "compiler had not implemented were accepted and SILENTLY DISCARDED — the program then compiled and ran "
        + "with the clause's intended effect simply absent. ⛔ WHY AN ERROR AND NOT THE §4.2.2 WARNING: that "
        + "subclause's second paragraph obliges an implementation to be ABLE to \"indicate violations of the "
        + "general formats and the explicit syntax rules of standard COBOL\", which is the floor; its FIRST "
        + "paragraph fixes what may be accepted — \"An implementation shall accept the syntax and provide the "
        + "functionality for all standard language elements required by this Working Draft International "
        + "Standard and the optional or processor-dependent language elements for which support is claimed\" — "
        + "and an unrecognized word run is neither. Refused at every edition and every strictness, because this "
        + "implementation declares no vendor dialect under which an extension clause could be admitted, and a "
        + "vendor extension is admitted only under the dialect that owns it, never by a catch-all. The clauses "
        + "each format admits are listed in its own subclause, and are reproduced — rendered from the printed "
        + "page, not read off the OCR — above each alternative list in the grammar.",
        "ISO §4.2.2 / §13.4.5.2 / §13.4.6.2 / §12.4.5.1 / §12.4.6.2 / §12.3.7.2");

    /// <summary>COBOLNET1971 — a paragraph header that is not one of the paragraphs its enclosing general format
    /// lists: the configuration section (§12.3.2, four bracketed paragraphs) and the identification division
    /// (§11.2.1). Distinct from COBOLNET1970 because the offending construct is a PARAGRAPH, not a clause, and
    /// naming it a clause would send the reader to the wrong subclause.</summary>
    public static readonly DiagnosticDescriptor ClosedFormatUnrecognizedParagraph = new(
        "COBOLNET1971", "closed-format-unrecognized-paragraph", EditionSeverity.Error,
        "A paragraph header here is not one of the paragraphs the enclosing ISO general format lists, and that "
        + "list is CLOSED. §12.3.2 admits exactly source-computer-paragraph, object-computer-paragraph, "
        + "special-names-paragraph and repository-paragraph; §11.2.1 admits the seven source-unit paragraphs "
        + "(PROGRAM-ID, FUNCTION-ID, CLASS-ID, FACTORY, OBJECT, METHOD-ID, INTERFACE-ID) plus OPTIONS, and — "
        + "below COBOL-2002, where they had not yet been removed — the AUTHOR, INSTALLATION, DATE-WRITTEN, "
        + "DATE-COMPILED and SECURITY comment-entry paragraphs. The grammar used to end both lists in a "
        + "vendor-extension catch-all matching any run of words, so a misspelled or misplaced paragraph header "
        + "was accepted and SILENTLY DISCARDED together with everything written under it. Refused at every "
        + "edition and every strictness (§4.2.2). ⚠ The COMMENT-ENTRY bodies of the removed paragraphs are "
        + "a different matter and are untouched: a comment-entry is arbitrary text by definition, which is why "
        + "ISO/IEC 1989:2023 defines no syntax for one anywhere.",
        "ISO §4.2.2 / §12.3.2 / §11.2.1");
    /// <summary>COBOLNET1994 — the INTO phrase written where its verb's ADMISSIBILITY rule does not admit it:
    /// ISO §14.9.30.3 syntax rule 1 for READ, §14.9.34.3 syntax rule 2 for RETURN (kb/Work PB337). ONE descriptor
    /// because it is one rule stated twice with one difference — the FD arm admits a record-less description
    /// entry and the SD arm does not — and the site quotes the verb's own sentence.</summary>
    public static readonly DiagnosticDescriptor IntoPhraseReceiverNotAdmissible = new(
        "COBOLNET1994", "into-phrase-receiver-not-admissible", EditionSeverity.Error,
        "ISO §14.9.30.3 syntax rule 1 (READ) and §14.9.34.3 syntax rule 2 (RETURN) each admit the INTO phrase on "
        + "exactly two grounds: a) the file description entry has at most one record description subordinate to "
        + "it (the RETURN wording is \"only one\"; READ's also admits \"no record description entry\"), or b) the "
        + "data item referenced by identifier-1 AND ALL record-names associated with file-name-1 \"describe an "
        + "alphanumeric group item or an elementary item of category alphanumeric or category national\". "
        + "Neither ground holds here. The phrase is an implicit MOVE from the record area (§14.9.30.4 GR4 b) / "
        + "§14.9.34.4 GR5 b)), and with several record descriptions sharing one area the area's content has no "
        + "single category — which is why the rule confines the multi-record case to the categories a group move "
        + "copies without conversion. An alphanumeric group item is §13.18.29.4 GR3's: no GROUP-USAGE clause "
        + "specified or implied, not strongly typed, not a variable-length group; a GROUP-USAGE NATIONAL group "
        + "qualifies through GR2 b) as an elementary item of category national. Read into the record area and "
        + "move from the record-name you mean, or reduce the file description entry to one record description.",
        "ISO §14.9.30.3 SR1 / §14.9.34.3 SR2");

    /// <summary>COBOLNET1995 — a STRONGLY-TYPED identifier-1 on an INTO phrase whose file description entry has
    /// the wrong NUMBER of record areas: ISO §14.9.30.3 syntax rule 2 for READ ("at most one"), §14.9.34.3 syntax
    /// rule 3 for RETURN ("exactly one") — kb/Work PB337. The COUNT obligation only; each rule's second sentence
    /// (the record area shall be a strongly-typed group item of the SAME type) is the same predicate over the
    /// same pair that §14.9.25.3 SR2 applies to the implicit move's sender, and is reported there — COBOLNET1533
    /// — rather than written down a second time.</summary>
    public static readonly DiagnosticDescriptor IntoPhraseStrongReceiverRecordAreas = new(
        "COBOLNET1995", "into-phrase-strong-receiver-record-areas", EditionSeverity.Error,
        "ISO §14.9.30.3 syntax rule 2: \"If identifier-1 is a strongly-typed group item, there shall be at most "
        + "one record area subordinate to the FD for file-name-1.\" §14.9.34.3 syntax rule 3 says the same of "
        + "RETURN with \"exactly one\", an SD being required to have a record description entry at all "
        + "(§13.4.6.3 SR2). The INTO phrase is an implicit MOVE into identifier-1, and a strongly-typed group "
        + "accepts only a whole-record source of its own type (§8.5.3.3); several record descriptions share one "
        + "record area, so which type the area holds is not decidable from the statement. The rule is reachable "
        + "only from COBOL-2002, the edition that introduced the TYPEDEF and TYPE clauses — below it there is no "
        + "strongly-typed item to be identifier-1.",
        "ISO §14.9.30.3 SR2 / §14.9.34.3 SR3");
    /// <summary>COBOLNET1997 — the <c>IN alphabet-name-1</c> phrase of a THROUGH range names a word that is not a
    /// declared alphabet. kb/Work PB398: the phrase had no grammar at all in EVALUATE, so none of §14.9.13.3 SR3
    /// had anywhere to be enforced.</summary>
    public static readonly DiagnosticDescriptor RangeAlphabetUndeclared = new(
        "COBOLNET1997", "range-alphabet-undeclared", EditionSeverity.Error,
        "The IN phrase of a THROUGH range (ISO §14.9.13.2's range-expression, and the VALUE clause's condition-name "
        + "list, §13.18.63.2 formats 3 and 5) names alphabet-name-1: an alphabet declared by an ALPHABET clause in "
        + "SPECIAL-NAMES (§12.3.7). The word written here declares no alphabet. §14.7.8 rule 2 makes that alphabet "
        + "the collating sequence the range is evaluated in, so an unresolved name has no ordering to give: declare "
        + "the alphabet, or drop the IN phrase and take the implementor's default sequence.",
        "ISO §14.9.13.3 SR3 / §13.18.63.3 SR31 / §12.3.7");

    /// <summary>COBOLNET1998 — §14.9.13.3 SR3 sentence 1 / §13.18.63.3 SR31 sentence 1: alphabet-name-1 may be
    /// written only over a range whose operands are of a class a collating sequence orders.</summary>
    public static readonly DiagnosticDescriptor RangeAlphabetOperandClass = new(
        "COBOLNET1998", "range-alphabet-operand-class", EditionSeverity.Error,
        "ISO §14.9.13.3 syntax rule 3: \"Alphabet-name-1 may be specified only when the literals or identifiers "
        + "specified in the THROUGH phrase are of class alphabetic, alphanumeric, or national.\" (The VALUE clause's "
        + "twin, §13.18.63.3 SR31, names class alphanumeric or national.) A NUMERIC range is ordered algebraically "
        + "by §14.7.8 rule 1 — \"the range of values includes literal-1, literal-2, and all algebraic values between\" "
        + "— and a collating sequence has no part in it, so naming one here has no meaning. Remove the IN phrase.",
        "ISO §14.9.13.3 SR3 / §13.18.63.3 SR31");

    /// <summary>COBOLNET1999 — §14.9.13.3 SR3 sentence 2 / §13.18.63.3 SR31 sentence 2: the CLASS of the alphabet
    /// shall match the class of the range's operands.</summary>
    public static readonly DiagnosticDescriptor RangeAlphabetClassMismatch = new(
        "COBOLNET1999", "range-alphabet-class-mismatch", EditionSeverity.Error,
        "ISO §14.9.13.3 syntax rule 3: \"If literal-3 or identifier-3 is of class national, alphabet-name-1 shall "
        + "reference an alphabet that defines a national collating sequence; otherwise, alphabet-name-1 shall "
        + "reference an alphabet that defines an alphanumeric collating sequence.\" The two classes are disjoint "
        + "reference domains (§12.3.6.3 SR1/SR2): an ALPHABET … FOR NATIONAL clause declares the national one, a plain "
        + "ALPHABET clause the alphanumeric one. An alphabet that names a coded character set ONLY — UTF-8 and "
        + "UTF-16, whose collating-sequence column in §12.3.7.4 Table 6 is empty — defines no sequence of either "
        + "class and is refused here for the same reason.",
        "ISO §14.9.13.3 SR3 / §13.18.63.3 SR31");

    // ── COBOLNET1984–1985 — the EXTENDED editing sign control symbols as a SET (ISO §13.18.40.3 SR24 and SR25,
    //    each SECOND sentence) — kb/Work PB530. Both rules are stated over the WHOLE list of EDITING phrases, so
    //    neither is askable while validating one phrase, and neither was asked: `PIC 9L9F9G` with three FOR
    //    phrases bound clean, and the phrase order the standard fixes was never compared to the symbol order.
    //    Sentence 1 of each rule needs no code — Table 10 carries it (COBOLNET1935). ──

    /// <summary>COBOLNET1984 — the two EDITING phrases of a two-extended-symbol PICTURE are written in the
    /// reverse order of their symbols (ISO §13.18.40.3 SR25, second sentence).</summary>
    public static readonly DiagnosticDescriptor PictureEditingPhraseOrder = new(
        "COBOLNET1984", "picture-editing-phrase-order", EditionSeverity.Error,
        "ISO §13.18.40.3 syntax rule 25, second sentence: \"When extended editing sign control symbols are used "
        + "and two are specified, the first occurrence of the EDITING phrase shall be for the leftmost symbol in "
        + "character-string-1 and the second occurrence shall be for the rightmost symbol in character-string-1.\" "
        + "The phrases here are written in the reverse order of the symbols they are for. The order is not "
        + "cosmetic: each extended symbol renders its own literal at its own position, so `PIC F999.99L` with "
        + "the phrases reversed renders -1.5 as \")001.50(\" where the conforming spelling renders \"(001.50)\". "
        + "⛔ \"The leftmost symbol\" is read as the leftmost OF THE TWO extended symbols the sentence names, so "
        + "this rule constrains the PHRASE order and not the symbols' placement — the alternative reading, that "
        + "the two shall also be character-string-1's first and last symbols, is not taken because the only "
        + "other text that would place an extended symbol (§13.18.40.6: 'es' takes \"the same precedence as the "
        + "'cs' symbol in the column and row of non-floating insertion symbols\") cannot be applied literally "
        + "without rejecting the standard's own Annex D.24 example, `PIC IS L9999.99F` with two FOR phrases, "
        + "against Table 10's blank leading-currency-before-trailing-currency cell (kb/Work PB528, PB530).",
        "ISO §13.18.40.3 SR25");

    /// <summary>COBOLNET1985 — more than two extended editing sign control symbols in one PICTURE clause (ISO
    /// §13.18.40.3 SR24, second sentence).</summary>
    public static readonly DiagnosticDescriptor PictureEditingExtendedCount = new(
        "COBOLNET1985", "picture-editing-extended-count", EditionSeverity.Error,
        "ISO §13.18.40.3 syntax rule 24, second sentence: \"For extended editing sign control symbols, either "
        + "one or two extended editing sign control symbols may be used in character-string-1.\" A third FOR "
        + "phrase exceeds that maximum. An extended symbol is the FOR form alone (SR12: \"If literal-1 is "
        + "specified, character-1 is a fixed editing sign control symbol. If the FOR phrase is specified, "
        + "character-1 is an extended editing sign control symbol\"), so any number of IS-form (simple "
        + "insertion) phrases is untouched by this rule. The bound is what makes SR25's second sentence "
        + "well-formed — it pairs the FIRST phrase with the leftmost symbol and the SECOND with the rightmost, "
        + "and says nothing about a third (kb/Work PB530).",
        "ISO §13.18.40.3 SR24");

    // ── COBOLNET1981/1982/1983 — the INITIALIZE category-name, once it became the standard's SET of thirteen
    //    words (kb/Work PB415). 1981 closes an OVER-acceptance the old scalar shape hid; 1982 and 1983 are the
    //    two syntax rules (§14.9.20.3 SR3 and SR4) that were unreachable while the five pointer-ish category
    //    names could not be spelled at all. ──

    /// <summary>COBOLNET1981 — <c>INITIALIZE … TO VALUE</c> written with neither ALL nor a category-name.</summary>
    public static readonly DiagnosticDescriptor InitializeValueChoiceMissing = new(
        "COBOLNET1981", "initialize-value-choice-missing", EditionSeverity.Error,
        "The VALUE phrase of an INITIALIZE statement is printed `{ ALL | category-name } TO VALUE` — a BRACE, and "
        + "§5.2.6.3 says \"the syntax element contained within the braces or one of the alternatives contained "
        + "within the braces shall be explicitly specified or is implicitly selected\". Nothing is implicitly "
        + "selected here, so one of ALL and a category-name shall be written. The compiler used to accept the "
        + "bare form and read it as ALL, defended by a \"§14.9.20.2 note 2\" the clause does not carry: that "
        + "subclause has no notes, and the general rule that does mention ALL (§14.9.20.4 GR2) answers what ALL "
        + "MEANS, not whether the choice may be omitted. Write `ALL TO VALUE` for the meaning the omission used "
        + "to be given. TO itself is an optional word (it is not underlined), so `ALL VALUE` is equally correct.",
        "ISO §14.9.20.2 / §5.2.6.3");

    /// <summary>COBOLNET1982 — literal-1 where §14.9.20.3 SR3 requires identifier-2: a REPLACING category-name of
    /// DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE or PROGRAM-POINTER.</summary>
    public static readonly DiagnosticDescriptor InitializeReplacingPointerNeedsIdentifier = new(
        "COBOLNET1982", "initialize-replacing-pointer-needs-identifier", EditionSeverity.Error,
        "§14.9.20.3 SR3: \"For each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or "
        + "PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be "
        + "specified.\" literal-1 is what the rule excludes, and it excludes it because §14.9.20.4 GR4 makes the "
        + "implicit statement for exactly those five categories a SET — `SET receiving-operand TO "
        + "sending-operand` — and no format of the SET statement (§14.9.39) admits a literal as its sending "
        + "operand. Name a data item of the same category instead.",
        "ISO §14.9.20.3 / §14.9.20.4 / §14.9.39");

    /// <summary>COBOLNET1983 — §14.9.20.3 SR4's SET validity for a pointer / object-reference REPLACING pair:
    /// identifier-2's category does not agree with the category-name.</summary>
    public static readonly DiagnosticDescriptor InitializeReplacingSetCategoryMismatch = new(
        "COBOLNET1983", "initialize-replacing-set-category-mismatch", EditionSeverity.Error,
        "§14.9.20.3 SR4: \"For each of the categories data-pointer, function-pointer, message-tag, "
        + "object-reference, and program-pointer specified in the REPLACING phrase, a SET statement with "
        + "identifier-2 as the sending operand and an item of the specified category as the receiving operand "
        + "shall be valid.\" The SET statement's pointer and object-reference formats (§14.9.39) admit only a "
        + "sending operand of the receiver's own category, so identifier-2 shall be an item of the category the "
        + "category-name names. ⚠ This is the NECESSARY condition only: whether two object references conform is "
        + "the §9.3.8.2 question the SET statement itself answers, and it is not restated here.",
        "ISO §14.9.20.3 / §14.9.39");

    // ── COBOLNET2012 / COBOLNET2013 — THE MULTI-OPERAND REPETITION RULE, ONE PER CLAUSE (kb/Work PB506) ──
    // ISO §13.18.63.3 SR35 (VALUE format 4) and §13.18.53.3 SR6 (SOURCE) are the SAME rule written twice, and
    // the compiler screens them through ONE reader (DataBinder.Reports.ScreenRepeatingOperandCount). They keep
    // SEPARATE codes because a reader who sees one has to be sent to the clause actually written — the shared
    // mechanism is an implementation fact, the citation is the user's.
    /// <summary>COBOLNET2012 — a report-section (format 4) VALUE clause with more than one operand on an entry
    /// that is not repeating, or whose operand count does not match the entry's repetitions. Before kb/Work
    /// PB506 the whole operand list was collapsed to one glued string, so `VALUE "AAA" "BBB" "CCC"` on a
    /// three-column entry printed AAA three times and the illegal counts were indistinguishable from the
    /// legal one — a silent wrong answer on conforming source and silent acceptance of non-conforming source.</summary>
    public static readonly DiagnosticDescriptor ReportValueOperandCount = new(
        "COBOLNET2012", "report-value-operand-count", EditionSeverity.Error,
        "A report-section VALUE clause (format 4) with more than one operand requires the entry to be a "
        + "repeating entry or to be subordinate to a repeating entry, and its operand count shall equal the "
        + "entry's number of repetitions or that number multiplied by the repetitions of successive higher "
        + "repeating entries.", "ISO §13.18.63.3 SR35 / §13.15.4 GR3");

    /// <summary>COBOLNET2013 — the SOURCE clause twin of COBOLNET2012 (ISO §13.18.53.3 SR6). The multi-operand
    /// SOURCE clause had no grammar surface at all before kb/Work PB506 (`SOURCES ARE A B C` was a raw parse
    /// error on conforming source), so this rule had nothing to screen.</summary>
    public static readonly DiagnosticDescriptor ReportSourceOperandCount = new(
        "COBOLNET2013", "report-source-operand-count", EditionSeverity.Error,
        "A SOURCE clause with more than one operand requires the entry to be a repeating entry or to be "
        + "subordinate to a repeating entry, and its operand count shall equal the entry's number of "
        + "repetitions or that number multiplied by the repetitions of successive higher repeating entries.",
        "ISO §13.18.53.3 SR6 / §13.15.4 GR3");

    // ── COBOLNET2009–2010 — the RECORD clause's own size syntax rules (ISO §13.18.43.3) ────────────────
    // ⛔ TWO CODES, BECAUSE THE TWO KINDS OF VIOLATION HAVE DIFFERENT SUBJECTS AND DIFFERENT REMEDIES. SR5 and
    // SR9 are about the CLAUSE's own pair of integers and are repaired by editing the clause; SR3 and SR4 are
    // about a RECORD DESCRIPTION ENTRY disagreeing with the clause and may be repaired at either end. Both arms
    // are rows of ONE table, `Binding.RecordClauseRules`, run from `DataBinder.ResolveFiles` (kb/Work PB721).

    /// <summary>COBOLNET2009 — the RECORD clause's own integer pair is inconsistent: ISO §13.18.43.3 SR5
    /// ("Integer-3 shall be greater than integer-2") for Format 2, SR9 ("Integer-5 shall be greater than
    /// integer-4") for Format 3. The two rules are the same sentence one format apart, so they share one code
    /// and the message names the operands of the format the program actually wrote.
    /// <para>⚠ NOT the place for the lower bound itself: §13.18.43.3 SR7/SR8 permit integer-2 / integer-4 to be
    /// ZERO ("shall be greater than or equal to zero" — the express override of §5.5 1)'s nonzero default), so
    /// <c>RECORD IS VARYING IN SIZE FROM 0</c> is legal COBOL and draws nothing. A NEGATIVE lower bound cannot be
    /// written at all: §5.5 1) makes every <c>integer-n</c> an unsigned literal and the general format spells it
    /// with the grammar's unsigned <c>integerLiteral</c>, so the minus sign is refused at parse.</para></summary>
    public static readonly DiagnosticDescriptor RecordClauseSizeRange = new(
        "COBOLNET2009", "record-clause-size-range", EditionSeverity.Error,
        "A RECORD clause states a maximum record size that is not greater than its minimum. ISO §13.18.43.3 "
        + "syntax rule 5 requires \"Integer-3 shall be greater than integer-2\" of the Format 2 "
        + "(RECORD IS VARYING IN SIZE FROM integer-2 TO integer-3) clause, and syntax rule 9 requires "
        + "\"Integer-5 shall be greater than integer-4\" of the Format 3 (RECORD CONTAINS integer-4 TO "
        + "integer-5) clause. Equal bounds break the rule as surely as inverted ones — the standard says "
        + "GREATER, not \"greater than or equal\", and a fixed-size file is written in Format 1. Until "
        + "kb/Work PB721 an inverted range compiled clean and the program learned about it only as an I-O "
        + "status '44' from §13.18.43.4 GR14 a) at run time, on the first WRITE.",
        "ISO §13.18.43.3 SR5 / SR9");

    /// <summary>COBOLNET2010 — a record description entry of the file describes a record whose size falls outside
    /// the range the FD's RECORD clause states: ISO §13.18.43.3 SR3 (Format 1) and SR4 (Format 2, whose single
    /// sentence states TWO obligations — "neither … a lesser number of bytes than … integer-2 nor … a greater
    /// number of bytes than … integer-3" — and therefore has one row per arm, kb/Work PB743's clamp).
    /// <para>The sizes compared are the standard's own: §13.18.43.4 GR8 a) and b), the sum over the record's
    /// elementary items excluding redefinitions and renamings with every occurs-depending table at its minimum
    /// and at its maximum occurrence count respectively. Format 3 has NO such rule — GR18 says the size of each
    /// record "is completely defined in the record description entry" — so no row screens it.</para></summary>
    public static readonly DiagnosticDescriptor RecordClauseDescriptionSize = new(
        "COBOLNET2010", "record-clause-description-size", EditionSeverity.Error,
        "A record description entry associated with a file description entry describes a record whose size is "
        + "outside the range its RECORD clause states. ISO §13.18.43.3 syntax rule 3 says of the Format 1 "
        + "clause \"No record description entry for the file may specify a number of bytes greater than "
        + "integer-1\", and syntax rule 4 says of the Format 2 clause \"Record descriptions for the file shall "
        + "describe neither records that contain a lesser number of bytes than that specified by integer-2 nor "
        + "records that contain a greater number of bytes than that specified by integer-3\". A record "
        + "description's byte count is §13.18.43.4 GR8's: the sum over its elementary items excluding "
        + "redefinitions and renamings, with an occurs-depending table contributing its minimum occurrences for "
        + "the lower comparison (GR8 a) and its maximum for the upper (GR8 b), and a dynamic-length elementary "
        + "item contributing zero for the lower and its maximum size (§8.5.1.10.1: the smallest of its LIMIT "
        + "phrase, the largest integer its prefixed usage can store and the implementor maximum) for the upper — "
        + "so a record with an unbounded dynamic-length member is refused by a fixed or bounded RECORD clause "
        + "(kb/Work PB1562).",
        "ISO §13.18.43.3 SR3 / SR4");

    /// <summary>COBOLNET2027 — the DYNAMIC LENGTH clause's LIMIT phrase asks for more characters than this
    /// implementation's maximum for a dynamic-length elementary item (ISO §8.5.1.10.1 / Annex A.1 item 62).
    /// kb/Work PB463: an out-of-range LIMIT used to be dropped by an <c>int.TryParse</c> and leave the item with
    /// NO bound at all, which is how a SET SIZE request past 2³² wrapped to its low bits.</summary>
    public static readonly DiagnosticDescriptor DynLengthLimitAboveImplementorMaximum = new(
        "COBOLNET2027", "dyn-length-limit-above-implementor-maximum", EditionSeverity.Warning,
        "ISO §8.5.1.10.1: \"The maximum size of a dynamic-length elementary item is smallest of: the value "
        + "declared in the LIMIT phrase; the largest integer that can be stored in an item of the usage specified "
        + "in the PREFIXED phrase; the maximum permitted by the implementor.\" The LIMIT phrase written here is "
        + "larger than the maximum permitted by this implementor (see docs/CONFORMANCE.md §7 row DOC-A.1-62), so "
        + "the implementor maximum is the item's maximum size and the phrase cannot raise it. The entry is LEGAL "
        + "— §13.18.19.3 states no rule bounding integer-1 unless a dynamic-length-structure-name is also written "
        + "(SR4) — which is why this reports rather than rejects; it exists so a program that plans on the larger "
        + "bound learns of the smaller one at compile time instead of through a clamped SET SIZE and a runtime "
        + "EC-STORAGE-NOT-AVAIL.",
        "ISO §8.5.1.10.1 / §13.18.19.4 GR2 / Annex A.1 item 62");
    // ── COBOLNET2045/2046 — the SUM clause's OPERANDS, once the addend became the written reference it always
    //    was (kb/Work PB482). Both rules had NO site at all: the addend's category (SR5) and the UPON operand's
    //    identity as a DETAIL (SR7) were never asked, so `SUM WS-TXT` over a PIC X(6) and `UPON <a control
    //    footing>` both compiled clean and produced a wrong total. The cross-report halves stay in the shared
    //    COBOLNET0899 staging family, which is where every other unimplemented Report Writer feature lives. ──

    /// <summary>COBOLNET2045 — a SUM addend that is not a numeric data item (§13.18.54.3 SR5), including every
    /// reference-modified spelling.</summary>
    public static readonly DiagnosticDescriptor ReportSumAddendNotNumeric = new(
        "COBOLNET2045", "report-sum-addend-not-numeric", EditionSeverity.Error,
        "§13.18.54.3 SR5: \"If the addend is identifier-1, it shall specify a numeric data item not defined in "
        + "the report section.\" The addend's content is added into the sum counter by the implicit ADD of "
        + "§13.18.54.4 GR3, which has no meaning for a group item or a non-numeric category. A REFERENCE-MODIFIED "
        + "addend fails the same rule for a reason worth spelling out: §8.4.3.3.4 GR6 c) makes the unique data "
        + "item reference modification creates \"class and category alphanumeric\" unless the usage is national, "
        + "so no reference-modified spelling can be the numeric data item SR5 requires. (The SUBSCRIPTED "
        + "spelling is legal and supported — identifier-1 is §8.4.3.1.2 Format 2's "
        + "qualified-data-name-with-subscripts.)",
        "ISO §13.18.54.3 / §13.18.54.4 / §8.4.3.3.4");

    /// <summary>COBOLNET2046 — an <c>UPON</c> operand that is not the name of a detail, or is qualified by
    /// something other than a single report-name (§13.18.54.3 SR7).</summary>
    public static readonly DiagnosticDescriptor ReportSumUponNotDetail = new(
        "COBOLNET2046", "report-sum-upon-not-detail", EditionSeverity.Error,
        "§13.18.54.3 SR7: \"Data-name-2 shall be the name of a detail. It may be qualified only by a "
        + "report-name.\" The UPON phrase names the GENERATE events that accumulate the addend (§13.18.54.4 GR7 "
        + "c) 2)), and only a detail report group is the operand of a GENERATE statement (§14.9.16.3 SR1), so a "
        + "name that is not a detail names an event that can never occur. The one qualifier the rule allows is "
        + "the report-name of §8.4.2.2.2 Format 1; a subscript or a reference modifier is not admitted at all, "
        + "since §8.4.2.3.3 SR2 permits a subscript only for an item with an OCCURS clause and §8.4.3.3.3 SR5's "
        + "NOTE bars reference modification wherever a general format writes data-name-n.",
        "ISO §13.18.54.3 / §13.18.54.4 / §14.9.16.3");

    /// <summary>COBOLNET2024 — a file's clause operand is written in a shape a <i>data-name-n</i> position does
    /// not admit (kb/Work PB489). Where a general format prints data-name-n the reference is a
    /// QUALIFIED-DATA-NAME — ISO §8.4.2.2.2 Format 1, <c>data-name-1 [ data-qualifier ] … [
    /// file-report-qualifier ]</c> — and the grammar's shared <c>dataReference</c> nonterminal also admits three
    /// shapes that are not one: a SPECIAL REGISTER (LINAGE-COUNTER / LINE-COUNTER / PAGE-COUNTER are §8.4.3.1
    /// Format 10 / Format 11 identifiers, and §8.4.3.14.3 SR1 / §8.4.3.15.3 SR1 confine them to the procedure
    /// division — and, for the report counters, a report-section SOURCE clause), a SUBSCRIPT (§8.4.2.3's
    /// qualified-data-name-WITH-subscripts is an identifier form, and each of these clauses independently forbids
    /// an operand subject to an OCCURS clause), and a REFERENCE-MODIFIER (§8.4.3.3.3's NOTE: "where data-name-n
    /// is used in a general format or syntax rule, then reference-modification is not permitted").
    /// <para>ONE code for the three because it is one obligation — the operand is not a qualified-data-name —
    /// and the message names which shape was written. The alternative was measured: the binder used to keep the
    /// FIRST word of the reference and discard the rest, so <c>LINAGE IS LINAGE-COUNTER OF LPF LINES</c> recorded
    /// the FILE NAME as the clause's data-name and died at OPEN naming a word the programmer never wrote as a
    /// data item (kb/Work PB489), and a subscripted or reference-modified key was accepted with the modifier
    /// silently dropped (kb/Work PB205).</para></summary>
    public static readonly DiagnosticDescriptor ClauseOperandNotADataName = new(
        "COBOLNET2024", "clause-operand-not-a-data-name", EditionSeverity.Error,
        "A clause operand (a file description or file control clause, or an OCCURS clause's DEPENDING, KEY or "
        + "CAPACITY phrase in the data or report section — §13.18.38.3 SR2/SR5/SR31), or a SORT or MERGE KEY "
        + "phrase operand (§14.9.40.2, §14.9.24.2; §14.9.40.3 SR14 b), written where the clause's "
        + "general format prints data-name-n is not a qualified-data-name (ISO §8.4.2.2.2 Format 1): it is a special register "
        + "(LINAGE-COUNTER, LINE-COUNTER or PAGE-COUNTER — §8.4.3.1 Format 10 / Format 11 identifiers, confined "
        + "to the procedure division by §8.4.3.14.3 SR1 and §8.4.3.15.3 SR1), or it carries a subscript "
        + "(§8.4.2.3 — an identifier form; the operand shall not be subject to any OCCURS clauses), or it is "
        + "reference-modified (§8.4.3.3.3 NOTE). Write a data-name, with IN/OF qualifiers if it needs them.",
        "ISO §8.4.2.2.2 / §8.4.2.3 / §8.4.3.1 / §8.4.3.3.3 / §8.4.3.14.3 / §8.4.3.15.3");

    /// <summary>COBOLNET2025 — the LINAGE clause's own operand syntax rules (ISO §13.18.34.3), screened at the
    /// file description entry once the data forest is indexed (kb/Work PB489). SR1 — "Data-name-1, data-name-2,
    /// data-name-3, and data-name-4 shall not be subject to any OCCURS clauses" — had no site at all: a LINAGE
    /// operand naming a table element compiled clean and killed the process at OPEN OUTPUT with a runtime
    /// "not resolvable to storage" throw, where §4.2.2 requires a compile-time indication.
    /// <para>SR2 (elementary unsigned numeric integer) and SR3 (integer-2 not greater than integer-1) are the
    /// other two tests of the same screen (kb/Work PB524) and report under this code, because the subject is the
    /// same — this clause's operand breaking one of its own syntax rules — and the message names the rule it
    /// caught.</para></summary>
    public static readonly DiagnosticDescriptor LinageClauseOperandRule = new(
        "COBOLNET2025", "linage-clause-operand-rule", EditionSeverity.Error,
        "A LINAGE clause operand breaks one of the clause's syntax rules (ISO §13.18.34.3): SR1 — \"Data-name-1, "
        + "data-name-2, data-name-3, and data-name-4 shall not be subject to any OCCURS clauses\"; SR2 — they "
        + "\"shall reference elementary unsigned numeric integer data items\"; SR3 — \"Integer-2 shall not be "
        + "greater than integer-1\". The site names the rule it caught.",
        "ISO §13.18.34.3");

    /// <summary>COBOLNET2026 — LINAGE-COUNTER as a RECEIVING operand (kb/Work PB489). §8.4.3.14.3 SR2: "The
    /// LINAGE-COUNTER identifier shall not be referenced as a receiving operand", and §13.18.34.4 GR7 b) gives
    /// the reason — "only the input-output control system may change the value of LINAGE-COUNTER".
    /// <para>⛔ IT EXISTS BECAUSE THE ARM WAS MISSING, NOT BECAUSE THE OUTCOME WAS. The receiving chokepoint
    /// screened LINE-COUNTER with a rule-citing rejection and PAGE-COUNTER with a correctly-labelled
    /// not-yet-implemented, while LINAGE-COUNTER — the only one of the three that is flatly ILLEGAL as a
    /// receiver — had no arm and inherited the catch-all, so permanently illegal source was reported as "a
    /// reference shape WiseOwl COBOL does not yet implement as a receiver" (COBOLNET0899). A user reads that as a
    /// promise and a future implementer reads it as a gap to close (feedback_two_arm_dispatch).</para></summary>
    public static readonly DiagnosticDescriptor LinageCounterReceiving = new(
        "COBOLNET2026", "linage-counter-receiving", EditionSeverity.Error,
        "LINAGE-COUNTER is referenced as a receiving operand. ISO §8.4.3.14.3 SR2: \"The LINAGE-COUNTER "
        + "identifier shall not be referenced as a receiving operand.\" §13.18.34.4 GR7 b) states the reason — "
        + "\"only the input-output control system may change the value of LINAGE-COUNTER\" — so this is a "
        + "permanent property of the language, not a feature awaiting implementation. The counter is set by "
        + "OPEN OUTPUT and by each WRITE (GR7 c / GR7 d); a program that needs its own line count shall keep it "
        + "in its own data item.",
        "ISO §8.4.3.14.3 SR2 / §13.18.34.4 GR7 b)");

    // ── COBOLNET2030/2031 — the two §14.9.20.3 screens INITIALIZE never asked at the one place identifier-1 and
    //    its REPLACING operands are resolved (kb/Work PB416). The statement's other two unasked rules need no new
    //    code: SR5 already had COBOLNET0835 (wired to a branch that could not reach a resolved RENAMES entry) and
    //    SR7 is a FUNNEL, so identifier-1 now resolves through ExpressionBinder.ResolveReceiving and collects that
    //    chokepoint's own receiving-operand diagnostics (COBOLNET1548 for a CONSTANT RECORD among them). ──

    /// <summary>COBOLNET2030 — identifier-1 of an INITIALIZE statement is of class index, the one class
    /// §14.9.20.3 SR1's list excludes.</summary>
    public static readonly DiagnosticDescriptor InitializeTargetClass = new(
        "COBOLNET2030", "initialize-target-class", EditionSeverity.Error,
        "§14.9.20.3 SR1: \"Identifier-1 shall be strongly typed or of class alphabetic, alphanumeric, boolean, "
        + "message-tag, national, numeric, object, or pointer.\" §8.5.2.1 Table 2 lists NINE classes and the rule "
        + "admits eight, so the whole rule is one exclusion: class INDEX — an elementary item explicitly or "
        + "implicitly described as usage index (§8.5.2.8). ⚠ It is a DIFFERENT rule from §14.9.20.4 GR5a1, which "
        + "excludes an index item CONTAINED IN identifier-1 from the receiver set and leaves it silently "
        + "unchanged; applying that exclusion to identifier-1 itself is what turned this syntax error into a "
        + "no-op, so a subordinate index item is still skipped without a word and only the target is diagnosed.",
        "ISO §14.9.20.3 / §8.5.2.1 / §8.5.2.8");

    /// <summary>COBOLNET2031 — §14.9.20.3 SR4's MOVE half: the implicit MOVE the REPLACING phrase names would not
    /// be a valid MOVE statement.</summary>
    public static readonly DiagnosticDescriptor InitializeReplacingMoveInvalid = new(
        "COBOLNET2031", "initialize-replacing-move-invalid", EditionSeverity.Error,
        "§14.9.20.3 SR4, second paragraph: \"For each of the other categories specified in the REPLACING phrase, "
        + "a MOVE statement with identifier-2 or literal-1 as the sending item and an item of the specified "
        + "category as the receiving operand shall be valid.\" §14.9.20.4 GR4 is what makes that a statement "
        + "about INITIALIZE — \"the effect of the execution of the INITIALIZE statement is as though a series of "
        + "implicit MOVE or SET statements\" — so every cell §14.9.25.3 refuses an explicit MOVE, it refuses "
        + "here. The receiving operand is the CATEGORY the REPLACING phrase names, not any particular item "
        + "identifier-1 contains, so the rule is decided once per REPLACING item and holds whether or not the "
        + "group happens to contain an item of that category. The same screen answers both statements "
        + "(MoveTable16), so an INITIALIZE REPLACING pair and the MOVE a programmer would write by hand can no "
        + "longer disagree.",
        "ISO §14.9.20.3 / §14.9.20.4 / §14.9.25.3");
    /// <summary>COBOLNET2048 — §13.18.63.3 SR27: the VALUE clause's <c>WHEN SET TO FALSE</c> literal-4 names a
    /// value the condition-name is TRUE for. kb/Work PB555: unreachable until <c>Condition88</c> carried
    /// literal-4 at all.</summary>
    public static readonly DiagnosticDescriptor FalseValueNotDistinct = new(
        "COBOLNET2048", "false-value-not-distinct", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 27: \"The value of literal-4 shall not be equal to the value of any "
        + "occurrence of literal-2. When the THROUGH phrase is specified: a) when literal-2 is of a class other "
        + "than alphanumeric or national, the value of literal-4 shall not be equal to any value in the range of "
        + "any occurrence of literal-2 through literal-3, inclusive. b) when literal-2 is of class alphanumeric "
        + "or national, and the runtime collating sequence is known, the value of literal-4 shall not be equal to "
        + "the value of any literal-2 or any value in the range of any occurrence of literal-2 through literal-3, "
        + "inclusive.\" literal-4 is the value §13.18.63.4 GR20 places in the conditional variable for "
        + "'SET condition-name TO FALSE', so naming a value inside the condition's own VALUE set would leave the "
        + "condition TRUE after a SET that says FALSE. Choose a literal-4 outside every VALUE and every VALUE "
        + "range. ⚠ b) is conditioned on the runtime collating sequence being KNOWN — SR26's note puts a LOCALE "
        + "sequence outside it — so a character range ordered by a LOCALE alphabet takes only the unconditional "
        + "first sentence.",
        "ISO §13.18.63.3 SR27 / §13.18.63.4 GR20 / §14.7.8");

    /// <summary>COBOLNET2049 — §14.9.39.3 SR7: <c>SET condition-name TO FALSE</c> over a condition-name whose
    /// VALUE clause writes no <c>WHEN SET TO FALSE</c> phrase. kb/Work PB555.</summary>
    public static readonly DiagnosticDescriptor SetFalseWithoutFalsePhrase = new(
        "COBOLNET2049", "set-false-without-false-phrase", EditionSeverity.Error,
        "ISO §14.9.39.3 syntax rule 7: \"If the FALSE phrase is specified, the FALSE phrase shall be specified in "
        + "the VALUE clause of the data description entry for condition-name-1.\" §14.9.39.4 GR7 places \"the "
        + "literal in the FALSE phrase of the VALUE clause associated with condition-name-1\" in the conditional "
        + "variable, and §13.18.63.4 GR20 says the same from the VALUE clause's side — with no such phrase there "
        + "is no value to place, and the standard states no default (NOTE 3 on GR20: \"The WHEN SET TO FALSE "
        + "phrase specifies just one of possibly many false values\", so the processor cannot choose one). Add "
        + "'WHEN SET TO FALSE IS literal-4' to the condition-name's VALUE clause.",
        "ISO §14.9.39.3 SR7 / §14.9.39.4 GR7 / §13.18.63.4 GR20");

    // ── COBOLNET2096/2097 — §8.4.2.3.3 SR2 and SR3, the two rules about a WRITTEN subscript list, screened at
    //    the one reference-resolution site (kb/Work PB877). Both were decidable there and neither was decided:
    //    the resolver returned a bare null and what the programmer saw depended on WHICH SIDE of the statement
    //    the reference was on — the receiving chokepoint's catch-all promised `PLAIN(1)` was "a reference shape
    //    WiseOwl COBOL does not yet implement as a receiver" (COBOLNET0899, a promise about permanently illegal
    //    source, the PB489 shape again), while the identical SENDING reference compiled clean and aborted at run
    //    time. §4.2.2 requires a compile-time mechanism for a syntax-rule violation. ──

    /// <summary>COBOLNET2096 — §8.4.2.3.3 SR2: a subscript is written on a data item whose description neither
    /// contains an OCCURS clause nor is subordinate to one, so no subscript may be written on it at all.</summary>
    public static readonly DiagnosticDescriptor SubscriptOnNonTableItem = new(
        "COBOLNET2096", "subscript-on-non-table-item", EditionSeverity.Error,
        "A subscript is written on a data item that is not a table element. ISO §8.4.2.3.3 SR2: \"If a subscript "
        + "is specified, the data description entry describing qualified-data-name-1 or the conditional variable "
        + "associated with qualified-condition-name-1 shall contain an OCCURS clause or shall be subordinate to a "
        + "data description entry that contains an OCCURS clause.\" Both halves count — an item SUBORDINATE to an "
        + "OCCURS is a legal subscripted reference even though its own entry carries no OCCURS clause — and an "
        + "item with neither is a permanent property of the program's data description, not a feature awaiting "
        + "implementation. Remove the subscript, or describe the item (or a containing group) with an OCCURS "
        + "clause. ⚠ If the parenthesis was meant to be reference modification, write the colon form "
        + "(§8.4.3.3) — `ITEM (1:4)`.",
        "ISO §8.4.2.3.3 SR2");

    /// <summary>COBOLNET2097 — §8.4.2.3.3 SR3: the reference writes a number of subscripts other than the number
    /// of OCCURS clauses in the description of the table element being referenced.</summary>
    public static readonly DiagnosticDescriptor SubscriptCountMismatch = new(
        "COBOLNET2097", "subscript-count-mismatch", EditionSeverity.Error,
        "A table element reference writes the wrong number of subscripts. ISO §8.4.2.3.3 SR3: \"Except as defined "
        + "in Syntax rule 5, when a reference is made to a table element, the number of subscripts shall equal the "
        + "number of OCCURS clauses in the description of the table element being referenced … When more than one "
        + "subscript is required, the subscripts are written in the order of successively less inclusive "
        + "dimensions of the table.\" Count every OCCURS clause on the item's own entry and on each of its parents, "
        + "and write one subscript for each, outermost first. ⚠ SR5 admits an OMITTED subscript list in seven "
        + "contexts — a SEARCH subject, a REDEFINES clause, an OCCURS KEY IS phrase, a SORT key or table subject, "
        + "a screen entry's FROM/TO/USING phrase and a report SUM addend — and this diagnostic is never raised for "
        + "a reference that writes none; writing too many has no such exception. ⚠ The SUBJECT of a Format-2 table "
        + "SORT omits only its OWN level (SR5 e), SR6 — a rightmost ALL says the same): it writes one subscript for "
        + "each ENCLOSING table, so this diagnostic is raised for a subject that writes more or fewer than that "
        + "(kb/Work PB1055).",
        "ISO §8.4.2.3.3 SR3");

    /// <summary>COBOLNET2270 — §8.4.2.3.3 SR5: a table element referenced with NO subscript outside the seven
    /// contexts that rule lists (kb/Work PB681). The third member of the written-subscript family 2096/2097 opened,
    /// screened at the same place: before it, the reference resolver returned an unreported null for the omitted
    /// list and the statement compiled into a run-time <c>NotImplemented</c> — <c>MOVE E TO B</c> over a table
    /// element compiled clean at every edition and aborted the run unit when the MOVE executed.</summary>
    public static readonly DiagnosticDescriptor TableElementNotSubscripted = new(
        "COBOLNET2270", "table-element-not-subscripted", EditionSeverity.Error,
        "A table element is referenced without subscripts. ISO §8.4.2.3.3 SR5: \"Each table element reference "
        + "shall be subscripted except when such reference appears: a) As the subject of a SEARCH statement. b) In "
        + "a REDEFINES clause. c) In the KEY IS phrase of an OCCURS clause. d) In the KEY phrase of a SORT statement "
        + "that references a table. e) As the subject of a SORT statement that references a table … f) In the FROM, "
        + "TO, or USING clause of a screen description entry when the subject of the entry has an OCCURS clause. g) "
        + "As a data-name addend in the SUM clause of a report description entry.\" A table element is an item whose "
        + "description contains an OCCURS clause or is subordinate to one; write one subscript per OCCURS clause, "
        + "outermost first (SR3), or name the containing group instead if the whole table was meant.",
        "ISO §8.4.2.3.3 SR5");

    // ── COBOLNET2106 / COBOLNET2107 — EVALUATE's TWO REMAINING SYNTAX-RULE SCREENS (kb/Work PB399) ───────
    //    §14.9.13.3 SR2 (the selection-object count) and SR4/SR9 (the range-expression's operands). Both were
    //    SYNTAX rules with no compile-time mechanism at all: SR2 was stood in for by a defensive index guard
    //    that returned a BoundUnsupported — a run-time abort, and only in the MORE-objects direction, while
    //    FEWER objects than subjects silently bound the written prefix of the pairs and branched on a strict
    //    subset of the statement's own selection subjects — and the range operands were handed straight to the
    //    operand binder with no admissibility question of any kind. §4.2.2 requires a compile-time mechanism
    //    for a syntax-rule violation.

    /// <summary>COBOLNET2106 — §14.9.13.3 SR2: a WHEN phrase writes a number of selection objects other than the
    /// number of selection subjects the EVALUATE statement declares. BOTH directions, because the rule is an
    /// equality and the positional correspondence SR7's stem requires is undefined the moment the counts
    /// differ.</summary>
    public static readonly DiagnosticDescriptor EvaluateSelectionObjectCount = new(
        "COBOLNET2106", "evaluate-selection-object-count", EditionSeverity.Error,
        "A WHEN phrase writes the wrong number of selection objects. ISO §14.9.13.3 SR2: \"The number of "
        + "selection objects within each set of selection objects shall be equal to the number of selection "
        + "subjects.\" The subjects are the operands of EVALUATE itself, separated by ALSO; the objects are the "
        + "operands of one WHEN phrase, separated by ALSO. Count them and make them equal — write ANY for a "
        + "position the phrase does not care about (SR7 c): \"The word ANY may correspond to a selection subject "
        + "of any type.\" ⚠ Each WHEN phrase is counted on its own: `WHEN 1 ALSO 2 WHEN 3` is TWO phrases "
        + "sharing one imperative-statement, and the second is a one-object phrase.",
        "ISO §14.9.13.3 SR2");

    /// <summary>COBOLNET2107 — the EVALUATE range-expression OPERAND band: §14.9.13.3 SR4 (the two ends shall be
    /// of the same class, and of none of four excluded classes) and SR9 (neither end shall reference a
    /// variable-length group). One band, because one screen over the range PAIR answers both and the rules are
    /// about the same two operands.</summary>
    public static readonly DiagnosticDescriptor EvaluateRangeOperandInvalid = new(
        "COBOLNET2107", "evaluate-range-operand", EditionSeverity.Error,
        "An EVALUATE range-expression (`WHEN a THROUGH b`) has an inadmissible operand. ISO §14.9.13.3 SR4: "
        + "\"The two operands in a range-expression shall be of the same class and shall not be of class "
        + "boolean, message-tag, object, or pointer.\" SR9: \"Neither identifier-3 nor identifier-4 shall "
        + "reference a variable-length group.\" CLASS is §8.5.2.1 Table 2's, not category — so alphabetic and "
        + "alphanumeric are two classes, the three pointer categories are one, and numeric-edited takes the "
        + "class of its usage. A range over pointers has no meaning to give: §8.8.4.2.2 Format 3 (the "
        + "message-tag-object-or-pointer-reference relation condition) prints no ordering operator at all, so "
        + "the inclusive `>=`/`<=` pair a range lowers to (§14.9.13.4 GR4 a) 5.) is not a comparison the "
        + "standard defines. Write a list of WHEN phrases, or an explicit condition, instead of a range.",
        "ISO §14.9.13.3 SR4 / SR9");

    // ── COBOLNET2102 / COBOLNET2103 — THE TWO PLACEMENT RULES THE EXIT / GOBACK / RESUME FAMILY SHARES ───
    //    (kb/Work PB404, PB409, PB410). Each of the two is written down THREE TIMES in the standard, once per
    //    statement under its own ordinal — §14.9.14.3 SR2 / §14.9.18.3 SR1 / §14.9.33.3 SR2 for the GLOBAL
    //    declarative, and §14.9.14.3 SR6 / §14.9.18.3 SR5 / §14.9.33.3 SR1 for the "declarative or PERFORM WHEN"
    //    position — so ONE descriptor per RULE carries the shared explanation and each raising site supplies its
    //    own statement and its own ordinal (the EcRaiseSite value). RESUME keeps COBOLNET0712/0713: its rules are
    //    about the whole statement and carry a second sentence (the NEXT STATEMENT operand) these do not.

    /// <summary>COBOLNET2102 — the GLOBAL-declarative prohibition: ISO §14.9.14.3 SR2 (EXIT Format 2) and
    /// §14.9.18.3 SR1 (GOBACK), which are the same sentence about two statements.</summary>
    public static readonly DiagnosticDescriptor ReturnInGlobalDeclarative = new(
        "COBOLNET2102", "return-in-global-declarative", EditionSeverity.Error,
        "A statement that returns from the source element is written inside a declarative procedure whose USE "
        + "statement carries the GLOBAL phrase. ISO §14.9.18.3 SR1: \"The GOBACK statement shall not be specified "
        + "in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement\", "
        + "and §14.9.14.3 SR2 states the same prohibition for the Format-2 EXIT statement. A GLOBAL declarative "
        + "may be selected on behalf of a CONTAINED program (§14.9.49.4 GR4 b)) while running in the DECLARING "
        + "program's data, so \"return to the activator\" has no single meaning there. Move the return out of the "
        + "declarative — let the declarative fall through to its own end — or drop the GLOBAL phrase from the USE "
        + "statement. ⚠ A GOBACK written OUTSIDE the declarative but executed within its RANGE is legal source "
        + "and is governed by §14.9.18.4 GR6 (EC-FLOW-GLOBAL-GOBACK) instead, at run time.",
        "ISO §14.9.18.3 SR1 / §14.9.14.3 SR2");

    /// <summary>COBOLNET2103 — the RAISING LAST placement rule: ISO §14.9.18.3 SR5 (GOBACK) and §14.9.14.3 SR6
    /// (EXIT), which admit the phrase in exactly two positions.</summary>
    public static readonly DiagnosticDescriptor RaisingLastOutOfPlace = new(
        "COBOLNET2103", "raising-last-out-of-place", EditionSeverity.Error,
        "A RAISING LAST EXCEPTION phrase is written outside the two positions its syntax rule admits. ISO "
        + "§14.9.18.3 SR5: \"The LAST phrase may be specified only in a declarative procedure or WHEN phrase of a "
        + "PERFORM statement\", and §14.9.14.3 SR6 states the same for the EXIT statement. LAST names the "
        + "run-unit's last exception status (§14.9.18.4 GR1 b) 3.), which only a declarative procedure or an "
        + "exception-checking PERFORM's WHEN phrase is guaranteed to be running under. Write "
        + "RAISING EXCEPTION exception-name-1 instead, or move the statement into a declarative or a WHEN phrase. "
        + "⚠ The rule carries NO method qualifier: inside a method definition the WHEN-phrase position is "
        + "admitted exactly as it is in a program.",
        "ISO §14.9.18.3 SR5 / §14.9.14.3 SR6");

    /// <summary>COBOLNET2104 — ISO §5.2.6.4's "only once" half, for a general format whose brace or bracket
    /// carries CHOICE INDICATORS. The grammar expresses the zero-or-more and any-order halves with a repetition;
    /// this is the half a parser rule cannot state without enumerating every ordering (kb/Work PB407).</summary>
    public static readonly DiagnosticDescriptor ChoiceAlternativeRepeated = new(
        "COBOLNET2104", "choice-alternative-repeated", EditionSeverity.Error,
        "An alternative of a general format's choice-indicator group is specified more than once. ISO §5.2.6.4: "
        + "\"Choice indicators are a pair of bars, |, that enclose a portion of a general format. When enclosed "
        + "by braces, one or more of the alternatives contained within the choice indicators shall be specified, "
        + "but any single alternative may be specified only once\" — and the bracketed form admits ZERO or more "
        + "on the same terms. The alternatives may be written in any order; each may be written once. Delete the "
        + "repeated phrase.",
        "ISO §5.2.6.4");
    // ── COBOLNET2125 / COBOLNET2126 / COBOLNET2127 — REFERENCES TO AN OCCURS DYNAMIC CAPACITY REGISTER
    //    (kb/Work PB457). The register is NOT in ByName (it is a view over the table's capacity, never storage),
    //    so a reference the resolver's capacity hook refused was not falling back to a slower path — it was the
    //    END of resolution, and the general resolver then said COBOLNET1639 "'…' is not defined — no declaration
    //    in this source element gives the name '…'", which is false about a name §13.18.38.3 SR30 declares. Every
    //    ill-formed reference form now names the rule it actually breaks. ──

    /// <summary>COBOLNET2125 — §13.18.38.3 SR31: a subscript is written on a dynamic-capacity table's CAPACITY
    /// register.</summary>
    public static readonly DiagnosticDescriptor CapacityRegisterSubscripted = new(
        "COBOLNET2125", "capacity-register-subscripted", EditionSeverity.Error,
        "A subscript is written on the CAPACITY register of a dynamic-capacity table. ISO §13.18.38.3 SR31: "
        + "\"Data-name-3 shall not be subscripted.\" The register is a single numeric item holding the current "
        + "capacity of the associated table (§13.18.38.4 GR15), not one of the table's elements — write its name "
        + "alone, optionally qualified (§13.18.38.3 SR30), and subscript the table's elements instead.",
        "ISO §13.18.38.3 SR31");

    /// <summary>COBOLNET2126 — a CAPACITY register whose table is itself subordinate to a table: §13.18.38.3 SR30
    /// places the register inside that outer table, so §8.4.2.3.3 SR3/SR5 require a subscript that §13.18.38.3
    /// SR31 forbids, and no reference form is writable. ⚠ A DETERMINATION — see ReferenceResolver.CapacityPlaceOf
    /// for the reading chosen and the reading rejected.</summary>
    public static readonly DiagnosticDescriptor CapacityRegisterUnderTable = new(
        "COBOLNET2126", "capacity-register-under-table", EditionSeverity.Error,
        "A CAPACITY register is referenced whose dynamic-capacity table is itself subordinate to another table. "
        + "ISO §13.18.38.3 SR30 treats data-name-3 \"as though implicitly defined at the same level as the entry "
        + "containing the OCCURS clause\" — inside that outer table, and deliberately unlike an occurs-depending "
        + "item, which SR20 forces outside the table it sizes — so ISO §8.4.2.3.3 SR3 and SR5 require one "
        + "subscript per enclosing OCCURS clause while ISO §13.18.38.3 SR31 forbids subscripting data-name-3 at "
        + "all. No reference form satisfies both: each outer occurrence holds its own dynamic-capacity table with "
        + "its own capacity, and the bare name designates none of them. DEFINING such a table is legal "
        + "(§8.5.1.9.1: it \"may be nested in any combination to the same number of levels as a fixed-capacity "
        + "table\") — only naming and then referencing its register is not. Drop the CAPACITY phrase from the "
        + "nested table and read its current capacity with FUNCTION LENGTH over the subscripted inner table.",
        "ISO §13.18.38.3 SR30/SR31 · §8.4.2.3.3 SR3/SR5");

    /// <summary>COBOLNET2127 — §13.18.38.3 SR30 with §8.4.2.2.3 SR4: the OF/IN qualifiers written on a CAPACITY
    /// register reference do not name successively more inclusive context of its implied position.</summary>
    public static readonly DiagnosticDescriptor CapacityRegisterQualifier = new(
        "COBOLNET2127", "capacity-register-qualifier", EditionSeverity.Error,
        "The OF/IN qualifiers written on a reference to a dynamic-capacity table's CAPACITY register do not name "
        + "its context. ISO §13.18.38.3 SR30 treats data-name-3 \"as though implicitly defined at the same level "
        + "as the entry containing the OCCURS clause\", so its qualifiers are the group names above that entry, "
        + "and ISO §8.4.2.2.3 SR4 requires them \"in the order of successively more inclusive levels in the "
        + "hierarchy\". Qualification is never REQUIRED here — SR30's first sentence makes data-name-3 unique in "
        + "the source element — but §8.4.2.2.3 SR2 permits it, and a written qualifier shall still be correct.",
        "ISO §13.18.38.3 SR30 · §8.4.2.2.3 SR4");

    // ── COBOLNET2072 / COBOLNET2073 — THE REQUIRED IMPERATIVE-STATEMENT OPERAND (kb/Work PB396) ──────────
    // Both are PARSE-layer diagnostics: the grammar rules that carry an imperative-statement operand cannot
    // match empty (`statementBlock` = `statement+`), so the violation arrives as a syntax error and
    // CobolErrorStrategy re-codes it. Registered here because every emitted code is a catalog descriptor —
    // that is what the next-free allocation scan and docs/DIAGNOSTICS.md read.

    /// <summary>An imperative-statement the general format shows OUTSIDE brackets (or stacked inside braces) was
    /// written empty — `IF X = 1 END-IF`, a WHEN with no body, `PERFORM UNTIL … END-PERFORM` with no body. The
    /// omission licence of §5.2.6.2 belongs to BRACKETED portions only, and §5.2.6.3 requires one alternative of
    /// a brace group to be explicitly specified; §14.9.19.3 SR1 states the same cardinality for IF a second
    /// way. Detected structurally from the ATN (the expected set admits everything that can start a statement
    /// block and the token starts none of it), so a general format added later is covered without new code.</summary>
    public static readonly DiagnosticDescriptor RequiredImperativeMissing = new(
        "COBOLNET2072", "required-imperative-missing", EditionSeverity.Error,
        "An imperative-statement operand that the general format leaves unbracketed was written empty.",
        "ISO §5.2.6.2 / §5.2.6.3 / §14.9.19.3 SR1");

    /// <summary>A WHEN OTHER phrase was written more than once, or ahead of a WHEN phrase. In EVALUATE's format
    /// `[ WHEN OTHER imperative-statement-2 ]` is ONE bracketed phrase that FOLLOWS the `{ … } …` repetition —
    /// §5.2.7 scopes an ellipsis to the portion between the matching delimiters immediately to its left, so the
    /// OTHER phrase is outside it — and PERFORM Format 3 stacks `[ WHEN OTHER EXCEPTION imperative-statement-3 ]`
    /// the same way. §14.9.13.4 GR5 b) is written for exactly one such phrase.</summary>
    public static readonly DiagnosticDescriptor WhenOtherOutOfPosition = new(
        "COBOLNET2073", "when-other-out-of-position", EditionSeverity.Error,
        "A WHEN OTHER phrase is repeated or precedes a WHEN phrase.",
        "ISO §14.9.13.2 / §14.9.28.2 Format 3 / §5.2.7");

    // ── COBOLNET2117-2120 — the PERFORM statement's FORMAT rules (kb/Work PB431, PB432). ⛔ WHY THESE ARE
    //    ERRORS AND NOT THE §4.2.2 WARNING: the COBOLNET1970 reading. §4.2.2's SECOND paragraph obliges an
    //    implementation to be ABLE to "indicate violations of the general formats and the explicit syntax rules
    //    of standard COBOL", which is the floor; its FIRST paragraph fixes what may be ACCEPTED — "An
    //    implementation shall accept the syntax and provide the functionality for all standard language
    //    elements required by this Working Draft International Standard and the optional or processor-dependent
    //    language elements for which support is claimed" — and a construct the general format does not print is
    //    neither. Refused at every edition and every strictness: none of the four formats changed shape across
    //    1985/2002/2014/2023 (Format 3 itself is 2023-only and gated by COBOLNET0900, upstream of these). ──

    /// <summary>COBOLNET2117 — an inline PERFORM carries more than one loop-control phrase. §14.9.28.2 Format 2
    /// prints ONE pair of square brackets over three STACKED alternatives (times-phrase / until-phrase /
    /// varying-phrase), so at most one may be written. kb/Work PB431 measured the silent loss: the grammar spelt
    /// the head a repetition and <c>BindPerformControl</c> read <c>FirstOrDefault()</c>, so every phrase after
    /// the first never reached the bound tree at all — <c>PERFORM 3 TIMES UNTIL X &gt; 100</c> ran three times,
    /// <c>PERFORM UNTIL X &gt; 4 3 TIMES</c> ran five (the FIRST phrase always won, whichever it was), and
    /// <c>PERFORM 3 TIMES UNTIL … VARYING I FROM 1 …</c> left I never initialized.</summary>
    public static readonly DiagnosticDescriptor PerformInlineHeadMultipleControlPhrases = new(
        "COBOLNET2117", "perform-inline-head-multiple-control-phrases", EditionSeverity.Error,
        "An inline PERFORM statement specifies more than one loop-control phrase. The ISO §14.9.28.2 Format 2 "
        + "general format is PERFORM [ times-phrase | until-phrase | varying-phrase ] imperative-statement-1 "
        + "END-PERFORM: one bracket over three stacked alternatives, which admits at most one of them. A "
        + "varying-phrase already carries its own UNTIL condition; to iterate a fixed count under a further "
        + "condition, nest one inline PERFORM inside another.",
        "ISO §4.2.2 / §14.9.28.2 Format 2");

    /// <summary>COBOLNET2118 — an exception-checking (Format-3) PERFORM carries a loop-control phrase.
    /// §14.9.28.2 Format 3's head is <c>[ WITH LOCATION ]</c> and nothing else: it prints no times-phrase, no
    /// until-phrase and no varying-phrase. kb/Work PB431: the Formats-2/3 merge put both heads on one grammar
    /// alternative, and the Format-3 binder never asked for a control phrase, so every one written there was
    /// dropped and the body ran exactly ONCE — a programmer who believed exception checking wrapped three
    /// iterations got one, silently.</summary>
    public static readonly DiagnosticDescriptor PerformFormat3LoopControlPhrase = new(
        "COBOLNET2118", "perform-format3-loop-control-phrase", EditionSeverity.Error,
        "An exception-checking PERFORM statement (one with a WHEN, WHEN OTHER, WHEN COMMON or FINALLY phrase, or "
        + "a [WITH] LOCATION head) specifies a loop-control phrase. The ISO §14.9.28.2 Format 3 general format "
        + "prints none: its head is [ WITH LOCATION ], followed by imperative-statement-1 and the WHEN phrases. "
        + "Nest the exception-checking PERFORM inside an ordinary inline PERFORM (or the other way round) to get "
        + "both behaviours.",
        "ISO §4.2.2 / §14.9.28.2 Format 3");

    /// <summary>COBOLNET2119 — a PERFORM VARYING FROM or BY operand outside the brace group §14.9.28.2's
    /// varying-phrase prints: <c>{ identifier-3 | index-name-2 | literal-1 }</c> for FROM,
    /// <c>{ identifier-4 | literal-2 }</c> for BY. kb/Work PB432: both slots were typed
    /// <c>arithmeticExpression</c>, which was simultaneously WIDER than the group (<c>FROM A + B BY B * 2</c>
    /// compiled and ran at every <c>--std</c>) and, through the token rewriter that mints the arithmetic ZERO by
    /// adjacency alone, NARROWER (the legal figurative <c>FROM ZERO</c> was a hard parse error).</summary>
    public static readonly DiagnosticDescriptor PerformVaryingOperandShape = new(
        "COBOLNET2119", "perform-varying-operand-shape", EditionSeverity.Error,
        "A PERFORM VARYING FROM or BY operand is not one of the alternatives the ISO §14.9.28.2 varying-phrase "
        + "prints in that slot. FROM admits { identifier-3 | index-name-2 | literal-1 } and BY admits "
        + "{ identifier-4 | literal-2 } — an identifier in every §8.4.3.1.2 form (qualified, subscripted, "
        + "reference-modified, a function-identifier), an index-name, or a numeric literal including the "
        + "figurative ZERO, but not an arithmetic expression. Compute the expression into a data item first.",
        "ISO §4.2.2 / §14.9.28.2");

    /// <summary>COBOLNET2120 — a PERFORM VARYING operand violates one of §14.9.28.3's lettered operand rules:
    /// SR4 a)/b)/c) (an index-name in the VARYING or AFTER phrase), SR5 a)/b)/c) (an index-name in the FROM
    /// phrase) or SR6 (the BY literal shall not be zero). The message names the exact rule and quotes it.
    /// kb/Work PB432: all seven obligations were absent from ONE binder function that applied no operand screen
    /// at all, and SR5's premise was never even computed. SR6 is the one whose absence changes a program's
    /// OUTCOME rather than its legality — <c>BY 0</c> is a guaranteed non-terminating loop.</summary>
    public static readonly DiagnosticDescriptor PerformVaryingOperandRule = new(
        "COBOLNET2120", "perform-varying-operand-rule", EditionSeverity.Error,
        "A PERFORM VARYING operand violates one of the ISO §14.9.28.3 operand syntax rules. SR4 constrains the "
        + "FROM and BY operands when an INDEX-NAME is varied: the identifiers shall reference integer data "
        + "items, the FROM literal shall be a positive integer, the BY literal a nonzero integer. SR5 "
        + "constrains them when the index-name is in the FROM phrase instead: the varied identifier and the BY "
        + "identifier shall reference integer data items and the BY literal shall be an integer. SR6 forbids a "
        + "zero BY literal outright, in every case — the augment value would be zero, so no induction variable "
        + "would ever change and the UNTIL condition could never become true through the phrase.",
        "ISO §14.9.28.3 SR4 / SR5 / SR6");
    /// <summary>A TYPE entry violates §13.16.3 SR14's same-entry composition rule — the TYPE clause's twin of
    /// SR12, which COBOLNET1555 carries for SAME AS. The two rules are the WARRANT for the one description copy
    /// (<c>DataBinder.CopyEntryDescription</c>, whose receiver-wins <c>??=</c> is safe only while neither
    /// subject can own the clauses it carries), so leaving SR14 unenforced did not merely accept illegal source:
    /// the subject's own PICTURE / USAGE / REDEFINES won, and silently DISCARDED the type's declared description
    /// — <c>01 T IS TYPEDEF PIC X(3). 01 A TYPE T PIC 9(5).</c> gave A a 5-digit numeric description the type
    /// never declared (kb/Work PB513).</summary>
    public static readonly DiagnosticDescriptor TypeEntryRule = new(
        "COBOLNET2150", "type-entry-rule", EditionSeverity.Error,
        "A TYPE entry specifies a clause that may not share the entry: only BASED, CLASS, CONSTANT RECORD, "
        + "DEFAULT, DESTINATION, entry-name, EXTERNAL, GLOBAL, INVALID, level-number, OCCURS, PRESENT WHEN, "
        + "PROPERTY, TYPEDEF, VALIDATE-STATUS, VALUE, and VARYING may.", "ISO §13.16.3 SR14");
    // ── COBOLNET2146 / COBOLNET2147 / COBOLNET2148 — THE PICTURE CHARACTER-STRING'S OWN SHAPE, before any
    //    symbol is read (kb/Work PB531, PB532). All three are ONE error surface in
    //    PictureAnalyzer.TryExpandRepeats / Analyze's prologue: the string is measured, every repetition factor
    //    is parsed ONCE and validated, and the expansion is bounded — so neither the literal `9(n)` spelling nor
    //    the constant-name spelling (DataBinder.Constants.ExpandPicConstants rewrites it to `(integer)` and
    //    hands it to the same expander) can reach an unchecked `StringBuilder.Append(char, int)`. ──

    /// <summary>COBOLNET2146 — character-string-1 is longer than the 63 characters ISO §13.18.40.3 SR4
    /// allows.</summary>
    public static readonly DiagnosticDescriptor PictureStringTooLong = new(
        "COBOLNET2146", "picture-string-too-long", EditionSeverity.Error,
        "ISO §13.18.40.3 syntax rule 4: \"The maximum number of characters allowed in character-string-1 is "
        + "63.\" The count is over character-string-1 AS WRITTEN, not over the repeat-expanded symbol run — "
        + "syntax rule 6's second sentence fixes that reading (\"The integer may be specified by a "
        + "constant-name, in which case the length of the integer, not the length of the constant-name, is "
        + "counted toward the maximum number of characters in character-string-1\"), and the expanded reading "
        + "would outlaw `PIC X(30000)`, which is four characters long. So 64 written X's are rejected while "
        + "`PIC X(30000)` stays legal, and a constant-name repetition factor is counted by the digit length of "
        + "the integer substituted for it (kb/Work PB532).",
        "ISO §13.18.40.3 SR4");

    /// <summary>COBOLNET2147 — a parenthesized repetition factor that is not an unsigned nonzero integer (ISO
    /// §13.18.40.3 SR6).</summary>
    public static readonly DiagnosticDescriptor PictureRepetitionFactor = new(
        "COBOLNET2147", "picture-repetition-factor", EditionSeverity.Error,
        "ISO §13.18.40.3 syntax rule 6: \"An unsigned nonzero integer that is enclosed in parentheses indicates "
        + "the number of consecutive occurrences of the symbol that immediately precedes the left parenthesis.\" "
        + "UNSIGNED and NONZERO are both load-bearing: `PIC X(-3)` and `PIC X(+3)` carry a sign, `PIC X(0)` is "
        + "zero, `PIC X()` and `PIC X(AB)` are no integer at all, and `PIC X(` never closes — none of them is "
        + "the integer this rule admits. The negative spelling used to leave the binder as an unhandled "
        + "System.ArgumentOutOfRangeException with no COBOL diagnostic and no source location, by both the "
        + "literal route and the constant-name route (`01 N CONSTANT AS -3.` + `PIC 9(N)`), and the signed "
        + "positive spelling was accepted silently (kb/Work PB531).",
        "ISO §13.18.40.3 SR6");

    /// <summary>COBOLNET2148 — the repeat-expanded character-string describes more character positions than
    /// WiseOwl COBOL's implementor-defined maximum for one elementary item.</summary>
    public static readonly DiagnosticDescriptor PictureItemTooLarge = new(
        "COBOLNET2148", "picture-item-too-large", EditionSeverity.Error,
        "⚠ IMPLEMENTOR-DEFINED LIMIT. The standard bounds a picture character-string two ways and neither "
        + "bounds its EXPANSION: §13.18.40.3 SR4 bounds the 63 characters it is WRITTEN in, and SR14 bounds a "
        + "numeric or fixed-point numeric-edited item to 1 through 31 DIGIT positions — an alphanumeric, "
        + "alphabetic, national or boolean character-string has no such cap, and Annex A.1 carries no "
        + "implementor-defined item for the maximum size of a data item. WiseOwl COBOL therefore fixes the maximum "
        + "number of character positions in one elementary item at 134 217 728 (2^27), the largest power of two "
        + "whose UTF-16 image (2 bytes per character position, alphanumeric and national alike) stays inside "
        + ".NET's single-object ceiling. Past it the compiler used to die with an OutOfMemoryException out of "
        + "`StringBuilder.Append(char, int)` rather than name the source line (kb/Work PB531).",
        "ISO §13.18.40.3 SR4 / SR14; Annex A.1 (no maximum-item-size item)");

    /// <summary>COBOLNET2149 — a PICTURE EDITING phrase whose character-1 is written as a quoted literal
    /// (ISO §13.18.40.2 Format 1 writes it bare).</summary>
    public static readonly DiagnosticDescriptor PictureEditingChar1NotALiteral = new(
        "COBOLNET2149", "picture-editing-char1-not-a-literal", EditionSeverity.Error,
        "The PICTURE clause's Format 1 general format is `EDITING character-1 { IS literal-1 | FOR { NEGATIVE IS "
        + "literal-2 | POSITIVE IS literal-3 } }`: character-1 is written BARE, with no quotation marks, exactly "
        + "as character-string-1 is, while literal-1, literal-2 and literal-3 are named as literals. "
        + "§13.18.40.3 syntax rule 8 types it — \"Character-1 shall be any basic letter in the COBOL character "
        + "set except those specified in a CURRENCY-SIGN clause or a basic letter character A, B, C, D, E, N, P, "
        + "R, S, V, X, Z or their lowercase equivalents\" — and §13.18.40.4 general rule 14 ('es') plus "
        + "§13.18.40.5 editing rule 3 make it a PICTURE SYMBOL occurring in character-string-1, not a literal "
        + "operand; syntax rule 9, which types the phrase's literals, enumerates only literal-1, literal-2 and "
        + "literal-3. Write `EDITING T IS \":\"`, not `EDITING \"T\" IS \":\"`. The quoted spelling is recognized "
        + "only so that it can be named: the grammar used to REQUIRE it, which made every conforming EDITING "
        + "phrase a parse error (kb/Work PB568).",
        "ISO §13.18.40.2 Format 1 / §13.18.40.3 SR8");
    // ── COBOLNET2141–2145 — the REPORT SECTION value clauses' EXPRESSION operand, its ROUNDED phrase, and the
    //    SUM counter's name (kb/Work PB852 × PB883 × PB840). §13.18.53.2 and §13.18.54.2 both print
    //    `arithmetic-expression-1` as an operand form and both close with `[ rounded-phrase ]`; neither had a
    //    grammar surface, so §13.18.53.3 SR3/SR5/SR7, §13.18.54.3 SR3/SR6 and the §13.18.53.4 GR2 implicit
    //    COMPUTE had no reachable population at all. ──

    /// <summary>COBOLNET2141 — an expression-valued SOURCE operand (or an operand carrying the ROUNDED phrase)
    /// on an entry that defines neither a numeric nor a numeric-edited item (ISO §13.18.53.3 SR3). SR5 folds the
    /// ROUNDED case in: "If identifier-1 is specified with the ROUNDED phrase, it is considered to be an
    /// arithmetic-expression", so the rule screens both spellings through one site.</summary>
    public static readonly DiagnosticDescriptor ReportSourceExpressionNotNumeric = new(
        "COBOLNET2141", "report-source-expression-not-numeric", EditionSeverity.Error,
        "§13.18.53.3 SR3: \"If arithmetic-expression-1 or the ROUNDED phrase is specified, the entry shall "
        + "define either a numeric data item or a numeric-edited data item.\" The operand is the subject of the "
        + "implicit COMPUTE statement of §13.18.53.4 GR2, whose receiving operand is the printable item, and a "
        + "COMPUTE has no receiving category outside numeric and numeric-edited. SR5 brings a ROUNDED identifier "
        + "under the same rule: \"If identifier-1 is specified with the ROUNDED phrase, it is considered to be "
        + "an arithmetic-expression.\"",
        "ISO §13.18.53.3 / §13.18.53.4");

    /// <summary>COBOLNET2142 — a multi-operand SOURCE clause with at least one arithmetic-expression operand
    /// whose operands are not each parenthesized (ISO §13.18.53.3 SR7). The rule is what makes the operand LIST
    /// readable at all once an operand may itself contain operators, so it is enforced, never assumed from the
    /// grammar's shape.</summary>
    public static readonly DiagnosticDescriptor ReportSourceOperandParens = new(
        "COBOLNET2142", "report-source-operand-parens", EditionSeverity.Error,
        "§13.18.53.3 SR7: \"If the SOURCE clause has more than one operand of which at least one is an "
        + "arithmetic-expression, each operand shall be enclosed in parentheses.\" Operands of a SOURCE clause "
        + "are separated by nothing but a space (§13.18.53.2's ellipsis repeats the brace pair), so without the "
        + "parentheses `SOURCES ARE A + B C` has two readings and the standard removes the choice by requiring "
        + "`(A + B) (C)`. EVERY operand takes them, including the ones that are bare identifiers.",
        "ISO §13.18.53.2 / §13.18.53.3");

    /// <summary>COBOLNET2143 — a ROUNDED phrase in a SUM clause on an entry with no COLUMN clause (ISO
    /// §13.18.54.3 SR3). §13.18.54.4 GR4 is the phrase's only general rule and it opens "If the entry also
    /// contains a COLUMN clause, the sum counter acts as a source data item" — with no printable item there is
    /// no transfer for the rounding to govern.</summary>
    public static readonly DiagnosticDescriptor ReportSumRoundedWithoutColumn = new(
        "COBOLNET2143", "report-sum-rounded-without-column", EditionSeverity.Error,
        "§13.18.54.3 SR3: \"The ROUNDED phrase may be specified in the SUM clause only if the COLUMN clause is "
        + "specified for the subject of the entry.\" The phrase governs §13.18.54.4 GR4's delivery of the sum "
        + "counter to the printable item, and an entry with no COLUMN clause defines no printable item "
        + "(§13.18.14), so the phrase would have nothing to round.",
        "ISO §13.18.54.3 / §13.18.54.4");

    /// <summary>COBOLNET2144 — an identifier inside a report value clause's arithmetic-expression operand that
    /// references a section the clause's syntax rule excludes: §13.18.53.3 SR4 (SOURCE — a report-section
    /// identifier shall be a report counter or a sum counter of the CURRENT report) or §13.18.54.3 SR6 (SUM — an
    /// identifier in the expression shall reference an entry in a section OTHER than the report section).</summary>
    public static readonly DiagnosticDescriptor ReportExpressionOperandSection = new(
        "COBOLNET2144", "report-expression-operand-section", EditionSeverity.Error,
        "An identifier inside a report value clause's arithmetic-expression operand references a data item its "
        + "clause does not admit. §13.18.53.3 SR4 says of SOURCE: \"Identifier-1 specifies a data item defined in "
        + "any section of the data division. If identifier-1 specifies a report section item, it shall be a "
        + "report counter identifier or a sum counter defined in the current report. This same Syntax rule "
        + "applies to any identifier appearing in arithmetic-expression-1.\" §13.18.54.3 SR6 says of SUM: \"If "
        + "the addend is arithmetic-expression-1, any identifiers it contains may reference entries in any "
        + "section of the data division other than the report section.\" The two clauses draw the line in "
        + "different places and each is enforced against its own rule.",
        "ISO §13.18.53.3 / §13.18.54.3");

    /// <summary>COBOLNET2145 — a procedure division reference to a SUM COUNTER that identifies no single counter:
    /// the qualifiers written reach no counter of the name, or the name with its qualifiers is established by more
    /// than one entry. ISO §13.18.54.4 GR5 makes the data-name the counter's name and GR12 permits statements to
    /// read and alter it, but GR1 gives EVERY entry its own counter, so a name two entries share identifies none of
    /// them (§8.4.2.2.1) until qualification (§8.4.2.2.2 Format 1; §8.4.2.2.3 SR4 — the report group entries the
    /// SUM entry is subordinate to, then its report-name) picks one.</summary>
    public static readonly DiagnosticDescriptor ReportSumCounterReference = new(
        "COBOLNET2145", "report-sum-counter-reference", EditionSeverity.Error,
        "A reference to a sum counter does not identify exactly one counter. §13.18.54.4 GR5: \"If a data-name "
        + "immediately follows the level number in the entry containing the SUM clause, the data-name is the "
        + "name of the sum counter, not the name of the associated printable item, if any\", and GR12: \"It is "
        + "permissible for procedure division statements to alter the content of sum counters.\" GR1 establishes "
        + "an independent counter for EACH such entry, so two entries may carry one data-name legally; it is the "
        + "REFERENCE that §8.4.2.2.1 requires to identify one resource uniquely, and a sum counter is qualified "
        + "by the data-names of the report group description entries it is subordinate to (§8.4.2.2.3 SR4) and "
        + "by its report-name (§8.4.2.2.2 Format 1).",
        "ISO §13.18.54.4 / §8.4.2.2.1 / §8.4.2.2.2");
    /// <summary>An inline method invocation's receiver (ISO §8.4.3.4.2's <c>{object-class-name-1 |
    /// identifier-1}</c>) or its method-name literal-1 is one the construct's own syntax rules exclude.
    /// §8.4.3.4.3 SR2: "Identifier-1 shall be of class object; neither the predefined object reference NULL
    /// nor a universal object reference shall be specified" — both are syntactically writable here, because
    /// the rule reuses INVOKE's own <c>objectReference</c> receiver so the two forms cannot disagree about
    /// what a receiver is (the P3 superset parse), and both are therefore rejected by NAME rather than by a
    /// general-format rejection dressed up as a syntax error. The universal arm is not a gap: §14.9.23.4
    /// GR7c's dynamic path exists for it, and the INVOKE statement reaches it.</summary>
    public static readonly DiagnosticDescriptor InlineInvocationReceiver = new(
        "COBOLNET2138", "inline-invocation-receiver", EditionSeverity.Error,
        "An inline method invocation's receiver or method-name literal is excluded by §8.4.3.4.3.",
        "ISO §8.4.3.4.3 SR2 / §8.4.3.4.2");

    /// <summary>An inline method invocation names a method whose procedure division header declares no
    /// RETURNING item. §8.4.3.4.1 makes the construct a REFERENCE to "a temporary data item returned from
    /// invocation of a method", and §8.4.3.4.4 GR1 b) describes that temporary entirely in terms of "the
    /// RETURNING parameter in the specification of the method identified by literal-1" — with no such
    /// parameter there is no item for the identifier to reference. §14.8.3.1 states the same obligation from
    /// the other side — "A returning item is implicitly specified in the activating element when a function
    /// or inline method invocation is referenced" — and §D.6.5.6.5 spells it out: "A returning item is
    /// required for function calls and inline method invocation; it is optional for program calls and method
    /// invocation using the INVOKE statement."
    /// The INVOKE statement is the form for a method that returns nothing.</summary>
    public static readonly DiagnosticDescriptor InlineInvocationNoReturning = new(
        "COBOLNET2139", "inline-invocation-no-returning", EditionSeverity.Error,
        "The method of an inline method invocation declares no RETURNING item.",
        "ISO §8.4.3.4.1 / §8.4.3.4.4 GR1 b) / §14.8.3.1");

    /// <summary>§8.4.3.4.3 SR4: "The data item referenced in the RETURNING phrase of the invoked method's
    /// procedure division header shall not be described with the ANY LENGTH clause or with the ACTIVE-CLASS
    /// phrase." Both would leave GR1 b)'s "same description, class, and category" temporary undescribable at
    /// the point of reference — an ANY LENGTH item has no length until activation, and an ACTIVE-CLASS
    /// reference has no class until the runtime class of the receiver is known.
    /// <para>Both arms have a negative golden (<c>pb428-inline-invocation-returning-active-class</c>, kb/Work PB428;
    /// <c>pb1167-inline-invocation-returning-any-length</c>, kb/Work PB1167).</para></summary>
    public static readonly DiagnosticDescriptor InlineInvocationReturningShape = new(
        "COBOLNET2140", "inline-invocation-returning-shape", EditionSeverity.Error,
        "The RETURNING item of an inline-invoked method is ANY LENGTH or ACTIVE-CLASS.",
        "ISO §8.4.3.4.3 SR4");
    // ── COBOLNET2155–COBOLNET2158 — THE MISSING GRAMMAR SURFACES OF WAVE 39 ───────────────────────────────
    // Four general formats the printed standard carries that the grammar had never written down, so each was
    // a bare ANTLR syntax error on conforming source: the VALUE Format-2 signed subscript (PB553), the
    // EXTERNAL clause's AS phrase (PB511), the §8.4.3.13 program-address-identifier (PB549) and the USAGE
    // clause's MESSAGE-TAG exclusivity rule (PB544).

    /// <summary>A <c>signedIntegerLiteral</c> slot was written with a space between the sign and its digits —
    /// `FROM ( + 1 )`. §8.3.3.3.2 2): "A literal shall not contain more than one sign character. If a sign is
    /// used, it shall appear as the leftmost character of the literal." A literal is ONE character-string, so
    /// a separated sign is not part of it. The grammar admits the shape (the sign is its own token in these
    /// slots) and this narrows it by name rather than leaving it to the ANTLR error reporter.</summary>
    public static readonly DiagnosticDescriptor SignedLiteralSignNotAdjacent = new(
        "COBOLNET2155", "signed-literal-sign-not-adjacent", EditionSeverity.Error,
        "A numeric literal is written with its sign separated from its digits (by a space or a line break), or with "
        + "more than one sign character. ISO §8.3.3.3.2 2): \"A literal shall not contain more than one sign "
        + "character. If a sign is used, it shall appear as the leftmost character of the literal\" — and a space "
        + "is a separator (§8.3.5), so `- 5` is two things, not the literal -5. Screened in every signed literal slot "
        + "(LiteralScreenPass) and in the VALUE clause's literal positions, which admit no arithmetic expression.",
        "ISO §8.3.3.3.2 2)");

    /// <summary>The literal of the EXTERNAL clause's <c>AS</c> phrase violated §13.18.22.3 SR3: "Literal-1
    /// shall be an alphanumeric or national literal and shall be neither a figurative constant nor a
    /// zero-length literal." The SEVENTH restatement of the one externalized-name sentence, screened through
    /// the one shared <c>ExternalizedName.Screen</c> the five identification-division paragraphs and the
    /// REPOSITORY program-specifier already use (kb/Work PB303).</summary>
    public static readonly DiagnosticDescriptor ExternalClauseAsLiteral = new(
        "COBOLNET2156", "external-clause-as-literal", EditionSeverity.Error,
        "The EXTERNAL clause's AS literal-1 is not an admissible externalized name.",
        "ISO §13.18.22.3 SR3");

    /// <summary>A §8.4.3.13 program-address-identifier — `ADDRESS OF PROGRAM { identifier-1 | literal-1 |
    /// program-prototype-name-1 }` — has an operand its own syntax rules exclude: SR1 ("Identifier-1 shall be
    /// of category alphanumeric or national"), SR2 ("Literal-1 shall be an alphanumeric or national literal
    /// whose length is not zero") or SR3 ("Program-prototype-name-1 shall be a program prototype specified in
    /// the REPOSITORY paragraph"). The receiving operand's own category is §14.9.39.3 SR21's and reports
    /// through <see cref="PointerOperandShape"/>, as the vendor ENTRY spelling's does.</summary>
    public static readonly DiagnosticDescriptor ProgramAddressOperand = new(
        "COBOLNET2157", "program-address-operand", EditionSeverity.Error,
        "The operand of ADDRESS OF PROGRAM is not an admissible program-address-identifier operand.",
        "ISO §8.4.3.13.3 SR1/SR2/SR3");

    /// <summary>A data description entry specifies USAGE MESSAGE-TAG together with another USAGE clause.
    /// §13.18.60.3 SR21: "If MESSAGE-TAG is specified, no other usage clauses shall be specified in the data
    /// description entry." ⚠ THIS IS A SYNTAX RULE, NOT THE NON-SUPPORT DECLINE: it is raised at every edition
    /// that has MESSAGE-TAG, ahead of and independently of <see cref="MessageTagUsageUnsupported"/>, so the
    /// rule has a subject even though the usage itself is declined (Annex A.3 item 4).</summary>
    public static readonly DiagnosticDescriptor MessageTagUsageExclusive = new(
        "COBOLNET2158", "message-tag-usage-exclusive", EditionSeverity.Error,
        "USAGE MESSAGE-TAG is specified with another USAGE clause in the same data description entry.",
        "ISO §13.18.60.3 SR21");

    /// <summary>Two entries of one source element externalize the same name. §13.18.22.3 SR2: "In the same
    /// source element, the externalized name of the subject of the entry that includes the EXTERNAL clause
    /// shall not be the same as the externalized name of any other entry that includes the EXTERNAL clause."
    /// ⛔ THE RULE ONLY BECAME VIOLABLE WHEN THE AS PHRASE GAINED A GRAMMAR (kb/Work PB511): before it, every
    /// externalized name was §13.18.22.4 GR5's default — the subject's own data-name or file-name — which
    /// §8.4.2.2 already keeps unique. With literal-1 writable, two subjects can name ONE run-unit
    /// <c>ExternalStore</c> cell, and the consequence is a silent alias of differently-shaped storage rather
    /// than a rejection, which is why the rule is screened at the one cell-keying site.</summary>
    public static readonly DiagnosticDescriptor ExternalizedNameNotUnique = new(
        "COBOLNET2159", "externalized-name-not-unique", EditionSeverity.Error,
        "Two entries in one source element externalize the same name.",
        "ISO §13.18.22.3 SR2");

    // ── COBOLNET2112 / COBOLNET2113 — THE SET STATEMENT'S FORMAT SELECTION (kb/Work PB449 + PB456 + PB458) ──

    /// <summary>COBOLNET2112 — §14.9.39.2: the receiving operands of a SET statement match no printed general
    /// format's receiving brace, or match one whose own syntax rule excludes some of them. kb/Work PB449.</summary>
    public static readonly DiagnosticDescriptor SetNoFormatAdmitsReceiver = new(
        "COBOLNET2112", "set-no-format-admits-receiver", EditionSeverity.Error,
        "No general format of the SET statement admits this list of receiving operands. Every receiving brace in "
        + "ISO §14.9.39.2 is written `{ … } …` — one or more operands of ONE kind — so the format is chosen from "
        + "the WHOLE list: index-names (Format 1, and Format 2 for UP/DOWN BY), an item of class index or an "
        + "integer data item (Format 1), an item of class object (Format 5), a data-pointer (Format 7, and "
        + "Format 10 for UP/DOWN BY), a function-pointer (Format 8), a program-pointer (Format 9), a "
        + "dynamic-capacity register (Format 14) or a dynamic-length elementary item (Format 16). Mixing kinds, "
        + "or writing a kind the direction admits nowhere (an integer data item under UP/DOWN BY — Format 2's "
        + "receiving operand is index-name-3), leaves the statement with no format. Write one statement per "
        + "receiving kind. ⚠ The diagnostic does not depend on which operand is written first: that asymmetry "
        + "was the defect it replaces.",
        "ISO §14.9.39.2 / §14.9.39.3 SR1, SR8, SR17, SR20, SR21, SR23, SR29, SR33");

    /// <summary>COBOLNET2113 — §14.9.39.3 SR30 / SR34: a SET amount written as a LITERAL (integer-1 of Format 14,
    /// integer-2 of Format 16) is outside the bound its own syntax rule states. kb/Work PB458.</summary>
    public static readonly DiagnosticDescriptor SetLiteralAmountOutOfRange = new(
        "COBOLNET2113", "set-literal-amount-out-of-range", EditionSeverity.Error,
        "A SET amount written as a literal is outside the range its general format's syntax rule allows. ISO "
        + "§14.9.39.3 SR30: \"Integer-1 shall be nonnegative and, if TO is specified, integer-1 shall be not less "
        + "than the minimum capacity defined in the corresponding OCCURS clause and not greater than the expected "
        + "capacity, if specified.\" SR34: \"Integer-2 shall be non-negative, and shall be equal to or less than "
        + "the maximum size of data-name-3, as specified in 8.5.1.10.\" These are SYNTAX rules over the literal "
        + "alternative, so the program is refused; the corresponding GENERAL rules (§14.9.39.4 GR29/GR30 and "
        + "GR37/GR38, with their EC-BOUND-SUBSCRIPT / EC-STORAGE-NOT-AVAIL conditions and their clamps) govern "
        + "the arithmetic-expression alternative at run time instead. ⚠ A sign written ADJACENT to the digits is "
        + "part of the literal (§8.3.3.3.2 rule 2), so `TO -1` is a negative integer-1/-2; `TO - 1` — separated — "
        + "is a unary operator over an expression and takes the general rule.",
        "ISO §14.9.39.3 SR30 / SR34 / §8.5.1.10.1 / §13.18.38.4 GR16, GR17");

    /// <summary>COBOLNET2167 — a VALUE clause that carries a FORMAT-3 or FORMAT-5 phrase on an entry whose
    /// level-number is not 88 (ISO §13.18.63.3 SR33). The grammar admits every VALUE format through one rule on
    /// purpose — formats 3 and 5 share their literal / THROUGH list — so the format-vs-level rule is the
    /// binder's, and nothing screened it: `01 X PIC 9 VALUE 1 THRU 5.` reached the emitter as the glued text
    /// `1THRU5` and failed the Roslyn compilation (CS1002), while `05 X PIC XXX VALUE "A" THRU "C".` compiled
    /// clean and stored the three characters `"A`. A syntax-rule violation is a compile-time reject, never a
    /// backend failure and never a silently stored value (kb/Work PB556).</summary>
    public static readonly DiagnosticDescriptor ValueFormatRequiresLevel88 = new(
        "COBOLNET2167", "value-format-requires-level-88", EditionSeverity.Error,
        "§13.18.63.3 SR33: \"Formats 3 and 5 may be specified only when the level-number of the subject of the "
        + "entry is 88.\" The THROUGH phrase and the IN alphabet-name phrase belong to formats 3 and 5, the "
        + "WHEN SET TO FALSE phrase to format 3 and the VALID / INVALID phrase to format 5; none of them "
        + "describes a data item's initial value, so on a level-01/05/77 entry there is no rule under which "
        + "they could take effect.",
        "ISO §13.18.63.3 / §13.18.63.2");

    /// <summary>COBOLNET2168 — a VALUE clause on a data item of class index, message-tag, object or pointer
    /// (ISO §13.16.3 SR10; §13.18.63.3 SR9 restates four of those usages by name), whether the entry wrote the
    /// USAGE clause or acquired it from its group (§13.18.60.4 GR1). SR9's four usages were screened two at a
    /// time until kb/Work PB557, and USAGE INDEX — which SR9 does not name and SR10 does — until kb/Work PB515:
    /// `77 I USAGE INDEX VALUE 7.` compiled clean and seeded the index item.</summary>
    public static readonly DiagnosticDescriptor ValueOnNonLiteralUsage = new(
        "COBOLNET2168", "value-on-non-literal-usage", EditionSeverity.Error,
        "§13.16.3 SR10: \"The VALUE clause shall not be specified for data items of class index, message-tag, "
        + "object, or pointer.\" §8.5.2.1 Table 2 files USAGE INDEX under class index, USAGE OBJECT REFERENCE under "
        + "class object, and USAGE POINTER, FUNCTION-POINTER and PROGRAM-POINTER under class pointer; §13.18.63.3 "
        + "SR9 restates the rule for FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE and PROGRAM-POINTER. NULL is no "
        + "escape: §8.4.3.10.1 makes it \"a predefined address of class pointer or a predefined content of class "
        + "message-tag\", not a literal. §13.18.63.4 GR4 settles such an item's initial value with no VALUE clause "
        + "at all: \"data items of class message-tag, class object, and class pointer are initialized to null\".",
        "ISO §13.16.3 SR10 / §13.18.63.3 SR9 / §13.18.63.4 GR4");

    // ── COBOLNET2172 / COBOLNET2173 — A WRITTEN SHAPE NO GENERAL FORMAT PRINTS (kb/Work PB412, PB421) ──────
    // Both are PARSE-layer diagnostics, and the stage is the point. Each names a shape the grammar used to
    // admit as the UNION of a clause's general formats, and each was answered downstream instead: the GO TO
    // complement fell into a bind arm that silently discarded operands or emitted a loud stage, and the MOVE
    // one emitted a program that died at run time saying a COBOL feature "is not yet implemented" — about
    // source §14.9.25.2 has no format for. The grammar rules now carry one alternative per printed format, so
    // the violation arrives as a syntax error and CobolErrorStrategy re-codes it with the format's own words.
    // ⛔ WHY ERRORS RATHER THAN THE §4.2.2 WARNING: the COBOLNET1970 / COBOLNET2117-2120 reading — §4.2.2's
    // first paragraph fixes what may be ACCEPTED ("An implementation shall accept the syntax and provide the
    // functionality for all standard language elements required by this Working Draft International Standard
    // and the optional or processor-dependent language elements for which support is claimed"), and a
    // construct no general format prints is neither. Refused at every edition: neither clause's formats
    // changed shape across 1985/2002/2014/2023.

    /// <summary>A GO TO statement was written in a shape §14.9.17.2 prints no format for — more than one
    /// procedure-name with no DEPENDING phrase, or a DEPENDING phrase with no procedure-name. Format 1 prints
    /// one UNBRACKETED procedure-name and no DEPENDING (§5.2.6.2 gives the omission licence to bracketed
    /// portions only); Format 2 prints <c>{ procedure-name-1 } … DEPENDING ON identifier-1</c>, whose brace
    /// group requires one alternative to be explicitly specified (§5.2.6.3) and whose DEPENDING is underlined
    /// and therefore required (§5.2.2).</summary>
    public static readonly DiagnosticDescriptor GoToFormatShape = new(
        "COBOLNET2172", "go-to-format-shape", EditionSeverity.Error,
        "A GO TO statement was written in a shape neither general format admits.",
        "ISO §14.9.17.2 / §5.2.2 / §5.2.6.2 / §5.2.6.3");

    /// <summary>A CORRESPONDING (or CORR) phrase was written after a MOVE statement's sending operand. Both
    /// general formats of §14.9.25.2 place the whole sending specification directly after the verb — Format 1
    /// is <c>MOVE { identifier-1 | literal-1 } TO { identifier-2 } …</c> and Format 2 is
    /// <c>MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4</c> — so no format admits a sending
    /// operand FOLLOWED by CORRESPONDING.</summary>
    public static readonly DiagnosticDescriptor MoveCorrespondingPosition = new(
        "COBOLNET2173", "move-corresponding-position", EditionSeverity.Error,
        "A CORRESPONDING phrase follows a MOVE statement's sending operand.",
        "ISO §14.9.25.2");

    // ⛔ COBOLNET2176 / COBOLNET2177 — THE VALUE CLAUSE'S FORMAT-3 SCREEN AND ITS PRINTED CONNECTIVE
    // (kb/Work PB890, PB559). Both are the same failure shape: an operand or a word the general format
    // constrains, accepted because nothing asked the format about it. Neither is a wrong answer on its own
    // — one leaves a phrase that governs nothing, the other a spelling no figure prints — which is exactly
    // why they were invisible. ⛔ §13.18.63.3 SR26 (an ASCENDING THROUGH pair, kb/Work PB552) is the third
    // member of the family and is DELIBERATELY NOT HERE: SR26 b) is conditioned on the runtime collating
    // sequence being KNOWN, which SR26's own NOTE 5 makes implementor-defined ("this is not a requirement
    // of an implementor"), and this processor currently answers that question TWO ways at once — the SR27
    // screen treats every non-LOCALE sequence as known at compile time, while §14.7.8's EC-RANGE-INVALID
    // arm treats the native sequence as a runtime fact (tests/conformance/2023/ec_range_invalid). Settling
    // that is an owner determination, not a screen.

    /// <summary>COBOLNET2176 — §13.18.63.3 SR31's "only when": the VALUE clause writes
    /// <c>IN alphabet-name-1</c> but no THROUGH phrase, so there are no THROUGH literals for the permission to
    /// apply to. kb/Work PB890.</summary>
    public static readonly DiagnosticDescriptor ValueAlphabetWithoutThrough = new(
        "COBOLNET2176", "value-alphabet-without-through", EditionSeverity.Error,
        "ISO §13.18.63.3 syntax rule 31: \"Alphabet-name-1 may be specified only when the literals specified in "
        + "the THROUGH phrase are of class alphanumeric or national.\" With no THROUGH phrase in the clause there "
        + "are no \"literals specified in the THROUGH phrase\", so the permission the words 'only when' grant is "
        + "never satisfied and alphabet-name-1 may not be written. This is a SYNTAX rule: it constrains what may "
        + "be written, whether or not the phrase would have an effect — a value LIST with no range is compared "
        + "by §8.8.4.5.3 GR2's ordinary relation rules under the PROGRAM collating sequence, which the IN phrase "
        + "does not reach. Either add the THROUGH phrase the alphabet is meant to order, or drop the IN phrase.",
        "ISO §13.18.63.3 SR31 / §14.7.8 / §8.8.4.5.3");

    /// <summary>COBOLNET2177 — §13.18.63.2: the VALUE clause's leading words are not a pairing the general
    /// format prints — <c>VALUE ARE</c> / <c>VALUES IS</c> in any format, or <c>VALUES</c> at all in Format 1.
    /// kb/Work PB559.</summary>
    public static readonly DiagnosticDescriptor ValueConnectiveNotPrinted = new(
        "COBOLNET2177", "value-connective-not-printed", EditionSeverity.Error,
        "ISO §13.18.63.2: Format 1 (data-item) prints \"VALUE IS literal-1\" — VALUE underlined, IS not, and "
        + "neither VALUES nor ARE appears in it; Formats 2 (table), 3 (condition-name) and 4 (report-section) "
        + "print a two-line REQUIRED CHOICE between 'VALUE IS' and 'VALUES ARE', so VALUE pairs with IS and "
        + "VALUES pairs with ARE. §5.2.6.3 makes a brace choice exactly one of its alternatives, so the "
        + "cross-product spellings 'VALUE ARE' and 'VALUES IS' are printed by no format. §13.18.63.3 SR17 "
        + "('The words VALUE and VALUES are equivalent') is a FORMAT 2 rule, reached by Format 3 through SR24 and "
        + "by Format 4 through SR34; Format 1 applies neither, which is why VALUES is not a Format-1 spelling. "
        + "§13.18.63.3 SR39 states the same pairing for Format 5, the one format that detaches the connective.",
        "ISO §13.18.63.2 / §13.18.63.3 SR17 / SR39 / §5.2.6.3");
    /// <summary>A <c>PERFORM procedure-name-1 THRU procedure-name-2</c> range has one end in the DECLARATIVES
    /// portion of the procedure division and the other outside it, or its two ends in two DIFFERENT declarative
    /// sections — §14.9.28.3 SR11: "When procedure-name-1 and procedure-name-2 are both specified and either is
    /// the name of a procedure in the declaratives portion of the procedure division, both shall be
    /// procedure-names in the same declarative section."
    /// <para>An ERROR, not the §4.2.2 warning, because the range HAS no conforming meaning to compile: the
    /// specified set of statements (§14.9.28.4 GR4) runs from the first statement of procedure-name-1 to the
    /// last of procedure-name-2, and across that boundary the intervening procedures belong to a different USE
    /// procedure or to none. Accepted, it produced a program that recursed until the CLR killed it, with no
    /// diagnostic at any <c>--std</c> (kb/Work PB433). Every edition: the rule is unchanged from COBOL-85.</para></summary>
    public static readonly DiagnosticDescriptor PerformRangeDeclaratives = new(
        "COBOLNET2186", "perform-range-declaratives", EditionSeverity.Error,
        "A PERFORM THRU range crosses the declaratives boundary or two declarative sections.",
        "ISO §14.9.28.3");

    /// <summary>A <c>&gt;&gt;TURN</c>, <c>&gt;&gt;PUSH</c> or <c>&gt;&gt;POP</c> directive is written lexically
    /// within an exception-checking (Format-3) PERFORM statement — §7.3.25.3 SR5, §7.3.22.3 SR4 and §7.3.20.3
    /// SR4, three syntax rules of one shape.
    /// <para>A SUPPRESSIBLE WARNING and not an error, by owner decision D20 (2026-07-19), which also fixed the
    /// ban as FLAT — the whole statement, imperative-statement-1 included: §4.2.2 requires for a violation of the
    /// syntax rules only "a warning mechanism that optionally may be invoked by the user at compile time", and
    /// §14.9.28.4 GR14's semantics are implemented for the accepted case, so the program compiles and runs. The
    /// warning is worth its weight because GR14 brackets the statement in an implicit PUSH ALL + TURN OFF ALL …
    /// POP ALL: a user's <c>&gt;&gt;TURN … ON</c> inside that bracket is unwound at END-PERFORM while
    /// §7.3.25.4 GR6 leads a reader to expect it to persist, and the surprise is otherwise delivered at run time,
    /// in exception-checking state (kb/Work PB595).</para></summary>
    public static readonly DiagnosticDescriptor DirectiveInExceptionCheckingPerform = new(
        "COBOLNET2187", "directive-in-exception-checking-perform", EditionSeverity.Warning,
        "A TURN, PUSH or POP directive is written inside an exception-checking PERFORM statement.",
        "ISO §7.3.25.3 / §7.3.22.3 / §7.3.20.3");

    /// <summary>COBOLNET2297 — an UNSUCCESSFUL <c>&gt;&gt;POP directive-name</c>: the named directive's state was
    /// never saved by a PUSH in this compilation group, or every saved state was already restored. ISO §7.3.20.4
    /// GR2 makes the warning an implementor OBLIGATION — "the POP directive is unsuccessful and the implementor
    /// shall provide a warning mechanism that the POP directive was unsuccessful" — and Annex A.1 item 140 marks
    /// it REQUIRED. A warning, not an error: the standard defines the unsuccessful POP as a processed directive
    /// that restores nothing, and the program means exactly that (kb/Work PB941). GR2 speaks of directive-name
    /// only, so a <c>&gt;&gt;POP ALL</c> with nothing stored draws no warning.</summary>
    public static readonly DiagnosticDescriptor PopDirectiveUnsuccessful = new(
        "COBOLNET2297", "pop-directive-unsuccessful", EditionSeverity.Warning,
        "A POP directive names a directive whose state was not saved by a PUSH directive, or was already restored "
        + "by an earlier POP; nothing is restored.",
        "ISO §7.3.20.4 GR2");

    /// <summary>COBOLNET2344 — a directive written where the standard admits it only BETWEEN clauses and statements
    /// of a compilation unit: the ALL form of <c>&gt;&gt;PUSH</c> / <c>&gt;&gt;POP</c> (§7.3.22.3 SR3 / §7.3.20.3 SR3:
    /// "If ALL is specified, the POP directive shall be specified only in a compilation unit, between clauses in
    /// divisions other than the procedure division, and between statements in the procedure division"),
    /// <c>&gt;&gt;FLAG-02</c> (§7.3.14.3 SR1) and <c>&gt;&gt;FLAG-14</c> (§7.3.15.3 SR1), and a PUSH or POP that names
    /// one of those (§7.3.20.3 SR2, §7.3.22.3 SR2). Two arms: OUTSIDE every compilation unit (before the first, or
    /// after an END marker; ALL form only), and INSIDE a clause or statement rather than between two. A WARNING:
    /// §4.2.2 requires "a warning mechanism … to indicate violations of the general formats and the explicit syntax
    /// rules", and the directive is processed as written (the D20 disposition of the sibling SR4 bans,
    /// COBOLNET2187). kb/Work PB1005, PB1065.</summary>
    public static readonly DiagnosticDescriptor DirectiveBetweenClausesPlacement = new(
        "COBOLNET2344", "directive-between-clauses-placement", EditionSeverity.Warning,
        "A directive that may be specified only between clauses and between statements (PUSH ALL, POP ALL, FLAG-02, "
        + "FLAG-14, or a PUSH / POP naming FLAG-02 or FLAG-14) is written outside a compilation unit, or inside a "
        + "clause or statement rather than between two.",
        "ISO §7.3.22.3 SR3 / §7.3.20.3 SR3 / §7.3.14.3 SR1 / §7.3.15.3 SR1 / §7.3.20.3 SR2 / §7.3.22.3 SR2");

    /// <summary>COBOLNET2652 — a directive is written where its placement rule forbids it (kb/Work PB1377, PB1378):
    /// LEAP-SECOND and PROPAGATE "shall not be specified within a compilation unit" (§7.3.17.3 SR1, §7.3.21.3 SR1),
    /// COBOL-WORDS "may be specified only before the first IDENTIFICATION DIVISION within a compilation group"
    /// (§7.3.10.3 SR1), and a PUSH or POP that names one of them "shall not be specified where directive-name must not
    /// be specified" (§7.3.20.3 SR2, §7.3.22.3 SR2). The rule lives as data on the directive's constructs.json row;
    /// <c>DirectiveSiteProcessor</c> (the text rule) and <c>DirectivePlacementPass</c> (the parse-tree rule) are its
    /// only reporters. An ERROR: a directive misplaced in a unit has no well-defined compilation-group state to
    /// apply (the warning rules of COBOLNET2344 and COBOLNET2187 leave the directive's effect unambiguous).</summary>
    public static readonly DiagnosticDescriptor DirectivePlacementViolation = new(
        "COBOLNET2652", "directive-placement-violation", EditionSeverity.Error,
        "A compiler directive is written where its syntax rules forbid it: LEAP-SECOND (§7.3.17.3 SR1) and PROPAGATE "
        + "(§7.3.21.3 SR1) not within a compilation unit, COBOL-WORDS (§7.3.10.3 SR1) only before the first "
        + "IDENTIFICATION DIVISION of the compilation group, and a POP or PUSH naming a directive (§7.3.20.3 SR2, "
        + "§7.3.22.3 SR2) not where that directive must not be specified.",
        "ISO §7.3.17.3 SR1 / §7.3.21.3 SR1 / §7.3.10.3 SR1 / §7.3.20.3 SR2 / §7.3.22.3 SR2");
    /// <summary>A PICTURE clause was written on a data description entry that HAS SUBORDINATE ENTRIES.
    /// §13.18.40.3 SR1 is the whole rule — "The PICTURE clause may be specified only at the elementary level" —
    /// and §8.5.1.3.1 says which entries those are: "The most basic subdivisions of a record, that is, those not
    /// further subdivided, are called elementary items". An entry with subordinates is further subdivided, so
    /// the clause is illegal on it at every edition (the rule is 85-era and unchanged in all four).
    /// <para>This is §13.16.3 SR8's CONVERSE, and the two together are the whole picture-PLACEMENT rule:
    /// COBOLNET0881 says "elementary ⇒ has a PICTURE", this says "has a PICTURE ⇒ elementary". Both are
    /// screened by one pass over the finished forest (<c>DataBinder.CheckPictureRequired</c>), because neither
    /// question can be asked at entry bind — the subordinate entries are not parsed yet.</para>
    /// <para>It had no screen at all, and the shape it admits has NO REPRESENTATION in the bound data model:
    /// <c>DataItem.IsElementary</c> is <c>Pic is not null</c> and <c>IsGroup</c> is <c>Pic is null &amp;&amp;
    /// Children.Count > 0</c>, so an entry with both is NEITHER, codegen emitted the PICTURE's scalar storage
    /// and dropped the subordinates, and the first reference to one of them handed the user a raw Roslyn
    /// <c>CS1061</c> against a generated file (kb/Work PB527).</para></summary>
    public static readonly DiagnosticDescriptor PictureAtElementaryLevel = new(
        "COBOLNET2191", "picture-at-elementary-level", EditionSeverity.Error,
        "A PICTURE clause was specified on an entry that has subordinate entries.",
        "ISO §13.18.40.3 SR1 / §8.5.1.3.1");
    /// <summary>EXCEPTION-OBJECT was written as a receiving operand. §8.4.3.6.3 SR1 states the rule for EVERY
    /// receiving operand in the language — "EXCEPTION-OBJECT shall not be specified as a receiving operand" —
    /// and §8.4.3.6.4 GR1 gives the reason: the predefined object reference denotes "the current exception
    /// object", which the run unit sets when an exception is raised.
    /// <para>⛔ IT EXISTS BECAUSE THE ARM WAS MISSING, NOT BECAUSE THE OUTCOME WAS (kb/Work PB922 — the
    /// <c>LinageCounterReceiving</c> shape one dispatch later). Only SET asked the question; the general
    /// receiving chokepoint did not, and neither did the resolver, so <c>MOVE U TO EXCEPTION-OBJECT</c> drew
    /// COBOLNET1639 ("is not defined … Check the spelling, or declare the item") beside COBOLNET0901 ("is a
    /// reserved word … cannot be used as a user-defined word") — two diagnostics that contradict each other,
    /// and neither of them the rule that was broken.</para></summary>
    public static readonly DiagnosticDescriptor ExceptionObjectReceiving = new(
        "COBOLNET2196", "exception-object-receiving", EditionSeverity.Error,
        "EXCEPTION-OBJECT is specified as a receiving operand. ISO §8.4.3.6.3 SR1: \"EXCEPTION-OBJECT shall not "
        + "be specified as a receiving operand.\" It is the predefined object reference for the current "
        + "exception object (§8.4.3.6.4 GR1), of which there is one instance in a run unit (GR2).",
        "ISO §8.4.3.6.3 SR1 / §8.4.3.6.4");

    // ── The class condition's §8.8.4.4.3 OPERAND rules (kb/Work PB571 + PB590). The screen used to return
    // early unless the operand's category was boolean, so SR1 was never asked of anything and SR3/SR5 had no
    // arm at all. SR4 and SR8 keep COBOLNET0844, which already reported them; these are the rules that had
    // none. ⛔ SR1 is ONE rule reported through TWO codes — its strongly-typed-group arm stays in the
    // COBOLNET1533 strong-typing family (StrongClassCondition), which is split by rule on purpose.
    /// <summary>A class condition's identifier-1 references a data item of class index, message-tag, object or
    /// pointer, or a variable-length group. ISO §8.8.4.4.3 SR1 closes the operand: "Identifier-1 shall not
    /// reference a data item of class index, message-tag, object, or pointer, nor a strongly-typed group, nor a
    /// variable-length group."
    /// <para>⛔ IT IS A WRONG ANSWER THAT WAS BEING GIVEN, NOT A MISSING REFUSAL (kb/Work PB571). An index data
    /// item's storage profile carries category NUMERIC (the occurrence number IS a number), so
    /// <c>IF IX IS NUMERIC</c> over a USAGE INDEX item compiled clean and printed TRUE — the exact contradiction
    /// of §13.18.60.4 GR10, "The class and category of an index data item are index". The class question about a
    /// class-INDEX item was answered as though its class were numeric.</para></summary>
    public static readonly DiagnosticDescriptor ClassConditionOperandClass = new(
        "COBOLNET2200", "class-condition-operand-class", EditionSeverity.Error,
        "A class condition's identifier-1 references a data item of class index, message-tag, object, or "
        + "pointer, or a variable-length group. ISO §8.8.4.4.3 SR1: \"Identifier-1 shall not reference a data "
        + "item of class index, message-tag, object, or pointer, nor a strongly-typed group, nor a "
        + "variable-length group.\"",
        "ISO §8.8.4.4.3 SR1 / §13.18.60.4");
    /// <summary>BOOLEAN was specified in a class condition over a numeric or numeric-edited operand. ISO
    /// §8.8.4.4.3 SR5: "BOOLEAN shall not be specified if the category of the data item referenced by
    /// identifier-1 is numeric or numeric-edited." One category short of SR4's list (which also names boolean),
    /// because a BOOLEAN class test over a category-boolean item is the ordinary case.</summary>
    public static readonly DiagnosticDescriptor ClassConditionBooleanCategory = new(
        "COBOLNET2201", "class-condition-boolean-category", EditionSeverity.Error,
        "BOOLEAN is specified in a class condition whose identifier-1 is of category numeric or numeric-edited. "
        + "ISO §8.8.4.4.3 SR5: \"BOOLEAN shall not be specified if the category of the data item referenced by "
        + "identifier-1 is numeric or numeric-edited.\"",
        "ISO §8.8.4.4.3 SR5");
    /// <summary>A class condition naming alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
    /// BOOLEAN or class-name-1 was written over an operand whose usage is neither display nor national. ISO
    /// §8.8.4.4.3 SR3 states exactly that list, and NUMERIC is absent from it because §8.8.4.4.3 SR8 gives the
    /// NUMERIC phrase the same rule with a category escape ("or whose category is numeric").</summary>
    public static readonly DiagnosticDescriptor ClassConditionOperandUsage = new(
        "COBOLNET2202", "class-condition-operand-usage", EditionSeverity.Error,
        "A class condition naming alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, BOOLEAN or "
        + "class-name-1 references an operand whose usage is neither display nor national. ISO §8.8.4.4.3 SR3: "
        + "\"If the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER, BOOLEAN, or class-name-1 "
        + "phrase is specified, identifier-1 shall reference a data-item whose usage is display or national.\"",
        "ISO §8.8.4.4.3 SR3");
    /// <summary>COBOLNET2205 — a SET statement selected ISO §14.9.39.2 Format 17 (message-tag), the STATEMENT
    /// half of the Annex A.3 item-4 asynchronous messaging facility this implementation declines
    /// (docs/CONFORMANCE.md §4 item 1). §4.2.6 ¶3 makes a compile-time warning mechanism naming the unsupported
    /// processor-dependent element MANDATORY, and this is Format 17's.
    /// <para>⛔ IT IS AN ERROR WHERE ITS SEND/RECEIVE SIBLING (COBOLNET1578) IS A WARNING, and the asymmetry is
    /// the operands': SEND and RECEIVE are accepted INERT because a statement that performs no message I-O is a
    /// coherent no-op, while Format 17's operands are message-tag DATA ITEMS whose own entries are already
    /// refused by name (COBOLNET1943) for want of any inert reading — §13.18.60.4 GR9 makes their class and
    /// category message-tag. A statement over items that do not exist cannot be accepted.</para>
    /// <para>⛔ IT EXISTS BECAUSE THE FORMAT HAD NO ROW, NOT BECAUSE THE VERDICT WAS MISSING (kb/Work PB453).
    /// <c>SetFormatSelection</c>'s own comment asserted Format 17 "cannot be reached"; it was, and the statement
    /// fell through to the NEAREST row — so <c>SET MT-A TO NULL</c> drew the §14.9.39.3 SR8 screen (COBOLNET0867,
    /// which SAYS "the receiving operand of an object-reference SET shall be a USAGE OBJECT REFERENCE data
    /// item") and <c>SET MT-A TO MT-B</c> drew the §8.8.1.1 screen (COBOLNET0844, "of category alphanumeric is
    /// not a numeric operand"). ⛔ BOTH QUOTATIONS ARE THE DIAGNOSTICS' WORDS, NOT THE STANDARD'S —
    /// SR8's own text is "Identifier-3 shall be any item of class object that is permitted as a receiving item". Both are true of a statement the
    /// program did not write, and both name a repair that is not one.</para></summary>
    public static readonly DiagnosticDescriptor McsMessageTagSetUnsupported = new(
        "COBOLNET2205", "mcs-message-tag-set-unsupported", EditionSeverity.Error,
        "a SET statement whose operand is a message-tag data item is ISO §14.9.39.2 Format 17 (message-tag), "
        + "the statement half of the asynchronous messaging facility — a processor-dependent element (§4.2.6; "
        + "Annex A.3 item 4) that is not supported. §14.9.39.3 SR35 makes both of Format 17's operands "
        + "message-tag data items, and the MESSAGE-TAG usage itself is refused by name (COBOLNET1943), so the "
        + "statement is refused rather than accepted inert — unlike SEND/RECEIVE, whose COBOLNET1578 accepts "
        + "them inert because a statement that performs no message I-O is still a coherent no-op. See "
        + "docs/CONFORMANCE.md §4 item 1.",
        "ISO §4.2.6 ¶3 / Annex A.3 item 4 / §14.9.39.2 Format 17 / §14.9.39.3 SR35",
        RecognizedNotImplemented, Annex: DeclinedAnnex.A3);

    /// <summary>Two definitions in one compilation group externalize the SAME name to the operating
    /// environment (kb/Work PB660). ISO §8.3.2.2 states the rule as two sentences over one list, so this is one
    /// diagnostic and not two: <i>"Within a run unit, all instances of a given name that is externalized to the
    /// operating environment shall identify the same kind of entity or item"</i> refuses the CROSS-KIND pair (an
    /// outermost program and a function under one name), and <i>"Except for method-names and property-names,
    /// when two or more source elements identify something with the same externalized name, they refer to the
    /// same instance"</i> refuses the SAME-KIND pair — two distinct definitions cannot be one instance.
    /// <para>⛔ THE CONTAINED HALF IS A DIFFERENT CLAUSE AND A DIFFERENT CODE (COBOLNET2214, §8.4.6.3): a
    /// contained program's name is not externalized at all (§8.3.2.2's list item 1 says "program-names of
    /// OUTERMOST programs"), so its uniqueness is scoped to its outermost program, not to the group.</para>
    /// <para>The comparison is CASE-INSENSITIVE, matching <c>ProgramTable.NameEquals</c> — the resolver this
    /// check exists to keep honest. §8.3.2.2 leaves that to the implementor ("The implementor defines the
    /// formation and mapping rules of these names"), and a check that compared more strictly than the resolver
    /// would pass source the run unit then resolves to the wrong definition.</para>
    /// <para>PROTOTYPE units are excluded by construction, not by omission: §10.6.2 SR2 and SR3 each describe
    /// a compilation group containing "both a {program|function} definition and a {program|function} prototype
    /// definition with the same externalized name", so that pair is the INTENDED shape.</para></summary>
    public static readonly DiagnosticDescriptor DuplicateExternalizedDefinition = new(
        "COBOLNET2213", "duplicate-externalized-definition", EditionSeverity.Error,
        "Two definitions in one compilation group externalize the same name to the operating environment. "
        + "ISO §8.3.2.2: \"Within a run unit, all instances of a given name that is externalized to the operating "
        + "environment shall identify the same kind of entity or item. Except for method-names and "
        + "property-names, when two or more source elements identify something with the same externalized name, "
        + "they refer to the same instance.\"",
        "ISO §8.3.2.2");
    /// <summary>Two programs contained directly or indirectly within one outermost program share a
    /// program-name (kb/Work PB660). ISO §8.4.6.3: <i>"The names assigned to programs that are contained
    /// directly or indirectly within the same outermost program shall be unique within that outermost
    /// program."</i> The scope is the OUTERMOST PROGRAM, not the compilation group — two different outermost
    /// programs may each contain a program of the same name, and §8.4.6.3 rules 1 and 2 are what keep the two
    /// apart at every reference.</summary>
    public static readonly DiagnosticDescriptor DuplicateContainedProgramName = new(
        "COBOLNET2214", "duplicate-contained-program-name", EditionSeverity.Error,
        "Two programs contained within one outermost program share a program-name. ISO §8.4.6.3: \"The names "
        + "assigned to programs that are contained directly or indirectly within the same outermost program "
        + "shall be unique within that outermost program.\"",
        "ISO §8.4.6.3");

    /// <summary>COBOLNET2215 — a FLOAT-INFINITY / FLOAT-NOT-A-NUMBER[-QUIET|-SIGNALING] class condition over an
    /// operand that is not described with a STANDARD floating-point usage (ISO §8.8.4.4.3 SR7; kb/Work PB225). The
    /// SET Format 15 twin of the same restriction is COBOLNET1940 (§14.9.39.3 SR32).</summary>
    public static readonly DiagnosticDescriptor ClassConditionNotStandardFloat = new(
        "COBOLNET2215", "class-condition-not-standard-float", EditionSeverity.Error,
        "A FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, FLOAT-NOT-A-NUMBER-QUIET or FLOAT-NOT-A-NUMBER-SIGNALING class "
        + "condition references an operand that is not described with a standard floating-point usage. ISO "
        + "§8.8.4.4.3 SR7: \"If the FLOAT-INFINITY, FLOAT-NOT-A-NUMBER, FLOAT-NOT-A-NUMBER-QUIET, or "
        + "FLOAT-NOT-A-NUMBER-SIGNALING phrase is specified, identifier-1 shall reference a data item described "
        + "with a standard floating-point usage.\"",
        "ISO §8.8.4.4.3 SR7");

    /// <summary>COBOLNET2216 — a FARTHEST-FROM-ZERO / IN-ARITHMETIC-RANGE / NEAREST-TO-ZERO class condition over
    /// an operand whose category is not numeric (ISO §8.8.4.4.3 SR6; kb/Work PB225) — a reference-modified slice
    /// included, whose category §8.4.3.3.4 GR6 c) makes alphanumeric or national.</summary>
    public static readonly DiagnosticDescriptor ClassConditionNotNumericCategory = new(
        "COBOLNET2216", "class-condition-not-numeric-category", EditionSeverity.Error,
        "A FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE or NEAREST-TO-ZERO class condition references an operand whose "
        + "category is not numeric. ISO §8.8.4.4.3 SR6: \"If FARTHEST-FROM-ZERO, IN-ARITHMETIC-RANGE, or "
        + "NEAREST-TO-ZERO is specified, identifier-1 shall reference a data item whose category is numeric.\"",
        "ISO §8.8.4.4.3 SR6");
    /// <summary>A function-identifier names a function-pointer but writes no argument-list parentheses (kb/Work
    /// PB847). ISO §8.4.3.2.3 SR5: <i>"If function-pointer-name-1 is specified, the parentheses shall be
    /// specified."</i> A prototype or intrinsic reference may omit an empty list; a function-pointer reference may
    /// not, because the bare name is the reference to the POINTER itself (what SET and a pointer comparison
    /// read).</summary>
    public static readonly DiagnosticDescriptor FunctionPointerParenthesesRequired = new(
        "COBOLNET2234", "function-pointer-parentheses-required", EditionSeverity.Error,
        "A function-identifier that names a function-pointer shall write its argument-list parentheses. ISO "
        + "§8.4.3.2.3 SR5: \"If function-pointer-name-1 is specified, the parentheses shall be specified.\"",
        "ISO §8.4.3.2.3");
    /// <summary>A PARAMETERIZED class or interface definition breaks one of the rules on its own USING clause
    /// (kb/Work PB759): a parameter-name that no class-specifier or interface-specifier of the definition's own
    /// REPOSITORY paragraph declares (ISO §11.3.3 SR8 / §11.6.3 SR4), a parameter-name written twice (§11.3.3
    /// SR9 / §11.6.3 SR7), or an EXPANDS phrase in that REPOSITORY paragraph (§12.3.8.3 SR3). The definition is
    /// a skeleton (§9.3.12 / §9.3.13) — every expansion inherits these defects — so they are diagnosed once, on
    /// the definition itself, whether or not anything expands it.</summary>
    public static readonly DiagnosticDescriptor ParameterizedDefinitionUsing = new(
        "COBOLNET2239", "parameterized-definition-using", EditionSeverity.Error,
        "A parameterized class or interface definition's USING clause names a parameter that its own REPOSITORY "
        + "paragraph does not declare with a class-specifier or interface-specifier, names a parameter twice, or "
        + "the definition's REPOSITORY paragraph specifies an EXPANDS phrase.",
        "ISO §11.3.3 SR8/SR9 / §11.6.3 SR4/SR7 / §12.3.8.3 SR3");
    /// <summary>A REPOSITORY class-specifier or interface-specifier EXPANDS phrase cannot create the class or
    /// interface it names (kb/Work PB759): the parameterized name or an actual parameter is not declared in the
    /// same REPOSITORY paragraph (ISO §12.3.8.3 SR4 / SR7); the EXPANDS operand is not a parameterized class
    /// (resp. interface) of the compilation group; the actual-parameter count differs from the definition's
    /// USING clause (§12.3.8.4 GR5 / GR8); an actual is not a class or interface of the kind its formal was
    /// declared as, so the §12.3.8.4 GR5 substitution would write a specifier naming the wrong kind; or two
    /// specifiers give one externalized name to expansions of different definitions or with different actual
    /// parameters (§9.3.12: they "shall not have the same externalized object-class-name"; §9.3.13 is the
    /// interface twin).</summary>
    public static readonly DiagnosticDescriptor ExpandsPhraseInvalid = new(
        "COBOLNET2240", "expands-phrase-invalid", EditionSeverity.Error,
        "A REPOSITORY EXPANDS phrase does not name a parameterized class or interface declared in the same "
        + "REPOSITORY paragraph, supplies a different number of actual parameters than the definition's USING "
        + "clause, supplies an actual parameter of the wrong kind or one that is not declared in the same "
        + "paragraph, or reuses an expansion's name for a different expansion.",
        "ISO §12.3.8.3 SR4/SR7 / §12.3.8.4 GR5/GR8 / §9.3.12 / §9.3.13");
    // ── COBOLNET2241–2243 — what a SPECIAL-NAMES entry may consist of (kb/Work PB716 + PB790 + PB862). ──

    /// <summary>A SPECIAL-NAMES switch-name / feature-name / device-name entry names a system-name this
    /// implementation does not make available, or writes ON/OFF STATUS on a name that is not a switch-name
    /// (kb/Work PB862). The available names are ONE table, <c>Cobol.Net.Editions/ImplementorNames.cs</c>, documented as
    /// Annex A.1 items 189/190/191 in docs/CONFORMANCE.md §7. <c>SPECIAL-NAMES. WIBBLE WOBBLE.</c> used to
    /// compile and register a mnemonic that named nothing.</summary>
    public static readonly DiagnosticDescriptor UnavailableImplementorName = new(
        "COBOLNET2241", "unavailable-implementor-name", EditionSeverity.Error,
        "A SPECIAL-NAMES entry of the switch-name / feature-name / device-name form names a system-name this "
        + "implementation does not make available. ISO §12.3.7.3 SR8: \"The implementor shall specify the names "
        + "that are available for switch-name-1, feature-name-1, and device-name-1.\" WiseOwl COBOL's names are the "
        + "device-names CONSOLE, SYSIN, SYSOUT and SYSERR, the feature-names C01 and CSP, and the switch-names "
        + "SWITCH-0 through SWITCH-36 and UPSI-0 through UPSI-7 (Annex A.1 items 189, 190, 191). A system-name "
        + "belongs to exactly one of the three types (§8.3.2.3.1), so ON STATUS / OFF STATUS — printed only in "
        + "the switch-name-1 arm of §12.3.7.2 — may follow a switch-name only.",
        "ISO §12.3.7.3 SR8");

    /// <summary>One ALPHABET literal-phrase entry writes BOTH a THROUGH range and ALSO operands (kb/Work PB790).
    /// The printed literal-phrase figure (§12.3.7.2, folio 291, RENDERED) stacks the two inside ONE pair of square
    /// brackets with no choice indicators, and §5.2.6.2 makes stacked bracket alternatives mutually exclusive.
    /// <c>"A" THRU "C" ALSO "D"</c> used to compile clean and silently drop the ALSO operands.</summary>
    public static readonly DiagnosticDescriptor AlphabetThroughWithAlso = new(
        "COBOLNET2242", "alphabet-through-with-also", EditionSeverity.Error,
        "An ALPHABET clause literal-phrase entry specifies both a THROUGH (THRU) phrase and an ALSO phrase. ISO "
        + "§12.3.7.2 prints the two inside one pair of brackets as alternatives — literal-1 [ {THROUGH|THRU} "
        + "literal-2 | {ALSO literal-3}… ] — and §5.2.6.2: \"Brackets, [ ], enclosing a portion of a general format "
        + "indicate that the syntax element contained within the brackets or one of the alternatives contained "
        + "within the brackets may be explicitly specified\". §12.3.7.4 GR7 places a THROUGH run on ascending "
        + "positions and an ALSO group on ONE shared position, so a merged entry has no meaning: write the range "
        + "and the equivalences as separate entries.",
        "ISO §12.3.7.2; §5.2.6.2");

    /// <summary>The ADVANCING operand of a WRITE is a mnemonic-name that is not a feature-name's (kb/Work PB862):
    /// a switch's or a device's mnemonic used to bind as a zero-line advance.</summary>
    public static readonly DiagnosticDescriptor WriteAdvancingMnemonicNotFeature = new(
        "COBOLNET2243", "write-advancing-mnemonic-not-feature", EditionSeverity.Error,
        "The ADVANCING phrase of a WRITE statement names a mnemonic-name that is associated with a switch-name or "
        + "a device-name. ISO §14.9.51.3 SR16: \"When mnemonic-name-1 is specified, the name is associated with a "
        + "feature-name specified by the implementor.\" §12.3.7.3 SR5 and SR7 confine a switch's mnemonic to SET and "
        + "a device's to ACCEPT and DISPLAY. WiseOwl COBOL's feature-names are C01 (top of the next page) and CSP "
        + "(suppress spacing).",
        "ISO §14.9.51.3 SR16");

    /// <summary>COBOLNET2272 — the body of a program, function or method PROTOTYPE carries something ISO §10.6.2
    /// SR4 a)–f) forbids (kb/Work PB894). ONE code for the one rule, whichever kind of prototype and whichever of
    /// the six restrictions — <c>PrototypeUnitRules.ScreenBody</c> is its only reporter.</summary>
    public static readonly DiagnosticDescriptor PrototypeBody = new(
        "COBOLNET2272", "prototype-body", EditionSeverity.Error,
        "A program prototype, function prototype or method prototype carries a part its definition may not. ISO "
        + "§10.6.2 SR4: \"The following restrictions apply to program prototypes, function prototypes, and method "
        + "prototypes: a) The identification division shall not contain an ARITHMETIC clause. b) The environment "
        + "division shall not contain an object-computer paragraph. c) The only clauses that may be specified in "
        + "the SPECIAL-NAMES paragraph are the ALPHABET clause, the CURRENCY clause, the DECIMAL-POINT clause, the "
        + "LOCALE clause, and the SYMBOLIC-CHARACTERS clause. d) The environment division shall not contain an "
        + "input-output section. e) The data division may contain only a linkage section. f) The procedure "
        + "division shall contain only a procedure division header.\" A prototype describes a signature; it has "
        + "no storage and no statements of its own.",
        "ISO §10.6.2 SR4");

    /// <summary>COBOLNET2273 — a program or function prototype follows another kind of source unit in its
    /// compilation group (ISO §10.6.2 SR1; kb/Work PB894).</summary>
    public static readonly DiagnosticDescriptor PrototypeOrder = new(
        "COBOLNET2273", "prototype-order", EditionSeverity.Error,
        "A program prototype or function prototype is written after a program, function, class or interface "
        + "definition. ISO §10.6.2 SR1: \"Within a compilation group, function-prototypes and program-prototypes "
        + "shall precede all other types of source units.\"",
        "ISO §10.6.2 SR1");

    /// <summary>COBOLNET2274 — a program, function, program prototype or function prototype is not written in its
    /// §10.6.1 source-unit format: it is contained where its format has no slot for it, contains a source unit its
    /// format has no slot for, omits the end marker its format prints unbracketed, closes with the other kind's end
    /// marker, or has contained programs without the procedure division that carries their slot (kb/Work PB894 for
    /// the prototype kinds; PB1507 generalized the one screen to every kind).</summary>
    public static readonly DiagnosticDescriptor SourceUnitFormat = new(
        "COBOLNET2274", "source-unit-format", EditionSeverity.Error,
        "A source unit does not follow its §10.6.1 format. ISO §10.6.1 prints four unit formats that differ in "
        + "exactly three ways, and every one is checked once, from one table: a program-definition alone has the "
        + "contained `[ procedure-division [ program-definition ] … ]` slot (so only a program definition "
        + "contains source units, only a program definition is contained, and the contained slot sits INSIDE the "
        + "procedure-division bracket); its end marker is `END PROGRAM`, where a function definition and a "
        + "function prototype close with `END FUNCTION`; and the end marker is bracketed only in the "
        + "program-definition format — a program prototype, a function prototype and a function definition always "
        + "end with it.",
        "ISO §10.6.1");

    /// <summary>COBOLNET2755 — a PROGRAM-ID attribute that §11.10.3 SR5 or SR6 forbids by the CONTAINER chain: an
    /// INITIAL clause under a recursive program, or a RECURSIVE clause under an initial program (kb/Work PB1507).</summary>
    public static readonly DiagnosticDescriptor ProgramAttributeContainment = new(
        "COBOLNET2755", "program-attribute-containment", EditionSeverity.Error,
        "A program's INITIAL or RECURSIVE clause conflicts with a program that contains it. ISO §11.10.3 SR5: \"The "
        + "INITIAL clause shall not be specified if any program that directly or indirectly contains this program is "
        + "a recursive program.\" SR6: \"The RECURSIVE clause shall not be specified if any program that directly or "
        + "indirectly contains this program is an initial program.\" The recursive attribute a program inherits "
        + "from its container (§11.10.4 GR4) counts as recursive for SR5.",
        "ISO §11.10.3 SR5 / SR6");

    /// <summary>COBOLNET2256 — an ASSIGN clause whose TO-phrase list is not one this implementation allows (ISO
    /// §12.4.5.2 SR5, determination DOC-A.1-71; kb/Work PB829). The general format admits the list; the grammar used
    /// to take ONE operand and answered a second with COBOL0308.</summary>
    public static readonly DiagnosticDescriptor AssignTargetListNotAllowed = new(
        "COBOLNET2256", "assign-target-list-not-allowed", EditionSeverity.Error,
        "The TO phrase of an ASSIGN clause lists operands in a combination this implementation does not allow. "
        + "ISO §12.4.5.1 writes `ASSIGN [TO] {device-name-1 | literal-1} …`, and ISO §12.4.5.2 SR5: \"The meaning "
        + "and rules for the allowable specification of device-name-1 and the value of literal-1 are defined by "
        + "the implementor.\" WiseOwl COBOL allows ONE operand, which names the physical file, or a device class (DISK "
        + "or PRINTER) followed by ONE operand naming the file (docs/CONFORMANCE.md §7 DOC-A.1-71).",
        "ISO §12.4.5.2 SR5 / Annex A.1 item 71");

    /// <summary>COBOLNET2257 — a SPECIAL-NAMES DYNAMIC LENGTH STRUCTURE clause this implementation refuses: it names
    /// a physical-structure-name, of which WiseOwl COBOL supports none (ISO §12.3.7.3 SR32, determination D-DL3), or it
    /// re-declares a dynamic-length-structure-name the same paragraph already declares (§8.4.2.1). kb/Work PB829 —
    /// the clause had no grammar and was COBOL0001 "unexpected 'DYNAMIC'".</summary>
    public static readonly DiagnosticDescriptor DynamicLengthStructureInvalid = new(
        "COBOLNET2257", "dynamic-length-structure-invalid", EditionSeverity.Error,
        "A DYNAMIC LENGTH STRUCTURE clause in the SPECIAL-NAMES paragraph (ISO §12.3.7.2) cannot be accepted. "
        + "Either it names a physical-structure-name — ISO §12.3.7.3 SR32: \"The implementor shall specify the "
        + "names supported for physical-structure-name-1\", and WiseOwl COBOL supports none (docs/CONFORMANCE.md §3 "
        + "D-DL3), so the layout must be described with the PREFIXED and/or DELIMITED phrases — or it declares a "
        + "dynamic-length-structure-name that the same paragraph already declares, so no reference could uniquely "
        + "identify one layout (ISO §8.4.2.1).",
        "ISO §12.3.7.3 SR32 / §8.4.2.1");

    /// <summary>COBOLNET2258 — a DYNAMIC LENGTH clause's dynamic-length-structure-name-1 that corresponds to no
    /// DYNAMIC LENGTH STRUCTURE declaration (ISO §13.18.19.3 SR2), or a LIMIT phrase larger than the maximum length
    /// that structure is associated with (SR4). kb/Work PB829 — the reference was refused outright as "not yet
    /// supported" (COBOLNET1562, retired and never to be reallocated).</summary>
    public static readonly DiagnosticDescriptor DynamicLengthStructureReference = new(
        "COBOLNET2258", "dynamic-length-structure-reference", EditionSeverity.Error,
        "A DYNAMIC LENGTH clause's dynamic-length-structure-name-1 violates a syntax rule of ISO §13.18.19.3. SR2: "
        + "\"If dynamic-length-structure-name-1 is specified, it shall correspond to a dynamic-length-structure-name "
        + "specified in the DYNAMIC LENGTH STRUCTURE clause in the SPECIAL-NAMES paragraph\" (this source element's "
        + "or a containing one's). SR4: \"If the LIMIT phrase and dynamic-structure-name-1 are both specified, "
        + "integer-1 shall not be greater than the maximum length associated with dynamic-length-structure-name-1\" "
        + "— the capacity of the structure's PREFIXED length field (ISO §12.3.7.4 GR18: 65535 for SHORT PREFIXED, "
        + "32767 for SIGNED SHORT PREFIXED), bounded by the implementor maximum.",
        "ISO §13.18.19.3 SR2 / SR4");

    /// <summary>The all-or-nothing operand-class rule of STRING, UNSTRING and INSPECT (kb/Work PB980) — ONE
    /// diagnostic for one rule shape, reported through <c>AllOrNothingClass</c>. STRING's half used to report as
    /// COBOLNET1626 (it shares a numbered rule with STRING's usage sentence); UNSTRING's and INSPECT's were not
    /// enforced at all.</summary>
    public static readonly DiagnosticDescriptor CharacterOperandClassMix = new(
        "COBOLNET2306", "character-operand-class-mix", EditionSeverity.Error,
        "A STRING, UNSTRING or INSPECT statement mixes operands of class national (or, for INSPECT, class boolean) "
        + "with operands of another class. STRING §14.9.43.3 SR1: \"If any one of literal-1, literal-2, "
        + "identifier-1, identifier-2, or identifier-3 is of class national, then all shall be of class national.\" "
        + "UNSTRING §14.9.48.3 SR3 says the same of identifier-1 through identifier-5 and both literals (a numeric "
        + "INTO receiver answers with its usage — SR4 pairs usage national with the national operands and usage "
        + "display with the others). INSPECT §14.9.22.3 SR4 says it of every operand except the TALLYING counter, for "
        + "class boolean and for class national. A figurative constant takes identifier-1's class and never mixes.",
        "ISO §14.9.43.3 SR1; §14.9.48.3 SR3; §14.9.22.3 SR4");

    /// <summary>A data reference followed by EMPTY parentheses (kb/Work PB969). The grammar's subscript capture admits
    /// the empty group because the keyword-omitted function-identifier's zero-argument list rides it.</summary>
    public static readonly DiagnosticDescriptor EmptyParenthesesOnDataReference = new(
        "COBOLNET2309", "empty-parentheses-on-data-reference", EditionSeverity.Error,
        "A data reference is followed by parentheses with nothing inside them — WS-X() or WS-X( ). ISO §8.4.2.3.2 "
        + "writes a subscript list as ( subscript … ), at least one subscript, and §8.4.3.3.2 a reference modifier as "
        + "( leftmost-position : [ length ] ). Empty parentheses are the zero-argument form of a function-identifier "
        + "(§8.4.3.2.2 brackets argument-1 inside them), which a data-name is not.",
        "ISO §8.4.2.3.2; §8.4.3.3.2; §8.4.3.2.2");

    /// <summary>A SPECIAL-NAMES FOR ALPHANUMERIC / FOR NATIONAL phrase written after the clause's definition
    /// (kb/Work PB977) — refused by name in ClosedFormatPass for the ALPHABET, CLASS and SYMBOLIC CHARACTERS
    /// clauses alike.</summary>
    public static readonly DiagnosticDescriptor SpecialNamesForPhraseMisplaced = new(
        "COBOLNET2315", "special-names-for-phrase-misplaced", EditionSeverity.Error,
        "A FOR ALPHANUMERIC / FOR NATIONAL phrase of the SPECIAL-NAMES paragraph is written after the clause's "
        + "definition. ISO §12.3.7.2 prints it in one position only — immediately after the name the clause declares "
        + "(ALPHABET alphabet-name-1 [FOR ALPHANUMERIC] IS …, ALPHABET alphabet-name-2 FOR NATIONAL IS …, CLASS "
        + "class-name-1 [FOR {ALPHANUMERIC | NATIONAL}] IS …) or, for SYMBOLIC CHARACTERS, before the first "
        + "symbolic-character-1. No edition and no dialect admits the trailing spelling; move the phrase.",
        "ISO §12.3.7.2");

    /// <summary>A bare operand that is none of the conditions is used as a condition (kb/Work PB982):
    /// <c>IF WS-X</c> over a PIC X item used to compile with no diagnostic and abort the run unit when reached.</summary>
    public static readonly DiagnosticDescriptor OperandIsNotACondition = new(
        "COBOLNET2318", "operand-is-not-a-condition", EditionSeverity.Error,
        "An operand that is not a condition is written where a conditional expression is required (IF, EVALUATE, "
        + "PERFORM UNTIL, SEARCH WHEN, or an operand of NOT / AND / OR / XOR). ISO §8.8.4.2.1: \"The simple "
        + "conditions are the relation, boolean, class, condition-name, switch-status, sign, and omitted-argument "
        + "conditions\"; a complex condition combines them (§8.8.4.1). A data item that is not a one-position "
        + "boolean item, a literal, an arithmetic expression, a bare class-name or alphabet-name (which needs the "
        + "identifier it tests, §8.8.4.4.2) and a switch's mnemonic-name (a switch-status condition is written with "
        + "its condition-name, §8.8.4.6.2) are none of them, and neither are the words TRUE and FALSE, which are "
        + "written only as an EVALUATE selection subject or selection object (§14.9.13.3 SR7 b). Parentheses "
        + "around a simple condition change nothing (§8.8.4.2.1), and make nothing else one. Within an abbreviated "
        + "combined relation condition the same operand is the object of the carried relation (§8.8.4.12) and is "
        + "not refused.",
        "ISO §8.8.4.2.1; §8.8.4.1");

    // kb/Work PB1390 — the termination half of §8.8.4.12.4 GR1. An abbreviated relation (subject omitted) whose
    // sequence has no stated subject to insert used to be refused with no diagnostic (the COBOLNET2319 internal net).
    public static readonly DiagnosticDescriptor AbbreviatedRelationWithoutSubject = new(
        "COBOLNET2552", "abbreviated-relation-without-subject", EditionSeverity.Error,
        "A relation condition is written with its subject omitted where no subject can be inserted. ISO §8.8.4.12.1: "
        + "\"any relation condition except the first may be abbreviated\" — and only within a consecutive sequence of "
        + "relation conditions in which no parentheses are used; §8.8.4.12.4 GR1: \"The insertion of an omitted subject "
        + "and/or relational operator terminates once a complete simple condition is encountered within a complex "
        + "condition.\" So an abbreviated relation that follows a class, sign, condition-name, switch-status, boolean "
        + "or parenthesized condition, or that opens a parenthesized group, has no subject. Write the subject.",
        "ISO §8.8.4.12.1; §8.8.4.12.4 GR1");

    // kb/Work PB1391 — the first syntax rule of §8.8.4.12.3, decided on the relation's CATEGORY where the abbreviation
    // carry is seeded (it used to hold only where the boolean-operator parse path happened to bind the relation).
    public static readonly DiagnosticDescriptor AbbreviatedRelationAfterBooleanRelation = new(
        "COBOLNET2723", "abbreviated-relation-after-boolean-relation", EditionSeverity.Error,
        "A relation condition is abbreviated after a boolean relation condition. ISO §8.8.4.12.3 SR1: "
        + "\"Relation-condition-1 shall not be a boolean relation condition.\" "
        + "§8.8.4.2.1: \"A relation condition involving operands of class boolean is a boolean relation condition\" "
        + "— so a sequence whose first relation compares boolean "
        + "items, boolean literals or boolean expressions (B1 = B2, B1 B-AND B2 = B\"0\") has no abbreviation: a "
        + "succeeding relation that omits its subject, or its subject and relational operator, has nothing it may "
        + "inherit. Write the subject and the operator in full.",
        "ISO §8.8.4.12.3 SR1");

    /// <summary>The internal-error net under the ONE <c>BoundConditionError</c> construction site (kb/Work PB982):
    /// a condition refused with no failing diagnostic recorded would compile clean and throw at run time.</summary>
    public static readonly DiagnosticDescriptor UnreportedConditionRefusal = new(
        "COBOLNET2319", "unreported-condition-refusal", EditionSeverity.Error,
        "WiseOwl COBOL internal error: the binder refused a condition form without reporting the rule it breaks. The "
        + "compile is failed rather than let the unbound condition reach the generated program, where it would "
        + "abort the run unit when evaluated. Please report the source that produced it.",
        "WiseOwl COBOL internal (no ISO rule)");

    /// <summary>GO TO … DEPENDING ON identifier-1 is not a numeric elementary integer data item (kb/Work PB210) —
    /// screened by the ONE operand-class screen (<c>OperandClassScreen</c>, the <c>OperandPositions</c> row).</summary>
    public static readonly DiagnosticDescriptor GoToDependingSelectorClass = new(
        "COBOLNET2324", "go-to-depending-selector-class", EditionSeverity.Error,
        "The DEPENDING ON operand of a GO TO statement is not a numeric elementary data item that is an integer. "
        + "ISO §14.9.17.3 SR1: \"Identifier-1 shall reference a numeric elementary data item that is an integer.\" "
        + "An alphanumeric, numeric-edited, group, index, floating-point or scaled item is refused: the "
        + "statement's transfer is defined only for an integer value (§14.9.17.4 GR2), and before this screen a "
        + "PIC 9V9 selector holding 2.7 was truncated to 2 and silently selected the second procedure-name. "
        + "Refused in both dialect lanes.",
        "ISO §14.9.17.3 SR1");

    /// <summary>SEARCH … VARYING identifier-2 is neither an index data item nor an integer data item, or is
    /// subscripted by identifier-1's first index-name (kb/Work PB211).</summary>
    public static readonly DiagnosticDescriptor SearchVaryingOperand = new(
        "COBOLNET2325", "search-varying-operand", EditionSeverity.Error,
        "The VARYING operand of a serial SEARCH violates ISO §14.9.37.3 SR5: \"Identifier-2 shall reference a "
        + "data item whose usage is index or a data item that is an integer. Identifier-2 shall not be "
        + "subscripted by the first or only index-name specified in the INDEXED phrase in the OCCURS clause "
        + "specified in the data description entry for identifier-1.\" A non-integer, floating-point, "
        + "alphanumeric or group item is refused (§14.9.37.4 GR3 b) increments it in step with the search "
        + "index), and so is an item subscripted by the search index itself, whose occurrence would move with "
        + "every step of the scan. Refused in both dialect lanes.",
        "ISO §14.9.37.3 SR5");

    /// <summary>The WRITE … ADVANCING count is neither an integer data item (identifier-2, §14.9.51.3 SR14) nor an
    /// integer literal that is positive or zero (integer-1, SR15) — kb/Work PB1023. identifier-2 is the
    /// <c>OperandPositions.WriteAdvancingIdentifier</c> row of the ONE operand-class screen.</summary>
    public static readonly DiagnosticDescriptor WriteAdvancingOperand = new(
        "COBOLNET2365", "write-advancing-operand", EditionSeverity.Error,
        "The ADVANCING phrase of a WRITE statement counts lines with an operand its syntax rules do not admit. ISO "
        + "§14.9.51.3 SR14: \"Identifier-2 shall reference an integer data item.\" SR15: \"Integer-1 shall be "
        + "positive or zero.\" An alphanumeric, numeric-edited, group, index, floating-point or scaled item is refused "
        + "(§14.9.51.4 GR25 a) advances the page \"the number of lines equal to that value\" — a count of lines, "
        + "defined only for an integer), and so is a literal that is not an unsigned integer; before this screen a PIC X item holding \"2\" advanced two lines and a PIC 9V9 "
        + "holding 1.5 advanced one. A constant-name standing for an integer literal is integer-1. Refused in both "
        + "dialect lanes.",
        "ISO §14.9.51.3 SR14 / SR15");

    /// <summary>A SET Format-1 (index-assignment) operand violates §14.9.39.3 SR1–SR4 (kb/Work PB212).</summary>
    public static readonly DiagnosticDescriptor SetIndexAssignmentOperand = new(
        "COBOLNET2326", "set-index-assignment-operand", EditionSeverity.Error,
        "A SET … TO statement of Format 1 (index-assignment, ISO §14.9.39.2) pairs operands its syntax rules do "
        + "not admit. SR1: identifier-1 shall reference a data item of class index or an integer data item. SR2: "
        + "identifier-2 shall reference a data item of class index. SR3: a class-index receiver shall not be "
        + "sent arithmetic-expression-1. SR4: a numeric receiver shall be sent index-name-2. §14.9.39.4 GR2 "
        + "defines the statement for exactly the admitted pairings. SR1 and SR2 are refused in both dialect "
        + "lanes; under --permissive, SR3 and SR4 (an arithmetic value or index data item sent to an integer "
        + "item, an arithmetic value sent to an index data item) are warnings and the value is stored.",
        "ISO §14.9.39.3 SR1-SR4");

    /// <summary>§13.16.3 SR21 — a PROPERTY clause in the same data description entry as a BASED or a TYPEDEF clause
    /// (kb/Work PB521, landed with kb/Work PB956: the a) leg used to be "enforced" only by the whole-unit refusal of
    /// BASED data in a class, and the b) leg by nothing — a TYPEDEF entry never reaches the property roster).</summary>
    public static readonly DiagnosticDescriptor PropertyWithBasedOrTypedef = new(
        "COBOLNET2333", "property-with-based-or-typedef", EditionSeverity.Error,
        "A data description entry specifies the PROPERTY clause together with a BASED clause or a TYPEDEF clause. "
        + "ISO §13.16.3 SR21: \"The PROPERTY clause shall not be specified in the same data description entry as: "
        + "a) a BASED clause, b) a TYPEDEF clause.\" A property's GET and SET methods act on the object's own "
        + "storage for the item, which a based entry (a template with no storage until an address is set) and a "
        + "type declaration (a template that describes no data item) do not have.",
        "ISO §13.16.3 SR21");

    /// <summary>An OCCURS KEY data-name-2 names no data item that is the table entry or subordinate to it
    /// (kb/Work PB1018). It used to compile clean — nothing checked the phrase until a SEARCH ALL or a table SORT
    /// read it.</summary>
    public static readonly DiagnosticDescriptor OccursKeyNotWithinTable = new(
        "COBOLNET2353", "occurs-key-not-within-table", EditionSeverity.Error,
        "A data-name of an OCCURS clause's ASCENDING / DESCENDING KEY phrase does not identify the entry containing "
        + "the OCCURS clause or an entry subordinate to it. ISO §13.18.38.3 SR3: \"The first specification of "
        + "data-name-2 shall be the name of either the entry containing the OCCURS clause or an entry subordinate to "
        + "the entry containing the OCCURS clause. Subsequent specification of data-name-2 shall be subordinate to "
        + "the entry containing the OCCURS clause.\" Name a data item of the table, qualified (K OF B) where the table "
        + "holds more than one item of that name.",
        "ISO §13.18.38.3 SR3");

    /// <summary>The internal-error net under EVERY refusal node (kb/Work PB1029) — the statement-level
    /// <c>BoundRejected</c> and the operand-level <c>BoundExprError</c> / <c>BoundOperandError</c> /
    /// <c>BoundBoolError</c> refusals — raised by the one statement funnel when a statement bound a refusal and
    /// drew no error, and by the refusal factory itself when no error has been recorded anywhere. Either way the
    /// refused node would otherwise compile clean and abort the run unit when reached. (A refused CONDITION keeps
    /// its own COBOLNET2319.)</summary>
    public static readonly DiagnosticDescriptor UnreportedRefusal = new(
        "COBOLNET2362", "unreported-refusal", EditionSeverity.Error,
        "WiseOwl COBOL internal error: the binder refused a statement or an operand without reporting the rule it "
        + "breaks. The compile is failed rather than let the refused node reach the generated program, where it "
        + "would abort the run unit when executed. Please report the source that produced it.",
        "WiseOwl COBOL internal (no ISO rule)");

    // kb/Work PB1030 — the reference resolver's segment materializer is the adjudicator of what may stand in a
    // subscript or reference-modifier position; a segment that is not an arithmetic expression used to come back
    // null UNREPORTED, and `DISPLAY E("A")` compiled with a "not implemented" warning and aborted at run time.
    public static readonly DiagnosticDescriptor NotASubscript = new(
        "COBOLNET2363", "not-a-subscript", EditionSeverity.Error,
        "Something that is not a subscript is written in a subscript or reference-modifier position. ISO §8.4.2.3.2: "
        + "a subscript is ALL, arithmetic-expression-1, or index-name-1 optionally followed by + or - and integer-1; "
        + "§8.4.2.3.3 SR6: \"The subscript ALL may be used only\" when the subscripted identifier is an intrinsic "
        + "function argument, or as the rightmost or only subscript of a table in a SORT statement's table format; "
        + "§8.4.3.3.3 SR4: \"Leftmost-position and length shall be arithmetic expressions.\"",
        "ISO §8.4.2.3.2 · §8.4.2.3.3 SR6 · §8.4.3.3.3 SR4");
    // kb/Work PB1030 — a reference to a name whose own declaration was refused: before, the resolver answered null
    // without a word and the statement was announced as a WiseOwl COBOL gap ("not implemented").
    public static readonly DiagnosticDescriptor ReferenceToRefusedDeclaration = new(
        "COBOLNET2364", "reference-to-refused-declaration", EditionSeverity.Error,
        "A statement references a name whose declaration the compiler refused — a REDEFINES entry its syntax rules "
        + "reject (Tier D), a RENAMES entry whose operands did not resolve, or a SCREEN SECTION entry (the section is "
        + "declined, COBOLNET1560). The declaration's own error names the rule; this one names the statement the "
        + "refusal reaches, so the reference is neither reported as undefined nor announced as a WiseOwl COBOL gap.",
        "ISO §8.4.2.1 (the reference identifies no bindable resource)");

    /// <summary>§14.9.49.3 SR3 — a statement in a declarative procedure references a nondeclarative procedure, and
    /// the statement is not RESUME (kb/Work PB362). A WARNING by determination (docs/CONFORMANCE.md §3): §4.2.2 asks
    /// only for a warning mechanism, the reference is executable as written, and the owner's precedence follows
    /// GnuCOBOL, which warns.</summary>
    public static readonly DiagnosticDescriptor DeclarativeReferencesNondeclarative = new(
        "COBOLNET2375", "declarative-references-nondeclarative", EditionSeverity.Warning,
        "A statement in a declarative procedure names a procedure in the nondeclarative portion of the procedure "
        + "division. ISO §14.9.49.3 SR3: \"Within a declarative procedure, there shall be no reference to any "
        + "nondeclarative procedures except in a RESUME statement.\" The program compiles and the statement runs "
        + "as written; the warning is the §4.2.2 indication that the source is outside the standard.",
        "ISO §14.9.49.3 SR3");

    /// <summary>§14.9.49.3 SR4 — a procedure of a declarative section is referenced from outside that section by a
    /// statement other than PERFORM (kb/Work PB362). An ERROR by determination (docs/CONFORMANCE.md §3).</summary>
    public static readonly DiagnosticDescriptor DeclarativeReferencedFromOutside = new(
        "COBOLNET2376", "declarative-referenced-from-outside", EditionSeverity.Error,
        "A GO TO, ALTER, or SORT/MERGE INPUT or OUTPUT PROCEDURE phrase names a procedure of a declarative section "
        + "from outside that section. ISO §14.9.49.3 SR4: \"Procedure-names within a declarative section may be "
        + "referenced in a different declarative section or in a nondeclarative procedure only with a PERFORM "
        + "statement.\" A USE procedure is entered by its USE dispatch or by a PERFORM, which returns; a transfer "
        + "into it has no activating USE to return to.",
        "ISO §14.9.49.3 SR4");

    /// <summary>§14.9.49.3 SR1 — a USE statement written anywhere other than immediately after a declarative
    /// section header (kb/Work PB361). The grammar admits USE as an ordinary statement so that the misplacement is
    /// named here rather than reported as a bare syntax error; the legal position is consumed by the declarative
    /// collection, so every USE statement that reaches the statement binder is misplaced.</summary>
    public static readonly DiagnosticDescriptor UseStatementPlacement = new(
        "COBOLNET2377", "use-statement-placement", EditionSeverity.Error,
        "A USE statement appears somewhere other than immediately after a section header in the declaratives "
        + "portion. ISO §14.9.49.3 SR1: \"A USE statement, when present, shall immediately follow a section header "
        + "in the declaratives portion of the procedure division and shall appear in a sentence by itself.\" A USE "
        + "statement is not executable; it describes when its declarative section runs.",
        "ISO §14.9.49.3 SR1");

    /// <summary>§14.9.49.3 SR10 / SR11 — the two restrictions on a USE BEFORE REPORTING procedure (kb/Work PB363):
    /// no GENERATE, INITIATE, or TERMINATE statement in its paragraphs, and no alteration of a control data
    /// item.</summary>
    public static readonly DiagnosticDescriptor UseBeforeReportingRestriction = new(
        "COBOLNET2378", "use-before-reporting-restriction", EditionSeverity.Error,
        "A USE BEFORE REPORTING procedure contains a statement its syntax rules forbid. ISO §14.9.49.3 SR10: \"The "
        + "GENERATE, INITIATE, or TERMINATE statements shall not appear in a paragraph within a USE BEFORE "
        + "REPORTING procedure.\" SR11: \"A USE BEFORE REPORTING procedure shall not alter the value of any "
        + "control data item.\" The procedure runs inside the report writer's processing of a report group, and "
        + "the report's control break detection has already read the control data items.",
        "ISO §14.9.49.3 SR10 / SR11");

    /// <summary>§11.9.7 — an ENTRY-CONVENTION clause written where §11.9.7.3 SR1 does not permit it, or naming an
    /// entry-convention-name-1, of which this implementation defines none (§11.9.7.4 GR3; Annex A.1 item 64,
    /// docs/CONFORMANCE.md §7). The clause used to be parsed and read by nothing but the edition gate, so any
    /// convention name compiled clean and the program was silently activated with the COBOL convention
    /// (kb/Work PB232).</summary>
    public static readonly DiagnosticDescriptor EntryConventionViolation = new(
        "COBOLNET2385", "entry-convention-violation", EditionSeverity.Error,
        "An ENTRY-CONVENTION clause is specified in a source element that may not carry it, or names an "
        + "entry-convention-name this implementation does not define. ISO §11.9.7.3 SR1: 'The ENTRY-CONVENTION "
        + "clause may be specified only in a class definition, a function definition, a function-prototype "
        + "definition, an interface definition, a program prototype definition, or a program definition that is not "
        + "contained within another program.' §11.9.7.4 GR3: 'When entry-convention-name-1 is specified, the "
        + "meaning of the entry convention is implementor-defined.' WiseOwl COBOL provides only the COBOL convention.",
        "ISO §11.9.7.3 SR1; §11.9.7.4 GR3");

    /// <summary>ISO §5.5 1) — an <c>integer-n</c> operand of a general format is written as zero where no associated
    /// rule permits it. The one screen is <c>Validation/IntegerOperandPass</c>, with the per-position exceptions in
    /// ONE table (kb/Work PB859). Before it, nothing enforced the NONZERO half anywhere: <c>OCCURS 0 TIMES</c>
    /// compiled and reached Roslyn as CS0029 errors in generated C#.</summary>
    public static readonly DiagnosticDescriptor IntegerOperandZero = new(
        "COBOLNET2386", "integer-operand-zero", EditionSeverity.Error,
        "An integer-n operand is zero. ISO §5.5 1): 'When the term integer-n (n = 1, 2, ...) is used in a general "
        + "format and associated rules, it refers to a fixed-point integer literal that shall be unsigned and nonzero "
        + "unless otherwise specified in the associated rules.' Only the positions whose rules expressly permit zero "
        + "accept it - LINAGE TOP/BOTTOM (§13.18.34.3 SR4), a relative LINE (§13.18.35.3 SR3), the OCCURS lower bound "
        + "(§13.18.38.3 SR16 / SR28), RECORD integer-2 / integer-4 (§13.18.43.3 SR7 / SR8), WRITE ADVANCING "
        + "(§14.9.51.3 SR15).",
        "ISO §5.5 1)");

    /// <summary>COBOLNET2427 — an <c>integer-n</c> the compiler binds into its model (a table size, a record or
    /// block size, a LINAGE or PAGE line count, a report LINE or COLUMN, an ordinal) exceeds 2,147,483,647, this
    /// implementation's limit. Screened once, pre-bind, by <c>Validation/IntegerOperandPass</c> for EVERY
    /// <c>integerLiteral</c> except the statement counts carried to run time at full value (PERFORM TIMES,
    /// WRITE ADVANCING). Before it, the report writer's and LINAGE's <c>int.Parse</c> took the compiler down with
    /// an unhandled <c>OverflowException</c> (kb/Work PB1058).</summary>
    public static readonly DiagnosticDescriptor IntegerOperandBeyondLimit = new(
        "COBOLNET2427", "integer-operand-beyond-limit", EditionSeverity.Error,
        "An integer-n operand exceeds this implementation's limit of 2,147,483,647 for an integer that sizes, "
        + "counts or positions something the compiler lays out. ISO §4.5: 'Translation may be unsuccessful due to "
        + "factors other than lack of conformance of a compilation group', and its NOTE names 'the limits of an "
        + "implementation'. The limit is documented in docs/CONFORMANCE.md §3 'Integer operands and host carriers'.",
        "ISO §4.5");
    /// <summary>§14.9.16.3 SR3/SR4, §14.9.21.3 SR2, §14.9.46.3 SR2 — a contained program's GENERATE, INITIATE or
    /// TERMINATE names a GLOBAL report (§13.18.27.3 SR1 e)) of a containing program whose file description entry
    /// is not GLOBAL (kb/Work PB369). The report is visible; the statement drives output to its file, which the
    /// contained program may not reference (the §13.18.27.3 SR3 posture, restated per verb).</summary>
    public static readonly DiagnosticDescriptor ContainedReportFileNotGlobal = new(
        "COBOLNET2392", "contained-report-file-not-global", EditionSeverity.Error,
        "A GENERATE, INITIATE, or TERMINATE statement in a contained program names a report defined in a "
        + "containing program, and the file description entry associated with that report does not contain a "
        + "GLOBAL clause. ISO §14.9.21.3 SR2 / §14.9.46.3 SR2 / §14.9.16.3 SR4: \"If report-name-1 is defined in a "
        + "containing program, the file description entry associated with report-name-1 shall contain a GLOBAL "
        + "clause\"; §14.9.16.3 SR3 says the same of a detail group's report and file. Add IS GLOBAL to the FD.",
        "ISO §14.9.16.3 SR3/SR4 · §14.9.21.3 SR2 · §14.9.46.3 SR2");

    // ── The data-description CLAUSE-PLACEMENT screen (kb/Work PB507 / PB512 / PB518 / PB519). One table —
    //    DataClausePlacement.Rules in the binder — reports through these three codes plus COBOLNET2191 (the
    //    PICTURE row, which predates the table). ──

    /// <summary>§13.16.3 SR11 — "The PICTURE, JUSTIFIED, and BLANK WHEN ZERO clauses may be specified only for an
    /// elementary data item." The JUSTIFIED and BLANK WHEN ZERO halves (the PICTURE half is COBOLNET2191,
    /// §13.18.40.3 SR1). Both clauses were recorded on a GROUP entry and silently ignored: the group's MOVE was
    /// neither right-justified nor blanked, and nothing told the programmer the clause was illegal (kb/Work
    /// PB512). Unchanged in all four editions.</summary>
    public static readonly DiagnosticDescriptor ClauseElementaryOnly = new(
        "COBOLNET2403", "clause-elementary-only", EditionSeverity.Error,
        "A JUSTIFIED or BLANK WHEN ZERO clause was specified on an entry that has subordinate entries. ISO §13.16.3 "
        + "SR11: \"The PICTURE, JUSTIFIED, and BLANK WHEN ZERO clauses may be specified only for an elementary data "
        + "item.\"",
        "ISO §13.16.3 SR11 / §13.18.32.3 SR1");

    /// <summary>The EXTERNAL / GLOBAL clause's RESIDENCE rules: the level-number (§13.16.3 SR6, §13.18.22.3 SR1,
    /// §13.18.27.3 SR1 b)), the section (§13.18.22.3 SR1 — EXTERNAL only in WORKING-STORAGE), the data-name format
    /// of the entry-name clause (§13.16.3 SR7, both the entry's own half and the FD-record half), and the
    /// same-entry exclusion (§13.16.3 SR5 — EXTERNAL with REDEFINES or BASED).
    /// <para>They had no clause-site screen: the post-bind registration scan wrote the level and name tests as
    /// <c>continue</c> filters, so a misplaced clause was accepted and then silently annulled (kb/Work PB518), and
    /// EXTERNAL on a REDEFINES entry was accepted and then re-based the redefines ANCHOR onto the run-unit cell,
    /// discarding the anchor's VALUE (kb/Work PB519).</para></summary>
    public static readonly DiagnosticDescriptor DataClausePlacement = new(
        "COBOLNET2404", "data-clause-placement", EditionSeverity.Error,
        "An EXTERNAL or GLOBAL clause was specified where its syntax rules do not admit it: below level 1, "
        + "outside the sections that admit it, on an entry without a data-name, or (EXTERNAL) in the same entry "
        + "as REDEFINES or BASED.",
        "ISO §13.16.3 SR5/SR6/SR7 · §13.18.22.3 SR1 · §13.18.27.3 SR1 b)");

    /// <summary>The SUBJECT rules of the BLANK WHEN ZERO and JUSTIFIED clauses — what the elementary item they are
    /// written on may be. §13.18.8.3 SR1 (category numeric-edited, or numeric without 'S') and SR2 (usage display
    /// or national, written or inherited); §13.18.32.3 SR3 (category alphabetic, alphanumeric, boolean or
    /// national — SR4's dynamic-length exclusion is §13.16.3 SR18's COBOLNET1563). The BLANK WHEN ZERO rules were written down once, in the
    /// legacy engine, and never ported: the typed-native binder only USED the flag, so the clause was silently
    /// ignored on every usage but display/national and on every non-numeric picture (kb/Work PB507).</summary>
    public static readonly DiagnosticDescriptor ClauseSubjectCategory = new(
        "COBOLNET2405", "clause-subject-category", EditionSeverity.Error,
        "A BLANK WHEN ZERO or JUSTIFIED clause was specified for an item whose category or usage the clause does "
        + "not admit.",
        "ISO §13.18.8.3 SR1/SR2 · §13.18.32.3 SR3");

    /// <summary>COBOLNET2406 — a VALUE clause written where §13.18.63.3 forbids its PLACEMENT (kb/Work PB550,
    /// PB551): a format 1 or format 2 VALUE clause in an entry that contains a REDEFINES clause or is subordinate
    /// to one (SR12, carried into format 2 by SR16), or a format 3 (condition-name) VALUE clause in an entry
    /// subordinate to a CONSTANT RECORD entry (SR25). Both compiled clean: the redefining entry's VALUE was
    /// silently discarded, and the condition-name under a CONSTANT RECORD was accepted and live.</summary>
    public static readonly DiagnosticDescriptor ValueClausePlacement = new(
        "COBOLNET2406", "value-clause-placement", EditionSeverity.Error,
        "A VALUE clause is written in an entry where the standard forbids it. ISO §13.18.63.3 SR12: \"The VALUE "
        + "clause shall not be specified in a data description entry that contains a REDEFINES clause or in an "
        + "entry that is subordinate to an entry containing a REDEFINES clause\" (SR16 applies it to format 2). "
        + "SR25: \"A format 3, 4, or 5 VALUE clause shall not be specified in any data description entry that "
        + "contains the CONSTANT RECORD clause, or in any data description entry subordinate to a data description "
        + "entry that contains the CONSTANT RECORD clause.\" Initialize the redefined entry instead; a level-88 "
        + "entry under a REDEFINES entry and the format 1 VALUEs that give a CONSTANT RECORD its content remain legal.",
        "ISO §13.18.63.3 SR12 / SR16 / SR25");

    /// <summary>COBOLNET2421 — the §13.18.16.2 CONTROL clause general format (kb/Work PB483): FINAL is ONE
    /// optional word written BEFORE the repeated data-name-1 bracket, never a repeatable operand. The grammar
    /// parses the superset <c>(FINAL | data-name)+</c> so the violation is named here rather than as a bare
    /// syntax error.</summary>
    public static readonly DiagnosticDescriptor ReportControlFinalPlacement = new(
        "COBOLNET2421", "report-control-final-placement", EditionSeverity.Error,
        "FINAL is written more than once in a CONTROL clause, or after a data-name-1. ISO §13.18.16.2 prints the "
        + "second operand form as `FINAL [ data-name-1 ] …`: FINAL once, first, and the ellipsis applies only to "
        + "the bracket enclosing data-name-1 (§5.2.7: \"the ellipsis applies to the portion of the format between "
        + "the determined pair of delimiters\"). §13.18.16.4 GR2: \"FINAL, if specified, is associated with the "
        + "highest level in the hierarchy\" — a FINAL written anywhere else would be a level that is not the "
        + "highest, and a second FINAL a level that can never break. Write FINAL once, as the first operand.",
        "ISO §13.18.16.2 · §5.2.7 · §13.18.16.4 GR2");

    /// <summary>COBOLNET2422 — the SIGN clause's placement rules, §13.18.52.3 SR1 (what the clause may be written
    /// on) and SR2 (the usage of an elementary subject) — kb/Work PB537. One screen for the data description
    /// entry and the report group description entry, whose SR1 bullets are the same test.</summary>
    public static readonly DiagnosticDescriptor SignClauseSubject = new(
        "COBOLNET2422", "sign-clause-subject", EditionSeverity.Error,
        "A SIGN clause is written on an entry it may not be written on. ISO §13.18.52.3 SR1: \"The SIGN clause "
        + "may be specified only for: — a numeric data or screen description entry whose picture character-string "
        + "contains the symbol 'S' — a numeric report group description entry whose picture character-string "
        + "contains the symbol 'S' — an alphanumeric group item, national group item, or strongly-typed group "
        + "item.\" SR2: \"The usage of an elementary item for which the SIGN clause is specified shall be display "
        + "or national.\" An unsigned, alphanumeric, national, edited or PICTURE-less elementary item, a bit group "
        + "or variable-length group item, and a signed item of any usage other than display or national each "
        + "violate one of the two rules.",
        "ISO §13.18.52.3 SR1 / SR2");

    /// <summary>COBOLNET2423 — a general-format element that is written in its own bracket WITHOUT an ellipsis
    /// is written more than once (§5.2.6.2 / §5.2.7). The report description entry's clauses (§13.14.2) and the
    /// PAGE clause's phrases (§13.18.39.2) may be written in any order (§13.14.3 SR2, §13.18.39.3 SR4), but an
    /// order licence is not a repetition licence (kb/Work PB483's sibling sweep).</summary>
    public static readonly DiagnosticDescriptor FormatElementRepeated = new(
        "COBOLNET2423", "format-element-repeated", EditionSeverity.Error,
        "A clause or phrase is written more than once where its general format admits it once. ISO §5.2.6.2: "
        + "brackets indicate that the enclosed element \"may be explicitly specified or that portion of the "
        + "general format may be omitted\"; §5.2.7: repetition is indicated only by an ellipsis, which \"applies to "
        + "the portion of the format between the determined pair of delimiters\". A rule that lets the elements be "
        + "written in any order (§13.14.3 SR2, §13.18.39.3 SR4) does not let any of them be written twice. Delete "
        + "the repeated clause or phrase.",
        "ISO §5.2.6.2 · §5.2.7");

    /// <summary>COBOLNET2424 — the OPTIONS paragraph's FLOAT-DECIMAL clause (§11.9.9), the §4.2.6 ¶3 warning a
    /// declined processor-dependent element owes. Annex A.3 item 13 makes the clause dependent "both on the
    /// capabilities of the processor and on support for the standard decimal floating-point usages", and neither
    /// FLOAT-DECIMAL-16 nor FLOAT-DECIMAL-34 is provided (A.3 item 19, COBOLNET1564). The clause was accepted with
    /// NO diagnostic until kb/Work R43 (2026-09-24) — a decline that leaked, which R43 item 5 makes a defect rather
    /// than a row to sign. ACCEPTED INERT rather than refused, the COBOLNET1778 precedent: every rule the clause
    /// states (§11.9.9.3 SR1-SR6) implies a phrase "for the USAGE clause in the data description entry of any data
    /// item described with a standard decimal floating-point usage", and no such item can be declared, so no
    /// program-visible value can change.</summary>
    public static readonly DiagnosticDescriptor FloatDecimalClauseUnsupported = new(
        "COBOLNET2424", "float-decimal-clause-unsupported", EditionSeverity.Warning,
        "The FLOAT-DECIMAL clause of the OPTIONS paragraph (ISO §11.9.9) is a processor-dependent element "
        + "(§4.2.6; Annex A.3 item 13) that is not supported: it sets the default encoding and endianness of the "
        + "standard decimal floating-point usages, and FLOAT-DECIMAL-16 and FLOAT-DECIMAL-34 are not provided "
        + "(Annex A.3 item 19). The clause is accepted and has no effect, because no data item it could apply to "
        + "can be declared. See docs/CONFORMANCE.md §2 row 13.",
        "ISO §4.2.6 ¶3 / Annex A.3 items 13 and 19 / §11.9.9", RecognizedNotImplemented,
        Annex: DeclinedAnnex.A3);

    /// <summary>A general format that prints ONE figurative spelling as a KEYWORD was written with a sibling
    /// spelling — <c>BLANK WHEN ZEROS</c>, <c>IF X IS ZEROES</c>, <c>INITIALIZE ALL TO BINARY ZERO</c>. ZERO,
    /// ZEROS and ZEROES (and each singular/plural figurative pair) are distinct §8.9 reserved words,
    /// interchangeable only where the §8.3.3.6.2 figurative constant itself is written; a keyword underlined in a
    /// general format is required as printed (§5.2.2). Emitted from the parse layer (<c>CobolErrorStrategy</c>),
    /// the families read from the grammar's ATN (kb/Work PB510).</summary>
    public static readonly DiagnosticDescriptor FigurativeSpellingNotTheKeyword = new(
        "COBOLNET2418", "figurative-spelling-not-the-keyword", EditionSeverity.Error,
        "A figurative-constant spelling was written where the general format names a different spelling as its "
        + "keyword.",
        "ISO §5.2.2 · §8.3.3.6.2 · §8.9 · §13.18.8.2 · §8.8.4.7.2 · §11.9.10.2");

    /// <summary>A PICTURE character-string whose LAST symbol is ',' or '.' is not followed immediately by the
    /// separator period — §13.18.40.3 SR7: "If the symbol ',' or the symbol '.' is the last symbol of
    /// character-string-1, the PICTURE clause shall be the last clause of the data description entry and shall
    /// be followed immediately (without an intervening separator space) by the separator period." Reached when a
    /// separator comma or semicolon (§8.3.5 rule 2) follows such a string — <c>PIC 999,, USAGE DISPLAY.</c>,
    /// <c>PIC 999., VALUE ZERO.</c> — the one shape the PICMODE lexer cannot fold into the separator period
    /// (kb/Work PB569).</summary>
    public static readonly DiagnosticDescriptor PictureTrailingSymbolNotLast = new(
        "COBOLNET2419", "picture-trailing-symbol-not-last", EditionSeverity.Error,
        "A PICTURE character-string ending in ',' or '.' is not followed immediately by the separator period.",
        "ISO §13.18.40.3 SR7 · §8.3.5 rules 2–3");

    /// <summary>An invocation of the method New through a factory object whose class does not inherit the standard
    /// class BASE (kb/Work PB1548). New is not predefined for every class: §16.2 puts it in BaseFactoryInterface,
    /// "the factory interface of the BASE class", §9.3.9 gives a subclass "all the methods defined for the inherited
    /// class definition", and §9.3.14.3 says "An instance object is created as the result of the NEW method being
    /// invoked on a factory object". So a class with no INHERITS FROM BASE (directly or through a superclass) has no
    /// New, and naming it is the violation of whichever §14.9.23.3 rule governs the receiver: SR3 for a class-name,
    /// §14.9.23.3 SR4 a)/c) for a FACTORY OF / FACTORY OF ACTIVE-CLASS reference, §14.9.23.3 SR4 f)/h) for SELF/SUPER in
    /// a factory method. One descriptor for every arm, so a new receiver form cannot reintroduce the
    /// predefined-for-everyone reading.</summary>
    public static readonly DiagnosticDescriptor NewWithoutBase = new(
        "COBOLNET2448", "new-without-base", EditionSeverity.Error,
        "INVOKE of the method New on a class that does not inherit from the standard class BASE.",
        "ISO §14.9.23.3 SR3/SR4 · §16.2 · §9.3.14.3");

    /// <summary>COBOLNET2455 — the FILE STATUS clause's data-name-1 breaks one of the clause's four syntax rules
    /// (kb/Work PB1080). None was screened: an OCCURS item compiled and died at OPEN, a PIC 99 item crashed the C#
    /// backend, a BASED item died EC-DATA-PTR-NULL at OPEN, and a one-, three-character, national, FD-record,
    /// dynamic-length or variable-length item ran with a truncated, padded or wrong-category status. The site names
    /// the rule it caught; <c>DataBinder.ScreenFileStatusItem</c> is the one screen.</summary>
    public static readonly DiagnosticDescriptor FileStatusItemRule = new(
        "COBOLNET2455", "file-status-item-rule", EditionSeverity.Error,
        "The FILE STATUS clause's data-name-1 breaks one of the clause's syntax rules (ISO §12.4.5.8.3): SR1 — it "
        + "shall not be subject to any OCCURS clauses; SR2 — it shall reference a two-character data item of the "
        + "category alphanumeric, defined in the working-storage, local-storage, or linkage section; SR3 — it shall "
        + "not reference a dynamic-length elementary item or a variable-length group; SR4 — it shall not be subject "
        + "to a BASED clause. The site names the rule it caught.",
        "ISO §12.4.5.8.3");

    /// <summary>COBOLNET2510 — a TYPE or SAME AS clause COMPOSES a BASED clause into an entry that may not carry one
    /// (kb/Work PB1300). §13.18.57.4 GR1 / §13.18.49.4 GR1 make the clause's effect "as though the data description
    /// identified by type-name-1 [data-name-1] had been coded in place", and neither GR-1 excludes BASED (§13.18.58.4
    /// GR3: "All other data description clauses … are assumed by data defined using the type-name"), so the
    /// composed entry answers to the BASED placement rules exactly as a written one does. The written arms keep
    /// their own codes; this one names the composition so the user sees WHERE the BASED clause came from.
    /// <c>DataBinder.ScreenComposedBased</c> is the one site.</summary>
    public static readonly DiagnosticDescriptor ComposedBasedPlacement = new(
        "COBOLNET2510", "composed-based-placement", EditionSeverity.Error,
        "A TYPE or SAME AS clause brings a BASED clause (from the description of type-name-1 or data-name-1) into "
        + "a data description entry where the BASED clause may not be specified: ISO §13.16.3 SR16 — only in the "
        + "linkage, working-storage and local-storage sections, at level-number 1 or 77; SR5 — not in the same entry "
        + "as the EXTERNAL clause; SR13 — not in the same entry as the CONSTANT RECORD clause. The site names the "
        + "rule it caught.",
        "ISO §13.18.57.4 GR1 · §13.18.49.4 GR1 · §13.16.3 SR5/SR13/SR16");

    /// <summary>COBOLNET2560 — the subject of a BASED entry is a dynamic-length elementary item or a variable-length
    /// group (kb/Work PB1213). The twin of COBOLNET1797 (SR1, class object); the shape is asked through
    /// <c>VariableLengthCompatibility.DynamicLengthOrVariableLengthGroup</c>, the one screen of that pair, and
    /// never through the BASED cell's carriage gates. <c>DataBinder.CheckUsageDeclarations</c> is the one site.</summary>
    public static readonly DiagnosticDescriptor BasedSubjectVariableLength = new(
        "COBOLNET2560", "based-subject-variable-length", EditionSeverity.Error,
        "ISO §13.18.5.3 syntax rule 2: \"The subject of the entry shall not be a dynamic-length elementary item or "
        + "a variable-length group.\" A variable-length group is §8.5.1.12.1's — a group with a dynamic-length "
        + "elementary item or a dynamic-capacity table subordinate to it; an occurs-depending group is not one.",
        "ISO §13.18.5.3 SR2");

    /// <summary>COBOLNET2561 — an item within a level-66 RENAMES range has a shape §13.18.45.3 SR8 bars (kb/Work
    /// PB1284). The range is enumerated over the declaration tree (§13.18.45.4 GR1/GR2) by
    /// <c>DataBinder.RenamesRangeFault</c>; the strongly-typed arms of the same rule are COBOLNET1532's.</summary>
    public static readonly DiagnosticDescriptor RenamesRangeContent = new(
        "COBOLNET2561", "renames-range-content", EditionSeverity.Error,
        "ISO §13.18.45.3 syntax rule 8: \"None of the items within the range, including data-name-2 and data-name-3, "
        + "if specified, shall be of class object, message-tag, or pointer, a strongly-typed group item, an item "
        + "subordinate to a strongly-typed group item, a variable-length data item, or an occurs-depending table.\" "
        + "This code names the class, variable-length (a dynamic-length elementary item or dynamic-capacity table, "
        + "§8.5.1.11.1) and occurs-depending arms; the strongly-typed arms report COBOLNET1532.",
        "ISO §13.18.45.3 SR8");

    /// <summary>COBOLNET2562 — a RENAMES … THROUGH area is not a whole number of bytes (kb/Work PB1284). Reachable
    /// only through USAGE BIT items, which §8.5.1.6.3 packs into shared bytes; measured on <c>BitLayout</c>.</summary>
    public static readonly DiagnosticDescriptor RenamesAreaNotWholeBytes = new(
        "COBOLNET2562", "renames-area-not-whole-bytes", EditionSeverity.Error,
        "ISO §13.18.45.3 syntax rule 10: \"The area described by data-name-2 THROUGH data-name-3 shall define an "
        + "integral number of bytes.\" Adjacent same-level USAGE BIT items share a byte (§8.5.1.6.3), so a range of "
        + "bit items can describe a fractional number of bytes.",
        "ISO §13.18.45.3 SR10");

    /// <summary>COBOLNET2563 — a PROPERTY clause breaks one of §13.18.42.3's syntax rules (kb/Work PB1273): SR1 (only in
    /// the working-storage section of a factory or instance definition — <c>DataBinder.BindEntry</c>), SR3's elementary
    /// half (the clause-placement table's ElementaryOnly row), and SR2 / SR3's qualification half / SR5 / SR6, asked of
    /// the item that carries the clause by <c>DataBinder.OoBindPropertyClauses</c>. The site names the rule it caught.
    /// SR4 (superclass collision) and the duplicate-accessor rule keep COBOLNET0842; §13.16.3 SR21 keeps COBOLNET2333.</summary>
    public static readonly DiagnosticDescriptor PropertyClauseRule = new(
        "COBOLNET2563", "property-clause-rule", EditionSeverity.Error,
        "A PROPERTY clause is written where ISO §13.18.42.3 does not admit it: SR1 — only in the working-storage section "
        + "of a factory definition or an instance definition; SR2 — not for a data item subject to an OCCURS clause; "
        + "SR3 — only for an elementary item whose name does not require qualification for uniqueness of reference; "
        + "SR5 — in or under a CONSTANT RECORD, the SET phrase (WITH NO SET) shall be specified; SR6 — not for an "
        + "object reference described with ACTIVE-CLASS. The site names the rule it caught.",
        "ISO §13.18.42.3 SR1/SR2/SR3/SR5/SR6");

    /// <summary>COBOLNET2630 — a prefixed literal whose CONTENT is outside its format's repertoire (kb/Work PB1441,
    /// PB1394): §8.3.3.2.3 SR5 / §8.3.3.5.3 SR4 "Hex-character-sequence-1 shall be composed of hexadecimal digits"
    /// (<c>X"…"</c>, <c>NX"…"</c>), §8.3.3.4.3 SR3 "Hexadecimal-digit-1 shall be a hexadecimal digit" (<c>BX"…"</c>)
    /// and §8.3.3.4.3 SR2 "Boolean-character-1 shall be a boolean character, '0' or '1'" (<c>B"…"</c>). Asked of every
    /// literal token by <c>LiteralScreenPass</c>; before the lexer admitted any content, such a literal silently
    /// re-lexed as a data-name plus a Format 1 literal.</summary>
    public static readonly DiagnosticDescriptor LiteralContentRepertoire = new(
        "COBOLNET2630", "literal-content-repertoire", EditionSeverity.Error,
        "A hexadecimal or boolean literal contains a character its format does not admit: X\"…\", NX\"…\" and BX\"…\" "
        + "admit only hexadecimal digits (0–9, A–F), and B\"…\" only the boolean characters 0 and 1.",
        "ISO §8.3.3.2.3 SR5 · §8.3.3.5.3 SR4 · §8.3.3.4.3 SR2/SR3");

    /// <summary>COBOLNET2631 — a comma or semicolon not immediately followed by a space (kb/Work PB1394). §8.3.5 2):
    /// "The COBOL characters comma and semicolon, immediately followed by a space, are separators" — without the space
    /// the character is no separator, so <c>N,M</c> is not two operands. The one legal space-less comma is the
    /// decimal point of a numeric literal under DECIMAL-POINT IS COMMA, which the numeric-literal rules own.
    /// Decided post-lex by <c>SeparatorRule</c>.</summary>
    public static readonly DiagnosticDescriptor SeparatorCommaWithoutSpace = new(
        "COBOLNET2631", "separator-comma-without-space", EditionSeverity.Error,
        "A comma or semicolon is not immediately followed by a space, so it is not a separator.",
        "ISO §8.3.5 2)");

    /// <summary>COBOLNET2632 — a period not followed by a space (kb/Work PB1394). §8.3.5 3): "The COBOL character
    /// period, when followed by a space, is a separator" — so <c>N.DISPLAY</c> ends no sentence. A period inside a
    /// numeric literal or a PICTURE character-string is part of that character-string, never this token. Decided
    /// post-lex by <c>SeparatorRule</c>.</summary>
    public static readonly DiagnosticDescriptor SeparatorPeriodWithoutSpace = new(
        "COBOLNET2632", "separator-period-without-space", EditionSeverity.Error,
        "A period is not followed by a space, so it is not a separator period.",
        "ISO §8.3.5 3)");

    /// <summary>COBOLNET2633 — a literal whose opening or closing delimiter is not separated as §8.3.5 5) requires
    /// (kb/Work PB1394): "The opening delimiter shall be immediately preceded by a space, left parenthesis, or opening
    /// pseudo-text delimiter. The closing delimiter shall be immediately followed by one of the separators space,
    /// comma, semicolon, period, right parenthesis, or closing pseudo-text delimiter." Decided post-lex by
    /// <c>SeparatorRule</c>, over every literal token of either lexer mode.</summary>
    public static readonly DiagnosticDescriptor SeparatorLiteralDelimiter = new(
        "COBOLNET2633", "separator-literal-delimiter", EditionSeverity.Error,
        "A literal's opening delimiter is not immediately preceded by a space, left parenthesis or opening "
        + "pseudo-text delimiter, or its closing delimiter is not immediately followed by a space, comma, semicolon, "
        + "period, right parenthesis or closing pseudo-text delimiter.",
        "ISO §8.3.5 5)");

    /// <summary>COBOLNET2634 — a file-name is specified in a second SELECT clause of the same factory, function, object
    /// or program (ISO §12.4.5.2 SR2; kb/Work PB1077). The second entry used to replace the first in the binder's name
    /// map, so the program ran against whichever ASSIGN target came last. Reported by
    /// <c>DataBinder.BindFileControl</c> at the second SELECT.</summary>
    public static readonly DiagnosticDescriptor FileNameSelectedTwice = new(
        "COBOLNET2634", "file-name-selected-twice", EditionSeverity.Error,
        "A file-name is specified in more than one SELECT clause. ISO §12.4.5.2 SR2: \"A given file-name may be "
        + "specified in only one SELECT clause within a factory, function, object, or program.\" Give each file "
        + "control entry its own file-name, or remove the duplicate entry.", "ISO §12.4.5.2 SR2");

    /// <summary>COBOLNET2635 — a SELECT clause names a file-name that has no file description entry and no sort-merge
    /// file description entry in the file section (ISO §12.4.5.2 SR3; kb/Work PB1077). Such a file compiled clean and
    /// its first OPEN aborted the run unit as a "compiler defect". Reported by <c>DataBinder.BindFileSection</c> at
    /// the file control entry.</summary>
    public static readonly DiagnosticDescriptor SelectWithoutFileDescription = new(
        "COBOLNET2635", "select-without-file-description", EditionSeverity.Error,
        "A file control entry names a file that has no file description entry (FD) and no sort-merge file description "
        + "entry (SD). ISO §12.4.5.2 SR3: \"For each file-name specified in a SELECT clause, there shall be a file "
        + "description entry or a sort-merge file description entry in the file section of the factory, function, "
        + "object, or program in which the SELECT clause is specified.\" Add the FD or SD, or remove the SELECT.",
        "ISO §12.4.5.2 SR3");

    /// <summary>COBOLNET2636 — a file description entry or a sort-merge file description entry names a file-name that
    /// no file control entry specifies (ISO §13.4.5.3 SR1 for an FD, §13.4.6.3 SR1 for an SD; kb/Work PB1237,
    /// PB1290 — the other arm of kb/Work PB1077's pairing). The binder used to synthesize a file with an empty
    /// assignment, so the program compiled and even ran I-O against it. Reported by <c>DataBinder.BindFileSection</c>
    /// at the entry.</summary>
    public static readonly DiagnosticDescriptor FileDescriptionWithoutSelect = new(
        "COBOLNET2636", "file-description-without-select", EditionSeverity.Error,
        "A file description entry (FD) or sort-merge file description entry (SD) names a file that no file control "
        + "entry specifies. ISO §13.4.5.3 SR1 and §13.4.6.3 SR1: \"File-name-1 shall be specified in a file control "
        + "entry.\" Add a SELECT clause for the file in the FILE-CONTROL paragraph.", "ISO §13.4.5.3 SR1 / §13.4.6.3 SR1");

    /// <summary>COBOLNET2670 — a reference modification whose leftmost-position and/or length are integer LITERALS
    /// and whose range violates §8.4.3.3.4 5) b)/c) against an item of FIXED size, at a statement where checking
    /// for EC-BOUND-REF-MOD is NOT enabled (kb/Work PB1707 part 1, owner decision R60). Reported by
    /// <c>ReferenceResolver</c> at the reference; the statement is refused. <see cref="RefModLiteralOutOfRangeChecked"/>
    /// is the same finding where checking IS enabled.</summary>
    public static readonly DiagnosticDescriptor RefModLiteralOutOfRange = new(
        "COBOLNET2670", "refmod-literal-out-of-range", EditionSeverity.Error,
        "A reference modification of an item of fixed size has a literal leftmost-position or length that lies "
        + "outside the item, and checking for EC-BOUND-REF-MOD is not enabled at this statement. ISO §8.4.3.3.4 5) "
        + "b) and c): the leftmost position shall be positive and not exceed the number of positions in the data "
        + "item, and \"The sum of leftmost-position and length minus the value one shall be less than or equal to "
        + "the number of positions in the data item referenced by identifier-1\"; a violation is the fatal "
        + "EC-BOUND-REF-MOD. §14.6.13.1.3 8) (last paragraph) licenses the compiler, when checking is not enabled "
        + "and it detects a fatal condition, to produce no executable code, and WiseOwl COBOL (as GnuCOBOL) refuses "
        + "the statement rather than build a program that can only terminate. Correct the literal, or enable "
        + "checking (>>TURN EC-BOUND-REF-MOD CHECKING ON) to have the run unit raise the condition at run time.",
        "ISO §8.4.3.3.4 5) b)/c) / §14.6.13.1.3 8)");

    /// <summary>COBOLNET2671 — the <see cref="RefModLiteralOutOfRange"/> finding at a statement where checking for
    /// EC-BOUND-REF-MOD IS enabled: the statement compiles, because the run-time raise of the condition is what the
    /// program asked for (a USE declarative may handle it), and the compiler warns that the raise is certain.</summary>
    public static readonly DiagnosticDescriptor RefModLiteralOutOfRangeChecked = new(
        "COBOLNET2671", "refmod-literal-out-of-range-checked", EditionSeverity.Warning,
        "A reference modification of an item of fixed size has a literal leftmost-position or length that lies "
        + "outside the item (ISO §8.4.3.3.4 5) b)/c)). Checking for EC-BOUND-REF-MOD is enabled at this statement, so "
        + "the program compiles and every execution of the statement raises the fatal EC-BOUND-REF-MOD.",
        "ISO §8.4.3.3.4 5) b)/c) / §14.6.13.1.3 8)");

    /// <summary>COBOLNET2727 — the get and set property methods of ONE property disagree about the property's data
    /// description (kb/Work PB1450). ISO §8.4.3.9.3 SR7: "The data description of the item specified in the RETURNING
    /// phrase of the get property method shall be the same as the data description of the item specified as the USING
    /// parameter of the set property method." The pair is read as the class sees it, so a GET inherited from a
    /// superclass and a SET written in the subclass (or the reverse) is compared like two accessors of one class.
    /// Rejected at bind — the §8.4.3.9.4 temp is modelled on ONE description, and with two it would cross a formal
    /// of another.</summary>
    public static readonly DiagnosticDescriptor PropertyAccessorDescriptionMismatch = new(
        "COBOLNET2727", "property-accessor-description-mismatch", EditionSeverity.Error,
        "The RETURNING item of a property's get property method and the USING parameter of its set property method "
        + "are not described identically. ISO §8.4.3.9.3 SR7: \"The data description of the item specified in the "
        + "RETURNING phrase of the get property method shall be the same as the data description of the item "
        + "specified as the USING parameter of the set property method.\"",
        "ISO §8.4.3.9.3 SR7");

    /// <summary>COBOLNET2761 — an interface's method PROTOTYPE redeclares one it inherits (kb/Work PB1502). ISO §11.7.3
    /// SR4 b): "if this method definition is contained in an interface definition, no inherited method prototype shall
    /// have the same method resolution signature as the method prototype declared by this method definition". SR2 forbids
    /// OVERRIDE in a prototype, so there is no legal spelling of a redeclaration. The interface twin of the class arm's
    /// SR4 a) (COBOLNET0837).</summary>
    public static readonly DiagnosticDescriptor InterfacePrototypeRedeclaresInherited = new(
        "COBOLNET2761", "interface-prototype-redeclares-inherited", EditionSeverity.Error,
        "An interface's method prototype has the same method resolution signature as a method prototype it inherits. "
        + "ISO §11.7.3 SR4 b); the OVERRIDE phrase is forbidden in a prototype (SR2).",
        "ISO §11.7.3 SR4 b)");

    /// <summary>COBOLNET2762 — an interface inherits one method-name from several interfaces whose prototypes do not
    /// all conform to one another (kb/Work PB1502). ISO §11.6.3 SR5: "the method prototype in each inherited interface
    /// shall be such that this interface conforms to all inherited interfaces"; §9.3.10: "The inheriting interface
    /// shall always conform to each of the inherited interfaces". Raised on the INHERITING interface.</summary>
    public static readonly DiagnosticDescriptor InterfaceInheritedPrototypesConflict = new(
        "COBOLNET2762", "interface-inherited-prototypes-conflict", EditionSeverity.Error,
        "A method-name inherited from more than one interface has prototypes that do not all conform to one another, so "
        + "the inheriting interface cannot conform to all of them. ISO §11.6.3 SR5; §9.3.10.",
        "ISO §11.6.3 SR5");

    /// <summary>COBOLNET2728 — an INVOKE argument or RETURNING item is a group item with a subordinate object reference
    /// described with the ACTIVE-CLASS phrase (kb/Work PB1116). ISO §14.9.23.3 SR13: "If Identifier-3, identifier-4,
    /// or identifier-5 references a group item, there shall not be an item subordinate to that group item that is an
    /// object reference described with the ACTIVE-CLASS phrase." Typed and universal receivers alike, both passing
    /// modes and the RETURNING item.</summary>
    public static readonly DiagnosticDescriptor InvokeActiveClassSubordinate = new(
        "COBOLNET2728", "invoke-active-class-subordinate", EditionSeverity.Error,
        "An INVOKE argument or RETURNING item is a group item with a subordinate object reference described with the "
        + "ACTIVE-CLASS phrase. ISO §14.9.23.3 SR13: \"If Identifier-3, identifier-4, or identifier-5 references a "
        + "group item, there shall not be an item subordinate to that group item that is an object reference described "
        + "with the ACTIVE-CLASS phrase.\"",
        "ISO §14.9.23.3 SR13");

    /// <summary>COBOLNET2738 — an I-O-CONTROL SAME clause violates one of its syntax rules (kb/Work PB1087): fewer than
    /// the two files its format prints (<c>file-name-1 { file-name-2 } …</c>), an operand that is no file of this
    /// source element (§12.4.6.4.3 SR2) or an external file connector (SR3), a file named in more clauses of a format
    /// than its kind allows (SR5 report file, SR6 sort or merge file, SR7 any other file), a sort-merge-area clause with
    /// no sort or merge file (SR8), or a file-area clause that shares files with a record-area clause (SR9) or with a
    /// sort-merge-area clause (SR10) without being contained in it, or a GLOBAL clause on the file or record description
    /// entries of files sharing a record area (§13.18.27.3 SR2, kb/Work PB1242). One code for the rule family, the
    /// message names the rule.</summary>
    public static readonly DiagnosticDescriptor SameClauseRule = new(
        "COBOLNET2738", "same-clause-rule", EditionSeverity.Error,
        "A SAME clause of the I-O-CONTROL paragraph violates one of its syntax rules. Every format names two or more "
        + "files (§12.4.6.4.2; §12.4.6.4.4 GR1, GR2); each shall be specified in the FILE-CONTROL paragraph of the "
        + "source element (SR2) and shall not reference an external file connector (SR3); a report file may be named "
        + "in one file-area clause only (SR5); a sort or merge file in at most one record-area and one sort-merge-area "
        + "clause and no file-area clause (SR6); any other file in at most one file-area clause, one record-area clause "
        + "and any number of sort-merge-area clauses (SR7); a sort-merge-area clause names at least one sort or merge "
        + "file (SR8); a file-area clause that shares a file with a record-area clause (SR9), or a non-sort file with "
        + "sort-merge-area clauses (SR10), is wholly contained in each. The file description entries and record "
        + "description entries of files sharing a record area do not include the GLOBAL clause (§13.18.27.3 SR2).",
        "ISO §12.4.6.4.3 SR2, SR3, SR5-SR10; §12.4.6.4.4 GR1, GR2; §13.18.27.3 SR2");

    /// <summary>COBOLNET2739 — a REDEFINES clause violates one of its ENTRY-level syntax rules (kb/Work PB1280): not the
    /// first clause of its entry (§13.18.44.3 SR1), a subject and data-name-2 of different level-numbers (SR2), a
    /// level-1 file-section entry (SR3), a CONSTANT RECORD as data-name-2 (SR13), or a subject whose required alignment
    /// differs from data-name-2's (SR15). The size screen (SR8) keeps COBOLNET1539. One code for the rule family; the
    /// message names the rule.</summary>
    public static readonly DiagnosticDescriptor RedefinesEntryRule = new(
        "COBOLNET2739", "redefines-entry-rule", EditionSeverity.Error,
        "A REDEFINES clause violates one of its entry-level syntax rules. It shall immediately follow the entry-name "
        + "clause, or the level-number when there is none (SR1); the level-numbers of data-name-2 and the subject of the "
        + "entry shall be identical (SR2); it shall not be specified in a level-1 entry in the file section (SR3); "
        + "data-name-2 shall not be described with the CONSTANT RECORD clause (SR13); and the subject's required "
        + "alignment shall be the same as the alignment of data-name-2 (SR15).",
        "ISO §13.18.44.3 SR1, SR2, SR3, SR13, SR15");

    /// <summary>COBOLNET2740 — a RENAMES entry violates one of its entry-level syntax rules (kb/Work PB1283): it does not
    /// immediately follow the last entry of its record (§13.18.45.3 SR2), an operand is subject to an OCCURS clause
    /// (SR3), is a level-1 or level-77 entry (SR5) or lies within a CONSTANT RECORD (SR6), or data-name-3 begins before
    /// data-name-2 or does not end after it (SR11). SR4 (the operands name items of this record) is COBOLNET1655, SR8
    /// the range content and SR10 the whole-byte area. One code for the rule family; the message names the rule.</summary>
    public static readonly DiagnosticDescriptor RenamesEntryRule = new(
        "COBOLNET2740", "renames-entry-rule", EditionSeverity.Error,
        "A RENAMES entry violates one of its entry-level syntax rules. All RENAMES entries of a record immediately "
        + "follow the last data description entry of that record (SR2); data-name-1, data-name-2 and data-name-3 are "
        + "not subject to any OCCURS clause (SR3); data-name-2 and data-name-3 do not refer to a level-1, 66, 77 or 88 "
        + "entry (SR5) nor to an entry within a CONSTANT RECORD (SR6); and data-name-3 does not begin before "
        + "data-name-2 and ends after it (SR11).",
        "ISO §13.18.45.3 SR2, SR3, SR5, SR6, SR11");

    /// <summary>Every descriptor declared above (reflected, so a new field is picked up automatically by the
    /// <c>docs/DIAGNOSTICS.md</c> generator and the drift test — no hand-maintained list to forget).</summary>
    public static IReadOnlyList<DiagnosticDescriptor> All { get; } = typeof(DiagnosticCatalog)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(DiagnosticDescriptor))
        .Select(f => (DiagnosticDescriptor)f.GetValue(null)!)
        .OrderBy(d => d.Code, StringComparer.Ordinal)
        .ThenBy(d => d.Id, StringComparer.Ordinal)
        .ToList();
}
