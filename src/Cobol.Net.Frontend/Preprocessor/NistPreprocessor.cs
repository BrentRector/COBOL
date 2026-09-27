// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// Preprocessor for NIST COBOL-85 test suite programs.
/// Replaces XXXXX### site-specific placeholders with CobolSharp-appropriate values.
///
/// NIST X-card convention (from CCVS85 executive routine):
///   XXXXX001     — Sequential file ASSIGN target (POPULATION-FILE)
///   XXXXX002     — Sequential file ASSIGN target (SOURCE-COBOL-PROGRAMS)
///   XXXXX051     — SPECIAL-NAMES implementor switch name 1
///   XXXXX052     — SPECIAL-NAMES implementor switch name 2
///   XXXXX053     — Implementor's own I-O-CONTROL entry (the obsolete RERUN clause) — DELETED (kb/Work PB829)
///   XXXXX055     — System printer file name (ASSIGN TO target for PRINT-FILE)
///   XXXXX056     — SPECIAL-NAMES display output device implementor-name
///   XXXXX057     — SPECIAL-NAMES accept input device implementor-name
///   XXXXX073     — SPECIAL-NAMES WRITE ADVANCING feature-name (→ CSP)
///   XXXXX058     — Control card file ASSIGN target
///   XXXXX068     — OBJECT-COMPUTER MEMORY SIZE value (obsolete clause)
///   XXXXX069     — Implementor's own ADDITIONAL FILE DESCRIPTION clause — DELETED (kb/Work PB829)
///   XXXXX081     — Non-COBOL characters string value
///   XXXXX082     — SOURCE-COMPUTER name
///   XXXXX083     — OBJECT-COMPUTER name
///   XXXXX084     — Implementor-specific label clause value (LABEL RECORDS)
///   XXXXX086-088 — Implementor's own DATA DESCRIPTION of a file/label identifier item (kb/Work PB487)
///   XXXXX090     — CLASS definition: single character value ("A")
///   XXXXX091     — CLASS definition: character range end value ("D")
/// </summary>
public static class NistPreprocessor
{
    /// <summary>
    /// Per-test allow-list of (NIST test name, X-card number) pairs whose SELECT OPTIONAL
    /// RELATIVE/INDEXED file's permanent <c>XXXXX###</c> ASSIGN target must STILL be mapped to the
    /// shared <c>"TF###"</c> physical file — i.e. the OPTIONAL file is read from a file a SEPARATE
    /// producer program wrote. By default an OPTIONAL RELATIVE/INDEXED target is left implementor-name-
    /// qualified for per-program absent-file isolation (IX216A/217A/218A, SQ202A/203A); this list is the
    /// narrow set of cross-program consumers that legitimately need the producer's shared file instead.
    ///   • ("RL213A","021") — RL212A writes 500 records to RL-FS1 (non-OPTIONAL XXXXP021 → "TF021");
    ///     RL213A SELECT OPTIONAL RL-FS1 ASSIGN TO XXXXX021 OPENs EXTEND, appends 501–520, re-reads 520.
    /// </summary>
    private static readonly System.Collections.Generic.HashSet<(string Test, string XCard)> OptionalSharedAssign =
        new() { ("RL213A", "021") };

