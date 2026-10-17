      *>--- conformance-case
      *> id: l1c15_linage_counter_qualified
      *> rules: [SR-8.4.3.1.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.4.3.1.3 SR11 — qualified-linage-counter (OF / IN
      *> file-name) selects that file's own LINAGE-COUNTER.
      *> Rule: "Qualified-linage-counter-1 is defined in 8.4.2.2,
      *> Qualification."
      *> cite.py: OK  §8.4.3.1.3 11)  (Syntax rules)
      *> §8.4.2.2.3 SR8: "LINAGE-COUNTER shall be qualified if more
      *> than one file description entry containing a LINAGE clause
      *> may be referenced within the source element"
      *> cite.py: OK  §8.4.2.2.3 8)  (Syntax rules)
      *> §13.18.34.4 GR7: "A separate LINAGE-COUNTER is supplied for
      *> each file ... whose file description entry contains a LINAGE
      *> clause." / "The value of LINAGE-COUNTER is automatically set
      *> to one at the time an OPEN statement with the OUTPUT phrase
      *> is executed" / "When the ADVANCING phrase of the WRITE
      *> statement is not specified, the LINAGE-COUNTER is incremented
      *> by the value one."
      *> cite.py: OK  §13.18.34.4 7) a)  (General rules)  [a), d), c)3.]
      *> DERIVATION: both FDs carry LINAGE, so SR8 requires the
      *> qualified forms used here (OF F1, IN F2).
      *>  O1: after OPEN OUTPUT both counters are 1 (7d) -> 01 01.
      *>  W1: F1 gets 3 WRITEs, F2 gets 1, no ADVANCING, so each WRITE
      *>   adds one (7c3): F1 = 1+3 = 4, F2 = 1+1 = 2 -> 04 02.
      *>   A single shared counter would print 05 05 (or equal values).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "L1C15B1.PRT".
           SELECT F2 ASSIGN TO "L1C15B2.PRT".
       DATA DIVISION.
       FILE SECTION.
       FD F1 LINAGE IS 20 LINES.
       01 R1 PIC X(4).
       FD F2 LINAGE IS 20 LINES.
       01 R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 C1 PIC 99.
       01 C2 PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1 F2.
           MOVE LINAGE-COUNTER OF F1 TO C1.
           MOVE LINAGE-COUNTER IN F2 TO C2.
           DISPLAY "O1 " C1 " " C2.
           MOVE "AAAA" TO R1.
           WRITE R1.
           WRITE R1.
           WRITE R1.
           MOVE "BBBB" TO R2.
           WRITE R2.
           MOVE LINAGE-COUNTER OF F1 TO C1.
           MOVE LINAGE-COUNTER IN F2 TO C2.
           DISPLAY "W1 " C1 " " C2.
           CLOSE F1 F2.
           STOP RUN.
