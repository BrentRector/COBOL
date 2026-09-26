      *> reject-at: 2002 2014 2023
      *> kb/Work PB1137 - ISO 14.9.23.3 SR9: identifier-3 (a BY REFERENCE
      *> INVOKE argument) shall be an address-identifier or a data item
      *> defined in the file, working-storage, local-storage or linkage
      *> section. PAGE-COUNTER is a special register (8.4.3.15.4 GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1137N1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1137N1K.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "pb1137n1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1137N1K.
       01 N4 PIC 9(4) VALUE 0.
       REPORT SECTION.
       RD RPT.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 DC COLUMN 1 PIC 9(4) SOURCE N4.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1137N1K "NEW" RETURNING O.
           OPEN OUTPUT RPTF.
           INITIATE RPT.
           INVOKE O "TAKEPC" USING BY REFERENCE PAGE-COUNTER.
           TERMINATE RPT.
           CLOSE RPTF.
           STOP RUN.
       END PROGRAM PB1137N1.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1137N1K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEPC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP PIC 9(4).
       PROCEDURE DIVISION USING LP.
           MOVE 5 TO LP.
       END METHOD TAKEPC.
       END OBJECT.
       END CLASS PB1137N1K.
