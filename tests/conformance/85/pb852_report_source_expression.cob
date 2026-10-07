      *> ⛔ THE SOURCE CLAUSE'S OTHER OPERAND FORM, AND ITS ROUNDED PHRASE (kb/Work PB852).  ISO 13.18.53.2's
      *> general format (PDF p485 rendered) writes
      *>     { SOURCE IS | SOURCES ARE } { identifier-1 | arithmetic-expression-1 } … [ rounded-phrase ]
      *> — the ellipsis follows the OPERAND brace pair, and the rounded-phrase sits OUTSIDE it, governing the
      *> whole clause.  COBOL.NET's grammar was `(SOURCE|SOURCES) (IS|ARE)? dataReference+`: neither the
      *> expression operand nor the ROUNDED phrase had any surface, so 13.18.53.3 SR3/SR5/SR7 and 13.18.53.4 GR2
      *> had no reachable population at all and `SOURCE IS (WS-A + WS-B)` was a raw parse error.
      *>
      *> THE RULES.  13.18.53.4 GR2: "Arithmetic-expression-1 specifies the operand of an implicit COMPUTE
      *> statement that is executed implicitly whenever the associated item is printed.  If the ROUNDED phrase is
      *> specified, the implicit COMPUTE statement has the corresponding ROUNDED phrase."  13.18.53.3 SR5: "If
      *> identifier-1 is specified with the ROUNDED phrase, it is considered to be an arithmetic-expression" — so
      *> the phrase turns even a bare identifier operand into GR2's COMPUTE, which is why one classifier decides
      *> the form for both clauses.  SR7: "If the SOURCE clause has more than one operand of which at least one is
      *> an arithmetic-expression, each operand shall be enclosed in parentheses" — the rule that makes an operand
      *> LIST readable once an operand may itself contain operators, and it is ENFORCED (COBOLNET2142), not
      *> assumed from the grammar's shape.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  WS-A = 100, WS-B = 23, WS-F = 1.55, WS-N = 2.  One TYPE DE group, one
      *> GENERATE; 13.18.14.4 GR1/GR2 place each field's leftmost character at its COLUMN.
      *>   COLUMN 1  PIC 9(3) SOURCE IS WS-A + WS-B   — GR2's COMPUTE: 100 + 23                        => 123
      *>   COLUMN 5  PIC 9(3) SOURCE IS WS-F ROUNDED  — SR5 makes it an expression, GR2 gives the COMPUTE
      *>                                                the ROUNDED phrase; 14.7.4.3 rule 1 with no
      *>                                                DEFAULT ROUNDED MODE clause is NEAREST-AWAY-FROM-ZERO,
      *>                                                so 1.55 into a scale-0 item                     => 002
      *>   COLUMN 9  PIC 9(3) SOURCE IS WS-F          — no phrase: GR1's implicit MOVE, which truncates
      *>                                                (14.6.8.2 rule 4)                               => 001
      *> SR7's multi-operand form needs a REPEATING entry (SR6), whose only vehicles — a multiple COLUMN or LINE
      *> clause, or a report-group OCCURS — are COBOL-2002 introductions, so it is witnessed by the 2002 sibling
      *> tests/conformance/2002/pb852_report_source_operand_parens.cob and its negative twin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB852RSX.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb852rsx.txt".
           SELECT CHK ASSIGN TO "pb852rsx.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SX.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-A    PIC 999   VALUE 100.
       01  WS-B    PIC 999   VALUE 23.
       01  WS-N    PIC 9     VALUE 2.
       01  WS-F    PIC 9V99  VALUE 1.55.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-SX PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS WS-A + WS-B.
           03  COLUMN 5  PIC 9(3) SOURCE IS WS-F ROUNDED.
           03  COLUMN 9  PIC 9(3) SOURCE IS WS-F.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SX.
           GENERATE DET.
           TERMINATE R-SX.
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
