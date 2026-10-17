*>--- conformance-case
*> id: pb140_delete_file_gate
*> rules: [GR-9.1.13.7-1, GR-14.9.10.4-2, GR-14.9.10.4-13,
*>    GR-14.9.10.4-16, GR-14.9.10.4-20]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140DF.
      *> kb/Work PB140 - ISO 14.9.10.4 GR13: DELETE FILE of an OPEN
      *> connector is '41'; it is an executed I-O statement, so a following
      *> sequential-access DELETE RECORD is '43' (9.1.13.7 3). After the
      *> CLOSE the deletion succeeds ('00', GR14ff) and a second DELETE
      *> FILE finds the file ABSENT - the SUCCESSFUL '05' (GR14).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb140df.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
           READ F AT END CONTINUE END-READ
           DISPLAY "READ1=" WS-ST
           DELETE FILE F
           DISPLAY "DF41=" WS-ST
           DELETE F RECORD
           DISPLAY "DEL43=" WS-ST
           CLOSE F
           DELETE FILE F
           DISPLAY "DF00=" WS-ST
           DELETE FILE F
           DISPLAY "DF05=" WS-ST
           STOP RUN.
