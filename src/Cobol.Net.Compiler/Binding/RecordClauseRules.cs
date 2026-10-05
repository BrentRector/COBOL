// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>What a §13.18.43.3 syntax rule is stated ABOUT — the axis the subclause's rules divide on, and the
/// reason this is an enum rather than a boolean: adding the next kind of subject (SR6's DEPENDING operand) is a
/// member and a row, not a second dispatch.</summary>
internal enum RecordRuleSubject
{
    /// <summary>The RECORD clause itself and the integers it states — SR5 and SR9, which relate integer-3 to
    /// integer-2 and integer-5 to integer-4 and need nothing outside the clause.</summary>
    Clause,

    /// <summary>ONE record description entry associated with the file description entry — SR3 and SR4, which
    /// compare a record's §13.18.43.4 GR8 byte count against the clause's bounds.</summary>
    RecordDescription,

    /// <summary>The RESOLVED data-name-1 of the Format 2 DEPENDING phrase — SR6's three obligations (elementary,
    /// unsigned integer, described in the working-storage, local-storage or linkage section; kb/Work PB858).</summary>
    DependingOperand,
}

/// <summary>ONE subject of a §13.18.43.3 syntax rule, as the screen sees it: the clause alone
/// (<paramref name="Record"/> null, the sizes meaningless), one record description entry with its
/// §13.18.43.4 GR8 a) / GR8 b) byte counts already computed, or the DEPENDING phrase's data-name-1.</summary>
/// <param name="Record">The record description entry, or null when the subject is not a record description.</param>
/// <param name="MinBytes">GR8 a) — the sum with every occurs-depending table at its MINIMUM occurrences.</param>
/// <param name="MaxBytes">GR8 b) — the same sum with every such table at its MAXIMUM occurrences.</param>
/// <param name="Depending">The resolved data-name-1 of the DEPENDING phrase, for a
/// <see cref="RecordRuleSubject.DependingOperand"/> row; null for every other subject.</param>
internal readonly record struct RecordClauseSubject(DataItem? Record, int MinBytes, int MaxBytes, DataItem? Depending = null)
{
    /// <summary>How a diagnostic names this record description: its 01 record-name, or the fact that it is the
    /// §14.9.30.4 GR6 IMPLIED entry, which has no name because the program never wrote one.</summary>
    public string Face => Record?.CobolName is { } n ? $"record '{n}'" : "the implied record description entry";

    /// <summary>The byte count as ONE number when the record has no variable-occurrence content, or the range
    /// when it has — so the message states the quantity the rule compared rather than a bare number the reader
    /// cannot find in the source.</summary>
    public string SizeFace => MinBytes == MaxBytes ? $"{MinBytes} bytes" : $"{MinBytes} to {MaxBytes} bytes";
}

/// <summary>ONE syntax rule of the RECORD clause (ISO §13.18.43.3).</summary>
/// <param name="RuleId">The traceability-inventory row this rule closes (<c>SR-13.18.43.3-5</c>). The drift test
/// keys on this, and a rule stating TWO obligations contributes TWO rows carrying the SAME id.</param>
/// <param name="Clause">The clause number, exactly as <c>scripts/spec/cite.py --check</c> takes it.</param>
/// <param name="Citation">The citation as it appears in the shipped diagnostic — clause plus printed ordinal.</param>
/// <param name="RuleText">A verbatim span of the PRINTED rule, long enough to identify it.
/// <c>RecordClauseRuleDriftTests</c> asserts this span is inside <paramref name="Clause"/>'s own region of
/// <c>specs/ISO_COBOL.md</c> AND inside the printed ordinal <paramref name="RuleId"/> names, so an inherited or
/// drifted clause number turns a test red instead of shipping (CLAUDE.md rule 1).</param>
/// <param name="Format">The §13.18.43.2 general format the rule is printed under. §13.18.43.3 heads its rules
/// FORMAT 1 / FORMAT 2 / FORMAT 3, so this column is the standard's own, not an invention — and it is a SCALAR
/// here, unlike <c>FileControlKeyRules.ScreenedOn</c>, because every rule in this subclause is stated inside the
/// format it screens; the subclause states no rule about a clause written where its format does not apply.</param>
/// <param name="Subject">What the rule speaks about (see <see cref="RecordRuleSubject"/>).</param>
/// <param name="Violated">Whether this subject breaks the rule.</param>
/// <param name="Message">The diagnostic body — what was written, then the rule, then the citation.</param>
/// <param name="Code">The diagnostic this row reports under: the clause's own integers and a record
/// description's size are different subjects with different remedies, so they carry different codes.</param>
internal sealed record RecordClauseRule(
    string RuleId,
    string Clause,
    string Citation,
    string RuleText,
    RecordClauseFormat Format,
    RecordRuleSubject Subject,
    Func<RecordClauseFacts, RecordClauseSubject, bool> Violated,
    Func<FileModel, RecordClauseFacts, RecordClauseSubject, string> Message,
    DiagnosticDescriptor Code);

