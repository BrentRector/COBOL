*>--- conformance-case
*> id: exception_file_arg
*> rules: [RV-15.28.4-2, FMT-15.28.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> FUNCTION EXCEPTION-FILE(file-connector-name) — the COBOL-2023 file-connector
      *> argument form (ISO §15.28.4 r2; Annex E.3.3 item 25). It reports the NAMED
      *> connector's current I-O status + SELECT-spelled file-name (r2b), or two
      *> alphanumeric spaces when the connector was never opened/attempted/accessed
      *> (r2a) — unlike the no-argument form, which reports the LAST exception. Here
      *> INF is written, then read to end-of-file (I-O status '10'), so
      *> EXCEPTION-FILE(INF) is "10INF"; UNUSED is only SELECTed, never touched, so
      *> EXCEPTION-FILE(UNUSED) is two spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXC-FILE-ARG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INF ASSIGN TO "excfarg.txt" FILE STATUS IS WS-FS.
           SELECT UNUSED ASSIGN TO "excfargu.txt".
       DATA DIVISION.
       FILE SECTION.
       FD INF.
       01 RINF PIC X(5).
       FD UNUSED.
       01 RUNU PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT INF.
           WRITE RINF FROM "HELLO".
           CLOSE INF.
           OPEN INPUT INF.
           READ INF AT END CONTINUE END-READ.
           READ INF AT END CONTINUE END-READ.
           DISPLAY "INF=[" FUNCTION EXCEPTION-FILE(INF) "]".
           DISPLAY "UNU=[" FUNCTION EXCEPTION-FILE(UNUSED) "]".
           CLOSE INF.
           STOP RUN.
