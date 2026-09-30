// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;

namespace CobolNet.Runtime.IO;

/// <summary>
/// A physical file's §9.1.6 FIXED FILE ATTRIBUTES as a value, and the §14.9.27.4 GR10 comparison over them.
/// <para>
/// ⛔ THERE IS NO CATALOG AND NO SIDECAR — owner decision 2026-09-07 (kb/Work PB802), verbatim: <i>"Let's match
/// GNUCobol's implementation in spirit. No sidecar of any type."</i> WiseOwl COBOL writes and reads NOTHING beside a
/// data file (NTFS alternate data streams were considered and rejected with the `.cbattr` sidecar they would
/// have replaced). A physical file's fixed file attributes are whatever ITS OWN BYTES record, so this type is a
/// VALUE and a COMPARISON — never a store. The bytes that carry it for a RELATIVE or INDEXED file are
/// <see cref="RecordFraming"/>'s store header, because the header IS the format.
/// </para>
/// <para>
/// ⛔ THE VALIDATED SET IS EXACTLY WHAT EACH ORGANIZATION'S FORMAT ENCODES, and that one rule is realized
/// STRUCTURALLY rather than as a table: §14.9.27.4 GR10 says "The implementor defines which of the fixed-file
/// attributes are validated during the execution of the OPEN statement. The validation of fixed-file attributes
/// may vary depending on the organization or storage medium of the file", and the variation lives in the three
/// overrides of <c>FileConnector.FixedAttributeConflict</c> — one per organization, each in the file where that
/// organization's format lives. A fourth organization brings its own answer with it instead of needing a row in
/// a switch here. The determination is documented in <c>docs/CONFORMANCE.md</c> §7 row <c>DOC-A.1-129</c> and
/// designed in <c>docs/COBOLNET_FILES_DESIGN.md</c> D10.
/// </para>
/// </summary>
/// <param name="Organization">The §9.1.6 primary attribute, and ONLY that: §9.1.6 names exactly three
/// organizations — "There are three organizations: sequential, relative, and indexed" — so this field carries
/// exactly three values (<see cref="Sequential"/> · <see cref="Relative"/> · <see cref="Indexed"/>). §9.1.7.2's
/// record-sequential/line-sequential distinction is NOT a fourth organization; it is §9.1.6's separately listed
/// <i>record delimiter</i>, which no sequential format records.</param>
/// <param name="Varying">The §9.1.6 "record type (fixed or variable)": true = RECORD IS VARYING.</param>
/// <param name="MinRecordSize">The §9.1.6 "minimum ... logical record size in bytes".</param>
/// <param name="MaxRecordSize">The §9.1.6 "maximum logical record size in bytes".</param>
/// <param name="Keys">The §9.1.6 "prime record key, alternate record keys, SUPPRESS WHEN attribute ... the
/// collating sequence of the keys for indexed files": index 0 is the prime key, 1.. the alternates in
/// declaration order. Empty for a non-indexed file.</param>
public sealed record FixedFileAttributes(
    string Organization,
    bool Varying,
    int MinRecordSize,
    int MaxRecordSize,
    IReadOnlyList<FixedFileAttributes.KeyDescriptor> Keys)
{
    /// <summary>One record key's fixed attributes (§12.4.5.12 / §12.4.5.6 / §12.4.5.7): the byte window into the
    /// record image, the WITH DUPLICATES phrase, the §12.4.5.6.4 GR6 SUPPRESS WHEN value (null = no phrase) and
    /// the key's collating sequence, identified by <see cref="Fingerprint"/>.</summary>
    public readonly record struct KeyDescriptor(int Offset, int Length, bool Duplicates, string? Suppress, string Collation);

    /// <summary>§9.1.6's primary attribute, sequential — "There are three organizations: sequential, relative,
    /// and indexed". Both §9.1.7.2 types of sequential file (record sequential and line sequential) are THIS
    /// organization; the delimiter that separates them is §9.1.6's own <i>record delimiter</i> attribute, which
    /// no sequential format records and which is therefore never compared.</summary>
    public const string Sequential = "SEQUENTIAL";

    /// <summary>§9.1.6's primary attribute, relative (§9.1.7.3).</summary>
    public const string Relative = "RELATIVE";

    /// <summary>§9.1.6's primary attribute, indexed (§9.1.7.4).</summary>
    public const string Indexed = "INDEXED";

    // ── The §14.9.27.4 GR10 comparison ───────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE ONE PLACE §14.9.27.4 GR10's COMPARISON IS WRITTEN: true when this RECORDED set (read back
    /// from the physical file's own format) and <paramref name="declared"/> (the connector's, from the file
    /// control paragraph and the file description entry) do not match, which GR10 makes a file attribute
    /// conflict condition — the OPEN is unsuccessful with I-O status '39' (§9.1.13.6 item 7: "The OPEN or DELETE
    /// FILE statement is unsuccessful because a conflict has been detected between the fixed file attributes and
    /// the attributes specified for that file in the source unit").
    /// <para>WHAT IS COMPARED IS WHATEVER THE CALLER READ. Only <c>KeyedConnector</c> calls this, because only
    /// the RELATIVE and INDEXED formats record a set to compare (<see cref="RecordFraming"/>'s store header);
    /// the sequential formats record nothing, or — for a RECORD VARYING record-sequential file — only whether
    /// their record-length framing parses, which is <c>SequentialConnector</c>'s own answer and needs no value.
    /// This method therefore holds no per-organization branch: the organization IS the caller.</para>
    /// <para>The twin of this method for §14.9.10.4 GR19 (DELETE FILE) is
    /// <c>FileRegistry.ValidateFixedFileAttributes</c>, whose set is EMPTY and deliberately so — Annex A.1 makes
    /// the two required determinations separately (items 50 and 129) and they are not the same answer: a DELETE
    /// FILE destroys the file, so nothing downstream depends on the description having been right.</para></summary>
    /// <param name="declared">The connector's declared attributes (<c>FileConnector.DeclaredAttributes</c>).</param>
    /// <param name="validateKeys">False when the run unit set <c>COBOLNET_KEYCHECK</c> off
    /// (<see cref="FileRegistry.KeyCheckVariable"/>) — the documented runtime opt-out for the INDEXED key check,
    /// GnuCOBOL's <c>COB_KEYCHECK=OFF</c> under this repository's naming. It removes the key table from the
    /// validated set for that run and leaves the organization, the record type and the record sizes in it; that
    /// narrowing is itself part of the A.1 item 129 determination, under GR10's "the implementor defines which
    /// of the fixed-file attributes are validated".</param>
    public bool Conflicts(FixedFileAttributes declared, bool validateKeys = true)
    {
        // §9.1.6's primary attribute. A relative store opened through an indexed description (or the reverse)
        // is not a record-length disagreement but a different physical structure.
        if (!string.Equals(Organization, declared.Organization, StringComparison.Ordinal)) return true;
        // §9.1.6's "record type (fixed or variable)" and "minimum ... and maximum logical record size in bytes"
        // — the framed store's slot layout IS those, so a description that disagrees cannot interpret it.
        if (Varying != declared.Varying) return true;
        if (MinRecordSize != declared.MinRecordSize || MaxRecordSize != declared.MaxRecordSize) return true;
        if (!validateKeys) return false;
        // §12.4.5.6.4 GR3's own second sentence makes the COUNT a requirement in its own right ("The number of
        // alternate record keys for the file shall also be the same as that used when the physical file was
        // created"), so it is compared before the descriptors rather than falling out of them.
        if (Keys.Count != declared.Keys.Count) return true;
        for (int i = 0; i < Keys.Count; i++)
            if (Keys[i] != declared.Keys[i]) return true;
        return false;
    }

    // ── The collating-sequence identity ──────────────────────────────────────────────────────────────────────

    /// <summary>The identity of a key's collating sequence (§9.1.6 "the collating sequence of the keys for
    /// indexed files"), as a short stable fingerprint of the WEIGHTS the sequence assigns — not of the
    /// alphabet-name that produced them. Two sequences that order every key value identically ARE the same
    /// fixed attribute however they were spelled, and two spellings of one name that weigh differently (a
    /// LOCALE sequence under a different locale) are NOT — which is exactly the property GR10 needs, since what
    /// the physical file's index order depends on is the weights.
    /// <para>A null sequence is the native ordinal one (§12.4.5.3 GR6 — no applicable COLLATING SEQUENCE
    /// clause), reported as <c>NATIVE</c>. A sequence that cannot be probed answers <c>UNKNOWN</c>, which
    /// compares equal to itself and so can never manufacture a '39'.</para></summary>
    public static string Fingerprint(CobolCollation? collation)
    {
        if (collation is null) return "NATIVE";
        try
        {
            // FNV-1a over the sequence's position count and the weight of EVERY native character — the 65,536
            // UTF-16 code units a key compares over in memory (D-N1; kb/Work PB1093, R52). A walk of only the
            // Latin-1 block let two sequences that differ above U+00FF (where the alphanumeric HIGH-VALUE U+FFFF
            // and every wide character sit) share a fingerprint, so a file created under one opened '00' under
            // the other.
            ulong h = 14695981039346656037UL;
            void Mix(long v)
            {
                for (int b = 0; b < 8; b++) { h ^= (byte)(v >> (b * 8)); h *= 1099511628211UL; }
            }
            Mix(collation.PositionCount);
            for (int c = 0; c <= char.MaxValue; c++) Mix(collation.Weight((char)c));
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }
        catch (Exception)   // a sequence that refuses to be probed is not evidence of a conflict
        {
            return "UNKNOWN";
        }
    }
}
