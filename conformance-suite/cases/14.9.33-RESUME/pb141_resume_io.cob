*>--- conformance-case
*> id: pb141_resume_io
*> rules: [GR-14.9.33.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB141RS.
      *> kb/Work PB141 - ISO 14.9.33.4 GR3: RESUME AT procedure-name in a
      *> Format-1 USE declarative transfers control there. The file has NO
      *> enabled EC-I-O name, so the verb hook is the PLAIN __IoCheck
      *> (the RESUME statement alone activates the EC model), whose
      *> discarded __RunUse result swallowed the transfer - the program
      *> fell through to the not-reached line.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb141rs-missing.dat"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-PARA.
           DISPLAY "HANDLER=" WS-ST
           RESUME AT RECOVER-PARA.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN.
           OPEN INPUT F
           DISPLAY "NOT-REACHED".
       RECOVER-PARA.
           DISPLAY "RECOVERED"
           STOP RUN.
