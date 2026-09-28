// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding.Model;

/// <summary>The file organization (ISO/IEC 1989:2023 §12.4.5.10). This slice implements the two sequential shapes;
/// relative/indexed are later G5 slices (registered loud).</summary>
public enum FileOrganization { Sequential, LineSequential, Relative, Indexed }

/// <summary>The access mode (ISO §12.4.5.3). Sequential is the default and the only mode the sequential slice needs.</summary>
public enum FileAccessMode { Sequential, Random, Dynamic }

/// <summary>The SHARING mode (ISO §12.4.5.15 / §14.9.27): how OTHER file connectors may access this file while
/// it is open. <c>None</c> = the implementor default (WiseOwl COBOL: ALL OTHER). NoOther = exclusive.</summary>
public enum SharingMode { None, AllOther, NoOther, ReadOnly }

/// <summary>The LOCK MODE granularity (ISO §12.4.5.9): MANUAL (locks acquired only by an explicit WITH LOCK) vs
/// AUTOMATIC (a READ locks the record). <see cref="Multiple"/> = WITH LOCK ON MULTIPLE RECORDS (else single —
/// any I-O except START releases the prior lock, §12.4.5.9 GR6).</summary>
public enum LockKind { None, Manual, Automatic }
public sealed record LockModeInfo(LockKind Kind, bool Multiple);

/// <summary>One ALTERNATE RECORD KEY clause of a file control entry, AS WRITTEN (ISO §12.4.5.6.2 — data-name-1,
/// its IN/OF qualifiers, the WITH DUPLICATES and SUPPRESS WHEN phrases), plus the item data-name-1 resolved to
/// and the source position its syntax rules report at.
/// <para>⛔ It is a carrier for the clause, not for the resolved key: <c>FileModel.AlternateKeys</c> holds only the
/// clauses that RESOLVED (its index is the runtime key index), and a clause that resolved to nothing is simply
/// missing from it. §12.4.5.6.3's syntax rules are stated about the CLAUSE, so the screen reads this list — which
/// is why <c>Item</c> is nullable here and not there.</para></summary>
public sealed class AlternateKeyClause
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Qualifiers { get; init; }
    public bool Duplicates { get; init; }

    /// <summary>The SUPPRESS WHEN phrase's literal-1 as the literal position read it (ISO §12.4.5.6.2) — set by
    /// <c>DataBinder.ResolveFiles</c>, because a constant-name operand (§13.10.3 SR2) is declared in the DATA
    /// DIVISION, after the file control entry. Null when the clause has no SUPPRESS WHEN phrase, or when its
    /// operand is no literal at all (already reported by the literal-position screen).</summary>
    public SuppressWhenOperand? SuppressWhen { get; set; }

    /// <summary>Where data-name-1 is written — the cursor §12.4.5.6.3's post-build screens report at.</summary>
    public CobolNet.Editions.DiagnosticCursor At { get; init; }

    /// <summary>What data-name-1 resolved to (set by <c>DataBinder.ResolveFiles</c>); null when the name
    /// references nothing describable — itself a §12.4.5.6.3 SR2 violation.</summary>
    public DataItem? Item { get; set; }
}

/// <summary>Which of the shapes ISO §12.4.5.6.3 SR7 names a SUPPRESS WHEN literal-1 was written in.</summary>
public enum SuppressWhenForm
{
    /// <summary>An alphanumeric, national or boolean literal (a concatenation expression and a constant-name
    /// arrive as the one literal they stand for — §8.8.3.3 GR3, §13.10.4 GR1).</summary>
    Literal,
    /// <summary>The Format-6 figurative <c>ALL literal-1</c>, or a symbolic-character (§8.3.3.6.2 Format 7),
    /// which stands for the ALL form of its one character.</summary>
    AllLiteral,
    /// <summary>A keyword figurative constant — ZERO, SPACE, QUOTE, HIGH-VALUE or LOW-VALUE (§8.3.3.6.2
    /// Formats 1–5), with or without the optional word ALL.</summary>
    Figurative,
    /// <summary>Neither an alphanumeric nor a national literal nor a character figurative: a numeric literal or
    /// the pointer figurative NULL. SR7 refuses it.</summary>
    NotAlphanumericOrNational,
}

/// <summary>The SUPPRESS WHEN literal-1 of one ALTERNATE RECORD KEY clause (ISO §12.4.5.6.2), read ONCE through the
/// data division's literal-position chokepoint — the operand §12.4.5.6.3 SR7 screens and from which the §12.4.5.6.4
/// GR6 key suppression value is built.</summary>
/// <param name="Written">The operand as written, for diagnostics.</param>
/// <param name="Form">Which SR7 shape it is.</param>
/// <param name="Class">The literal's class for <see cref="SuppressWhenForm.Literal"/> and
/// <see cref="SuppressWhenForm.AllLiteral"/>; null for a keyword figurative (§8.3.3.6.4 GR1 — it takes the class
/// of the context) and for <see cref="SuppressWhenForm.NotAlphanumericOrNational"/>.</param>
/// <param name="Characters">The decoded characters of literal-1 (the Format-6 literal-1 for ALL); empty for a
/// keyword figurative.</param>
/// <param name="FigurativeKind">The keyword figurative's kind (<c>FigurativeConstants.KindOf</c>'s letters);
/// null otherwise.</param>
public sealed record SuppressWhenOperand(string Written, SuppressWhenForm Form, CobolNet.Common.LiteralClass? Class,
    string Characters, char? FigurativeKind);

/// <summary>The §12.4.5.6.4 GR6 key suppression value of one resolved alternate key, as the relation condition
/// compares it with the key: <paramref name="Value"/> is in the key's own character positions — a figurative or
/// ALL literal repeated to the key's size (§8.3.3.6.4 GR2), a shorter literal extended with spaces (§8.8.4.2.7 2)),
/// a longer one cut back to the key's size when every excess position is a space — and
/// <paramref name="National"/> says those positions are national characters, which the record image carries as
/// UTF-16BE byte pairs (determination D-N1).</summary>
public readonly record struct KeySuppression(string Value, bool National);

/// <summary>
/// A bound file connector (COBOLNET_DESIGN §8): the SELECT clause's properties joined with the FD's record
/// description(s). The FD's record area is a typed field — for multiple <c>01</c>s under one FD they SHARE one
/// storage area (ISO §9.1.2 / §13.18 — the file-edge analogue of REDEFINES), modeled by synthesizing each secondary
/// record as a REDEFINES of the first so the existing <see cref="RedefinesClass"/> tier machinery makes them alias
/// (the singular-pattern rule — no second storage-sharing mechanism). A WRITE/READ moves the record's character
/// image across the on-disk edge.
/// </summary>
public sealed class FileModel
{
    /// <summary>The COBOL file-name (the SELECT / FD name). At emission the multi-unit driver QUALIFIES it with
    /// the owning program's path ("PROG::FILE") to namespace the run-unit-global runtime registry — a file
    /// connector is internal to its program (ISO §8.6.3; two IC-suite units both declare PRINT-FILE). Name
    /// resolution is finished by then (bound nodes hold FileModel references), so the rename is emit-only.</summary>
    public required string CobolName { get; set; }

