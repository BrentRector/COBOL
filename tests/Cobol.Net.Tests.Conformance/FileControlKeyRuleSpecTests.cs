// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The file control entry's key syntax rules, screened AT THE ENTRY (ISO/IEC 1989:2023 §12.4.5.1 Format 1 ·
/// §12.4.5.2 SR10 · §12.4.5.6.3 · §12.4.5.12.3 · §12.4.5.13.3; kb/Work PB699).
/// <para>⛔ THE DISCRIMINATOR THESE TESTS EXIST FOR is the one the negative corpus cannot state on its own: the
/// SAME entry, once with a keyed verb and once with nothing but OPEN and CLOSE, has to produce the SAME
/// diagnostics. The rules used to be screened by <c>KeyedIoBinder.KeyedValidateFile</c> on the first keyed verb
/// naming the file, so the verb-bearing half was green and the entry-only half compiled clean — a green test
/// that held the gap open. Every case below is therefore run BOTH ways and the two results compared.</para>
/// <para>They also pin the two properties the move changed: the diagnostic is positioned at the CLAUSE (not at
/// whatever statement happened to touch the file first), and a file is reported ONCE however many statements
/// name it — the one-report-per-file property that used to be a <c>HashSet</c> memo and is now a consequence of
/// running the screen once per entry.</para>
/// </summary>
public sealed class FileControlKeyRuleSpecTests
{
    private static readonly int[] AllEditions = [85, 2002, 2014, 2023];

