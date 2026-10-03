      *> kb/Work PB1456 + PB1049 - a report-section SOURCE may name the LINE-COUNTER or PAGE-COUNTER of ANOTHER
      *> report, qualified by its report-name (ISO 8.4.2.2.3 SR9/SR10), at the rule's INTRODUCING edition ('85).
      *>
      *> RULES (cite.py --check, all OK):
      *> 8.4.2.2.3 SR9: "LINE-COUNTER shall be qualified each time it is referenced in the procedure division if more
      *> than one report description entry is specified in the source element. In the report section, an unqualified
      *> reference to LINE-COUNTER is qualified implicitly by the name of the report in whose report description entry
      *> the reference is made. Whenever the LINE-COUNTER of a different report is referenced, LINE-COUNTER shall be
      *> qualified explicitly by the report-name associated with the different report." SR10 is its PAGE-COUNTER twin.
      *> 8.4.3.15.3 SR1: "In the report section, PAGE-COUNTER and LINE-COUNTER may be referenced only in a SOURCE
      *> clause." 13.18.53.3 SR4: a report counter identifier is admitted in a SOURCE clause.
      *> 8.4.3.15.1: the counters "exist independently for each report" (each report has its own PAGE-COUNTER and
      *> LINE-COUNTER; INITIATE sets PAGE-COUNTER to 1 and LINE-COUNTER to 0, 14.9.21.4 GR1).
      *>
      *> DERIVATION (two reports on two files, PAGE LIMIT 30 LINES each, so each detail line is the first body line of
      *> its page at line 1, then line 2):
      *>   D1 (R1: own PAGE-COUNTER, then LINE-COUNTER OF R2)   D2 (R2: PAGE-COUNTER OF R1, then its own LINE-COUNTER)
      *>   GENERATE D1:  R1's PAGE-COUNTER is 1; R2 has printed nothing, its LINE-COUNTER is 0      => R1: `01 00`
      *>   GENERATE D2:  R1's PAGE-COUNTER is 1; R2's own (implicitly qualified) LINE-COUNTER is 1  => R2: `01 01`
      *>   MOVE 40 TO PAGE-COUNTER OF R1, then GENERATE D2: 40 and R2's LINE-COUNTER 2              => R2: `40 02`
      *>   GENERATE D1:  R1's PAGE-COUNTER is now 40; R2's LINE-COUNTER is 2                        => R1: `40 02`
      *> Fails (COBOLNET0899 'a counter of another report ... not yet implemented') on a binder that stages the
      *> qualified counter of a different report.
      *>
      *> THE READ-BACK IS BYTE-WISE: a one-character record on a second SELECT over each report file is legal at every
      *> edition (see pb565_report_repeating_entry).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1456O85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb1456o85a.txt".
           SELECT P2 ASSIGN TO "pb1456o85b.txt".
           SELECT C1 ASSIGN TO "pb1456o85a.txt".
           SELECT C2 ASSIGN TO "pb1456o85b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 REPORT IS R1.
       FD  P2 REPORT IS R2.
       FD  C1.
       01  C1-REC PIC X.
       FD  C2.
       01  C2-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-BYTE PIC X.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE PAGE-COUNTER.
           02  COLUMN 4 PIC 99 SOURCE LINE-COUNTER OF R2.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE PAGE-COUNTER OF R1.
           02  COLUMN 4 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1 P2.
           INITIATE R1 R2.
           GENERATE D1.
           GENERATE D2.
           MOVE 40 TO PAGE-COUNTER OF R1.
           GENERATE D2.
           GENERATE D1.
           TERMINATE R1 R2.
           CLOSE P1 P2.
           DISPLAY "R1".
           OPEN INPUT C1.
           PERFORM UNTIL WS-EOF = "Y"
               READ C1
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE C1-REC TO WS-BYTE PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE C1.
           PERFORM SHOW-LINE.
           DISPLAY "R2".
           MOVE "N" TO WS-EOF.
           OPEN INPUT C2.
           PERFORM UNTIL WS-EOF = "Y"
               READ C2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END MOVE C2-REC TO WS-BYTE PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE C2.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF WS-BYTE = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF WS-BYTE NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
