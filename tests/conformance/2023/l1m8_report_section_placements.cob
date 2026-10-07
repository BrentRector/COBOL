      *> ISO §13.8.3 1) — a REPORT SECTION is legal in a program, a
      *> function, a factory and an instance definition (positive half)
      *> SR1: "The report section may be specified in a function
      *>   definition or a program definition. Within a class
      *>   definition, the report section may be specified only in a
      *>   factory definition or an instance definition, but not in a
      *>   method definition. The report section shall not be specified
      *>   within an interface definition."
      *>   cite.py: OK  §13.8.3 1)  (Syntax rule)
      *> The two prohibitions are the negatives
      *>   conformance:negative/l1m8-report-section-in-method and
      *>   conformance:negative/l1m8-report-section-in-interface.
      *> Each of the four legal owners declares its own FD + RD. The
      *>   program and the function run INITIATE / GENERATE / TERMINATE
      *>   on theirs, reporting LINE-COUNTER right after the GENERATE
      *>   (the factory and object legs: see below). Each detail is the
      *>   chronologically
      *>   first body group of its report, so no page fit test applies
      *>   (§13.18.35.4 4)), and its first LINE clause is absolute, so
      *>   "the report group's first line number is given by integer-1"
      *>   (OK §13.18.35.4 5) a)) and LINE-COUNTER is set equal to it
      *>   (§13.18.35.4 6)); with one line and no NEXT GROUP that is the
      *>   final value (OK §8.4.3.15.4 4)). Distinct integers identify
      *>   the owner: program 2, function 3, factory 4, instance 5.
      *> The class inherits from BASE, so INVOKE ... "NEW" is available
      *>   for the instance leg.
      *> The FACTORY and OBJECT legs pin ACCEPTANCE only. A method is a
      *>   separate source element (§3.164 "source unit excluding any
      *>   contained source units"), and a report cannot be driven from
      *>   it: §8.4.6.2.2 makes a constant-, data-, record-, file- or
      *>   type-name declared in an object definition global, but NOT a
      *>   report-name, so INITIATE/TERMINATE R-F (and so any GENERATE)
      *>   is out of reach; §8.4.6.2.5 makes PAGE-COUNTER/LINE-COUNTER
      *>   local unless the RD says GLOBAL, and §13.18.27.3 4) forbids
      *>   GLOBAL in a factory or instance definition; §8.4.6.2.1 lets a
      *>   local name be referenced only in its own source element. So
      *>   RUNF/RUNO name none of R-F, R-O, DET-F, DET-O, LINE-COUNTER or
      *>   PAGE-COUNTER. They OPEN/CLOSE the report file (a file-name
      *>   declared in an object definition IS global) and return the
      *>   leg's identifying integer, 4 or 5.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. L1M8FN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FN ASSIGN TO "l1m8rp-fn.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT-FN REPORT IS R-FN.
       LINKAGE SECTION.
       01 RES PIC 99.
       REPORT SECTION.
       RD R-FN PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 8 FOOTING 9.
       01 DET-FN TYPE DE LINE 3.
          02 COLUMN 1 PIC X(8) VALUE "FUNCTION".
       PROCEDURE DIVISION RETURNING RES.
       MAIN-FN.
           OPEN OUTPUT RPT-FN.
           INITIATE R-FN.
           GENERATE DET-FN.
           MOVE LINE-COUNTER TO RES.
           TERMINATE R-FN.
           CLOSE RPT-FN.
           GOBACK.
       END FUNCTION L1M8FN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1M8RK
           FUNCTION L1M8FN.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-P ASSIGN TO "l1m8rp-p.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT-P REPORT IS R-P.
       WORKING-STORAGE SECTION.
       01 W-LC PIC 99.
       01 O USAGE OBJECT REFERENCE L1M8RK.
       REPORT SECTION.
       RD R-P PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 8 FOOTING 9.
       01 DET-P TYPE DE LINE 2.
          02 COLUMN 1 PIC X(7) VALUE "PROGRAM".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT-P.
           INITIATE R-P.
           GENERATE DET-P.
           MOVE LINE-COUNTER TO W-LC.
           TERMINATE R-P.
           CLOSE RPT-P.
           DISPLAY "PROGRAM LC=" W-LC.
           MOVE FUNCTION L1M8FN TO W-LC.
           DISPLAY "FUNCTION LC=" W-LC.
           INVOKE L1M8RK "RUNF" RETURNING W-LC.
           DISPLAY "FACTORY LC=" W-LC.
           INVOKE L1M8RK "NEW" RETURNING O.
           INVOKE O "RUNO" RETURNING W-LC.
           DISPLAY "OBJECT LC=" W-LC.
           STOP RUN.
       END PROGRAM L1M8RP.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1M8RK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "l1m8rp-f.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT-F REPORT IS R-F.
       REPORT SECTION.
       RD R-F PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 8 FOOTING 9.
       01 DET-F TYPE DE LINE 4.
          02 COLUMN 1 PIC X(7) VALUE "FACTORY".
       PROCEDURE DIVISION.
       METHOD-ID. RUNF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 99.
       PROCEDURE DIVISION RETURNING RES.
       MAIN-F.
           OPEN OUTPUT RPT-F.
           CLOSE RPT-F.
           MOVE 4 TO RES.
       END METHOD RUNF.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-O ASSIGN TO "l1m8rp-o.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT-O REPORT IS R-O.
       REPORT SECTION.
       RD R-O PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 8 FOOTING 9.
       01 DET-O TYPE DE LINE 5.
          02 COLUMN 1 PIC X(6) VALUE "OBJECT".
       PROCEDURE DIVISION.
       METHOD-ID. RUNO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RES PIC 99.
       PROCEDURE DIVISION RETURNING RES.
       MAIN-O.
           OPEN OUTPUT RPT-O.
           CLOSE RPT-O.
           MOVE 5 TO RES.
       END METHOD RUNO.
       END OBJECT.
       END CLASS L1M8RK.
