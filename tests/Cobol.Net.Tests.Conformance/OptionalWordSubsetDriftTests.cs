// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE OPTIONAL-WORD DRIFT GATE — ISO §8.3.2.4.3: "Within each format, uppercase words that are not underlined
/// are called optional words and may be specified at the user's option WITH NO EFFECT ON THE SEMANTICS OF THE
/// FORMAT." §5.2.3 says the same of the words themselves. This test takes that literally: for each format below
/// it writes EVERY SUBSET of the format's optional words and demands that all of them compile and produce
/// BYTE-IDENTICAL output. A word made required again fails immediately, and so does a word whose presence quietly
/// changes what the statement binds to.
/// </summary>
/// <remarks>
/// WHY A SUBSET SWEEP AND NOT TWO SPELLINGS. kb/Work PB332: <c>useStatement</c> accepted STANDARD and ON as
/// optional because "the CCVS suite and mainstream compilers write both" spellings — the optional-word set was
/// derived from what a witness corpus happened to contain, so the two words no witness omitted (AFTER, PROCEDURE)
/// stayed required and rejected legal COBOL for as long as nobody wrote them. A pair of hand-picked spellings
/// would have passed the whole time. The POWER SET cannot: it writes the combination nobody thought of.
///
/// Adding a format is one row in <see cref="Formats"/>. The optional-word list is measured from the printed page
/// (<c>scripts/spec/figure_extract.py</c>), never from the transcription and never from the corpus, and
/// <c>scripts/spec/audit_grammar_optional_words.py</c> is the tool that finds the ones still missing.
/// </remarks>
public sealed class OptionalWordSubsetDriftTests
{
    /// <param name="Name">Test identity, and the PROGRAM-ID stem.</param>
    /// <param name="Clause">The general format whose underlining was measured.</param>
    /// <param name="Edition">The ISO edition to compile as — the earliest at which the format exists.</param>
    /// <param name="OptionalWords">The words printed WITHOUT an underline in that format.</param>
    /// <param name="ProgramId">The template's PROGRAM-ID stem — rewritten per spelling so no name is compiled twice.</param>
    /// <param name="Template">Source carrying one <c>{n}</c> slot per optional word.</param>
    private sealed record FormatCase(string Name, string Clause, int Edition, string[] OptionalWords,
                                     string ProgramId, string Template);

    // ── USE Format 1 (§14.9.49.2): USE [GLOBAL] AFTER STANDARD {EXCEPTION | ERROR} PROCEDURE ON {…} ──────────
    // Underlined on printed folio 774: USE, GLOBAL, EXCEPTION, ERROR, INPUT, OUTPUT, I-O, EXTEND. Plain: AFTER,
    // STANDARD, PROCEDURE, ON. Neither file is ever opened, so the CLOSE is unsuccessful with I-O status 42
    // (§14.9.6.4 GR1) and the declarative selected by §14.9.49.4 GR6 a) reports it.
    private const string UseFormat1 = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWA.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F1 ASSIGN TO "opwa.dat"
                   ORGANIZATION IS SEQUENTIAL
                   ACCESS MODE IS SEQUENTIAL
                   FILE STATUS IS ST1.
           DATA DIVISION.
           FILE SECTION.
           FD F1.
           01 R1 PIC X(8).
           WORKING-STORAGE SECTION.
           01 ST1 PIC XX.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H-SECT SECTION.
               USE {0} {1} ERROR {2} {3} F1.
           H-PARA.
               DISPLAY "HANDLER=" ST1.
           END DECLARATIVES.
           MAIN-SECT SECTION.
           MAIN.
               CLOSE F1
               DISPLAY "AFTER=" ST1
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── USE Format 3 (§14.9.49.2): USE AFTER {EXCEPTION CONDITION | EC} … ─────────────────────────────────────
    // Only AFTER is plain. EC is synonymous with EXCEPTION CONDITION (§14.9.49.3 SR12), so the two spellings of
    // the keyword pair are covered by the two rows below rather than by the subset sweep.
    private const string UseFormat3 = """
          >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWB.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 G.
              05 T PIC 9(2) OCCURS 3 TIMES.
           01 IDX PIC 9(2) VALUE 5.
           01 R  PIC 9(2) VALUE 0.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H SECTION.
               USE {0} EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
           H-P.
               DISPLAY "HANDLED".
               RESUME AT NEXT STATEMENT.
           END DECLARATIVES.
           MAIN SECTION.
           MAIN-P.
               MOVE T (IDX) TO R.
               DISPLAY "AFTER".
               STOP RUN.
    """;

    private const string UseFormat3Ec = """
          >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWC.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 G.
              05 T PIC 9(2) OCCURS 3 TIMES.
           01 IDX PIC 9(2) VALUE 5.
           01 R  PIC 9(2) VALUE 0.
           PROCEDURE DIVISION.
           DECLARATIVES.
           H SECTION.
               USE {0} EC EC-BOUND-SUBSCRIPT.
           H-P.
               DISPLAY "HANDLED".
               RESUME AT NEXT STATEMENT.
           END DECLARATIVES.
           MAIN SECTION.
           MAIN-P.
               MOVE T (IDX) TO R.
               DISPLAY "AFTER".
               STOP RUN.
    """;

