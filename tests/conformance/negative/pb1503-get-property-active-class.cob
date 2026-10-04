      *> reject-at: 2002 2014 2023
      *> kb/Work PB1503 - ISO/IEC 1989:2023 section 11.7.3 SR6: "If the GET phrase is specified, ... The returning item
      *> shall not be an object reference described with the ACTIVE-CLASS phrase."
      *>   cite.py: OK  11.7.3 6)
      *> The FACTORY's get property method returns an ACTIVE-CLASS object reference (the factory arm of the rule).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1503B INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           SET LK-R TO NULL.
       END METHOD.
       END FACTORY.
       END CLASS PB1503B.
