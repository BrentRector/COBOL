      *> ISO §14.9.21.4 GR5 — multi-name INITIATE order, RESUME NEXT
      *> GR5: "The results of executing an INITIATE statement in which
      *> more than one report-name-1 is specified is the same as if a
      *> separate INITIATE statement had been executed for each
      *> report-name-1 in the same order as specified in the
      *> statement. If an implicit INITIATE statement results in the
      *> execution of a declarative procedure that executes a RESUME
      *> statement with the NEXT STATEMENT phrase, processing resumes
      *> at the next implicit INITIATE statement, if any."
      *>   cite.py: OK  §14.9.21.4 5)  (General rules)
      *> Supporting (each cite.py --check OK):
      *>  §14.9.21.4 1) c) "PAGE-COUNTER is set to 1"
      *>  §14.9.21.4 2) "If it is in the active state, the
      *>    EC-REPORT-ACTIVE exception condition is set to exist and
      *>    the execution of the INITIATE statement has no other
      *>    effect."
      *>  §14.9.21.4 4) "A successful INITIATE statement places the
      *>    report in the active state."
      *>  §8.4.3.15.3 1) "In the procedure division, PAGE-COUNTER and
      *>    LINE-COUNTER may be referenced in any context where an
      *>    integer data item may appear"; 3) bars only LINE-COUNTER
      *>    as a receiving operand - so MOVE ... TO PAGE-COUNTER is
      *>    legal and is the probe that shows WHEN each report was
      *>    initiated.
      *>  §14.9.33.4 2) a) - without §14.9.21.4 GR5 the implicit
      *>    CONTINUE
      *>    "immediately follows the end of the statement that was
      *>    executing ... unless general rules associated with the
      *>    applicable statement specify otherwise"; GR5 sentence 2
      *>    is that "otherwise".
      *>  §14.9.46.4 2) - a TERMINATE with no GENERATE since the
      *>    INITIATE "causes no processing of any kind to take place
      *>    for any report groups" (so it prints nothing).
      *>  §15.33.3 1) - EXCEPTION-STATUS is the 31-character
      *>    left-justified exception-name.
      *> DERIVATION.
      *>  INITIATE R-B: inactive, file open OUTPUT -> PC-B = 1, active.
      *>  MOVEs: PC-A = 5, PC-B = 7, PC-C = 5.
      *>  INITIATE R-A R-B R-C = INITIATE R-A; R-B; R-C (sentence 1):
      *>   R-A: inactive -> PC-A = 1, active.
      *>   R-B: ACTIVE -> EC-REPORT-ACTIVE, no other effect (PC-B
      *>        stays 7); checking is ON, so the declarative runs:
      *>          "ACTIVE EC-REPORT-ACTIVE"
      *>          "IN-HANDLER A=1 B=7 C=5"
      *>        A=1 pins R-A initiated BEFORE R-B; C=5 pins R-C not
      *>        yet initiated (written order). RESUME AT NEXT STATEMENT
      *>        resumes at the next implicit INITIATE (sentence 2):
      *>   R-C: inactive -> PC-C = 1, active.
      *>  "AFTER A=1 B=7 C=1" - C=1 pins sentence 2: resuming after
      *>  the whole INITIATE statement would leave C=5.
      *>  TERMINATE R-A R-B R-C: all active, no GENERATE -> nothing.
      *> The fatal EC is handled by the declarative, so the run unit
      *> continues.
      *> EDITION: >>TURN, USE AFTER EXCEPTION CONDITION, RESUME and
      *> FUNCTION EXCEPTION-STATUS are COBOL-2002. At 1985 GR5 has
      *> no observable separable from GR1 (the order cannot be seen);
      *> conformance:85/l1_initiate_multi_each_report_85 is a GR1 c)
      *> witness only, not a GR5 one.
       >>TURN EC-REPORT-ACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1INIR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1INIRA.RPT".
           SELECT RPTB ASSIGN TO "L1INIRB.RPT".
           SELECT RPTC ASSIGN TO "L1INIRC.RPT".
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
       DECLARATIVES.
       UA SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-ACTIVE.
       UA-P.
           DISPLAY "ACTIVE " FUNCTION EXCEPTION-STATUS.
           MOVE PAGE-COUNTER OF R-A TO W-A.
           MOVE PAGE-COUNTER OF R-B TO W-B.
           MOVE PAGE-COUNTER OF R-C TO W-C.
           DISPLAY "IN-HANDLER A=" W-A " B=" W-B " C=" W-C.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPTA RPTB RPTC.
           INITIATE R-B.
           MOVE 5 TO PAGE-COUNTER OF R-A.
           MOVE 7 TO PAGE-COUNTER OF R-B.
           MOVE 5 TO PAGE-COUNTER OF R-C.
           INITIATE R-A R-B R-C.
           MOVE PAGE-COUNTER OF R-A TO W-A.
           MOVE PAGE-COUNTER OF R-B TO W-B.
           MOVE PAGE-COUNTER OF R-C TO W-C.
           DISPLAY "AFTER A=" W-A " B=" W-B " C=" W-C.
           TERMINATE R-A R-B R-C.
           CLOSE RPTA RPTB RPTC.
           STOP RUN.