    /// <summary>
    /// Replace NIST XXXXX placeholders in COBOL source text.
    /// </summary>
    /// <param name="source">Raw or normalized COBOL source text.</param>
    /// <param name="testName">NIST test program name (e.g., "NC101A"). Used to derive
    /// the output file name so each test writes to its own file.</param>
    /// <returns>Source text with placeholders replaced.</returns>
    public static string Process(string source, string testName)
    {
        // ── File assignments ──

        // XXXXX055: printer assignment → string literal with test name
        source = source.Replace("XXXXX055", $"\"{testName}\"");

        // XXXXX001 / XXXXX002 are data-file ASSIGN targets. They are NOT special-cased to a shared literal
        // here — they flow through the organization-aware mapping below (line 72) like every other
        // XXXXX### data file: a RELATIVE/INDEXED file maps to a shared "TF###" (so producer/consumer run
        // units share one physical file), while a SEQUENTIAL file's target is left as the implementor-name
        // so the Binder qualifies it per program-id for test isolation (DEVLOG 244/255). A blanket
        // `XXXXX001 → "TFIL1"` literal here previously defeated that isolation, so an OPEN-INPUT-on-an-
        // absent-file test (SQ141A/SQ142A) saw a leftover shared file and got status 00 instead of 35.

        // XXXXX058: control card file assignment
        source = source.Replace("XXXXX058", "\"CONTROL\"");

        // XXXXX065: "4-digit integer for the NUMBER OF RECORDS" the ST115A/ST117A SORT chain builds into its
        // input file (used as a PROCEDURE-division loop bound `… GREATER THAN XXXXX065` and divided by 51 to
        // get NUMBER-OF-SETS — ST117A `DIVIDE XXXXX065 BY 51`). Left unsubstituted it is a bogus loop bound
        // and the file-build loop never terminates. Must be a multiple of 51 (51 records per set); 204 = 4
        // sets keeps the test meaningful and fast. Match only the STANDALONE token (alphanumeric boundaries
        // both sides) — IX106A embeds the 5-char string "XXXXX065" inside a longer test-data literal
        // ("…XXXXXXXXXX065ALTKEY1…", i.e. preceded by 'X' and followed by 'A'); a plain .Replace would
        // corrupt that baselined literal, so the lookbehind/lookahead excludes it.
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX065(?![A-Za-z0-9])", "204");

        // XXXXX063: the implementor's native collating sequence as a 51-character nonnumeric literal — the
        // 51 distinct key characters the ST sort/merge tests use, listed in ascending native-collating order.
        // CobolSharp's native sequence is ASCII, so this is those characters sorted by ASCII code: a leading
        // space, then "$$", then "()*+,-./", then "0-9", then ";<=>", then "A-Z" (51 chars total). ST137A
        // redefines it as ASCIIS — the expected post-sort key order it compares the sorted file against;
        // ST147A uses it for both its key source and its expected order (a MERGE of already-collated inputs).
        // Left unsubstituted the expected-order item is blank and every collation check has nothing to compare
        // against (ST137A's SRT-TEST-003..005 fail). Token-boundary anchored so it never matches the embedded
        // "…XXXXXXXX063A…" inside IX106A's baselined test-data literal. A MatchEvaluator emits the literal
        // (its '$' characters are passed through verbatim, not interpreted as a replacement template).
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX063(?![A-Za-z0-9])",
            _ => "\" $$()*+,-./0123456789;<=>ABCDEFGHIJKLMNOPQRSTUVWXYZ\"");