    // ── START … [WITH] LENGTH (§14.9.41.2) ───────────────────────────────────────────────────────────────────
    // Printed folio 754 underlines LENGTH and leaves WITH plain. §14.9.41.4 GR17 b) makes the temporary key area
    // 2 characters, so both spellings must position at AA01 and read it back identically.
    private const string StartWithLength = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWD.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT IXF ASSIGN TO "opwd.dat"
                   ORGANIZATION IS INDEXED
                   ACCESS MODE IS DYNAMIC
                   RECORD KEY IS IX-KEY
                   FILE STATUS IS ST1.
           DATA DIVISION.
           FILE SECTION.
           FD IXF.
           01 IX-REC.
              05 IX-KEY PIC X(4).
              05 IX-VAL PIC X(4).
           WORKING-STORAGE SECTION.
           01 ST1 PIC XX.
           PROCEDURE DIVISION.
           MAIN.
               OPEN OUTPUT IXF
               MOVE "AA01" TO IX-KEY MOVE "ONE " TO IX-VAL WRITE IX-REC
               MOVE "BB01" TO IX-KEY MOVE "TWO " TO IX-VAL WRITE IX-REC
               CLOSE IXF
               OPEN INPUT IXF
               MOVE "AAZZ" TO IX-KEY
               START IXF KEY IS >= IX-KEY {0} LENGTH 2
                   INVALID KEY DISPLAY "INVALID"
               END-START
               READ IXF NEXT RECORD
                   AT END DISPLAY "EOF"
               END-READ
               DISPLAY "KEY=" IX-KEY "|" IX-VAL "|" ST1
               CLOSE IXF
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── SPECIAL-NAMES §12.3.7.2, the COBOL-85 clauses (kb/Work PB695) ────────────────────────────────────────
    // Printed folio 290 rules ALPHANUMERIC CLASS COMMA CRT CURRENCY CURSOR DECIMAL-POINT IN LOCALE NATIONAL OFF
    // ON ORDER PICTURE SPECIAL-NAMES. STATUS SYMBOL TABLE THROUGH THRU; folios 291/292 add ALPHABET, NATIVE,
    // STANDARD-1/2, UCS-4, UTF-8/16 and SYMBOLIC. IS is in none of them, and neither is the CHARACTERS of
    // SYMBOLIC CHARACTERS (the heading line's one rule stops three points short of it). §13.18.44 exchanges the
    // period and the comma under DECIMAL-POINT IS COMMA, so every spelling must edit 123,45 identically.
    private const string SpecialNames85 = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWE.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           SPECIAL-NAMES.
               DECIMAL-POINT {0} COMMA
               CURRENCY {1} {2} "@"
               SYMBOLIC {3} SC-ORD66 {4} 66
               ALPHABET AL-NATV {5} NATIVE
               SWITCH-1 {6} MNEM-ONE.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 N   PIC 9(3)V99 VALUE 123,45.
           01 CUR PIC @@@9,99.
           01 SC  PIC X.
           PROCEDURE DIVISION.
           MAIN.
               MOVE N TO CUR
               DISPLAY "CUR=" CUR
               MOVE SC-ORD66 TO SC
               DISPLAY "SC=" SC
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── SPECIAL-NAMES §12.3.7.2, the post-85 words ───────────────────────────────────────────────────────────
    // The FOR that introduces ALPHANUMERIC/NATIONAL is plain wherever the paragraph prints it (CLASS on folio
    // 290, ALPHABET on 291, SYMBOLIC CHARACTERS on 292) — since PB695 one grammar rule serves all three — and
    // the WITH of `[ WITH PICTURE SYMBOL literal-8 ]` is plain too.
    private const string SpecialNamesFor = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWF.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           SPECIAL-NAMES.
               CLASS HEXDIG {0} ALPHANUMERIC IS "0" THRU "9"
               SYMBOLIC CHARACTERS {1} ALPHANUMERIC SC-ORD66 IS 66
               ALPHABET AL-ALNUM {2} ALPHANUMERIC IS NATIVE
               CURRENCY SIGN IS "@" {3} PICTURE SYMBOL "#".
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 D5  PIC X VALUE "5".
           01 SC  PIC X.
           01 N   PIC 9(3)V99 VALUE 123.45.
           01 CUR PIC ###9.99.
           PROCEDURE DIVISION.
           MAIN.
               IF D5 IS HEXDIG DISPLAY "HEX=yes" ELSE DISPLAY "HEX=no" END-IF
               MOVE SC-ORD66 TO SC
               DISPLAY "SC=" SC
               MOVE N TO CUR
               DISPLAY "CUR=" CUR
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── SPECIAL-NAMES §12.3.7.2, the implementor-switch entry ────────────────────────────────────────────────
    // Folio 290's switch rows carry EXACTLY two underline rules on the whole stack, under ON and under OFF, so
    // `[IS mnemonic-name-1]`, `ON [STATUS] [IS] condition-name-1` and the OFF row are five optional words. The
    // grammar used to hand-write that power set as two alternatives per phrase and dropped a member of it.
    private const string SpecialNamesSwitch = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWG.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           SPECIAL-NAMES.
               SWITCH-2 {0} MNEM-TWO ON {1} {2} SW-IS-ON OFF {3} {4} SW-IS-OFF.
           PROCEDURE DIVISION.
           MAIN.
               DISPLAY "PARSED"
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── OBJECT-COMPUTER §12.3.6.2 ────────────────────────────────────────────────────────────────────────────
    // Printed folio 285's whole underline roster is ALPHANUMERIC CLASSIFICATION LOCALE NATIONAL OBJECT-COMPUTER.
    // SEQUENCE SYSTEM-DEFAULT USER-DEFAULT, so CHARACTER, PROGRAM, COLLATING, FOR and IS are all optional. The
    // reversed alphabet makes the choice observable: §12.3.6.4 9) + §8.8.4.2.7 compare under it, so "A" > "Z".
    private const string ObjectComputer = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWH.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           OBJECT-COMPUTER.
               {0} SEQUENCE {1} ALPHANUMERIC {2} AL-REV.
           SPECIAL-NAMES.
               ALPHABET AL-REV IS "Z" THRU "A".
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 LOW-CH  PIC X VALUE "A".
           01 HIGH-CH PIC X VALUE "Z".
           PROCEDURE DIVISION.
           MAIN.
               IF LOW-CH < HIGH-CH DISPLAY "A-LT-Z=yes" ELSE DISPLAY "A-LT-Z=no" END-IF
               DISPLAY "DONE"
               STOP RUN.
    """;

    // PROGRAM and COLLATING are separate optional words of the same head; the FOR-less arm above cannot also
    // exercise the `IS alphabet-name-1` arm, so this row sweeps the head over that arm instead — at COBOL-85,
    // where that arm is the whole format.
    // ⛔ THE COMPUTER-NAME IS WRITTEN DELIBERATELY, and it is what makes this row bite. Omitting it is a
    // SEPARATE 2002 relaxation (`computer-name-optional-2002`, COBOLNET0900), so a name-less spelling at 85 is
    // correctly rejected and would only measure that gate. With the name present, every subset also proves the
    // clause is read as a CLAUSE after the name and not as the closed format's residue (kb/Work PB830's
    // `unrecognizedClause`), whether it opens on PROGRAM, on COLLATING or on SEQUENCE.
    private const string ObjectComputerHead = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWI.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           OBJECT-COMPUTER. IBM-370
               {0} {1} SEQUENCE {2} AL-REV.
           SPECIAL-NAMES.
               ALPHABET AL-REV IS "Z" THRU "A".
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 LOW-CH  PIC X VALUE "A".
           01 HIGH-CH PIC X VALUE "Z".
           PROCEDURE DIVISION.
           MAIN.
               IF LOW-CH < HIGH-CH DISPLAY "A-LT-Z=yes" ELSE DISPLAY "A-LT-Z=no" END-IF
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── OPTIONS §11.9.10.2 and SET §14.9.39.2 Formats 7 and 16 ───────────────────────────────────────────────
    // Folio 277's roster omits SECTION and TO; folio 730 rules ADDRESS and not the OF beside it (folio 140, the
    // §8.4.3.11.2 data-address-identifier, rules ADDRESS alone); folio 732 rules SET, SIZE and TO and not OF.
    private const string OptionsAndSet = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWJ.
           OPTIONS.
               INITIALIZE WORKING-STORAGE {0} {1} HIGH-VALUES.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 W-FILL PIC X(4).
           01 W-ITEM PIC X(4) VALUE "ABCD".
           01 W-PTR  USAGE POINTER.
           01 W-DYN  PIC X DYNAMIC LENGTH.
           LINKAGE SECTION.
           01 W-BASED PIC X(4) BASED.
           PROCEDURE DIVISION.
           MAIN.
               IF W-FILL = HIGH-VALUES DISPLAY "FILL=high" ELSE DISPLAY "FILL=other" END-IF
               SET W-PTR TO ADDRESS {2} W-ITEM
               SET ADDRESS {3} W-BASED TO W-PTR
               DISPLAY "BASED=" W-BASED
               SET SIZE {4} W-DYN TO 3
               DISPLAY "LEN=" FUNCTION LENGTH (W-DYN)
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── CODE-SET §13.18.13.2 ─────────────────────────────────────────────────────────────────────────────────
    // PDF p414 / folio 384 carries exactly three underlines in the whole format — CODE-SET, ALPHANUMERIC,
    // NATIONAL. FOR and IS are plain. STANDARD-1's correspondence to the native set is the identity over the
    // ISO 646 characters (§12.3.7.4 GR7 c), so the round trip is byte-exact whichever subset is written.
    private const string CodeSetFor = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWK.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           SPECIAL-NAMES.
               ALPHABET AL1 IS STANDARD-1.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F1 ASSIGN TO "opwk.dat"
               ORGANIZATION IS SEQUENTIAL.
           DATA DIVISION.
           FILE SECTION.
           FD F1 CODE-SET {0} ALPHANUMERIC {1} AL1.
           01 R1 PIC X(6).
           WORKING-STORAGE SECTION.
           01 EOF-FLAG PIC X VALUE "N".
           PROCEDURE DIVISION.
           MAIN.
               OPEN OUTPUT F1
               MOVE "HELLO!" TO R1
               WRITE R1
               CLOSE F1
               MOVE SPACES TO R1
               OPEN INPUT F1
               READ F1 AT END MOVE "Y" TO EOF-FLAG END-READ
               CLOSE F1
               DISPLAY "OUT=" R1
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── DYNAMIC LENGTH §13.18.19.2 ───────────────────────────────────────────────────────────────────────────
    // PDF p427 / folio 397 rules DYNAMIC and LIMIT and nothing else; LENGTH and IS are plain. §13.18.19.4 GR1
    // makes the minimum length zero, so LEN0 is 0 in every spelling.
    private const string DynamicLength = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWL.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 W-DYN PIC X DYNAMIC {0} LIMIT {1} 30.
           PROCEDURE DIVISION.
           MAIN.
               DISPLAY "LEN0=" FUNCTION LENGTH (W-DYN)
               MOVE "ABC" TO W-DYN
               DISPLAY "LEN1=" FUNCTION LENGTH (W-DYN)
               DISPLAY "VAL=" W-DYN
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── INITIALIZE §14.9.20.2 ────────────────────────────────────────────────────────────────────────────────
    // PDF p667 / folio 637 rules INITIALIZE, FILLER, ALL, VALUE, REPLACING, BY, DEFAULT and the category names.
    // WITH, both THENs, DATA and BOTH occurrences of TO are plain. The all-omitted spelling of the last slot pair
    // is the bare `INITIALIZE N1 DEFAULT`, which is exactly what `initializeOperandList`'s reservedHere("DEFAULT")
    // guard exists for — this row is its power-set witness.
    private const string InitializeWords = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWM.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 G1.
              05 FILLER PIC X(2) VALUE "ZZ".
              05 G1-A   PIC X(2) VALUE "AB".
           01 N1 PIC 9(2).
           PROCEDURE DIVISION.
           MAIN.
               MOVE "QQQQ" TO G1
               MOVE 77 TO N1
               INITIALIZE G1 {0} FILLER ALL {1} VALUE
               DISPLAY "G1=" G1
               INITIALIZE N1 {2} {3} DEFAULT
               DISPLAY "N1=" N1
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── VALUE §13.18.63.2 format 3 ───────────────────────────────────────────────────────────────────────────
    // PDF p546 / folio 516: the `[ WHEN SET TO FALSE IS literal-4 ]` bracket carries ONE rule, under FALSE.
    // WHEN, SET, TO and both IS/ARE connectives are plain. IN is plain too and is deliberately NOT a slot — with
    // it omitted, alphabet-name-1 is indistinguishable from one more literal-2 (§13.10.3 SR2 lets a constant-name
    // stand where a format specifies a literal), so the operand loop always wins; see the grammar note.
    private const string ValueFalseWords = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWN.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 C1 PIC 9 VALUE 1.
              88 CN1 VALUE {0} 1 {1} {2} {3} FALSE {4} 0.
           PROCEDURE DIVISION.
           MAIN.
               IF CN1 DISPLAY "CN1=yes" ELSE DISPLAY "CN1=no" END-IF
               MOVE 3 TO C1
               IF CN1 DISPLAY "CN3=yes" ELSE DISPLAY "CN3=no" END-IF
               DISPLAY "DONE"
               STOP RUN.
    """;

