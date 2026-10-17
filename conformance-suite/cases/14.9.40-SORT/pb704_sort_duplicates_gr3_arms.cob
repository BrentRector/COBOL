*>--- conformance-case
*> id: pb704_sort_duplicates_gr3_arms
*> rules: [GR-14.9.40.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB704 - the OTHER TWO ARMS of ISO 14.9.40.4 GR3, the rule the DUPLICATES phrase exists to
      *> buy.  The phrase is ONE grammar rule serving BOTH general formats (14.9.40.2 Format 1 file sort and
      *> Format 2 table sort), so a fix proven only on the arm the defect was reported against is the shape
      *> this codebase reproduces most often; pb704_sort_duplicates_in_order.cob pins GR3 b) (the input
      *> procedure) and this program pins the remaining two.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *>   GR3 a) - "The order of the associated input files as specified in the SORT statement.  Within a
      *>            given input file the order is that in which the records are accessed from that file."
      *>            USING names I1 then I2; I1 holds B1 A1, I2 holds B2 A2.  ASCENDING on the one-character
      *>            key (GR8 a): the A group first, and within it I1's A1 before I2's A2; then B1 then B2.
      *>            => USE=A1 A2 B1 B2
      *>   GR3 c) - "The relative order of the contents of these table elements before sorting takes place."
      *>            The table holds B1 A1 B2 A2 in that order; ASCENDING on T-KEY gives the A group first,
      *>            each group keeping its pre-sort relative order.
      *>            => TBL=A1 A2 B1 B2
      *> Without the DUPLICATES phrase GR4 leaves both orders UNDEFINED - which is why a compiler that
      *> rejects the phrase leaves a program no conforming way to ask for either.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB704GR3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT I1 ASSIGN TO "pb704gr3a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT I2 ASSIGN TO "pb704gr3b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT O1 ASSIGN TO "pb704gr3o.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT S1 ASSIGN TO "pb704gr3s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  I1.
       01  I1-REC PIC XX.
       FD  I2.
       01  I2-REC PIC XX.
       FD  O1.
       01  O1-REC PIC XX.
       SD  S1.
       01  S-REC.
           05  S-KEY PIC X.
           05  S-TAG PIC X.
       WORKING-STORAGE SECTION.
       01  W-I    PIC 9 VALUE 0.
       01  W-EOF  PIC X VALUE "N".
       01  W-SEED.
           05  FILLER PIC XX VALUE "B1".
           05  FILLER PIC XX VALUE "A1".
           05  FILLER PIC XX VALUE "B2".
           05  FILLER PIC XX VALUE "A2".
       01  W-SEED-R REDEFINES W-SEED.
           05  W-PAIR PIC XX OCCURS 4 TIMES.
       01  W-TBL.
           05  W-ELEM OCCURS 4 TIMES.
               10  T-KEY PIC X.
               10  T-TAG PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT I1
           MOVE W-PAIR (1) TO I1-REC WRITE I1-REC
           MOVE W-PAIR (2) TO I1-REC WRITE I1-REC
           CLOSE I1
           OPEN OUTPUT I2
           MOVE W-PAIR (3) TO I2-REC WRITE I2-REC
           MOVE W-PAIR (4) TO I2-REC WRITE I2-REC
           CLOSE I2
           SORT S1 ON ASCENDING KEY S-KEY
               WITH DUPLICATES IN ORDER
               USING I1 I2 GIVING O1
           OPEN INPUT O1
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ O1 AT END MOVE "Y" TO W-EOF
                   NOT AT END DISPLAY "USE=" O1-REC
               END-READ
           END-PERFORM
           CLOSE O1
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 4
               MOVE W-PAIR (W-I) TO W-ELEM (W-I)
           END-PERFORM
           SORT W-ELEM ON ASCENDING KEY T-KEY
               WITH DUPLICATES IN ORDER
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 4
               DISPLAY "TBL=" W-ELEM (W-I)
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
