// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;               // IDiagnosticSink / EditionDiagnostic / EditionSeverity
using CobolNet.Editions.Diagnostics;   // DiagnosticCatalog
using CobolNet.Frontend.Generated;     // CobolParserCore

namespace CobolNet.Validation;

/// <summary>
/// The four arms of ISO §13.18.33.3 — the syntax rules that state, PER DATA DIVISION SECTION, which level-numbers
/// an entry may carry. The arm is a property of the entry's SECTION, never of the entry itself, which is why the
/// screen classifies from the parse-tree ancestry instead of trusting any one binder's local knowledge.
/// </summary>
internal enum LevelNumberArm
{
    /// <summary>§13.18.33.3 SR2 — a data description entry subordinate to an FD or SD entry. Note 77 is NOT in
    /// this set: a noncontiguous item has no meaning in a record area (§13.18.33.4 GR2a).</summary>
    FileRecord,

    /// <summary>§13.18.33.3 SR5 — a data description entry in the WORKING-STORAGE, LOCAL-STORAGE or LINKAGE
    /// section. The only arm that admits 77.</summary>
    NoncontiguousCapable,

    /// <summary>§13.18.33.3 SR4 — a report group description entry subordinate to an RD entry.</summary>
    ReportGroup,

    /// <summary>§13.18.33.3 SR6 — a screen description entry.</summary>
    ScreenItem,
}

/// <summary>
/// Which of the four ISO §13.16.2 general formats a DATA DESCRIPTION entry's BODY is written in. Decided from the
/// BODY alone and never from the level-number, because the level-number is precisely what §13.18.33.4 GR2 and
/// §13.16.3 SR1 constrain — using it as the evidence would make every one of those rules vacuous.
/// </summary>
internal enum EntryBodyForm
{
    /// <summary>The §13.10 constant entry (<c>01 name CONSTANT AS literal</c>) — not a §13.16 data description
    /// entry at all; its level rule is §13.10.2 and lives with the constant bind.</summary>
    Constant,

    /// <summary>§13.16.2 Format 2, the renames format: a <c>RENAMES</c> clause body.</summary>
    Renames,

    /// <summary>§13.16.2 Formats 3 and 4, the condition-name and validation formats: <c>88 [condition-name]
    /// value-clause .</c> — a body whose clauses are ALL value clauses, and at least one.</summary>
    ValueOnly,

    /// <summary>§13.16.2 Format 1, the data-description format: anything else.</summary>
    DataDescription,
}

/// <summary>
/// THE level-number rule, written down ONCE. Two axes constrain a level-number and they are independent:
/// <list type="bullet">
/// <item>the SECTION the entry lives in — ISO §13.18.33.3 SR2 / SR4 / SR5 / SR6, four different permitted sets;</item>
/// <item>the general FORMAT the entry is written in — §13.18.33.4 GR2b/GR2c ("may be used only as described by …")
/// and §13.16.3 SR1 ("Level-number may be 77 or 1 through 49" for a format-1 entry), plus §13.16.3 SR2's
/// obligation that a 77 entry name itself.</item>
/// </list>
/// Four grammar rules spell a level-number (<c>dataDescriptionEntry</c>, <c>reportGroupEntry</c>, the report
/// section's <c>constantEntry</c>, <c>screenDescriptionEntry</c>) and the sets they must satisfy differ; a per-binder
/// copy of the test would be one copy per rule, and the binders historically carried ZERO (kb/Work PB485).
/// </summary>
internal static class LevelNumberRules
{
    /// <summary>One SECTION arm: the verbatim §13.18.33.3 requirement sentence, its rule number, and the SPECIAL
    /// level-numbers it admits on top of the universal 1–49 range (§13.18.33.1).</summary>
    /// <param name="SyntaxRule">The §13.18.33.3 syntax-rule number ("2", "4", "5", "6").</param>
    /// <param name="Requirement">The rule sentence, verbatim from §13.18.33.3 but for its leading capital — the
    /// diagnostic quotes the standard rather than paraphrasing it.</param>
    /// <param name="Specials">The special level-numbers this arm admits (§13.18.33.4 GR2). 1–49 is universal.</param>
    internal readonly record struct Arm(string SyntaxRule, string Requirement, int[] Specials)
    {
        /// <summary>§13.18.33.1: "Level numbers 1 through 49 indicate the position of a data item or screen item
        /// within the hierarchical structure … In addition, level numbers 66, 77, and 88 are used to identify
        /// special entries." The 1–49 range is common to every arm; <see cref="Specials"/> is the difference.</summary>
        internal bool Permits(int level) => level is >= 1 and <= 49 || Array.IndexOf(Specials, level) >= 0;
    }

