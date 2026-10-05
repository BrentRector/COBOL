      *> reject-at: 85 2002 2014 2023
      *> RECORD ... DEPENDING ON AN ITEM OF THE FD'S OWN RECORD (kb/Work PB858).
      *> ISO/IEC 1989:2023 §13.18.43.3 SR6: "Data-name-1 shall describe an elementary
      *> unsigned integer in the working-storage, local-storage, or linkage section."
      *> F-LEN is an elementary unsigned integer, but it is described in the FILE
      *> section (inside F-REC), so the third obligation is broken. The rule is in every
      *> edition (Annex E lists no change to the RECORD clause). Refused, COBOLNET2922.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB858DR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb858dr.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD IS VARYING IN SIZE FROM 1 TO 20
           DEPENDING ON F-LEN.
       01 F-REC.
          05 F-LEN PIC 9(2).
          05 F-DATA PIC X(18).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
