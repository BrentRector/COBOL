      *> kb/Work PB1036 -- the sort-merge STATEMENT exception conditions, CHECKED.
      *> ISO 14.9.40.4 GR9 a)/c): "If the file referenced by file-name-2 [-3] is in an
      *> open mode when this phase commences, the EC-SORT-MERGE-FILE-OPEN exception
      *> condition is set to exist". 14.9.24.4 GR7/GR12: at the start of execution of
      *> the MERGE statement no USING/GIVING file shall be in the open mode, "otherwise
      *> the EC-SORT-MERGE-FILE-OPEN exception condition is set to exist and the
      *> execution of the MERGE statement terminates". 14.9.24.4 GR6: USING records
      *> not in KEY order -> EC-SORT-MERGE-SEQUENCE. 14.9.40.4 GR10: a format 1 SORT
      *> executed in the range of an input procedure -> EC-SORT-MERGE-ACTIVE.
      *> 13.18.43.4 GR14 b): a RELEASE whose record size is outside the RECORD
      *> VARYING range -> EC-SORT-MERGE-RELEASE, the RELEASE is unsuccessful.
      *> 14.9.40.4 GR12 b): a USING record larger than the largest record of the
      *> sort file -> EC-SORT-MERGE-RELEASE, the SORT is terminated.
      *> All are Table 13 Fatal. 14.6.13.1.3 2): "If the executed statement is a
      *> MERGE or SORT statement, then the rules for those statements apply" -- the
      *> declarative runs and the statement is terminated; the run unit continues
      *> (docs/CONFORMANCE.md 3, D-SMA), so D-V completes NORMALLY and still returns.
      *> The EC-I-O family now reaches the implicit transfers too (GR12 a).
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, in execution order --
      *>   SM=EC-SORT-MERGE-FILE-OPEN  A: G-OUT is open when phase c) commences;
      *>                               FEED-A released QQQ, nothing is written.
      *>   A GS=00                     the test precedes the implicit OPEN, so GS
      *>                               still holds the program's own OPEN status.
      *>   SM=EC-SORT-MERGE-FILE-OPEN  B: M-A is open at the start of the MERGE.
      *>   B
      *>   SM=EC-SORT-MERGE-SEQUENCE   C: M-A holds BBB then AAA; OUT-C never runs.
      *>   C
      *>   SM=EC-SORT-MERGE-ACTIVE     E: FEED-E PERFORMs INNER-E, which executes a
      *>                               SORT (GR10's range is the EXECUTED one - a
      *>                               SORT WRITTEN in the input procedure is
      *>                               refused at compile time, 14.9.40.3 SR3, so
      *>                               the inner SORT lives in a paragraph outside
      *>                               it); the inner SORT is terminated (FEED-2
      *>                               never runs) and the declarative completes
      *>                               normally.
      *>   E-IN                        FEED-E continues and releases AAA.
      *>   R=AAA / R=BBB               the OUTER sort is intact.
      *>   E
      *>   F0                          a RELEASE of a 3-byte record outside any SORT:
      *>                               EC-FLOW-RELEASE is not enabled, 3 is in range.
      *>   RS=EC-SORT-MERGE-RELEASE    F: VLEN 9 is outside 2 TO 5.
      *>   F
      *>   RS=EC-SORT-MERGE-RELEASE    G: W5's 5-byte record into the 3-byte SF.
      *>   G WS=00                     the terminated SORT left W5 open (CONFORMANCE
      *>                               3, the implicit-transfer determination (d)).
      *>   IO=EC-I-O-PERMANENT-ERROR   H: the implicit OPEN of the missing file '35'.
      *>   H US=35
      >>TURN EC-SORT-MERGE EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1036SM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G-OUT ASSIGN TO "pb1036-g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GS.
           SELECT M-A ASSIGN TO "pb1036-a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT M-B ASSIGN TO "pb1036-b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT W5 ASSIGN TO "pb1036-w5.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS WS5.
           SELECT U-MISS ASSIGN TO "pb1036-missing.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS US.
           SELECT SF ASSIGN TO "pb1036-f.srt".
           SELECT S2 ASSIGN TO "pb1036-2.srt".
           SELECT VF ASSIGN TO "pb1036-v.srt".
       DATA DIVISION.
       FILE SECTION.
       FD G-OUT.
       01 G-REC PIC X(3).
       FD M-A.
       01 A-REC PIC X(3).
       FD M-B.
       01 B-REC PIC X(3).
       FD W5.
       01 W5-REC PIC X(5).
       FD U-MISS.
       01 U-REC PIC X(3).
       SD SF.
       01 SF-REC PIC X(3).
       SD S2.
       01 S2-REC PIC X(3).
       SD VF RECORD IS VARYING IN SIZE FROM 2 TO 5 CHARACTERS
             DEPENDING ON VLEN.
       01 VF-REC.
          05 VF-K PIC X(2).
          05 FILLER PIC X(3).
       WORKING-STORAGE SECTION.
       01 GS    PIC XX.
       01 WS5   PIC XX.
       01 US    PIC XX.
       01 VLEN  PIC 99.
       01 WS-EOF PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-V SECTION.
           USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-FILE-OPEN
               EC-SORT-MERGE-SEQUENCE EC-SORT-MERGE-ACTIVE.
       D-V-P.
           DISPLAY "SM=" FUNCTION EXCEPTION-STATUS.
       D-R SECTION.
           USE AFTER EXCEPTION CONDITION EC-SORT-MERGE-RELEASE.
       D-R-P.
           DISPLAY "RS=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       D-IO SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O.
       D-IO-P.
           DISPLAY "IO=" FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT M-A M-B W5.
           MOVE "BBB" TO A-REC.
           WRITE A-REC.
           MOVE "AAA" TO A-REC.
           WRITE A-REC.
           MOVE "CCC" TO B-REC.
           WRITE B-REC.
           MOVE "ZZZZZ" TO W5-REC.
           WRITE W5-REC.
           CLOSE M-A M-B W5.
      *> A -- SORT GIVING a file the program holds open.
           OPEN OUTPUT G-OUT.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS FEED-A GIVING G-OUT.
           DISPLAY "A GS=" GS.
           CLOSE G-OUT.
      *> B -- MERGE USING a file the program holds open.
           OPEN INPUT M-A.
           MERGE S2 ON ASCENDING KEY S2-REC
               USING M-A M-B GIVING G-OUT.
           DISPLAY "B".
           CLOSE M-A.
      *> C -- MERGE USING a file out of KEY order.
           MERGE S2 ON ASCENDING KEY S2-REC
               USING M-A M-B OUTPUT PROCEDURE IS OUT-C.
           DISPLAY "C".
      *> E -- a SORT executed in the range of an input procedure.
           MOVE 0 TO WS-EOF.
           SORT SF ON ASCENDING KEY SF-REC
               INPUT PROCEDURE IS FEED-E OUTPUT PROCEDURE IS OUT-E.
           DISPLAY "E".
      *> F -- RELEASE statements, in range and out of range.
           MOVE "BBBBB" TO VF-REC.
           MOVE 3 TO VLEN.
           RELEASE VF-REC.
           DISPLAY "F0".
           MOVE "CCCCC" TO VF-REC.
           MOVE 9 TO VLEN.
           RELEASE VF-REC.
           DISPLAY "F".
      *> G -- the implicit USING release of an over-long record.
           SORT SF ON ASCENDING KEY SF-REC
               USING W5 OUTPUT PROCEDURE IS OUT-C.
           CLOSE W5.
           DISPLAY "G WS=" WS5.
      *> H -- EC-I-O on the implicit OPEN of a USING file.
           SORT SF ON ASCENDING KEY SF-REC
               USING U-MISS OUTPUT PROCEDURE IS OUT-C.
           DISPLAY "H US=" US.
           STOP RUN.
       FEED-A.
           MOVE "QQQ" TO SF-REC.
           RELEASE SF-REC.
       FEED-E.
           MOVE "BBB" TO SF-REC.
           RELEASE SF-REC.
           PERFORM INNER-E.
           DISPLAY "E-IN".
           MOVE "AAA" TO SF-REC.
           RELEASE SF-REC.
       FEED-2.
           DISPLAY "FEED-2 MUST NOT RUN".
       OUT-E.
           PERFORM UNTIL WS-EOF = 1
               RETURN SF
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "R=" SF-REC
               END-RETURN
           END-PERFORM.
       OUT-C.
           DISPLAY "OUT-C MUST NOT RUN".
       INNER-E.
           SORT S2 ON ASCENDING KEY S2-REC
               INPUT PROCEDURE IS FEED-2 OUTPUT PROCEDURE IS OUT-C.
