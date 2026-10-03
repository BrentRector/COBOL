      *> kb/Work PB1292 + PB1306 + PB1316 - the SOURCE clause's operand forms that need a 2002 construct (ISO
      *> 13.18.53), at their INTRODUCING edition: a VARYING counter as a SUBSCRIPT of a source item (the VARYING
      *> clause is 2002) and a numeric CONSTANT-NAME as an operand (the constant entry is 2002). The '85 forms - a
      *> subscripted or reference-modified identifier and a sum counter - are pb1292_source_forms_85.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.53.2: the SOURCE operand is `{ identifier-1 | arithmetic-expression-1 }`.
      *> 13.18.64.4 GR4 NOTE: "this allows data-name-1 to be used as a source data item, as a subscript to a source
      *> data item or as part of the identifier in the DEFAULT clause."
      *> 13.18.64.3 SR2: the counter "may be referenced only within the current entry or a subordinate entry" - here
      *> the entry that holds the SOURCE clause is the entry that declares the counter.
      *> 13.18.64.4 GR3: occurrence 1 takes arithmetic-expression-1 (FROM), each later one adds arithmetic-expression-2.
      *> 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a format specifies a
      *> literal of the class and category of constant-name-1." A numeric literal is an operand of
      *> arithmetic-expression-1 (8.8.1.1), so a numeric constant-name is a SOURCE operand.
      *> 13.18.53.3 SR3: an arithmetic-expression-1 operand needs a numeric entry (PIC 9 here).
      *>
      *> DERIVATION (WS-C = ABCDEFGHI, KC = 5):
      *>   LINE 1 - SOURCE KC places 5 at column 1 => `5`.
      *>   LINE 2 - K = 1, 3, 5 (FROM 1 BY 2) subscripts WS-CC: WS-CC(1) = A, WS-CC(3) = C, WS-CC(5) = E, at columns
      *>            1, 3, 5 (STEP 2) => `A C E`.
      *>   LINE 3 - the same with the subscript an expression of the counter: WS-CC(K + 1) = B, D, F => `B D F`.
      *> Fails (COBOLNET0899 'SOURCE KC does not resolve to a data item', 'a subscripted ... SOURCE operand ... is not
      *> yet implemented') on a binder whose identifier arm looks a NAME up in ordinary storage.
      *>
      *> THE READ-BACK IS BYTE-WISE ON PURPOSE (see pb565_report_repeating_entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1292SF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb1292sf.txt".
           SELECT CHK ASSIGN TO "pb1292sf.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SF.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-C   PIC X(9) VALUE "ABCDEFGHI".
       01  WS-C-TABLE REDEFINES WS-C.
           05  WS-CC PIC X OCCURS 9.
       01  KC CONSTANT AS 5.
       REPORT SECTION.
       RD  R-SF PAGE LIMIT 20 LINES.
       01  DET-A TYPE DETAIL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 SOURCE KC.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X OCCURS 3 TIMES STEP 2
                   VARYING K FROM 1 BY 2 SOURCE WS-CC(K).
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X OCCURS 3 TIMES STEP 2
                   VARYING J FROM 1 BY 2 SOURCE WS-CC(J + 1).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SF.
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