    /// <summary>The file-name EXACTLY as spelled in the SELECT clause (or the FD/SD when no SELECT names it) —
    /// never qualified, never case-folded: FUNCTION EXCEPTION-FILE / EXCEPTION-FILE-N return "the file-name exactly
    /// as specified in the SELECT clause" (ISO §15.28.4 r1c/r2b, §15.29.4 r1c/r2b; kb/Work PB63), so the connector
    /// is registered with THIS name and the runtime never recovers a display name from the registry KEY (whose
    /// bands — <c>PROG::</c>, <c>::EXT::</c> + the uppercased external name, <c>Class::INST::</c> + a per-object
    /// <c>#N</c> — are an emit-side namespace).</summary>
    public string SelectName { get; init; } = "";

    /// <summary>The ASSIGN clause's TO-phrase target text — literal-1's decoded value or device-name-1 (ISO
    /// §12.4.5.3 GR3 a); resolved to a host path at run time by <c>CobolFile.ResolveHostPath</c>. Defaults to the
    /// file-name when no ASSIGN clause is present. ⛔ EMPTY for the bare <c>ASSIGN USING data-name-1</c> form, which
    /// specifies no device-name-1/literal-1 at all: the connector then registers UNASSOCIATED and
    /// <see cref="AssignUsingName"/>'s content associates it at each OPEN/SORT/MERGE (GR3 b). Defaulting it to the
    /// file-name there is what made a bare-USING file's records land in <c>&lt;file-name&gt;.txt</c>.</summary>
    public string AssignTarget { get; set; } = "";

    /// <summary>The ASSIGN clause's USING data-name-1 as written (ISO §12.4.5.3 GR3 b — dynamic file assignment,
    /// §9.1.21), resolved post-build to <see cref="AssignUsingItem"/>; null when the clause has no USING phrase.
    /// Present on BOTH general-format arms: <c>ASSIGN TO device-name-1 … USING data-name-1</c> and the bare
    /// <c>ASSIGN USING data-name-1</c> — GR3 b applies "when the USING phrase … is specified", with no condition on
    /// the TO phrase, so the USING content wins over literal-1 whenever both are written.</summary>
    public string? AssignUsingName { get; set; }

    /// <summary>The USING data-name-1 reference's IN/OF qualifier words, written order (innermost first); empty when
    /// unqualified. §12.4.5.2 SR7 lets data-name-1 be any alphanumeric item outside this file's FD, so it can need
    /// qualification exactly as a RECORD KEY reference can (§8.4.2.2).</summary>
    public IReadOnlyList<string> AssignUsingQualifiers { get; set; } = [];

    /// <summary>The resolved ASSIGN … USING data item (set post-build, the <see cref="FileStatusItem"/> pattern);
    /// null when the clause has no USING phrase or the name resolves to nothing.</summary>
    public DataItem? AssignUsingItem { get; set; }

    /// <summary>The source position of the USING data-name-1 reference — the cursor §12.4.5.2 SR7's diagnostics
    /// report at, since the rule can only be checked once the data forest is indexed (post-build).</summary>
    public CobolNet.Editions.DiagnosticCursor AssignUsingAt { get; set; }

    /// <summary>The file organization (default SEQUENTIAL).</summary>
    public FileOrganization Organization { get; set; } = FileOrganization.Sequential;

    /// <summary>Whether the entry WROTE an ORGANIZATION clause. False is the §12.4.5.10.3 GR6 default —
    /// <i>"When the ORGANIZATION clause is not specified, sequential organization with the RECORD SEQUENTIAL
    /// phrase is implied"</i> — which <see cref="Organization"/> alone cannot distinguish from a written
    /// <c>ORGANIZATION IS SEQUENTIAL</c>. Every rule that REPORTS a file's organization needs the difference:
    /// the common shape of a §12.4.5.2 SR8 violation is `SELECT F ASSIGN … RECORD KEY IS K.` with no clause at
    /// all, and a message that just said "this file is sequential" would name something the source does not
    /// contain (kb/Work PB742).</summary>
    public bool OrganizationWritten { get; set; }

    /// <summary>⛔ THE ONE English face of this entry's organization, for a diagnostic that has to name it — the
    /// written phrase, or GR6's implied default said as the default it is. Three rules render it (§12.4.5.5.2
    /// SR2's ACCESS screen, §12.4.5.2 SR8 over the COLLATING SEQUENCE clause, and SR8/SR9 over the key clauses in
    /// <see cref="FileControlKeyRules"/>); one phrasing keeps them from drifting into three different names for
    /// the same file. A SORT-MERGE file has no organization to name — §12.4.5.1 Format 4 admits only the
    /// SEQUENTIAL phrase and the entry is described by an SD — so it says what it is instead.</summary>
    public string OrganizationFace =>
        IsSortMerge ? "a sort-merge file (described by a sort-merge file description entry)"
        : !OrganizationWritten ? "record sequential by default, the ORGANIZATION clause being omitted (ISO §12.4.5.10.3 GR6)"
        : Organization switch
        {
            FileOrganization.LineSequential => "LINE SEQUENTIAL",
            FileOrganization.Relative => "RELATIVE",
            FileOrganization.Indexed => "INDEXED",
            _ => "sequential",
        };

    /// <summary>The access mode (default SEQUENTIAL).</summary>
    public FileAccessMode AccessMode { get; set; } = FileAccessMode.Sequential;

    /// <summary>True for a SELECT OPTIONAL file.</summary>
    public bool Optional { get; set; }

    /// <summary>The FILE STATUS data-name as written, resolved to <see cref="FileStatusItem"/> post-build; null if none.</summary>
    public string? FileStatusName { get; set; }

    /// <summary>The FILE STATUS data-name's IN/OF qualifier words, innermost first (ISO §8.4.2.2). Kept because
    /// the clause's operand is a qualified-data-name and a same-named item elsewhere in the source element makes
    /// the qualifier the ONLY thing that identifies it — dropping it resolved the first declaration in order
    /// (kb/Work PB489, the shape measured on the LINAGE clause).</summary>
    public IReadOnlyList<string> FileStatusQualifiers { get; set; } = [];

    /// <summary>The source position of the FILE STATUS clause's data-name — where its post-build resolution
    /// reports (the operand resolves only once the data forest is indexed).</summary>
    public CobolNet.Editions.DiagnosticCursor FileStatusAt { get; set; }

    /// <summary>The resolved FILE STATUS data item (set post-build), or null if the file has no FILE STATUS clause.</summary>
    public DataItem? FileStatusItem { get; set; }

    /// <summary>The FD's record description(s), in declaration order. The first is the canonical storage area; every
    /// other shares it (synthesized REDEFINES).</summary>
    public List<DataItem> Records { get; } = [];

    /// <summary>The source position of the file control entry itself — the cursor the entry's own syntax rules
    /// report at when the violation is the ABSENCE of a clause (ISO §12.4.5.1's required members and §12.4.5.2
    /// SR10), since there is no clause to point at. See <see cref="FileControlKeyRules"/>.</summary>
    public CobolNet.Editions.DiagnosticCursor EntryAt { get; set; }

    /// <summary>The RECORD KEY base data-name as written (ISO §12.4.5.12), resolved post-build to
    /// <see cref="RecordKeyItem"/>; null when absent (the clause is required for ORGANIZATION INDEXED).</summary>
    public string? RecordKeyName { get; set; }