    // ── kb/Work PB695 family 3 ────────────────────────────────────────────────────────────────────────────────
    // ALTERNATE RECORD KEY (§12.4.5.6.2). PDF p350 / folio 320: ALTERNATE, RECORD, SOURCE, DUPLICATES and
    // SUPPRESS carry underline rectangles (94–96% cover); KEY (box 174.45–193.41), IS, WITH and WHEN (box
    // 303.50–334.12) carry none. The four plain words are the slots. §12.4.5.6.4 GR6 withholds the ALTERNATE
    // access path of a record whose alternate key equals literal-1, so the walk must skip 02/XX in EVERY
    // spelling — a subset that quietly stopped applying the suppression would change the output, not just fail.
    private const string AlternateSuppress = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWO.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F ASSIGN TO "opwo.dat"
                   ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
                   RECORD KEY IS R-KEY
                   ALTERNATE RECORD {0} {1} R-ALT {2} DUPLICATES
                       SUPPRESS {3} "XX"
                   FILE STATUS IS WS-FS.
           DATA DIVISION.
           FILE SECTION.
           FD F.
           01 R.
              05 R-KEY PIC X(2).
              05 R-ALT PIC X(2).
           WORKING-STORAGE SECTION.
           01 WS-FS  PIC X(2).
           01 WS-EOF PIC 9 VALUE 0.
           PROCEDURE DIVISION.
           MAIN.
               OPEN OUTPUT F.
               MOVE "01" TO R-KEY. MOVE "AA" TO R-ALT. WRITE R.
               MOVE "02" TO R-KEY. MOVE "XX" TO R-ALT. WRITE R.
               MOVE "03" TO R-KEY. MOVE "BB" TO R-ALT. WRITE R.
               CLOSE F.
               OPEN INPUT F.
               MOVE LOW-VALUES TO R-ALT.
               START F KEY IS >= R-ALT
                   INVALID KEY CONTINUE
               END-START.
               PERFORM UNTIL WS-EOF = 1
                   READ F NEXT
                       AT END MOVE 1 TO WS-EOF
                       NOT AT END DISPLAY "ALT=" R-KEY R-ALT
                   END-READ
               END-PERFORM.
               CLOSE F.
               DISPLAY "DONE"
               STOP RUN.
    """;

    // LOCK MODE (§12.4.5.9.2). PDF p355 / folio 325: LOCK (90.8%), the second LOCK (90.8%), ON (83.5%),
    // MULTIPLE (95.0%), MANUAL, AUTOMATIC, RECORD and RECORDS carry rules; MODE (box 115.12–144.04),
    // IS (146.38–155.14) and WITH (251.70–278.69) carry none. §12.4.5.9.4 GR3 makes the clause inert for a
    // file opened in the sharing-with-no-other mode, so every spelling must round-trip the record identically.
    private const string LockModeWords = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWP.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F ASSIGN TO "opwp.dat"
                   ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
                   RECORD KEY IS R-KEY
                   LOCK {0} {1} AUTOMATIC {2} LOCK ON RECORD
                   FILE STATUS IS WS-FS.
           DATA DIVISION.
           FILE SECTION.
           FD F.
           01 R.
              05 R-KEY PIC X(2).
              05 R-VAL PIC X(4).
           WORKING-STORAGE SECTION.
           01 WS-FS PIC X(2).
           PROCEDURE DIVISION.
           MAIN.
               OPEN OUTPUT F.
               MOVE "01" TO R-KEY.
               MOVE "ABCD" TO R-VAL.
               WRITE R.
               CLOSE F.
               OPEN I-O F.
               MOVE "01" TO R-KEY.
               READ F
                   INVALID KEY DISPLAY "MISSING"
                   NOT INVALID KEY DISPLAY "VAL=" R-VAL
               END-READ.
               DISPLAY "FS=" WS-FS
               CLOSE F.
               DISPLAY "DONE"
               STOP RUN.
    """;

