*>--- conformance-case
*> id: pb133_returning_pointer
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB133 wave B - ISO 14.2.3 GR7 over POINTER carriers: a program-pointer and a data-pointer
      *> RETURNING item each deliver through their own typed lane (the old ABI had numeric/string lanes
      *> only, and LEGAL source drew backend CS1503 - the PB111 shape). The program-pointer comes back
      *> non-null (SET ... TO ENTRY of the OUTERMOST program, 8.4.3.13 GR1); the data-pointer stays NULL
      *> (the callee never sets it - 13.18.63 initial state). Derived: PP-SET, DP-NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPRET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       01 DP USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PPS" AS NESTED RETURNING PP
           CALL "DPS" AS NESTED RETURNING DP
           IF PP NOT = NULL DISPLAY "PP-SET" END-IF
           IF DP = NULL DISPLAY "DP-NULL" END-IF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PPS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION RETURNING L-P.
       P.
           SET L-P TO ENTRY "PPRET"
           GOBACK.
       END PROGRAM PPS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DPS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-D USAGE POINTER.
       PROCEDURE DIVISION RETURNING L-D.
       P.
           GOBACK.
       END PROGRAM DPS.
       END PROGRAM PPRET.
