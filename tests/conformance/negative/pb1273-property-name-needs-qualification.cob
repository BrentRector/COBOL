      *> reject-at: 2002 2014 2023
      *> kb/Work PB1273 — ISO/IEC 1989:2023 §13.18.42.3 SR3: "The PROPERTY clause may be specified only for an
      *> elementary item whose name does not require qualification for uniqueness of reference." N names two items.
      *> Before the fix the accessor bound the FIRST N in tree order — R1's, the one WITHOUT the clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1273NQM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1273NQC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1273NQC.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1273NQC "NEW" RETURNING A.
           CONTINUE.
           STOP RUN.
       END PROGRAM PB1273NQM.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1273NQC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
          05 N PIC X VALUE "A".
       01 R2.
          05 N PIC 9 VALUE 7 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           CONTINUE.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1273NQC.
