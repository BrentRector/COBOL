      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1390 — the TERMINATION half of ISO §8.8.4.12.4 GR1: "The insertion of an omitted subject and/or
      *> relational operator terminates once a complete simple condition is encountered within a complex
      *> condition." After the class condition B IS NUMERIC nothing is carried, so `OR < 2` has no subject to
      *> insert (§8.8.4.12.1 lets a relation omit its subject only when a preceding relation condition supplies
      *> one). It was refused with no diagnostic (the COBOLNET2319 internal-error net); it is now COBOLNET2552.
      *> (A group that OPENS with an abbreviated relation, `OR (< 2)`, never parses: every leading tier begins
      *> with an unabbreviated condition.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68NAS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           IF A = 1 OR B IS NUMERIC OR < 2
               DISPLAY "T"
           END-IF
           STOP RUN.
