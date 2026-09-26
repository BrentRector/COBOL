// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>The role a key operand plays in a file control entry — the axis the ORGANIZATION formats of
/// ISO/IEC 1989:2023 §12.4.5.1 are cut on: a prime RECORD KEY and an ALTERNATE RECORD KEY belong to Format 1
/// (indexed), a RELATIVE KEY to Format 2 (relative).</summary>
internal enum FileKeyRole
{
    /// <summary>RECORD KEY IS data-name-1 (ISO §12.4.5.12) — Format 1, exactly one per entry.</summary>
    PrimeRecordKey,

    /// <summary>ALTERNATE RECORD KEY IS data-name-1 (ISO §12.4.5.6) — Format 1, zero or more per entry.</summary>
    AlternateRecordKey,

    /// <summary>RELATIVE KEY IS data-name-1 (ISO §12.4.5.13) — Format 2, at most one per entry.</summary>
    RelativeKey,

    /// <summary>NOT A KEY CLAUSE: the file control ENTRY itself, for a rule whose subject is the entry rather
    /// than one operand of it — §12.4.5.2 SR8 and SR9's SECOND sentence, <i>"The associated file description
    /// entry shall not be a sort-merge file description entry"</i>, which is broken by an entry that specifies
    /// §12.4.5.1's Format 1 or Format 2 in ANY of its ways, including by writing only
    /// <c>ORGANIZATION IS INDEXED</c> with no key clause at all. It contributes exactly ONE operand, with a null
    /// name, so a rule about the entry is the same kind of row as a rule about an operand.</summary>
    Entry,
}

/// <summary>The KIND of file a rule is screened on — the set that <see cref="FileControlKeyRule.ScreenedOn"/>
/// names. One member per <see cref="FileOrganization"/>, plus <see cref="SortMerge"/> for a file described by a
/// sort-merge file description entry, which §12.4.5.1's Format 4 gives no organization to speak of.
/// <para>⛔ IT IS A SET, NOT A SCALAR, and that is the whole shape of kb/Work PB742. The column used to be one
/// <see cref="FileOrganization"/> meaning "the organization whose §12.4.5.1 format carries this clause", which
/// conflated two different facts: WHICH FORMAT the clause belongs to (a function of the key role, and fixed) and
/// WHICH FILES the rule is in force over. They are the same set for every rule stated INSIDE a format, and they
/// are DISJOINT for §12.4.5.2 SR8/SR9, which are stated about a clause appearing where its format does not
/// apply. A scalar column cannot express the second, so those two rules had no row and no site at all
/// (feedback_model_the_rule_shape_not_one_case).</para></summary>
[Flags]
internal enum FileKinds
{
    None = 0,
    Sequential = 1 << FileOrganization.Sequential,
    LineSequential = 1 << FileOrganization.LineSequential,
    Relative = 1 << FileOrganization.Relative,
    Indexed = 1 << FileOrganization.Indexed,

    /// <summary>A file described by a sort-merge file description entry (an SD). Not an organization: §12.4.5.1
    /// Format 4 admits only the SEQUENTIAL phrase, and §12.4.5.2 SR13 requires the SD.</summary>
    SortMerge = 1 << 8,

    /// <summary>Every organization — the four §12.4.5.10.3 phrases, no sort-merge file.</summary>
    AnyOrganization = Sequential | LineSequential | Relative | Indexed,
}

/// <summary>ONE operand of a file control entry, as the screen sees it: the clause's data-name-1 as WRITTEN
/// (<paramref name="Name"/>, null when the clause is absent), what it resolved to (<paramref name="Item"/>, null
/// when it referenced nothing describable), and the cursor its rules report at.
/// <para>An ABSENT clause is an operand too — with a null name — because §12.4.5.1's required members and
/// §12.4.5.2 SR10 are violated by the absence itself. Modelling absence as a missing operand instead would need a
/// second kind of rule and a second arm to dispatch on, which is the shape this file exists to avoid.</para>
/// <para><paramref name="ClauseFace"/> is the clause's name as the standard prints it, and it is the ONLY
/// spelling of it: every message below renders it rather than repeating a literal, so a role and its
/// diagnostics cannot drift apart.</para>
/// <para><paramref name="Written"/> is the THIRD state a clause can be in, and it exists because "absent" and
/// "named nothing" are not the same fact (kb/Work PB358). A clause written in the declined
/// <c>record-key-name-1 SOURCE IS data-name-2 …</c> form (Annex A.3 item 40 — COBOLNET1954) is refused at bind
/// and contributes NO data-name, so <paramref name="Name"/> is null while the clause is plainly there. Only the
/// §12.4.5.1 Format-1 requiredness row reads it, because that is the only rule whose subject is the clause's
/// PRESENCE; every operand rule below it tests <paramref name="Item"/> or <paramref name="Name"/> and is
/// correctly silent on a refused clause.</para>
/// <para><paramref name="SuppressWhen"/> is an ALTERNATE RECORD KEY clause's SUPPRESS WHEN literal-1 as read
/// (§12.4.5.6.3 SR7's subject); null on every other role and on a clause without the phrase.</para></summary>
internal readonly record struct FileKeyOperand(
    FileKeyRole Role, string ClauseFace, string? Name, DataItem? Item, DiagnosticCursor At, bool Written = true,
    SuppressWhenOperand? SuppressWhen = null);

