*>--- conformance-case
*> id: l1c32_relation_national_mixed
*> rules: [GR-8.8.4.2.1-5, GR-8.8.4.2.1-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.2.1 5) 7) — national-national and national-alphanumeric/alphabetic relations
      *>
      *> THE RULES. §8.8.4.2.1: "Comparisons are defined for the following:"
      *>   5) "Two operands of class national."
      *>   7) "Two operands of different classes where each operand is from the set of
      *>      classes alphanumeric, alphabetic, or national."
      *> and, same clause: "A national group item or a bit group item shall be treated as
      *> an elementary national data item" ... and "A class alphabetic operand shall be
      *> treated as though it were an operand of class alphanumeric."
      *> §8.8.4.2.6: "The alphanumeric operand is treated as though it were converted and
      *> moved in accordance with the rules of the MOVE statement from an alphanumeric
      *> elementary data item to a temporary elementary data item of class national with
      *> the same length in terms of character positions as the alphanumeric operand."
      *> §8.8.4.2.10 (standard comparison; no PROGRAM COLLATING SEQUENCE is declared and
      *> only SPACE and A-D are compared, ordered SPACE < A < B < C < D):
      *>   1) "The operand that contains the character that is positioned higher in the
      *>      national collating sequence is the greater operand."
      *>   2) "comparison proceeds as though the shorter operand were extended on the
      *>      right by sufficient national spaces".
      *>
      *> cite.py (NOTE PB1554: cite.py labels the unnumbered national-group sentence of
      *> §8.8.4.2.1 as list item "13)"; it is the paragraph after item 13):
      *>   OK  §8.8.4.2.1 5)  (General)
      *>   OK  §8.8.4.2.1 7)  (General)
      *>   OK  §8.8.4.2.1 13)  (General)  [the group/alphabetic/national-group sentence]
      *>   OK  §8.8.4.2.6   (Comparison of alphanumeric and national operands)
      *>   OK  §8.8.4.2.10 1)  (Standard comparison)
      *>   OK  §8.8.4.2.10 2)  (Standard comparison)
      *>
      *> Directory 2002: class national (PIC N, N"..", GROUP-USAGE NATIONAL) enters
      *> with ISO/IEC 1989:2002.
      *>
      *> DERIVATION of every expected line (T = condition true, F = false):
      *>  item 5 (national vs national)
      *>   R5-1 N3 "ABC" < N3D "ABD": 3rd C < D -> T
      *>   R5-2 N3 "ABC" = N5 "ABC  ": N3 extended by national spaces -> T
      *>   R5-3 N5 "ABC  " < N3D "ABD": N3D extended "ABD  ", 3rd C < D -> T
      *>   R5-4 national group NG ("AB") = N4 "AB  ": NG is elementary N(2),
      *>        extended "AB  " -> T
      *>   R5-5 NG "AB" < N3 "ABC": "AB " vs "ABC", SPACE < C -> T
      *>   R5-6 N3 > N5: equal -> F
      *>  item 7 (national vs alphanumeric / alphabetic)
      *>   R7-1 N3 "ABC" = X5 "ABC  ": X5 -> national "ABC  " (5 positions);
      *>        N3 extended -> T
      *>   R7-2 N3 "ABC" = A3 "ABC": A3 treated as alphanumeric -> national -> T
      *>   R7-3 X2 "AB" < N3 "ABC": national "AB" extended "AB " < "ABC" -> T
      *>   R7-4 NG "AB" = X2 "AB" -> T
      *>   R7-5 N3D "ABD" > X5 "ABC  ": 3rd D > C -> T
      *>   R7-6 A3 "ABC" = N3D "ABD" -> F
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C32B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N3           PIC N(3) VALUE N"ABC".
       01 N3D          PIC N(3) VALUE N"ABD".
       01 N4           PIC N(4) VALUE N"AB".
       01 N5           PIC N(5) VALUE N"ABC".
       01 NG           GROUP-USAGE NATIONAL.
          05 NG-1      PIC N(2) VALUE N"AB".
       01 A3           PIC A(3) VALUE "ABC".
       01 X2           PIC X(2) VALUE "AB".
       01 X5           PIC X(5) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF N3 < N3D DISPLAY "R5-1 T" ELSE DISPLAY "R5-1 F".
           IF N3 = N5 DISPLAY "R5-2 T" ELSE DISPLAY "R5-2 F".
           IF N5 < N3D DISPLAY "R5-3 T" ELSE DISPLAY "R5-3 F".
           IF NG = N4 DISPLAY "R5-4 T" ELSE DISPLAY "R5-4 F".
           IF NG < N3 DISPLAY "R5-5 T" ELSE DISPLAY "R5-5 F".
           IF N3 > N5 DISPLAY "R5-6 T" ELSE DISPLAY "R5-6 F".
           IF N3 = X5 DISPLAY "R7-1 T" ELSE DISPLAY "R7-1 F".
           IF N3 = A3 DISPLAY "R7-2 T" ELSE DISPLAY "R7-2 F".
           IF X2 < N3 DISPLAY "R7-3 T" ELSE DISPLAY "R7-3 F".
           IF NG = X2 DISPLAY "R7-4 T" ELSE DISPLAY "R7-4 F".
           IF N3D > X5 DISPLAY "R7-5 T" ELSE DISPLAY "R7-5 F".
           IF A3 = N3D DISPLAY "R7-6 T" ELSE DISPLAY "R7-6 F".
           STOP RUN.