    /// <summary>The source position of the RECORD KEY clause's data-name-1 — where §12.4.5.12.3's syntax rules
    /// report, since they are screened post-build (the operand resolves only once the data forest is indexed).</summary>
    public CobolNet.Editions.DiagnosticCursor RecordKeyAt { get; set; }

    /// <summary>Whether the entry WROTE a RECORD KEY clause at all — which is not the same question as whether
    /// <see cref="RecordKeyName"/> is set, and §12.4.5.1 Format 1's requiredness rule asks THIS one: the clause
    /// is an unbracketed member of the indexed format, so the rule is about the clause's PRESENCE, never about
    /// what it named. The two answers diverge for exactly one shape — the declined
    /// <c>RECORD KEY IS record-key-name-1 SOURCE IS data-name-2 …</c> form (Annex A.3 item 40, COBOLNET1954,
    /// kb/Work PB358), which names a record-key-name (§8.3.2.2.24) and therefore no data-name. Without this,
    /// a program that wrote the clause and was told the form is not provided was ALSO told its indexed file
    /// "has no RECORD KEY clause", which is false and points at a different repair.</summary>
    public bool RecordKeyClauseWritten { get; set; }

    /// <summary>The RECORD KEY reference's IN/OF qualifier words, written order (innermost first) — identically
    /// named key items under different areas of the record are legal and selected by qualification (ISO
    /// §8.4.2.2; IX215A's three IX-FD3-KEY items). Empty = unqualified.</summary>
    public IReadOnlyList<string> RecordKeyQualifiers { get; set; } = [];

    /// <summary>The resolved prime RECORD KEY item — a data item within the file's record (ISO §12.4.5.12 SR2).</summary>
    public DataItem? RecordKeyItem { get; set; }

    /// <summary>The ALTERNATE RECORD KEY clauses as written, in declaration order (ISO §12.4.5.6), each carrying
    /// what its data-name-1 resolved to. ⛔ THIS is the §12.4.5.6.3 screen's subject list, NOT
    /// <see cref="AlternateKeys"/>: a clause whose data-name-1 references nothing describable is ABSENT from the
    /// resolved list, so the two are not index-aligned in exactly the case a syntax rule has to talk about.</summary>
    public List<AlternateKeyClause> AlternateKeyNames { get; } = [];

    /// <summary>The resolved alternate keys, in declaration order (the runtime key index is the list index).
    /// <c>Suppress</c> is the §12.4.5.6.4 GR6 key suppression value sized to the key (null = no SUPPRESS WHEN
    /// phrase, or an operand §12.4.5.6.3 SR7 refused).</summary>
    public List<(DataItem Item, bool Duplicates, KeySuppression? Suppress)> AlternateKeys { get; } = [];

    // ── §12.4.5.7 COLLATING SEQUENCE (INDEXED record-key collating) — raw capture, resolved post-build ──────────

    /// <summary>Format 1 (file-level) COLLATING SEQUENCE as written: (alphabet-name-1 [ALPHANUMERIC], alphabet-name-2
    /// [NATIONAL]); null when no file-level clause is present. <see cref="FileLevelCollatingCount"/> counts them for
    /// §12.4.5.7.3 SR3 (at most one per file control entry).</summary>
    public (string? Alnum, string? Nat)? FileLevelCollating { get; set; }
    public int FileLevelCollatingCount { get; set; }

    /// <summary>Where the (first) file-level clause is written — §12.4.5.7.3 SR1/SR2 report there.</summary>
    public CobolNet.Editions.DiagnosticCursor FileLevelCollatingAt { get; set; }

    /// <summary>Format 2 (key-level) COLLATING SEQUENCE clauses as written: each names one or more RECORD KEY /
    /// ALTERNATE RECORD KEY items and their alphabet-name-3 (§12.4.5.7.2 Format 2), and where the clause is written
    /// (§12.4.5.7.3 SR4/SR5/SR7/SR8 report there).</summary>
    public List<(IReadOnlyList<string> KeyNames, string Alphabet, CobolNet.Editions.DiagnosticCursor At)> KeyLevelCollating { get; } = [];

    /// <summary>The resolved PRIME key collating sequence (a literal-phrase table or a LOCALE sequence — the
    /// <see cref="AlphabetDef"/> the emitter renders as a runtime <c>CobolCollation</c>); null = native ordinal (no
    /// applicable COLLATING SEQUENCE clause, §12.4.5.3 GR6). Emitted into the runtime registration.</summary>
    public AlphabetDef? PrimeKeyCollation { get; set; }

    /// <summary>The resolved per-alternate-key collating sequences, index-aligned with <see cref="AlternateKeys"/>;
    /// each null = native ordinal.</summary>
    public List<AlphabetDef?> AlternateKeyCollations { get; } = [];

    /// <summary>The FD CODE-SET clause's resolved §13.18.13.4 GR6 correspondence — the NATIVE character each
    /// medium code unit of the named coded character set represents — or null when the file's medium is the
    /// native character set (no CODE-SET clause, GR7; or a clause naming NATIVE / UTF-16, whose correspondence
    /// IS the identity over the whole channel and needs no conversion). An ISO/IEC 646 set (STANDARD-1,
    /// STANDARD-2, ASCII) carries its 128-entry identity so the connector can refuse what the set lacks (kb/Work
    /// PB1542). The emitter renders it as the connector's <c>RegisterCodeSet</c>
    /// argument; it is a COMPILE-TIME artifact of the alphabet, exactly as <see cref="PrimeKeyCollation"/>'s
    /// weights are (kb/Work PB793).</summary>
    public char[]? CodeSetCorrespondence { get; set; }

    /// <summary>Which class the FD's CODE-SET clause selected for its §13.18.13.3 SR3 a)/b) record screen —
    /// <c>true</c> = alphabet-name-2 / national (b), <c>false</c> = alphabet-name-1 / alphanumeric (a),
    /// <c>null</c> = no clause, or a clause whose alphabet failed SR1/SR2 and is not screened against.
    /// <para>⛔ The screen itself is <c>DataBinder.CheckCodeSetRecordItems</c>, a DECLARED pass at
    /// <see cref="CobolNet.Binding.Passes.PassPhase.SignResolved"/>, not the FD walk: SR3's "described with the SIGN IS SEPARATE
    /// clause" and "described as usage display" are both facts a LATER pass settles (§13.18.52.4 GR1 group-SIGN
    /// inheritance; §13.18.60.4 GR1 group-USAGE inheritance), and reading them at the FD refused legal source
    /// (kb/Work PB536). This property is the only thing that travels between the two.</para></summary>
    public bool? CodeSetRecordClass { get; set; }

    /// <summary>The RELATIVE KEY data-name as written (ISO §12.4.5.13), resolved post-build; the item lives OUTSIDE
    /// the file's record (SR3) and holds the 1-based relative record number (GR1).</summary>
    public string? RelativeKeyName { get; set; }

    /// <summary>The RELATIVE KEY data-name's IN/OF qualifier words, innermost first (ISO §8.4.2.2) — see
    /// <see cref="FileStatusQualifiers"/> for why a clause operand keeps them (kb/Work PB489).</summary>
    public IReadOnlyList<string> RelativeKeyQualifiers { get; set; } = [];