    /// <summary>The §13.18.33.3 table. §13.18.33.3 SR3 ("A level-number in the range of 1 through 9 may be
    /// specified as 01 through 09") needs no row: it is a SPELLING permission, honoured by the VALUE screen, and the
    /// two-digit bound on the spelling (§8.3.2.2.13) is <see cref="LevelNumberPass.VisitLevelNumber"/>'s length test.</summary>
    private static readonly Dictionary<LevelNumberArm, Arm> Table = new()
    {
        [LevelNumberArm.FileRecord] = new("2",
            "data description entries subordinate to a FD or SD entry shall have level-numbers with the values "
            + "66, 88, or 1 through 49", [66, 88]),
        [LevelNumberArm.NoncontiguousCapable] = new("5",
            "data description entries in the working-storage section, local-storage section, and linkage section "
            + "shall have level-numbers 66, 77, 88, or 1 through 49", [66, 77, 88]),
        [LevelNumberArm.ReportGroup] = new("4",
            "report group description entries that are subordinate to an RD entry shall have level-numbers with "
            + "the values 1 through 49", []),
        [LevelNumberArm.ScreenItem] = new("6",
            "screen description entries shall have level-numbers 1 through 49", []),
    };

    /// <summary>The rule for <paramref name="arm"/>.</summary>
    internal static Arm For(LevelNumberArm arm) => Table[arm];

    /// <summary>Which §13.18.33.3 arm governs this <c>levelNumber</c> node — decided by the entry rule that spells
    /// it and, for a data description entry, by the SECTION that contains the entry. A <see langword="null"/>
    /// result means the grammar grew an arm this table does not know; the screen then stays silent (never
    /// over-rejects) and <c>LevelNumberArmDriftTests</c> fails, which is the mechanism that keeps this current.</summary>
    internal static LevelNumberArm? Classify(CobolParserCore.LevelNumberContext ctx) => ctx.Parent switch
    {
        CobolParserCore.DataDescriptionEntryContext e => e.Parent switch
        {
            CobolParserCore.FileDescriptionEntryContext
                or CobolParserCore.SortMergeDescriptionEntryContext => LevelNumberArm.FileRecord,
            CobolParserCore.WorkingStorageSectionContext
                or CobolParserCore.LocalStorageSectionContext
                or CobolParserCore.LinkageSectionContext => LevelNumberArm.NoncontiguousCapable,
            _ => null,
        },
        CobolParserCore.ReportGroupEntryContext => LevelNumberArm.ReportGroup,
        // A constant entry standing in the REPORT SECTION (§13.8.2, kb/Work PB1226) is a §13.10 entry, not a
        // report group description entry, so §13.18.33.3 SR4 does not speak about it; its level rule is §13.10.2's
        // `{1 | 01}` and is reported where the constant is bound (DataBinder.BindConstantEntry), exactly as for a
        // constant in any other section (CheckEntryFormat's Constant arm).
        CobolParserCore.ConstantEntryContext => null,
        CobolParserCore.ScreenDescriptionEntryContext => LevelNumberArm.ScreenItem,
        _ => null,
    };

