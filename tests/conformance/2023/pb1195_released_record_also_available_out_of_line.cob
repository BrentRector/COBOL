      *> ISO 1989:2023 §14.9.51.4 GR4 (WRITE), §14.9.35.4 GR6 (REWRITE), §14.9.32.4 GR3 (RELEASE) - SAME RECORD AREA.
      *> The three statements say nearly the same thing: the logical record released "is no longer available in the record area unless"
      *> the file is "specified in a SAME RECORD AREA clause. The logical record is also available as a record of
      *> other file-names referenced in the same SAME RECORD AREA clause ..., as well as the file associated with
      *> record-name-1." §12.4.6.4.4 GR2 makes the listed files share one area "for processing the current logical
      *> record", and §12.4.6.4.4 GR2 says the same of a record READ.
      *> A DYNAMIC LENGTH elementary record (§13.18.19, §8.5.1.10) is OUT OF LINE (docs/CONFORMANCE.md D-FRA): it has no
      *> character window over the shared area, so the one-area rule has to be carried out by a store at each
      *> statement. A READ already made the record available in it; WRITE, REWRITE and RELEASE did not.
      *>
      *> WHY EACH LEG CAN FAIL (every expected value is the rule's own text; the contents of the area BEYOND the
      *> released record are not defined by these rules, so the program displays only the released bytes):
      *>   W1  - WRITE of the out-of-line record F1-DYN: the record is also available as F2-REC, a record of F2.
      *>   W2  - WRITE of the character record F2-REC: also available as F1-DYN, a record of F1, at the record's
      *>         own length (8).
      *>   R   - READ of F1, whose every record is out of line: made available in F2-REC, a record of the clause.
      *>   RW  - REWRITE of F1-DYN: also available as F2-REC.
      *>   RW2 - REWRITE of F2-REC: also available as F1-DYN.
      *>   REL - RELEASE of the sort file's record SRT-REC: also available as F3-DYN, the out-of-line record of F3,
      *>         which is in the same clause as the sort file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1195AA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1195a.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "pb1195b.dat" ORGANIZATION IS SEQUENTIAL.
           SELECT SRT ASSIGN TO "pb1195s.dat".
           SELECT F3 ASSIGN TO "pb1195c.dat" ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2.
           SAME RECORD AREA FOR SRT F3.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-DYN PIC X DYNAMIC LENGTH.
       FD  F2.
       01  F2-REC PIC X(8).
       SD  SRT.
       01  SRT-REC PIC X(4).
       FD  F3.
       01  F3-DYN PIC X DYNAMIC LENGTH.
       WORKING-STORAGE SECTION.
       01  WS-LEN PIC 9.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN SECTION.
       M-1.
      *> W1 - WRITE F1-DYN.
           OPEN OUTPUT F1.
           MOVE "HELLO" TO F1-DYN.
           WRITE F1-DYN.
           DISPLAY "W1 " F2-REC (1:5).
           MOVE "AAAAA" TO F1-DYN.
           WRITE F1-DYN.
           CLOSE F1.
      *> W2 - WRITE F2-REC.
           OPEN OUTPUT F2.
           MOVE "WORLD123" TO F2-REC.
           WRITE F2-REC.
           MOVE FUNCTION LENGTH(F1-DYN) TO WS-LEN.
           DISPLAY "W2 [" F1-DYN "] " WS-LEN.
           CLOSE F2.
      *> R and RW - F1 open I-O: READ, then REWRITE the second record.
           OPEN I-O F1.
           READ F1.
           DISPLAY "R F1=[" F1-DYN "] F2=" F2-REC (1:5).
           READ F1.
           MOVE "BBBBB" TO F1-DYN.
           REWRITE F1-DYN.
           DISPLAY "RW F2=" F2-REC (1:5).
      *> RW2 - F2 open I-O beside it: READ then REWRITE F2-REC.
           OPEN I-O F2.
           READ F2.
           MOVE "NEWVALUE" TO F2-REC.
           REWRITE F2-REC.
           DISPLAY "RW2 [" F1-DYN "]".
           CLOSE F2.
           CLOSE F1.
      *> REL - RELEASE under a SAME RECORD AREA clause naming the sort file and F3.
           OPEN OUTPUT F3.
           SORT SRT ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           CLOSE F3.
           DISPLAY "DONE".
           STOP RUN.
       FEED SECTION.
       FEED-1.
           MOVE "KEY1" TO SRT-REC.
           RELEASE SRT-REC.
           DISPLAY "REL [" F3-DYN "]".
       DRAIN SECTION.
       DRAIN-1.
           PERFORM UNTIL WS-EOF = "Y"
               RETURN SRT RECORD
                   AT END MOVE "Y" TO WS-EOF
               END-RETURN
           END-PERFORM.
