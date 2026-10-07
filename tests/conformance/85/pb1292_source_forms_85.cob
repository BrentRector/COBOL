      *> kb/Work PB1292 - the SOURCE clause's identifier-1 in the forms the '85 report writer already has (ISO
      *> 13.18.53), at the rule's INTRODUCING edition: a SUBSCRIPTED identifier, a REFERENCE-MODIFIED identifier and
      *> a SUM COUNTER of the current report. (The forms that need a 2002 construct are
      *> pb1292_source_operand_forms.)
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.53.2: the SOURCE operand is `{ identifier-1 | arithmetic-expression-1 }`; an identifier is a
      *> qualified-data-name with subscripts and a reference modification (8.4.3.1.2).
      *> 13.18.53.4 GR1: "If identifier-1 is specified without the ROUNDED phrase, it specifies the sending operand of
      *> an implicit MOVE statement in which the data item referenced by identifier-1 is moved to the printable item."
      *> 13.18.53.3 SR4: "If identifier-1 specifies a report section item, it shall be a report counter identifier
      *> or a sum counter defined in the current report."
      *> 13.18.54.4 GR5: the data-name of an entry containing a SUM clause "is the name of the sum counter".
      *> 13.18.54.4 GR2: the counter "is reset to zero ... at the end of the processing of the report group in which
      *> it is printed".
      *>
      *> DERIVATION (T(1..3) = T1 T2 T3, WS-X = WXYZ, WS-SUB = 3):
      *>   forms line  - WS-T(2) = T2 at column 1; WS-X(2:2) = XY at column 4; WS-T(WS-SUB - 1) = WS-T(2) = T2 at
      *>                 column 7  =>  `T2 XY T2`.
      *>   sum line    - RUNTOT sums WS-AMT; SOURCE RUNTOT reads the sum counter of the current report, which has
      *>                 received this GENERATE's WS-AMT before the group's lines are printed (GR7 b/c): 5 then 7.
      *>                 => `0005 0005` then `0007 0007` (the counter resets after each detail group, GR2).
      *>   control footing FINAL - the counter was reset after the last detail, so SOURCE RUNTOT reads 0000.
      *> Fails (COBOLNET0899 'a subscripted or reference-modified SOURCE operand ... is not yet implemented',
      *> 'SOURCE RUNTOT does not resolve to a data item') on a binder whose identifier arm looks a NAME up in ordinary
      *> storage.
      *>
      *> THE READ-BACK IS BYTE-WISE: a one-character record on a second SELECT over the report file is legal at every
      *> edition (see pb565_report_repeating_entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1292F85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1292f85.txt".
           SELECT CHK ASSIGN TO "pb1292f85.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SF.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-TAB.
           05  WS-T PIC X(2) OCCURS 3.
       01  WS-X   PIC X(4) VALUE "WXYZ".
       01  WS-SUB PIC 9 VALUE 3.
       01  WS-AMT PIC 9(4) VALUE 0.
       REPORT SECTION.
       RD  R-SF CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET-A TYPE DETAIL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(2) SOURCE WS-T(2).
               03  COLUMN 4 PIC X(2) SOURCE WS-X(2:2).
               03  COLUMN 7 PIC X(2) SOURCE WS-T(WS-SUB - 1).
           02  LINE PLUS 1.
               03  RUNTOT COLUMN 1 PIC 9(4) SUM WS-AMT.
               03  COLUMN 6 PIC 9(4) SOURCE RUNTOT.
       01  CF-FINAL TYPE CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 6 PIC 9(4) SOURCE RUNTOT.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "T1" TO WS-T(1).
           MOVE "T2" TO WS-T(2).
           MOVE "T3" TO WS-T(3).
           OPEN OUTPUT PRT.
           INITIATE R-SF.
           MOVE 5 TO WS-AMT.
           GENERATE DET-A.
           MOVE 7 TO WS-AMT.
           GENERATE DET-A.
           TERMINATE R-SF.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
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