/// <summary>
/// ⛔ THE ONE SCREEN FOR THE RECORD CLAUSE'S SIZE SYNTAX RULES (ISO/IEC 1989:2023 §13.18.43.3), run from
/// <c>DataBinder.ResolveFiles</c> over EVERY declared file once the data forest is indexed — the sibling of
/// <see cref="FileControlKeyRules"/>, at the same site, in the same shape (kb/Work PB721).
///
/// <para>WHY IT EXISTS. <c>DataBinder.BindRecordClause</c> parsed the clause's integers and stored them, and
/// NOTHING checked them. Six of the subclause's nine syntax rules had no enforcement site anywhere in the
/// compiler: <c>FD F RECORD IS VARYING IN SIZE FROM 20 TO 5 DEPENDING ON L.</c> — an SR5 violation written in
/// two adjacent literals — compiled clean at every edition, and the program learned about it only as an I-O
/// status '44' on the first WRITE, which is §13.18.43.4 GR14 a)'s runtime CONSEQUENCE and not the clause's
/// diagnosis. The same held for a record description that does not fit the clause it is written under
/// (SR3/SR4). Those are syntax rules of the ENTRY: the entry violates them whether or not any statement
/// references the file, exactly as PB699 established for the file control entry's key clauses.</para>
///
/// <para>WHY POST-BUILD, AND NOT AT <c>BindRecordClause</c>. SR3 and SR4 compare the clause's integers against
/// §13.18.43.4 GR8's byte counts of the file's RECORD DESCRIPTION ENTRIES, and GR8's summation reads occurrence
/// counts, group usage, redefinition targets and the §8.5.1.6.3 bit layout — facts that are settled only once
/// the whole forest is bound and indexed. The clause loop runs while the file description entry is still being
/// read. The source POSITION the rules report at is captured there instead
/// (<see cref="FileModel.RecordClauseAt"/>), which is the <c>RecordKeyAt</c> pattern.</para>
///
/// <para>WHY A TABLE. §13.18.43.3 states the SAME rule three times over different operands — SR3 bounds a record
/// description by integer-1, SR4 bounds it by integer-2/integer-3, SR5 and SR9 are one sentence printed once per
/// variable-length format — and a set of rules with one member written down as an <c>if</c> is exactly the shape
/// in which the missing members hide (kb/Work PB354, PB699, PB742). One row per obligation, one loop, one report
/// site: implementing the next rule of this subclause is adding a row, and <c>RecordClauseRuleDriftTests</c>
/// re-derives every row's RULE TEXT from <c>specs/ISO_COBOL.md</c> so a row can neither carry an inherited
/// clause number nor go stale.</para>
///
/// <para>⛔ SR4 IS ONE SENTENCE WITH TWO OBLIGATIONS AND THEREFORE TWO ROWS ("neither … a lesser number of bytes
/// than that specified by integer-2 nor … a greater number of bytes than that specified by integer-3"). This is
/// the PB743 clamp applied before the defect rather than after it: a rule read as one predicate gets one screen,
/// and the arm that ships alone is silent forever. The drift test re-derives the split from the printed rule.</para>
///
/// <para>EDITIONS. Every rule here is present in all four supported editions (85 · 2002 · 2014 · 2023): the
/// RECORD clause and its syntax rules predate COBOL-85, and Annex E — the complete 2014→2023 delta — carries no
/// entry for this subclause. So the table needs no edition column and the negatives reject at every edition.</para>
///
/// <para>⚖ NOT HERE, AND WHY, rule by rule, so the absences are decisions rather than oversights:</para>
/// <list type="bullet">
/// <item><b>SR1</b> ("If no record description entries are specified … the RECORD clause shall be specified") is
/// about the clause's PRESENCE, is decidable while the entry is read, and is screened inline in
/// <c>DataBinder.MaterializeImpliedRecord</c> (COBOLNET1836) — the same "needs nothing resolved, reports as the
/// entry is read" criterion that keeps §12.4.5.5.2 SR2 out of <see cref="FileControlKeyRules"/>.</item>
/// <item><b>SR2</b> ("The words BYTES and CHARACTERS are synonymous and may be used interchangeably") is a
/// LEXICAL permission, not a check: it is discharged by the grammar admitting both words in
/// <c>CobolData.g4 recordClause</c>, with BYTES carried as the §8.10 context-sensitive word it is.</item>
/// <item><b>SR7 and SR8</b> ("Integer-2 / integer-4 shall be greater than or equal to zero") are discharged by
/// the GENERAL FORMAT, and a row asserting them would be a dead lookup. §5.5 1) makes every <c>integer-n</c> "a
/// fixed-point integer literal that shall be unsigned and nonzero unless otherwise specified in the associated
/// rules", the format writes integer-2 and integer-4 as the grammar's unsigned <c>integerLiteral</c>, so a
/// negative operand cannot be written; SR7/SR8 are the express override of the NONZERO half, and what they
/// require of this compiler is that <c>FROM 0</c> be ACCEPTED — which is a positive golden, not a screen.</item>
/// </list>
/// </summary>
internal static class RecordClauseRules
{
    /// <summary>Every RECORD clause size syntax rule this compiler screens. ⛔ Order is significant only in that
    /// a clause-level row is stated before the record-level rows it can make vacuous: an inverted range (SR5/SR9)
    /// is reported once about the clause, and the record rows below still fire independently, because a record
    /// that also fails SR4 fails it against a bound the program wrote and can act on.</summary>
    /// <summary>The whole of §13.18.43.3 SR4, as the standard prints it — quoted ONCE and shipped by BOTH of
    /// its rows. The rule has two OBLIGATIONS and therefore two rows, but it is ONE sentence and the reader
    /// needs all of it to see which half was broken; the same sentence spelled into two message expressions
    /// is one rule written down twice, and the second copy is the one that would not be corrected.</summary>
    private const string Sr4Sentence =
        "record descriptions for the file shall describe neither records that contain a lesser number of "
        + "bytes than that specified by integer-2 nor records that contain a greater number of bytes than "
        + "that specified by integer-3 (ISO §13.18.43.3 SR4)";

