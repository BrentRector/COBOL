// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;

namespace CobolNet.Binding;

/// <summary>The SHAPE of a data-description clause-placement rule — what §13.16.3 and the clauses' own syntax
/// rules restrict about WHERE a clause may be written, independent of what the clause means.</summary>
internal enum ClausePlacementKind
{
    /// <summary>Only on an entry with no subordinate entries (§8.5.1.3.1's "not further subdivided"). Decidable
    /// only over the finished forest — the subordinates are not parsed when the entry binds.</summary>
    ElementaryOnly,
    /// <summary>Only at the level-numbers <see cref="ClausePlacementRule.ResidenceLevels"/> admits (level 1 when
    /// the row names none), and only in the sections <see cref="ClausePlacementRule.Sections"/> names — one rule
    /// because every such sentence in the standard states both halves together.</summary>
    Residence,
    /// <summary>Only with the data-name format of the entry-name clause (never FILLER, never omitted).</summary>
    DataNameRequired,
    /// <summary>Never in an entry whose level-number is one of <see cref="ClausePlacementRule.Levels"/> — the
    /// "shall not be specified in a data description entry that has a level-number of …" shape.</summary>
    NotAtLevel,
    /// <summary>Never in the same entry as any clause of <see cref="ClausePlacementRule.Excluded"/>.</summary>
    NotWith,
}

/// <summary>The sections a <see cref="ClausePlacementKind.Residence"/> rule admits, one bit per
/// <see cref="EntrySection"/>.</summary>
[Flags]
internal enum EntrySections
{
    None = 0,
    WorkingStorage = 1 << (int)EntrySection.WorkingStorage,
    LocalStorage = 1 << (int)EntrySection.LocalStorage,
    Linkage = 1 << (int)EntrySection.Linkage,
    File = 1 << (int)EntrySection.File,
    All = WorkingStorage | LocalStorage | Linkage | File,
}

/// <summary>One placement rule, transcribed from its sentence. <see cref="Clauses"/> is the SUBJECT set (every
/// clause the sentence restricts), <see cref="Rule"/> the citation and <see cref="Sentence"/> the rule's own
/// words, which is what the diagnostic quotes. <see cref="TypeDeclarationInAnySection"/> is §13.18.22.3 SR1's
/// third residence — "in level 1 type declarations", which names no section. <see cref="Levels"/> is the level-number
/// set a <see cref="ClausePlacementKind.NotAtLevel"/> rule forbids; it is stated whole as the sentence states it, so
/// a level the entry binder never sees here (66 and 88 are RENAMES and condition-name entries) is still in the row.
/// <see cref="ResidenceLevels"/> is the OPPOSITE set — the level-numbers a <see cref="ClausePlacementKind.Residence"/>
/// rule ADMITS; null is level 1 alone, which is what every residence sentence says but §13.16.3 SR16's ("The level
/// number of such data description entries shall be 1 or 77").</summary>
internal sealed record ClausePlacementRule(
    DataClauseKind Clauses, ClausePlacementKind Kind, string Rule, string Sentence, DiagnosticDescriptor Code,
    DataClauseKind Excluded = DataClauseKind.None, EntrySections Sections = EntrySections.All,
    bool TypeDeclarationInAnySection = false, IReadOnlyList<int>? Levels = null,
    IReadOnlyList<int>? ResidenceLevels = null)
{
    /// <summary>Why an entry at <paramref name="level"/> in <paramref name="section"/> breaks this
    /// <see cref="ClausePlacementKind.Residence"/> row, in words for the diagnostic — or null when it does not.
    /// ⛔ THE ONE residence test: <c>DataBinder.ScreenClausePlacement</c> asks it of a WRITTEN clause and
    /// <c>DataBinder.ScreenComposedBased</c> of a BASED clause a TYPE or SAME AS clause composed into the entry, so
    /// the two arms cannot hold different halves of one sentence (kb/Work PB516: the SECTION half of SR16 was asked
    /// by neither). <paramref name="section"/> is null for an entry no section describes (a compiler temporary).</summary>
    public string? ResidenceFault(int level, EntrySection? section, bool isTypeDeclaration)
    {
        if (ResidenceLevels is { } admitted ? !admitted.Contains(level) : level != 1)
            return $"this entry's level-number is {level:00}";
        if (section is { } s && (Sections & (EntrySections)(1 << (int)s)) == 0
            && !(TypeDeclarationInAnySection && isTypeDeclaration))
            return $"this entry is in the {DataBinder.SectionWords(s)}";
        return null;
    }
}