    // DELETE FILE's exception phrases (§14.9.10.2 Format 2), written NEGATIVE-FIRST. PDF p635 / folio 605:
    // EXCEPTION carries a rule in both phrases (95.5%) and NOT one (89.5%); NEITHER occurrence of ON has any.
    // The file is never created, so §14.9.10.4 GR14 ("not present … successful … '05'") makes the NOT phrase
    // the one that runs in every spelling.
    private const string DeleteFileNotOn = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWQ.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT F ASSIGN TO "opwq-absent.dat"
                   ORGANIZATION IS SEQUENTIAL
                   FILE STATUS IS WS-FS.
           DATA DIVISION.
           FILE SECTION.
           FD F.
           01 R PIC X(4).
           WORKING-STORAGE SECTION.
           01 WS-FS PIC X(2).
           PROCEDURE DIVISION.
           MAIN.
               DELETE FILE F
                   NOT {0} EXCEPTION DISPLAY "NOEXC"
                   {1} EXCEPTION DISPLAY "EXC"
               END-DELETE.
               DISPLAY "FS=" WS-FS
               DISPLAY "DONE"
               STOP RUN.
    """;

    // INVOKE's argument phrases (§14.9.23.2). PDF p681 / folio 651: REFERENCE (96.3%), CONTENT (95.4%) and
    // VALUE (92.9%) carry rules; none of the three occurrences of BY does. §14.2.3 GR8 makes the BY REFERENCE
    // store visible in the caller and §14.8.2.3.3 keeps the BY CONTENT copy private, so the two-line output is
    // the observable that a mis-selected passing mode would change.
    private const string InvokeByWords = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWR.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               CLASS OPWRC.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 O USAGE OBJECT REFERENCE OPWRC.
           01 A PIC X(2) VALUE "11".
           01 B PIC X(2) VALUE "22".
           PROCEDURE DIVISION.
           MAIN.
               INVOKE OPWRC "NEW" RETURNING O
               INVOKE O "MIX" USING {0} REFERENCE A {1} CONTENT B
               DISPLAY "REF=" A
               DISPLAY "CON=" B
               DISPLAY "DONE"
               STOP RUN.
           END PROGRAM OPWR.

           IDENTIFICATION DIVISION.
           CLASS-ID. OPWRC INHERITS FROM BASE.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               CLASS BASE.
           IDENTIFICATION DIVISION.
           OBJECT.
           PROCEDURE DIVISION.

           METHOD-ID. MIX.
           DATA DIVISION.
           LINKAGE SECTION.
           01 LREF PIC X(2).
           01 LCON PIC X(2).
           PROCEDURE DIVISION USING LREF LCON.
           M.
               MOVE "AA" TO LREF
               MOVE "BB" TO LCON
               EXIT METHOD.
           END METHOD MIX.

           END OBJECT.
           END CLASS OPWRC.
    """;

