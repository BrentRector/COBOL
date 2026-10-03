      *> kb/Work PB1456 - a report counter inside a SOURCE arithmetic-expression-1 (ISO 13.18.53), at the rule's
      *> INTRODUCING edition (the expression operand is 2002), in a program with TWO reports.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.53.3 SR4: "If identifier-1 specifies a report section item, it shall be a report counter identifier or
      *> a sum counter defined in the current report. This same Syntax rule applies to any identifier appearing in
      *> arithmetic-expression-1."
      *> 8.4.2.2.3 SR10: "PAGE-COUNTER shall be qualified each time it is referenced in the procedure division if more
      *> than one report description entry is specified in the source element. In the report section, an unqualified
      *> reference to PAGE-COUNTER is qualified implicitly by the name of the report in whose report description entry
      *> the reference is made. Whenever the PAGE-COUNTER of a different report is referenced, PAGE-COUNTER shall be
      *> qualified explicitly by the report-name associated with the different report."
      *> 13.18.53.4 GR2: arithmetic-expression-1 is the operand of an implicit COMPUTE executed whenever the item is
      *> printed.
      *>
      *> DERIVATION (R2's detail prints two expressions; R1 has printed one line, so R1's PAGE-COUNTER is 1 and R2's
      *> own PAGE-COUNTER is 1):
      *>   (PAGE-COUNTER OF R1 + 100) = 101 at column 1; the UNQUALIFIED (PAGE-COUNTER + 100) names R2's own counter,
      *>   the report in whose RD the reference is made, even though the program has two reports: 101 at column 5
      *>   => `101 101`. After MOVE 40 TO PAGE-COUNTER OF R1 the first expression is 140 => `140 101`.
      *> Fails (COBOLNET2144 'neither a report counter nor a sum counter of this report' for the qualified form and
      *> COBOLNET0899 'unqualified PAGE-COUNTER with more than one report' for the unqualified one) on a binder that
      *> resolves a counter inside an expression by the procedure division's 'exactly one report' rule.
      *>
      *> THE READ-BACK IS BYTE-WISE (see pb565_report_repeating_entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1456CE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb1456cea.txt".
           SELECT P2 ASSIGN TO "pb1456ceb.txt".
           SELECT C2 ASSIGN TO "pb1456ceb.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 REPORT IS R1.
       FD  P2 REPORT IS R2.
       FD  C2.
       01  C2-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE PAGE-COUNTER.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 999 SOURCE (PAGE-COUNTER OF R1 + 100).
           02  COLUMN 5 PIC 999 SOURCE (PAGE-COUNTER + 100).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1 P2.
           INITIATE R1 R2.
           GENERATE D1.
           GENERATE D2.
           MOVE 40 TO PAGE-COUNTER OF R1.
           GENERATE D2.
           TERMINATE R1 R2.
           CLOSE P1 P2.
           OPEN INPUT C2.
           PERFORM UNTIL WS-EOF = "Y"
               READ C2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE C2.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF C2-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF C2-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE C2-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
