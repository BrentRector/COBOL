      *> reject-at: 2002 2014 2023
      *> ISO §13.8.3 1) — a REPORT SECTION in a METHOD definition
      *> Rule: "Within a class definition, the report section may be
      *>   specified only in a factory definition or an instance
      *>   definition, but not in a method definition."
      *>   cite.py: OK  §13.8.3 1)  (Syntax rule)
      *> Method RUNM of the instance definition declares an (empty)
      *> REPORT SECTION; nothing else in the program is wrong (the
      *> general format lets a REPORT SECTION hold no entries). The
      *> positive half is conformance:2023/l1m8_report_section_placements.
      *> Expected: COBOLNET1519, the method-section arm; the .err pins
      *> its REPORT SECTION message (the FILE / SCREEN arms share the
      *> code).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8NM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS L1M8NMC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE L1M8NMC.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE L1M8NMC "NEW" RETURNING O.
           INVOKE O "RUNM".
           STOP RUN.
       END PROGRAM L1M8NM.

       IDENTIFICATION DIVISION.
       CLASS-ID. L1M8NMC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RUNM.
       DATA DIVISION.
       REPORT SECTION.
       PROCEDURE DIVISION.
       MAIN-M.
           CONTINUE.
       END METHOD RUNM.
       END OBJECT.
       END CLASS L1M8NMC.
