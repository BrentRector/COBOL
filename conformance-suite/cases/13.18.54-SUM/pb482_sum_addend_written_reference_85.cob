      *>--- conformance-case
      *> id: pb482_sum_addend_written_reference_85
      *> rules: [SR-13.18.54.3-5, SR-13.18.54.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.54.3 SR1: "Each data-name-1, identifier-1 or arithmetic-expression-1 is an addend. The whole
      *> clause is referred to as a SUM clause even though the SUM keyword may appear more than once."
      *> ISO 13.18.54.3 SR5: "If the addend is identifier-1, it shall specify a numeric data item not defined in
      *> the report section."  An IDENTIFIER is 8.4.3.1.2 Format 2, qualified-data-name-with-subscripts, so a
      *> SUBSCRIPT is part of the addend's written reference - and a subscript may be an integer literal, an
      *> index-name or an arithmetic expression (8.4.2.3), none of which has a value in the data division.
      *> ISO 13.18.54.4 GR1: "Each entry containing a SUM clause establishes an independent sum counter" - ONE
      *> counter per ENTRY, however many times the SUM keyword appears.
      *> ISO 13.18.54.4 GR7 c): with no UPON phrase the addend is added "whenever any GENERATE statement is
      *> executed for the current report" (c 1); with an UPON phrase, "whenever any GENERATE statement is
      *> executed for a detail referenced by the UPON phrase" (c 2) - so the phrase belongs to ITS SUM group.
      *> ISO 13.18.54.4 GR9: "If the SUM clause specifies more than one addend, the result is the same as when
      *> all the addends were summed separately according to the above rules and the results added together."
      *> ISO 8.3.2.4.3: "uppercase words that are not underlined are called optional words and may be specified
      *> at the user's option with no effect on the semantics of the format" - and the printed general format
      *> (13.18.54.2, PDF p487) underlines SUM, UPON, RESET and FINAL and leaves OF plain, so SUM OF is legal.
      *>
      *> THE POINT OF THIS PROGRAM.  Every addend below is a SUBSCRIPTED reference into one WORKING-STORAGE
      *> table, written five ways, and one entry writes the SUM keyword TWICE with a different UPON list each
      *> time.  Before kb/Work PB482 the binder captured an addend with the FILE STATUS key helper - base word
      *> plus IN/OF qualifiers, subscript dropped on the floor - so every column here either ABORTED the process
      *> at the first GENERATE ("SUM addend not resolvable to storage") or, for the two-SUM entry, silently
      *> discarded all but the last SUM group.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  WS-CELL holds 11, 22, 33; IX is SET TO 2 and WS-K is 1, so all
      *> three of WS-CELL(2), WS-CELL(IX) and WS-CELL(WS-K + 1) are the SAME item, value 22.  Three GENERATE
      *> statements are executed - DET-A, DET-B, DET-A - and none of columns 1, 5 and 9 carries an UPON phrase,
      *> so GR7 c) 1) adds on each of the three: 22 * 3 = 066 in each.  Column 13 writes two SUM groups:
      *> WS-CELL(1) = 11 UPON DET-A fires on the two GENERATE DET-A statements (22) and WS-CELL(3) = 33 UPON
      *> DET-B fires on the one GENERATE DET-B (33), and GR1 puts both into the ONE counter of that entry:
      *> 22 + 33 = 055.  Column 17 has three addends in one group, GR9: (11 + 22 + 33) * 3 = 198.  No RESET
      *> phrase is written, so GR2 resets each counter at the end of the group it is printed in - the CONTROL
      *> FOOTING FINAL, produced once by TERMINATE (14.9.46.4 GR2) - and the printed values are the totals over
      *> the whole report.  The detail lines carry no digits, so the report file's digits, in file order, are
      *> exactly the footing line: 066 066 066 055 198.
      *>
      *> HOW IT IS WITNESSED AT COBOL-85.  ORGANIZATION IS LINE SEQUENTIAL is a COBOL-2023 introduction and a
      *> sum counter's own data-name is not yet referable from the procedure division, so the report file is
      *> read back one character per record - the pb523_linage_margins_on_the_medium_85 precedent - and every
      *> character that passes the 8.8.4.4 NUMERIC class test is appended to a buffer.  Expected:
      *> DIGITS=066066066055198     and NDIG=15.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB482SUM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb482sum.rpt".
           SELECT RDR ASSIGN TO "pb482sum.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD PRT REPORT IS R-1.
       FD RDR.
       01 R-CHAR PIC X.
       WORKING-STORAGE SECTION.
       01 WS-TAB.
          05 WS-CELL PIC 99 OCCURS 3 TIMES INDEXED BY IX.
       01 WS-K      PIC 9    VALUE 1.
       01 EOF-SW    PIC 9    VALUE 0.
       01 NDIG      PIC 99   VALUE 0.
       01 DIGIT-BUF PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 25.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "A=".
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "B=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 COLUMN 1  PIC 999 SUM WS-CELL(2).
          02 COLUMN 5  PIC 999 SUM WS-CELL(IX).
          02 COLUMN 9  PIC 999 SUM WS-CELL(WS-K + 1).
          02 COLUMN 13 PIC 999 SUM OF WS-CELL(1) UPON DET-A
                               SUM OF WS-CELL(3) UPON DET-B.
          02 COLUMN 17 PIC 999 SUM WS-CELL(1) WS-CELL(2) WS-CELL(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 11 TO WS-CELL(1).
           MOVE 22 TO WS-CELL(2).
           MOVE 33 TO WS-CELL(3).
           SET IX TO 2.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DET-A.
           GENERATE DET-B.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT RDR.
           PERFORM UNTIL EOF-SW = 1
               READ RDR
                   AT END MOVE 1 TO EOF-SW
                   NOT AT END
                       IF R-CHAR IS NUMERIC
                           ADD 1 TO NDIG
                           MOVE R-CHAR TO DIGIT-BUF(NDIG:1)
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RDR.
           DISPLAY "DIGITS=" DIGIT-BUF.
           DISPLAY "NDIG=" NDIG.
           STOP RUN.
