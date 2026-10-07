      *> A PARENTHESISED-FIRST SUM ADDEND (kb/Work PB924).  ISO 13.18.54.3
      *> SR9: "Within a report description entry, if the keyword SUM is
      *> preceded by the keyword FUNCTION, SUM is a reference to the SUM
      *> intrinsic function.  Otherwise, SUM refers to the report writer
      *> SUM clause" - so a '(' after a bare SUM here can only open a
      *> parenthesised arithmetic-expression-1 addend (13.18.54.2).  The
      *> lexer captured it as a function argument list (SUM is a keyword-
      *> omitted intrinsic name elsewhere), and `SUM (WS-K * WS-M)` died at
      *> the token INSIDE the parentheses while `SUM WS-K * WS-M` compiled.
      *>
      *> DERIVATION.  WS-K = 10, WS-M = 3.  One GENERATE, so each addend
      *> is accumulated once (13.18.54.4 GR7 c 1) into a zeroed counter:
      *>   COLUMN 1  SUM (WS-K * WS-M)       - 30                 => 0030
      *>   COLUMN 7  SUM OF (WS-K + 1) * WS-M - 11 * 3            => 0033
      *>   COLUMN 13 SUM (WS-K) WS-M          - GR9: two addends,
      *>                                        10 + 3            => 0013
      *>   COLUMN 19 SUM WS-K * WS-M          - the unparenthesised
      *>                                        control           => 0030
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB924RSP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb924rsp.txt".
           SELECT CHK ASSIGN TO "pb924rsp.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-SP.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-K    PIC 99    VALUE 10.
       01  WS-M    PIC 99    VALUE 3.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-SP CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) VALUE "DET".
       01  CFG TYPE IS CONTROL FOOTING FINAL.
           02  LINE PLUS 1.
               03  COLUMN 1  PIC 9999 SUM (WS-K * WS-M).
               03  COLUMN 7  PIC 9999 SUM OF (WS-K + 1) * WS-M.
               03  COLUMN 13 PIC 9999 SUM (WS-K) WS-M.
               03  COLUMN 19 PIC 9999 SUM WS-K * WS-M.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-SP.
           GENERATE DET.
           TERMINATE R-SP.
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
