      *> reject-at: 2002 2014 2023
      *> ISO 13.18.58.3 SR1: 'If the subject of the entry is an elementary
      *>   item, the STRONG phrase shall not be specified.' T TYPEDEF STRONG
      *>   TYPE E is elementary by 13.18.57.4 GR1 (E is PIC X(3)), exactly as
      *>   T TYPEDEF STRONG PIC X(3) is (kb/Work PB1301).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1301NEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E TYPEDEF PIC X(3).
       01 T TYPEDEF STRONG TYPE E.
       01 V TYPE T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "UNREACHABLE"
           STOP RUN.
