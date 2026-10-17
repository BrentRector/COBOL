*>--- conformance-case
*> id: delete_file
*> rules: [DOC-A.1-51]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.10 (COBOL-2023) — DELETE FILE deletes the physical file. After deleting, OPEN INPUT of the
      *> same file reports "file not available" (I-O status 35).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DELFILE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "df-conf.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "HELLO" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           DELETE FILE F.
           OPEN INPUT F.
           DISPLAY "ST=" WS-ST.
           STOP RUN.