    /// <summary>Which §13.16.2 general format the entry's BODY is written in. Read from the body only — see
    /// <see cref="EntryBodyForm"/> for why the level-number may not participate.</summary>
    internal static EntryBodyForm BodyForm(CobolParserCore.DataDescriptionEntryContext entry)
    {
        var body = entry.dataDescriptionBody();
        if (body?.constantEntryBody() is not null) return EntryBodyForm.Constant;
        if (body?.renamesClause() is not null) return EntryBodyForm.Renames;
        var clauses = body?.dataDescriptionClauses()?.dataDescriptionClause();
        // §13.16.2 Formats 3 and 4 are `88 [condition-name] value-clause .` — a value clause and nothing else.
        // The grammar folds the condition-name, table and content-validation VALUE spellings into one
        // `valueClause` rule, so "every clause is a value clause" is the whole test. An entry with NO clauses is
        // not format 3 or 4 either: both formats require the value-clause.
        return clauses is { Length: > 0 } && Array.TrueForAll(clauses, c => c.valueClause() is not null)
            ? EntryBodyForm.ValueOnly
            : EntryBodyForm.DataDescription;
    }

    /// <summary>Does this entry's VALUE clause carry §13.18.63.2 format 5's <c>{ INVALID | VALID }</c> phrase —
    /// the phrase that makes the entry a §13.16.2 FORMAT 4 (validation) entry rather than a format 3?
    /// <para>It is what decides whether the condition-name may be omitted, because format 4 is the only one of
    /// the two that brackets the name, and format 4's value-clause is format 5 (a required choice per the
    /// printed figure). The A.4.14 validation facility itself is DECLINED and refused by name at bind
    /// (COBOLNET1708) — that is a separate verdict about a construct this compiler does not claim, and it must
    /// not be pre-empted by an entry-FORMAT message that would be simply wrong about it.</para></summary>
    internal static bool HasContentValidationPhrase(CobolParserCore.DataDescriptionEntryContext entry)
    {
        var clauses = entry.dataDescriptionBody()?.dataDescriptionClauses()?.dataDescriptionClause();
        return clauses is not null
            && Array.Exists(clauses, c => c.valueClause()?.validateValidPhrase() is not null);
    }