/// <summary>
/// ⛔ <b>THE DATA-DESCRIPTION CLAUSE-PLACEMENT SCREEN — one table, two sites, no private copies</b>
/// (kb/Work PB507 / PB512 / PB518 / PB519).
///
/// <para><b>Why a table.</b> Every rule here has the same shape — "clause C may be specified only
/// {for an elementary item | at level 1 in sections S | with a data-name | without clause D}" — and before this
/// file the shape had been written down as a scatter of ad-hoc checks that each covered one clause: PICTURE's
/// elementary-only rule had a screen (COBOLNET2191) while JUSTIFIED and BLANK WHEN ZERO, named in the SAME
/// sentence (§13.16.3 SR11), had none; BASED×EXTERNAL was refused while REDEFINES×EXTERNAL, named in the SAME
/// sentence (SR5), was not; and the EXTERNAL/GLOBAL level and entry-name rules lived only as <c>continue</c>
/// filters inside the post-bind registration scan, so a misplaced clause was silently ANNULLED rather than
/// reported. Each was the two-arm defect: one arm of a sentence enforced, its sibling not.</para>
///
/// <para><b>The two sites.</b> The entry-local rows (<see cref="ClausePlacementKind.Residence"/>,
/// <see cref="ClausePlacementKind.DataNameRequired"/>, <see cref="ClausePlacementKind.NotWith"/>) run in
/// <c>BindEntry</c> through <see cref="ScreenClausePlacement"/>, where the level-number, the entry-name, the
/// section and the WRITTEN clause set (<see cref="DataClauseKind"/>) are all known. The
/// <see cref="ClausePlacementKind.ElementaryOnly"/> rows run over the finished forest
/// (<see cref="CheckElementaryOnlyClauses"/>), because whether an entry has subordinates is not known until
/// the entries after it are bound.</para>
///
/// <para><b>Recovery is REFUSAL of the clause, and the refusal is load-bearing.</b> A refused EXTERNAL or GLOBAL
/// clause never reaches <see cref="DataItem.HasExternalClause"/> / <see cref="DataItem.HasGlobalClause"/>, which
/// are the ONLY inputs of the run-unit re-basing and the global-name registration
/// (<c>CallBindExternalAndGlobal</c>). That is what closes PB519's runtime arm: an EXTERNAL clause on a
/// REDEFINES entry used to fold the redefines ANCHOR onto the external cell and discard the anchor's VALUE.</para>
///
/// <para><b>The table is total over the clause vocabulary.</b> <see cref="PlacementAnsweredElsewhere"/> names,
/// for every <see cref="DataClauseKind"/> with no row here, where its placement rule IS enforced (or that
/// §13.16.3 states none). <c>DataClausePlacementDriftTests</c> is red when a clause kind is in neither, so a
/// clause added to the grammar forces the question "where may it be written?" rather than inheriting silence.</para>
/// </summary>
internal static class ClausePlacementRules
{
    /// <summary>The rules, in §13.16.3 order where they come from §13.16.3.</summary>
    public static readonly IReadOnlyList<ClausePlacementRule> Rules =
    [
        // §13.18.40.3 SR1 — the PICTURE row predates the table (kb/Work PB527) and keeps its own code.
        new(DataClauseKind.Picture, ClausePlacementKind.ElementaryOnly, "§13.18.40.3 SR1",
            "The PICTURE clause may be specified only at the elementary level.",
            DiagnosticCatalog.PictureAtElementaryLevel),
        // §13.16.3 SR11 (restated for JUSTIFIED by §13.18.32.3 SR1) — kb/Work PB512.
        new(DataClauseKind.Justified | DataClauseKind.BlankWhenZero, ClausePlacementKind.ElementaryOnly,
            "§13.16.3 SR11",
            "The PICTURE, JUSTIFIED, and BLANK WHEN ZERO clauses may be specified only for an elementary data item.",
            DiagnosticCatalog.ClauseElementaryOnly),
        // §13.16.3 SR5 — kb/Work PB519. The BASED half used to be a private arm of the BASED block.
        new(DataClauseKind.External, ClausePlacementKind.NotWith, "§13.16.3 SR5",
            "The EXTERNAL clause shall not be specified in the same data description entry as the REDEFINES or "
            + "BASED clause.",
            DiagnosticCatalog.DataClausePlacement, Excluded: DataClauseKind.Redefines | DataClauseKind.Based),
        // §13.16.3 SR6 with §13.18.27.3 SR1 b) — kb/Work PB518.
        new(DataClauseKind.Global, ClausePlacementKind.Residence, "§13.16.3 SR6 / §13.18.27.3 SR1 b)",
            "The CONSTANT RECORD and GLOBAL clauses may be specified only in data description entries whose "
            + "level-number is 1.",
            DiagnosticCatalog.DataClausePlacement, Sections: EntrySections.All),
        // §13.18.22.3 SR1 — PB518's sibling: the registration scan's `externalLegal` filter was its only site.
        new(DataClauseKind.External, ClausePlacementKind.Residence, "§13.18.22.3 SR1",
            "The EXTERNAL clause may be specified only in file description entries, in level 1 data description "
            + "entries in the working-storage section, and in level 1 type declarations.",
            DiagnosticCatalog.DataClausePlacement, Sections: EntrySections.WorkingStorage,
            TypeDeclarationInAnySection: true),
        // §13.16.3 SR16 — kb/Work PB516. Both sentences in one row: the level half used to be a private arm of the
        // BASED block and the SECTION half was asked by nothing, so `FD F. 01 R BASED PIC X(10).` compiled — a
        // record area the file subsystem fills, described at once as an unallocated template. The TYPE / SAME AS
        // composed BASED asks the same row (DataBinder.ScreenComposedBased, ResidenceFault).
        new(DataClauseKind.Based, ClausePlacementKind.Residence, "§13.16.3 SR16",
            "The BASED clause may be specified only in data description entries in the linkage section, in the "
            + "working-storage section, and in the local-storage section. The level number of such data description "
            + "entries shall be 1 or 77.",
            DiagnosticCatalog.DataClausePlacement,
            Sections: EntrySections.Linkage | EntrySections.WorkingStorage | EntrySections.LocalStorage,
            ResidenceLevels: [1, 77]),
        // §13.16.3 SR21 — kb/Work PB1273. It was a private arm of the OO pass, which walks only the object/factory
        // working storage it synthesizes accessors for, so the refusal hung on SR1's placement being right.
        new(DataClauseKind.Property, ClausePlacementKind.NotWith, "§13.16.3 SR21",
            "The PROPERTY clause shall not be specified in the same data description entry as: a) a BASED clause, "
            + "b) a TYPEDEF clause.",
            DiagnosticCatalog.PropertyWithBasedOrTypedef, Excluded: DataClauseKind.Based | DataClauseKind.Typedef),
        // §13.18.42.3 SR3's ELEMENTARY half — kb/Work PB1273. Its qualification half ("whose name does not require
        // qualification for uniqueness of reference") asks the object's whole name space and is
        // OoBindPropertyClauses'; SR1 (object / factory working storage only) is BindEntry's, because its axis is
        // the source element, not the section.
        new(DataClauseKind.Property, ClausePlacementKind.ElementaryOnly, "§13.18.42.3 SR3",
            "The PROPERTY clause may be specified only for an elementary item whose name does not require "
            + "qualification for uniqueness of reference.",
            DiagnosticCatalog.PropertyClauseRule),
        // §13.18.38.3 SR1 a) — kb/Work PB1260. The OCCURS clause's level rule: no table at level 01 or 77 (66 and 88 are
        // the same sentence; they reach the binder as RENAMES / condition-name entries, which carry no OCCURS clause,
        // and the grammar refuses the clause there). Level 01 is the one an entry binder SEES: `01 A PIC X OCCURS 3`
        // compiled as a table. The sibling SR1 b) (an occurs-depending table subordinate to the OCCURS entry) and SR10
        // (seven subscripts) need the finished forest and are OdoResolve's.
        new(DataClauseKind.Occurs, ClausePlacementKind.NotAtLevel, "§13.18.38.3 SR1 a)",
            "The OCCURS clause shall not be specified in a data description entry that: a) Has a level-number of 01, "
            + "66, 77, or 88",
            DiagnosticCatalog.DataClausePlacement, Levels: [1, 66, 77, 88]),
        // §13.16.3 SR7 — kb/Work PB518. The FD-record half is ScreenFileRecordEntryNames.
        new(DataClauseKind.External | DataClauseKind.Global, ClausePlacementKind.DataNameRequired, "§13.16.3 SR7",
            "The data-name format of the entry-name clause shall be specified for any entry containing the GLOBAL "
            + "or EXTERNAL clause, or for record descriptions associated with a file description entry that "
            + "contains the EXTERNAL or GLOBAL clause.",
            DiagnosticCatalog.DataClausePlacement),
    ];