        // XXXXX064: the implementor's native collating sequence in DESCENDING order as a 51-character
        // nonnumeric literal — the exact character mirror of XXXXX063 (ST144A's own source comments carry this
        // ASCII sample). ST144A merges pre-collated inputs and checks the merged key order against it; left
        // unsubstituted the expected-order items held the raw token text (the pre-substitution golden encoded
        // the legacy's blank placeholder — ST144A re-baselined with this change, the ST137A/ST147A precedent,
        // DEVLOG ~293). Token-boundary anchored + MatchEvaluator exactly as for 063 (IX106A embeds "…064…"
        // inside a baselined test-data literal; '$' must not act as a replacement template).
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX064(?![A-Za-z0-9])",
            _ => "\"ZYXWVUTSRQPONMLKJIHGFEDCBA>=<;9876543210/.-,+*)($$ \"");

        // Data-file ASSIGN targets shared across run units by X-card number, mapped (per SELECT…period
        // entry, after COPY expansion) to one "TF###" literal so a producer's output is read by the matching
        // consumer in a shared directory:
        //   • XXXXD### (consume) — ALWAYS shared. It deliberately reads a file another program produced
        //     (XXXXP002 written by SM203A ↔ XXXXD002 read by SM204A; ST104A's XXXXP001 ↔ ST105A's XXXXD001),
        //     including an OPTIONAL consumer that expects the file present (SQ203A's "FILE PRESENT" test).
        //   • XXXXP### (produce) and the RELATIVE/INDEXED XXXXX### (permanent) variant — shared too, but
        //     ONLY for a non-OPTIONAL SELECT (e.g. RL108A creates XXXXX061, RL109A/RL110A consume it).
        // A SEQUENTIAL XXXXX### is left as an implementor-name so the Binder qualifies it per program-id for
        // absent-file isolation (DEVLOG 244 — SQ130A's XXXXX014/062). A SELECT OPTIONAL file tests
        // presence/absence PER PROGRAM, so its own/produce target (XXXXP###, or a RELATIVE/INDEXED XXXXX###)
        // is likewise left implementor-name-qualified — otherwise another run unit's leftover TF### makes an
        // "absent optional" file appear present (IX216A/217A/218A: OPEN INPUT/EXTEND of an absent optional
        // indexed file must give 05 / READ → AT END 10, not 00 from a producer's shared TF###).
        // Region pattern: a SELECT entry runs from the SELECT keyword to its terminating period. Two
        // hazards drive the exact shape of this regex, both rooted in NormalizeToFreeForm rewriting
        // fixed-form indicator-column lines into free-form "*> …" comment lines that survive in this text:
        //   (1) It must NOT stop at the first literal '.', because CCVS routinely interposes "*> …" comment
        //       lines (e.g. SM203A/SM204A's "…DURING EXTRACTION." note) BETWEEN `SELECT TEST-FILE ASSIGN TO`
        //       and the `XXXXD002.` operand. A naive `[\s\S]*?\.` stops at the comment's period, leaving the
        //       operand (which sits AFTER the comments) unmapped → producer and consumer then qualify TF002
        //       per-program and stop sharing a file (SM204A read an empty file). So the alternation consumes
        //       whole "*> …" comment lines (periods and all), terminating only at the entry's REAL period.
        //   (2) But (1) makes the body skip comment periods, so it MUST be anchored to a real-code SELECT —
        //       `(?m)^[ \t]*SELECT` — never a "SELECT" sitting INSIDE a "*> …" comment. The file-I/O suites
        //       comment out an optional scratch-file SELECT (indicator 'P', e.g. SQ130A/141A/142A's INDEXED
        //       RAW-DATA on X-card 62). Matching that commented "SELECT" would let the comment-skipping body
        //       run past the whole comment block into real code (no real period until then), and because the
        //       comment says INDEXED it would wrongly map the following SEQUENTIAL XXXXX001/014 — destroying
        //       the per-program isolation those absent-file status tests depend on.
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?m)^[ \t]*SELECT\b(?:\*>[^\n]*\n|[^.])*\.",
            m =>
            {
                string sel = System.Text.RegularExpressions.Regex.Replace(
                    m.Value, @"XXXXD(\d+)", "\"TF$1\"");
                bool optional = System.Text.RegularExpressions.Regex.IsMatch(sel, @"\bSELECT\s+OPTIONAL\b");
                if (!optional)
                {
                    sel = System.Text.RegularExpressions.Regex.Replace(sel, @"XXXXP(\d+)", "\"TF$1\"");
                    if (System.Text.RegularExpressions.Regex.IsMatch(sel, @"\b(RELATIVE|INDEXED)\b"))
                        sel = System.Text.RegularExpressions.Regex.Replace(
                            sel, @"(ASSIGN\s+(?:TO\s+)?)XXXXX(\d+)", "$1\"TF$2\"");
                }
                else if (System.Text.RegularExpressions.Regex.IsMatch(sel, @"\b(RELATIVE|INDEXED)\b"))
                {
                    // A SELECT OPTIONAL RELATIVE/INDEXED file normally keeps its implementor-name (so the
                    // Binder qualifies it per program-id) — that isolation is REQUIRED by the self-producing
                    // "absent optional" family (IX216A/217A/218A OPEN INPUT/EXTEND of an absent optional
                    // indexed file expects status 05/AT-END 10, NOT 00 from a leftover shared TF###; likewise
                    // SQ202A/203A). But a HANDFUL of OPTIONAL consumers legitimately read a file a SEPARATE
                    // producer program created and therefore MUST share that producer's "TF###". RL213A is
                    // one: RL212A (a distinct program) writes 500 records to RL-FS1 via the non-OPTIONAL
                    // `ASSIGN TO XXXXP021` → "TF021", then RL213A `SELECT OPTIONAL RL-FS1 ASSIGN TO XXXXX021`
                    // OPENs EXTEND, appends 501–520, and re-reads all 520. Left implementor-name-qualified,
                    // RL213A's RL-FS1 resolves to its own empty per-program file and every record-number check
                    // FAILs (ISO §13.18.43 OPTIONAL phrase governs only presence semantics, not the physical
                    // assignment target). The allow-list maps the OPTIONAL XXXXX### → "TF###" for exactly the
                    // (test, X-card) pairs that are cross-program consumers, never touching the isolation family.
                    sel = System.Text.RegularExpressions.Regex.Replace(
                        sel, @"(ASSIGN\s+(?:TO\s+)?)XXXXX(\d+)",
                        mm => OptionalSharedAssign.Contains((testName, mm.Groups[2].Value))
                            ? mm.Groups[1].Value + "\"TF" + mm.Groups[2].Value + "\""
                            : mm.Value);
                }
                return sel;
            });

        // ── SPECIAL-NAMES ──

        // XXXXX051: implementor switch name 1 (SPECIAL-NAMES ... IS switch-name)
        source = source.Replace("XXXXX051", "SWITCH-1");

        // XXXXX052: implementor switch name 2
        source = source.Replace("XXXXX052", "SWITCH-2");

        // XXXXX056: display output device implementor-name
        source = source.Replace("XXXXX056", "CONSOLE");

        // XXXXX057: accept input device implementor-name
        source = source.Replace("XXXXX057", "CONSOLE");

        // XXXXX073: the feature-name of a WRITE ... ADVANCING mnemonic-name (SQ207M). CSP — "suppress spacing", a
        // zero-line advance — is the feature-name whose rule the SQ207M golden encodes (the record lands on the
        // current line). Substituted since kb/Work PB862: the unsubstituted placeholder used to bind as a "device"
        // only because the SPECIAL-NAMES binder accepted any word as a system-name (§12.3.7.3 SR8 now refuses it).
        source = source.Replace("XXXXX073", "CSP");

        // ── CONFIGURATION ──

        // XXXXX082: SOURCE-COMPUTER
        source = source.Replace("XXXXX082", "COBOLSHARP");

        // XXXXX083: OBJECT-COMPUTER
        source = source.Replace("XXXXX083", "DOTNET");

        // XXXXX068: MEMORY SIZE value (obsolete COBOL-74 clause, semantically inert)
        source = source.Replace("XXXXX068", "65535");

        // ── Data/FD clauses ──

        // XXXXX084: implementor-specific label clause value (LABEL RECORDS)
        source = source.Replace("XXXXX084", "STANDARD");

        // XXXXX086/087/088: the implementor's own DATA DESCRIPTION of a file/label identifier item. The card is
        // written `01 IX-FD1-ID1` / `XXXXX086.` — the whole clause list is the X-card, and the implementor
        // supplies it. WiseOwl COBOL provides NO implementor-defined data description clause (§13.16.2 Format 1 is a
        // closed list, kb/Work PB487), so the description it supplies is a plain alphanumeric item; §13.16.3 SR8
        // requires an elementary entry to carry a PICTURE, which is exactly what these cards exist to provide.
        // ⛔ Behaviour-preserving for the whole corpus, verified per site: IX206A declares its three
        // (IX-FD1-ID1/IX-FD1-ID2/IX-FS1-ID2) and NEVER references them; IX205A's is a `*`-indicator comment
        // card; SQ401M's `03 VKEY XXXXX086.` is the operand of the obsolete, inert `VALUE OF` FD clause. Until
        // PB487 these cards were left unsubstituted and the §13.16.2 vendor catch-all ATE the raw token — the
        // items bound with no description at all and no diagnostic, which is the silent misbind that closing the
        // clause list exists to end. Token-boundary anchored, the XXXXX063/064/065 discipline (IX106A embeds
        // these digit runs inside a baselined test-data literal).
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX08[678](?![A-Za-z0-9])", "PICTURE X(10)");

        // XXXXX069 / XXXXX053: the two X-cards that name a construct WiseOwl COBOL DOES NOT PROVIDE, so the card is
        // DELETED — which is the CCVS-documented action for an optional card the implementor does not supply,
        // and the same treatment kb/Work PB487 gave XXXXX086-088 above.
        //   • XXXXX069 — the corpus states its own contract on the next line: "REPLACE WITH ADDITIONAL INFO
        //     (*OPT G ONLY)" (DB104A, DB202A-204A), and the card sits inside an FD clause list (ST139A:147,
        //     ST147A:170, between BLOCK CONTAINS and RECORD CONTAINS). It is an IMPLEMENTOR'S OWN FILE
        //     DESCRIPTION CLAUSE, and ISO §13.4.5.2 is a CLOSED list (kb/Work PB829) with no slot for one.
        //   • XXXXX053 — the whole I-O-CONTROL entry of IX302M:30 / RL302M:30 / SQ302M:29, written `XXXXX053.`
        //     with "Message expected for above statement: OBSOLETE" beneath it: the implementor's own RERUN
        //     clause, the obsolete §12.4.6.2 entry removed at COBOL-2002. WiseOwl COBOL supplies none, and an empty
        //     I-O-CONTROL paragraph is legal (the format brackets both of its clauses), so the card and its
        //     separator period go together — leaving the period behind would strand a clause-less DOT.
        // ⛔ Until kb/Work PB829 both cards were left unsubstituted and the vendor catch-all ATE the raw token,
        // so an FD and an I-O-CONTROL paragraph bound a description the program never wrote and no diagnostic
        // said so — the same silent misbind PB487 found under XXXXX086-088. Token-boundary anchored, the
        // XXXXX063/064/065 discipline (IX106A embeds these digit runs inside a baselined test-data literal).
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX069(?![A-Za-z0-9])", "");
        source = System.Text.RegularExpressions.Regex.Replace(
            source, @"(?<![A-Za-z0-9])XXXXX053(?![A-Za-z0-9])\s*\.", "");

        // ── Literal values ──

        // XXXXX081: non-COBOL characters value (implementor-defined)
        source = source.Replace("XXXXX081", "\"!@#$%^&*\"");

        // XXXXX090: CLASS definition character value (ordinal "A")
        source = source.Replace("XXXXX090", "\"A\"");

        // XXXXX091: CLASS definition character range end value (ordinal "D")
        source = source.Replace("XXXXX091", "\"D\"");

        return source;
    }
}
