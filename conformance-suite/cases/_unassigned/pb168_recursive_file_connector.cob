      *>--- conformance-case
      *> id: pb168_recursive_file_connector
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB168R RECURSIVE.
      *> kb/Work PB168 - A RECURSIVE UNIT'S INTERNAL FILE CONNECTORS ARE
      *> UNIT-SCOPED, LAST-USED STATE. 14.6.2.3.2 PUTS INTERNAL FILE
      *> CONNECTORS "TO NOT BE IN ANY OPEN MODE" (ACTION 3) ONLY WHEN
      *> DATA IS PLACED IN THE INITIAL STATE - FOR A NON-INITIAL UNIT'S
      *> STATIC DATA THAT IS CASES 1-3 (RUN-UNIT START / INITIAL
      *> CONTAINER / AFTER CANCEL) - AND 14.6.2.3.3 KEEPS THEM LAST-USED
      *> OTHERWISE. SO THE DEPTH-2 ACTIVATION MUST FIND THE CONNECTOR
      *> DEPTH 1 OPENED, STILL OPEN, AND WRITE THROUGH IT; THE RESUMED
      *> DEPTH-1 ACTIVATION MUST FIND ITS OWN CONNECTOR INTACT. BEFORE
      *> THE FIX EVERY FRESH RECURSIVE INSTANCE RE-REGISTERED AND
      *> SILENTLY REPLACED THE LIVE CONNECTOR - THE DEPTH-2 WRITE
      *> ANSWERED '42' AND THE DISPLACED WRITER'S BUFFER WAS LOST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb168r.dat"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 DEPTH PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO DEPTH
           IF DEPTH = 1
               OPEN OUTPUT F
               MOVE "D1" TO F-REC
               WRITE F-REC
               DISPLAY "W1=" FS
               CALL "PB168R"
               MOVE "D3" TO F-REC
               WRITE F-REC
               DISPLAY "W3=" FS
               CLOSE F
               OPEN INPUT F
               PERFORM 3 TIMES
                   READ F
                   DISPLAY F-REC " " FS
               END-PERFORM
               CLOSE F
           ELSE
               MOVE "D2" TO F-REC
               WRITE F-REC
               DISPLAY "W2=" FS
           END-IF
           GOBACK.
       END PROGRAM PB168R.