    /// <summary>Every clause kind with NO row above, and where its placement is answered. ⛔ Not a work list:
    /// each entry names an EXISTING enforcement site or states that §13.16.3 / the clause's own syntax rules
    /// restrict nothing about placement. <c>DataClausePlacementDriftTests</c> requires every
    /// <see cref="DataClauseKind"/> to be in exactly one of the two.</summary>
    public static readonly IReadOnlyDictionary<DataClauseKind, string> PlacementAnsweredElsewhere =
        new Dictionary<DataClauseKind, string>
        {
            [DataClauseKind.Usage] = "no placement rule — group or elementary (§13.18.60.4 GR1 inheritance)",
            [DataClauseKind.Redefines] = "§13.18.44.3 — ScreenLeadingClausePosition (position) / ScreenRedefinesEntry (DataBinder.RedefinesEntry.cs: level, file-section, size, alignment)",
            [DataClauseKind.Value] = "§13.18.63.3 — ScreenValueLiteral / CheckGroupValueDeclarations",
            [DataClauseKind.Sign] = "§13.18.52.3 — InheritSignClauses (group and elementary both legal)",
            [DataClauseKind.Synchronized] = "§13.18.55 — group legal at 2023 (UsageInheritanceGroup's edition gate)",
            [DataClauseKind.Aligned] = "§13.18.1.3 SR1 — CheckAlignedClauses",
            [DataClauseKind.ConstantRecord] = "§13.16.3 SR6 / §13.18.15.3 SR1 — BindEntry's CONSTANT RECORD block and BindEntries",
            [DataClauseKind.Type] = "§13.16.3 SR14 — BindEntry's TYPE composition check",
            [DataClauseKind.Typedef] = "§13.16.3 SR15 — RegisterTypeDecl and BindEntries (level 1, data-name); SR4 — ScreenLeadingClausePosition (first clause)",
            [DataClauseKind.SameAs] = "§13.16.3 SR12 — BindEntry's SAME AS composition check",
            [DataClauseKind.AnyLength] = "§13.18.2.3 — AnyLengthValidateUnit (linkage, elementary)",
            [DataClauseKind.DynamicLength] = "§13.18.19.3 — BindEntry's DYNAMIC LENGTH block",
            [DataClauseKind.GroupUsage] = "§13.18.29.3 SR1 — UsageInheritanceGroup / UsageInheritanceElementary",
            [DataClauseKind.SelectWhen] = "Annex A.4.8 — declined by name in BindEntry (COBOLNET1560 family)",
            [DataClauseKind.Validation] = "§13.18.64 family — validation clauses have no §13.16.3 placement rule",
            [DataClauseKind.Unrecognized] = "not a clause — refused by name (COBOLNET1941)",
        };
}