    // INHERITS (§11.3.2). PDF p294 / folio 264: INHERITS carries a rule (95.0%) and FROM's box 140.01–168.09
    // carries none. ⛔ FROM is REQUIRED INSIDE the optional `[ INHERITS FROM … ]` group, so
    // audit_grammar_optional_words.py structurally cannot report it (kb/Work PB715) — this row is the only
    // mechanical guard the omission has. §11.3.4 GR1 gives the subclass its superclass's methods, so the
    // inherited MIX must answer in both spellings.
    private const string InheritsFromWord = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWS.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               CLASS OPWSD.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 O USAGE OBJECT REFERENCE OPWSD.
           01 A PIC X(2) VALUE "11".
           PROCEDURE DIVISION.
           MAIN.
               INVOKE OPWSD "NEW" RETURNING O
               INVOKE O "MIX" USING A
               DISPLAY "REF=" A
               DISPLAY "DONE"
               STOP RUN.
           END PROGRAM OPWS.

           IDENTIFICATION DIVISION.
           CLASS-ID. OPWSB INHERITS FROM BASE.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               CLASS BASE.
           IDENTIFICATION DIVISION.
           OBJECT.
           PROCEDURE DIVISION.

           METHOD-ID. MIX.
           DATA DIVISION.
           LINKAGE SECTION.
           01 LREF PIC X(2).
           PROCEDURE DIVISION USING LREF.
           M.
               MOVE "AA" TO LREF
               EXIT METHOD.
           END METHOD MIX.