/// <summary>ONE syntax rule of the file control entry stated about a key clause.</summary>
/// <param name="RuleId">The traceability-inventory row this rule closes (<c>SR-12.4.5.12.3-1</c>), or null for a
/// requirement carried by a GENERAL FORMAT rather than by a numbered rule (§12.4.5.1 Format 1's unbracketed
/// RECORD KEY clause). The drift test keys on this.</param>
/// <param name="Clause">The clause number, exactly as <c>scripts/spec/cite.py --check</c> takes it.</param>
/// <param name="Citation">The citation as it appears in the shipped diagnostic — clause plus printed ordinal.</param>
/// <param name="RuleText">A verbatim span of the PRINTED rule, long enough to identify it. The drift test asserts
/// this span is inside <paramref name="Clause"/>'s own region of <c>specs/ISO_COBOL.md</c>, so an inherited or
/// drifted clause number turns a test red instead of shipping (CLAUDE.md rule 1).</param>
/// <param name="ScreenedOn">The SET of file kinds this rule is in force over. For a rule stated inside a format
/// it is exactly that format's own kind (a RECORD KEY rule screens indexed files); for §12.4.5.2 SR8/SR9 it is
/// DISJOINT from the format's kind, because those rules are about a clause written where its format does not
/// apply. See <see cref="FileKinds"/> for why it may not be a scalar.</param>
/// <param name="Role">Which clause of the entry the rule is stated about (<see cref="FileKeyRole.Entry"/> for a
/// rule stated about the entry itself).</param>
/// <param name="Applies">The entry-level precondition beyond the kind (an access mode, a present FD).</param>
/// <param name="Violated">Whether this operand breaks the rule.</param>
/// <param name="Message">The diagnostic body — what the operand IS, then the rule, then the citation.</param>
/// <param name="Code">The diagnostic this row reports under. Almost every row is COBOLNET0863's own subject —
/// "a file control entry's key clause breaks one of its syntax rules" — which is why the code is a column with a
/// default rather than a parameter every row restates. §12.4.5.2 SR8/SR9's SECOND sentence is the exception: its
/// subject is the FILE DESCRIPTION ENTRY, not a key clause, and its remedy is a different edit, so it carries its
/// own code (COBOLNET1900).</param>
internal sealed record FileControlKeyRule(
    string? RuleId,
    string Clause,
    string Citation,
    string RuleText,
    FileKinds ScreenedOn,
    FileKeyRole Role,
    Func<FileModel, bool> Applies,
    Func<FileModel, FileKeyOperand, bool> Violated,
    Func<FileModel, FileKeyOperand, string> Message,
    DiagnosticDescriptor? Code = null)
{
    /// <summary>The descriptor <see cref="FileControlKeyRules.Screen"/> reports this row under.</summary>
    public DiagnosticDescriptor Diagnostic => Code ?? DiagnosticCatalog.FileKeyClauseRule;
}