public sealed partial class DataBinder
{
    /// <summary>The entry-local rows of <see cref="ClausePlacementRules.Rules"/>, applied to one data description
    /// entry. Returns the clauses REFUSED — the caller drops each one's effect, so a refused clause binds as
    /// though it had not been written, under an already-failed compile. One diagnostic per violated rule.</summary>
    private DataClauseKind ScreenClausePlacement(DataClauseKind written, int level, bool isFiller,
        EntrySection section, string entryWhere)
    {
        var refused = DataClauseKind.None;
        foreach (var rule in ClausePlacementRules.Rules)
        {
            var subject = written & rule.Clauses;
            if (subject == DataClauseKind.None) continue;
            string? why = rule.Kind switch
            {
                // Level 77 is not level 1: §13.16.3 SR1 lists them as distinct level-numbers, and a residence
                // sentence admits 77 only where it says so (SR16 — the row's ResidenceLevels).
                ClausePlacementKind.Residence =>
                    rule.ResidenceFault(level, section, (written & DataClauseKind.Typedef) != 0),
                ClausePlacementKind.NotAtLevel when rule.Levels!.Contains(level) =>
                    $"this entry's level-number is {level:00}",
                ClausePlacementKind.DataNameRequired when isFiller =>
                    "this entry has no data-name (the FILLER format of the entry-name clause, written or implied)",
                ClausePlacementKind.NotWith when (written & rule.Excluded) is var bad && bad != DataClauseKind.None =>
                    $"this entry also specifies {DataClauseKinds.Name(bad)}",
                _ => null,
            };
            if (why is null) continue;
            bool one = (subject & (subject - 1)) == 0;
            Edition.Error(rule.Code, $"{entryWhere}: the {DataClauseKinds.Name(subject)} clause{(one ? " is" : "s are")} "
                + $"not admitted here — {why}. \"{rule.Sentence}\" (ISO {rule.Rule})");
            refused |= subject;
        }
        return refused;
    }