    /// <summary>The source position of the RELATIVE KEY clause's data-name-1 — where §12.4.5.13.3's three syntax
    /// rules report (screened post-build, once the operand resolves).</summary>
    public CobolNet.Editions.DiagnosticCursor RelativeKeyAt { get; set; }

    /// <summary>The resolved RELATIVE KEY item (an unsigned integer item, ISO §12.4.5.13.3 SR2).</summary>
    public DataItem? RelativeKeyItem { get; set; }

    /// <summary>True once an FD was matched to this SELECT (a SELECT with no FD is an error the front-end already
    /// diagnoses; here it simply has no records and is never opened with data).</summary>
    public bool HasFd { get; set; }

    /// <summary>True for an <c>FD … IS EXTERNAL</c> file (ISO §13.18.22.4 GR4a/GR4b): the file connector is an
    /// EXTERNAL file connector — ONE per run unit, shared by every program describing it — and the record data
    /// is external (one record area per run unit). At emission the connector keys the run-unit registry by
    /// <c>"::EXT::" + ExternalName</c> instead of the per-program <c>PROG::FILE</c> qualification, and the record
    /// area re-bases onto a run-unit <c>ExternalStore</c> cell (the WS-EXTERNAL Tier-B machinery). FILE STATUS
    /// items stay per-program (§13.18.22 is about the connector and record data, not the status item).</summary>
    public bool IsExternal { get; set; }

    /// <summary>True for an <c>FD … IS GLOBAL</c> file (ISO §13.18.30 / §13.18.27): the file-name and its
    /// record-names are GLOBAL names — visible in every directly/indirectly contained program; the contained
    /// program's verbs reach the OWNER's one connector and record storage.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>The externalized name of an EXTERNAL file connector — the FD name (ISO §13.18.22.4 GR5; the
    /// grammar carries no <c>AS literal</c> on the FD clause yet). Null for a non-EXTERNAL file.</summary>
    public string? ExternalName { get; set; }

    /// <summary>For a per-object instance file (M2-OO-1i): the name of the emitted object field holding this
    /// connector's per-object minted key (<c>__fkey_&lt;name&gt;</c>). One connector per object instance
    /// (ISO §9.1.4 — implicit CLOSE at object deletion), so the runtime key is minted per object rather than a
    /// static literal. Null for every program / factory / EXTERNAL file, whose connector key IS the static
    /// qualified <see cref="CobolName"/>; <c>EmitText.FileKeyExpr</c> selects between the two.</summary>
    public string? InstanceKeyField { get; set; }

    /// <summary>True when this file is described by an SD (sort-merge file description, ISO §13.4.6): it has no
    /// host storage — only SORT/MERGE/RELEASE/RETURN may reference it (SR3/SR4); its runtime store is the
    /// in-memory <c>CobolSort</c> buffer.</summary>
    public bool IsSortMerge { get; set; }

    /// <summary>The RECORD clause's variable-length model (ISO §13.18.43 — RECORD IS VARYING / RECORD CONTAINS m
    /// TO n), or null when the records are fixed-length. All the record verbs consume it: WRITE/REWRITE/RELEASE
    /// take the record length from the DEPENDING item (GR13a) or the record's own size (GR13b/c) and fail with
    /// I-O status '44' outside [min,max] (GR14); READ/RETURN restore the just-read record's length (GR15).</summary>
    public VaryingRecordInfo? Varying { get; set; }

    /// <summary>The resolved RECORD VARYING … DEPENDING ON data item (set post-build, like
    /// <see cref="FileStatusItem"/>); null when fixed-length or no DEPENDING phrase.</summary>
    public DataItem? VaryingDependingItem { get; set; }

    /// <summary>The report-names of the FD's REPORT(S) clause (ISO §13.18.46), in written order — a report file
    /// is exactly an FD with a non-empty list (§9.1.22; it legally has NO record description entries). The
    /// names resolve to <see cref="ReportModel"/>s post-build (<c>DataBinder.ResolveReports</c>).</summary>
    public List<string> ReportNames { get; } = [];

    /// <summary>True for a REPORT FILE — an FD whose REPORT clause names at least one report (§9.1.22,
    /// §13.18.46). THE ONE spelling of that test.</summary>
    public bool IsReportFile => ReportNames.Count > 0;

    /// <summary>integer-1 of a Format 1 <c>RECORD CONTAINS integer-1</c> clause (ISO §13.18.43.2) — a count of
    /// BYTES (§13.18.43.4 GR1: "Each integer in a RECORD clause specifies a record size in terms of bytes") — or
    /// null when the clause is absent or variable-length. It SIZES THE FILE: <see cref="RecordWidth"/> reads it
    /// (§13.18.43.4 GR6: "Integer-1 specifies the number of bytes contained in each record in the file"), and a
    /// report file's line width prefers it over the computed field extent (COBOLNET_REPORT_WRITER_DESIGN §4).</summary>
    public int? RecordContains { get; set; }

    /// <summary>How a diagnostic NAMES the entry that describes this file: a sort-merge file description
    /// entry (ISO §13.4.6) or an ordinary file description entry (§13.4.5). ⛔ The two are different GENERAL
    /// FORMATS, so calling an SD an FD sends the reader to the wrong clause — and the RECORD clause is shared
    /// by both, which is exactly where the confusion is reachable. The <see cref="OrganizationFace"/>
    /// pattern: written once, read by every message that needs it.</summary>
    public string EntryFace => IsSortMerge ? "sort-merge file description entry" : "file description entry";

    /// <summary>The source position of the RECORD clause, where §13.18.43.3's syntax rules report — they are
    /// screened post-build (<see cref="RecordClauseRules"/>, run from <c>DataBinder.ResolveFiles</c>), because a
    /// record description's byte count is a §13.18.43.4 GR8 quantity over a forest that is not complete while
    /// the file description entry is being read. Unset when the entry writes no RECORD clause, in which case
    /// there is nothing for these rules to speak about.</summary>
    public CobolNet.Editions.DiagnosticCursor RecordClauseAt { get; set; }

    /// <summary>The SHARING clause mode (ISO §12.4.5.15; <see cref="SharingMode.None"/> = the implementor default).
    /// An OPEN SHARING phrase overrides it per-OPEN (§14.9.27; carried on the bound OPEN).</summary>
    public SharingMode Sharing { get; set; } = SharingMode.None;

    /// <summary>The LOCK MODE clause (ISO §12.4.5.9), or null when absent (locking off — the single-run-unit
    /// default is no record locking).</summary>
    public LockModeInfo? LockMode { get; set; }

    /// <summary>The RESERVE clause's integer-1 (ISO §12.4.5.14), or null when the entry writes no RESERVE clause.
    /// The managed I-O model's buffering is the stream's, so no allocation reads it; its reader is the external
    /// file connector's entry identity, which §12.4.5.3 GR1 d) makes include "The same value for integer-1 in the
    /// RESERVE clause" (<c>OoEmitter.SelectFingerprint</c>, kb/Work PB1079).</summary>
    public int? ReserveAreas { get; set; }

    /// <summary>The RECORD DELIMITER clause's phrase as written — <c>STANDARD-1</c> or the uppercased
    /// feature-name-1 (ISO §12.4.5.11) — or null when the entry writes no such clause. The clause is declined
    /// accept-inert (kb/Work PB292; it changes no framing), so its one reader is the external file connector's
    /// entry identity: §12.4.5.3 GR1 c) requires "Either the STANDARD-1 phrase or a consistent value of
    /// feature-name-1 in the RECORD DELIMITER clause" (<c>OoEmitter.SelectFingerprint</c>, kb/Work PB1079).</summary>
    public string? RecordDelimiter { get; set; }

