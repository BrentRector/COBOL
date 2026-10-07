// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using CobolNet.Runtime;
using Core = CobolParserCore;

/// <summary>
/// The I-O-CONTROL paragraph (ISO §12.4.6): the SAME clause's membership model, its syntax rules
/// (§12.4.6.4.3 SR2-SR10) and the record-area sharing it links (§12.4.6.4.4 GR2).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>Bind the I-O-CONTROL paragraph (ISO §12.4.6). A record-area SAME clause (Format 2) makes the listed
    /// files "share a memory area for processing the current logical record … equivalent to an implicit redefinition
    /// of the area with records aligned on the leftmost byte position" (§12.4.6.4 GR2) — modeled by chaining each
    /// listed file's FIRST record as a synthesized REDEFINES of the first LISTED file's first record, exactly the
    /// multi-01-under-one-FD mechanism (the singular-pattern rule): the tier machinery then aliases every record of
    /// every listed file over ONE backing, and READ/WRITE/RELEASE image distribution gives the
    /// record-of-the-most-recently-read-file semantics for free. A sort/merge file may appear in a record-area
    /// clause (SR6 — ST131A's <c>READ FILE3</c> then <c>RELEASE S3</c> with no FROM relies on it). The file-area
    /// (Format 1) and sort-merge-area (Format 3) formats are storage-economy permissions (GR1/GR4 — shared/reusable
    /// ALLOCATION plus open-mode constraints on the program, nothing a typed-native runtime must alias) — bound as
    /// conformant no-ops; MULTIPLE FILE TAPE is obsolete and parsed-and-ignored (grammar note), and so is the
    /// X3.23-1985 RERUN clause (a checkpoint HINT with no program-visible effect — a null rerun facility is
    /// conforming; deleted by ISO 2002, 0902-gated ≥2002 by the version-conformance pass, VCR Table 7 row 7.15) —
    /// both skip through the non-SAME <c>continue</c> below by design. The APPLY COMMIT clause (§12.4.6.3) is the one
    /// other arm that is not a no-op here: the facility is declined (COBOLNET1709) but the clause still records
    /// WHICH FILES the source made "subject to an APPLY COMMIT clause" on <see cref="FileModel.SubjectToApplyCommit"/>,
    /// because §14.9.27.3 SR8's leading conjunct keys on exactly that and is reachable under <c>--permissive</c>
    /// (kb/Work PB319). EVERY SAME format also records its MEMBERSHIP on its files
    /// (<see cref="FileModel.SameClauses"/>, kb/Work PB1139), which the SORT and MERGE statements read for
    /// §14.9.40.3 SR10 / §14.9.24.3 SR11 (<c>SortBinder.ScreenSameClauses</c>). The clause's OWN syntax rules —
    /// the arity its format prints and §12.4.6.4.3 SR2, SR3 and SR5-SR10 — are <see cref="ScreenSameOperands"/> and
    /// <see cref="ScreenSameClauses"/> (kb/Work PB1087).</summary>
    private void BindIoControl(Core.ProgramUnitContext program)
    {
        foreach (var env in EnvDivisions(program)) BindIoControl(env);
    }

    private void BindIoControl(Core.EnvironmentDivisionContext env)
    {
        var io = env.inputOutputSection()?.ioControlParagraph();
        if (io is null) return;
        var sites = new List<SameSite>();
        foreach (var clause in io.ioControlClause())
        {
            using var _ = Edition.At(clause);
            // The APPLY COMMIT clause (ISO §12.4.6.3) — the facility is DECLINED by name (COBOLNET1709,
            // DeclinedFacilityPass), but the clause still tells the binder WHICH FILES the source made "subject
            // to an APPLY COMMIT clause", and two syntax rules key on exactly that: §14.9.27.3 SR8's leading
            // conjunct (enforced at StatementValidation.CheckOpenSharingAllOther) and §12.4.5.9.3 SR1. The fact
            // is LIVE, not speculative: COBOLNET1709 is PermissiveInert, so under --permissive the clause is
            // accepted with a warning and the program compiles (kb/Work PB319). §12.4.6.3.2's operand list is a
            // repetition of an all-optional [file-name-1][identifier-1] pair, so a name that resolves to a file
            // IS file-name-1 and anything else is identifier-1 — the symbol table is the only discriminator.
            // Only a BARE word can be file-name-1 (§8.4.2.2.2 gives a file-name no qualified format, kb/Work
            // PB2040): a qualified or subscripted operand is identifier-1, and its glued text (`F OF G` → `FOFG`)
            // must never be looked up as a file-name.
            if (clause.applyCommitClause() is { } apply)
            {
                foreach (var operand in apply.dataReference())
                    if (operand.cobolWord() is { } word && operand.dataReferenceSuffix().Length == 0
                        && FilesByName.TryGetValue(word.GetText(), out var subject))
                    {
                        subject.SubjectToApplyCommit = true;
                        // §12.4.5.9.3 SR1 — the LOCK MODE clause of the file control entry, bound before this paragraph.
                        ScreenApplyCommitSubject(subject, subject.LockMode is not null, "LOCK MODE clause",
                            "ISO §12.4.5.9.3 SR1: \"This clause shall not be specified for a file that is the subject of "
                            + "an APPLY COMMIT clause for which there is an implicit LOCK MODE IS AUTOMATIC WITH LOCK ON "
                            + "MULTIPLE RECORDS applied automatically\"");
                    }
                continue;
            }
            if (clause.sameClause() is not { } same) continue;
            // ⛔ EVERY SAME FORMAT IS RECORDED ON ITS FILES (kb/Work PB1139): the clause's membership is what
            // §14.9.40.3 SR10 and §14.9.24.3 SR11 ask the SORT and MERGE statements about — "No pair of file-names in
            // the same SORT statement may be specified in the same SAME SORT AREA or SAME SORT-MERGE AREA clause" —
            // and until then only the RECORD AREA format was modelled (as a peer list), so those rules had nothing to read.
            // §12.4.6.4.3 SR1: SORT and SORT-MERGE are equivalent — ONE format, the written words kept for messages.
            var kind = same.RECORD() is not null ? SameClauseKind.RecordArea
                : same.SORT_MERGE() is not null || same.SORT() is not null ? SameClauseKind.SortMergeArea
                : SameClauseKind.Area;
            string written = "SAME" + (same.RECORD() is not null ? " RECORD" : same.SORT_MERGE() is not null ? " SORT-MERGE"
                : same.SORT() is not null ? " SORT" : "") + " AREA";
            var (members, complete) = ScreenSameOperands(same, written);
            var recorded = new SameClause(kind, members, written);
            foreach (var m in members) m.SameClauses.Add(recorded);
            sites.Add(new SameSite(same, recorded, complete));
        }
        ScreenSameClauses(sites);
        // §12.4.6.4.3 SR11 — asked after the WHOLE paragraph, because an APPLY COMMIT clause may follow the SAME
        // clause it constrains. One clause mixing the two kinds is one violation, reported at the clause.
        foreach (var site in sites)
            if (site.Clause.Members.FirstOrDefault(f => f.SubjectToApplyCommit) is { } subject
                && site.Clause.Members.FirstOrDefault(f => !f.SubjectToApplyCommit) is { } other)
            {
                using var _ = Edition.At(site.Ctx);
                ScreenApplyCommitSubject(subject, true,
                    $"{site.Clause.Written} naming '{subject.CobolName}' and '{other.CobolName}'",
                    "ISO §12.4.6.4.3 SR11: \"A file or record area that is subject to an APPLY COMMIT clause shall not be "
                    + "specified with another file or record area that is not subject to an APPLY COMMIT clause\"");
            }
        foreach (var site in sites)
            if (site.Clause.Kind == SameClauseKind.RecordArea) LinkSameRecordArea(site.Clause);
    }

    /// <summary>⛔ THE ONE REPORT FOR THE APPLY COMMIT ANTECEDENT FAMILY (kb/Work PB666) — the syntax rules of CLAIMED
    /// clauses and statements whose antecedent is "a file subject to an APPLY COMMIT clause": §12.4.5.9.3 SR1 (the
    /// LOCK MODE clause), §12.4.6.4.3 SR11 (a SAME clause mixing subject and non-subject files), §14.9.27.3 SR7 (OPEN's
    /// sharing phrase), §14.9.30.3 SR5 / §14.9.35.3 SR5 (READ's and REWRITE's lock phrases) and §14.9.47.3 SR2
    /// (UNLOCK). Each caller passes its own rule quoted with its citation; the fact is
    /// <see cref="FileModel.SubjectToApplyCommit"/>, recorded off this paragraph.
    /// <para>⛔ THESE RULES ARE LIVE, NOT VACUOUS. kb/Work PB371 (owner, 2026-09-02) recorded the family CONFORMS
    /// "witnessed by the refusal of the antecedent": COBOLNET1709 refuses the APPLY COMMIT clause (Annex A.4.3 is
    /// not claimed). But COBOLNET1709 is <c>PermissiveInert</c>: under <c>--permissive</c> the clause is a warning,
    /// the fact is recorded and the program compiles — and every one of these forbidden shapes then compiled with
    /// zero errors. A refusal witnesses a rule only in the lanes where it refuses, so each rule is enforced here in
    /// its own right (COBOLNET2974), in both lanes. (§12.4.6.4.4 GR3, the family's seventh member, is a general rule
    /// about an ACTIVE clause's sharing at run time; no APPLY COMMIT clause is ever active here, in either lane.)</para></summary>
    /// <param name="file">The file the clause or statement names.</param>
    /// <param name="written">Whether the source wrote the construct the rule forbids for such a file.</param>
    /// <param name="what">The construct as written, for the message.</param>
    /// <param name="rule">The caller's own rule, quoted with its citation.</param>
    /// <returns>true when the rule holds; false after reporting.</returns>
    internal bool ScreenApplyCommitSubject(FileModel file, bool written, string what, string rule)
    {
        if (!(written && file.SubjectToApplyCommit)) return true;
        Edition.Error(DiagnosticCatalog.ApplyCommitSubjectRule, $"{what} — the file '{file.CobolName}' is named in an "
            + $"I-O-CONTROL APPLY COMMIT clause, and {rule}.");
        return false;
    }

    /// <summary>Only SAME RECORD AREA shares storage (Format 2) — SORT/SORT-MERGE AREA is Format 3, and SAME AREA
    /// (Format 1) gives no record-area effect either (docs/CONFORMANCE.md §7, A.1 items 168-169).
    /// <para>⛔ THE GROUP IS ONE STORAGE AREA FOR THE WHOLE RUNTIME ELEMENT, and that is also §14.9.6.4 GR7's
    /// answer for CLOSE. GR7: "If file-name-1 is specified in a SAME RECORD AREA clause, the record area
    /// is available to the runtime element if any of the file connectors referenced by the other
    /// file-names in that SAME RECORD AREA clause are open." The area emitted here is a live typed field
    /// of the program class, so a CLOSE of one member cannot take it away from a still-open sibling —
    /// GR7's availability branch holds STRUCTURALLY, not by omission (pinned by
    /// conformance:85/pb235_same_record_area_close). GR7's other branch — a successful CLOSE with no open
    /// member left makes the area UNAVAILABLE — carries no obligation a conforming program can observe:
    /// the standard defines nothing about referencing an unavailable record area (and Annex A.2 item 5
    /// makes the unsuccessful case outright undefined), so WiseOwl COBOL's determination is that the storage
    /// KEEPS ITS LAST CONTENT, documented at docs/CONFORMANCE.md §7, A.1 item 24 (kb/Work PB235).</para></summary>
    private static void LinkSameRecordArea(SameClause clause)
    {
        DataItem? anchor = null;
        foreach (var f in clause.Members)
        {
            if (f.Records.Count == 0) continue;
            // The area's CHARACTER half is linked here; an out-of-line record (FileModel.IsOutOfLineRecord —
            // D-FRA, kb/Work PB981) has no window over it and reaches the shared area through
            // FileModel.OutOfLineRecords, which is why every sharing file knows its peers
            // (FileModel.SameRecordAreaPeers, derived from the clause just recorded).
            if (f.CharacterAnchor is not { } fAnchor) continue;
            if (anchor is null) { anchor = fAnchor; continue; }
            // §12.4.6.4.4 GR2: "equivalent to an implicit redefinition of the area with records aligned on the
            // leftmost byte position" — implicit, like the FD's own records (kb/Work PB836).
            if (!ReferenceEquals(fAnchor, anchor) && fAnchor.RedefinesTarget is null)
                fAnchor.SetRedefinition(anchor, RedefinitionKind.SameRecordArea);
        }
    }

    /// <summary>One SAME clause as bound: its parse node (the position every rule reports at), its membership model
    /// and whether EVERY operand resolved to a file of this source element — a rule that asks what the clause
    /// contains (SR8) is silent about a clause whose own name resolution already failed.</summary>
    private readonly record struct SameSite(Core.SameClauseContext Ctx, SameClause Clause, bool Complete);

    /// <summary>⛔ THE OPERAND RULES OF ONE SAME CLAUSE (kb/Work PB1087), returning the files its operands resolve
    /// to and whether all of them did.
    /// <para>The format prints <c>file-name-1 { file-name-2 } …</c> in all three formats (§12.4.6.4.2), the braces
    /// REQUIRING a file-name-2, and §12.4.6.4.4 GR1/GR2 define the clause over "two or more files referenced by
    /// file-name-1, file-name-2" — so a clause naming fewer than two distinct files is not a legal form (the
    /// grammar admits it, superset-parse / bind-narrow). SR2: each operand "shall be specified in the FILE-CONTROL
    /// paragraph of the source element that contains this SAME clause" — <see cref="FilesByName"/> holds exactly
    /// this unit's own files here (a containing program's GLOBAL files merge in after the unit's data binds, so a
    /// SAME clause cannot name one) — so a name that is no file, or a data-name, is refused rather than dropped.
    /// SR3: the operands "shall not reference an external file connector".</para></summary>
    private (List<FileModel> Members, bool Complete) ScreenSameOperands(Core.SameClauseContext same, string written)
    {
        var members = new List<FileModel>();
        var seenNames = new HashSet<string>(CobolNames.Comparer);
        bool complete = true;
        foreach (var fn in same.fileName())
        {
            string name = fn.GetText();
            seenNames.Add(name);
            using var _ = Edition.At(fn);
            if (!FilesByName.TryGetValue(name, out var file))
            {
                complete = false;
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{written}: '{name}' is not a file specified in the FILE-CONTROL paragraph of this source element "
                    + "(ISO §12.4.6.4.3 SR2: \"File-name-1 and file-name-2 shall be specified in the FILE-CONTROL "
                    + "paragraph of the source element that contains this SAME clause.\")");
                continue;
            }
            if (file.IsExternal)
            {
                complete = false;
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{written}: file '{name}' references an external file connector (ISO §12.4.6.4.3 SR3: "
                    + "\"File-name-1 and file-name-2 shall not reference an external file connector.\")");
                continue;
            }
            if (!members.Contains(file)) members.Add(file);
        }
        if (seenNames.Count < 2)
        {
            using var _ = Edition.At(same);
            Edition.Error(DiagnosticCatalog.SameClauseRule,
                $"{written} names {(seenNames.Count == 0 ? "no file" : "only one file")}: every SAME format prints "
                + "\"file-name-1 { file-name-2 } …\" (ISO §12.4.6.4.2) and the clause is defined over \"two or more "
                + "files referenced by file-name-1, file-name-2\" (ISO §12.4.6.4.4 GR1, GR2)");
        }
        return (members, complete);
    }

    /// <summary>What a file IS to the SAME clause (§12.4.6.4.3 SR5-SR7 sort the file-names into three kinds).</summary>
    private enum SameFileKind { Report, SortMerge, Other }

    private static SameFileKind SameKindOf(FileModel f) =>
        f.IsSortMerge ? SameFileKind.SortMerge : f.IsReportFile ? SameFileKind.Report : SameFileKind.Other;

    /// <summary>⛔ THE OCCURRENCE TABLE: how many SAME clauses of one format a file of one kind may be named in
    /// (§12.4.6.4.3 SR5, SR6, SR7 — one rule shape, three rows) and the rule's own words. Both axes are switched
    /// BY AN UNREACHABLE-THROW arm only, so a new <see cref="SameFileKind"/> or <see cref="SameClauseKind"/> member
    /// fails loudly on its first SAME clause instead of falling into a silently unlimited cell.</summary>
    private static (int Max, int Sr, string Text) SameOccurrenceRule(SameFileKind kind, SameClauseKind format) => kind switch
    {
        SameFileKind.Report => (format switch
        {
            SameClauseKind.Area => 1,
            SameClauseKind.RecordArea => 0,
            SameClauseKind.SortMergeArea => 0,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 5, "a given file-name that represents a report file may be specified in one file-area format SAME clause "
            + "and shall not be specified in a record-area format or sort-merge-area format SAME clause"),
        SameFileKind.SortMerge => (format switch
        {
            SameClauseKind.Area => 0,
            SameClauseKind.RecordArea => 1,
            SameClauseKind.SortMergeArea => 1,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 6, "a given file-name that represents a sort or merge file may be specified in one record-area format SAME "
            + "clause and in one sort-merge-area SAME clause, and shall not be specified in a file-area format SAME clause"),
        SameFileKind.Other => (format switch
        {
            SameClauseKind.Area => 1,
            SameClauseKind.RecordArea => 1,
            SameClauseKind.SortMergeArea => int.MaxValue,
            _ => throw new System.Diagnostics.UnreachableException(),
        }, 7, "a given file-name that represents a file other than a report file or a sort or merge file may be "
            + "specified in one file-area format, in one record-area format, and in one or more sort-merge-area format "
            + "SAME clauses"),
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static string SameKindName(SameFileKind kind) => kind switch
    {
        SameFileKind.Report => "report file",
        SameFileKind.SortMerge => "sort or merge file",
        SameFileKind.Other => "file",
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static string SameFormatName(SameClauseKind format) => format switch
    {
        SameClauseKind.Area => "file-area",
        SameClauseKind.RecordArea => "record-area",
        SameClauseKind.SortMergeArea => "sort-merge-area",
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    /// <summary>⛔ THE CROSS-CLAUSE RULES OF THE SAME CLAUSES OF ONE I-O-CONTROL PARAGRAPH (kb/Work PB1087;
    /// §12.4.6.4.3 SR5-SR10), over the membership every clause recorded: the per-file occurrence limits
    /// (<see cref="SameOccurrenceRule"/> — SR5, SR6, SR7), SR8 (a sort-merge-area clause names a sort or merge
    /// file), SR9 (a file-area clause that shares a file with a record-area clause is wholly inside it) and SR10 (a
    /// file-area clause is wholly inside every sort-merge-area clause that names one of its non-sort files). Both
    /// the SORT and the SORT-MERGE spelling are the one sort-merge-area format (SR1), which is what
    /// <see cref="SameClauseKind"/> says.</summary>
    private void ScreenSameClauses(IReadOnlyList<SameSite> sites)
    {
        var count = new Dictionary<(FileModel, SameClauseKind), int>();
        foreach (var site in sites)
            foreach (var f in site.Clause.Members)
            {
                var role = SameKindOf(f);
                var (max, sr, text) = SameOccurrenceRule(role, site.Clause.Kind);
                count[(f, site.Clause.Kind)] = count.GetValueOrDefault((f, site.Clause.Kind)) + 1;
                if (count[(f, site.Clause.Kind)] <= max) continue;
                using var _ = Edition.At(site.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{site.Clause.Written}: {SameKindName(role)} '{f.CobolName}' "
                    + (max == 0 ? $"is named in a {SameFormatName(site.Clause.Kind)} format clause, which its kind bars"
                                : $"is named in more than one {SameFormatName(site.Clause.Kind)} format clause")
                    + $" (ISO §12.4.6.4.3 SR{sr}: {text})");
            }

        // §13.18.27.3 SR2 (kb/Work PB1242): the GLOBAL clause is written on neither the file description entry nor a
        // record description entry of any file the record-area clause shares.
        foreach (var site in sites.Where(s => s.Clause.Kind == SameClauseKind.RecordArea))
            foreach (var f in site.Clause.Members)
            {
                string? where = f.IsGlobal ? "file description entry"
                    : f.Records.Any(r => r.HasGlobalClause) ? "record description entry" : null;
                if (where is null) continue;
                using var _ = Edition.At(site.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{site.Clause.Written}: the {where} of file '{f.CobolName}' includes the GLOBAL clause (ISO §13.18.27.3 "
                    + "SR2: \"If the SAME RECORD AREA clause is specified for several files, the record description "
                    + "entries or the file description entries for these files shall not include the GLOBAL clause.\")");
            }

        foreach (var site in sites)
        {
            if (site.Clause.Kind != SameClauseKind.SortMergeArea || !site.Complete
                || site.Clause.Members.Any(m => m.IsSortMerge)) continue;
            using var _ = Edition.At(site.Ctx);
            Edition.Error(DiagnosticCatalog.SameClauseRule,
                $"{site.Clause.Written} names no sort or merge file (ISO §12.4.6.4.3 SR8: \"At least one file-name "
                + "specified in a sort-merge-area format SAME clause shall represent a sort or merge file.\")");
        }

        foreach (var area in sites.Where(s => s.Clause.Kind == SameClauseKind.Area))
        {
            foreach (var record in sites.Where(s => s.Clause.Kind == SameClauseKind.RecordArea))
            {
                var missing = SameMissingFrom(area.Clause, record.Clause);
                if (missing.Count == 0 || missing.Count == area.Clause.Members.Count) continue;
                using var _ = Edition.At(record.Ctx);
                Edition.Error(DiagnosticCatalog.SameClauseRule,
                    $"{record.Clause.Written} shares files with {area.Clause.Written} but omits {SameNames(missing)} "
                    + "(ISO §12.4.6.4.3 SR9: \"If one or more file-names specified in a file-area format SAME clause "
                    + "are also specified in a record-area format SAME clause, all of the file-names specified in the "
                    + "file-area format SAME clause shall also be specified in the record-area format SAME clause.\")");
            }
            var reported = new HashSet<SameSite>();
            foreach (var f in area.Clause.Members.Where(m => SameKindOf(m) == SameFileKind.Other))
                foreach (var sm in sites.Where(s => s.Clause.Kind == SameClauseKind.SortMergeArea && s.Clause.Members.Contains(f)))
                {
                    var missing = SameMissingFrom(area.Clause, sm.Clause);
                    if (missing.Count == 0 || !reported.Add(sm)) continue;
                    using var _ = Edition.At(sm.Ctx);
                    Edition.Error(DiagnosticCatalog.SameClauseRule,
                        $"{sm.Clause.Written} names '{f.CobolName}', which {area.Clause.Written} also names, but omits "
                        + $"{SameNames(missing)} (ISO §12.4.6.4.3 SR10: \"If a file-name that represents a file other "
                        + "than a sort or merge file is specified in a file-area format SAME clause and in one or more "
                        + "sort-merge-area format SAME clauses, all of the file-names specified in that file-area "
                        + "format SAME clause shall also be specified in those sort-merge-area format SAME clause(s).\")");
                }
        }
    }

    private static List<FileModel> SameMissingFrom(SameClause subset, SameClause superset) =>
        subset.Members.Where(m => !superset.Members.Contains(m)).ToList();

    private static string SameNames(IEnumerable<FileModel> files) =>
        string.Join(", ", files.Select(f => $"'{f.CobolName}'"));
}
