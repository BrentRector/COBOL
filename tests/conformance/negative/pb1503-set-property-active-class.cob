      *> reject-at: 2002 2014 2023
      *> kb/Work PB1503 - ISO/IEC 1989:2023 section 11.7.3 SR7: "If the SET phrase is specified, ... The USING parameter
      *> shall not be an object reference described with the ACTIVE-CLASS phrase."
      *>   cite.py: OK  11.7.3 7)
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1503C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY PEER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-P USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK-P.
       MAIN.
           CONTINUE.
       END METHOD.
       END OBJECT.
       END CLASS PB1503C.