    /// <summary>Whether an I-O-CONTROL APPLY COMMIT clause (ISO §12.4.6.3) names this file — the STATIC,
    /// source-level property the standard calls "subject to an APPLY COMMIT clause", as distinct from
    /// §12.4.6.3.4 GR5's runtime notion of a clause being <i>active</i>. Set by
    /// <c>DataBinder.BindIoControl</c>, which runs after the file control entries are bound (the clause is in a
    /// different paragraph, so this fact is NOT knowable while a SELECT is being bound).
    /// <para>⛔ IT IS A LIVE FACT, NOT A PLACEHOLDER FOR THE DAY A.4.3 IS CLAIMED. COBOLNET1709 declines the
    /// clause, but its descriptor is <c>PermissiveInert: true</c>, so under <c>--permissive</c> the clause is
    /// accepted with a warning and the program compiles — and a syntax rule that keys on subject-hood
    /// (§14.9.27.3 SR8's leading conjunct, read at <c>StatementValidation.CheckOpenSharingAllOther</c>) then has
    /// a program to speak about. Measured, not deduced: before this field existed, an APPLY COMMIT file with
    /// <c>SHARING WITH ALL OTHER</c> and no LOCK MODE clause was rejected COBOLNET1512 under
    /// <c>--std 2023 --permissive</c>, where SR8 exempts it and §12.4.5.9.3 SR1 forbids writing the very LOCK
    /// MODE clause the rejection demands (kb/Work PB319).</para></summary>
    public bool SubjectToApplyCommit { get; set; }

    /// <summary>The LINAGE clause's logical-page model (ISO §13.18.34), or null when the FD has no LINAGE clause.
    /// Its presence generates the file's LINAGE-COUNTER register (§8.4.3.14 / §13.18.34 GR7a) and enables the
    /// WRITE END-OF-PAGE phrases (§14.9.51 SR19). Data-name operands resolve post-build in
    /// <c>DataBinder.ResolveFiles</c> (the <see cref="VaryingDependingItem"/> pattern).</summary>
    public LinageInfo? Linage { get; set; }

    /// <summary>The variable-length minimum record size in BYTES — integer-2 when the clause states it, else
    /// ISO §13.18.43.4 GR9's "the least number of bytes described for a record in that file" (each record by
    /// <see cref="MinRecordSize"/>, GR8 a); −1 for fixed-length records.
    /// <para>⛔ ONE formula for BOTH arms that reach it — an explicit Format 2/3 clause that omits integer-2, and
    /// the implied Format 2 clause of <see cref="ImpliesVariableFormat"/> (§13.18.43.4 GR5 b) states the implied
    /// integer-2 as the same smallest record). <see cref="VaryMax"/> is its twin.</para></summary>
    public int VaryMin => !RecordSizeVaries ? -1
        : Varying?.Min ?? (Records.Count == 0 ? 1 : Records.Min(MinRecordSize));

    /// <summary>The variable-length maximum record size in BYTES — integer-3 when the clause states it, else
    /// ISO §13.18.43.4 GR10's "the greatest number of bytes described for a record in that file" (each record by
    /// <see cref="MaxRecordSize"/>, GR8 b — a dynamic-length member at its maximum), which is
    /// <see cref="RecordMax"/>; −1 for fixed-length records.
    /// <para>⛔ ONE formula for BOTH arms (kb/Work PB1276). The explicit arm used to read <see cref="RecordWidth"/>
    /// — the CHARACTER AREA's width, which leaves out every out-of-line and dynamic extent — while the implied
    /// Format 2 arm read <see cref="RecordMax"/>, so `RECORD IS VARYING IN SIZE` over a record with a
    /// DYNAMIC LENGTH member refused (status 44) the very record the same program without the clause
    /// wrote.</para></summary>
    public int VaryMax => !RecordSizeVaries ? -1 : Varying?.Max ?? Math.Max(1, RecordMax);

    /// <summary>True when the file's record size varies — an explicit variable-length RECORD clause, or the
    /// implied Format 2 clause of <see cref="ImpliesVariableFormat"/>. THE ONE question every registration asks
    /// before passing <see cref="VaryMin"/>/<see cref="VaryMax"/> to its connector.</summary>
    public bool RecordSizeVaries => Varying is not null || ImpliesVariableFormat;

    /// <summary>⛔ DETERMINATION D-FRA (docs/CONFORMANCE.md §3, kb/Work PB981) — ISO §13.18.43.4 GR5: "If the
    /// RECORD clause is not specified, an implicit format 1 or format 2 RECORD clause is assumed to be
    /// specified. This implicit RECORD clause is defined by the implementor". WiseOwl COBOL implies FORMAT 2 exactly
    /// when a record description is VARIABLE-LENGTH (a dynamic-length elementary record, or a variable-length
    /// group — §8.5.1.12.1): such a record has no one size, and §13.18.43.4 GR13 c) already sizes a record containing a
    /// variable-occurrence item by its extent at the time of the output statement, which is what WRITE sends.
    /// Every other file keeps the implied Format 1 it has always had, so no fixed-record file changes
    /// shape.</summary>
    public bool ImpliesVariableFormat => Varying is null && RecordContains is null && Records.Any(IsVariableLengthRecord);

    /// <summary>The maximum size specified by the record description entries — ISO §14.9.30.4 GR14/GR15's
    /// truncation bound ("the record is truncated on the right to the maximum size"), computed by
    /// <see cref="MaxRecordSize"/>. It equals <see cref="RecordWidth"/> for every file whose records are all
    /// character-window records; it exceeds it only for a variable-length record, whose dynamic members reach
    /// past the character area (D-FRA).</summary>
    public int RecordMax => Records.Count == 0 ? 0 : Math.Max(RecordWidth, Records.Max(MaxRecordSize));

    /// <summary>⛔ THE ONE READER of WHICH §13.18.43.2 GENERAL FORMAT this file description entry's RECORD clause
    /// was written in, and of the integers it states — the projection every §13.18.43.3 syntax rule is stated
    /// over (<see cref="RecordClauseFacts"/>). Null when the entry writes no RECORD clause, which is the one case
    /// those rules have nothing to say about (§13.18.43.4 GR5 then implies one, and an implied clause is derived
    /// from the record descriptions and cannot contradict them).
    /// <para>It is a PROJECTION of <see cref="Varying"/> and <see cref="RecordContains"/>, never a third copy of
    /// the clause: those two already discriminate all three formats between them — Format 1 is
    /// <c>Varying is null</c> with a stated <c>RecordContains</c>, Format 2 is
    /// <c>Varying.VaryingClause</c>, Format 3 is the remaining variable-length shape — and a stored duplicate is
    /// the copy that would drift.</para></summary>
    public RecordClauseFacts? RecordClause => Varying is { } v
        ? new RecordClauseFacts(v.VaryingClause ? RecordClauseFormat.Varying : RecordClauseFormat.FixedOrVariable,
            v.Min, v.Max, RecordClauseAt)
        : RecordContains is { } n ? new RecordClauseFacts(RecordClauseFormat.Fixed, null, n, RecordClauseAt)
        : null;