           END OBJECT.
           END CLASS OPWSB.

           IDENTIFICATION DIVISION.
           CLASS-ID. OPWSD INHERITS {0} OPWSB.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               CLASS OPWSB.
           IDENTIFICATION DIVISION.
           OBJECT.
           PROCEDURE DIVISION.
           END OBJECT.
           END CLASS OPWSD.
    """;
    // ── SORT §14.9.40.2, the DUPLICATES phrase: [ WITH DUPLICATES IN ORDER ] ─────────────────────────────────
    // MEASURED on the printed page (PDF pages 774 and 775 — Format 1 and Format 2 print the same line): of the
    // four words only DUPLICATES carries an underline rule, so WITH, IN and ORDER are all optional (§5.2.3) and
    // all eight subsets are conforming spellings of the SAME phrase.
    // ⛔ WRITTEN TWICE, AT 85 AND AT 2023, AND THE SECOND ROW IS THE REGRESSION (kb/Work PB704). §8.9 reserves
    // ORDER from 2002, and while the word rode `cobolWord` the §8.9 funnel — which screens every IDENTIFIER
    // occurrence POSITION-BLIND — read the format's own word as a user-defined one: the four spellings that
    // WRITE ORDER were COBOLNET0901 at 2002/2014/2023 and clean at 85. A single-edition row would have passed
    // the whole time (feedback_edition_gate_sweep).
    // The observable is §14.9.40.4 GR3 b): among records equal on every key the order of return is the order in
    // which the input procedure RELEASED them, so the output pins the phrase's ONE semantic effect and not
    // merely that it parsed.
    private const string SortDuplicates = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWT.
           ENVIRONMENT DIVISION.
           INPUT-OUTPUT SECTION.
           FILE-CONTROL.
               SELECT S-FILE ASSIGN TO "opwt.tmp".
           DATA DIVISION.
           FILE SECTION.
           SD  S-FILE.
           01  S-REC.
               05  S-KEY PIC X.
               05  S-TAG PIC X.
           WORKING-STORAGE SECTION.
           01  W-I   PIC 9 VALUE 0.
           01  W-EOF PIC X VALUE "N".
           01  W-SRC.
               05  FILLER PIC XX VALUE "B1".
               05  FILLER PIC XX VALUE "A1".
               05  FILLER PIC XX VALUE "B2".
               05  FILLER PIC XX VALUE "A2".
           01  W-SRC-R REDEFINES W-SRC.
               05  W-PAIR PIC XX OCCURS 4 TIMES.
           PROCEDURE DIVISION.
           MAIN-SECT SECTION.
           MAIN.
               SORT S-FILE ON ASCENDING KEY S-KEY
                   {0} DUPLICATES {1} {2}
                   INPUT PROCEDURE IS IN-PROC
                   OUTPUT PROCEDURE IS OUT-PROC.
               DISPLAY "DONE".
               STOP RUN.
           IN-PROC SECTION.
           IN-P.
               PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 4
                   MOVE W-PAIR (W-I) TO S-REC
                   RELEASE S-REC
               END-PERFORM.
           OUT-PROC SECTION.
           OUT-P.
               PERFORM UNTIL W-EOF = "Y"
                   RETURN S-FILE AT END MOVE "Y" TO W-EOF
                       NOT AT END DISPLAY "OUT=" S-REC
                   END-RETURN
               END-PERFORM.
    """;

    // The 2023 twin of the row above. DERIVED, not copied: one template, two editions, so the two rows can never
    // drift apart — and a DISTINCT PROGRAM-ID stem, because both rows run in ONE test process and a same-named
    // assembly is served from the load context that already holds it (the per-spelling rule, one level up).
    private static readonly string SortDuplicates2023 =
        SortDuplicates.Replace("OPWT", "OPWU", StringComparison.Ordinal)
                      .Replace("opwt.tmp", "opwu.tmp", StringComparison.Ordinal);