    /// <summary>The whole of §13.18.43.3 SR6, shipped by its three rows for the reason <see cref="Sr4Sentence"/>
    /// is shipped by SR4's two.</summary>
    private const string Sr6Sentence =
        "data-name-1 shall describe an elementary unsigned integer in the working-storage, local-storage, or "
        + "linkage section (ISO §13.18.43.3 SR6)";

    private static readonly RecordClauseRule[] Rules =
    [
        // ── FORMAT 1 (fixed-length) ──────────────────────────────────────────────────────────────────────────
        // SR3 compares a record's MAXIMUM (GR8 b / GR3 "plus the sum of the maximum number of bytes in any
        // occurs-depending table subordinate to the record"): a record description with an ODO table SPECIFIES
        // its largest size, so it is that size the rule bounds.
        new("SR-13.18.43.3-3", "13.18.43.3", "ISO §13.18.43.3 SR3",
            "No record description entry for the file may specify a number of bytes greater than integer-1",
            RecordClauseFormat.Fixed, RecordRuleSubject.RecordDescription,
            (c, s) => c.Upper is { } n && s.MaxBytes > n,
            (f, c, s) => $"{f.EntryFace} '{f.CobolName}': {s.Face} describes {s.SizeFace}, but the RECORD "
                + $"clause states {c.UpperName} = {c.Upper}; no record description entry for the file may specify "
                + "a number of bytes greater than integer-1 (ISO §13.18.43.3 SR3)",
            DiagnosticCatalog.RecordClauseDescriptionSize),

        // ── FORMAT 2 (variable-length) ───────────────────────────────────────────────────────────────────────
        // SR5 first, so a program with BOTH an inverted range and records outside it hears the clause's own
        // inconsistency as well as the record's. Equal bounds violate it: the standard says GREATER.
        new("SR-13.18.43.3-5", "13.18.43.3", "ISO §13.18.43.3 SR5",
            "Integer-3 shall be greater than integer-2",
            RecordClauseFormat.Varying, RecordRuleSubject.Clause,
            (c, _) => c is { Lower: { } lo, Upper: { } hi } && hi <= lo,
            (f, c, _) => $"{f.EntryFace} '{f.CobolName}': the RECORD IS VARYING IN SIZE clause states "
                + $"FROM {c.Lower} TO {c.Upper}; {c.UpperName} shall be greater than {c.LowerName} "
                + "(ISO §13.18.43.3 SR5)",
            DiagnosticCatalog.FileClauseSizeRange),

        // SR4, arm 1 of 2 — "neither records that contain a LESSER number of bytes than that specified by
        // integer-2". A record description's SMALLEST size is GR8 a)'s: every occurs-depending table at its
        // minimum occurrence count, which is the smallest record the description describes.
        new("SR-13.18.43.3-4", "13.18.43.3", "ISO §13.18.43.3 SR4",
            "shall describe neither records that contain a lesser number of bytes than that specified by integer-2",
            RecordClauseFormat.Varying, RecordRuleSubject.RecordDescription,
            (c, s) => c.Lower is { } lo && s.MinBytes < lo,
            (f, c, s) => $"{f.EntryFace} '{f.CobolName}': {s.Face} describes {s.SizeFace}, which is fewer "
                + $"than the {c.LowerName} = {c.Lower} its RECORD IS VARYING IN SIZE clause states; "
                + Sr4Sentence,
            DiagnosticCatalog.RecordClauseDescriptionSize),

        // SR4, arm 2 of 2 — "nor records that contain a GREATER number of bytes than that specified by
        // integer-3", against GR8 b)'s maximum.
        new("SR-13.18.43.3-4", "13.18.43.3", "ISO §13.18.43.3 SR4",
            "nor records that contain a greater number of bytes than that specified by integer-3",
            RecordClauseFormat.Varying, RecordRuleSubject.RecordDescription,
            (c, s) => c.Upper is { } hi && s.MaxBytes > hi,
            (f, c, s) => $"{f.EntryFace} '{f.CobolName}': {s.Face} describes {s.SizeFace}, which is more "
                + $"than the {c.UpperName} = {c.Upper} its RECORD IS VARYING IN SIZE clause states; "
                + Sr4Sentence,
            DiagnosticCatalog.RecordClauseDescriptionSize),

        // SR6 — "Data-name-1 shall describe an elementary unsigned integer in the working-storage, local-storage, or
        // linkage section" (kb/Work PB858). ⛔ THREE OBLIGATIONS, THREE ROWS, for the reason SR4 is two: a rule read
        // as one predicate ships with an arm silent. The integer arm asks only of an elementary item (a group has no
        // numeric description to judge, and the elementary row has already named it). "Integer" is §5.5 2) b) 2.'s
        // "a fixed-point numeric data item … whose description does not include any digit positions to the right of
        // the radix point" — PicInfo.IsIntegerDescription, so a trailing-P PIC 99P (§13.18.40.4 GR14) IS one; where
        // the standard bars the symbol P it says so (§12.4.5.13.3 SR2). "Unsigned" is no S.
        // The section is DataItem.Section (kb/Work PB1080), the fact §12.4.5.8.3 SR2's FILE STATUS screen reads.
        new("SR-13.18.43.3-6", "13.18.43.3", "ISO §13.18.43.3 SR6",
            "Data-name-1 shall describe an elementary",
            RecordClauseFormat.Varying, RecordRuleSubject.DependingOperand,
            (_, s) => s.Depending is { IsElementary: false },
            (f, _, s) => $"{f.EntryFace} '{f.CobolName}': the RECORD clause's DEPENDING ON '{s.Depending?.CobolName}' "
                + $"is a group item; {Sr6Sentence}",
            DiagnosticCatalog.RecordDependingOperand),

        new("SR-13.18.43.3-6", "13.18.43.3", "ISO §13.18.43.3 SR6",
            "elementary unsigned integer",
            RecordClauseFormat.Varying, RecordRuleSubject.DependingOperand,
            (_, s) => s.Depending is { IsElementary: true } d && d.Pic is not { IsIntegerDescription: true, Signed: false },
            (f, _, s) => $"{f.EntryFace} '{f.CobolName}': the RECORD clause's DEPENDING ON '{s.Depending?.CobolName}' "
                + $"is not an unsigned integer; {Sr6Sentence}",
            DiagnosticCatalog.RecordDependingOperand),

        new("SR-13.18.43.3-6", "13.18.43.3", "ISO §13.18.43.3 SR6",
            "in the working-storage, local-storage, or linkage section",
            RecordClauseFormat.Varying, RecordRuleSubject.DependingOperand,
            (_, s) => s.Depending?.Section is { } sec
                && sec is not (EntrySection.WorkingStorage or EntrySection.LocalStorage or EntrySection.Linkage),
            (f, _, s) => $"{f.EntryFace} '{f.CobolName}': the RECORD clause's DEPENDING ON '{s.Depending?.CobolName}' "
                + $"is described in the {(s.Depending?.Section is { } sec ? DataBinder.SectionWords(sec) : "file section")}; "
                + Sr6Sentence,
            DiagnosticCatalog.RecordDependingOperand),

        // ── FORMAT 3 (fixed-or-variable-length) ──────────────────────────────────────────────────────────────
        // ⛔ SR9 IS SR5 ONE FORMAT OVER, AND FORMAT 3 HAS NO SR4-ANALOGUE — deliberately, not by omission:
        // §13.18.43.4 GR18 says that for this format "the size of each record is completely defined in the
        // record description entry", so the standard states no rule bounding the descriptions by integer-4 and
        // integer-5. Inventing one here would reject legal source.
        new("SR-13.18.43.3-9", "13.18.43.3", "ISO §13.18.43.3 SR9",
            "Integer-5 shall be greater than integer-4",
            RecordClauseFormat.FixedOrVariable, RecordRuleSubject.Clause,
            (c, _) => c is { Lower: { } lo, Upper: { } hi } && hi <= lo,
            (f, c, _) => $"{f.EntryFace} '{f.CobolName}': the RECORD CONTAINS clause states "
                + $"{c.Lower} TO {c.Upper}; {c.UpperName} shall be greater than {c.LowerName} "
                + "(ISO §13.18.43.3 SR9)",
            DiagnosticCatalog.FileClauseSizeRange),
    ];