    /// <summary>The NAME obligation of the §13.16.2 format this entry is written in — the requirement text when
    /// the entry's name slot breaks it, <see langword="null"/> when it does not. The ONE place the rule lives
    /// (kb/Work PB849): every format is read off its printed figure (§13.16.2, PDF p394 rendered), so a new
    /// level-number format is one more arm here, not one more hand-written case in the pass.
    /// <list type="bullet">
    /// <item>Format 1 is <c>level-number [ entry-name-clause ]</c>: the entry-name clause — data-name OR FILLER
    /// (§13.18.20.3 SR3) — and it may be omitted (§13.16.3 SR4). Only level 77 narrows it: §13.16.3 SR2, "The
    /// data-name format of the entry-name clause shall be specified if level-number is 77", and an omitted
    /// entry-name is the filler format by SR4.</item>
    /// <item>Format 2 is <c>66 data-name-1 RENAMES …</c> (also §13.18.45.2) — data-name-1 printed UNBRACKETED and
    /// NOT as an entry-name clause, so neither omission nor FILLER (the entry-name clause's filler format,
    /// §13.18.20.2 format 3, which only format 1 carries) is the format.</item>
    /// <item>Format 3 is <c>88 condition-name-1 value-clause .</c> — the name unbracketed; format 4 is
    /// <c>88 [ condition-name-2 ] value-clause .</c>, whose value-clause is §13.18.63.2 format 5 with its
    /// required <c>{ INVALID | VALID }</c> choice. So a nameless 88 is legal only with that phrase (§13.16.3
    /// SR24, "Format 3 or 4 is used for each condition-name"), and FILLER is never a condition-name in
    /// either.</item>
    /// </list></summary>
    internal static string? EntryNameObligation(CobolParserCore.DataDescriptionEntryContext entry, int level)
    {
        var name = entry.dataName();
        bool isFiller = name is not null && name.FILLER() is not null;
        return level switch
        {
            77 when name is null || isFiller =>
                "the data-name format of the entry-name clause shall be specified if level-number is 77 — a "
                + "noncontiguous item cannot be FILLER, explicitly or by omission (ISO §13.16.3 SR2, SR4)",
            66 when name is null || isFiller =>
                "the renames format of the data description entry is written `66 data-name-1 RENAMES …` and "
                + "names data-name-1 unconditionally; it carries no entry-name clause, so the entry can be "
                + "neither unnamed nor FILLER (ISO §13.16.2 format 2, §13.18.45.2, §13.18.33.4 GR2b)",
            88 when isFiller =>
                "a condition-name entry names a condition-name — `88 condition-name-1 value-clause .` or "
                + "`88 [condition-name-2] value-clause .` — and carries no entry-name clause, so FILLER, the "
                + "entry-name clause's filler format, is not a name either format admits "
                + "(ISO §13.16.2 formats 3 and 4, §13.18.20.2 format 3, §13.16.3 SR24)",
            88 when name is null && !HasContentValidationPhrase(entry) =>
                "the condition-name format of the data description entry is written `88 condition-name-1 "
                + "value-clause .` and names condition-name-1 unconditionally; only the validation format, "
                + "`88 [condition-name-2] value-clause .`, may omit it, and its value clause requires the "
                + "INVALID or VALID phrase, which this entry has not got — so this entry is neither format "
                + "(ISO §13.16.2 formats 3 and 4, §13.18.63.2 format 5, §13.16.3 SR24)",
            _ => null,
        };
    }

    /// <summary>The entry's own name, for the diagnostic. Every arm's name is optional in the grammar (an
    /// unnamed entry is FILLER), so the caller gets a stable stand-in rather than a null.</summary>
    internal static string EntryName(RuleContext? entry) => entry switch
    {
        CobolParserCore.DataDescriptionEntryContext e => e.dataName()?.GetText() ?? "FILLER",
        CobolParserCore.ReportGroupEntryContext r => r.dataName()?.GetText() ?? "FILLER",
        CobolParserCore.ConstantEntryContext c => c.dataName()?.GetText() ?? "FILLER",
        CobolParserCore.ScreenDescriptionEntryContext s => s.screenName()?.GetText() ?? "FILLER",
        _ => "FILLER",
    };
}