    // ── USAGE §13.18.60.2, the three pointer usages: POINTER [ TO type-name-1 ], FUNCTION-POINTER TO
    // function-prototype-name-1, PROGRAM-POINTER [ TO program-prototype-name-1 ] (kb/Work PB848). RENDERED — PDF
    // p533 / folio 503: the underline rule sits under POINTER / FUNCTION-POINTER / PROGRAM-POINTER, never under TO.
    // ⛔ TO IS REQUIRED-INSIDE-AN-OPTIONAL-GROUP in the grammar's old `(TO cobolWord)?`, the shape
    // audit_grammar_optional_words.py cannot see (kb/Work PB715), so this row is the mechanical guard. Each TO slot
    // sits on the TO-FULL twin of a TO-less entry, and §14.9.39.3 SR19/SR20/SR22 admit the copy between the twins
    // only because both name the same type / prototype — a TO-less operand that bound as nothing would fail to
    // compile rather than print something else.
    private const string PointerUsageTo = """
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWVP IS PROTOTYPE.
           DATA DIVISION.
           LINKAGE SECTION.
           01 L-X PIC 9(4).
           PROCEDURE DIVISION USING L-X.
           END PROGRAM OPWVP.
           IDENTIFICATION DIVISION.
           FUNCTION-ID. OPWVF.
           DATA DIVISION.
           LINKAGE SECTION.
           01 L-ARG PIC S9(4).
           01 L-RES PIC S9(9).
           PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
               COMPUTE L-RES = L-ARG * 2
               GOBACK.
           END FUNCTION OPWVF.
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWV.
           ENVIRONMENT DIVISION.
           CONFIGURATION SECTION.
           REPOSITORY.
               FUNCTION OPWVF
               PROGRAM OPWVP.
           DATA DIVISION.
           WORKING-STORAGE SECTION.
           01 REC-T IS TYPEDEF STRONG.
              05 A PIC X(2).
           01 DPT1 IS TYPEDEF USAGE POINTER REC-T.
           01 DPT2 IS TYPEDEF USAGE POINTER {0} REC-T.
           01 PPT1 IS TYPEDEF USAGE PROGRAM-POINTER OPWVP.
           01 PPT2 IS TYPEDEF USAGE PROGRAM-POINTER {1} OPWVP.
           01 DP1 TYPE DPT1.
           01 DP2 TYPE DPT2.
           01 FP1 USAGE FUNCTION-POINTER OPWVF.
           01 FP2 USAGE FUNCTION-POINTER {2} OPWVF.
           01 PP1 TYPE PPT1.
           01 PP2 TYPE PPT2.
           01 W TYPE REC-T.
           01 WS-N PIC 9(4) VALUE 0042.
           PROCEDURE DIVISION.
           MAIN.
               SET DP1 TO ADDRESS OF W
               SET DP2 TO DP1
               IF DP2 = DP1 DISPLAY "DP-SAME" END-IF
               SET FP1 TO ADDRESS OF FUNCTION OPWVF
               SET FP2 TO FP1
               IF FP2 = FP1 DISPLAY "FP-SAME" END-IF
               SET PP1 TO ENTRY "OPWVP"
               SET PP2 TO PP1
               IF PP2 = PP1 DISPLAY "PP-SAME" END-IF
               CALL PP2 USING WS-N
               DISPLAY "DONE"
               STOP RUN.
           END PROGRAM OPWV.
           IDENTIFICATION DIVISION.
           PROGRAM-ID. OPWVP.
           DATA DIVISION.
           LINKAGE SECTION.
           01 L-X PIC 9(4).
           PROCEDURE DIVISION USING L-X.
               DISPLAY "CALLED " L-X
               GOBACK.
           END PROGRAM OPWVP.
    """;

    // ── USAGE OBJECT REFERENCE §13.18.60.2: [ FACTORY OF ] ACTIVE-CLASS / [ FACTORY OF ] object-class-name-1 —
    // FACTORY is underlined on folio 503 and OF is not (kb/Work PB848's sibling sweep; the grammar required OF).
    // DERIVED from the PB389 golden, which writes BOTH forms (a factory-of-class item in the program, a
    // factory-of-ACTIVE-CLASS item in a factory method) and prints what each one invokes — so the template can
    // never drift from a program whose output is already pinned.
    private static readonly string ObjectReferenceFactoryOf =
        File.ReadAllText(Path.Combine(ConformanceCorpus.Root, "2002", "pb389_object_reference_descriptor.cob"))
            .Replace("FACTORY OF PB389B", "FACTORY {0} PB389B", StringComparison.Ordinal)
            .Replace("FACTORY OF ACTIVE-CLASS", "FACTORY {1} ACTIVE-CLASS", StringComparison.Ordinal)
            .Replace("PROGRAM-ID. PB389M.", "PROGRAM-ID. OPWX.", StringComparison.Ordinal)
            .Replace("END PROGRAM PB389M.", "END PROGRAM OPWX.", StringComparison.Ordinal);