    /// <summary>The MAXIMUM byte size of one record description (ISO §13.18.43.4 GR8 b): "The maximum number of
    /// table elements described in the record is used in the summation above to determine the maximum number of
    /// bytes associated with the record description" — the twin of <see cref="MinRecordSize"/>, and the quantity
    /// §13.18.43.3 SR3 and SR4's upper arm compare against.
    /// <para>⛔ IT IS <see cref="DataItem.ByteWidth"/>, not a second summation, and this member exists to say so
    /// ONCE: GR8's opening sentence is "the sum of the number of bytes in all elementary data items EXCLUDING
    /// REDEFINITIONS AND RENAMINGS", which is exactly the sum <c>ByteWidth</c> already computes (it skips every
    /// <c>RedefinesTargetName is not null</c> child, and lays a bit-bearing subtree out by the §8.5.1.6.3 walk,
    /// which is GR4's "the entire byte in which that data item ends"), and an occurs-depending table's
    /// <see cref="DataItem.Occurs"/> is its MAXIMUM occurrence count, which is GR8 b)'s own choice.</para>
    /// <para>⛔ IN BYTES, NEVER CHARACTER POSITIONS (kb/Work PB1277): GR3 sizes a record by "the number of bytes
    /// required to store the logical record, regardless of the types of characters used". This read
    /// <see cref="DataItem.ImageWidth"/> — the CARRIER's character positions, which count a national position
    /// once where D-N1 stores two bytes — so a national record measured half its size against the RECORD
    /// integers: legal source was refused and oversize source accepted.</para></summary>
    internal static int MaxRecordSize(DataItem item) =>
        IsVariableLengthRecord(item) ? (int)Math.Min(int.MaxValue, MaxDynamicExtent(item)) : item.ByteWidth;

    /// <summary>GR8 b)'s maximum for a record with variable-length members (kb/Work PB981): a dynamic-length
    /// item contributes its MAXIMUM size (§8.5.1.10.1 — <see cref="DataItem.DynMaxSize"/> characters, two bytes
    /// each when national, D-N1). Every other member is <see cref="DataItem.ByteWidth"/>'s own sum, so this is
    /// that sum with the variable-length members filled in, never a second layout.
    /// <para>⛔ A DYNAMIC-LENGTH ELEMENTARY ITEM IS THE ONLY VARIABLE-LENGTH MEMBER A RECORD CAN HOLD, so this
    /// walk has no dynamic-capacity-table arm (kb/Work PB1604, discharged): a dynamic-capacity table "may be
    /// defined in any place, other than the file section" (§8.5.1.9.1 3)), and COBOLNET1526
    /// (<c>DataBinder.DynamicResolve</c>) refuses one under any FD / SD record — written there directly, or
    /// arriving through a TYPE or SAME AS clause — so no record reaches this walk with one
    /// (<c>OccursDynamicGuardTests.FileSectionDynamicTable_EveryRoute_Rejected1526</c> pins every route). Were
    /// that rule ever relaxed, GR8 b)'s maximum for such a table is the implementor's MAXIMUM capacity
    /// (§8.5.1.9.1, DOC-A.1-60), never its TO phrase, which is only the EXPECTED capacity and "may be exceeded
    /// with a nonfatal exception".</para></summary>
    internal static long MaxDynamicExtent(DataItem item) =>
        item.IsDynamicLength ? (long)item.DynMaxSize * (item.Pic?.Category is PicCategory.National ? 2 : 1)
        : item.IsElementary || !CobolNet.Binding.ReferenceResolver.HasVariableLengthSubordinate(item) ? (long)item.ByteWidth * (item.Occurs ?? 1)
        : item.Children.Where(c => c.RedefinesTargetName is null).Sum(MaxDynamicExtent) * (item.Occurs ?? 1);

    /// <summary>⛔ THE ONE "does this record have a fixed character window?" predicate — D-FRA
    /// (docs/CONFORMANCE.md §3, kb/Work PB981). A record description that is a dynamic-length elementary item,
    /// a variable-length group (§8.5.1.12.1), or an elementary item of a pointer class is an OUT-OF-LINE record:
    /// it does not overlay the file's character record area, so bind time does NOT make it an implicit
    /// redefinition of the first record (§13.18.33.4 GR3 / §12.4.6.4.4 GR2).
    /// <para>The standard permits exactly this placement: "Dynamic-length elementary items may be physically
    /// located in memory within the record they are subordinate to, or they may be located elsewhere in the
    /// computer's memory" (§8.5.1.10.3), a dynamic-length item's internal structure is implementor-defined
    /// (§8.5.1.10.2), and a pointer-class item holds a managed reference with no character image
    /// (CONFORMANCE.md A.1 items 210/216). The ONE area is realized at the transfer boundary, where it is
    /// observable: a READ / RETURN makes the record available in EVERY record of the area
    /// (<see cref="OutOfLineRecords"/>), and a WRITE / REWRITE / RELEASE of an out-of-line record sends its
    /// contiguous image (§8.5.1.11.2).</para></summary>
    public static bool IsOutOfLineRecord(DataItem record) =>
        IsVariableLengthRecord(record) || (record.IsElementary && SlotWindow.CarriedBySlot(record));

    /// <summary>A record description whose size varies at run time — a dynamic-length elementary item or a
    /// variable-length group (§8.5.1.12.1). The subset of <see cref="IsOutOfLineRecord"/> that makes the implied
    /// RECORD clause Format 2 (<see cref="ImpliesVariableFormat"/>).</summary>
    public static bool IsVariableLengthRecord(DataItem record) =>
        record.IsDynamicLength || (record.IsGroup && CobolNet.Binding.ReferenceResolver.HasVariableLengthSubordinate(record));

    /// <summary>The records of this file's area that ride OUT OF LINE (<see cref="IsOutOfLineRecord"/>) and are
    /// not <see cref="AreaRecord"/> itself, together with those of every file that shares the area through a
    /// record-area SAME clause (§12.4.6.4.4 GR2 — "two or more files ... are to share a memory area for
    /// processing the current logical record"). A READ / RETURN on this file makes the current record available
    /// in each of them — the out-of-line half of the one-area rule; the character half is the shared
    /// backing.</summary>
    public IEnumerable<DataItem> OutOfLineRecords =>
        new[] { this }.Concat(SameRecordAreaPeers).SelectMany(f => f.Records)
            .Where(r => IsOutOfLineRecord(r) && !ReferenceEquals(r, AreaRecord)).Distinct();

    /// <summary>The OTHER files named with this one in a record-area SAME clause (§12.4.6.4.4 GR2), set by
    /// <c>DataBinder</c> when it links their records into one area.</summary>
    public List<FileModel> SameRecordAreaPeers { get; } = [];

    /// <summary>The first record of this file that has a character window — the anchor every other
    /// character-window record implicitly redefines (§13.18.33.4 GR3), or null when every record is out of
    /// line.</summary>
    public DataItem? CharacterAnchor => Records.FirstOrDefault(r => !IsOutOfLineRecord(r));

