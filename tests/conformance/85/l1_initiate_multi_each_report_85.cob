      *> ISO §14.9.21.4 GR1 c) — INITIATE sets each named report's
      *> PAGE-COUNTER to 1. GR1: "The INITIATE statement performs the
      *> following initialization functions for each specified
      *> report:" ... c) "PAGE-COUNTER is set to 1"
      *>   cite.py: OK  §14.9.21.4 1)  (General rules)
      *> This is a SUPPORTING GR1 c) witness only. It does NOT witness
      *> GR5 (cite.py: OK §14.9.21.4 5)): its one line is fixed by
      *> GR1 alone and is the same for either initiation order, so an
      *> implementation that broke GR5's written order would pass it.
      *> At 1985 GR5 has no observable separable from GR1 - without
      *> exception checking or RESUME no conforming program can see
      *> the order - so the 85 cell of GR5 is unwitnessed.
      *> Supporting (each cite.py --check OK):
      *>  §8.4.3.15.3 1) PAGE-COUNTER may be referenced in the
      *>    procedure division "in any context where an integer data
      *>    item may appear"; 3) bars only LINE-COUNTER as a receiver.
      *>  §14.9.46.4 2) - a TERMINATE with no GENERATE since the
      *>    INITIATE prints nothing.
      *> At 1985 there is no exception checking and no RESUME, so the
      *> ORDER is not observable; what is observable is that EVERY
      *> named report - and only the named reports - is initiated.
      *> DERIVATION. PC-A, PC-B, PC-C are each set to 5 (no report is
      *> active). INITIATE R-B R-A = INITIATE R-B; INITIATE R-A; each
      *> sets its own PAGE-COUNTER to 1. R-C is not named: its
      *> PAGE-COUNTER keeps 5.
      *>   "AFTER A=1 B=1 C=5"
      *> An implementation that initiated only the first (or only the
      *> last) name would leave A=5 (or B=5) - a GR1 violation.
      *> GR5 (both sentences) is pinned at 2002+ by
      *> conformance:2002/l1_initiate_resume_next_implicit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INIM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1INIMA.RPT".
           SELECT RPTB ASSIGN TO "L1INIMB.RPT".
           SELECT RPTC ASSIGN TO "L1INIMC.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPTA REPORT IS R-A.
       FD RPTB REPORT IS R-B.
       FD RPTC REPORT IS R-C.
       WORKING-STORAGE SECTION.
       01 W-A PIC 9.
       01 W-B PIC 9.
       01 W-C PIC 9.
       REPORT SECTION.
       RD R-A.
       01 DA TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DA".
       RD R-B.
       01 DB TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DB".
       RD R-C.
       01 DC TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DC".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT RPTA RPTB RPTC.
           MOVE 5 TO PAGE-COUNTER OF R-A.
           MOVE 5 TO PAGE-COUNTER OF R-B.
           MOVE 5 TO PAGE-COUNTER OF R-C.
           INITIATE R-B R-A.
           MOVE PAGE-COUNTER OF R-A TO W-A.
           MOVE PAGE-COUNTER OF R-B TO W-B.
           MOVE PAGE-COUNTER OF R-C TO W-C.
           DISPLAY "AFTER A=" W-A " B=" W-B " C=" W-C.
           TERMINATE R-A R-B.
           CLOSE RPTA RPTB RPTC.
           STOP RUN.