    /// <summary>The table, for <c>RecordClauseRuleDriftTests</c> — the only reason it is not private.</summary>
    internal static IReadOnlyList<RecordClauseRule> Catalog => Rules;

    /// <summary>Screen one entry's RECORD clause against every rule of its general format. Called once per
    /// declared file from <c>DataBinder.ResolveFiles</c>, so a file is reported once however many statements
    /// name it — and it covers the SORT-MERGE file description entry as well as the file description entry,
    /// because §13.4.6's format carries the same RECORD clause and <c>BindRecordClause</c> is one method with two
    /// callers. Measured, not assumed: <c>SD S RECORD IS VARYING IN SIZE FROM 20 TO 5.</c> reports both codes,
    /// and <see cref="FileModel.EntryFace"/> is what names the entry the reader actually wrote.</summary>
    public static void Screen(FileModel file, EditionContext edition)
    {
        // No RECORD clause ⇒ §13.18.43.4 GR5 implies one FROM the record descriptions, and an implied clause
        // cannot contradict the descriptions it was derived from. Nothing in this subclause speaks about it.
        if (file.RecordClause is not { } clause) return;
        using var _ = edition.At(clause.At.IsSet ? clause.At : file.EntryAt);

        // Subject-outer: the record descriptions are enumerated ONCE for all of the record-level rules.
        foreach (var rule in Rules)
        {
            if (rule.Format != clause.Format || rule.Subject != RecordRuleSubject.Clause) continue;
            var subject = default(RecordClauseSubject);
            if (rule.Violated(clause, subject)) edition.Error(rule.Code, rule.Message(file, clause, subject));
        }
        // data-name-1 of the DEPENDING phrase, RESOLVED by DataBinder.ResolveFiles before this screen runs (null when
        // the phrase is absent or its resolution failed and was reported there), reported at the phrase.
        if (file.VaryingDependingItem is { } depending)
        {
            using var at = edition.At(file.Varying!.DependingAt.IsSet ? file.Varying.DependingAt : file.EntryAt);
            var subject = new RecordClauseSubject(null, 0, 0, depending);
            foreach (var rule in Rules)
            {
                if (rule.Format != clause.Format || rule.Subject != RecordRuleSubject.DependingOperand) continue;
                if (rule.Violated(clause, subject)) edition.Error(rule.Code, rule.Message(file, clause, subject));
            }
        }
        if (!Rules.Any(r => r.Format == clause.Format && r.Subject == RecordRuleSubject.RecordDescription)) return;
        foreach (var record in file.Records)
        {
            // ⛔ EVERY record description is a subject, a variable-length one included (kb/Work PB1562). GR8 b) sums
            // the MAXIMUM, and §8.5.1.10.1 gives a dynamic-length item one — "the smallest of" its LIMIT phrase, the
            // largest integer storable in its prefixed usage and the implementor maximum (DOC-A.1-62) — so a record
            // holding one has a maximum byte count like a record holding a table (FileModel.MaxRecordSize already
            // computes it, and the implied clause of PB1276 reads it), and its minimum is the item at zero length
            // (FileModel.MinRecordSize). Skipping these records made SR3 and SR4 vacuous for exactly the descriptions
            // that can overflow them: `RECORD CONTAINS 20` over two unbounded dynamic members compiled clean and its
            // records read back mis-split. (A dynamic-capacity table cannot be in a record at all — COBOLNET1526,
            // §8.5.1.9.1 3) — so it needs no exclusion.)
            var subject = new RecordClauseSubject(record, FileModel.MinRecordSize(record), FileModel.MaxRecordSize(record));
            foreach (var rule in Rules)
            {
                if (rule.Format != clause.Format || rule.Subject != RecordRuleSubject.RecordDescription) continue;
                if (rule.Violated(clause, subject)) edition.Error(rule.Code, rule.Message(file, clause, subject));
            }
        }
    }
}