/// <summary>
/// ⛔ THE ONE SCREEN FOR THE FILE CONTROL ENTRY'S KEY SYNTAX RULES (ISO/IEC 1989:2023 §12.4.5), run from
/// <c>DataBinder.ResolveFiles</c> over EVERY declared file once the data forest is indexed.
/// <para>WHY IT EXISTS. These rules were enforced by <c>KeyedIoBinder.KeyedValidateFile</c>, which ran on the
/// FIRST KEYED VERB that named the file. An entry whose RECORD KEY and ALTERNATE RECORD KEY both sat under an
/// OCCURS clause therefore compiled with zero diagnostics when the procedure division only OPENed and CLOSEd the
/// file, and grew two errors the moment a READ was added (kb/Work PB699, measured). They are syntax rules of the
/// ENTRY: the entry violates them whether or not a statement references the file, and §4.2.2's mechanism has to
/// be able to indicate it. The screen therefore lives with the entry, and the verbs keep only the rules that are
/// genuinely stated about a STATEMENT.</para>
/// <para>⛔ THE ORGANIZATION COLUMN IS A SET (<see cref="FileKinds"/>), NOT A SCALAR. It began as one
/// <see cref="FileOrganization"/> that meant two things at once — the format that CARRIES a clause and the files
/// a rule is IN FORCE over — which are the same for a rule stated inside a format and OPPOSITE for §12.4.5.2 SR8
/// and SR9, whose whole subject is a clause written where its format does not apply. Those two rules therefore
/// had no row and no site: `SELECT F ASSIGN … RECORD KEY IS K.` (no ORGANIZATION clause, so record sequential by
/// §12.4.5.10.3 GR6) compiled clean and bound a key nothing read (kb/Work PB742, measured).</para>
/// <para>⛔ TWO ARMS OF ONE SENTENCE ARE TWO ROWS WITH ONE RULE-ID. §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 each
/// join a CATEGORY and a LOCATION with "within"; only the location arm was ever screened, so a `PIC 9(5)` prime
/// key built an index on an operand the standard forbids, silently (kb/Work PB743). The inventory row is one, and
/// it earns CONFORMS only when every row carrying its id is live — which is why PB699 recorded no verdict for
/// either while it held the location arm alone.</para>
/// <para>WHY A TABLE. The three key clauses state overlapping rule SETS keyed by organization — §12.4.5.12.3 and
/// §12.4.5.6.3 state the identical OCCURS ban word for word, §12.4.5.13.3 states it a third time for the relative
/// key — and the shape where one member of such a set is written down is exactly the shape in which the missing
/// members hide (kb/Work PB354 found two that way, PB699 a third). One row per rule, one loop, one report site:
/// adding the next key rule is a row, and <c>FileControlKeyRuleDriftTests</c> re-derives every row's RULE TEXT
/// from <c>specs/ISO_COBOL.md</c> so a row can neither carry an inherited clause number nor go stale.</para>
/// <para>EDITIONS. Every rule here is present in all four supported editions (85 · 2002 · 2014 · 2023) — the
/// RECORD KEY, ALTERNATE RECORD KEY and RELATIVE KEY clauses and their syntax rules all predate COBOL-85 — so
/// the table needs no edition column and the negatives reject at every edition. A rule that arrived with a later
/// edition would add one, and its gate would ride <c>EditionContext</c> exactly as every other introduction does.</para>
/// <para>⚖ THE SIBLING SITE. Entry rules that are decidable from the CLAUSES ALONE — §12.4.5.5.2 SR2 (no DYNAMIC
/// or RANDOM on a sequential file, kb/Work PB692) and the §12.4.5.9 SR2 screen beside it — stay inline in
/// <c>DataBinder.BindFileControl</c>, because they need nothing resolved and can report as the entry is read.
/// This table holds the rules that need a RESOLVED DATA ITEM: a key clause names a data-name whose OCCURS
/// ancestry, PICTURE and owning record description are unknown until the data forest is indexed. That is the
/// whole criterion for which of the two a new entry rule joins — `COBOLNET_FILES_DESIGN.md` D19.</para>
/// <para>NOT HERE, deliberately: the SEMANTIC rules of the same clauses — §12.4.5.12.3 SR3/SR4/SR5 and
/// §12.4.5.6.3 SR3/SR4/SR6 — which need machinery this screen does not have (variable-length record geometry, the
/// record-key-name-1 SOURCE phrase, which has no grammar carrier: Annex A.3 item 40). Each is one row when it is
/// implemented; that is the point of the table — §12.4.5.6.3 SR5 and SR7 became rows exactly that way (kb/Work
/// PB1025, PB1072).</para>
/// <para>ALSO NOT HERE, and the one place §12.4.5.2 SR8 is written down twice: the COLLATING SEQUENCE clause is a
/// Format-1 clause too, and a file-level one on a non-indexed file is refused by
/// <c>DataBinder.ResolveFileCollating</c> (COBOLNET1582) — because that test is ALSO the guard that stops the
/// rest of that method, and splitting a guard from its report would leave the resolution running on an entry it
/// has already refused. Its citation was §12.4.5.7.1, the clause's descriptive General paragraph; it is SR8 and
/// now says so. The traceability row `SR-12.4.5.2-8` names both sites.</para>
/// </summary>
internal static class FileControlKeyRules
{
    /// <summary>Every file-control-entry key syntax rule this compiler screens, keyed by organization and role.
    /// ⛔ ORDER IS SIGNIFICANT ONLY WITHIN A ROLE, and only so a program hears the most specific true sentence
    /// first: the "clause absent" and "operand references nothing in a record of this file" rows are mutually
    /// exclusive by construction, and every operand rule below them tests <c>Item</c>, which both of those leave
    /// null.</summary>
    private static readonly FileControlKeyRule[] Rules =
    [
        // ── Format 1 (indexed) — the prime RECORD KEY ────────────────────────────────────────────────────────
        // ⚠ THE ONLY ROW WHOSE RULE IS A GENERAL FORMAT, so it has no numbered ordinal and no sentence of its
        // own: §12.4.5.1's Format 1 diagram prints `RECORD KEY IS …` with NO bracket around it, and the figure's
        // own note says every stacked group in it is a plain brace or a plain bracket — so an unbracketed clause
        // is a required member of the indexed entry. The quoted span is that note (a sentence the normalizer can
        // match); the DIAGRAM half is asserted separately by
        // FileControlKeyRuleDriftTests.IndexedFormat_PrintsTheRecordKeyClauseUnbracketed, because a markup-laden
        // syntax diagram is not a sentence and pretending otherwise would make this row's guard vacuous.
        new(null, "12.4.5.1", "ISO §12.4.5.1 Format 1",
            "No choice indicators appear anywhere in this figure",
            FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            f => f.HasFd,
            // ⛔ "NAMED NOTHING" IS NOT "ABSENT". The predicate was `op.Name is null` alone until 2026-09-09,
            // which was safe only while every RECORD KEY clause that parsed also bound a data-name. The declined
            // SOURCE key form (Annex A.3 item 40 → COBOLNET1954, kb/Work PB358) names a record-key-name and no
            // data-name, so a program that WROTE the clause was then told its file has none — a false sentence
            // pointing at a different repair. This rule's subject is the clause's PRESENCE (an unbracketed member
            // of Format 1), so it reads the presence fact.
            (_, op) => op.Name is null && !op.Written,
            (f, op) => $"indexed file '{f.CobolName}' has no {op.ClauseFace} clause (ISO §12.4.5.1 Format 1 — the "
                + "RECORD KEY clause is unbracketed in the indexed format, so it is required for ORGANIZATION "
                + "INDEXED)"),

        new("SR-12.4.5.12.3-2", "12.4.5.12.3", "ISO §12.4.5.12.3 SR2",
            "within a record description entry associated with the file-name specified in this file control entry",
            FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            f => f.HasFd && f.Records.Count > 0,
            (f, op) => op.Name is not null && !RecordLayout.IsInRecordOfFile(f, op.Item),
            (f, op) => $"{op.ClauseFace} '{op.Name}' {(op.Item is null ? "references nothing described in this program" : "references an item outside this file's record descriptions")}; "
                + $"data-name-1 shall reference a data item within a record description entry associated with "
                + $"'{f.SelectName}' (ISO §12.4.5.12.3 SR2)"),

        // SR2's OTHER obligation. The sentence joins a CATEGORY and a LOCATION with "within", and a rule read as
        // one predicate gets one screen: the location half above shipped alone and a `PIC 9(5)` prime key built
        // an index on an operand the standard forbids, silently (kb/Work PB743). Two rows, one rule-id — the
        // inventory row is CONFORMS only when BOTH are live, which is why PB699 left it without a verdict.
        new("SR-12.4.5.12.3-2", "12.4.5.12.3", "ISO §12.4.5.12.3 SR2",
            "shall reference a data item of category alphanumeric or category national",
            FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && !ItemCategory.IsAlphanumericOrNational(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is {ItemCategory.Face(op.Item!)}; data-name-1 shall reference "
                + "a data item of category alphanumeric or category national (ISO §12.4.5.12.3 SR2)"),

        new("SR-12.4.5.12.3-1", "12.4.5.12.3", "ISO §12.4.5.12.3 SR1",
            "Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses",
            FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not be "
                + "(ISO §12.4.5.12.3 SR1)"),

        // ⛔ THE MINIMUM-RECORD-SIZE RULE (kb/Work PB1025). Unenforced until 2026-09-22: a RECORD KEY that a
        // dynamic-length item preceded compiled clean, was sliced at its FIXED-run offset, and a READ … KEY
        // returned a different record. The reach is RecordLayout.KeyWindowOf's — the ONE answer SORT/MERGE's
        // §14.9.40.3 SR6 g) reads too — so a key after a variable-length member is measured at its furthest byte.
        new("SR-12.4.5.12.3-4", "12.4.5.12.3", "ISO §12.4.5.12.3 SR4",
            "data-name-1 and each data-name-2 shall be contained within the first n bytes of the record",
            FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            f => f.RecordSizeVaries,
            (f, op) => BeyondMinimum(f, op.Item) is not null,
            (f, op) => BeyondMinimumMessage(f, op, "n", "ISO §12.4.5.12.3 SR4")),

        // §12.4.5.2 SR8 sentence 1, over the PRIME key clause. Screened on every organization EXCEPT indexed —
        // this is the row the scalar Organization column could not hold. `AnyOrganization & ~Indexed` includes
        // the OMITTED clause, which §12.4.5.10.3 GR6 makes record sequential and which is the shape that hits
        // this in practice: `SELECT F ASSIGN … RECORD KEY IS K.` (kb/Work PB742). A SORT-MERGE file is NOT in the
        // set — a file with no organization cannot be "not an indexed file" in sentence 1's sense; sentence 2's
        // Entry row below is what speaks about it, so the entry hears one true sentence rather than two.
        new("SR-12.4.5.2-8", "12.4.5.2", "ISO §12.4.5.2 SR8",
            "Format 1 shall be specified only for an indexed file",
            FileKinds.AnyOrganization & ~FileKinds.Indexed, FileKeyRole.PrimeRecordKey,
            _ => true,
            (_, op) => op.Name is not null,
            (f, op) => $"file '{f.CobolName}' has a {op.ClauseFace} clause, which appears only in the indexed file "
                + $"control entry (ISO §12.4.5.1 Format 1), but this file is {f.OrganizationFace}; Format 1 shall "
                + "be specified only for an indexed file (ISO §12.4.5.2 SR8)"),

        // ── Format 1 (indexed) — each ALTERNATE RECORD KEY clause ────────────────────────────────────────────
        new("SR-12.4.5.6.3-2", "12.4.5.6.3", "ISO §12.4.5.6.3 SR2",
            "within a record description entry associated with the file-name to which the ALTERNATE RECORD KEY clause is subordinate",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            f => f.HasFd && f.Records.Count > 0,
            (f, op) => !RecordLayout.IsInRecordOfFile(f, op.Item),
            (f, op) => $"{op.ClauseFace} '{op.Name}' {(op.Item is null ? "references nothing described in this program" : "references an item outside this file's record descriptions")}; "
                + $"data-name-1 shall be defined within a record description entry associated with "
                + $"'{f.SelectName}' (ISO §12.4.5.6.3 SR2)"),

        new("SR-12.4.5.6.3-2", "12.4.5.6.3", "ISO §12.4.5.6.3 SR2",
            "shall be defined as a data item of category alphanumeric or national",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && !ItemCategory.IsAlphanumericOrNational(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is {ItemCategory.Face(op.Item!)}; data-name-1 shall be defined "
                + "as a data item of category alphanumeric or national (ISO §12.4.5.6.3 SR2)"),

        new("SR-12.4.5.6.3-1", "12.4.5.6.3", "ISO §12.4.5.6.3 SR1",
            "Data-name-1 and data-name-2 shall not be subject to any OCCURS clauses",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not "
                + "be (ISO §12.4.5.6.3 SR1)"),

        // SR5 — the ALTERNATE RECORD KEY twin of §12.4.5.12.3 SR4 above, one reach reader for both (kb/Work PB1025).
        new("SR-12.4.5.6.3-5", "12.4.5.6.3", "ISO §12.4.5.6.3 SR5",
            "each data-name-1 and data-name-2 shall be contained within the first x bytes of the record",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            f => f.RecordSizeVaries,
            (f, op) => BeyondMinimum(f, op.Item) is not null,
            (f, op) => BeyondMinimumMessage(f, op, "x", "ISO §12.4.5.6.3 SR5")),

        // SR7 — the SUPPRESS WHEN literal-1 (kb/Work PB1072). ONE numbered rule, THREE obligations, three rows with
        // one rule-id: the operand's KIND, its CATEGORY against data-name-1's, and an ALL literal's length. Nothing
        // screened any of them, so SUPPRESS WHEN 5 / B"1" / ALL "AB" and a national literal on an alphanumeric key
        // compiled clean. The operand is read ONCE (DataBinder.ReadSuppressWhen, the literal-position chokepoint),
        // so a constant-name, a symbolic-character and a concatenation expression arrive here as the literal they
        // stand for. A keyword figurative has no category of its own (§8.3.3.6.4 GR1 — it takes the context's), so
        // the category row asks only a literal.
        new("SR-12.4.5.6.3-7", "12.4.5.6.3", "ISO §12.4.5.6.3 SR7",
            "Literal-1 shall be an alphanumeric literal, a national literal, or a figurative constant",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op.SuppressWhen is { } s
                && (s.Form is SuppressWhenForm.NotAlphanumericOrNational || s.Class is LiteralClass.Boolean),
            (_, op) => $"{op.ClauseFace} '{op.Name}': SUPPRESS WHEN {op.SuppressWhen?.Written} is "
                + $"{SuppressWhenFace(op.SuppressWhen)}; literal-1 shall be an alphanumeric literal, a national literal, "
                + "or a figurative constant (ISO §12.4.5.6.3 SR7)"),

        new("SR-12.4.5.6.3-7", "12.4.5.6.3", "ISO §12.4.5.6.3 SR7",
            "shall be of the same category as data-name-1 or data-name-2",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op is { SuppressWhen.Class: LiteralClass.Alphanumeric or LiteralClass.National, Item: { } i }
                && ItemCategory.IsAlphanumericOrNational(i)   // SR2 speaks about any other key
                && (op.SuppressWhen!.Class is LiteralClass.National) != !ItemCategory.IsAlphanumeric(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}': SUPPRESS WHEN {op.SuppressWhen?.Written} is "
                + $"{SuppressWhenFace(op.SuppressWhen)}, but '{op.Name}' is {ItemCategory.Face(op.Item!)}; literal-1 "
                + "shall be of the same category as data-name-1 (ISO §12.4.5.6.3 SR7)"),

        new("SR-12.4.5.6.3-7", "12.4.5.6.3", "ISO §12.4.5.6.3 SR7",
            "If ALL literal is specified, the literal shall be one character long",
            FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, op) => op.SuppressWhen is { Form: SuppressWhenForm.AllLiteral, Characters.Length: not 1 },
            (_, op) => $"{op.ClauseFace} '{op.Name}': SUPPRESS WHEN {op.SuppressWhen?.Written} is an ALL literal "
                + $"{op.SuppressWhen?.Characters.Length} characters long; if ALL literal is specified, the literal "
                + "shall be one character long (ISO §12.4.5.6.3 SR7)"),

        // SR8 sentence 1 over each ALTERNATE RECORD KEY clause. Every written clause violates it (the operand
        // list holds the clauses AS WRITTEN, so there is no absent case to exclude).
        new("SR-12.4.5.2-8", "12.4.5.2", "ISO §12.4.5.2 SR8",
            "Format 1 shall be specified only for an indexed file",
            FileKinds.AnyOrganization & ~FileKinds.Indexed, FileKeyRole.AlternateRecordKey,
            _ => true,
            (_, _) => true,
            (f, op) => $"file '{f.CobolName}' has an {op.ClauseFace} clause, which appears only in the indexed file "
                + $"control entry (ISO §12.4.5.1 Format 1), but this file is {f.OrganizationFace}; Format 1 shall "
                + "be specified only for an indexed file (ISO §12.4.5.2 SR8)"),

        // ── Format 2 (relative) — the RELATIVE KEY clause ────────────────────────────────────────────────────
        // ⚠ THE REQUIREMENT IS §12.4.5.2 SR10, NOT §12.4.5.13. The old site cited "§12.4.5.13 — required for
        // random/dynamic access"; §12.4.5.13 has no syntax rules at all (they are in §12.4.5.13.3, and none of
        // the three requires the clause), and the RELATIVE KEY clause is BRACKETED in the §12.4.5.1 Format 2
        // diagram, so the format does not require it either. SR10 is the one sentence that does (kb/Work PB699).
        new("SR-12.4.5.2-10", "12.4.5.2", "ISO §12.4.5.2 SR10",
            "The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of the ACCESS clause is specified",
            FileKinds.Relative, FileKeyRole.RelativeKey,
            f => f.AccessMode != FileAccessMode.Sequential,
            (_, op) => op.Name is null,
            (f, op) => $"relative file '{f.CobolName}' is ACCESS {f.AccessMode.ToString().ToUpperInvariant()} but "
                + $"has no {op.ClauseFace} clause; the RELATIVE clause shall be specified if the DYNAMIC or RANDOM "
                + "phrase of the ACCESS clause is specified (ISO §12.4.5.2 SR10)"),

        new("SR-12.4.5.13.3-1", "12.4.5.13.3", "ISO §12.4.5.13.3 SR1",
            "Data-name-1 shall not be subject to any OCCURS clauses",
            FileKinds.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (_, op) => op.Item is { } i && RecordLayout.IsSubjectToOccurs(i),
            (_, op) => $"{op.ClauseFace} '{op.Name}' is subject to an OCCURS clause; data-name-1 shall not be "
                + "(ISO §12.4.5.13.3 SR1)"),

        new("SR-12.4.5.13.3-2", "12.4.5.13.3", "ISO §12.4.5.13.3 SR2",
            "shall reference an unsigned integer data item whose description does not contain the picture symbol",
            FileKinds.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (_, op) => op.Item is { } i && i.Pic is not { Category: PicCategory.Numeric, Scale: 0, Signed: false },
            (_, op) => $"{op.ClauseFace} '{op.Name}' shall be an unsigned integer without the symbol 'P' "
                + "(ISO §12.4.5.13.3 SR2)"),

        new("SR-12.4.5.13.3-3", "12.4.5.13.3", "ISO §12.4.5.13.3 SR3",
            "shall not be defined in a record description entry subordinate to the associated file-name",
            FileKinds.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (f, op) => op.Item is { } i && RecordLayout.IsInRecordOfFile(f, i),
            (f, op) => $"{op.ClauseFace} '{op.Name}' shall not be defined within a record description of file "
                + $"'{f.CobolName}' (ISO §12.4.5.13.3 SR3)"),

        // §12.4.5.2 SR9 sentence 1, the twin of SR8 one format over: the RELATIVE KEY clause appears only in
        // §12.4.5.1's Format 2, so writing it on a file that is not relative specifies Format 2 for a file the
        // rule does not admit. `ORGANIZATION IS SEQUENTIAL … RELATIVE KEY IS R` compiled clean and bound a key
        // no path ever read (kb/Work PB742).
        new("SR-12.4.5.2-9", "12.4.5.2", "ISO §12.4.5.2 SR9",
            "Format 2 shall be specified only for a relative file",
            FileKinds.AnyOrganization & ~FileKinds.Relative, FileKeyRole.RelativeKey,
            _ => true,
            (_, op) => op.Name is not null,
            (f, op) => $"file '{f.CobolName}' has a {op.ClauseFace} clause, which appears only in the relative file "
                + $"control entry (ISO §12.4.5.1 Format 2), but this file is {f.OrganizationFace}; Format 2 shall "
                + "be specified only for a relative file (ISO §12.4.5.2 SR9)"),

        // ── The ENTRY — §12.4.5.2 SR8 / SR9's SECOND sentence ───────────────────────────────────────────────
        // ⛔ ONE SENTENCE, PRINTED TWICE. "The associated file description entry shall not be a sort-merge file
        // description entry" closes both SR8 and SR9 verbatim, so both rows quote it and both report under the
        // same code: the violation is not a key clause's, it is the ENTRY's, and the remedy is to describe the
        // file with an FD or to stop writing the format's clauses. It cannot be an operand rule because the
        // format may be specified by the ORGANIZATION clause ALONE — `SD` + `ORGANIZATION IS INDEXED` with no
        // key clause at all is a measured silent accept (kb/Work PB742) — which is why FileKeyRole.Entry exists.
        new("SR-12.4.5.2-8", "12.4.5.2", "ISO §12.4.5.2 SR8",
            "The associated file description entry shall not be a sort-merge file description entry",
            FileKinds.SortMerge, FileKeyRole.Entry,
            SpecifiesIndexedFormat,
            (_, _) => true,
            (f, _) => $"file '{f.CobolName}' is described by a sort-merge file description entry, but its file "
                + $"control entry specifies the indexed format ({IndexedFormatMarker(f)}); the file description "
                + "entry associated with ISO §12.4.5.1 Format 1 shall not be a sort-merge file description entry "
                + "(ISO §12.4.5.2 SR8)",
            DiagnosticCatalog.FileControlFormatOnSortMerge),

        new("SR-12.4.5.2-9", "12.4.5.2", "ISO §12.4.5.2 SR9",
            "The associated file description entry shall not be a sort-merge file description entry",
            FileKinds.SortMerge, FileKeyRole.Entry,
            SpecifiesRelativeFormat,
            (_, _) => true,
            (f, _) => $"file '{f.CobolName}' is described by a sort-merge file description entry, but its file "
                + $"control entry specifies the relative format ({RelativeFormatMarker(f)}); the file description "
                + "entry associated with ISO §12.4.5.1 Format 2 shall not be a sort-merge file description entry "
                + "(ISO §12.4.5.2 SR9)",
            DiagnosticCatalog.FileControlFormatOnSortMerge),
    ];

    // ── Which §12.4.5.1 FORMAT an entry specifies ───────────────────────────────────────────────────────────
    // An entry specifies a format by writing a clause that ONLY that format carries. Read off §12.4.5.1: Format 1
    // (indexed) alone carries `[ORGANIZATION IS] INDEXED`, RECORD KEY, ALTERNATE RECORD KEY and the
    // collating-sequence-clause; Format 2 (relative) alone carries `[ORGANIZATION IS] RELATIVE` and RELATIVE KEY.
    // ⚠ A clause carried by MORE THAN ONE format identifies none — ACCESS MODE IS RANDOM appears in Formats 1 and
    // 2 both, and the standard screens it with the ACCESS clause's own rule (§12.4.5.5.2 SR2, DataBinder), not
    // with SR8/SR9. The collating-sequence-clause is likewise screened at its own resolution site
    // (DataBinder.ResolveFileCollating, COBOLNET1582), which has to guard on the organization anyway.

    // ⛔ THE CLAUSE LIST IS WRITTEN ONCE. Each `…Marker` NAMES the clause that specifies the format — a message
    // that only said "the indexed format" would leave the writer of an `SD` + `RECORD KEY` entry with nothing to
    // edit — and returns null when none is written, which is also the "does this entry specify the format?"
    // answer. A separate boolean predicate beside the marker would be the same list twice, and the second copy is
    // the one that would not learn about the next Format-1 clause.
    private static string? IndexedFormatMarker(FileModel f) =>
        f.Organization is FileOrganization.Indexed ? "ORGANIZATION IS INDEXED"
        : f.RecordKeyName is not null ? "a RECORD KEY clause"
        : f.AlternateKeyNames.Count > 0 ? "an ALTERNATE RECORD KEY clause"
        : f.FileLevelCollating is not null || f.KeyLevelCollating.Count > 0 ? "a COLLATING SEQUENCE clause"
        : null;

    private static string? RelativeFormatMarker(FileModel f) =>
        f.Organization is FileOrganization.Relative ? "ORGANIZATION IS RELATIVE"
        : f.RelativeKeyName is not null ? "a RELATIVE KEY clause"
        : null;

    /// <summary>What a SUPPRESS WHEN operand IS, in the words §12.4.5.6.3 SR7 uses — the phrase its three rows
    /// print.</summary>
    private static string SuppressWhenFace(SuppressWhenOperand? s) => s switch
    {
        { Form: SuppressWhenForm.NotAlphanumericOrNational } =>
            "neither an alphanumeric nor a national literal nor a character figurative constant",
        { Form: SuppressWhenForm.Figurative } => "a figurative constant",
        { Form: SuppressWhenForm.AllLiteral } => $"an ALL {ClassFace(s.Class)} literal",
        _ => $"{(s?.Class is LiteralClass.Alphanumeric ? "an" : "a")} {ClassFace(s?.Class)} literal",
    };

    private static string ClassFace(LiteralClass? c) => c switch
    {
        LiteralClass.National => "national",
        LiteralClass.Boolean => "boolean",
        _ => "alphanumeric",
    };

    private static bool SpecifiesIndexedFormat(FileModel f) => IndexedFormatMarker(f) is not null;

    /// <summary>The key's window when it reaches past the file's minimum record size (§12.4.5.12.3 SR4 /
    /// §12.4.5.6.3 SR5 — "contained within the first n bytes of the record, where n equals the minimum record size
    /// specified for the file"; §13.18.43.4 GR9 supplies the minimum a RECORD clause leaves unstated), else null.
    /// A key outside this file's records is SR2's to report, and is not measured here.</summary>
    private static RecordLayout.KeyWindow? BeyondMinimum(FileModel f, DataItem? key)
    {
        if (key is null || !RecordLayout.IsInRecordOfFile(f, key)) return null;
        var root = key;
        while (root.Parent is { } p) root = p;
        return RecordLayout.KeyWindowOf(root, key, key.ByteWidth) is { } w && w.MaxEnd > f.VaryMin ? w : null;
    }

    private static string BeyondMinimumMessage(FileModel f, FileKeyOperand op, string n, string citation)
    {
        string where = BeyondMinimum(f, op.Item) switch
        {
            { FollowsVariable: true } w => $"follows a variable-length member of its record and can reach byte {w.MaxEnd}",
            { } w => $"occupies bytes {w.Offset + 1}..{w.MaxEnd} of the record",
            null => "is not contained within the minimum record size",
        };
        return $"{op.ClauseFace} '{op.Name}' {where}, but indexed file '{f.CobolName}' contains variable-length "
            + $"records with minimum size {f.VaryMin}; the key shall be contained within the first {n} bytes of the "
            + $"record, where {n} equals the minimum record size ({citation})";
    }

    private static bool SpecifiesRelativeFormat(FileModel f) => RelativeFormatMarker(f) is not null;

    /// <summary>The table, for <c>FileControlKeyRuleDriftTests</c> — the only reason it is not private.</summary>
    internal static IReadOnlyList<FileControlKeyRule> Catalog => Rules;

    /// <summary>Screen one file control entry against every rule of its organization. Called once per file from
    /// <c>DataBinder.ResolveFiles</c>, so a file is reported once however many statements name it — the
    /// one-report-per-file property the old per-verb memo (<c>_keyedCheckedFiles</c>) existed to provide, now a
    /// consequence of WHERE the screen runs rather than a set the screen has to carry.</summary>
    /// <param name="refusedOperands">Key operand names already REFUSED and reported, for which no rule here may
    /// speak again: a §8.4.2.2.3 SR1 ambiguity (kb/Work PB978 — two items ARE described), and an operand the
    /// capture refused for its written SHAPE (a subscript or reference-modifier, COBOLNET2024 — kb/Work PB481),
    /// recorded as written and so naming no item. Either has no item, and SR2's "references nothing described"
    /// would be a false second verdict about it.</param>
    public static void Screen(FileModel file, EditionContext edition, IReadOnlySet<string>? refusedOperands = null)
    {
        // ⛔ A SORT-MERGE FILE IS SCREENED, and the rules that speak about it say so in their own ScreenedOn set.
        // This method used to open with `if (file.IsSortMerge) return;` and a comment observing that an SD whose
        // SELECT writes ORGANIZATION INDEXED breaks §12.4.5.2 SR8 — a rule the table did not then carry. It does
        // now, so a global veto here would silence exactly the two rows written for the case (kb/Work PB742).
        // The kind is computed ONCE per entry; every row's set is tested against it.
        var kind = KindOf(file);
        // Role-outer: each role's operands are enumerated ONCE for all of its rules, so a file control entry
        // costs one iterator per role rather than one per rule.
        foreach (var role in Roles)
        {
            if (!HasApplicableRule(file, kind, role)) continue;   // a relative entry never enumerates a RECORD KEY
            foreach (var op in Operands(file, role))
            {
                if (op is { Item: null, Name: { } refused } && refusedOperands?.Contains(refused) == true) continue;
                foreach (var rule in Rules)
                {
                    if (rule.Role != role || (rule.ScreenedOn & kind) == 0
                        || !rule.Applies(file) || !rule.Violated(file, op)) continue;
                    using var _ = edition.At(op.At.IsSet ? op.At : file.EntryAt);
                    edition.Error(rule.Diagnostic, rule.Message(file, op));
                }
            }
        }
    }

    /// <summary>The one <see cref="FileKinds"/> member this entry IS. A file described by a sort-merge file
    /// description entry is <see cref="FileKinds.SortMerge"/> whatever its ORGANIZATION clause said: §12.4.5.1
    /// Format 4 gives it no organization to be, and every rule that cares about the clause it nonetheless wrote
    /// reads it from the <see cref="FileModel"/> directly.</summary>
    private static FileKinds KindOf(FileModel file) =>
        file.IsSortMerge ? FileKinds.SortMerge : (FileKinds)(1 << (int)file.Organization);

    /// <summary>Whether any rule of <paramref name="role"/> is in force for this entry — the file's kind and the
    /// entry-level precondition, both cheap, tested before an operand is materialized.</summary>
    private static bool HasApplicableRule(FileModel file, FileKinds kind, FileKeyRole role)
    {
        foreach (var rule in Rules)
            if (rule.Role == role && (rule.ScreenedOn & kind) != 0 && rule.Applies(file)) return true;
        return false;
    }

    /// <summary>The roles, in the order a program hears about them — the §12.4.5.1 format order (prime key,
    /// alternate keys, relative key), with the ENTRY last because its rule is about what the others add up to.</summary>
    private static readonly FileKeyRole[] Roles =
        [FileKeyRole.PrimeRecordKey, FileKeyRole.AlternateRecordKey, FileKeyRole.RelativeKey, FileKeyRole.Entry];

    /// <summary>The operands a role contributes to the entry. The prime and relative roles always contribute
    /// EXACTLY ONE — with a null name when the clause is absent, which is what lets an absence rule and an
    /// operand rule be the same kind of row.</summary>
    private static IEnumerable<FileKeyOperand> Operands(FileModel file, FileKeyRole role)
    {
        switch (role)
        {
            case FileKeyRole.PrimeRecordKey:
                // `Written` is the entry's own record of whether the clause appeared, NOT `Name is not null`:
                // the declined SOURCE key form writes the clause and binds no data-name (kb/Work PB358).
                yield return new FileKeyOperand(role, "RECORD KEY", file.RecordKeyName, file.RecordKeyItem,
                    file.RecordKeyAt, file.RecordKeyClauseWritten);
                break;
            case FileKeyRole.AlternateRecordKey:
                // The clauses AS WRITTEN, not FileModel.AlternateKeys: a clause whose data-name-1 resolved to
                // nothing is absent from the resolved list, and that is precisely the case SR2 speaks about.
                foreach (var alt in file.AlternateKeyNames)
                    yield return new FileKeyOperand(role, "ALTERNATE RECORD KEY", alt.Name, alt.Item, alt.At,
                        SuppressWhen: alt.SuppressWhen);
                break;
            case FileKeyRole.RelativeKey:
                yield return new FileKeyOperand(role, "RELATIVE KEY", file.RelativeKeyName, file.RelativeKeyItem,
                    file.RelativeKeyAt);
                break;
            case FileKeyRole.Entry:
                // Exactly one, with no name and no item: the ENTRY is the subject, and the rows on this role
                // read the FileModel. The cursor is the entry's own, which is where a rule about the entry's
                // FORMAT has to report — no single clause is the violation.
                yield return new FileKeyOperand(role, "file control entry", null, null, file.EntryAt);
                break;
        }
    }
}
