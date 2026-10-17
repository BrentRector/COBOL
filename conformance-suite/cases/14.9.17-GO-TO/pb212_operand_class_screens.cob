      *>--- conformance-case
      *> id: pb212_operand_class_screens
      *> rules: [SR-14.9.17.3-1, SR-14.9.37.3-5, SR-14.9.39.3-1,
      *>    SR-14.9.39.3-2, SR-14.9.39.3-3, SR-14.9.39.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.17.3 SR1, 14.9.37.3 SR5, 14.9.39.3 SR1-SR4 and 14.9.28.3
      *> SR2: the LEGAL shape of every class-closed operand position the
      *> ONE operand-class screen guards (kb/Work PB210, PB211, PB212).
      *> Expected values from the general rules: 14.9.17.4 GR2 (selector
      *> 1..n picks the n-th name), 14.9.37.4 GR3 b) (an integer VARYING
      *> item is incremented by one with the search index; an index data
      *> item by the same amount), 14.9.39.4 GR2 (index-name receiver
      *> from an integer operand; numeric receiver from index-name-2 gets
      *> the occurrence number), 14.9.28.4 (a scaled induction variable).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB212P85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 OCCURS 5 INDEXED BY IX1 PIC X.
       01 T2.
          05 E2 OCCURS 5 INDEXED BY IX2 PIC 9.
       01 SEL  PIC 9 VALUE 2.
       01 SELB PIC S9(4) COMP VALUE 3.
       01 N    PIC 9(4).
       01 VI   PIC 9(2) VALUE 0.
       01 IXD  USAGE INDEX.
       01 IXD2 USAGE INDEX.
       01 PV   PIC 9V9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCDE" TO T1.
           MOVE "12345" TO T2.
           GO TO G1 G2 DEPENDING ON SEL.
           DISPLAY "GO-FELL".
       G1.
           DISPLAY "GO-1".
           GO TO G3.
       G2.
           DISPLAY "GO-2".
       G3.
           GO TO H1 H2 H3 DEPENDING ON SELB.
           DISPLAY "GOB-FELL".
       H1.
           DISPLAY "GOB-1".
           GO TO K0.
       H2.
           DISPLAY "GOB-2".
           GO TO K0.
       H3.
           DISPLAY "GOB-3".
       K0.
           SET IX1 TO 1.
           MOVE 0 TO VI.
           SEARCH E1 VARYING VI
               AT END DISPLAY "S1-END"
               WHEN E1 (IX1) = "C" DISPLAY "S1-HIT " VI
           END-SEARCH.
           SET IX1 TO 1.
           SET IXD TO IX1.
           SEARCH E1 VARYING IXD
               AT END DISPLAY "S2-END"
               WHEN E1 (IX1) = "D" CONTINUE
           END-SEARCH.
           SET IX1 TO 1.
           SET IX1 TO IXD.
           DISPLAY "S2-HIT " E1 (IX1).
           SET IX2 TO 4.
           SET N TO IX2.
           DISPLAY "SET-N " N.
           SET IXD2 TO IX2.
           SET IXD TO IXD2.
           SET IX2 TO 1.
           SET IX2 TO IXD.
           DISPLAY "SET-IXD " E2 (IX2).
           MOVE 2 TO N.
           SET IX2 TO N.
           DISPLAY "SET-IXN " E2 (IX2).
           PERFORM VARYING PV FROM 0.5 BY 0.5 UNTIL PV > 1.2
               DISPLAY "PV " PV
           END-PERFORM.
           STOP RUN.
