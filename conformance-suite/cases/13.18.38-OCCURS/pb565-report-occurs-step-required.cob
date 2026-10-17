*>--- conformance-case
*> id: pb565-report-occurs-step-required
*> rules: [SR-13.18.38.3-16, SR-13.18.38.3-25]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ AN ABSOLUTE COLUMN ON A REPEATING ENTRY REQUIRES THE STEP PHRASE (kb/Work PB565).
*> ISO/IEC 1989:2023 §13.18.38.3 SR25: "The STEP phrase shall be specified if the entry: a) contains an
*> absolute LINE clause, or b) has an entry with an absolute LINE clause subordinate to it, or c) contains an
*> absolute COLUMN clause, or d) is subordinate to an entry with a LINE clause and has an entry with an
*> absolute COLUMN clause subordinate to it. In all other cases, the STEP phrase is optional."
*>
*> The rule has teeth: §13.18.38.4 GR12 gives a STEP'd entry an interval, and its closing sentence gives an
*> unSTEP'd one "the vertical or horizontal interval … defined by the relative LINE or COLUMN numbers" — which
*> an ABSOLUTE COLUMN does not supply, so every repetition would land in the same column. This is the shape
*> the PB565 defect note's own repro carried, and it was not conforming source: an implementation that
*> accepted it would print one item where three were written, silently.
*>
*> Rejected at every edition that HAS the clause (it enters at 2002 — see pb565-report-occurs-at-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB565STP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb565stp.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-ST.
       REPORT SECTION.
       RD  R-ST PAGE LIMIT IS 20 LINES.
       01  D-ST TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(3) OCCURS 3 TIMES VALUE "AAA".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-ST.
           GENERATE D-ST.
           TERMINATE R-ST.
           CLOSE RPT.
           STOP RUN.