    /// <summary>The MINIMUM byte size of one record description (ISO §13.18.43.4 GR8 a): "The minimum number of
    /// table elements described in the record is used in the summation above to determine the minimum number of
    /// bytes associated with the record description" — the twin of <see cref="MaxRecordSize"/> (a bare
    /// <c>RECORD IS VARYING</c> over an ODO record — RL211A — has minimum 120, not its 140-byte maximum).
    /// <para>⛔ It is <see cref="DataItem.ByteWidth"/> with the varying tables at their minimum, NOT a second
    /// re-summation of leaves (kb/Work PB1277): wherever no table varies beneath an item, the item's minimum IS
    /// its <c>ByteWidth</c> — BYTES (GR3, so a national position counts two, D-N1) laid out by the one
    /// §8.5.1.6.3 walk (GR4, so two 3-bit items share a byte). The re-summation this replaced counted each leaf
    /// in character positions and rounded each bit leaf to a byte of its own, so a record of two
    /// <c>PIC 1(3) USAGE BIT</c> items had minimum 2 and maximum 1 and no WRITE of it could succeed.</para></summary>
    internal static int MinRecordSize(DataItem item) => (int)Math.Min(int.MaxValue, MinExtent(item));

    /// <summary>GR8 a)'s summation, per occurrence of <paramref name="item"/> (a parent multiplies by the item's
    /// own <see cref="DataItem.MinimumOccurrences"/>). Three arms:
    /// <list type="bullet">
    ///   <item>no table varies beneath the item — its <see cref="DataItem.ByteWidth"/>, the fixed GR8 sum;</item>
    ///   <item>a bit-bearing subtree over an occurs-depending table — the §8.5.1.6.3 placement of that table (the
    ///         record's unique trailing variable part, §13.18.38.3 SR22) plus its minimum run, rounded up to the
    ///         byte it ends in (GR4) — counted in BITS for the reason <see cref="OdoModel.WrapGroup"/> gives: a
    ///         character sum of shared-byte bit runs over-counts;</item>
    ///   <item>otherwise the sum over the non-redefining children, each at its minimum occurrence count.</item>
    /// </list></summary>
    private static long MinExtent(DataItem item) =>
        item.IsElementary || !HasVaryingTableBeneath(item) ? item.ByteWidth
        : item.HasBitDescendant && OdoModel.TableUnder(item) is { OccursSpec: { } os } table
                && BitLayout.StartBitOf(item, table) is >= 0 and var start
            ? BitLayout.Characters(start
                + (os.Min == 0 ? 0 : BitLayout.StrideBits(table) * (os.Min - 1) + BitLayout.WidthBits(table)))
        : item.Children.Where(c => c.RedefinesTargetName is null).Sum(c => MinExtent(c) * c.MinimumOccurrences);

    /// <summary>Whether an occurs-depending table — the one table in a record whose occurrence count varies —
    /// lies strictly beneath <paramref name="item"/> (a redefinition contributes nothing to GR8's sum, so one
    /// beneath a redefining entry does not count). A dynamic-capacity table cannot be in a record: see
    /// <see cref="MaxDynamicExtent"/> (§8.5.1.9.1 3), COBOLNET1526).</summary>
    private static bool HasVaryingTableBeneath(DataItem item) =>
        item.Children.Any(c => c.RedefinesTargetName is null
            && (c.OccursSpec is { DependingName: not null } || HasVaryingTableBeneath(c)));

    /// <summary>⛔ THE RECORD AREA'S WIDTH IN BYTES — what every connector registration, the SORT/MERGE record
    /// length and the fixed-file attributes read (ISO §13.18.43.4 GR2: "The implicit or explicit RECORD clause
    /// specifies the size of records in the record area"). Two sources, one answer:
    /// <list type="bullet">
    ///   <item>an explicit Format 1 clause — <see cref="RecordContains"/>: §13.18.43.4 GR6, "Integer-1 specifies
    ///         the number of bytes contained in each record in the file". §13.18.43.3 SR3 lets every record
    ///         description be SMALLER than integer-1, so the descriptions do not size such a file (kb/Work
    ///         PB1276 — two 10-byte WRITEs under <c>RECORD CONTAINS 20</c> produced one 20-byte record);</item>
    ///   <item>otherwise the descriptions' PHYSICAL (codec) width — the max over the records of the extent the
    ///         emitted <c>AsImage()</c>/<c>FromImage()</c> spans, which is §13.18.43.4 GR5 a)'s implied
    ///         "record size of the largest record description entry" (P5.8: <c>ImageWidth</c> under-counted a
    ///         record whose REDEFINES redefiner is wider than its target — the codec spans the class-max
    ///         backing, §13.18.44).</item>
    /// </list>
    /// The maximum of the two is taken so a program SR3 has already refused (a description larger than
    /// integer-1) still sizes an area that holds its records. A variable-length file's bounds are
    /// <see cref="VaryMin"/>/<see cref="VaryMax"/>; this stays its character area.</summary>
    public int RecordWidth => Math.Max(RecordContains ?? 0,
        Records.Count == 0 ? 0 : Records.Max(Model.RecordLayout.PhysicalWidth));

    /// <summary>The record description whose view spans the WHOLE record area — the largest one (ISO §13.18.33.4
    /// GR3: level-1 entries under an FD are "implicit redefinitions of the same area"; §13.18.43.4 GR5 a) sizes it
    /// as "the record size of the largest record description entry"). Reading a record makes it available in the
    /// whole area, so every area-wide store/read (sequential and keyed READ, sort RETURN) must go through THIS
    /// record's view — a shorter <c>Records[0]</c> window would truncate the splice (ST111A's 50/75/100 FD,
    /// RL106A's 56/102 pair). Null when the FD has no record description.</summary>
    /// <para>⛔ AN OUT-OF-LINE RECORD (<see cref="IsOutOfLineRecord"/>) IS NEVER CHOSEN WHILE A CHARACTER-WINDOW
    /// RECORD EXISTS (kb/Work PB981): it has no window over the character area, and its image width is not the
    /// area's. When EVERY record is out of line the first one is the area — the file's only storage.</para>
    /// <para>⛔ "Largest" is measured in BYTES — <see cref="Model.RecordLayout.PhysicalWidth"/>, the unit
    /// <see cref="RecordWidth"/> sizes the area in (§13.18.43.4 GR3; kb/Work PB1277's sibling). Measured in the
    /// carrier's character positions (<c>ImageWidth</c>) a <c>PIC N(10)</c> record (20 bytes, D-N1) lost to a
    /// <c>PIC X(15)</c> one, and a READ spliced the 20-byte record through the 15-byte view, so the national
    /// record received seven and a half characters.</para>
    public DataItem? AreaRecord => Records.Count == 0 ? null
        : Records.Where(r => !IsOutOfLineRecord(r)).MaxBy(Model.RecordLayout.PhysicalWidth) ?? Records[0];

    /// <summary>True for either sequential shape (the only organizations this slice can OPEN/READ/WRITE).</summary>
    public bool IsSequential => Organization is FileOrganization.Sequential or FileOrganization.LineSequential;
}

