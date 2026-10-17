*>--- conformance-case
*> id: pb586_level88_range_boundaries
*> rules: [SR-13.18.63.3-2]
*> accept: [85]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 13.18.63.3 SR2/SR3 (ALL FORMATS; kb/Work PB586): a level-88 literal shall be a
      *> permissible value in its conditional variable's PICTURE range (13.18.63.4 GR19). The BOUNDARY values are
      *> exactly representable and must stay legal: -99.9 and 99.9 for S9(2)V9 (SR3: a signed subject takes a
      *> negative literal), 99 for 9(2), and zero.
      *> WHY EACH LINE CAN FAIL:
      *>   LOW=     Y - W holds -99.9, the range's low end (14.7.8: the range is inclusive).
      *>   HIGH=    Y - W holds 99.9, its high end.
      *>   ZERO=    Y - W holds 0, which is also in C-ALL: YY.
      *>   TOP=     Y - U holds 99, U-TOP's single value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB586RBPOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC S9(2)V9.
           88 C-ALL VALUE -99.9 THRU 99.9.
           88 C-Z VALUE 0.
       01 U PIC 9(2).
           88 U-TOP VALUE 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -99.9 TO W
           IF C-ALL DISPLAY "LOW=Y" ELSE DISPLAY "LOW=N" END-IF
           MOVE 99.9 TO W
           IF C-ALL DISPLAY "HIGH=Y" ELSE DISPLAY "HIGH=N" END-IF
           MOVE 0 TO W
           IF C-ALL AND C-Z DISPLAY "ZERO=YY" ELSE DISPLAY "ZERO=N" END-IF
           MOVE 99 TO U
           IF U-TOP DISPLAY "TOP=Y" ELSE DISPLAY "TOP=N" END-IF
           STOP RUN.
