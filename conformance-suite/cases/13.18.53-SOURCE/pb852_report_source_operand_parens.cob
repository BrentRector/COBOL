*>--- conformance-case
*> id: pb852_report_source_operand_parens
*> rules: [SR-13.18.53.3-7, GR-13.18.53.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ⛔ ISO 13.18.53.3 SR7 — THE PARENTHESIZED OPERAND LIST (kb/Work PB852).  "If the SOURCE clause has more
      *> than one operand of which at least one is an arithmetic-expression, each operand shall be enclosed in
      *> parentheses."  Operands of a SOURCE clause are separated by nothing but a space (13.18.53.2's ellipsis
      *> repeats the brace pair), so once an operand may itself contain operators the list has two readings and
      *> the standard removes the choice by requiring the parentheses — on EVERY operand, the bare identifiers
      *> included.  The rule is ENFORCED (COBOLNET2142), never inferred from the grammar's shape; its negative
      *> twin is tests/conformance/negative/pb852-source-operand-parens.cob.
      *>
      *> WHY THIS COPY IS AT 2002 AND THE REST OF PB852 AT 85.  SR7 only bites on a multi-operand clause, which
      *> SR6 confines to a repeating entry ("the entry shall be a repeating entry or shall be subordinate to a
      *> repeating entry"), and 13.15.4 GR3's repetition vehicles — a multiple COLUMN or LINE clause, or a
      *> report-group OCCURS — are COBOL-2002 introductions.  The ROUNDED phrase and the expression operand
      *> themselves are witnessed at 85 by tests/conformance/85/pb852_report_source_expression.cob.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  WS-A = 100, WS-B = 23, WS-N = 2.  One TYPE DE group, one GENERATE.
      *> The entry writes COLUMNS ARE 1 5 — two repetitions (13.15.4 GR3) — and two SOURCE operands, so
      *> 13.18.53.4 GR4 assigns "successive operands to successive repeating printable items, horizontally and
      *> then vertically": operand 1 to COLUMN 1, operand 2 to COLUMN 5.
      *>   (WS-A)            => 100
      *>   (WS-B * WS-N)     => 23 × 2 = 46, into PIC 9(3) => 046
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB852ROP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb852rop.txt".
           SELECT CHK ASSIGN TO "pb852rop.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-OP.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-A    PIC 999   VALUE 100.
       01  WS-B    PIC 999   VALUE 23.
       01  WS-N    PIC 9     VALUE 2.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-OP PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMNS ARE 1 5 PIC 9(3)
               SOURCES ARE (WS-A) (WS-B * WS-N).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-OP.
           GENERATE DET.
           TERMINATE R-OP.
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
