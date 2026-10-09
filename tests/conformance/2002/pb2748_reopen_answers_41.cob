      *> kb/Work PB2748 - ISO 14.9.27.4 GR2: an OPEN of a file
      *> connector that is already open is unsuccessful with '41',
      *> and GR25: the file is not affected. GR2 precedes the
      *> sharing rules (docs/CONFORMANCE.md DOC-A.1-104), so a
      *> re-OPEN that a sibling connector's READ ONLY sharing mode
      *> would refuse is still '41', never Table 19's '61', and the
      *> still-open connector keeps reading where it was.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2748REOPEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2748ro.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT G ASSIGN TO "pb2748ro.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 GS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           MOVE "BBBB" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN INPUT F
           DISPLAY "F OPEN INPUT " FS
           READ F
           DISPLAY "F READ " FS " " F-REC
           OPEN INPUT G
           DISPLAY "G OPEN INPUT " GS
           OPEN I-O F
           DISPLAY "F RE-OPEN I-O " FS
           OPEN EXTEND SHARING WITH NO OTHER F
           DISPLAY "F RE-OPEN EXTEND " FS
           READ F
           DISPLAY "F READ " FS " " F-REC
           CLOSE F
           DISPLAY "F CLOSE " FS
           CLOSE G
           DISPLAY "G CLOSE " GS
           STOP RUN.
