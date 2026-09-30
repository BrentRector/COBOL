// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime.Tree;

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Validation;

using Core = CobolParserCore;

/// <summary>
/// ⭐ ONE ROW PER CORRESPONDING VERB — the operand rule §14.9.25.3 SR12, §14.9.2.3 SR6 and §14.9.44.3 SR6 each
/// state, AS DATA, so <see cref="StatementValidation.CheckCorrespondingGroupOperand"/> can ask each caller's own
/// rule instead of the intersection of the three (kb/Work PB392).
/// <para>The three rules differ on two axes and agreed on neither before this: ADD and SUBTRACT enumerate FOUR
/// group kinds and exclude level-66 by name; MOVE says only "group data items" and never mentions level-66. A
/// shared screen therefore has to carry the difference as a row, not as a comment — a fourth CORRESPONDING verb
/// would be a row here and no edit anywhere else.</para>
/// </summary>
/// <param name="Verb">MOVE | ADD | SUBTRACT — the statement the programmer wrote, for the message.</param>
/// <param name="Clause">The caller's own rule, e.g. "§14.9.2.3 SR6".</param>
/// <param name="Admitted">The group kinds the rule's own text admits.</param>
/// <param name="ExcludesLevel66">True when the rule's text names level-number 66 (the arithmetic spellings), so
/// the diagnostic may quote it. MOVE's SR12 does not, and quoting it there was a miscitation.</param>
/// <summary>⭐ ONE ROW PER OPEN TAPE PHRASE — the two alternatives of ISO §14.9.27.2's per-file-name
/// <c>[ REVERSED | WITH NO REWIND ]</c> bracket, each stating its own ORGANIZATION rule and its own OPEN MODE
/// rule AS DATA, so <see cref="StatementValidation.CheckOpenTapePhrase"/> is a lookup rather than a chain of
/// per-phrase methods. The two phrases' rules differ on BOTH axes — NO REWIND admits either sequential kind
/// (§9.1.7.2) and INPUT or OUTPUT (§14.9.27.3 SR5/SR6), REVERSED admits record sequential only and INPUT only
/// — which is exactly why a shared screen has to carry the difference as a row. A third tape phrase is a row
/// here and no edit anywhere else (kb/Work PB668).</summary>
/// <param name="Phrase">The phrase AS WRITTEN, for the message.</param>
/// <param name="AdmitsOrganization">The rule's own organization predicate over the file control entry.</param>
/// <param name="OrganizationFace">How the admitted organization is named in the message.</param>
/// <param name="OrganizationClause">The clause that states the organization rule.</param>
/// <param name="Modes">The open modes the phrase's group may specify.</param>
/// <param name="OpenModeClause">The clause that states the open-mode rule.</param>
/// <param name="OrganizationCode">The diagnostic for the organization rule.</param>
/// <param name="OpenModeCode">The diagnostic for the open-mode rule.</param>
internal readonly record struct OpenTapePhraseRule(
    string Phrase, Func<FileModel, bool> AdmitsOrganization, string OrganizationFace, string OrganizationClause,
    BoundOpenMode[] Modes, string OpenModeClause, string OrganizationCode, string OpenModeCode)
{
    /// <summary>§14.9.27.3 SR5 + SR6 — NO REWIND is for sequential files (§9.1.7.2 admits BOTH sequential
    /// kinds, the same <see cref="FileModel.IsSequential"/> predicate the CLOSE twin §14.9.6.3 SR1 tests) under
    /// INPUT or OUTPUT.</summary>
    private static readonly OpenTapePhraseRule NoRewind = new(
        "WITH NO REWIND", f => f.IsSequential, "sequential", "ISO §14.9.27.3 SR5",
        [BoundOpenMode.Input, BoundOpenMode.Output], "ISO §14.9.27.3 SR6",
        DiagnosticCatalog.OpenNoRewindOrganization.Code, DiagnosticCatalog.OpenNoRewindOpenMode.Code);

    /// <summary>COBOL-85's REVERSED — NARROWER than its sibling on BOTH axes (VERSION_CHANGE_REFERENCE row
    /// 7.12): RECORD sequential organization only, because the phrase's whole effect is the backward retrieval
    /// §14.9.30.4 GR21 c) defines over a record number and §14.9.30.3 SR7 denies to LINE SEQUENTIAL; and the
    /// INPUT group only, because the other three modes have no READ for a retrieval direction to govern.</summary>
    private static readonly OpenTapePhraseRule Reversed = new(
        "REVERSED", f => f.Organization is FileOrganization.Sequential, "record sequential",
        "ISO §14.9.30.4 GR21 c) — REVERSED establishes the backward retrieval only a record sequential "
        + "file's record number supports (§14.9.30.3 SR7 denies it to LINE SEQUENTIAL); "
        + "VERSION_CHANGE_REFERENCE row 7.12",
        [BoundOpenMode.Input], "VERSION_CHANGE_REFERENCE row 7.12 — COBOL-85's OPEN general format writes "
        + "REVERSED in the INPUT group only",
        DiagnosticCatalog.OpenReversedOrganization.Code, DiagnosticCatalog.OpenReversedOpenMode.Code);

    /// <summary>⛔ THE ONE LOOKUP, and the reason the rows are named rather than indexed: a switch over the
    /// enum cannot silently mis-key when a member is inserted, and a member with NO row is LOUD at the first
    /// OPEN that writes the phrase instead of reading whichever row happened to sit at its ordinal. That is the
    /// drift guard this structure carries INSTEAD of a test asserting an array's order (CLAUDE.md rule 5 — the
    /// shape that makes the next case automatic).</summary>
    public static OpenTapePhraseRule For(BoundOpenTapePhrase phrase) => phrase switch
    {
        BoundOpenTapePhrase.NoRewind => NoRewind,
        BoundOpenTapePhrase.Reversed => Reversed,
        _ => throw new InvalidOperationException(
            $"no syntax-rule row for the OPEN tape phrase {phrase} — a new alternative of §14.9.27.2's "
            + "per-file-name bracket needs its organization rule and its open-mode rule here (kb/Work PB668)"),
    };
}

internal readonly record struct CorrespondingOperandRule(
    string Verb, string Clause, GroupKinds Admitted, bool ExcludesLevel66)
{
    /// <summary>ISO §14.9.25.3 SR12 — "Identifier-3 and identifier-4 shall specify group data items and shall
    /// not be reference-modified." Every group kind is a group data item, and NOTE 5 under §14.9.25.4 GR11 puts
    /// it beyond doubt for the two the other verbs exclude: "For purposes of MOVE CORRESPONDING, bit group items
    /// and national group items are processed as group items, rather than as elementary items."</summary>
    public static readonly CorrespondingOperandRule Move =
        new("MOVE", "§14.9.25.3 SR12", GroupKinds.Any, ExcludesLevel66: false);

    /// <summary>ISO §14.9.2.3 SR6 — "Identifier-4 and identifier-5 shall be alphanumeric group items, national
    /// group items, variable-length groups, or strongly-typed group items and shall not be described with
    /// level-number 66."</summary>
    public static readonly CorrespondingOperandRule Add = new("ADD", "§14.9.2.3 SR6", ArithmeticKinds, true);

    /// <summary>ISO §14.9.44.3 SR6 — word for word ADD's SR6, with identifier-4 the subtrahend group.</summary>
    public static readonly CorrespondingOperandRule Subtract =
        new("SUBTRACT", "§14.9.44.3 SR6", ArithmeticKinds, true);

    /// <summary>SR6's four admitted kinds, written ONCE because the two rules are the same sentence: a bit group
    /// is the one kind left out, and §14.7.6 rule 3 is why — "In an ADD or SUBTRACT statement, both of the data
    /// items are numeric data items", which no child of a bit group (category boolean, §13.18.29.4 GR1) can be,
    /// so every implied pair would silently fail to correspond.</summary>
    private const GroupKinds ArithmeticKinds =
        GroupKinds.Alphanumeric | GroupKinds.National | GroupKinds.VariableLength | GroupKinds.StronglyTyped;
}

/// <summary>
/// The edition-INVARIANT syntax-rule check catalog lifted out of the verb binders (P7 Step 10; the phase
/// doc's §Step 10). The contract (fixed at 10c, the AS-BUILT PLAN's convention): every <c>Check*</c> is a
/// PURE check — it reports to the ONE sink (<c>data.Edition</c>) with byte-identical message text and
/// returns the verdict (<c>true</c> = conformant); the CALLING verb binder owns all control flow (error
/// placeholders, statement aborts, operand rewrites). Edition GATING never lives here — the
/// <c>VersionConformancePass</c> is the sole edition funnel (DESIGN-version-conformance-pipeline); the
/// residual inline gates move VERBATIM with their verb binders until Exec Step E folds them.
/// </summary>
internal sealed class StatementValidation(DataBinder data)
{
    // ── THE STATEMENT-OPERAND SCREENS THAT USED TO BE RUN-TIME LOUDS (kb/Work PB236) ─────────────────────────
    //    `BoundUnsupported` was the carrier for THREE incompatible jobs — a genuine feature DEFERRAL, an
    //    ill-formed OPERAND, and an illegal PLACEMENT — and StatementEmitter rendered all three as the same
    //    `NotImplemented.Run(...)`. The two user-error jobs belong at COMPILE time, per ISO §4.2.2 ¶2 ("An
    //    implementation shall provide a warning mechanism that optionally may be invoked by the user at compile
    //    time to indicate violations of the general formats and the explicit syntax rules of standard COBOL"),
    //    and they belong HERE, in the one syntax-rule check catalog, not at each verb binder.