    private static readonly FormatCase[] Formats =
    [
        new("use-format-1", "14.9.49.2 Format 1", 85, ["AFTER", "STANDARD", "PROCEDURE", "ON"], "OPWA", UseFormat1),
        new("use-format-3", "14.9.49.2 Format 3", 2023, ["AFTER"], "OPWB", UseFormat3),
        new("use-format-3-ec", "14.9.49.2 Format 3 (EC)", 2023, ["AFTER"], "OPWC", UseFormat3Ec),
        new("start-with-length", "14.9.41.2", 2002, ["WITH"], "OPWD", StartWithLength),
        new("special-names-85", "12.3.7.2 (COBOL-85 clauses)", 85,
            ["IS", "SIGN", "IS", "CHARACTERS", "IS", "IS", "IS"], "OPWE", SpecialNames85),
        new("special-names-for", "12.3.7.2 (FOR / WITH)", 2023, ["FOR", "FOR", "FOR", "WITH"], "OPWF",
            SpecialNamesFor),
        new("special-names-switch", "12.3.7.2 (switch entry)", 85, ["IS", "STATUS", "IS", "STATUS", "IS"], "OPWG",
            SpecialNamesSwitch),
        new("object-computer-for", "12.3.6.2 (FOR arm)", 2023, ["PROGRAM", "FOR", "IS"], "OPWH", ObjectComputer),
        new("object-computer-head", "12.3.6.2 (IS arm)", 85, ["PROGRAM", "COLLATING", "IS"], "OPWI",
            ObjectComputerHead),
        new("options-initialize-and-set", "11.9.10.2 + 14.9.39.2 Formats 7/16", 2023,
            ["SECTION", "TO", "OF", "OF", "OF"], "OPWJ", OptionsAndSet),
        // kb/Work PB695 family 2 — the four CobolData.g4 formats.
        new("code-set-for", "13.18.13.2", 2002, ["FOR", "IS"], "OPWK", CodeSetFor),
        new("dynamic-length", "13.18.19.2", 2014, ["LENGTH", "IS"], "OPWL", DynamicLength),
        new("initialize-value-and-default", "14.9.20.2", 2002, ["WITH", "TO", "THEN", "TO"], "OPWM",
            InitializeWords),
        new("value-when-set-to-false", "13.18.63.2 Format 3", 2002, ["IS", "WHEN", "SET", "TO", "IS"], "OPWN",
            ValueFalseWords),
        // kb/Work PB695 family 3 — the last eight audit candidates plus the INHERITS sibling the audit is
        // structurally blind to. The three DECLINED constructs family 3 also relaxed (APPLY COMMIT's ON,
        // VALIDATE-STATUS's WHEN, ERASE's END and OF) cannot appear here — a subset row must COMPILE AND RUN,
        // and those refuse by name — so they are pinned by the negative corpus instead
        // (pb695-apply-commit-no-on, pb695-validate-status-no-when, pb695-erase-no-end-of).
        new("alternate-suppress-when", "12.4.5.6.2", 2023, ["KEY", "IS", "WITH", "WHEN"], "OPWO",
            AlternateSuppress),
        new("lock-mode-words", "12.4.5.9.2", 2002, ["MODE", "IS", "WITH"], "OPWP", LockModeWords),
        new("delete-file-not-on-exception", "14.9.10.2 Format 2", 2023, ["ON", "ON"], "OPWQ", DeleteFileNotOn),
        new("invoke-by-words", "14.9.23.2", 2002, ["BY", "BY"], "OPWR", InvokeByWords),
        new("inherits-from-word", "11.3.2", 2002, ["FROM"], "OPWS", InheritsFromWord),
        new("sort-duplicates-85", "14.9.40.2 (DUPLICATES phrase)", 85, ["WITH", "IN", "ORDER"], "OPWT",
            SortDuplicates),
        new("sort-duplicates-2023", "14.9.40.2 (DUPLICATES phrase, ORDER reserved)", 2023,
            ["WITH", "IN", "ORDER"], "OPWU", SortDuplicates2023),
        // kb/Work PB848 — TO in the three pointer usages, and OF in OBJECT REFERENCE's FACTORY phrase.
        new("pointer-usage-to", "13.18.60.2 (pointer usages)", 2014, ["TO", "TO", "TO"], "OPWV", PointerUsageTo),
        new("object-reference-factory-of", "13.18.60.2 (OBJECT REFERENCE FACTORY OF)", 2002, ["OF", "OF"], "OPWX",
            ObjectReferenceFactoryOf),
    ];

    public static IEnumerable<object[]> Cases() => Formats.Select(f => new object[] { f.Name });

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryOptionalWordSubset_CompilesAndRunsIdentically(string name)
    {
        FormatCase f = Formats.Single(c => c.Name == name);
        int slots = f.OptionalWords.Length;
        string? baseline = null;
        string baselineSpelling = "";

        for (int mask = 0; mask < (1 << slots); mask++)
        {
            // Slot i is written when its bit is set; the all-bits-set spelling is the fully-written form.
            object[] words = [.. Enumerable.Range(0, slots).Select(i => (mask & (1 << i)) != 0 ? f.OptionalWords[i] : "")];
            string spelling = string.Join(" ", words.Cast<string>().Where(w => w.Length > 0));
            if (spelling.Length == 0) spelling = "none written";
            // ⛔ A UNIQUE PROGRAM-ID AND ASSEMBLY NAME PER SPELLING. Every subset compiles inside ONE test
            // process, and a same-named assembly is served from the load context that already holds it — the
            // run would then compare a spelling against ITSELF and pass whatever the grammar did.
            string unit = f.ProgramId + mask.ToString("00");
            string source = string.Format(f.Template, words).Replace(f.ProgramId + ".", unit + ".", StringComparison.Ordinal);
            Assert.Contains("PROGRAM-ID. " + unit + ".", source, StringComparison.Ordinal);

            string dir = CutRunner.NewTempDir("optword");
            try
            {
                string src = Path.Combine(dir, unit + ".cob");
                src = CompiledProgramCache.StageSource(src, source);
                string dll = Path.Combine(dir, unit + ".dll");
                var r = CompiledProgramCache.Compile(new CompilerDriver.Options(src, dll, DialectLevel: f.Edition, SourceFormat: InitialReferenceFormat.Auto));
                Assert.True(r.Success,
                    $"ISO §{f.Clause}: the optional words [{spelling}] must compile at --std {f.Edition} " +
                    $"(§5.2.3 — they are printed WITHOUT an underline): {string.Join("\n", r.Errors)}");

                var (ran, stdout, detail) = CutRunner.Run(dll, dir);
                Assert.True(ran, $"[{f.Name}] with [{spelling}] must run: {detail}");
                string normalized = CutRunner.Normalize(stdout);
                if (baseline is null)
                {
                    baseline = normalized;
                    baselineSpelling = spelling;
                    continue;
                }
                Assert.True(baseline == normalized,
                    $"ISO §8.3.2.4.3: an optional word is specified \"with no effect on the semantics of the " +
                    $"format\", but §{f.Clause} written [{spelling}] differs from [{baselineSpelling}].\n" +
                    $"  [{baselineSpelling}] -> {baseline}\n  [{spelling}] -> {normalized}");
            }
            finally { CutRunner.TryDelete(dir); }
        }

        Assert.NotNull(baseline);
    }
}
