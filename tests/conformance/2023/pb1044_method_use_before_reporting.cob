      *> PB1044 - ISO 14.9.49.4 GR4 a) / GR8 - a METHOD's own USE BEFORE
      *>   REPORTING declarative runs just before its group is produced.
      *> cite.py --check 14.9.49.4 "The declarative is invoked just before
      *>   the named report group is produced" -> OK  14.9.49.4 8)
      *> cite.py --check 13.8.3 "Within a class definition, the report
      *>   section may be specified only in a factory definition or an in"
      *>   -> OK  13.8.3 1)  (the RD in the instance definition is legal)
      *> Derivation: method M GENERATEs DL, so its declarative D-BR is
      *>   invoked before DL is produced: BEFORE-REPORTING-RAN, then the
      *>   method's own DISPLAY: M-DONE.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1044K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb1044.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS RR.
       WORKING-STORAGE SECTION.
       01 W PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD RR PAGE LIMIT 20.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(3) SOURCE W.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BR SECTION.
           USE BEFORE REPORTING DL.
       D-BR-P.
           DISPLAY "BEFORE-REPORTING-RAN".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT RPTF
           INITIATE RR
           GENERATE DL
           TERMINATE RR
           CLOSE RPTF
           DISPLAY "M-DONE"
           GOBACK.
       END METHOD M.
       END OBJECT.
       END CLASS PB1044K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1044M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1044K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1044K.
       PROCEDURE DIVISION.
           INVOKE PB1044K "NEW" RETURNING O
           INVOKE O "M"
           STOP RUN.
       END PROGRAM PB1044M.