    /// <summary>⛔ THE ONE file-name → <see cref="FileModel"/> resolution for a STATEMENT operand (kb/Work
    /// PB236). Seven binder sites — UNLOCK, OPEN, CLOSE, READ, DELETE, DELETE FILE and START — each wrote its
    /// own <c>FilesByName.TryGetValue</c> and its own unreported <c>BoundUnsupported</c>, so a word naming no
    /// file connector compiled clean at every edition and either aborted the run unit or, on an unexecuted
    /// path, said nothing at all. Each of those statements' general formats writes the operand as
    /// <c>file-name-1</c>, and ISO §8.4.2.1 fixes what a name must do: "In order to use a resource, a statement
    /// shall contain a reference that uniquely identifies that resource." A word that identifies no file
    /// connector identifies no resource.
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.UndefinedReference"/> (COBOLNET1639),
    /// not a new code: "this source element defines no such name" is ONE rule, and the file-name space was
    /// simply missing from the place that already reports it for data-names (feedback_one_rule_one_place). A
    /// new statement that names a file gets the diagnostic by calling this, not by remembering to write
    /// one.</para></summary>
    /// <param name="name">The file-name as written.</param>
    /// <param name="verb">The statement, for the message, e.g. "OPEN".</param>
    /// <param name="file">The resolved model when this returns true.</param>
    /// <param name="admitsReportFile">True ONLY for the statements §13.4.5.3 SR9 lists — CLOSE, and OPEN (whose
    /// INPUT / I-O half is <see cref="CheckOpenModeForFile"/>'s). Every other statement resolving a file-name is
    /// refused a report file by <see cref="ScreenReportFileReference"/>, so a new statement is screened by
    /// construction (kb/Work PB1171).</param>
    /// <param name="statementRule">The statement's own restatement of the report-file rule, quoted with its
    /// citation, when it has one (SORT §14.9.40.3 SR8, MERGE §14.9.24.3 SR9).</param>
    /// <returns>true when the name identifies a file connector the statement may reference; false after
    /// reporting.</returns>
    public bool ResolveFile(string name, string verb, [NotNullWhen(true)] out FileModel? file,
                            bool admitsReportFile = false, string? statementRule = null)
    {
        if (data.FilesByName.TryGetValue(name, out file))
            return admitsReportFile || ScreenReportFileReference(file, verb, statementRule);
        data.Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"'{name}' is not defined as a file — {verb} names file-name-1 in its general format, and no SELECT "
            + "or file description entry in this source element gives that name, so the statement's reference "
            + "identifies no resource (ISO §8.4.2.1: \"a statement shall contain a reference that uniquely "
            + "identifies that resource\"). Check the spelling, or add the file to the FILE-CONTROL paragraph.");
        return false;
    }

    /// <summary>⛔ THE ONE REPORT-FILE REFERENCE RULE (kb/Work PB1171) — ISO §13.4.5.3 SR9, stated a second time
    /// from the REPORT clause's side as §13.18.46.3 SR3: "The subject of a file description entry that specifies a
    /// REPORT clause may be referenced in the procedure division only by the USE statement, the WHEN phrase of a
    /// PERFORM statement, the CLOSE statement, or the OPEN statement with the OUTPUT or EXTEND phrase." It used to be
    /// asked by OPEN alone (COBOLNET2371, its INPUT / I-O half), so READ, START, DELETE, DELETE FILE, UNLOCK, a
    /// SORT / MERGE USING or GIVING file and WRITE of a report FD's record compiled clean and ran against the report
    /// writer's file. <see cref="ResolveFile"/> asks it for every statement that is not in the list; the statements
    /// whose operand is not a file-name ask it themselves (SORT/MERGE USING/GIVING through ResolveFile with their
    /// own rule; WRITE §14.9.51.3 SR12 and REWRITE §14.9.35.3 SR11 on the file of the record they resolved — such a
    /// record is already refused at its entry, §13.4.5.3 SR8 COBOLNET2578, and the statement's own rule is reported
    /// as well, because every rule a statement violates is, kb/Work PB352).</summary>
    /// <returns>true when <paramref name="file"/> is not a report file; false after reporting.</returns>
    public bool ScreenReportFileReference(FileModel file, string verb, string? statementRule = null)
    {
        if (!file.IsReportFile) return true;
        data.Edition.Error(DiagnosticCatalog.ReportFileReference,
            $"{verb} '{file.CobolName}' — '{file.CobolName}' is a report file (its file description entry specifies "
            + "a REPORT clause), and ISO §13.4.5.3 SR9: \"The subject of a file description entry that specifies a "
            + "REPORT clause may be referenced in the procedure division only by the USE statement, the WHEN phrase "
            + "of a PERFORM statement, the CLOSE statement, or the OPEN statement with the OUTPUT or EXTEND phrase\""
            + (statementRule is null ? "" : $"; {statementRule}")
            + ". A report file is written only through INITIATE, GENERATE and TERMINATE.");
        return false;
    }

    /// <summary>⛔ THE ONE <c>procedure-name</c> OPERAND RULE (kb/Work PB390), reported for every statement
    /// whose general format prints that operand: PERFORM, GO TO (both formats), ALTER, RESUME AT and the
    /// SORT/MERGE INPUT/OUTPUT PROCEDURE phrases. The resolution itself is
    /// <see cref="Procedure.ProcedureTableBuilder.ResolveProcedureOperand"/> — §8.4.2.2's in-section →
    /// global → section-name order, ONE implementation — and this is where its failure becomes a DIAGNOSTIC
    /// instead of a bound node.
    /// <para>⛔ THE STAGE WAS THE DEFECT. Eight sites each turned "this name resolves to nothing" into a
    /// <c>BoundUnsupported</c>, which the emitter renders as <c>NotImplemented.Run(...)</c>: a misspelled
    /// <c>PERFORM</c> COMPILED, shipped an assembly, and aborted the run unit with an unhandled .NET exception
    /// telling the user that WiseOwl COBOL had not implemented a feature — the diagnosis sent to the wrong party,
    /// at the wrong time, and on an unexecuted path never sent at all. ISO §4.2.2 ¶2 puts violations of "the
    /// general formats and the explicit syntax rules" in the compile-time mechanism.</para>
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.UndefinedReference"/> (COBOLNET1639)
    /// and no new code — "this source element defines no such name" is ONE rule (ISO §8.4.2.1), and the
    /// procedure-name space was simply missing from the place that already reports it for data-names and (since
    /// kb/Work PB236) for file-names. SORT's own private copy of this message folded into it here.</para>
    /// <para>The scope half is quoted because it is the half a reader disbelieves: §8.4.6.1 lists paragraph-name
    /// and section-name among the words that "may be referenced only by statements in the source element in
    /// which the user-defined word is declared", which is why a CONTAINED program's paragraph, and a sibling
    /// METHOD's, are correctly not found — a method definition begins with an identification division
    /// (§11.7.1), so it is a contained source unit (§3.165) and thus its own source element (§3.164).
    /// ⛔ NOT §11.7 ALONE, which is where the deleted <c>OoScopeHint</c> pointed: that clause is the
    /// METHOD-ID paragraph, and the only scoping rule it states (§11.7.4 GR5) is about words defined in the
    /// method's DATA DIVISION — a real clause answering a different question.</para></summary>
    /// <param name="refText">The procedure-name as written, for the message.</param>
    /// <param name="head">Its head word (the qualifier stripped), for the data-name discrimination.</param>
    /// <param name="verb">The statement or phrase, e.g. "PERFORM" or "SORT INPUT PROCEDURE".</param>
    /// <param name="rule">The caller's own syntax rule quoted with its citation, or "" where the statement
    /// states none of its own (GO TO and ALTER rest on §8.4.2.1 alone).</param>
    /// <param name="methodLocal">True inside a method body — resolution is confined there (ISO §11.7).</param>
    /// <returns>Always false: the operand is refused, HAVING REPORTED.</returns>
    public bool RejectProcedureName(string refText, string head, string verb, string rule, bool methodLocal)
    {
        bool isDataName = data.Symbols.TryResolve(head, data.ActiveScope, out _);
        data.Edition.Error(DiagnosticCatalog.UndefinedReference,
            $"{verb} '{refText}' names no procedure: no paragraph or section in this source element carries "
            + "that name, so the reference identifies no resource (ISO §8.4.2.1 — \"In order to use a resource, "
            + "a statement shall contain a reference that uniquely identifies that resource\"; §8.4.6.1 — a "
            + "paragraph-name or section-name \"may be referenced only by statements in the source element in "
            + "which the user-defined word is declared\")"
            + (isDataName
                ? $"; '{head}' is declared as a DATA ITEM, and a procedure-name is a different class of "
                  + "user-defined word (§8.3.2.2)"
                : "")
            + (methodLocal
                ? "; resolution inside a method is METHOD-LOCAL — a method definition begins with an "
                  + "identification division (ISO §11.7.1) so it is a contained source unit (§3.165) and "
                  + "therefore its own source element (§3.164: \"source unit excluding any contained source "
                  + "units\"), which is what the scope rule above is measured against — so the paragraphs of "
                  + "sibling methods and of the driver program are not visible here"
                : "")
            + (rule.Length > 0 ? $". {rule}" : ""));
        return false;
    }

    /// <summary>⛔ THE OTHER HALF OF §8.4.2.1's "UNIQUELY identifies that resource" (kb/Work PB466): the
    /// reference names MORE than one procedure. <see cref="RejectProcedureName"/> above answers "no such name";
    /// this answers "which one?", and the two are separate diagnostics because a user sent to look for a
    /// missing paragraph will not find the second one they wrote.
    /// <para>Which rule broke decides which code and which repair the message can honestly offer.
    /// §8.4.2.2.1 rule 1 — "No other name has the identical spelling" — false with no rule-2-through-6 excuse
    /// means §8.4.2.2.3 SR1's "sequence of qualifiers that precludes any ambiguity of reference" was owed and
    /// not written, which the user CAN supply (§8.4.2.2.2 format 4, <c>paragraph-name IN section-name</c>) —
    /// unless the duplicated word is the section-name itself, which takes no qualifier at all. §8.4.2.2.3 SR7 —
    /// "If explicitly referenced, a paragraph-name shall not be duplicated within a section" — admits no
    /// qualifier by construction, so the only repair is a rename, and telling the user to qualify would be
    /// telling them to write something the standard has no format for.</para>
    /// <para>Reported at the REFERENCE, never at the declaration: both rules are conditioned on the name being
    /// "explicitly referenced", so a program that declares a duplicate and never mentions it is conforming and
    /// shall compile (§4.2.2 — an implementation "shall accept the syntax … for all standard language elements
    /// required by" the standard).</para></summary>
    /// <param name="ambiguity">The rule that failed, the spelling, and every declaration carrying it.</param>
    /// <param name="verb">The statement or phrase, e.g. "PERFORM" or "SORT INPUT PROCEDURE".</param>
    /// <returns>Always false: the operand is refused, HAVING REPORTED.</returns>
    public bool RejectAmbiguousProcedureName(Procedure.ProcedureAmbiguity ambiguity, string verb)
    {
        string candidates = string.Join("; ", ambiguity.Candidates);
        if (ambiguity.Rule == Procedure.ProcedureAmbiguityRule.InSectionDuplicate)
        {
            data.Edition.Error(DiagnosticCatalog.ParagraphNameDuplicatedInSection,
                $"{verb} '{ambiguity.Name}': that paragraph-name is declared {ambiguity.Candidates.Count} times "
                + $"within section '{ambiguity.Section}' ({candidates}), "
                + "and it is explicitly referenced here (ISO §8.4.2.2.3 SR7 — \"If explicitly referenced, a "
                + "paragraph-name shall not be duplicated within a section\"). No qualifier can separate the "
                + "two: §8.4.2.2.2 format 4 gives a paragraph-name only its section-name as a qualifier, and "
                + "both declarations carry the same one — rename one of the paragraphs.");
            return false;
        }
        data.Edition.Error(DiagnosticCatalog.AmbiguousProcedureName,
            $"{verb} '{ambiguity.Name}': the reference is not unique — this source element declares that name "
            + $"{ambiguity.Candidates.Count} times ({candidates}) — so it identifies no one procedure (ISO "
            + "§8.4.2.1: \"a statement shall contain a reference that uniquely identifies that resource\"). "
            + "§8.4.2.2.1 requires that \"uniqueness shall be established through qualification for each "
            + "user-defined name explicitly referenced, except as specified in rules 2 through 6\": rule 1 "
            + "(\"No other name has the identical spelling\") is false here, and rule 6 (\"The name is a "
            + "paragraph-name and the section containing the reference also contains the named paragraph\") "
            + "does not apply to this reference. Write the qualifier — paragraph-name IN section-name, "
            + "§8.4.2.2.2 format 4 — or rename one of the declarations; a duplicated SECTION-name takes no "
            + "qualifier at all and can only be renamed.");
        return false;
    }

    /// <summary>⛔ ISO §14.9.28.3 SR11 — the DECLARATIVES constraint on a <c>procedure-name-1 THRU
    /// procedure-name-2</c> range (kb/Work PB433): "When procedure-name-1 and procedure-name-2 are both
    /// specified and either is the name of a procedure in the declaratives portion of the procedure division,
    /// both shall be procedure-names in the same declarative section."
    /// <para>⛔ ONE PREDICATE, BOTH HALVES. The rule has two failing shapes and a smallest-diff fix sees only
    /// the loud one: a range with ONE end in the declaratives (which the positional dispatcher runs off the end
    /// of the declarative section and into whatever physically follows — including the PERFORM itself, so the
    /// run unit dies of a stack overflow with no diagnostic), and a range whose two ends are in DIFFERENT
    /// declarative sections (which runs quietly and wrongly, since the two sections carry different USE
    /// dispatch). Both are "the two ends are not procedures of the same declarative section", so both are this
    /// one test — asked of the SECTION each end resolved from, never of pc arithmetic.</para>
    /// <para>The test is section IDENTITY, not name equality: <see cref="Procedure.SectionInfo"/> is the
    /// collected object, one per declarative section, so two sections that share a spelling (which
    /// §8.4.2.2.1 would already have made an ambiguous reference) cannot pass by accident.</para>
    /// <para>⛔ CHECKED WHERE BOTH ENDS ARE STILL RESOLUTIONS. After <see cref="PcRange.Through"/> composes the
    /// range there is only a pc pair, and which section each end came from is gone — which is why the rule was
    /// implemented nowhere and why <see cref="Procedure.ResolvedProcedure"/> carries the section.</para>
    /// <para>Every edition: the rule is COBOL-85's PERFORM declaratives constraint carried unchanged through
    /// 2002/2014/2023, so there is no introduction gate and the diagnostic fires at every <c>--std</c>.</para></summary>
    /// <param name="first">The resolved procedure-name-1.</param>
    /// <param name="thru">The resolved procedure-name-2.</param>
    /// <param name="name1">Procedure-name-1 as written, for the message.</param>
    /// <param name="name2">Procedure-name-2 as written, for the message.</param>
    /// <param name="verb">The statement naming the range, for the message (PERFORM).</param>
    /// <returns>True when the range conforms; false HAVING REPORTED.</returns>
    public bool CheckDeclarativesRange(
        ResolvedProcedure first, ResolvedProcedure thru, string name1, string name2, string verb)
    {
        // Neither end is in the declaratives portion — SR11's antecedent ("either is the name of a procedure in
        // the declaratives portion") is false and the rule says nothing about the range.
        if (!first.IsDeclarative && !thru.IsDeclarative) return true;
        // Either end is: then BOTH shall be procedure-names in the SAME declarative section. One object.
        if (ReferenceEquals(first.Section, thru.Section)) return true;

        data.Edition.Error(DiagnosticCatalog.PerformRangeDeclaratives,
            $"{verb} '{name1}' THRU '{name2}': {Portion(first, name1)} and {Portion(thru, name2)}, so the range "
            + "straddles a boundary the standard does not admit — \"When procedure-name-1 and procedure-name-2 "
            + "are both specified and either is the name of a procedure in the declaratives portion of the "
            + "procedure division, both shall be procedure-names in the same declarative section\" (ISO "
            + "§14.9.28.3 SR11). The range is a SEQUENCE of procedures (§14.9.28.4 GR4 — \"all statements "
            + "beginning with the first statement of procedure-name-1 and ending with the last statement of "
            + "procedure-name-2\"), and the procedures between the two ends here belong to a different USE "
            + "procedure, or to none. Perform the declarative range and the nondeclarative range separately, "
            + "or name two procedures of the one declarative section.");
        return false;
    }

    /// <summary>⛔ ISO §14.9.49.3 SR3 and SR4 — the DECLARATIVES BOUNDARY on a procedure-name reference (kb/Work
    /// PB362). A two-arm rule whose arms point in OPPOSITE directions, and each arm exempts a different verb:
    /// <list type="bullet">
    /// <item><b>SR3, OUT of a declarative</b> — "Within a declarative procedure, there shall be no reference to any
    /// nondeclarative procedures except in a RESUME statement." The reference is written inside a declarative
    /// section and names a procedure outside the declaratives portion.</item>
    /// <item><b>SR4, INTO a declarative section</b> — "Procedure-names within a declarative section may be
    /// referenced in a different declarative section or in a nondeclarative procedure only with a PERFORM
    /// statement." The reference names a procedure of a declarative section and is written anywhere outside THAT
    /// section.</item>
    /// </list>
    /// <para>Both are asked of SECTION IDENTITY (the collected <see cref="Procedure.SectionInfo"/> the reference is
    /// written in and the one the resolution carries), never of pc arithmetic — the PB433 shape.</para>
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md §3, "Declaratives reference boundary"): the standard asks of a
    /// syntax-rule violation only §4.2.2's warning mechanism, so the SEVERITY is ours to choose, and the owner's
    /// precedence (ISO, then GnuCOBOL) settles it: an SR4 violation is an ERROR — a GO TO, ALTER or SORT/MERGE
    /// procedure phrase that enters a USE procedure from outside has no activating USE to return to (§14.9.49.4
    /// GR7), and GnuCOBOL refuses it ("invalid reference … in DECLARATIVES") — while an SR3 violation is a
    /// WARNING, as GnuCOBOL's is: a PERFORM of a nondeclarative paragraph from a USE procedure is executable as
    /// written and is common in production source, which IBM Enterprise COBOL and Micro Focus accept outright.</para>
    /// <para>Every edition: both rules are COBOL-85's USE syntax rules carried unchanged into 2023.</para></summary>
    /// <param name="target">The resolved procedure.</param>
    /// <param name="referencingSection">The section containing the reference (null outside every section).</param>
    /// <param name="name">The procedure-name as written, for the message.</param>
    /// <param name="verb">The statement or phrase, for the message.</param>
    /// <param name="kind">Which statement writes the reference — PERFORM and RESUME are the two exemptions.</param>
    /// <returns>False HAVING REPORTED the SR4 error; true otherwise (including after the SR3 warning).</returns>
    public bool CheckDeclarativesBoundary(
        ResolvedProcedure target, SectionInfo? referencingSection, string name, string verb,
        ProcedureReferenceKind kind)
    {
        bool fromDeclarative = referencingSection is { IsDeclarative: true };
        // SR4 — a procedure of a declarative section, referenced from outside THAT section by anything but PERFORM.
        if (target.IsDeclarative && !ReferenceEquals(target.Section, referencingSection)
            && kind != ProcedureReferenceKind.Perform)
        {
            data.Edition.Error(DiagnosticCatalog.DeclarativeReferencedFromOutside,
                $"{verb} '{name}': '{name}' is a procedure of declarative section '{target.Section!.Name}', and this "
                + "statement is " + (fromDeclarative
                    ? $"in a different declarative section, '{referencingSection!.Name}'"
                    : "in the nondeclarative portion of the procedure division")
                + " — \"Procedure-names within a declarative section may be referenced in a different declarative "
                + "section or in a nondeclarative procedure only with a PERFORM statement\" (ISO §14.9.49.3 SR4). A USE "
                + "procedure is entered by its USE dispatch or performed; PERFORM it, or move the shared code out of "
                + "the declaratives.");
            return false;
        }
        // SR3 — a nondeclarative procedure, referenced from inside a declarative procedure by anything but RESUME.
        if (fromDeclarative && !target.IsDeclarative && kind != ProcedureReferenceKind.Resume)
            data.Edition.Warning(DiagnosticCatalog.DeclarativeReferencesNondeclarative,
                $"{verb} '{name}': '{name}' is in the nondeclarative portion of the procedure division, and this "
                + $"statement is in declarative section '{referencingSection!.Name}' — \"Within a declarative "
                + "procedure, there shall be no reference to any nondeclarative procedures except in a RESUME "
                + "statement\" (ISO §14.9.49.3 SR3)");
        return true;
    }

    /// <summary>One end of a procedure range, described the way SR11 divides them: the declarative section it
    /// belongs to, or the nondeclarative portion (ISO §14.3).</summary>
    private static string Portion(ResolvedProcedure end, string name) =>
        end.Section is { IsDeclarative: true } s
            ? $"'{name}' is a procedure of declarative section '{s.Name}'"
            : $"'{name}' is in the nondeclarative portion of the procedure division";

    /// <summary>⛔ ISO §14.9.39.3 SR6 — "Condition-name-1 shall be associated with a conditional variable"
    /// (kb/Work PB390), the Format-4 <c>SET condition-name-1 … TO TRUE</c> operand rule. COBOL has TWO kinds of
    /// condition-name (§8.4.4): one associated with a CONDITIONAL VARIABLE (a level-88 entry) and one
    /// associated with the status of an EXTERNAL SWITCH (a SPECIAL-NAMES <c>ON STATUS IS</c> / <c>OFF STATUS
    /// IS</c> name, §12.3.7). SR6 admits the first kind only — the second is set through Format 3,
    /// <c>SET mnemonic-name-1 TO ON|OFF</c>, whose SR5 requires an external switch "the status of which may be
    /// altered".
    /// <para>⛔ THE OLD MESSAGE DENIED THE FACT THE RULE TURNS ON. A switch-status name reached a
    /// <c>BoundUnsupported</c> reading "SET 'SW-ON' TO TRUE (not a condition-name)" — but SW-ON IS a
    /// condition-name; what it is not is a condition-name of a conditional VARIABLE, which is exactly what SR6
    /// says. Told it was not a condition-name at all, a user looks for a missing level-88 instead of reaching
    /// for Format 3. The rule's own discriminating case was also its silent one: it compiled clean and aborted
    /// the run unit.</para></summary>
    /// <param name="refText">The operand as written.</param>
    /// <param name="switchName">The implementor's external-switch name when the operand IS a switch-status
    /// condition-name, else null.</param>
    /// <returns>Always false: the operand is refused, HAVING REPORTED.</returns>
    public bool RejectSetConditionName(string refText, string? switchName)
    {
        if (switchName is not null)
            data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                $"SET '{refText}' TO TRUE — '{refText}' IS a condition-name, but the SPECIAL-NAMES kind: it "
                + $"names the status of external switch {switchName} (ISO §12.3.7), and §8.4.4.1 distinguishes "
                + "that kind from the level-88 kind, which is the one associated with a conditional variable. "
                + "\"Condition-name-1 shall be associated with a conditional variable\" (ISO §14.9.39.3 SR6), "
                + "so this operand is not admitted in Format 4. Set the switch through Format 3 — "
                + "SET <mnemonic-name> TO ON or OFF (SR5).");
        else
            data.Edition.Error(DiagnosticCatalog.UndefinedReference,
                $"SET '{refText}' TO TRUE — '{refText}' is not a condition-name: no level-88 entry and no "
                + "SPECIAL-NAMES switch-status name in this source element declares it, so the reference "
                + "identifies no resource (ISO §8.4.2.1), and §14.9.39.3 SR6 requires a condition-name "
                + "associated with a conditional variable.");
        return false;
    }

    /// <summary>The CORRESPONDING group-operand rule, ONE screen for its three spellings (kb/Work PB236), each
    /// asked as ITS OWN <see cref="CorrespondingOperandRule"/> row rather than as a shared approximation:
    /// MOVE §14.9.25.3 SR12 — "Identifier-3 and identifier-4 shall specify group data items and shall not be
    /// reference-modified" — and ADD §14.9.2.3 SR6 / SUBTRACT §14.9.44.3 SR6 — "Identifier-4 and identifier-5
    /// shall be alphanumeric group items, national group items, variable-length groups, or strongly-typed group
    /// items and shall not be described with level-number 66." The two spellings differ (only the arithmetic
    /// ones exclude level-66 and enumerate the admitted group kinds), so the message names the rule the CALLER
    /// is under rather than reciting all three at everyone.
    /// <para>⛔ THE ADMITTED KINDS ARE A SET, AND THE SCREEN ASKS THE SET (kb/Work PB392). This read
    /// <c>if (item.IsGroup) return true;</c> — a boolean where SR6 names FOUR of the five group kinds — so a
    /// <c>GROUP-USAGE BIT</c> group was admitted to ADD and SUBTRACT CORRESPONDING, and the statement then ran
    /// as a SILENT NO-OP: §14.7.6 rule 3 requires both items of an implied pair to be numeric and every child of
    /// a bit group is category boolean, which is precisely WHY SR6 leaves bit groups out. MEASURED at
    /// <c>--std 2023</c>: <c>SUBTRACT CORRESPONDING SRC FROM DST</c> over two <c>GROUP-USAGE BIT</c> groups
    /// compiled, ran and changed nothing. MOVE's SR12 admits every group kind — NOTE 5 under §14.9.25.4 GR11
    /// processes bit and national groups "as group items" for MOVE CORRESPONDING — so the fix is NOT a stricter
    /// screen for all three verbs but the rule's own set, per verb.</para>
    /// <para>⛔ AND THE LEVEL-66 ARM IS SR6'S, NOT SR12'S. Its message quoted "shall not be described with
    /// level-number 66" under whatever <c>clause</c> the caller passed, so a <c>MOVE CORRESPONDING</c> over a
    /// RENAMES entry attributed to §14.9.25.3 SR12 eight words SR12 does not contain — a citation that passes
    /// <c>cite.py --check</c> on the arithmetic spelling and is false on the MOVE one. (Its replacement then
    /// claimed "a RENAMES entry is not a group data item" under SR12 — false for the THROUGH form, which GR2
    /// makes one; kb/Work PB966.)</para>
    /// <para>⛔ THE LEVEL-66 CASE GETS ITS OWN REASON — AND ONLY WHERE THE RULE GIVES ONE (kb/Work PB966). The
    /// arithmetic SR6 excludes level 66 BY NAME, asked of <see cref="Place.DenotedItem"/> so the non-THROUGH
    /// alias (which resolves to data-name-2's place) cannot slip past it. MOVE's SR12 names no level number: a
    /// THROUGH alias IS an alphanumeric group item (§13.18.45.4 GR2) and is admitted, and a non-THROUGH alias is
    /// judged by data-name-2's attributes (GR1).</para>
    /// <para>⛔ AND THE OPERAND IS A <see cref="Place"/>, NOT A <see cref="DataItem"/>, BECAUSE SR12's SECOND
    /// HALF IS ABOUT THE REFERENCE (kb/Work PB390). "…and shall not be reference-modified" cannot be asked of
    /// the resolved item: a reference modifier rides on the <see cref="RefModPlace"/> DECORATOR and
    /// <c>PlaceDecorator.Item</c> answers with the INNER item, so a ref-modified group passed the group test
    /// and the prohibition went unchecked — the same blindness kb/Work PB347 measured at RELEASE. What caught
    /// it instead was an accident of the access-path factory (<c>CorrAccess.Create</c>'s default arm), under a
    /// storage-shape message naming neither the rule nor reference modification; and once that factory started
    /// switching on <c>Place.Undecorated</c> (kb/Work PB393) the accident stopped catching it at all, so
    /// <c>MOVE CORRESPONDING G1(1:3) TO G2</c> silently moved the WHOLE group with the modifier discarded. The
    /// arithmetic spellings need no separate rule for it: §8.4.3.3.4 GR6 makes the result of reference
    /// modification an ELEMENTARY data item ("The unique data item is considered to be an elementary data item
    /// without the JUSTIFIED clause"), which is none of the four group kinds their SR6 admits.</para></summary>
    /// <param name="operand">The resolved operand, DECORATIONS INTACT.</param>
    /// <param name="refText">The operand as written, for the message.</param>
    /// <param name="rule">The CALLER's own row — its clause, its admitted group kinds, and whether its text
    /// names level-66 (see <see cref="CorrespondingOperandRule"/>).</param>
    /// <returns>true when the operand is admitted.</returns>
    public bool CheckCorrespondingGroupOperand(Place operand, string refText, CorrespondingOperandRule rule)
    {
        for (Place p = operand; p is PlaceDecorator d; p = d.Inner)
            if (d is RefModPlace)
            {
                data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                    $"{rule.Verb} CORRESPONDING operand '{refText}' is reference-modified — the operands of a "
                    + "CORRESPONDING statement are group items, and reference modification yields an "
                    + "elementary data item (ISO §8.4.3.3.4 GR6: \"The unique data item is considered to be an "
                    + $"elementary data item without the JUSTIFIED clause\"), so it is not one (ISO {rule.Clause}). "
                    + "Name the group itself, or MOVE the modified reference to a matching item.");
                return false;
            }
        // ⛔ "DESCRIBED WITH LEVEL-NUMBER 66" IS AN IDENTITY QUESTION, SO IT ASKS THE DENOTED ITEM (kb/Work PB966).
        // The non-THROUGH alias resolves to data-name-2's own place (§13.18.45.4 GR1), whose Item is data-name-2 —
        // asked of Item, `ADD CORRESPONDING ALIAS-OF-GROUP TO G` passed SR6's "shall not be described with
        // level-number 66" because the entry it asked about was the 05, not the 66. DenotedItem is the entry the
        // NAME denotes (Place.DenotesAs), for both RENAMES forms.
        if (rule.ExcludesLevel66 && operand.DenotedItem?.Renames is not null)
        {
            data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                $"{rule.Verb} CORRESPONDING operand '{refText}' is described with level-number 66 — the "
                + $"operands \"shall not be described with level-number 66\" (ISO {rule.Clause})");
            return false;
        }
        // ⛔ AND WHERE THE RULE DOES NOT NAME LEVEL 66, A LEVEL-66 ENTRY IS JUDGED BY WHAT IT IS (kb/Work PB966).
        // MOVE's SR12 asks only for "group data items", and §13.18.45.4 GR2 — "When the THROUGH phrase is
        // specified, data-name-1 defines an alphanumeric group item that includes all elementary items" — makes a
        // THROUGH alias exactly that; GroupKindsOf answers Alphanumeric for it. What stood here refused EVERY
        // level-66 operand by name under SR12, which names no level number: legal source rejected. The
        // non-THROUGH alias takes data-name-2's attributes (GR1), so it is a group exactly when data-name-2 is.
        DataItem item = operand.Item;
        GroupKinds kinds = ItemCategory.GroupKindsOf(item);
        if ((kinds & rule.Admitted) != GroupKinds.None) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule, kinds is GroupKinds.None
            ? $"{rule.Verb} CORRESPONDING operand '{refText}' is an elementary item — both operands shall be "
              + $"group items (ISO {rule.Clause})"
            // The kind IS a group kind, and the rule names a narrower set: say which kind it is and which the
            // rule admits, because "shall be group items" would be a false explanation of a group's refusal.
            : $"{rule.Verb} CORRESPONDING operand '{refText}' is {ItemCategory.Face(item)} — the operands shall "
              + $"be {ItemCategory.Spell(rule.Admitted)} (ISO {rule.Clause})");
        return false;
    }

    /// <summary>⛔ THE ONE <c>record-name-1</c> OPERAND RULE, shared by the three statements whose general
    /// format prints that operand: WRITE (ISO §14.9.51.3 SR5), REWRITE (§14.9.35.3 SR1) and RELEASE
    /// (§14.9.32.3 SR1). All three say the same thing — the operand shall be "the name of a logical record" in
    /// a file description entry, and it "may be qualified". Returns the owning file, or false HAVING REPORTED;
    /// the shape is <see cref="ResolveFile"/>'s, so a fourth verb naming a record inherits the rule instead of
    /// re-deriving it.
    /// <para>⛔ THE PREDICATE IS IDENTITY, NOT CONTAINMENT (kb/Work PB347). What this replaced —
    /// <c>SequentialIoBinder.FileOfRecord</c> — walked the reference's data item UP to its top-level 01
    /// (<c>while (root.Parent is { } p) root = p;</c>) and asked whether THAT ROOT was one of a file's records,
    /// so the predicate actually enforced was <i>the reference lies somewhere INSIDE a record</i>. Every
    /// subordinate item of a record passed it, at all three verbs, silently: on an 8-byte SD whose record is
    /// <c>05 SR-KEY PIC X(3) / 05 SR-DATA PIC X(5)</c>, <c>RELEASE SR-DATA</c> compiled with no diagnostic at
    /// any <c>--std</c> and injected SR-DATA's 5-byte image, space-extended to the SD's 8, into the sorted
    /// result as a record the program never released — the released width is taken from the REFERENCE, so a
    /// wrong operand produces a wrong-WIDTH record rather than anything loud. The upward walk survives here as
    /// the EXPLANATION rather than the verdict: naming the record the operand is subordinate to is the one
    /// thing that tells the user what to write instead.</para>
    /// <para>⛔ AND record-name-1 IS NOT AN IDENTIFIER, so it is not reference-modifiable. A record-name is a
    /// user-defined word (§8.3.2.2.25, "A record-name identifies a record described in a record description
    /// entry"), and §5.2.4's operand table gives that operand type — "User-defined word, including
    /// qualification and subscripting if needed" — qualification and subscripting, and nothing else.
    /// §8.4.3.3.3 SR5 permits reference modification only "anywhere an identifier referencing a data item of
    /// class alphanumeric, boolean, or national is permitted", and its NOTE spells the consequence out:
    /// "Because the references to data items are restricted to identifiers, where data-name-n is used in a
    /// general format or syntax rule, then reference-modification is not permitted." The printed RELEASE format
    /// (rendered from the PDF, not read from the OCR) writes <c>record-name-1</c>, not <c>identifier-1</c>.
    /// A reference modifier rides on the <see cref="RefModPlace"/> DECORATOR and leaves <see cref="Place.Item"/>
    /// untouched, which is precisely why the containment test could not see it: <c>RELEASE SRT-REC(1:3)</c>
    /// released a 3-byte record into an 8-byte sort file.</para>
    /// <para>The diagnostic is the EXISTING <see cref="DiagnosticCatalog.StatementOperandRule"/> (COBOLNET1757)
    /// and no new code: that descriptor's own contract is that "the CODE is the identity of the MECHANISM (a
    /// bind-time operand refusal), the MESSAGE carries the rule", and this IS that mechanism. ISO §4.2.2 ¶2
    /// puts the indication at compile time; before this, WRITE and REWRITE staged it to
    /// <c>BoundUnsupported</c>, so <c>WRITE WS-REC</c> got a COBOLNET1756 deferral WARNING — the compiler's own
    /// gap — for what is the source's error.</para></summary>
    /// <param name="record">The resolved operand.</param>
    /// <param name="refText">The operand as written, for the message.</param>
    /// <param name="verb">WRITE | REWRITE | RELEASE.</param>
    /// <param name="rule">The caller's own syntax rule, quoted verbatim with its citation.</param>
    /// <param name="containedRule">The caller's own rule for a record-name "defined in a containing program and
    /// … referenced in a contained program" (WRITE §14.9.51.3 SR21, REWRITE §14.9.35.3 SR3), quoted verbatim.</param>
    /// <param name="file">The owning file when this returns true.</param>
    /// <returns>true when the reference IS a logical record of a file description entry visible here.</returns>
    /// <remarks>⛔ A RECORD CAN BE VISIBLE WHEN ITS FILE IS NOT (kb/Work PB1193). §13.18.27.3 SR1 b) lets a
    /// file-section level-1 entry carry its OWN GLOBAL clause, which makes the record-name a global name
    /// (§13.18.27.4 GR1) under an FD that has none — so a contained program resolves the record, but the file stays
    /// the container's alone, and SR21 / SR3 are exactly the rule for that case ("the file description entry … shall
    /// contain a GLOBAL clause"). The record is found in <see cref="DataBinder.ContainerLocalFiles"/> and refused BY
    /// THAT RULE; it used to fall to the "not a logical record of any file description entry" arm below, a false
    /// reason that would have become a silent acceptance the day that arm learned about global records.</remarks>
    public bool ResolveRecordName(Place record, string refText, string verb, string rule, string containedRule,
                                  [NotNullWhen(true)] out FileModel? file)
    {
        file = null;
        for (Place p = record; p is PlaceDecorator d; p = d.Inner)
            if (d is RefModPlace)
            {
                data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                    $"{verb} '{refText}' — record-name-1 is a record-name, not an identifier, so it shall not "
                    + "be reference-modified: ISO §5.2.4 gives a \"User-defined word, including qualification "
                    + "and subscripting if needed\" those two decorations and no other, and §8.4.3.3.3 SR5 "
                    + "permits reference modification only \"anywhere an identifier referencing a data item of "
                    + $"class alphanumeric, boolean, or national is permitted\". {rule}. Write the record-name "
                    + "alone; to send part of a record, use the FROM phrase or a MOVE.");
                return false;
            }
        if (FileWhoseRecordIs(record.Item) is { } owner) { file = owner; return true; }
        if (data.ContainerLocalFiles.FirstOrDefault(f => f.Records.Contains(record.Item)) is { } hiddenFile)
        {
            data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
                $"{verb} '{refText}' — '{refText}' is a logical record of '{hiddenFile.CobolName}', which a containing "
                + "program describes without the GLOBAL clause; the record's own GLOBAL clause (ISO §13.18.27.3 SR1 b)) "
                + $"makes the record-name visible here, not the file. {containedRule}. Specify GLOBAL in the file "
                + $"description entry of '{hiddenFile.CobolName}', or write the {verb} in the program that describes it.");
            return false;
        }
        DataItem root = record.Item;
        while (root.Parent is { } up) root = up;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"{verb} '{refText}' — {rule}"
            + (!ReferenceEquals(root, record.Item) && FileWhoseRecordIs(root) is { } containing
                ? $"; '{refText}' is subordinate to '{root.CobolName}', which is the logical record of "
                  + $"'{containing.CobolName}' — name that record, not one of its items"
                : $"; '{refText}' is not a logical record of any file description entry"));
        return false;
    }

    /// <summary>The file whose <see cref="FileModel.Records"/> contain this item — the IDENTITY test behind
    /// <see cref="ResolveRecordName"/>, asked of the reference's own item and (for the message only) of its
    /// top-level record. The second sweep is an inherited GLOBAL FD's record (ISO §13.18.30 — the record-names
    /// of a GLOBAL FD are GLOBAL names): the owning file is a CONTAINER's <see cref="FileModel"/>, present in
    /// this unit only through the <c>FilesByName</c> merge (<c>CallBindUnit</c>), so a contained program's
    /// WRITE/REWRITE of the owner's record resolves to the owner's ONE connector (IC233A's family; never a
    /// second mapping mechanism).</summary>
    private FileModel? FileWhoseRecordIs(DataItem item)
    {
        foreach (var f in data.Files)
            if (f.Records.Contains(item)) return f;
        foreach (var f in data.FilesByName.Values)
            if (f.Records.Contains(item)) return f;
        return null;
    }

    /// <summary>ISO §14.9.32.3 SR1's SECOND half — "…in a SORT-MERGE file description entry". The first half
    /// ("shall be the name of a logical record", and its no-reference-modification corollary) is
    /// <see cref="ResolveRecordName"/>'s, shared with WRITE and REWRITE; this is the part RELEASE alone has,
    /// and it is asked only of a reference that already IS a logical record — so <paramref name="file"/> is
    /// never null and the old "this name is not a record of any file description entry" arm has moved to the
    /// one place that can also say WHICH record the operand was subordinate to (kb/Work PB347). Only the STAGE
    /// was ever wrong here (kb/Work PB236): on a path the flow skipped, the program compiled AND ran to normal
    /// completion with no message at any time.</summary>
    public bool CheckReleaseRecord(FileModel file, string refText)
    {
        if (file.IsSortMerge) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"RELEASE '{refText}' — record-name-1 shall be the name of a logical record in a sort-merge (SD) "
            + $"file description entry (ISO §14.9.32.3 SR1); '{refText}' is a record of '{file.CobolName}', "
            + "which is described by an FD");
        return false;
    }

    /// <summary>ISO §14.9.34.3 SR1 — "File-name-1 shall be described by a sort-merge file description entry in
    /// the data division." The RETURN twin of <see cref="CheckReleaseRecord"/> (kb/Work PB236). The
    /// "not declared at all" case is NOT this rule and never reaches here: it is
    /// <see cref="ResolveFile"/>'s §8.4.2.1 verdict, and conflating the two told a user whose file simply had
    /// an FD that the name was undefined.</summary>
    public bool CheckReturnFile(FileModel file)
    {
        if (file.IsSortMerge) return true;
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule,
            $"RETURN '{file.CobolName}' — file-name-1 shall be described by a sort-merge (SD) file description "
            + "entry in the data division (ISO §14.9.34.3 SR1); this file is described by an FD");
        return false;
    }

    /// <summary>A SORT/MERGE operand list the statement's GENERAL FORMAT does not print (kb/Work PB236) —
    /// §14.9.24.2's MERGE format prints <c>USING file-name-2 {file-name-3}…</c> (two names minimum) and
    /// requires one of <c>OUTPUT PROCEDURE</c> or <c>GIVING</c>; §14.9.40.3's table-sort rules fix what a key
    /// may be. ISO §4.2.2 ¶2 puts violations of "the general formats and the explicit syntax rules" in the
    /// compile-time warning mechanism, and these used to be run-time louds.</summary>
    public bool RejectStatementOperand(string message)
    {
        data.Edition.Error(DiagnosticCatalog.StatementOperandRule, message);
        return false;
    }

    /// <summary>⛔ THE ONE ENTRY for the <c>SEARCH ALL</c> Format-2 operand rules, ISO §14.9.37.3 SR7–SR13
    /// (kb/Work PB445) — seven consecutive syntax rules that read the same two things and so were unenforced
    /// together. The rules themselves live in <see cref="SearchAllFormat2Rules"/> because they are predicates over
    /// a MODEL (the ordered OCCURS KEY phrase, and the WHEN decomposed into its Format-2 operands) rather than
    /// field tests, and the model is what makes the next Format-2 rule a predicate instead of a tree walk; this
    /// entry keeps the check catalog's single front door.
    /// <para>It takes a PARSE CONTEXT, unlike its neighbours here, and it has to: SR8 and SR9 are rules about the
    /// subscript AS WRITTEN — "shall be subscripted by the first index-name … shall not be followed by a '+' or a
    /// '–'" — a fact the bound operand has already erased. The <see cref="ReferenceResolver"/> comes from the
    /// caller for the same reason (the raw subscript segments are its to read), and so does the
    /// <see cref="ConditionBinder"/>: SR9 and SR11 are rules about WHICH level-88 a WHEN operand names, which is
    /// ISO §8.4.2.2 Format 2's question and is answered in exactly one place — <c>ConditionBinder.ConditionOf</c>
    /// (kb/Work PB443; the screen used to answer it again, by base word, and disagree with the bind).</para></summary>
    public bool CheckSearchAllFormat2(Core.SearchAllStatementContext s, DataItem table, ReferenceResolver refs,
                                     ConditionBinder conditions) =>
        new SearchAllFormat2Rules(data, refs, this, conditions).Check(s, table);

    /// <summary>⛔ THE ONE RESOLUTION AND SCREEN OF SEARCH's <c>identifier-1</c>, ISO §14.9.37.3 SR1–SR3 — the
    /// three ALL-FORMATS rules, which are three predicates over the SAME thing: the reference as written
    /// (kb/Work PB443). Both formats call it, because both print the same operand.
    /// <para>⛔ THE RESOLUTION IS HALF THE FIX AND IT IS THE HALF THAT WAS A WRONG ANSWER. Both binder arms read
    /// <c>dref.cobolWord()</c> — the BASE WORD — and then took <c>candidates.FirstOrDefault(i =&gt; i.IsTable)</c>,
    /// so with two groups each declaring a table <c>E</c>, <c>SEARCH E IN G2</c> searched whichever <c>E</c> was
    /// declared FIRST and quietly produced the wrong answer on legal, unambiguous COBOL; swapping the two
    /// declarations was the whole difference between right and wrong. §8.4.2.2 makes qualification the thing that
    /// establishes uniqueness, and §14.9.37.4 GR1 ("The SEARCH statement automatically varies the first or only
    /// index associated with identifier-1") fixes the expected answer: for a qualified reference, identifier-1 is
    /// the QUALIFIED table. <see cref="ReferenceResolver.ResolveTableOperand"/> is that one resolution.</para>
    /// <para>Staging: every verdict here is a COMPILE-TIME diagnostic (ISO §4.2.2 ¶2), never the
    /// <c>BoundUnsupported</c> run-time loud these used to be — which told the user WiseOwl COBOL had not implemented
    /// a feature, on a program that still compiled and shipped, and said nothing at all on an unexecuted path
    /// (kb/Work PB390's invariant; PB444 owns the SR4 arm). SR1, SR2's second sentence and SR3 report and let the
    /// bind CONTINUE, so one compile reports every violation it can see; only SR2's first sentence returns null,
    /// because without a table there is nothing left to bind.</para></summary>
    /// <param name="dref">identifier-1 as written.</param>
    /// <param name="verb">"SEARCH" or "SEARCH ALL", for the message.</param>
    /// <param name="refs">The resolver whose §8.4.2.2 matcher settles the qualification.</param>
    /// <returns>The table identifier-1 names, or null after reporting when the statement cannot bind.</returns>
    public DataItem? ResolveSearchTable(Core.DataReferenceContext dref, string verb, ReferenceResolver refs)
    {
        string written = dref.GetText();
        if (refs.ResolveTableOperand(dref) is not { } operand)
        {
            // A name identifying no item is already reported (COBOLNET1639 — §8.4.2.1 "In order to use a
            // resource, a statement shall contain a reference that uniquely identifies that resource"). What is
            // left is a reference that identifies a resource with NO data description — a special register — and
            // the rule it violates is SR2's first sentence.
            if (!refs.WasDiagnosed(dref))
                Identifier1(verb, written, "identifier-1 is a special register, which has no data description "
                    + "entry: \"the data description of identifier-1 shall contain an OCCURS clause with an "
                    + "INDEXED phrase\" (ISO §14.9.37.3 SR2)");
            return null;
        }
        var table = operand.Item;

        // SR2, first sentence — the OCCURS clause and its INDEXED phrase. Reported FIRST and alone: SR2's second
        // sentence and SR3 are both about "the level for which the SEARCH is applicable", which a non-table has
        // not got, so screening them against one would name a level that does not exist.
        if (!table.IsTable)
        {
            Identifier1(verb, written, $"'{table.CobolName}' is not a table — \"the data description of "
                + "identifier-1 shall contain an OCCURS clause with an INDEXED phrase\" (ISO §14.9.37.3 SR2)");
            return null;
        }
        if (table.IndexNames.Count == 0)
        {
            Identifier1(verb, written, $"the OCCURS clause of '{table.CobolName}' has no INDEXED phrase — \"the "
                + "data description of identifier-1 shall contain an OCCURS clause with an INDEXED phrase\" (ISO "
                + "§14.9.37.3 SR2). The search varies the first index associated with identifier-1 (§14.9.37.4 "
                + "GR1), so a table with no index cannot be searched; add INDEXED BY to the OCCURS clause.");
            return null;
        }

        // SR1 — the reference modifier. Not a shape this backend has not got round to: identifier-1 names a
        // TABLE, and a character slice of one is not a table.
        if (operand.IsReferenceModified)
            Identifier1(verb, written, "\"Identifier-1 shall not be reference-modified\" (ISO §14.9.37.3 SR1)");

        // SR2's second sentence — the ONLY written-form rule the subscript count can decide, and it is an UPPER
        // bound: a reference to a table item normally takes one subscript per OCCURS level (§8.4.2.3), SR2
        // removes the SEARCHED level's, so more subscripts than identifier-1 has SUPERORDINATE tables means one
        // of them is at that level.
        //
        // ⛔ THERE IS NO LOWER BOUND, AND SR3 IS NOT ONE. "Identifier-1 may be contained within one or more other
        // tables, for which the subscripting is still required" reads like a demand on identifier-1's own written
        // form, and §14.9.37.4 GR1 says in so many words that it is not: "The subscript that is used to determine
        // the occurrence of each superordinate table to search is specified by the user IN THE WHEN PHRASES.
        // Therefore, each appropriate subscript shall be set to the desired value before the SEARCH statement is
        // executed." The subscripting SR3 keeps required is required THERE — in the WHEN phrases, at run time —
        // not on identifier-1, so SR3 places no screen here at all; a lower bound enforced here REJECTS LEGAL
        // SOURCE. Measured, and this is how the mistake was caught: the CCVS suite writes the bare spelling
        // throughout — NC233A line 516 is `SEARCH ALL GRP2-ENTRY … WHEN SEC (IDX-1, IDX-2) = "(01,01)"` over
        // `02 GRP-ENTRY OCCURS 10 … 03 GRP2-ENTRY OCCURS 10`, the outer occurrence coming from IDX-1 in the WHEN
        // exactly as GR1 describes — and a lower bound failed six NIST programs (NC231A NC232A NC233A NC234A
        // NC237A NC238A) outright. Both spellings are legal: bare, and with the superordinate subscripts written
        // (which SR2 permits, banning only the searched level's).
        // ⛔ THE ARITY WALK IS WRITTEN ONCE, IN DataItem (kb/Work PB877 — it found FIVE copies, and
        // SubscriptAdmissionDriftTests.TheSr3ArityWalk_IsWrittenExactlyOnce fails if a sixth appears).
        // SubscriptArity counts identifier-1 ITSELF plus every table ancestor, and the !IsTable arm above
        // has already returned, so the SUPERORDINATE count is exactly one less.
        int superordinate = table.SubscriptArity - 1;
        if (operand.SubscriptCount > superordinate)
            Identifier1(verb, written, $"identifier-1 carries {operand.SubscriptCount} subscript"
                + $"{(operand.SubscriptCount == 1 ? "" : "s")} and '{table.CobolName}' has "
                + (superordinate == 0 ? "no enclosing table"
                    : superordinate == 1 ? "1 enclosing table" : $"{superordinate} enclosing tables")
                + " — \"identifier-1 shall not be subscripted at the level for which the SEARCH is applicable\" "
                + "(ISO §14.9.37.3 SR2). The search supplies that occurrence itself by varying the table's first "
                + "index (§14.9.37.4 GR1); an enclosing table's occurrence may be written here or left to the "
                + "WHEN phrases, which is where GR1 puts it.");
        return table;
    }

    /// <summary>The identifier-1 sink (ISO §14.9.37.3 SR1–SR3) — one code for one operand position, the same way
    /// COBOLNET1964–1966 group SR7–SR13 by the operand each is about.</summary>
    private bool Identifier1(string verb, string written, string message)
    {
        data.Edition.Error(DiagnosticCatalog.SearchIdentifier1Operand, $"{verb} {written}: {message}");
        return false;
    }

    /// <summary>ISO §14.9.37.3 SR4, an ALL-FORMATS rule — "If the END-SEARCH phrase is specified, the NEXT SENTENCE
    /// phrase shall not be specified" (kb/Work PB444). Both formats call it with their WHEN arms, so the two cannot
    /// disagree. "The NEXT SENTENCE phrase" is the WHEN phrase's own brace alternative — an arm that IS NEXT
    /// SENTENCE (<see cref="StatementBinder.IsNextSentenceArm"/>, the ONE reading of that alternative); a NEXT
    /// SENTENCE inside an IF nested in a WHEN arm is the IF's phrase, not this statement's. Reported and the bind
    /// continues (§4.2.2 ¶2 — the compile has failed, and one compile reports every violation it can see).</summary>
    /// <param name="verb">"SEARCH" or "SEARCH ALL", for the message.</param>
    /// <param name="endSearch">The END-SEARCH phrase, or null.</param>
    /// <param name="whenArms">The WHEN phrases' bodies, in source order.</param>
    public bool CheckSearchEndSearchNextSentence(string verb, ITerminalNode? endSearch,
                                                 IEnumerable<Core.StatementBlockContext> whenArms)
    {
        if (endSearch is null || !whenArms.Any(StatementBinder.IsNextSentenceArm)) return true;
        data.Edition.Error(DiagnosticCatalog.SearchEndSearchNextSentence,
            $"{verb}: the END-SEARCH phrase is specified and a WHEN phrase's body is NEXT SENTENCE — \"If the "
            + "END-SEARCH phrase is specified, the NEXT SENTENCE phrase shall not be specified\" (ISO §14.9.37.3 "
            + "SR4). Write CONTINUE in that WHEN phrase, or remove END-SEARCH and end the sentence with a period.");
        return false;
    }

    // ── INSPECT (ISO §14.9.22.3) — lifted at 10c ─────────────────────────────────────────────────────────────

    /// <summary>SR5 — a TALLYING counter shall be an elementary numeric data item.</summary>
    public bool CheckInspectTallyCounter(Place counter)
    {
        // "An elementary numeric data item" is §8.5.2.1 Table 2's class NUMERIC — a USAGE INDEX item is class INDEX, and
        // its storage PicInfo (category Numeric, Digits 0) must not pass for it (kb/Work PB1127, the R27/PB640 trap).
        if (counter.Item.Pic is { Category: PicCategory.Numeric, Usage: not Usage.Index }) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT TALLYING counter '{counter.Item.CobolName}' shall "
            + "be an elementary numeric data item (ISO §14.9.22.3 SR5)");
        return false;
    }

    /// <summary>SR7 — with REPLACING CHARACTERS, literal-3 shall be ONE character (an identifier-5 of another
    /// size is the runtime GR15 case — the runtime uses its first character, deterministic).</summary>
    public bool CheckInspectCharactersReplacement(BoundOperand rep)
    {
        if (rep is not BoundStringLiteral { Value.Length: not 1 } bad) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING CHARACTERS BY a {bad.Value.Length}-"
            + "character literal — literal-3 shall be one character in length (ISO §14.9.22.3 SR7)");
        return false;
    }

    /// <summary>SR6 — non-figurative literal-1 / literal-3 of unequal size is illegal (statically known, so
    /// diagnosed at compile time; the identifier-size mismatch is the runtime GR14 EC case). Called on the
    /// no-figurative-expansion path — the figurative rewrite (bind logic) stays in the binder.</summary>
    public bool CheckInspectReplacingSize(BoundOperand pat, BoundOperand rep, bool figurative)
    {
        if (pat is not BoundStringLiteral lp || rep is not BoundStringLiteral lr || figurative
            || lp.Value.Length == lr.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT REPLACING: literal '{lp.Value}' and replacement "
            + $"'{lr.Value}' differ in size (ISO §14.9.22.3 SR6 — equal size unless the replacement is figurative)");
        return false;
    }

    /// <summary>SR9 — CONVERTING literal-4 / literal-5 of unequal size (equal size unless literal-5 is
    /// figurative; the figurative expansion stays in the binder).</summary>
    public bool CheckInspectConvertingSize(BoundOperand from, BoundOperand to, bool figurative)
    {
        if (from is not BoundStringLiteral lf || to is not BoundStringLiteral lt || figurative
            || lf.Value.Length == lt.Value.Length) return true;
        data.Edition.Error("COBOLNET0846", $"INSPECT CONVERTING: '{lf.Value}' and '{lt.Value}' differ in "
            + "size (ISO §14.9.22.3 SR9 — equal size unless literal-5 is figurative)");
        return false;
    }

    /// <summary>⛔ THE ONE ELEMENTARY ARM OF INSPECT'S USAGE RULES (kb/Work PB856). ISO §14.9.22.3 SR1 and SR2 end in
    /// the same words — "an elementary item described implicitly or explicitly as usage display or national" — and
    /// SR2 used to be read as its DISPLAY half only, so a national identifier operand was refused while the
    /// diagnostic quoted the rule that admits it. Both rules read this predicate; usage through the ONE §8.5.2.1
    /// reader (<see cref="ItemCategory.UsageOf"/>), so an item inside a GROUP-USAGE NATIONAL group answers its
    /// implicit usage.</summary>
    internal static bool IsInspectElementaryCharacterItem(DataItem item) =>
        !ItemCategory.IsGroupItem(item) && ItemCategory.UsageOf(item) is Usage.Display or Usage.National;

    /// <summary>ISO §14.9.22.3 SR1 — identifier-1 "shall reference either an alphanumeric or national group item or
    /// an elementary item described implicitly or explicitly as usage display or national": the group kinds by
    /// the ONE group classifier (a bit, strongly-typed or variable-length group is none of the two named), the
    /// elementary arm shared with SR2.</summary>
    internal static bool IsInspectIdentifier1(DataItem item) =>
        (ItemCategory.GroupKindsOf(item) & (GroupKinds.Alphanumeric | GroupKinds.National)) != 0
        || IsInspectElementaryCharacterItem(item);

    /// <summary>SR2 — identifier-3 … identifier-n (every TALLYING / REPLACING / BEFORE / AFTER / CONVERTING
    /// identifier) shall reference an elementary item of usage display or national.</summary>
    public bool CheckInspectOperandUsage(Place p, string refText)
    {
        if (IsInspectElementaryCharacterItem(p.Item)) return true;
        data.Edition.Error("COBOLNET0847", $"INSPECT operand '{refText}' shall reference an elementary item "
            + "described implicitly or explicitly as usage display or national (ISO §14.9.22.3 SR2)");
        return false;
    }

    // ── The … INTO phrase's OWN syntax rules (READ §14.9.30.3 · RETURN §14.9.34.3) ───────────────────────────

    /// <summary>
    /// ⛔ <b>THE ONE ADMISSIBILITY SCREEN FOR AN <c>… INTO</c> RECEIVER</b> — READ (ISO §14.9.30.3 SR1 and SR2)
    /// and RETURN (§14.9.34.3 SR2 and SR3), four syntax rules over two verbs, reached from the ONE place all
    /// three INTO arms funnel through (<c>MoveBinder.BindIntoPhrase</c>), so the next INTO-bearing verb inherits
    /// the rule by construction rather than by an edit.
    ///
    /// <para><b>What it enforces.</b> (1) The admissibility pair, which is the whole of the rule: the phrase is
    /// admitted when the description entry has at most one record description (arm a), OR when identifier-1 AND
    /// EVERY record-name of the file <i>"describe an alphanumeric group item or an elementary item of category
    /// alphanumeric or category national"</i> (arm b). (2) The strongly-typed receiver's record-area COUNT.
    /// Both rules' wording, and the one place they differ, ride <see cref="IntoPhraseRules"/>.</para>
    ///
    /// <para><b>Why arm b) is worded over ALL the record-names.</b> Several record descriptions share ONE record
    /// area (§13.4.2), and the phrase moves <i>the record area</i> (§14.9.30.4 GR4 b). Which description the area
    /// currently holds is a run-time fact, so the only way the move's category can be known at compile time is
    /// for every description — and the receiver — to be a shape a group move copies without conversion.</para>
    ///
    /// <para><b>What this screen deliberately does NOT check.</b> The second sentence of §14.9.30.3 SR2 /
    /// §14.9.34.3 SR3 (<i>"shall be a strongly-typed group item of the same type as identifier-1"</i>) is the same
    /// predicate over the same pair that §14.9.25.3 SR2 already applies to this move's SENDER — which IS the
    /// record area — and <c>MoveTable16.Validity</c> refuses it under SR2 and <c>MoveBinder</c> reports it as COBOLNET1533, naming the phrase. A second
    /// copy here is one rule in two places, this repository's most reproducible defect. The COUNT obligation is
    /// checked here because nothing else can express it: with several record areas the MOVE screen inspects only
    /// <c>FileModel.AreaRecord</c>, so a file whose LARGEST record happens to be the right type passed
    /// everything (kb/Work PB337, probe 5).</para>
    ///
    /// <para><b>No edition gate, and none is needed</b> (feedback_four_editions_one_compiler — the question was
    /// asked, not skipped). The admissibility rule is an all-editions rule, present since COBOL-85. The two
    /// shapes that make the 2023 wording NARROWER than its '85 ancestor — <i>alphanumeric</i> group item rather
    /// than any group item, and category <i>national</i> — cannot be DECLARED below the edition that introduced
    /// them (TYPEDEF/TYPE and USAGE NATIONAL are COBOL-2002, DYNAMIC LENGTH is COBOL-2014), so one predicate is
    /// per-edition-correct by construction rather than by a gate that could drift. §13.4.5.3 SR3's record-less FD
    /// is likewise unobservable: the binder materializes the implied record area (FILES design D18), so arm a)
    /// answers the same at every edition.</para>
    /// </summary>
    /// <param name="file">The file whose record area is the phrase's sender.</param>
    /// <param name="receiver">identifier-1, as resolved.</param>
    /// <param name="rules">The verb's own rules row.</param>
    /// <returns>true when the phrase conforms; false after reporting.</returns>
    public bool CheckIntoReceiver(FileModel file, Place receiver, IntoPhraseRules rules)
    {
        int count = file.Records.Count;
        // Arm a) of the admissibility rule and the strong-receiver rule impose THE SAME condition, which is why
        // one predicate answers both: READ's read "no record description entry or only one" and "at most one
        // record area" (count ≤ 1); RETURN's read "only one" and "exactly one" (count = 1).
        bool admittedByCount = rules.AdmitsRecordCount(count);

        // ── §14.9.30.3 SR2 / §14.9.34.3 SR3, the COUNT obligation. A strongly-typed identifier-1 is unreachable
        //    below COBOL-2002 (the TYPE clause's edition), so this arm needs no gate of its own.
        //    The receiver's PLACE KIND is deliberately not filtered the way arm b) filters it below: a
        //    reference-modified or RENAMES view of a strongly-typed group — which §8.4.3.3.4 GR6 would make an
        //    elementary alphanumeric item, outside this rule's reach — cannot BE written. Measured, not assumed:
        //    `READ f INTO WS-STRONG(1:4)` draws COBOLNET1647 (§8.4.3.3.3 SR1, reference modification of a
        //    strongly-typed group item) and a RENAMES over one draws COBOLNET1532 (§13.18.57.3 SR4), both before
        //    this screen runs. A guard for a shape nothing can reach is a guard nothing can contradict. ──
        if (StrongTypeModel.IsStrongGroup(receiver.Item))
        {
            // ⛔ THE ADMISSIBILITY RULE IS NOT ALSO REPORTED FOR A STRONG RECEIVER, and that is a derivation, not
            // a preference: §13.18.29.4 GR3 excludes a strongly-typed group from the alphanumeric group items, so
            // arm b) can NEVER admit one, leaving arm a) — which is the condition just tested. For this receiver
            // the two rules say the same thing and SR2/SR3 is the one that names the reason; emitting both is
            // emitting one fact twice.
            if (admittedByCount) return true;
            data.Edition.Error(DiagnosticCatalog.IntoPhraseStrongReceiverRecordAreas,
                $"{rules.Statement} '{ReceiverFace(receiver)}': identifier-1 is a strongly-typed group item, so "
                + $"{rules.StrongReceiverCount} ({rules.StrongReceiverCite}) — {EntryFace(file, rules, count)}. The record descriptions "
                + "share one record area, so which type the area holds is not decidable from the statement.");
            return false;
        }

        // ── §14.9.30.3 SR1 / §14.9.34.3 SR2, arm a) then arm b). ──
        if (admittedByCount) return true;
        // BOTH halves of arm b), and the FIRST offender of each is kept so the message names WHICH item failed
        // rather than restating the rule. identifier-1 is reported in preference to a record: it is the operand
        // the programmer chose, and a record-name they may not have written.
        DataItem? badRecord = file.Records.FirstOrDefault(r => !AdmitsIntoRecord(r));
        bool badReceiver = !AdmitsIntoOperand(receiver);
        if (badRecord is null && !badReceiver) return true;

        string why = badReceiver
            ? $"identifier-1 '{ReceiverFace(receiver)}' is {ItemCategory.Face(receiver.Item)}"
            : $"the record '{badRecord!.CobolName ?? "FILLER"}' is {ItemCategory.Face(badRecord)}";
        data.Edition.Error(DiagnosticCatalog.IntoPhraseReceiverNotAdmissible,
            $"{rules.Statement} '{ReceiverFace(receiver)}': {rules.AdmissibilityCite} admits the phrase on two "
            + $"grounds and neither holds — a) {rules.AdmissibilityArmA}, but {EntryFace(file, rules, count)}; or b) identifier-1 and "
            + "ALL record-names associated with file-name-1 \"describe an alphanumeric group item or an "
            + $"elementary item of category alphanumeric or category national\", but {why}. Write the statement "
            + "without the INTO phrase and MOVE from the record-name you mean.");
        return false;
    }

    /// <summary>Arm b) applied to identifier-1. A REFERENCE-MODIFIED or level-66 RENAMES receiver is admitted
    /// WITHOUT reading the item behind it: §8.4.3.3.4 GR6 makes the unique data item a ref-mod identifies "an
    /// elementary item of class and category alphanumeric" whatever identifier-1 is described as, and a level-66
    /// THROUGH alias is "an alphanumeric group item" by §13.18.45.4 GR2 — the FIRST alternative arm b) admits,
    /// not the elementary one — so asking the underlying entry's category would reject a legal
    /// <c>READ F INTO WS-NUM(1:4)</c> on a rule that does not reach it. Both places are admitted, by two
    /// different clauses; the alias is a group item, NOT a composed elementary view (kb/Work PB430). The same
    /// reading <c>MoveTable16.OperandItem</c> takes of the alias for SR9 (kb/Work PB907).</summary>
    private static bool AdmitsIntoOperand(Place p) =>
        p is RefModPlace or RenamesPlace || AdmitsIntoRecord(p.Item);

    /// <summary>Arm b) applied to ONE record-name — "an alphanumeric group item or an elementary item of category
    /// alphanumeric or category national", which is exactly <see cref="ItemCategory.IsAlphanumericOrNational"/>:
    /// its group arm IS §13.18.29.4 GR3's alphanumeric group item and its national arm covers both the elementary
    /// national item and the GR2 b) national group. Never a second category walk.</summary>
    private static bool AdmitsIntoRecord(DataItem item) => ItemCategory.IsAlphanumericOrNational(item);

    /// <summary>How identifier-1 names itself in these two diagnostics.</summary>
    private static string ReceiverFace(Place p) => p.Item.CobolName ?? p.Item.CsName;

    /// <summary>How the description entry names itself in these two diagnostics. A method rather than a local,
    /// so the string is built on the DIAGNOSTIC path only — <see cref="CheckIntoReceiver"/> runs on every INTO
    /// phrase in the source element and almost all of them conform.</summary>
    private static string EntryFace(FileModel file, IntoPhraseRules rules, int count) =>
        $"this {rules.EntryFace} for '{file.CobolName}' has {count}";

    // ── Arithmetic composite of operands (ISO §14.7 rule 2) — lifted at 10e ──────────────────────────────────

    /// <summary>The per-edition COMPOSITE-OF-OPERANDS check (ISO §14.7 rule 2, NATIVE arithmetic, the four
    /// arithmetic statements ONLY — COMPUTE expressions are explicitly exempt, §8.8.1.2 r7): the hypothetical item
    /// superimposing the statement's fixed-point operands aligned on their decimal points shall not exceed the
    /// edition's digit cap (18 at COBOL-85; the 2023 text says 31). Float/binary-native operands are excluded
    /// (rule 2b — the composite is then over the remaining operands).</summary>
    public bool CheckComposite(string verb, IEnumerable<BoundExpr> operands, IEnumerable<Receiver> receivers)
    {
        if (data.Options.Arithmetic != ArithmeticMode.Native) return true;   // §14.7 r2 applies to native only
        int maxInt = 0, maxFrac = 0;
        void Shape(int digits, int scale)
        {
            maxInt = Math.Max(maxInt, digits - scale);   // a negative (P-scaled) scale ADDS integer positions
            maxFrac = Math.Max(maxFrac, Math.Max(0, scale));
        }
        // ISO §14.7.7 rule 2b: a data item of usage binary-char/-short/-long/-double is EXCLUDED from the composite
        // (the composite is then over the OTHER operands, still capped at 31) — like a floating-point operand. Those
        // four picture-less usages carry Category Numeric + IsFloat false, so without this guard they were wrongly
        // superimposed (BINARY-DOUBLE contributes up to 20 integer digits) and pushed a conforming program past 31
        // (COBOLNET0805). COMP-5 is NOT in rule 2b's list and stays counted; float usages are already IsFloat. (CA6.)
        // Category NUMERIC-EDITED is IN the composite (kb/Work PB155): a MULTIPLY/DIVIDE GIVING resultant is a
        // composite member (§14.9.26.3 SR4 counts ALL operands; §14.9.12.3 SR4 excludes only REMAINDER) and
        // §14.9.26.3/§14.9.12.3 SR2 admit it as numeric-edited — requiring Category Numeric silently dropped it.
        // A floating-point-FORM edited mask (IsFloatEdited) is excluded as a DOCUMENTED READING, not rule 2b's
        // text (its list names usages and float literals, never an edited form): the composite is a
        // superimposition "aligned on their decimal points" (§14.7.7 r2's own definition) and an E-form mask
        // has no fixed decimal point to align — the same property that puts every listed exclusion outside
        // the superimposition. Its DigitPositions is 0 anyway, so inclusion would contribute nothing.
        static bool InComposite(PicInfo p) =>
            p is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false, IsFloatEdited: false }
            && p.Usage is not (Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble);
        void OfExpr(BoundExpr e)
        {
            switch (e)
            {
                case BoundNumRef { Place.Item.Pic: { } p } when InComposite(p):
                    var (d, s) = data.StoredShapeOf(p);   // the ONE fixed-point geometry (edited: mask-derived)
                    Shape(d, s);
                    break;
                // §14.7.7 rule 2a/2b: a FLOATING-POINT literal is excluded from the composite — counting the
                // E-form's characters here rejected legal `ADD 1.5E+3 TO <PIC 9(28)>` (kb/Work PB155).
                case BoundNumLiteral lit when !CobolNet.Common.NumericLiteral.IsFloatingPointForm(lit.Text):
                    string t = lit.Text.TrimStart('+', '-');
                    int dot = t.IndexOf('.');
                    Shape(t.Count(char.IsAsciiDigit), dot < 0 ? 0 : t.Length - dot - 1);
                    break;
            }
        }
        foreach (var e in operands) OfExpr(e);
        foreach (var r in receivers)
            if (r.Place.Item.Pic is { } rp && InComposite(rp))
            {
                var (rd, rs) = data.StoredShapeOf(rp);
                Shape(rd, rs);
            }

        // The cap is 31 at EVERY edition (ISO §14.7 rule 2a — the 2023 text). A COBOL-85-specific tightening to
        // 18 was considered and REFUTED by the conformance corpus itself: CCVS-85 NC101A multiplies 9(3)V9(3) by
        // 9(18) (composite 21) as a deliberate SIZE ERROR test, and every conforming '85 implementation accepts
        // it — so the 18-digit figure does not govern the composite (it caps '85 PICTURE/literal capacity only).
        int composite = maxInt + maxFrac;
        if (composite <= 31) return true;
        data.Edition.Error("COBOLNET0805",
            $"{verb}: the composite of operands spans {composite} digits ({maxInt} integer + {maxFrac} fraction); "
            + "ISO/IEC 1989 caps the composite of operands at 31 digits (§14.7 rule 2)");
        return false;
    }

    // ── INITIALIZE (ISO §14.9.20.3) — lifted at 10e ──────────────────────────────────────────────────────────

    /// <summary>§14.9.20.3 SR6 ("the same category shall not be repeated in a REPLACING phrase") AND §5.2.6.4
    /// ("any single alternative shall be specified only once") — ONE check, because a category-name is a SET of
    /// category words and the two rules are the same ban at two scopes: SR6 across the phrase's items, §5.2.6.4
    /// within one item's category-name. <paramref name="namedSoFar"/> is the caller's running union.</summary>
    public bool CheckInitializeCategoryUnique(InitializeCategorySet namedSoFar, InitializeCategory cat)
    {
        if (!namedSoFar.Contains(cat)) return true;
        data.Edition.Error("COBOLNET0834",
            $"INITIALIZE REPLACING repeats the category-name {InitializeCategories.Spelling(cat)} "
            + "(ISO §14.9.20.3 SR6 — each category at most once in the phrase; §5.2.6.4 — each alternative at "
            + "most once within one category-name)");
        return false;
    }

    /// <summary>ISO §14.9.20.2 + §5.2.6.3 — the VALUE phrase's <c>{ ALL | category-name }</c> is a BRACE, and
    /// "one of the alternatives contained within the braces shall be explicitly specified or is implicitly
    /// selected": neither is implicitly selected here, so a bare <c>TO VALUE</c> is not conforming source. It was
    /// accepted (and read as ALL) on the strength of a §14.9.20.2 "note 2" the clause does not carry — kb/Work
    /// PB415. The binder recovers as ALL after this diagnostic, so one statement yields one message.</summary>
    public void CheckInitializeValueChoicePresent() =>
        data.Edition.Error(DiagnosticCatalog.InitializeValueChoiceMissing,
            "INITIALIZE ... TO VALUE requires ALL or a category-name before VALUE (ISO §14.9.20.2 — the braced "
            + "choice is mandatory, §5.2.6.3)");

    /// <summary>ISO §14.9.20.3 SR3 — "for each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or
    /// PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be
    /// specified". literal-1 is what the rule excludes, because §14.9.20.4 GR4 makes the implicit statement a SET
    /// for exactly those five categories and no SET format (§14.9.39) admits a literal sending operand.</summary>
    public void CheckInitializeReplacingSetCategoryIdentifier(InitializeCategorySet cats, string literalText)
    {
        foreach (var cat in InitializeCategories.All)
            if (cats.Contains(cat) && InitializeCategories.IsSetForm(cat))
            {
                data.Edition.Error(DiagnosticCatalog.InitializeReplacingPointerNeedsIdentifier,
                    $"INITIALIZE REPLACING {InitializeCategories.Spelling(cat)} ... BY the literal {literalText}: "
                    + "identifier-2 shall be specified (ISO §14.9.20.3 SR3 — the implicit statement for this "
                    + "category is a SET, §14.9.20.4 GR4, and no SET format admits a literal sending operand)");
                return;   // one message per REPLACING item, naming the first offending category
            }
    }

    /// <summary>ISO §14.9.20.3 SR4 for the SET-form categories — "a SET statement with identifier-2 as the sending
    /// operand and an item of the specified category as the receiving operand shall be valid". §14.9.39's pointer
    /// and object-reference formats admit only a sending operand of the receiver's own category, so category
    /// agreement is the necessary condition; an object-reference pair's §9.3.8.2 class conformance is the OO
    /// lane's rule and is not re-stated here.</summary>
    public void CheckInitializeReplacingSetCategoryAgrees(
        InitializeCategory cat, InitializeCategory? senderCat, string senderText)
    {
        string spelled = InitializeCategories.Spelling(cat);
        data.Edition.Error(DiagnosticCatalog.InitializeReplacingSetCategoryMismatch,
            $"INITIALIZE REPLACING {spelled} ... BY {senderText}, which is "
            + (senderCat is { } sc ? $"an item of category {InitializeCategories.Spelling(sc)}" : "not a data item")
            + $": ISO §14.9.20.3 SR4 requires that `SET <a {spelled} item> TO identifier-2` be valid, and "
            + "§14.9.39 admits only a sending operand of the receiver's own category");
    }

    /// <summary>ISO §14.9.20.3 SR5 — "The data description entry for the data item referenced by identifier-1
    /// shall not contain a RENAMES clause" (a level-66 entry).
    /// <para>⛔ IT TAKES THE <b>RESOLVED</b> ITEM (kb/Work PB416). It used to take the candidate list a NAME
    /// lookup returns, and its only caller asked it on the arm where the reference did NOT resolve — an arm a
    /// level-66 entry never reaches, because it resolves perfectly well. The check was written, cited, covered by
    /// a diagnostic code, and unreachable; the violation reached the user as an unhandled run-time
    /// <c>NotImplementedCobolFeatureException</c>. Asking the resolved item is also what makes the rule
    /// QUALIFICATION-correct: `INITIALIZE R66 IN REC-2` names one entry, and a name lookup that returns every
    /// same-named candidate answers about the wrong one as readily as the right one.</para>
    /// <para>⛔ AND IT ASKS THE ENTRY THE NAME DENOTES, NOT THE ONE WHOSE STORAGE IS USED (kb/Work PB966). The
    /// non-THROUGH alias resolves to data-name-2's own place (§13.18.45.4 GR1), whose <see cref="Place.Item"/> is
    /// data-name-2 — so `INITIALIZE GALIAS` over `66 GALIAS RENAMES SG` compiled and initialized SG, measured,
    /// while its THROUGH sibling was refused. <see cref="Place.DenotedItem"/> answers the level-66 entry for both
    /// forms; a reference-modified operand (whose DenotedItem is null — there is no declared item) is asked
    /// through its inner reference, as before.</para></summary>
    public bool CheckInitializeTargetRenames(string refText, Place place)
    {
        Place named = place;
        while (named.DenotedItem is null && named is PlaceDecorator d) named = d.Inner;
        if ((named.DenotedItem ?? place.Item).Renames is null) return true;
        data.Edition.Error("COBOLNET0835",
            $"INITIALIZE '{refText}' — the data description entry for identifier-1 shall not contain a RENAMES "
            + "clause (ISO §14.9.20.3 SR5)");
        return false;
    }

    // ── Sequential file I/O (ISO §14.9.27 / §14.9.51) — lifted at 10h ───────────────────────────────────────

    /// <summary>§14.9.27.3 SR8 — OPEN … SHARING WITH ALL OTHER (clause or phrase) requires a LOCK MODE clause.</summary>
    /// <summary>§13.4.6.3 SR3/SR4 — a sort-merge (SD) file-name may be referenced ONLY by SORT / MERGE /
    /// RELEASE / RETURN (and a SORT/MERGE USING/GIVING). Every other input-output statement rejects at BIND
    /// time; the old posture was a patchwork — a bind error at UNLOCK, a runtime loud stage at
    /// OPEN/WRITE/READ/REWRITE, and NOTHING at CLOSE / DELETE / DELETE FILE, where the statement then ran
    /// against an UNREGISTERED connector and the fail-open registry reported the SUCCESSFUL '00' (kb/Work
    /// PB140). Returns the message (for the caller's BoundUnsupported placeholder), or null for a non-SD
    /// file.</summary>
    public string? ScreenSortMergeFile(FileModel file, string verb)
    {
        if (!file.IsSortMerge) return null;
        string msg = $"{verb} may not name the sort-merge file '{file.CobolName}' — an SD file-name may "
            + "appear only in SORT/MERGE/RELEASE/RETURN (ISO §13.4.6.3 SR3/SR4)";
        data.Edition.Error(DiagnosticCatalog.SortMergeFileInIoStatement, msg);
        return msg;
    }

    /// <summary>⛔ THE ONE SCREEN for the L1–L3 "phrase written where this statement's syntax rules forbid it"
    /// leniency family (kb/Work PB144). Three syntax rules across READ/REWRITE/DELETE say a phrase "shall not be
    /// specified" in a particular access mode or organization, and all three were TOLERATED unconditionally with
    /// a "CCVS-lenient" comment and no strict arm — so at <c>--std 2023</c> strict the compiler accepted source
    /// the standard forbids, silently (measured, not assumed: a REWRITE with INVALID KEY on both a
    /// sequential-organization and a relative-sequential-access file compiled clean and printed no diagnostic).
    /// <para>The severity decision routes through <see cref="EditionContext.Removed"/> — THE policy seam, which
    /// already carries documented-dialect-leniency gating as well as removed-construct gating: an ERROR under
    /// strict, a WARNING under <c>--permissive</c> with the bind UNCHANGED, so the CCVS-85 corpus that motivated
    /// the leniency keeps compiling and keeps its existing semantics. Never a local <c>Permissive</c> test and
    /// never a parallel <c>Lenient()</c> method.</para>
    /// <para>Returns true when the phrase is forbidden (the caller may still bind it — under permissive it must,
    /// and under strict the compile has already failed), so a call site reads as a screen, not a branch.</para></summary>
    /// <param name="forbidden">Whether the rule's condition holds — computed by the caller, since only it knows
    /// the statement's own access-mode/organization test.</param>
    /// <param name="phrase">The phrase as the program wrote it, e.g. "INVALID KEY".</param>
    /// <param name="verb">The statement, e.g. "REWRITE".</param>
    /// <param name="because">The rule's own condition, quoted from it, e.g. "a file with sequential organization".</param>
    /// <param name="citation">The §/SR, e.g. "ISO §14.9.35.3 SR2".</param>
    public bool ScreenForbiddenPhrase(bool forbidden, string phrase, string verb, string because, string citation)
    {
        if (!forbidden) return false;
        data.Edition.Removed(DiagnosticCatalog.IoPhraseForbiddenHere.Code,
            $"the {phrase} phrase shall not be specified for a {verb} statement that references {because} "
            + $"({citation})");
        return true;
    }

    /// <summary>⛔ THE ONE ENFORCEMENT OF ISO §14.9.30.3 SR6 — "None of the phrases ADVANCING, AT END, NEXT,
    /// NOT AT END, or PREVIOUS shall be specified if ACCESS MODE RANDOM is specified in the file control entry
    /// for file-name-1." Both READ binder arms call THIS: the rule is about the file control entry's access
    /// mode, which every organization has, and a copy per arm is how §14.9.30.3's other phrase rules came to be
    /// enforced on one arm only (kb/Work PB334). ⚠ On the SEQUENTIAL-organization arm it is now
    /// DEFENCE IN DEPTH, not the only guard: §12.4.5.5.2 SR2 ("The DYNAMIC and RANDOM phrases shall not be
    /// specified for a sequential file") is screened at the FILE CONTROL ENTRY as of kb/Work PB692
    /// (<c>DataBinder.BindFileControl</c>, COBOLNET1858), so <c>IsSequential &amp;&amp; AccessMode is Random</c>
    /// no longer reaches a statement at all. It stays because it is the STATEMENT's own rule and costs a
    /// field test — never because the entry rule might be missing.
    /// <para>The message names EVERY phrase the statement actually wrote — the at-end bracket carries choice
    /// indicators (§5.2.6.4), so AT END and NOT AT END can both be present and both are named.</para></summary>
    public bool CheckReadRandomAccessPhrases(FileModel file, bool advancing, bool atEnd, bool notAtEnd,
        bool next, bool previous)
    {
        if (file.AccessMode != FileAccessMode.Random) return true;
        var present = new List<string>();
        if (advancing) present.Add("ADVANCING");
        if (atEnd) present.Add("AT END");
        if (notAtEnd) present.Add("NOT AT END");
        if (next) present.Add("NEXT");
        if (previous) present.Add("PREVIOUS");
        if (present.Count == 0) return true;
        ScreenForbiddenPhrase(true, string.Join(" / ", present), "READ",
            "a file whose file control entry specifies ACCESS MODE RANDOM", "ISO §14.9.30.3 SR6");
        return false;
    }

    /// <summary>⛔ THE ONE ENFORCEMENT OF ISO §14.9.30.3 SR10 — "The KEY phrase may be specified only if
    /// ORGANIZATION IS INDEXED is specified in the file control entry for file-name-1." The rule names an
    /// ORGANIZATION, so it cannot live on one organization's binder arm: it was enforced only in
    /// <c>KeyedIoBinder.BindRead</c> (which reaches RELATIVE) while <c>SequentialIoBinder.BindRead</c> — the arm
    /// every SEQUENTIAL and LINE SEQUENTIAL file takes — never called <c>readKey()</c> at all, so the phrase was
    /// parsed and dropped without a word (kb/Work PB334). Returns true when the organization admits the
    /// phrase.</summary>
    public bool CheckReadKeyOrganization(FileModel file)
    {
        if (file.Organization == FileOrganization.Indexed) return true;
        data.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}': the KEY phrase may be "
            + "specified only when ORGANIZATION IS INDEXED (ISO §14.9.30.3 SR10)");
        return false;
    }

    /// <summary>⛔ THE ONE SCREEN for the MIRROR of <see cref="ScreenForbiddenPhrase"/>: a phrase the printed
    /// general format shows WITHOUT brackets, and that the program omitted (kb/Work PB350). ISO §5.2.6.2 makes
    /// brackets the ONLY convention that lets a portion of a format be omitted and §5.2.2 makes an underlined
    /// keyword required subject to those conventions, so an unbracketed phrase line is MANDATORY — a grammar
    /// that writes it <c>phrase?</c> under-rejects, which is the falsely-PERMISSIVE twin of the transcription's
    /// falsely-restrictive OCR bias.
    /// <para>⚠ THIS IS A HARD ERROR AT EVERY EDITION, deliberately NOT routed through
    /// <see cref="EditionContext.Removed"/> the way its forbidden-phrase mirror is. That seam exists because the
    /// CCVS-85 corpus really does write the phrases §14.9.30.3 SR6 forbids, and tolerating them leaves the bind
    /// UNCHANGED. Nothing is lenient about the omission this screen catches: measured over
    /// <c>tests/conformance</c> and <c>tests/nist</c>, every RETURN in the corpus writes its AT END, and a
    /// permissive arm would keep alive exactly the wrong answer PB350 reports — control falling THROUGH a
    /// RETURN at end of data onto an undefined record area (§14.9.34.4 GR3).</para>
    /// <para>⚠ It is screened HERE and not in the grammar on purpose. The grammar already produces a tree the
    /// binder can read, and a bind-time diagnostic can NAME the omitted phrase, the statement and the clause,
    /// where an ANTLR parse error can only report an unexpected token. Making the phrase mandatory in the
    /// grammar would also make the §14.9.34.3 SR4 reversed spelling report as a token-level surprise. The
    /// grammar rule carries a comment pointing here; <c>RequiredFormatPhraseDriftTests</c> re-derives from the
    /// transcription that RETURN's AT END is still the only §14.9 format line this applies to.</para>
    /// <para>Returns true when the phrase was omitted, so a call site reads as a screen, not a branch. The
    /// caller may still bind the statement — the compile has already failed.</para></summary>
    /// <param name="omitted">Whether the phrase is absent — computed by the caller, which alone knows how its
    /// own parse tree spells the phrase's presence.</param>
    /// <param name="phrase">The phrase as the format prints it, e.g. "AT END".</param>
    /// <param name="verb">The statement, e.g. "RETURN".</param>
    /// <param name="because">Why the format makes it mandatory, in the format's own terms.</param>
    /// <param name="citation">The §, e.g. "ISO §14.9.34.2".</param>
    public bool ScreenOmittedRequiredPhrase(bool omitted, string phrase, string verb, string because, string citation)
    {
        if (!omitted) return false;
        data.Edition.Error(DiagnosticCatalog.FormatRequiredPhraseOmitted,
            $"the {phrase} phrase is required on every {verb} statement and is omitted — {because} "
            + $"({citation}; brackets are the only convention that makes a portion of a general format "
            + "omissible, ISO §5.2.6.2)");
        return true;
    }

    /// <summary>⛔ THE ONE AND ONLY ENFORCEMENT OF ISO §14.9.27.3 SR8 — "When file-name-1 is not subject to an
    /// APPLY COMMIT clause, then if the sharing phrase is omitted from the OPEN statement and the ALL phrase is
    /// specified in the SHARING clause of the file control entry for file-name-1 or if the ALL phrase is
    /// specified on the OPEN statement, the LOCK MODE clause shall be specified in the file control entry for
    /// file-name-1." The rule is a SYNTAX RULE OF THE OPEN STATEMENT about file-name-1, so the OPEN binder is
    /// its only possible home; a second copy at the file control entry (deleted, kb/Work PB319) had to drop the
    /// antecedent — the antecedent names an OPEN statement the SELECT cannot see, and its leading conjunct names
    /// an I-O-CONTROL clause bound later — and so rejected legal source while double-reporting the violations.
    /// <para>THE ANTECEDENT IS WRITTEN OUT IN FULL, IN THE RULE'S OWN ORDER. (1) The leading conjunct: a file
    /// subject to an APPLY COMMIT clause is EXEMPT, and the exemption is load-bearing rather than decorative
    /// because §12.4.5.9.3 SR1 forbids writing a LOCK MODE clause "for a file that is the subject of an APPLY
    /// COMMIT clause" — without the exemption the two rules would make such a file unwritable. It is reachable
    /// today: COBOLNET1709 declines the clause but is <c>PermissiveInert</c>, so <c>--permissive</c> compiles the
    /// program. (2) The two disjuncts collapse to ONE test on the EFFECTIVE sharing mode, which is what the
    /// caller passes: <c>sharing ?? file.Sharing</c> is the ALL phrase when the OPEN group wrote one and the
    /// file control entry's clause when it did not (§14.9.27.4 GR23), which is exactly "[ALL on the OPEN]" OR
    /// "[phrase omitted AND the SELECT says ALL]". A phrase that is present and is NOT ALL satisfies neither
    /// disjunct, and a file with no OPEN statement is never asked at all — both are legal.</para>
    /// <para>Pure: returns false when the rule is violated so a call site reads as a screen.</para></summary>
    public bool CheckOpenSharingAllOther(FileModel file, SharingMode? effectiveSharing)
    {
        if (file.SubjectToApplyCommit) return true;   // SR8's leading conjunct — cf. §12.4.5.9.3 SR1
        if (!(effectiveSharing is SharingMode.AllOther && file.LockMode is null)) return true;
        data.Edition.Error("COBOLNET1512", $"OPEN of file '{file.CobolName}' with SHARING WITH ALL OTHER "
            + "requires the file to have a LOCK MODE clause (ISO §14.9.27.3 SR8)");
        return false;
    }

    /// <summary>⛔ THE ONE SCREEN for the OPEN statement's per-file-name TAPE PHRASE — both alternatives of
    /// §14.9.27.2's <c>[ REVERSED | WITH NO REWIND ]</c> bracket, over ONE table
    /// (<see cref="OpenTapePhraseRule"/>, one NAMED row per phrase behind
    /// <see cref="OpenTapePhraseRule.For"/>). Each phrase constrains the same two axes — the file's ORGANIZATION
    /// and its group's OPEN MODE — so the table has a column per axis and the next tape phrase is a ROW, not a
    /// third pair of methods. Returns true when the phrase stands; false when it was REPORTED and the caller
    /// shall drop it (both rules are screens, not branches: §14.9.27.3's violations are reported and the
    /// statement still binds, so the run-time phrase effects only ever see combinations their general rules
    /// define — kb/Work PB317/PB318, and kb/Work PB668 for REVERSED).
    /// <para>NO REWIND's two rules are §14.9.27.3 SR5 — <i>"The NO REWIND phrase may be specified only for
    /// sequential files"</i>, the EXACT twin of the CLOSE rule §14.9.6.3 SR1 that <c>BindClose</c> enforces as
    /// COBOLNET1693, which is why the OPEN half went unchecked for so long — and SR6, <i>"The NO REWIND phrase
    /// may be specified only when the INPUT or OUTPUT phrase is specified"</i>, which §14.9.27.4 GR12 a)
    /// corroborates by naming only EXTEND as the mode that suppresses the positioning the phrase talks about.
    /// SR5's predicate is ORGANIZATION, not access mode — §9.1.7.2 puts record sequential and line sequential
    /// both under sequential organization — and it is what makes §14.9.27.4 GR11 answerable: GR11 keys on the
    /// storage medium and a relative or indexed file is category (d) Non-sequential under §14.9.6.4 GR2 — a
    /// category for which §14.9.27.4 defines no NO REWIND effect in either GR11 or GR12.</para>
    /// <para>REVERSED's two rules come from the COBOL-85 general format this repository does not hold
    /// (VERSION_CHANGE_REFERENCE row 7.12): the INPUT group only, and — one step NARROWER than its sibling —
    /// record sequential organization only, because the phrase's whole effect is the backward retrieval
    /// §14.9.30.4 GR21 c) defines and §14.9.30.3 SR7 denies the backward walk to LINE SEQUENTIAL
    /// organization.</para></summary>
    public bool CheckOpenTapePhrase(FileModel file, BoundOpenMode mode, BoundOpenTapePhrase phrase)
    {
        if (phrase is BoundOpenTapePhrase.None) return true;
        var rule = OpenTapePhraseRule.For(phrase);
        if (!rule.AdmitsOrganization(file))
        {
            data.Edition.Error(rule.OrganizationCode,
                $"OPEN '{file.CobolName}' {rule.Phrase} — the phrase may be specified only for "
                + $"{rule.OrganizationFace} files ({rule.OrganizationClause})");
            return false;
        }
        if (!rule.Modes.Contains(mode))
        {
            data.Edition.Error(rule.OpenModeCode,
                $"OPEN {OpenModeFace(mode)} '{file.CobolName}' {rule.Phrase} — the phrase may be specified only "
                + $"when the {string.Join(" or ", rule.Modes.Select(OpenModeFace))} phrase is specified "
                + $"({rule.OpenModeClause})");
            return false;
        }
        return true;
    }

    /// <summary>⛔ THE ONE SCREEN for the OPEN statement's syntax rules that pair a group's OPEN MODE with a
    /// property of the FILE (kb/Work PB318) — the two rules that had no site at all:
    /// <list type="bullet">
    /// <item>§14.9.27.3 SR1 — <i>"The OPEN statement for a report file shall not contain the INPUT phrase or the
    /// I-O phrase."</i> §13.18.46.3 SR3 states the same boundary from the file's side ("the OPEN statement with the
    /// OUTPUT or EXTEND phrase").</item>
    /// <item>§14.9.27.3 SR2 — <i>"The EXTEND phrase shall be specified only if the access mode of the file
    /// connector referenced by file-name-1 is sequential and the LINAGE clause is not specified in the file
    /// description entry for file-name-1."</i> ONE sentence, TWO conjuncts, and it keys on the ACCESS MODE, not the
    /// organization, so EXTEND stays legal on a relative or indexed file with ACCESS MODE IS SEQUENTIAL. Both
    /// conjuncts are reported when both fail — every rule a statement violates is reported (kb/Work PB352).</item>
    /// </list>
    /// The rules that pair the TAPE PHRASE with the mode (SR5/SR6) are <see cref="CheckOpenTapePhrase"/>'s; SR8 is
    /// <see cref="CheckOpenSharingAllOther"/>'s; SR9 (I-O with a FORMAT clause) is discharged by the FORMAT
    /// clause's documented non-support (Annex A.4.8). Pure: false when a rule was violated and reported; the
    /// statement still binds (a screen, not a branch — the compile has already failed).</summary>
    public bool CheckOpenModeForFile(FileModel file, BoundOpenMode mode)
    {
        bool ok = true;
        if (file.IsReportFile && mode is BoundOpenMode.Input or BoundOpenMode.IO)
        {
            data.Edition.Error(DiagnosticCatalog.OpenReportFileMode,
                $"OPEN {OpenModeFace(mode)} '{file.CobolName}' — a report file shall not be opened INPUT or I-O "
                + "(ISO §14.9.27.3 SR1; §13.18.46.3 SR3)");
            ok = false;
        }
        if (mode is BoundOpenMode.Extend && file.AccessMode is not FileAccessMode.Sequential)
        {
            data.Edition.Error(DiagnosticCatalog.OpenExtendAccessLinage,
                $"OPEN EXTEND '{file.CobolName}' — the file's access mode is "
                + $"{file.AccessMode.ToString().ToUpperInvariant()}; EXTEND requires sequential access "
                + "(ISO §14.9.27.3 SR2)");
            ok = false;
        }
        if (mode is BoundOpenMode.Extend && file.Linage is not null)
        {
            data.Edition.Error(DiagnosticCatalog.OpenExtendAccessLinage,
                $"OPEN EXTEND '{file.CobolName}' — the file description entry specifies a LINAGE clause, which "
                + "EXTEND does not admit (ISO §14.9.27.3 SR2)");
            ok = false;
        }
        return ok;
    }

    /// <summary>The written form of an open mode, for a diagnostic (§14.9.27.2's four phrase words).</summary>
    private static string OpenModeFace(BoundOpenMode mode) => mode switch
    {
        BoundOpenMode.Output => "OUTPUT",
        BoundOpenMode.Extend => "EXTEND",
        BoundOpenMode.IO => "I-O",
        _ => "INPUT",
    };


    /// <summary>§14.9.51 SR19 (the silent-drop bug class) — the END-OF-PAGE / NOT END-OF-PAGE phrase requires
    /// a LINAGE clause in the file's file description entry.</summary>
    public bool CheckWriteEopLinage(FileModel file)
    {
        if (file.Linage is not null) return true;
        data.Edition.Error("COBOLNET0860", $"WRITE … END-OF-PAGE on file '{file.CobolName}', whose file "
            + "description entry has no LINAGE clause (ISO §14.9.51.3 SR19)");
        return false;
    }

    /// <summary>§14.9.51 SR18 — ADVANCING PAGE and END-OF-PAGE shall not both be specified in one WRITE.</summary>
    public bool CheckWriteEopAdvancingPage(bool advancingPage)
    {
        if (!advancingPage) return true;
        data.Edition.Error("COBOLNET0861", "WRITE … ADVANCING PAGE with an END-OF-PAGE phrase: the two "
            + "shall not both be specified in a single WRITE statement (ISO §14.9.51.3 SR18)");
        return false;
    }

    /// <summary>§14.9.51.3 SR17 — <i>"The BEFORE and AFTER phrases shall not both be specified if the PAGE
    /// phrase is specified."</i> The pair itself is a COBOL-2023 addition (Annex §E.3.3 item 2, gated in
    /// <c>VersionConformancePass</c>); this is the one SYNTAX rule that narrows it, and it lives here beside
    /// SR18 and SR19 rather than inside the WRITE binder's operand plumbing (kb/Work PB712 — the old
    /// two-ADVANCING-phrase grammar checked SR17 as one arm of a malformed-pair test that also caught shapes
    /// §14.9.51.2 Format 1 cannot print).</summary>
    public bool CheckWriteBeforeAfterPage(bool bothWords, bool advancingPage)
    {
        if (!(bothWords && advancingPage)) return true;
        data.Edition.Error(DiagnosticCatalog.WriteBeforeAfterAdvancingPage,
            "WRITE … BEFORE AFTER ADVANCING PAGE: the BEFORE and AFTER phrases shall not both be specified if "
            + "the PAGE phrase is specified (ISO §14.9.51.3 SR17)");
        return false;
    }

    /// <summary>§14.9.51 SR13 — with a LINAGE clause, the ADVANCING phrase shall not name a SPECIAL-NAMES
    /// mnemonic (the caller resolves the mnemonic test through the per-unit registry).</summary>
    public bool CheckWriteAdvancingMnemonic(FileModel file, bool advancingNamesMnemonic)
    {
        if (!(file.Linage is not null && advancingNamesMnemonic)) return true;
        data.Edition.Error(DiagnosticCatalog.IoStatementOperandRule, $"WRITE … ADVANCING mnemonic-name on file '{file.CobolName}', whose "
            + "file description entry contains a LINAGE clause (ISO §14.9.51.3 SR13)");
        return false;
    }

    // ── The relational-operand SR checkpoint (ISO §8.8.4.2.2 / §8.8.4.2.3; lifted from ConditionBinder's
    //    CheckedRelational at P7 Step 10t/3 — the 10o deviation-(b) pure-lift discharged). ────────────────────

    /// <summary>The edition-invariant SR checks that ride the ONE <c>BoundRelational</c> checkpoint — reached
    /// by every relation (IF, EVALUATE pairings/ranges, PERFORM UNTIL, SEARCH WHEN, sole-operand conditions).
    /// A PURE emission check (no verdict — the caller always builds the node; the checks are side-effect
    /// diagnostics): class-boolean comparability (§8.8.4.2.2 Format 2 — boolean operands compare only with a
    /// boolean or the figurative ZERO, equality only — COBOLNET0844), and the strongly-typed-group rules
    /// (§8.8.4.2.3 SR1: same type both sides — COBOLNET1533; SR4: a strong group with a boolean/object/pointer
    /// leaf is equality-only — COBOLNET1535 <c>strong-compare-ordering</c>), and the
    /// §8.8.4.2.2 FORMAT 3 band (<see cref="CheckFormat3Relation"/>) — "a relation condition involving operands
    /// of class message-tag, object, or pointer is a message-tag-object-or-pointer-reference relation
    /// condition" (§8.8.4.2.1), whose general format admits ONLY <c>IS [NOT] EQUAL TO</c> / <c>=</c> /
    /// <c>&lt;&gt;</c> and whose SR5 requires both operands to be of one of those three classes AND of the same
    /// CATEGORY (COBOLNET0868 for a relation naming class object, COBOLNET0869 otherwise).
    /// <para>⛔ THAT BAND WAS WRITTEN IN <c>ConditionBinder.BindComparison</c>'s relation arm until kb/Work
    /// PB399 — one caller of this checkpoint, not the checkpoint — so it screened `IF P &gt;= Q` and said
    /// nothing about the SAME pair under EVALUATE, SEARCH WHEN, PERFORM UNTIL or an abbreviated relation.
    /// Measured: `EVALUATE WS-P WHEN WS-Q THRU WS-R` compiled clean and ordered raw addresses, and
    /// `EVALUATE WS-P WHEN WS-X` reached the BACKEND and failed as a raw C# CS1503. §14.9.13.3 SR7 a) makes an
    /// EVALUATE pair's objects "valid operands for comparison … in accordance with 8.8.4.2", so every §8.8.4.2
    /// rule is owed to it; the two-arm shape is fixed by moving the rule to the arm both sides share, never by
    /// copying it. It was also written TWICE there, once per class, which is why the two copies disagreed about
    /// the same category (only the object copy screened its operands' identity at all).</para></summary>
    public void CheckRelationalOperands(BoundOperand left, string op, BoundOperand right)
    {
        CheckFormat3Relation(left, op, right);
        // ISO §8.3.3.6.3 SR3 — a multi-character ALL literal shall not be ASSOCIATED with a numeric or
        // numeric-edited item, and §8.3.3.6.4 GR2's NOTE 1 names "compared with it" as an association. Asked of
        // the ONE predicate the MOVE edition pass asks (FigurativeAssociation — kb/Work PB422), in either
        // operand order; this checkpoint is every relation's (IF / EVALUATE / PERFORM UNTIL / SEARCH WHEN).
        CheckFigurativeAssociation(left, right);
        CheckFigurativeAssociation(right, left);
        // ⛔ The class of an operand is asked of the ONE classifier (IntrinsicResultType.OperandCategory — total
        // over literals, fields, ref-mod views, groups, ALL literals, boolean expressions and COMPUTED operands),
        // never re-derived here (kb/Work PB68): the local switch that stood here had no arm for a
        // BoundComputedOperand wrapping a BOOLEAN-result intrinsic, so `IF FUNCTION BOOLEAN-OF-INTEGER(544, 6) =
        // B"100000"` — exactly the §8.8.4.2.8 boolean comparison — was rejected as a class mix, while the mirror
        // against an ALPHANUMERIC literal (illegal) was accepted and evaluated. §15.13.1 makes the function's type
        // boolean; §15.2 item 2 its class and category; the classifier already knew.
        static bool IsBoolOperand(BoundOperand o) => IntrinsicResultType.OperandCategory(o) is PicCategory.Boolean;
        bool lb = IsBoolOperand(left), rb = IsBoolOperand(right);
        if (lb || rb)
        {
            // Asked of the §8.5.2.1 Table-2 lattice's CANDIDATE set (kb/Work PB1427): an operand is boolean-compatible
            // when class boolean is among the classes it may present — ZERO is "one or more of the boolean character
            // '0'" (§8.3.3.6.4 GR4), SPACE / QUOTE / HIGH-VALUE / LOW-VALUE are character values only (GR5–GR8), an
            // index data item is class index. The set is EMPTY only for "not statically decidable" (a recovery
            // profile, an operand whose binding already failed — kb/Work PB960), which fails OPEN: the rule rejects
            // what it NAMES, never what the compiler could not classify. (The category reader that stood here
            // answered null for EVERY figurative and every PICTURE-less item, so `IF B1 = SPACE` and a boolean
            // against an index data item passed as "undecidable".)
            static bool BoolCompatible(BoundOperand o) =>
                IntrinsicArgumentRules.CandidateClasses(o) is var set
                && (set.Length == 0 || Array.IndexOf(set, CobolClass.Boolean) >= 0);
            if (!(BoolCompatible(left) && BoolCompatible(right)))
                data.Edition.Error("COBOLNET0844", "a boolean operand may be compared only with another "
                    + "boolean operand or the figurative constant ZERO (ISO §8.8.4.2.2; §8.8.4.2.1 F1 "
                    + "SR2/SR3 exclude class boolean from the general relation)");
            else if (op is not ("==" or "!="))
                data.Edition.Error("COBOLNET0844", "boolean operands compare for equality only — an ordering "
                    + "relation is not defined for class boolean (ISO §8.8.4.2.2 Format 2)");
        }
        // §8.8.4.2.3 SR1 (data-model D17): if either operand is a strongly-typed group, both shall be of the same
        // type (§8.5.3.3). This is the ONE relation checkpoint, so it also covers EVALUATE pairings/ranges,
        // PERFORM UNTIL, and SEARCH WHEN.
        DataItem? sl = left is BoundFieldOperand fl ? fl.Place.Item : null;
        DataItem? sr = right is BoundFieldOperand fr ? fr.Place.Item : null;
        if ((sl is { } && StrongTypeModel.IsStrongGroup(sl)) || (sr is { } && StrongTypeModel.IsStrongGroup(sr)))
        {
            if (sl is null || sr is null || !StrongTypeModel.SameType(sl, sr))
                data.Edition.Error(DiagnosticCatalog.StrongCompareMismatch, "a strongly-typed group may be compared only with a group of the "
                    + "same type (ISO §8.8.4.2.3 SR1 / §8.5.3.3)");
            // §8.8.4.2.3 SR4: "Strongly-typed group items that contain elementary items of class boolean,
            // message-tag, object, or pointer, may be compared only for equality or inequality" — an ordering
            // relation on such a group is a syntax error. Every other same-type ordering is LEGAL and is compared
            // element by element (§8.8.4.2.12 — ConditionBinder.CheckedRelational lowers it; kb/Work PB1469
            // retired the signed-leaf stage that stood here while the relation was a whole-group image).
            else if (op is not ("==" or "!=") && (ContainsNonOrderableLeaf(sl) || ContainsNonOrderableLeaf(sr)))
                data.Edition.Error(DiagnosticCatalog.StrongCompareOrdering, "a strongly-typed group containing a boolean, object-reference, "
                    + "or pointer element may be compared only for equality or inequality "
                    + "(ISO §8.8.4.2.3 SR4 — no ordering relation is defined for such a group)");
        }
        // ISO §8.5.1.12.1 (kb/Work PB1467): a variable-length group "may not undergo a comparison … unless the other
        // operand is a compatible group" — asked of the ONE pair classifier the relation's construction also asks,
        // and of the ONE §8.5.1.12.1 reader the MOVE asks (VariableLengthCompatibility.PairRefusal). A strongly-typed
        // pair is not asked: §8.8.4.2.12 compares it element by element (SR1 above already required one type).
        else if (RelationPair.Classify(left, right) is RelationPairShape.VariableLengthGroup)
        {
            if (VariableLengthCompatibility.PairRefusal(OperandItem(left), OperandItem(right), "first", "second",
                    "may be compared only with a compatible GROUP") is { } why)
                data.Edition.Error(DiagnosticCatalog.VariableLengthGroupComparison, why);
        }
        // Every other pair is a GENERAL relation (or a boolean / Format 3 one, which the table answers "not mine"):
        // §8.8.4.2.1's closed "Comparisons are defined for the following:" list, asked of the ONE table.
        else CheckGeneralRelationComparability(left, right);
    }

    /// <summary>⛔ §8.8.4.2.1's "Comparisons are defined for the following:" over the general relation, asked of the
    /// ONE table (<see cref="RelationComparability"/>, kb/Work PB1468) at the ONE relation checkpoint — so IF,
    /// EVALUATE subject/object pairs and ranges, PERFORM UNTIL, SEARCH WHEN and abbreviated relations inherit it.
    /// <list type="bullet">
    /// <item>§8.8.4.2.13's index pairs are an unconditional error: "Relation tests may be made only between" its
    /// three pairs, and a pair outside them has no comparison to fall back on — an index-name against an
    /// alphanumeric operand used to abort the run unit ("computed expression in a string context").</item>
    /// <item>§8.8.4.2.5's numeric operand of item 6 is an error with the leniency dialect-gated — the DA6
    /// disposition (<c>IntrinsicArgumentRules</c>' remarks): §4.2.2's "This warning mechanism shall indicate
    /// violations of such rules" obliges the flag, and the character comparison of the value's text that
    /// --permissive keeps is what the compiler has always evaluated for the pair.</item>
    /// </list></summary>
    private void CheckGeneralRelationComparability(BoundOperand left, BoundOperand right)
    {
        switch (RelationComparability.Breach(left, right))
        {
            case RelationComparabilityBreach.IndexPair:
                data.Edition.Error(DiagnosticCatalog.RelationIndexPair, "an index-name or index data item is compared "
                    + "with an operand ISO §8.8.4.2.13 does not pair it with — \"Relation tests may be made only "
                    + "between\" two index-names, an index-name and a numeric data item or numeric literal, or an index "
                    + "data item and an index-name or another index data item");
                break;
            case RelationComparabilityBreach.NumericAgainstCharacter when data.Edition.Permissive:
                data.Edition.Warning(DiagnosticCatalog.RelationNumericNotInteger, "a numeric operand that is not an "
                    + "integer literal or an integer numeric data item of usage display or national is compared with a "
                    + "character operand (ISO §8.8.4.2.5) — accepted under --permissive as the character comparison of "
                    + "its value's text");
                break;
            case RelationComparabilityBreach.NumericAgainstCharacter:
                data.Edition.Error(DiagnosticCatalog.RelationNumericNotInteger, "a numeric operand compared with an "
                    + "operand of class alphanumeric, alphabetic or national shall be \"an integer literal or an integer "
                    + "numeric data item of usage display or national\" (ISO §8.8.4.2.5; §8.8.4.2.1 item 6 is the only "
                    + "such pair the standard defines) — a non-integer, COMPUTATIONAL, packed, floating-point or "
                    + "arithmetic-expression operand has no defined comparison with it");
                break;
        }
    }

    /// <summary>The DATA ITEM a relation operand denotes, or null for an operand that is not one (a literal, a
    /// figurative constant, a computed value, a reference-modified view — kb/Work PB602's identity question).</summary>
    private static DataItem? OperandItem(BoundOperand o) => o is BoundFieldOperand { Place.DenotedItem: { } item } ? item : null;

    /// <summary>§8.3.3.6.3 SR3 over one side of a relation: <paramref name="figurative"/> compared with the DATA
    /// ITEM <paramref name="other"/> names — an elementary item's own category (a reference-modified view is
    /// operated upon as alphanumeric, §8.4.3.3.4 GR2, and a group is never numeric, §8.5.2.1, so neither is SR3's).</summary>
    private void CheckFigurativeAssociation(BoundOperand figurative, BoundOperand other)
    {
        if (!FigurativeAssociation.IsMultiCharacterAll(figurative)
            || other is not BoundFieldOperand { Place: var place } || place is RefModPlace || place.Item.IsGroup) return;
        FigurativeAssociation.GateSr3(data.Edition.Edition, data.Edition.Sink, figurative, place.Item.Pic?.Category,
            $"a comparison of ALL \"{((BoundAllLiteral)figurative).Literal}\" with {place.Item.CobolName}");
    }

    /// <summary>⛔ THE ONE §8.8.4.2.2 FORMAT 3 SCREEN — the "message-tag-object-or-pointer-reference relation
    /// condition" ISO §8.8.4.2.1 names: "a relation condition involving operands of class message-tag, object,
    /// or pointer is a message-tag-object-or-pointer-reference relation condition". Two rules, over one pair:
    /// <list type="number">
    /// <item>the Format 3 general format prints only <c>IS [NOT] EQUAL TO</c> / <c>IS [NOT] =</c> / <c>IS
    /// &lt;&gt;</c> — no ordering operator is written in it at all, so an ordering relation on such an operand
    /// is a format violation;</item>
    /// <item>§8.8.4.2.3 SR5 — "Identifier-3 and identifier-4 shall reference data items of class message-tag,
    /// object, or pointer, and shall be of the same category", which §8.8.4.2.1 item 11 states a second way for
    /// the pointer class ("Two operands of class pointer where each operand is of the same category").</item>
    /// </list>
    /// <para>⛔ ONE SCREEN OVER ALL THREE CLASSES, because the standard writes ONE format and ONE syntax rule
    /// over all three (kb/Work PB399). The object and pointer halves used to be two independently-written
    /// blocks, and every difference between them was a hole: only the object half existed for message-tag
    /// (which is none of it), and NEITHER asked SR5's "same CATEGORY", so a data-pointer compared to a
    /// program-pointer — two categories §8.8.4.2.1 item 11 keeps apart — was accepted. Both halves also asked
    /// <c>Pic?.Category == PicCategory.Pointer</c>, which is one of the THREE categories Table 2 gathers into
    /// class pointer, so a program-pointer or function-pointer operand answered "not a pointer" and left the
    /// whole band silent.</para>
    /// <para>The figurative NULL rides on either side and in either class: §8.4.3.10.1 makes it "a predefined
    /// address of class pointer or a predefined content of class message-tag", §8.4.3.9 makes the object
    /// spelling "class object and category object reference", and §8.4.3.10.3 SR1 a) admits it "in a
    /// pointer-or-object-reference relation condition" by name. It has no category of its own, so it is exempt
    /// from SR5's same-category test as well.</para></summary>
    private void CheckFormat3Relation(BoundOperand left, string op, BoundOperand right)
    {
        // ⛔ CLASS IS ASKED OF THE ONE ISO §8.5.2.1 TABLE-2 LATTICE (IntrinsicArgumentRules), never of a local
        // `Pic?.Category == …` test. That local test is what the two old copies used, and it is one of the
        // THREE categories Table 2 gathers into class pointer — so a PROGRAM-POINTER or FUNCTION-POINTER
        // operand answered "not a pointer" and the whole band stayed silent on it. The lattice also folds a
        // bit / national GROUP onto its §13.18.29.4 as-if picture and reports an INDEX item as class index
        // rather than its storage category, neither of which raw `Pic` could see.
        static bool IsFormat3(CobolClass? c) => c is CobolClass.Object or CobolClass.Pointer;
        static bool IsNull(BoundOperand o) => o is BoundPredefinedNull;
        // NULL's class opposite `other`: the other side's Format-3 class, class pointer opposite any other decidable
        // operand, and "undecidable" (abstain) opposite an operand whose binding already failed (kb/Work PB960).
        static CobolClass? NullReading(BoundOperand o, BoundOperand other) =>
            !IsNull(o) ? IntrinsicArgumentRules.ClassOf(o)
            : IntrinsicArgumentRules.CandidateClasses(other) switch
            {
                [] => null,
                [var oc] when IsFormat3(oc) => oc,
                _ => CobolClass.Pointer,
            };
        // ⚠ NULL HAS NO CLASS OF ITS OWN IN A RELATION. §8.4.3.10.3 SR1 selects its reading from "the
        // associated data item's class" (a) pointer, b) message-tag) and §8.4.3.9 gives the object spelling
        // class object, so in a relation it takes the OTHER operand's class. The lattice reports it as class
        // POINTER — the right answer for every class-CLOSED operand slot it is asked about, and the wrong one
        // here, where `IF an-object-reference = NULL` is the relation §8.4.3.10.3 SR1 a) admits by name.
        // ⛔ …BUT ONLY OPPOSITE A FORMAT-3 OPERAND (kb/Work PB1427). Taking the other side's class UNCONDITIONALLY
        // gave NULL class alphanumeric opposite an alphanumeric item and class numeric opposite a numeric one, so
        // `IF X = NULL` / `IF N = NULL` made neither side Format 3, left the band silent, and ran NULL as LOW-VALUE.
        // §8.4.3.10.3 SR1 a) admits NULL only "in a pointer-or-object-reference relation condition"; opposite
        // anything else it keeps the class the lattice gives it — pointer — and SR5 below refuses the pair.
        // NULL opposite NULL: §8.4.3.10.3 SR1 — "This item may be used only in the following cases, depending upon
        // the associated data item's class" — and neither operand is a data item for NULL to be associated with, so
        // no case applies; it is the ONE SR1 refusal, COBOLNET2576 (kb/Work PB1427). The pair must stop here: no
        // renderer reads NULL, because every admitted slot compares it against the class of its other operand.
        if (IsNull(left) && IsNull(right))
        {
            PredefinedNullRule.Report(data.Edition, "a relation comparing NULL with NULL, which has no data-item operand "
                + "for NULL to be associated with (SR1 admits it only \"depending upon the associated data item's "
                + "class\") — compare a pointer, object-reference or message-tag data item with NULL —");
            return;
        }
        var lc = NullReading(left, right);
        var rc = NullReading(right, left);
        if (!IsFormat3(lc) && !IsFormat3(rc)) return;   // a general-relation or boolean condition — not this band
        // ⚠ CLASS MESSAGE-TAG — Format 3's third class — has no lattice member, because USAGE MESSAGE-TAG is
        // DECLINED non-support (COBOLNET1943, Annex A.3 item 4) and `ParseUsage` refuses it BY NAME at every
        // edition, so no message-tag operand can reach a bound relation on a compile that is not already
        // errored. The rule below is written over the classes the lattice can present; the day the usage lands,
        // the lattice gains its member and this screen sees it without a change here.
        // COBOLNET0868 is the object-reference band, COBOLNET0869 the pointer/address one (whose descriptor
        // names object references too); a relation that mentions class object reports under 0868.
        bool obj = lc is CobolClass.Object || rc is CobolClass.Object;
        string code = obj ? DiagnosticCatalog.ObjectRelationShape.Code : DiagnosticCatalog.PointerOperandShape.Code;
        string band = obj ? "an object-reference" : "a data-pointer";
        if (op is not ("==" or "!="))
        {
            data.Edition.Error(code, $"{band} relation admits only [NOT] EQUAL / '=' / '<>' — ISO §8.8.4.2.2 "
                + "Format 3 (message-tag-object-or-pointer-reference) prints no ordering operator, and no "
                + "ordering is defined for a reference or an address");
            return;   // ONE diagnostic per written relation: the operand rule below is about the same pair
        }
        // §8.8.4.2.3 SR5's two halves, asked of the pair. An operand whose class is not statically decidable
        // (an already-reported error node, a figurative whose class the context picks) abstains rather than
        // drawing a second diagnostic about something the binder has already spoken about.
        if (!IsFormat3(lc) || !IsFormat3(rc))
        {
            if (lc is not null && rc is not null)
                data.Edition.Error(code, $"both operands of {band} relation shall be of class message-tag, "
                    + "object or pointer — a data item of one of those classes, or the predefined NULL "
                    + "(ISO §8.8.4.2.3 SR5; §8.4.3.10.3 SR1)");
            return;
        }
        // "… and shall be of the same category" — the three pointer CATEGORIES are ONE class (Table 2) but SR5
        // keeps them apart, and §8.8.4.2.1 item 11 says the same in the comparison-defined list ("Two operands
        // of class pointer where each operand is of the same category"). NULL is exempt: it is a predefined
        // address, not an identifier-3/identifier-4, and has no category of its own.
        if (IsNull(left) || IsNull(right)) return;
        if (lc != rc || (IntrinsicResultType.OperandCategory(left) is { } lcat
                         && IntrinsicResultType.OperandCategory(right) is { } rcat && lcat != rcat))
            data.Edition.Error(code, $"the two operands of {band} relation are not of the same category "
                + "(ISO §8.8.4.2.3 SR5 — \"Identifier-3 and identifier-4 shall reference data items of class "
                + "message-tag, object, or pointer, and shall be of the same category\"; §8.8.4.2.1 item 11 "
                + "defines the pointer comparison only \"where each operand is of the same category\")");
    }

    /// <summary>True when a group (or elementary) item has any elementary item of a CLASS §8.8.4.2.3 SR4 names —
    /// boolean, message-tag, object or pointer — which makes a strongly-typed group comparable only for equality.
    /// ⛔ THE CLASS IS ASKED OF THE ONE §8.5.2.1 TABLE-2 LATTICE (<see cref="IntrinsicArgumentRules.ClassOfItem"/>),
    /// never of a local category list (kb/Work PB1469): the list that stood here named three CATEGORIES —
    /// boolean, object-reference and data-pointer — and so missed PROGRAM-POINTER and FUNCTION-POINTER, the other
    /// two categories Table 2 gathers into class pointer; `IF T1 &gt; T2` over a strong group with a
    /// program-pointer leaf compiled clean. (Class message-tag has no lattice member because USAGE MESSAGE-TAG is
    /// declined non-support, COBOLNET1943; the day it lands, the lattice gains it and this sees it unchanged.)</summary>
    private static bool ContainsNonOrderableLeaf(DataItem item)
    {
        if (item.IsElementary)
            return IntrinsicArgumentRules.ClassOfItem(item) is CobolClass.Boolean or CobolClass.Object or CobolClass.Pointer;
        foreach (var c in item.Children)
            if (ContainsNonOrderableLeaf(c)) return true;
        return false;
    }
}
