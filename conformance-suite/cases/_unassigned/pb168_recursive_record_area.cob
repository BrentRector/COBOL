      *>--- conformance-case
      *> id: pb168_recursive_record_area
      *> rules: []
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB168RA RECURSIVE.
      *> kb/Work PB168 (the review fleet's 8.6.4 finding) - A RECURSIVE
      *> UNIT'S FD RECORD AREA IS STATIC, ONE COPY PER RUN UNIT, LIKE
      *> ITS CONNECTOR: 8.6.4 "DATA ITEMS AND FILE CONNECTORS DEFINED
      *> IN THE WORKING-STORAGE OR FILE SECTION OF A SOURCE ELEMENT
      *> THAT IS NOT AN INITIAL PROGRAM ARE STATIC ITEMS" AND "FOR
      *> STATIC ITEMS THAT ARE NOT OBJECT DATA, THERE IS ONE COPY IN A
      *> RUN UNIT". SO THE INNER ACTIVATION'S READ LEAVES ITS RECORD IN
      *> THE ONE SHARED AREA AND THE RESUMED OUTER ACTIVATION SEES IT.
      *> A PER-ACTIVATION AREA (THE PRE-FIX EMISSION) SHOWS THE OUTER
      *> ACTIVATION'S STALE FIRST RECORD INSTEAD - A SILENT WRONG
      *> ANSWER THE CONNECTOR-ONLY HALF OF THE FIX WOULD HAVE SHIPPED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb168ra.dat"
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
               MOVE "D2" TO F-REC
               WRITE F-REC
               CLOSE F
               OPEN INPUT F
               READ F
               DISPLAY "R1=" F-REC " " FS
               CALL "PB168RA"
               DISPLAY "OUTER=" F-REC
               CLOSE F
           ELSE
               READ F
               DISPLAY "INNER=" F-REC " " FS
           END-IF
           GOBACK.
       END PROGRAM PB168RA.
