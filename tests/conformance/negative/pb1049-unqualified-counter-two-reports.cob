      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1049 - ISO 8.4.2.2.3 SR10: "PAGE-COUNTER shall be qualified each time it is referenced in the procedure
      *> division if more than one report description entry is specified in the source element." Two RDs, and the
      *> procedure division MOVEs an unqualified PAGE-COUNTER: a qualification violation, COBOLNET2729 - not the
      *> recognized-but-not-implemented code COBOLNET0899, which tells the reader a legal program is unsupported.
      *> (In the report section the same unqualified counter is legal: the enclosing RD qualifies it implicitly.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1049N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "pb1049n1a.txt".
           SELECT P2 ASSIGN TO "pb1049n1b.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  P1 REPORT IS R1.
       FD  P2 REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 99 VALUE 0.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 30 LINES.
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE PAGE-COUNTER.
       RD  R2 PAGE LIMIT IS 30 LINES.
       01  D2 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE PAGE-COUNTER.
       PROCEDURE DIVISION.
           OPEN OUTPUT P1 P2.
           INITIATE R1 R2.
           MOVE PAGE-COUNTER TO WS-N.
           TERMINATE R1 R2.
           CLOSE P1 P2.
           STOP RUN.
