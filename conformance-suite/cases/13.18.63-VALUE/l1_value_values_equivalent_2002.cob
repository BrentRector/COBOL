      *>--- conformance-case
      *> id: l1_value_values_equivalent_2002
      *> rules: [SR-13.18.63.3-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.63.3 SR17 — "The words VALUE and VALUES are
      *> equivalent." — THE GATE-BOUNDARY COPY, compiled at --std 2002.
      *>
      *> The rule is printed in the FORMAT 2 (table) block and SR24
      *> ("Syntax rules 10 and 17 above apply") imports it into
      *> FORMAT 3. Format 2 is a COBOL-2002 introduction, so 2002 is
      *> the EARLIEST edition this row claims: this copy is what goes
      *> red if the Format-2 gate ever drifts upward to 2014/2023, and
      *> the 2023 copy (l1_value_values_equivalent) is what goes red if
      *> the equivalence itself drifts at the default edition. The two
      *> assert the same four lines because the words are ONE token
      *> position with no version predicate on either side.
      *>
      *> DERIVED EXPECTATIONS — from the rule text, not from a run:
      *>  T1 §13.18.63.4 GR12 initializes the element named by
      *>     subscript-1 (= 1) to "AB"; GR13 reuses the literal until
      *>     the element named by subscript-2 (= 3) is initialized, so
      *>     all three elements hold "AB"  =>  "ABABAB".
      *>  T2 Two literals over four elements: GR12 gives element 1
      *>     "A" and element 2 "B"; GR13 reuses the list in order
      *>     through element 4  =>  "ABAB".
      *>  C-IN / C-OUT §8.8.4.5.3 GR3 — the test is true if one of the
      *>     values of the condition-name equals its conditional
      *>     variable. CV = 3 is one of {1,2,3} => T; 7 is not => F.
      *>
      *> §5.2.3 (Optional words) makes IS and ARE optional, so G-C vs
      *> G-D differ in EXACTLY the one word the rule is about.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VAL72.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
      *> Format 2, one literal over three elements — four legal heads.
       01  G-A.
           05  TA PIC X(2) OCCURS 3 VALUE IS "AB" FROM (1) TO (3).
       01  G-B.
           05  TB PIC X(2) OCCURS 3 VALUES ARE "AB" FROM (1) TO (3).
       01  G-C.
           05  TC PIC X(2) OCCURS 3 VALUE "AB" FROM (1) TO (3).
       01  G-D.
           05  TD PIC X(2) OCCURS 3 VALUES "AB" FROM (1) TO (3).
      *> Format 2, a two-literal list reused over four elements.
       01  G-E.
           05  TE PIC X OCCURS 4 VALUE "A" "B" FROM (1) TO (4).
       01  G-F.
           05  TF PIC X OCCURS 4 VALUES "A" "B" FROM (1) TO (4).
      *> Format 3 (SR24 imports SR17) — the same list, both words.
       01  CVA PIC 9 VALUE 3.
           88  CNA VALUE 1 2 3.
       01  CVB PIC 9 VALUE 3.
           88  CNB VALUES 1 2 3.
       01  R-A PIC X VALUE "F".
       01  R-B PIC X VALUE "F".
       PROCEDURE DIVISION.
       MAIN-P.
           IF G-A = G-B AND G-A = G-C AND G-A = G-D
               DISPLAY "SR17-T1=OK " G-A
           ELSE
               DISPLAY "SR17-T1=BAD " G-A " " G-B " " G-C " " G-D
           END-IF
           IF G-E = G-F
               DISPLAY "SR17-T2=OK " G-E
           ELSE
               DISPLAY "SR17-T2=BAD " G-E " " G-F
           END-IF
           MOVE "F" TO R-A
           MOVE "F" TO R-B
           IF CNA MOVE "T" TO R-A END-IF
           IF CNB MOVE "T" TO R-B END-IF
           IF R-A = R-B
               DISPLAY "SR17-C-IN=OK " R-A
           ELSE
               DISPLAY "SR17-C-IN=BAD " R-A " " R-B
           END-IF
           MOVE 7 TO CVA
           MOVE 7 TO CVB
           MOVE "F" TO R-A
           MOVE "F" TO R-B
           IF CNA MOVE "T" TO R-A END-IF
           IF CNB MOVE "T" TO R-B END-IF
           IF R-A = R-B
               DISPLAY "SR17-C-OUT=OK " R-A
           ELSE
               DISPLAY "SR17-C-OUT=BAD " R-A " " R-B
           END-IF
           STOP RUN.