    /// <summary>The clauses whose POSITION in the entry the standard fixes — "shall immediately follow the
    /// entry-name clause" — each with the rule that says so and the code it reports under. The REDEFINES row is
    /// §13.18.44.3 SR1 (restated by §13.16.3 SR4); the TYPEDEF row is §13.16.3 SR4 alone, which nothing asked until
    /// kb/Work PB486, so `01 T PIC X(4) IS TYPEDEF.` compiled. §13.16.3 SR4 ends "The remaining clauses may be
    /// written in any order", so this set is closed.</summary>
    private static readonly (DataClauseKind Kind, string Rule, DiagnosticDescriptor Code)[] LeadingClauses =
    [
        (DataClauseKind.Redefines, "§13.18.44.3 SR1", DiagnosticCatalog.RedefinesEntryRule),
        (DataClauseKind.Typedef, "§13.16.3 SR4", DiagnosticCatalog.DataClausePlacement),
    ];

    /// <summary>§13.16.3 SR4's positional sentences: "The REDEFINES clause, if specified, shall immediately follow
    /// the entry-name clause, if specified; otherwise, the REDEFINES clause shall immediately follow the
    /// level-number. If the TYPEDEF clause is specified, … the TYPEDEF clause shall immediately follow the
    /// entry-name clause." The clause list is free-order (<c>dataDescriptionClause*</c>), so the check is each
    /// clause's ORDINAL in the entry: the entry-name and level-number are not in the list, which makes "immediately
    /// follows" exactly "is its FIRST clause". (A TYPEDEF entry without a data-name is SR15's, asked by
    /// <c>RegisterTypeDecl</c>; REDEFINES beside TYPEDEF is SR3's, so such an entry draws both.)</summary>
    private void ScreenLeadingClausePosition(DataDescriptionCst entry, string? cobolName)
    {
        var clauses = entry.Clauses;
        for (int i = 1; i < clauses.Count; i++)
            foreach (var (kind, rule, code) in LeadingClauses)
                if (clauses[i].Kind == kind)
                    Edition.Error(code, $"'{cobolName ?? "FILLER"}': the {DataClauseKinds.Name(kind)} clause follows "
                        + $"{DataClauseKinds.Name(clauses[0].Kind)} but shall immediately follow the entry-name clause"
                        + (kind == DataClauseKind.Redefines
                            ? ", or the level-number when the entry-name clause is not specified" : "")
                        + $" (ISO {rule})");
    }

    internal static string SectionWords(EntrySection section) => section switch
    {
        EntrySection.WorkingStorage => "working-storage section",
        EntrySection.LocalStorage => "local-storage section",
        EntrySection.Linkage => "linkage section",
        EntrySection.File => "file section",
        _ => section.ToString(),
    };

    /// <summary>§13.16.3 SR7's SECOND half — "…or for record descriptions associated with a file description entry
    /// that contains the EXTERNAL or GLOBAL clause" — the one row whose subject is the FD rather than the entry.
    /// Called once per FD after its clause loop, when <see cref="FileModel.IsExternal"/> / <c>IsGlobal</c> are
    /// known and before an implied record is materialized (that record is the compiler's, not written).</summary>
    private void ScreenFileRecordEntryNames(FileModel file)
    {
        if (!file.IsExternal && !file.IsGlobal) return;
        var rule = ClausePlacementRules.Rules.First(r => r.Kind is ClausePlacementKind.DataNameRequired);
        string clause = file.IsExternal ? (file.IsGlobal ? "EXTERNAL and GLOBAL" : "EXTERNAL") : "GLOBAL";
        foreach (var record in file.Records)
        {
            if (record.CobolName is not null) continue;
            using var _ = Edition.At(record);
            Edition.Error(rule.Code, $"a record description entry of file '{file.CobolName}' has no data-name, and "
                + $"the file description entry contains the {clause} clause. \"{rule.Sentence}\" (ISO {rule.Rule})");
        }
    }

