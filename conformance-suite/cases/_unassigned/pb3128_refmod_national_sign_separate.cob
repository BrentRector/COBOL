*>--- conformance-case
*> id: pb3128_refmod_national_sign_separate
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3128 review R1-1 - the positions of a reference-modified USAGE NATIONAL numeric item
      *> written with SIGN IS SEPARATE include the sign position. 8.4.3.3.4 GR3 operates on it "as if it
      *> were redefined as a data item of class and category national of the same size" (OK 8.4.3.3.4 3)),
      *> and GR4: "If the data description entry for identifier-1 contains a SIGN IS SEPARATE clause, the
      *> sign position is assigned an ordinal number within that data item" (OK 8.4.3.3.4 4)). So
      *> S PIC S9(4) USAGE NATIONAL SIGN LEADING SEPARATE holds N"-0012", FIVE national positions, and
      *> GR5 b)/c) bound leftmost-position and length by five. Before the fix the position count read the
      *> PICTURE length (4, the digits alone): T(1:5) and T(5:1) were refused as past "the 4-position
      *> item" (COBOLNET2670, legal source rejected) and S(2:) ran one position short.
      *> DERIVATION (S = "-0012", T = "0012-"):
      *>   FUNCTION LENGTH(S) / LENGTH(S(1:))      -> 5|5
      *>   MOVE S(2:) TO X Y  (PIC N(8))           -> 0012    |0012    |   (GR1 intermediate: 4 positions)
      *>   MOVE T(1:5) TO X                        -> 0012-   |
      *>   MOVE T(3:) TO X                         -> 12-     |
      *>   S(1:1) = N"-" , T(5:1) = N"-"           -> LEAD-SIGN|TRAIL-SIGN
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3128NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC S9(4) USAGE NATIONAL SIGN LEADING SEPARATE VALUE -12.
       01 T  PIC S9(4) USAGE NATIONAL SIGN TRAILING SEPARATE VALUE -12.
       01 X  PIC N(8).
       01 Y  PIC N(8).
       PROCEDURE DIVISION.
           DISPLAY FUNCTION LENGTH(S) "|" FUNCTION LENGTH(S(1:))
           MOVE S(2:) TO X Y
           DISPLAY X "|" Y "|"
           MOVE T(1:5) TO X
           DISPLAY X "|"
           MOVE T(3:) TO X
           DISPLAY X "|"
           IF S(1:1) = N"-"
               DISPLAY "LEAD-SIGN|" WITH NO ADVANCING
           ELSE
               DISPLAY "LEAD-WRONG|" WITH NO ADVANCING
           END-IF
           IF T(5:1) = N"-"
               DISPLAY "TRAIL-SIGN"
           ELSE
               DISPLAY "TRAIL-WRONG"
           END-IF
           STOP RUN.
       END PROGRAM PB3128NS.
