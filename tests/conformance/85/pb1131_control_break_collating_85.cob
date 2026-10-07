      *> kb/Work PB1131 - a CONTROL break is sensed by the program's own
      *> comparison: under a PROGRAM COLLATING SEQUENCE whose alphabet
      *> ranks two characters equal (ALSO), a change between them is
      *> NOT a control break.
      *>
      *> "Subsequent executions of any GENERATE statement for that report
      *> automatically test the current value of each control data item,
      *> in order major to minor, for equality with the corresponding
      *> prior control."
      *>   cite.py: OK  §13.18.16.4 3)  (General rules)
      *> "The alphanumeric program collating sequence and national program
      *> collating sequence are used to determine the truth value of any
      *> alphanumeric comparisons and national comparisons, respectively,
      *> that are: ... c) Implicitly specified by the presence of a
      *> CONTROL clause in a report description entry."
      *>   cite.py: OK  §12.3.6.4 11) c)  (General rules)
      *>
      *> DERIVATION. Alphabet SEQ-AB gives "A" and "B" ONE ordinal
      *> position (ALSO), so the relation WS-K = "B" is true when WS-K
      *> holds "A" (the IF leg below shows it). The control item takes
      *> the values A, B, C, C across four GENERATEs:
      *>   GENERATE 1 (A) saves the prior;           -> "DE A"
      *>   GENERATE 2 (B) compares EQUAL: no break;  -> "DE B"
      *>   GENERATE 3 (C) differs: CF with the prior value, the running
      *>     count of the ended group (2), then the detail;
      *>                                   -> "CF A 2", "DE C"
      *>   GENERATE 4 (C) no break;                  -> "DE C"
      *>   TERMINATE: the final control footing.     -> "CF C 2"
      *> The CF's COLUMN 4 SOURCE WS-K prints the prior control (GR4 a)).
      *> Fails if the break is sensed by a code-unit compare of the two
      *> images (the defect: an extra "CF A 1" after the first detail).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1131C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS SEQ-AB.
       SPECIAL-NAMES.
           ALPHABET SEQ-AB IS "A" ALSO "B", "C", "D"
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D SYM-X0C
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1131c.txt".
           SELECT CHK ASSIGN TO "pb1131c.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC X     VALUE "A".
       01  WS-CNT  PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS WS-K.
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "DE".
           02  COLUMN 4 PIC X SOURCE WS-K.
       01  TYPE CF WS-K LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "CF".
           02  COLUMN 4 PIC X SOURCE WS-K.
           02  COLUMN 6 PIC 9 SUM WS-CNT.
       PROCEDURE DIVISION.
       MAIN-P.
           IF WS-K = "B"
               DISPLAY "A = B UNDER SEQ-AB"
           ELSE
               DISPLAY "A NOT = B"
           END-IF.
           MOVE 1 TO WS-CNT.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           MOVE "B" TO WS-K.
           GENERATE DE-1.
           MOVE "C" TO WS-K.
           GENERATE DE-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D AND CHK-REC NOT = SYM-X0C
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           DISPLAY "[" WS-LINE(1:6) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