    /// <summary>The <see cref="ClausePlacementKind.ElementaryOnly"/> rows over the finished forest — the
    /// "has this clause ⇒ is elementary" direction for PICTURE (§13.18.40.3 SR1), JUSTIFIED and BLANK WHEN ZERO
    /// (§13.16.3 SR11). §8.5.1.3.1: "The most basic subdivisions of a record, that is, those not further
    /// subdivided, are called elementary items", so the test is <see cref="HasNoSubordinates"/>, never
    /// <see cref="DataItem.IsElementary"/> (which is DEFINED as <c>Pic is not null</c> and would make the PICTURE
    /// row circular).
    /// <para>Reported over <see cref="ConformanceForest"/> (a defective TYPEDEF template is named once, at the
    /// entry the programmer must change) and RECOVERED over <see cref="CompositionForest"/> (every clone
    /// <c>ExpandTypes</c> made is repaired too). The recovery clears the clause, never the subordinates: for
    /// PICTURE that makes the entry the group its hierarchy declares (kb/Work PB527 — the model cannot hold an
    /// entry that is both), and for JUSTIFIED / BLANK WHEN ZERO it drops a flag no group consumer may read.</para></summary>
    private void CheckElementaryOnlyClauses()
    {
        foreach (var item in ConformanceForest())
        {
            if (HasNoSubordinates(item)) continue;
            foreach (var rule in ClausePlacementRules.Rules)
            {
                if (rule.Kind is not ClausePlacementKind.ElementaryOnly) continue;
                var present = ElementaryOnlyClausesOn(item) & rule.Clauses;
                if (present == DataClauseKind.None) continue;
                int n = item.Children.Count;
                using var _ = Edition.At(item);
                Edition.Error(rule.Code, $"data item '{item.CobolName ?? "FILLER"}': the {DataClauseKinds.Name(present)} "
                    + $"clause may be specified only at the elementary level (ISO {rule.Rule}) — this entry has {n} "
                    + $"subordinate {(n == 1 ? "entry" : "entries")}, so it is not an elementary item (§8.5.1.3.1: the "
                    + "elementary items are the subdivisions \"not further subdivided\"). Remove "
                    + (present == DataClauseKind.Picture && item.PictureText is { } pt
                        ? $"PICTURE {pt}" : $"the {DataClauseKinds.Name(present)} clause")
                    + " from this entry, or remove the subordinate entries");
            }
        }

        foreach (var item in CompositionForest())
        {
            if (HasNoSubordinates(item)) continue;
            item.Pic = null;              // the entry becomes the group its hierarchy declares
            item.Justified = false;
            item.BlankWhenZero = false;
            item.Property = null;         // a refused PROPERTY defines no accessor (§13.18.42.3 SR3)
        }
    }

    /// <summary>The <see cref="ClausePlacementKind.ElementaryOnly"/> subject clauses <paramref name="item"/>
    /// carries, read from the bound facts each clause leaves. ⛔ A clause joining an ElementaryOnly row adds its
    /// arm HERE and its recovery in <see cref="CheckElementaryOnlyClauses"/>; <c>DataClausePlacementDriftTests</c>
    /// is red for an ElementaryOnly subject this method does not report.</summary>
    internal static DataClauseKind ElementaryOnlyClausesOn(DataItem item) =>
        (item.Pic is not null ? DataClauseKind.Picture : DataClauseKind.None)
        | (item.Justified ? DataClauseKind.Justified : DataClauseKind.None)
        | (item.BlankWhenZero ? DataClauseKind.BlankWhenZero : DataClauseKind.None)
        | (item.Property is not null ? DataClauseKind.Property : DataClauseKind.None);