/// <summary>The variable-length record model of a RECORD clause (ISO §13.18.43): the declared minimum/maximum
/// record sizes (null when unstated) and the <c>VARYING … DEPENDING ON</c> data-name (null when none — a
/// <c>RECORD CONTAINS m TO n</c> file varies without a length register).</summary>
/// <param name="VaryingClause">True for FORMAT 2 (<c>RECORD IS VARYING IN SIZE …</c>), false for FORMAT 3
/// (<c>RECORD CONTAINS integer-4 TO integer-5</c>). ⛔ THE TWO FORMATS ARE NOT INTERCHANGEABLE even though both
/// describe variable-length records: §13.18.43.4 GR16 (the READ/RETURN INTO sending size) is stated under the
/// FORMAT 2 heading, and §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) designate the implied move an alphanumeric group
/// move only "If the file description entry contains a RECORD IS VARYING clause" — a format-3 file's record size
/// is instead "completely defined in the record description entry" (GR18) and its move is classified normally.
/// The two formats were indistinguishable here until kb/Work PB339, which is why a rule keyed on the WORD
/// VARYING had nothing to key on.</param>
public sealed record VaryingRecordInfo(int? Min, int? Max, string? DependingName, bool VaryingClause)
{
    /// <summary>The DEPENDING ON data-name's IN/OF qualifier words, innermost first (ISO §8.4.2.2) — see
    /// <see cref="FileModel.FileStatusQualifiers"/> for why a clause operand keeps them (kb/Work PB489).</summary>
    public IReadOnlyList<string> DependingQualifiers { get; set; } = [];

    /// <summary>The source position of the DEPENDING ON data-name — where its post-build resolution reports.
    /// The RECORD clause is a FILE DESCRIPTION entry clause, so the file control entry's cursor would point at
    /// a different line of the program.</summary>
    public CobolNet.Editions.DiagnosticCursor DependingAt { get; set; }
}

/// <summary>Which of the three general formats of ISO §13.18.43.2 a RECORD clause was written in. The axis
/// §13.18.43.3's syntax rules are cut on: SR3 is stated under FORMAT 1, SR4/SR5/SR6/SR7 under FORMAT 2, SR8/SR9
/// under FORMAT 3, and SR1/SR2 under ALL FORMATS.</summary>
public enum RecordClauseFormat
{
    /// <summary>Format 1 (fixed-length): <c>RECORD CONTAINS integer-1 {BYTES|CHARACTERS}</c>.</summary>
    Fixed = 1,

    /// <summary>Format 2 (variable-length): <c>RECORD IS VARYING IN SIZE [FROM integer-2] [TO integer-3]
    /// {BYTES|CHARACTERS} [DEPENDING ON data-name-1]</c>.</summary>
    Varying = 2,

    /// <summary>Format 3 (fixed-or-variable-length): <c>RECORD CONTAINS integer-4 TO integer-5
    /// {BYTES|CHARACTERS}</c>.</summary>
    FixedOrVariable = 3,
}

/// <summary>One RECORD clause AS WRITTEN, reduced to what §13.18.43.3's syntax rules speak about: the general
/// format, its lower and upper integer operands (null where the format has none or the program omitted it), and
/// where to report. Produced by <see cref="FileModel.RecordClause"/>; consumed by <c>RecordClauseRules</c>.</summary>
/// <param name="Format">The §13.18.43.2 general format the clause was written in.</param>
/// <param name="Lower">integer-2 (Format 2) or integer-4 (Format 3); null for Format 1, which has no lower
/// operand, and for a Format-2 clause written without the FROM phrase (§13.18.43.4 GR9 then supplies the
/// minimum from the record descriptions, so it cannot contradict them).</param>
/// <param name="Upper">integer-1 (Format 1), integer-3 (Format 2) or integer-5 (Format 3); null for a Format-2
/// clause written without the TO phrase (GR10 supplies it the same way).</param>
/// <param name="At">The clause's source position.</param>
public readonly record struct RecordClauseFacts(
    RecordClauseFormat Format, int? Lower, int? Upper, CobolNet.Editions.DiagnosticCursor At)
{
    /// <summary>The printed name of the lower operand in this clause's own general format — so a diagnostic
    /// names the operand the program's author actually wrote rather than a generic "minimum".</summary>
    public string LowerName => Format switch
    {
        RecordClauseFormat.Varying => "integer-2",
        RecordClauseFormat.FixedOrVariable => "integer-4",
        _ => "integer-1",
    };

    /// <summary>The printed name of the upper operand in this clause's own general format.</summary>
    public string UpperName => Format switch
    {
        RecordClauseFormat.Varying => "integer-3",
        RecordClauseFormat.FixedOrVariable => "integer-5",
        _ => "integer-1",
    };

    /// <summary>The clause as the standard heads its general format, for a diagnostic's first clause.</summary>
    public string FormatFace => Format switch
    {
        RecordClauseFormat.Varying => "RECORD IS VARYING IN SIZE (ISO §13.18.43.2 Format 2)",
        RecordClauseFormat.FixedOrVariable => "RECORD CONTAINS integer-4 TO integer-5 (ISO §13.18.43.2 Format 3)",
        _ => "RECORD CONTAINS integer-1 (ISO §13.18.43.2 Format 1)",
    };
}

/// <summary>One LINAGE clause operand (ISO §13.18.34 GR6): a fixed literal value (GR6a) or a data-name whose
/// content is read at the GR6b evaluation points (OPEN OUTPUT / WRITE ADVANCING PAGE / page overflow), resolved
/// post-build to <see cref="Item"/> (SR1/SR2 — an elementary unsigned integer not under OCCURS, so a plain
/// name lookup suffices).</summary>
public sealed record LinageOperand(int? Literal, string? DataName)
{
    /// <summary>The resolved data item for the <see cref="DataName"/> form (set post-build); null for a literal.</summary>
    public DataItem? Item { get; set; }

    /// <summary>The operand's IN/OF qualifier words, innermost first (ISO §8.4.2.2). §13.18.34.2 prints
    /// <i>data-name-1</i>, which is a qualified-data-name (§8.4.2.2.2 Format 1), so <c>LINAGE IS SZ OF GRP-B
    /// LINES</c> is a legal — and, with two SZs, the ONLY legal — spelling. The binder used to keep the base word
    /// alone and resolve it by first match, so that clause silently built the whole logical page on GRP-A's SZ
    /// (kb/Work PB489, measured: EOP / LINAGE-COUNTER 1 where the standard requires NO-EOP / 5).</summary>
    public IReadOnlyList<string> Qualifiers { get; set; } = [];

    /// <summary>The source position of this operand — where §13.18.34.3's syntax rules and the operand's own
    /// resolution report (both run post-build, once the data forest is indexed).</summary>
    public CobolNet.Editions.DiagnosticCursor At { get; set; }
}

/// <summary>The LINAGE clause's four operands (ISO §13.18.34): the page-body size (GR2), the footing start
/// (GR3 — the footing area is [footing, page size] inclusive), and the top/bottom margins (GR4/GR5). A null
/// <see cref="Footing"/>/<see cref="Top"/>/<see cref="Bottom"/> = the phrase is absent (margins 0, GR1; no
/// footing ⇒ no end-of-page condition independent of page overflow, GR1).</summary>
public sealed record LinageInfo(LinageOperand Body, LinageOperand? Footing, LinageOperand? Top, LinageOperand? Bottom)
{
    /// <summary>The non-null operands, for uniform post-build resolution.</summary>
    public IEnumerable<LinageOperand> Operands
    {
        get
        {
            yield return Body;
            if (Footing is not null) yield return Footing;
            if (Top is not null) yield return Top;
            if (Bottom is not null) yield return Bottom;
        }
    }
}
