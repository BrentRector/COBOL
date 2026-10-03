      *> kb/Work PB1306 - the VARYING clause's counter is a data item of its ENTRY, in scope for the entry and every
      *> subordinate entry (ISO 13.18.64), at the rule's INTRODUCING edition (the report writer VARYING clause, 2002).
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.64.4 GR1: "Each entry containing a VARYING clause establishes an independent temporary integer data
      *> item that shall be large enough to contain the maximum expected value." - the ENTRY, not only a printable
      *> leaf: a group entry's counter exists.
      *> 13.18.64.3 SR2: "This definition of data-name-1 may be referenced only within the current entry or a
      *> subordinate entry." and "Data-name-1 shall not be defined elsewhere in the source element, except as
      *> data-name-1 in another VARYING clause of an entry not subordinate to the subject of the current entry."
      *> NOTE "Such a reuse refers to a completely independent data item."
      *> 13.18.64.3 SR3: "...may be referenced in arithmetic-expression-2 of the same VARYING clause or in
      *> arithmetic-expression-1 or arithmetic-expression-2 of a VARYING clause in a subordinate entry."
      *> 13.18.64.4 GR3: "a) For the first occurrence, the value of arithmetic-expression-1 is moved to data-name-1.
      *> If the FROM phrase is absent, 1 is moved to data-name-1. b) For the second and subsequent occurrences, the
      *> value of arithmetic-expression-2 is added to data-name-1. If the BY phrase is absent, 1 is added."
      *> 13.18.64.4 GR4 NOTE: the counter may be used "as a source data item".
      *> 13.18.38.4 GR12: STEP is the horizontal interval between successive occurrences of the repeated item.
      *>
      *> DERIVATION (columns come from the STEP phrases, values from GR3):
      *>   LINE 1 - a GROUP entry's counter (03 OCCURS 3 TIMES STEP 5 VARYING K, 04 COLUMN 1 SOURCE K): K is 1, 2, 3
      *>            for the three occurrences of the 03 entry, printed at columns 1, 6, 11 => `1    2    3`.
      *>   LINE 2 - a SUBORDINATE entry names the counter in its FROM (SR3): J is 1, 2 (FROM 1, default BY 1); in
      *>            each occurrence of the 03 entry the 04 entry's own counter L starts again at FROM J * 10 + 1
      *>            (GR3 a: its first occurrence), so 11, 12 then 21, 22. The 03 entries are 10 columns apart and
      *>            the 04 occurrences 3 apart => 11 at column 1, 12 at 4, 21 at 11, 22 at 14.
      *>   LINE 3 - BY names its own counter (SR3): M = 1; then 1 + 1 = 2; then 2 + 2 = 4 => `1  2  4`.
      *>   LINES 4, 5 - the name N reused by two entries that are not subordinate to one another is two independent
      *>            counters (SR2 NOTE): FROM 1 gives 1, 2, 3 and FROM 5 BY 2 gives 5, 7, 9.
      *> Fails on a binder that keeps the counter only on the printable leaf (LINE 1 is COBOLNET0899 K does not
      *> resolve'), that cannot see an ancestor's counter (LINE 2, COBOLNET1639), that stages BY-names-K loud
      *> (LINE 3), or that counts L across the enclosing occurrences instead of restarting it (LINE 2).
      *>
      *> THE READ-BACK IS BYTE-WISE ON PURPOSE (see pb565_report_repeating_entry): LINE SEQUENTIAL is a COBOL-2023
      *> introduction, so a one-character record on a second SELECT over the same file reassembles each line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1306VS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1306vs.txt".
           SELECT CHK ASSIGN TO "pb1306vs.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VS.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-VS PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  OCCURS 3 TIMES STEP 5 VARYING K FROM 1 BY 1.
                   04  COLUMN 1 PIC 9 SOURCE K.
           02  LINE PLUS 1.
               03  OCCURS 2 TIMES STEP 10 VARYING J FROM 1.
                   04  COLUMN 1 PIC 99 OCCURS 2 TIMES STEP 3
                       VARYING L FROM J * 10 + 1 SOURCE L.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING M FROM 1 BY M SOURCE M.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING N FROM 1 SOURCE N.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 3
                   VARYING N FROM 5 BY 2 SOURCE N.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VS.
           GENERATE DET-A.
           TERMINATE R-VS.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