    /// <summary>The SUBJECT rules of BLANK WHEN ZERO and JUSTIFIED — what the elementary item they are written on
    /// may BE (kb/Work PB507). Runs after <c>UsageInheritancePass</c> and <see cref="CheckPictureRequired"/>, because
    /// §13.18.8.3 SR2 reads the usage the subject is "implicitly or explicitly described as", and §13.18.60.4 GR1
    /// inheritance supplies the implicit half: <c>01 G COMP. 05 A PIC 999 BLANK WHEN ZERO.</c> violates SR2 as
    /// surely as the direct spelling.
    /// <list type="bullet">
    /// <item>§13.18.8.3 SR1 — category numeric-edited, or numeric without 'S'. The category is the one the
    /// PICTURE character-string gives, which §13.18.8.4 GR2 then re-defines as numeric-edited for a display /
    /// national numeric subject; so both categories are admitted here. The 'S' half is §13.18.40.3's own
    /// SR22 (COBOLNET1934, PictureComposition) — the same fact, reported once, at the picture.</item>
    /// <item>§13.18.8.3 SR2 — usage display or national, written or inherited.</item>
    /// <item>§13.18.32.3 SR3 — category alphabetic, alphanumeric, boolean or national (never an EDITED
    /// category: <see cref="PicInfo.EditMask"/> is what makes one, see its doc).</item>
    /// <item>§13.18.32.3 SR4 — "shall not be specified for a dynamic-length elementary item" — has NO arm here,
    /// deliberately: §13.16.3 SR18 already refuses JUSTIFIED beside DYNAMIC LENGTH (the only other clauses it
    /// permits are level-number, entry-name, PICTURE, USAGE and VALUE — COBOLNET1563, BindEntry), which is the
    /// same fact reported once. No TYPE / SAME AS path can compose the pair either: both copy from an entry that
    /// SR18 has already screened.</item>
    /// </list>
    /// Recovery clears the clause, so no consumer applies a blank-when-zero or a right justification to an item the
    /// clause may not describe. Reported over <see cref="ConformanceForest"/>, recovered over
    /// <see cref="CompositionForest"/>, the two-forest discipline.</summary>
    internal void CheckClauseSubjects()
    {
        foreach (var item in ConformanceForest())
        {
            if (!HasNoSubordinates(item) || item.Pic is not { } pic) continue;
            string name = item.CobolName ?? "FILLER";
            if (item.BlankWhenZero && BlankWhenZeroViolation(pic) is { } bwz)
            {
                using var _ = Edition.At(item);
                Edition.Error(DiagnosticCatalog.ClauseSubjectCategory, $"data item '{name}': {bwz}");
            }
            if (item.Justified && JustifiedViolation(pic) is { } jus)
            {
                using var _ = Edition.At(item);
                Edition.Error(DiagnosticCatalog.ClauseSubjectCategory, $"data item '{name}': {jus}");
            }
        }

        foreach (var item in CompositionForest())
        {
            if (item.Pic is not { } pic) continue;
            if (item.BlankWhenZero && BlankWhenZeroViolation(pic) is not null) item.BlankWhenZero = false;
            if (item.Justified && JustifiedViolation(pic) is not null) item.Justified = false;
        }
    }

    private static string? BlankWhenZeroViolation(PicInfo pic)
    {
        if (pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited))
            return "the BLANK WHEN ZERO clause may be specified only for an elementary item described by its picture "
                + "character-string as category numeric-edited or as numeric without the picture symbol 'S' "
                + $"(ISO §13.18.8.3 SR1) — this item is {CategoryWords(pic)}";
        if (pic.Usage is not (Usage.Display or Usage.National))
            return "the subject of a BLANK WHEN ZERO clause shall be implicitly or explicitly described as usage "
                + $"display or usage national (ISO §13.18.8.3 SR2) — this item's usage is {UsageFamilies.UsageWord(pic.Usage)}";
        return null;
    }

    private static string? JustifiedViolation(PicInfo pic)
    {
        if (pic.Category is not (PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean)
            || pic.EditMask is not null)
            return "the JUSTIFIED clause may be specified only for a data item whose category is alphabetic, "
                + $"alphanumeric, boolean, or national (ISO §13.18.32.3 SR3) — this item is {CategoryWords(pic)}";
        return null;
    }

    /// <summary>The §8.5.2 category of <paramref name="pic"/>, in the standard's words, for a message.</summary>
    private static string CategoryWords(PicInfo pic) => pic.Category switch
    {
        PicCategory.Alphanumeric when pic.EditMask is not null => "of category alphanumeric-edited",
        PicCategory.Alphanumeric when pic.IsAlphabetic => "of category alphabetic",
        PicCategory.Alphanumeric => "of category alphanumeric",
        PicCategory.National when pic.EditMask is not null => "of category national-edited",
        PicCategory.National => "of category national",
        PicCategory.Numeric => "of category numeric",
        PicCategory.NumericEdited => "of category numeric-edited",
        PicCategory.Boolean => "of category boolean",
        _ => $"of usage {UsageFamilies.UsageWord(pic.Usage)}",
    };
}
