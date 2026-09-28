      *> reject-at: 2002 2014 2023
      *> kb/Work PB1273 — ISO/IEC 1989:2023 §13.18.42.3 SR5: "If the PROPERTY clause is specified in a data item
      *> described with the CONSTANT RECORD clause, or in any data item subordinate to a data item described with the
      *> CONSTANT RECORD clause, the SET phrase shall be specified." N lacks WITH NO SET. It compiled clean before.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1273NCM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1273NCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1273NCC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1273NCC "NEW" RETURNING A.
           CONTINUE.
           STOP RUN.
       END PROGRAM PB1273NCM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1273NCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
          05 N PIC 9 VALUE 1 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1273NCC.