/// <summary>
/// The LEVEL-NUMBER pass — ISO §13.18.33 (the level-number clause) and the two §13.16.3 rules that state the same
/// obligation at the data description entry. A SIBLING to <see cref="ExpressionFormationPass"/> on that pass's own
/// stated precedent ("giving non-edition syntax-rule conformance its own home is also what makes the NEXT such rule
/// automatic instead of adding a fourth place to remember").
///
/// <para><b>What it fixes.</b> Before it, a level-number was screened NOWHERE in the typed-native front end.
/// <c>DataBinder</c> read the number with a bare <c>int.TryParse</c> and no test of any kind, so <c>78 K VALUE 5.</c>
/// (a MicroFocus/GnuCOBOL extension ISO does not define) bound as a memberless GROUP nested under whatever entry
/// preceded it — because 78 exceeds every open level — compiled clean in the strict <c>--std 2023</c> lane, and
/// threw <c>NotImplementedCobolFeatureException</c> at RUN time on any numeric use. §4.2.2 requires "a warning
/// mechanism … to indicate violations of the general formats and the explicit syntax rules" at COMPILE time; a
/// run-time abort is not that. <c>50 BAD PIC X(3).</c> and a twelve-digit level ran clean too, and on the FORMAT
/// axis <c>05 R RENAMES A THRU B.</c> reached the emitter and produced uncompilable C#. kb/Work PB485.</para>
///
/// <para><b>Two axes, one screen.</b> A level-number is constrained by the SECTION its entry lives in
/// (§13.18.33.3, four different sets — 77 is legal in working-storage and illegal in a record area) and,
/// independently, by the general FORMAT the entry is written in (§13.18.33.4 GR2b/GR2c: 66 "may be used only as
/// described by the renames format", 88 "only … the condition-name format or the validation format"; §13.16.3 SR1:
/// a format-1 entry's "Level-number may be 77 or 1 through 49"; §13.16.3 SR2: a 77 entry shall carry the data-name
/// format of the entry-name clause). Both axes ask one question about one token, so both live here. The format
/// axis is decided from the entry BODY and never from the level-number — see <see cref="EntryBodyForm"/>.</para>
///
/// <para><b>Why a pass and not a check in <c>DataBinder</c>.</b> Several grammar rules spell a <c>levelNumber</c>
/// and they reach different binders: <c>dataDescriptionEntry</c> → <c>DataBinder.BindEntries</c>,
/// <c>reportGroupEntry</c> → <c>DataBinder.BindReportGroups</c>, <c>screenDescriptionEntry</c> →
/// <c>ScreenFacility</c>. A fifth rule, the non-ISO <c>linkageProcedureParameter</c>, once reached NO binder at all
/// and its entries silently vanished (it is deleted, kb/Work PB1252). Writing the test at each site would be one
/// copy of one rule per site with the next site guaranteed to be forgotten (the two-arm-dispatch shape this
/// repo reproduces most often). One traversal over the ONE grammar node that carries a level-number screens every
/// arm that exists and every arm that will exist, and <c>LevelNumberArmDriftTests</c> fails the moment the grammar
/// adds one the classifier does not know.</para>
///
/// <para><b>Why it runs BEFORE binding.</b> The rules are pure syntax — they read a token, the section ancestry
/// and the entry body, and need nothing the binder produces. Running first means the user sees the level-number
/// error instead of the cascade a phantom nesting produces downstream, and no bind-time failure on the malformed
/// structure can preempt it. The three post-bind sibling passes run late because they consume the bound model;
/// this one has no such reason to wait.</para>
///
/// <para><b>Why it is not edition-gated and not dialect-gated.</b> Every set here is the same in COBOL-85, 2002,
/// 2014 and 2023 — neither clause carries an <c>introducedIn</c> — so this is not an edition gate (the negative
/// goldens assert all four editions reject). And <c>--permissive</c> softens exactly one verdict,
/// <c>ConstructAvailability.Removed</c> → Warning, the migration mode for constructs an edition REMOVED; a level
/// number ISO never defined is not a removed construct, so there is no permissive arm to write. Same reasoning,
/// measured the same way, as <see cref="ExpressionFormationPass"/>.</para>
/// </summary>
internal sealed class LevelNumberPass(IDiagnosticSink sink) : CursorFollowingVisitor(sink)
{
    /// <summary>ISO §8.3.2.2.13 — "A level-number, expressed as a one-digit or two-digit number".</summary>
    private const int MaxLevelNumberDigits = 2;

    /// <summary>Screen every level-number in the group's raw parse tree.</summary>
    internal static void Run(CobolParserCore.CompilationUnitContext tree, IDiagnosticSink sink) =>
        new LevelNumberPass(sink).VisitPositioned(tree);

