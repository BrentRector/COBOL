      *> reject-at: 2002 2014 2023
      *> kb/Work PB1503 - ISO/IEC 1989:2023 section 11.7.3 SR7: "If the SET phrase is specified, then the method shall
      *> have a single USING parameter specified in the procedure division header and no RETURNING phrase."
      *>   cite.py: OK  11.7.3 7)
      *> The set property method names two USING parameters.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1503A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-BAL PIC 9(4).
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A PIC 9(4).
       01 LK-B PIC 9(4).
       PROCEDURE DIVISION USING LK-A LK-B.
       MAIN.
           MOVE LK-A TO W-BAL.
       END METHOD.
       END OBJECT.
       END CLASS PB1503A.
