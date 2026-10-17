*>--- conformance-case
*> id: pb885_report_depending_qualified
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> A REPORT-WRITER OCCURS ... DEPENDING ON OPERAND IS A QUALIFIED-DATA-NAME, AND IT IS BOUND AS WRITTEN
      *> (kb/Work PB885). ISO/IEC 1989:2023 §13.18.38.2 Format 3 prints DEPENDING ON data-name-1, and
      *> §13.18.38.3 SR2 (all formats): "Data-name-1 and data-name-2 shall not be subscripted." A data-name
      *> may be QUALIFIED — §8.4.2.2.1: "Identical user-defined names may be specified in a source unit;
      *> however, uniqueness shall be established through qualification for each user-defined name
      *> explicitly referenced". Before PB885 the operand was captured by a helper that kept the name and
      *> its qualifiers and silently DROPPED a written subscript; the capture is now the ONE data-name-n
      *> screen (a subscript is refused - negative pb885-report-depending-subscripted), and the
      *> qualified operand resolves through the ONE clause-operand resolver, which counts survivors.
      *>
      *> DERIVATION. CNT OF G1 = 2 and CNT OF G2 = 3, so only the qualifier decides which is data-name-1.
      *> §13.18.38.4 GR13: "If the value of data-name-1 is in the range integer-1 to (integer-2 - 1), the
      *> OCCURS clause has the same effect as an OCCURS clause with no TO or DEPENDING phrases and with an
      *> integer-2 equal to the current value of data-name-1." OCCURS 1 TO 3: 2 is in 1..2, so TWO
      *> repetitions. GR12a: each successive occurrence is printed integer-3 = 2 columns to the right, so
      *> "A" at columns 1 and 3 => [A A]. Had CNT OF G2 (3, outside 1..2) been bound, GR13's first leg
      *> would give all three => [A A A].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885RDQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb885rdq.txt".
           SELECT CHK ASSIGN TO "pb885rdq.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-DQ.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  G1.
           05  CNT PIC 9 VALUE 2.
       01  G2.
           05  CNT PIC 9 VALUE 3.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-DQ PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X OCCURS 1 TO 3 TIMES
                   DEPENDING ON CNT OF G1 STEP 2 VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-DQ.
           GENERATE DET-A.
           TERMINATE R-DQ.
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
