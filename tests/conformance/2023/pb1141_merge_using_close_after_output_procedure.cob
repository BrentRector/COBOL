      *> PB1141 - ISO 14.9.24.4 GR7 c): the as-if CLOSE of each USING
      *>   file "is not performed until after control passes the last
      *>   statement in the output procedure" when an output procedure is
      *>   specified.
      *> cite.py --check 14.9.24.4 "If an output procedure is specified,
      *>   this termination is not performed until after control passes
      *>   the last statement in the output procedure" -> OK  14.9.24.4 7)
      *> Derivation: the as-if READ of GR7 b) leaves each USING file's FILE
      *>   STATUS at the at end condition, 10, and (GR7 c) the CLOSE has
      *>   not happened while the output procedure runs: FA and FB read 10
      *>   after the last RETURN. After the MERGE both have been closed
      *>   successfully: 00. The merged records come out in key order:
      *>   A1 A2 B1 B2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1141.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "pb1141a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FA-ST.
           SELECT FB ASSIGN TO "pb1141b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FB-ST.
           SELECT MRG ASSIGN TO "pb1141m.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 FA-REC PIC X(2).
       FD FB.
       01 FB-REC PIC X(2).
       SD MRG.
       01 MRG-REC.
          05 MRG-K PIC X(2).
       WORKING-STORAGE SECTION.
       01 FA-ST PIC XX VALUE "ZZ".
       01 FB-ST PIC XX VALUE "ZZ".
       01 MORE PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       MAIN.
           OPEN OUTPUT FA
           MOVE "A1" TO FA-REC
           WRITE FA-REC
           MOVE "A2" TO FA-REC
           WRITE FA-REC
           CLOSE FA
           OPEN OUTPUT FB
           MOVE "B1" TO FB-REC
           WRITE FB-REC
           MOVE "B2" TO FB-REC
           WRITE FB-REC
           CLOSE FB
           MERGE MRG ON ASCENDING KEY MRG-K
               USING FA FB
               OUTPUT PROCEDURE IS COLLECT
           DISPLAY "AFTER FA=" FA-ST " FB=" FB-ST
           STOP RUN.
       COLLECT SECTION.
       COLLECT-1.
           PERFORM UNTIL MORE = "N"
               RETURN MRG
                   AT END MOVE "N" TO MORE
                   NOT AT END DISPLAY "REC " MRG-K
               END-RETURN
           END-PERFORM
           DISPLAY "IN-OP FA=" FA-ST " FB=" FB-ST.
