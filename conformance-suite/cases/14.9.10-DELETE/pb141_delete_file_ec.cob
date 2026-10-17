*>--- conformance-case
*> id: pb141_delete_file_ec
*> rules: [GR-14.9.10.4-20]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB141DE.
      *> kb/Work PB141 - ISO 14.9.10.4 GR20 b): with the ON EXCEPTION
      *> phrase written, the enabled level-3 EC-I-O for the status STILL
      *> sets to exist ('41' -> EC-I-O-LOGIC-ERROR, 9.1.13.1); only the
      *> declarative dispatch is suppressed (the phrase is the handler).
      *> The old emitter skipped the whole hook, leaving EXCEPTION-STATUS
      *> stale inside imperative-statement-3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb141de.dat"
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
           DELETE FILE F
               ON EXCEPTION
                   DISPLAY "EXC=" WS-ST " ES=" FUNCTION EXCEPTION-STATUS
           END-DELETE
           CLOSE F
           STOP RUN.