    /// <summary>ISO §13.18.33.3 SR2 / SR4 / SR5 / SR6 (the section axis) and, for a data description entry,
    /// §13.18.33.4 GR2b/GR2c + §13.16.3 SR1/SR2 (the format axis).</summary>
    public override object? VisitLevelNumber(CobolParserCore.LevelNumberContext ctx)
    {
        if (LevelNumberRules.Classify(ctx) is { } armKind)
        {
            string text = ctx.GetText();
            // A level-number too large to be an int (a twelve-digit literal parses as INTEGERLIT and used to
            // become a silent `lvl = 0` in DataBinder) is out of range for every arm by definition, so the
            // unparseable case takes the same diagnostic rather than a second mechanism.
            bool known = int.TryParse(text, out int level);
            var arm = LevelNumberRules.For(armKind);
            // ⛔ THE SPELLING IS PART OF THE RULE (kb/Work PB1246). §8.3.2.2.13: "A level-number, expressed as a
            // one-digit or two-digit number" — and §13.18.33.3 SR3 grants exactly one leading-zero form, "A
            // level-number in the range of 1 through 9 may be specified as 01 through 09". `001` has the VALUE 1
            // and is no level-number at all, so the value test below cannot see it; the grammar's INTEGERLIT takes
            // any digit run, which is why the length is asked here, in the one place every arm's level-number passes.
            if (text.Length > MaxLevelNumberDigits)
                Report(ctx, DiagnosticCatalog.LevelNumberOutOfRange, text,
                    "a level-number is expressed as a one-digit or two-digit number, and a level-number in the "
                    + "range of 1 through 9 may be specified as 01 through 09 (ISO §8.3.2.2.13, §13.18.33.3 SR3)");
            else if (!known || !arm.Permits(level))
                Report(ctx, DiagnosticCatalog.LevelNumberOutOfRange, text,
                    $"{arm.Requirement} (ISO §13.18.33.3 SR{arm.SyntaxRule})");
            else if (ctx.Parent is CobolParserCore.DataDescriptionEntryContext entry)
                CheckEntryFormat(ctx, entry, text, level);
        }
        return base.VisitChildren(ctx);
    }