    private static IReadOnlyList<string> Diagnostics(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Fckr_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            var r = CompiledProgramCache.Compile(
                new CobolNet.CompilerDriver.Options(src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, SourceFormat: InitialReferenceFormat.Auto));
            return [.. r.Errors];
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>The procedure division that names the file WITHOUT any keyed verb — the shape that used to
    /// escape every one of these rules.</summary>
    private const string EntryOnlyBody = """
                PROCEDURE DIVISION.
                MAIN.
                    OPEN INPUT F
                    CLOSE F
                    STOP RUN.
                """;

    /// <summary>The same, plus the keyed verb that used to be the only thing that triggered the screen.</summary>
    private const string WithKeyedVerbBody = """
                PROCEDURE DIVISION.
                MAIN.
                    OPEN INPUT F
                    READ F NEXT AT END CONTINUE END-READ
                    CLOSE F
                    STOP RUN.
                """;

    private static string Indexed(string keyClauses, string record, string working, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRIX.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-ix.dat"
                ORGANIZATION IS INDEXED
                ACCESS MODE IS DYNAMIC
        {keyClauses}
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {record}
        {working}
        {body}
        """;

    private static string Relative(string access, string keyClause, string record, string working, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRRL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-rl.dat"
                ORGANIZATION IS RELATIVE
                ACCESS MODE IS {access}
        {keyClause}
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {record}
        {working}
        {body}
        """;

    /// <summary>Every rule of the table, at every edition it applies to (all four — the RECORD KEY, ALTERNATE
    /// RECORD KEY and RELATIVE KEY clauses and these syntax rules all predate COBOL-85), reported for the
    /// ENTRY-ONLY program and for the verb-bearing one ALIKE. The expected substring is the §, so a rule that
    /// starts reporting under a neighbour's citation fails here.</summary>
    public static TheoryData<string, string, string> Cases()
    {
        var d = new TheoryData<string, string, string>();

        // §12.4.5.12.3 SR1 — the prime key under OCCURS.
        d.Add("ix-record-key-occurs", "§12.4.5.12.3 SR1", "        RECORD KEY IS F-KEY.|"
            + "01 F-REC.\n   05 F-KG.\n      10 F-KEY PIC X(3) OCCURS 2 TIMES.\n   05 F-D PIC X(4).|");
        // §12.4.5.6.3 SR1 — an alternate key under OCCURS, the prime key legal.
        d.Add("ix-alternate-key-occurs", "§12.4.5.6.3 SR1",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-AG.\n      10 F-ALT PIC X(2) OCCURS 2 TIMES.|");
        // §12.4.5.12.3 SR2 — the prime key names an item outside the file's records.
        d.Add("ix-record-key-outside-record", "§12.4.5.12.3 SR2", "        RECORD KEY IS W-OUT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).|WORKING-STORAGE SECTION.\n01 W-OUT PIC X(3).");
        // §12.4.5.6.3 SR2 — an alternate key names an item outside the file's records (silently dropped before).
        d.Add("ix-alternate-key-outside-record", "§12.4.5.6.3 SR2",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS W-OUT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).|WORKING-STORAGE SECTION.\n01 W-OUT PIC X(3).");
        // §12.4.5.1 Format 1 — the RECORD KEY clause is unbracketed, so it is required for an indexed file.
        d.Add("ix-no-record-key", "ISO §12.4.5.1 Format 1", "        .|01 F-REC.\n   05 F-KEY PIC X(3).|");
        // §12.4.5.12.3 SR2's CATEGORY arm — the prime key is inside the record (so the location arm is
        // satisfied) but is category NUMERIC (kb/Work PB743). The two arms share a rule-id, so a case that
        // satisfied only one of them could not tell them apart; this one satisfies the location arm exactly.
        d.Add("ix-record-key-numeric", "category alphanumeric or category national", "        RECORD KEY IS F-KEY.|"
            + "01 F-REC.\n   05 F-KEY PIC 9(3).\n   05 F-D PIC X(4).|");
        // §12.4.5.6.3 SR2's CATEGORY arm, with a LEGAL prime key beside it so the count of 1 is the alternate's.
        d.Add("ix-alternate-key-numeric", "category alphanumeric or national",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-ALT PIC 9(2).|");
        // §12.4.5.12.3 SR3 / §12.4.5.6.3 SR3 — a key that is a dynamic-length elementary item (kb/Work PB1073). The
        // alternate key is the LAST item, so the minimum-record-size rule has nothing to add and SR3 is the only
        // rule this entry breaks.
        d.Add("ix-record-key-dynamic-length", "§12.4.5.12.3 SR3", "        RECORD KEY IS F-KEY.|"
            + "01 F-REC.\n   05 F-KEY PIC X DYNAMIC LENGTH.\n   05 F-D PIC X(4).|");
        d.Add("ix-alternate-key-dynamic-length", "§12.4.5.6.3 SR3",
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT.|"
            + "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-D PIC X(2).\n   05 F-ALT PIC X DYNAMIC LENGTH.|");
        return d;
    }

    /// <summary>A file control entry whose ORGANIZATION is written by the caller — the axis §12.4.5.2 SR8 and
    /// SR9 are stated on, and the one every other template here holds fixed.</summary>
    private static string Organized(string organization, string clauses, string record, string working, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRORG.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-org.dat"
        {organization}
        {clauses}
        DATA DIVISION.
        FILE SECTION.
        FD F.
        {record}
        {working}
        {body}
        """;

    /// <summary>⛔ §12.4.5.2 SR8 / SR9, SENTENCE 1 — <i>"Format 1 shall be specified only for an indexed
    /// file"</i> / <i>"Format 2 shall be specified only for a relative file"</i>, over EVERY organization the
    /// clause is illegal on, INCLUDING the omitted ORGANIZATION clause that §12.4.5.10.3 GR6 makes record
    /// sequential — the shape the defect was actually reported on (kb/Work PB742).
    /// <para>⚠ THE AXIS IS FLIPPED DELIBERATELY. Every other test in this file holds the organization at INDEXED
    /// or RELATIVE and varies the operand; this one holds the CLAUSE fixed and varies the organization, which is
    /// the only way to see a rule whose subject is the disagreement between the two. The last row of each set is
    /// the boundary: the same clause on the organization whose format carries it draws nothing.</para></summary>
    [Theory]
    // The Format-1 key clauses, on each organization that is not indexed. The "" organization is the ABSENT
    // ORGANIZATION clause — the GR6 default, and the reason the message names the omission.
    [InlineData("", "        RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    [InlineData("        ORGANIZATION IS SEQUENTIAL", "        RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    [InlineData("        ORGANIZATION IS LINE SEQUENTIAL", "        RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    [InlineData("        ORGANIZATION IS RELATIVE", "        RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    [InlineData("", "        ALTERNATE RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    [InlineData("        ORGANIZATION IS SEQUENTIAL", "        ALTERNATE RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", true)]
    // …and the boundary: on an INDEXED file the very same clause is the format's own required member.
    [InlineData("        ORGANIZATION IS INDEXED", "        RECORD KEY IS F-KEY.", "§12.4.5.2 SR8", false)]
    // The Format-2 key clause, on each organization that is not relative, and its own boundary.
    [InlineData("", "        RELATIVE KEY IS W-RK.", "§12.4.5.2 SR9", true)]
    [InlineData("        ORGANIZATION IS SEQUENTIAL", "        RELATIVE KEY IS W-RK.", "§12.4.5.2 SR9", true)]
    [InlineData("        ORGANIZATION IS LINE SEQUENTIAL", "        RELATIVE KEY IS W-RK.", "§12.4.5.2 SR9", true)]
    [InlineData("        ORGANIZATION IS RELATIVE", "        RELATIVE KEY IS W-RK.", "§12.4.5.2 SR9", false)]
    public void AKeyClauseOutsideItsFormat_IsRefusedAtEveryEdition(
        string organization, string clauses, string citation, bool expectRefusal)
    {
        string source = Organized(organization, clauses,
            "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-D PIC X(4).",
            "WORKING-STORAGE SECTION.\n01 W-RK PIC 9(4).", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            int hits = Diagnostics(source, edition).Count(e => e.Contains(citation, StringComparison.Ordinal));
            Assert.True(hits == (expectRefusal ? 1 : 0),
                $"[{organization.Trim()} + {clauses.Trim()}] @ {edition}: {citation} reported {hits}×, expected "
                + (expectRefusal ? 1 : 0));
        }
    }

    /// <summary>The entry-only / with-a-verb discriminator on SR8 itself: these are ENTRY rules, so the program
    /// that only OPENs and CLOSEs the file must hear exactly what the program with a READ hears. The rule was
    /// never screened anywhere before, so this is the property that keeps it from drifting onto a verb later.</summary>
    [Fact]
    public void Sr8_ReportsOnce_WithOrWithoutAKeyedVerb()
    {
        string record = "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-D PIC X(4).";
        foreach (int edition in AllEditions)
        {
            int entryOnly = Diagnostics(Organized("", "        RECORD KEY IS F-KEY.", record, "", EntryOnlyBody), edition)
                .Count(e => e.Contains("§12.4.5.2 SR8", StringComparison.Ordinal));
            int withVerb = Diagnostics(Organized("", "        RECORD KEY IS F-KEY.", record, "", WithKeyedVerbBody), edition)
                .Count(e => e.Contains("§12.4.5.2 SR8", StringComparison.Ordinal));
            Assert.Equal(1, entryOnly);
            Assert.Equal(1, withVerb);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IndexedEntryRule_ReportsAtEveryEdition_WithOrWithoutAKeyedVerb(string name, string citation, string parts)
    {
        string[] p = parts.Split('|');
        foreach (int edition in AllEditions)
        {
            int entryOnly = Diagnostics(Indexed(p[0], p[1], p[2], EntryOnlyBody), edition)
                .Count(e => e.Contains(citation, StringComparison.Ordinal));
            int withVerb = Diagnostics(Indexed(p[0], p[1], p[2], WithKeyedVerbBody), edition)
                .Count(e => e.Contains(citation, StringComparison.Ordinal));
            // ⛔ THE DISCRIMINATOR: the entry rule is reported EXACTLY ONCE whether or not a keyed verb names
            // the file. Before PB699 the entry-only count was 0 and the verb-bearing one was 1.
            Assert.True(entryOnly == 1, $"{name} @ {edition}: entry-only program reported {citation} {entryOnly}×, expected 1");
            Assert.True(withVerb == 1, $"{name} @ {edition}: verb-bearing program reported {citation} {withVerb}×, expected 1");
        }
    }

    /// <summary>§12.4.5.2 SR10 — <i>"The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of
    /// the ACCESS clause is specified."</i> ⚠ THE CITATION IS THE ASSERTION: this rejection used to cite
    /// §12.4.5.13, which has no syntax rules at all, and §12.4.5.1 Format 2 BRACKETS the RELATIVE KEY clause, so
    /// neither the clause nor the format requires it — SR10 is the one sentence that does.</summary>
    [Theory]
    [InlineData("RANDOM")]
    [InlineData("DYNAMIC")]
    public void RelativeKeyRequiredByAccessMode_CitesSr10_AtEveryEdition(string access)
    {
        foreach (int edition in AllEditions)
        {
            var entryOnly = Diagnostics(Relative(access, "        .", "01 F-REC PIC X(8).", "", EntryOnlyBody), edition);
            Assert.Contains(entryOnly, e => e.Contains("§12.4.5.2 SR10", StringComparison.Ordinal));
            Assert.DoesNotContain(entryOnly, e => e.Contains("§12.4.5.13 —", StringComparison.Ordinal));
        }
    }

    /// <summary>ACCESS SEQUENTIAL does NOT require the clause: §12.4.5.2 SR10 names only DYNAMIC and RANDOM, and
    /// the Format 2 diagram brackets it. The rule's own boundary, measured — not merely the violation.</summary>
    [Fact]
    public void RelativeKeyOmitted_UnderAccessSequential_IsLegal()
    {
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(Relative("SEQUENTIAL", "        .", "01 F-REC PIC X(8).", "", EntryOnlyBody), edition);
            Assert.DoesNotContain(d, e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
        }
    }

    /// <summary>§12.4.5.13.3's three syntax rules, each on an entry-only program: SR1 (OCCURS), SR2 (an unsigned
    /// integer without 'P'), SR3 (not defined in a record description entry subordinate to the file-name).
    /// SR2 and SR3 were cited as "§12.4.5.13 SR2/SR3" before PB699 — a clause that carries no syntax rules.</summary>
    [Theory]
    [InlineData("§12.4.5.13.3 SR1", "01 F-REC PIC X(8).", "WORKING-STORAGE SECTION.\n01 W-T.\n   05 W-RK PIC 9(4) OCCURS 3 TIMES.")]
    [InlineData("§12.4.5.13.3 SR2", "01 F-REC PIC X(8).", "WORKING-STORAGE SECTION.\n01 W-RK PIC S9(4).")]
    [InlineData("§12.4.5.13.3 SR3", "01 F-REC.\n   05 W-RK PIC 9(4).\n   05 F-D PIC X(4).", "")]
    public void RelativeKeyOperandRule_ReportsOnAnEntryOnlyProgram_AtEveryEdition(string citation, string record, string working)
    {
        foreach (int edition in AllEditions)
        {
            var entryOnly = Diagnostics(
                Relative("DYNAMIC", "        RELATIVE KEY IS W-RK.", record, working, EntryOnlyBody), edition);
            Assert.Contains(entryOnly, e => e.Contains(citation, StringComparison.Ordinal));
            Assert.DoesNotContain(entryOnly, e => e.Contains("§12.4.5.13 SR", StringComparison.Ordinal));
        }
    }

    /// <summary>THE OVER-REJECTION GUARD. A legal indexed entry — a GROUP prime key (§13.18.29.4 GR3 makes a
    /// group with no GROUP-USAGE clause an alphanumeric group item, which §12.4.5.12.3 SR2 admits), reached
    /// through an IN qualifier (§8.4.2.2), plus an alternate key WITH DUPLICATES — draws no key diagnostic at
    /// any edition. The screen's within-a-record test walks to the 01 root, and a qualified reference has to
    /// survive it.</summary>
    [Fact]
    public void LegalIndexedEntry_WithAGroupKeyAndAQualifiedReference_IsAccepted()
    {
        string source = Indexed(
            "        RECORD KEY IS F-KEY IN F-REC\n        ALTERNATE RECORD KEY IS F-ALT WITH DUPLICATES.",
            "01 F-REC.\n   05 F-KEY.\n      10 F-K1 PIC X.\n      10 F-K2 PIC X.\n   05 F-ALT PIC X(2).",
            "", WithKeyedVerbBody);
        foreach (int edition in AllEditions)
            Assert.DoesNotContain(Diagnostics(source, edition), e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
    }

    /// <summary>A sort-merge entry, whose SELECT is the caller's — §12.4.5.1 Format 4 admits ASSIGN and an
    /// optional <c>[ ORGANIZATION IS ] SEQUENTIAL</c> and nothing else.</summary>
    private static string SortMerge(string clauses) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FCKRSD.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN TO "fckr-sd.tmp"
        {clauses}
        DATA DIVISION.
        FILE SECTION.
        SD F.
        01 F-REC.
           05 F-KEY PIC X(3).
           05 F-D PIC X(4).
        WORKING-STORAGE SECTION.
        01 F-FS PIC XX.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY "X"
            STOP RUN.
        """;

    /// <summary>⛔ §12.4.5.2 SR8 / SR9, SENTENCE 2 — <i>"The associated file description entry shall not be a
    /// sort-merge file description entry"</i>, printed verbatim in both rules (COBOLNET1900, kb/Work PB742).
    /// <para>The entry may specify its format by a key clause OR by the ORGANIZATION clause ALONE, and the
    /// ORGANIZATION-only rows are the ones no per-key-clause screen could ever reach — they are why the table has
    /// a rule stated about the ENTRY. The last row is the BOUNDARY and is the load-bearing half of this test:
    /// the screen used to skip every sort-merge file with a blanket early return, so a legal Format-4 entry has
    /// to keep drawing nothing now that the return is gone.</para></summary>
    [Theory]
    [InlineData("        RECORD KEY IS F-KEY.", 1)]
    [InlineData("        ALTERNATE RECORD KEY IS F-KEY.", 1)]
    [InlineData("        ORGANIZATION IS INDEXED.", 1)]
    [InlineData("        ORGANIZATION IS RELATIVE.", 1)]
    [InlineData("        ORGANIZATION IS SEQUENTIAL.", 0)]   // Format 4's own optional clause
    [InlineData("        .", 0)]                             // Format 4 with the clause omitted
    public void AFormat1Or2ClauseOnASortMergeEntry_IsRefusedAtEveryEdition(string clauses, int expected)
    {
        foreach (int edition in AllEditions)
        {
            int hits = Diagnostics(SortMerge(clauses), edition)
                .Count(e => e.Contains("COBOLNET1900", StringComparison.Ordinal));
            Assert.True(hits == expected, $"[SD + {clauses.Trim()}] @ {edition}: COBOLNET1900 {hits}×, expected {expected}");
        }
    }

    /// <summary>THE OTHER HALF OF THE DELETED EARLY RETURN. A legal sort-merge entry must draw no KEY-clause
    /// diagnostic either — before kb/Work PB742 that was guaranteed by <c>if (file.IsSortMerge) return;</c>, and
    /// it is now a property of every row's <c>ScreenedOn</c> set. A row that forgot to exclude the sort-merge
    /// kind would tell an ordinary SORT program that its file "has no RECORD KEY clause".</summary>
    [Fact]
    public void ALegalSortMergeEntry_DrawsNoKeyClauseDiagnostic()
    {
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(SortMerge("        ORGANIZATION IS SEQUENTIAL."), edition);
            Assert.DoesNotContain(d, e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
            Assert.DoesNotContain(d, e => e.Contains("COBOLNET1900", StringComparison.Ordinal));
        }
    }

    /// <summary>⛔ §12.4.5.2 SR11 SENTENCE 2 AND SR13 — every clause a sort-merge file's entry writes beyond
    /// §12.4.5.1 Format 4 (SELECT, ASSIGN, <c>[ [ ORGANIZATION IS ] SEQUENTIAL ]</c>) is refused by exactly ONE
    /// row, under the rule that fits it (COBOLNET1900, kb/Work PB773): a clause only Format 3 prints names that
    /// format (SR11), one that several formats print names none (SR13). The minimum edition is where the clause
    /// itself exists, because an edition error for the clause would otherwise be what the case measures.</summary>
    [Theory]
    [InlineData("        FILE STATUS IS F-FS.", "SR13", 85)]
    [InlineData("        ACCESS MODE IS SEQUENTIAL.", "SR13", 85)]
    [InlineData("        RESERVE 2 AREAS.", "SR13", 85)]
    [InlineData("        ACCESS MODE IS RANDOM.", "SR13", 85)]   // also a §12.4.5.5.2 SR2 error: two rules, two sentences
    [InlineData("        LOCK MODE IS MANUAL.", "SR13", 2002)]
    [InlineData("        LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS.", "SR13", 2002)]
    [InlineData("        SHARING WITH NO OTHER.", "SR13", 2002)]
    [InlineData("        RECORD DELIMITER IS STANDARD-1.", "SR11", 85)]
    public void AClauseBeyondFormat4_OnASortMergeEntry_IsRefusedByTheRuleThatFitsIt(
        string clauses, string rule, int minEdition)
    {
        foreach (int edition in AllEditions.Where(e => e >= minEdition))
        {
            int hits = Diagnostics(SortMerge(clauses), edition)
                .Count(e => e.Contains("COBOLNET1900", StringComparison.Ordinal)
                    && e.Contains($"(ISO §12.4.5.2 {rule})", StringComparison.Ordinal));
            Assert.True(hits == 1, $"[SD + {clauses.Trim()}] @ {edition}: {rule} reported {hits}×, expected 1");
        }
    }

    /// <summary>The entry rules are about the ENTRY, so a clause beyond Format 4 on an SD is refused whether or not
    /// the program ever names the file in a SORT statement — the bare program above has none.</summary>
    [Fact]
    public void ASortMergeEntryWithTwoClausesBeyondFormat4_IsRefusedOncePerClause()
    {
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(SortMerge("        FILE STATUS IS F-FS\n        RECORD DELIMITER IS STANDARD-1."), edition);
            Assert.Equal(1, d.Count(e => e.Contains("(ISO §12.4.5.2 SR13)", StringComparison.Ordinal)));
            Assert.Equal(1, d.Count(e => e.Contains("(ISO §12.4.5.2 SR11)", StringComparison.Ordinal)));
        }
    }

    /// <summary>§12.4.5.2 SR11 SENTENCE 1 — <i>"Format 3 shall be specified only for a sequential file or a report
    /// file"</i> — over every organization (COBOLNET2912): the RECORD DELIMITER clause is Format 3's alone, so it
    /// is refused on an indexed or relative file, and the BOUNDARY rows are the same clause on the sequential
    /// files it is for, and the clauses that Formats 1 to 3 share on an indexed file, which are not Format 3's.</summary>
    [Theory]
    [InlineData("        ORGANIZATION IS INDEXED\n        RECORD KEY IS F-KEY\n        RECORD DELIMITER IS STANDARD-1.", 1)]
    [InlineData("        ORGANIZATION IS RELATIVE\n        RECORD DELIMITER IS STANDARD-1.", 1)]
    [InlineData("        ORGANIZATION IS SEQUENTIAL\n        RECORD DELIMITER IS STANDARD-1.", 0)]
    [InlineData("        RECORD DELIMITER IS STANDARD-1.", 0)]
    [InlineData("        ORGANIZATION IS INDEXED\n        RECORD KEY IS F-KEY\n        FILE STATUS IS F-FS\n        RESERVE 2 AREAS.", 0)]
    public void Format3Clause_OnAKeyedFile_IsRefusedAtEveryEdition(string clauses, int expected)
    {
        string source = Organized("", clauses, "01 F-REC.\n   05 F-KEY PIC X(3).\n   05 F-D PIC X(4).",
            "WORKING-STORAGE SECTION.\n01 F-FS PIC XX.", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            int hits = Diagnostics(source, edition).Count(e => e.Contains("COBOLNET2912", StringComparison.Ordinal));
            Assert.True(hits == expected, $"[{clauses.Replace('\n', ' ').Trim()}] @ {edition}: COBOLNET2912 {hits}×, expected {expected}");
        }
    }

    /// <summary>The two ALTERNATE RECORD KEY rules of kb/Work PB1073, each over every position the rule names: the
    /// prime key's own leftmost byte (an alternate that IS the prime key, and one that is the first subordinate of
    /// the prime GROUP), and another alternate's (two clauses on one item, and a group and its first subordinate).
    /// A clause coincides with something and so does the OTHER one, so a coinciding pair is reported twice — each
    /// clause is the subject of its own sentence — and a lone coincidence with the prime key once.</summary>
    [Theory]
    [InlineData("        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-KEY.", 1)]
    [InlineData("        RECORD KEY IS F-KG\n        ALTERNATE RECORD KEY IS F-K1.", 1)]
    [InlineData("        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT\n        ALTERNATE RECORD KEY IS F-ALT.", 2)]
    [InlineData("        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-AG\n        ALTERNATE RECORD KEY IS F-A1.", 2)]
    // The boundary: an alternate that begins INSIDE the prime group but not at its first byte, one after it, and a
    // prime key that begins where an alternate's group does not.
    [InlineData("        RECORD KEY IS F-KG\n        ALTERNATE RECORD KEY IS F-K2\n        ALTERNATE RECORD KEY IS F-ALT.", 0)]
    [InlineData("        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-A2\n        ALTERNATE RECORD KEY IS F-AG.", 0)]
    public void AnAlternateKey_AtAnotherKeysLeftmostBytePosition_IsRefusedAtEveryEdition(string keyClauses, int expected)
    {
        const string record = "01 F-REC.\n   05 F-KEY PIC X(2).\n   05 F-KG REDEFINES F-KEY.\n      10 F-K1 PIC X.\n      10 F-K2 PIC X.\n"
            + "   05 F-ALT PIC X(3).\n   05 F-AG.\n      10 F-A1 PIC X.\n      10 F-A2 PIC X(2).";
        foreach (int edition in AllEditions)
            foreach (string body in new[] { EntryOnlyBody, WithKeyedVerbBody })
            {
                int hits = Diagnostics(Indexed(keyClauses, record, "", body), edition)
                    .Count(e => e.Contains("§12.4.5.6.3 SR4", StringComparison.Ordinal));
                Assert.True(hits == expected,
                    $"[{keyClauses.Replace('\n', ' ').Trim()}] @ {edition}: SR4 reported {hits}×, expected {expected}");
            }
    }

    /// <summary>THE OVER-REJECTION GUARD ON THE CATEGORY ARM (kb/Work PB743). §12.4.5.12.3 SR2 and §12.4.5.6.3
    /// SR2 admit <i>category alphanumeric</i>, and §13.18.29.4 GR3 makes a group with no GROUP-USAGE clause an
    /// ALPHANUMERIC GROUP ITEM — so a group key is legal and a category screen written as an elementary-PICTURE
    /// test would refuse it. The relative key beside it is the cross-role boundary: §12.4.5.13.3 SR2 REQUIRES the
    /// unsigned integer the record-key rules forbid, so the two category rules must not leak into each other.</summary>
    [Fact]
    public void LegalCategories_AGroupRecordKeyAndANumericRelativeKey_AreAccepted()
    {
        string indexed = Indexed(
            "        RECORD KEY IS F-KEY\n        ALTERNATE RECORD KEY IS F-ALT.",
            "01 F-REC.\n   05 F-KEY.\n      10 F-K1 PIC X.\n      10 F-K2 PIC X.\n   05 F-ALT PIC X(2).",
            "", EntryOnlyBody);
        string relative = Relative("DYNAMIC", "        RELATIVE KEY IS W-RK.", "01 F-REC PIC X(8).",
            "WORKING-STORAGE SECTION.\n01 W-RK PIC 9(4).", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            Assert.DoesNotContain(Diagnostics(indexed, edition), e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
            Assert.DoesNotContain(Diagnostics(relative, edition), e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
        }
    }

    // ── The DECLINED second key form: record-key-name-1 SOURCE IS { data-name-2 } … ────────────────────────
    //    ISO §12.4.5.12.2 / §12.4.5.6.2 print a required choice of TWO key forms and this implementation
    //    provides only the first. Annex A.3 item 40 makes the SOURCE phrase processor-dependent, §4.2.6 ¶3
    //    obliges the compiler to say so at compile time, and its closing sentence — "The implementor is not
    //    required to produce executable code when unsupported processor-dependent language elements are used" —
    //    is what makes REFUSING (rather than accepting inert) conforming. kb/Work PB358.
    //    The refusal itself is pinned by the negative corpus (tests/conformance/negative/pb358-*). What lives
    //    HERE is what that corpus structurally cannot say: the two properties either side of the refusal.
    private const string SourcePhraseDecline = "COBOLNET1954";

    /// <summary>⛔ THE CASCADE GUARD, and it is a MEASURED regression, not a hypothetical: the first build of
    /// the grammar surface emitted COBOLNET1954 <i>and</i> §12.4.5.1 Format 1's "indexed file 'IXF' has no
    /// RECORD KEY clause" — a false sentence (the program plainly wrote the clause) pointing at a different
    /// repair. The Format-1 rule's subject is the clause's PRESENCE, so it now reads
    /// <c>FileKeyOperand.Written</c> rather than inferring absence from a null data-name. Asserting the ABSENCE
    /// of the second diagnostic is the only way to keep the two facts apart — a count of "at least one error"
    /// passes either way (feedback_green_gates_arent_evidence).</summary>
    [Fact]
    public void PrimeKeySourcePhrase_IsRefusedByName_AndTheClauseIsNotAlsoReportedMissing()
    {
        string source = Indexed("        RECORD KEY IS F-COMPOSITE SOURCE IS F-K1 F-K2.",
            "01 F-REC.\n   05 F-K1 PIC X(2).\n   05 F-K2 PIC X(2).\n   05 F-D PIC X(4).", "", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(source, edition);
            Assert.Contains(d, e => e.Contains(SourcePhraseDecline, StringComparison.Ordinal));
            Assert.DoesNotContain(d, e => e.Contains("has no RECORD KEY clause", StringComparison.Ordinal));
            Assert.DoesNotContain(d, e => e.Contains("ISO §12.4.5.1 Format 1", StringComparison.Ordinal));
        }
    }

    /// <summary>The ALTERNATE twin, refused from the same site under the same code — Annex A.3 item 40 names
    /// both clauses in ONE sentence, so a fix reaching only the prime key would be this repository's most
    /// reproducible defect shape (feedback_two_arm_dispatch). The prime key here is legal, so the only thing
    /// the entry can be refused for is the alternate clause's SOURCE phrase.
    /// <para>The <c>WITH DUPLICATES</c> row is not decoration: it proves the clause is recognized WHOLE rather
    /// than up to the SOURCE token, which is what the generic parse error used to do — it reported a second
    /// unexpected-token error at <c>WITH</c>.</para></summary>
    [Theory]
    [InlineData("        RECORD KEY IS F-K1\n        ALTERNATE RECORD KEY IS F-ALT SOURCE IS F-K2 F-D.")]
    [InlineData("        RECORD KEY IS F-K1\n        ALTERNATE RECORD KEY IS F-ALT SOURCE IS F-K2 WITH DUPLICATES.")]
    public void AlternateKeySourcePhrase_IsRefusedByName_AtEveryEdition(string keyClauses)
    {
        string source = Indexed(keyClauses,
            "01 F-REC.\n   05 F-K1 PIC X(2).\n   05 F-K2 PIC X(2).\n   05 F-D PIC X(4).", "", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(source, edition);
            Assert.Contains(d, e => e.Contains(SourcePhraseDecline, StringComparison.Ordinal));
            Assert.DoesNotContain(d, e => e.Contains("has no RECORD KEY clause", StringComparison.Ordinal));
        }
    }

    /// <summary>⛔ THE COMPLEMENT — what the decline must NOT reach (feedback_measure_the_selectors_complement).
    /// A widened grammar that swallowed the ordinary data-name-1 arm into the declined one would refuse every
    /// indexed file in the corpus, and a test that only asserts the refusal fires would still be green.
    /// The optional-word spellings are here for the same reason: §12.4.5.12.2's KEY and IS carry no underline
    /// on the printed page, so <c>RECORD F-KEY</c> is a conforming RECORD KEY clause and must stay one.</summary>
    [Theory]
    [InlineData("        RECORD KEY IS F-K1\n        ALTERNATE RECORD KEY IS F-K2 WITH DUPLICATES.")]
    [InlineData("        RECORD KEY F-K1\n        ALTERNATE RECORD KEY F-K2.")]
    [InlineData("        RECORD F-K1.")]
    public void AnOrdinaryKeyClause_DrawsNoSourcePhraseDecline_AtEveryEdition(string keyClauses)
    {
        string source = Indexed(keyClauses,
            "01 F-REC.\n   05 F-K1 PIC X(2).\n   05 F-K2 PIC X(2).\n   05 F-D PIC X(4).", "", EntryOnlyBody);
        foreach (int edition in AllEditions)
        {
            var d = Diagnostics(source, edition);
            Assert.DoesNotContain(d, e => e.Contains(SourcePhraseDecline, StringComparison.Ordinal));
            Assert.DoesNotContain(d, e => e.Contains("COBOLNET0863", StringComparison.Ordinal));
        }
    }
}