    /// <summary>The data description entry is the only one of the four arms §13.16.3 and §13.18.33.4 GR2 speak
    /// about — a report group entry, a screen entry and a procedure parameter are not data description entries,
    /// so the format axis stops here rather than being asserted of constructs the clauses never name.</summary>
    private void CheckEntryFormat(CobolParserCore.LevelNumberContext ctx, CobolParserCore.DataDescriptionEntryContext entry, string text, int level)
    {
        switch (LevelNumberRules.BodyForm(entry), level)
        {
            // A §13.10 CONSTANT entry is NOT a §13.16 data description entry — it allocates no storage and is a
            // compile-time substitution — so none of the format rules below speak about it. Its own level rule is
            // §13.10.2, enforced where the constant is bound (DataBinder.Constants.cs). Without this arm a
            // mis-leveled constant entry would draw a RENAMES-format message about a construct that has no
            // formats, which is a worse diagnostic than none.
            case (EntryBodyForm.Constant, _):
                break;

            // §13.16.2 Format 2 is written `66 data-name-1 RENAMES …`, and §13.18.33.4 GR2b: "Level-number 66 is
            // assigned to identify RENAMES entries and may be used only as described by the renames format of the
            // data description entry." A renames body under any other level reached the EMITTER and produced
            // uncompilable C# (`_T_0 does not contain a definition for 'R'`) — kb/Work PB485.
            case (EntryBodyForm.Renames, not 66):
                Report(ctx, DiagnosticCatalog.LevelNumberEntryFormat, text,
                    "level-number 66 is assigned to identify RENAMES entries and may be used only as described "
                    + "by the renames format of the data description entry, which is written `66 data-name-1 "
                    + "RENAMES …` (ISO §13.18.33.4 GR2b, §13.16.2 format 2)");
                break;

            // §13.18.33.4 GR2b again, the other direction: a level-66 entry that is not a renames entry is a
            // format-1 entry at 66, which §13.16.3 SR1 ("Level-number may be 77 or 1 through 49") forbids.
            case (not EntryBodyForm.Renames, 66):
                Report(ctx, DiagnosticCatalog.LevelNumberEntryFormat, text,
                    "level-number 66 is assigned to identify RENAMES entries and may be used only as described "
                    + "by the renames format of the data description entry; this entry has no RENAMES clause, so "
                    + "it is a format-1 entry, whose level-number may be 77 or 1 through 49 "
                    + "(ISO §13.18.33.4 GR2b, §13.16.3 SR1)");
                break;

            // §13.18.33.4 GR2c: "Level-number 88 … may be used only as described by the condition-name format or
            // the validation format of the data description entry." Both formats are `88 [condition-name]
            // value-clause .` — a value clause and nothing else.
            case (not EntryBodyForm.ValueOnly, 88):
                Report(ctx, DiagnosticCatalog.LevelNumberEntryFormat, text,
                    "level-number 88 may be used only as described by the condition-name format or the "
                    + "validation format of the data description entry, and both are written `88 "
                    + "[condition-name] value-clause .`; this entry's body is not a value clause alone, so it is "
                    + "a format-1 entry, whose level-number may be 77 or 1 through 49 "
                    + "(ISO §13.18.33.4 GR2c, §13.16.2 formats 3 and 4, §13.16.3 SR1)");
                break;

            // §13.16.2 Format 3, THE OTHER HALF OF THE ARM ABOVE (kb/Work PB501). That arm asks whether the body
            // is a value clause and nothing else; this asks whether the entry has the NAME its format requires.
            // RENDERED from the printed page (folio 364 / PDF p394, `scripts/render-spec-page.py 394`), because
            // the whole verdict turns on a bracket:
            //     Format 3 (condition-name):  88 condition-name-1 value-clause .      ← NOT bracketed: required
            //     Format 4 (validation):      88 [ condition-name-2 ] value-clause .  ← bracketed: optional
            // Format 4's licence to omit the name is not free-standing: its value-clause is §13.18.63.2's
            // format 5 (content-validation-entry), whose figure ends in a REQUIRED `{ INVALID | VALID }` choice
            // (the transcription's figure note says so in those words, and the phrase has its own grammar rule,
            // `validateValidPhrase`). So a nameless 88 whose VALUE clause carries no VALID/INVALID phrase is
            // neither format, and §13.16.3 SR24 — "Format 3 or 4 is used for each condition-name" — leaves no
            // third possibility.
            // It was accepted at every edition and the entry then EVAPORATED: DataBinder.BindCondition's first
            // statement returns on a missing data-name, so `88 VALUE 1.` parsed (VALUE opens the value clause,
            // not the name slot), bound nothing, produced no diagnostic and compiled to a .dll — whatever the
            // programmer meant by it silently gone.
            //
            // ⛔ ONE ARM FOR EVERY FORMAT WITH A NAME SLOT (kb/Work PB849). The obligation is a property of the
            // (level, format) pair and lives in ONE place, LevelNumberRules.EntryNameObligation. Before PB849 it
            // was two hand-written arms — 77 and this nameless-88 case — and the third format whose name is
            // printed unbracketed, renames, had NONE: `66 RENAMES A THRU B.` and `66 FILLER RENAMES A THRU B.`
            // compiled silently and the entry evaporated, and so did `88 FILLER VALUE "A".` (FILLER is the
            // entry-name clause's filler format, which only format 1 carries).
            case (_, 66 or 77 or 88) when LevelNumberRules.EntryNameObligation(entry, level) is { } requirement:
                Report(ctx, DiagnosticCatalog.LevelNumberEntryFormat, text, requirement);
                break;
        }
    }

    private void Report(CobolParserCore.LevelNumberContext ctx, DiagnosticDescriptor descriptor, string text,
        string requirement)
    {
        string where = $"level-number {text} on '{LevelNumberRules.EntryName(ctx.Parent)}'";
        Sink.Report(new EditionDiagnostic(descriptor.Code, EditionSeverity.Error, descriptor.Id,
            $"{where}: {requirement}", where, descriptor.IsoSection));
    }
}
