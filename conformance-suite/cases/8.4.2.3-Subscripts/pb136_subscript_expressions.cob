*>--- conformance-case
*> id: pb136_subscript_expressions
*> rules: [GR-8.4.2.3.4-1, GR-8.8.1.2-5, FMT-8.4.2.3.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB136 - the subscript splitter and quotient evaluation. (1) The spec's own NOTE 2 form:
      *> a depth-0 '(' after an identifier starts a NEW subscript segment when the name carries no OCCURS
      *> (XCOUNTER), so `DOG (XCOUNTER (- YCOUNTER + 3))` is subscripts [3, 2] -> 77 (it was rejected
      *> with a wrong-subscript-count error); after a TABLE name the '(' is the name's own subscript, so
      *> `DOG (BAKER (I) 3)` stays [BAKER(I), 3] -> 66 (declaration-informed splitting - the ambiguity
      *> 8.4.2.3.2 leaves to the declarations). (2) A quotient-bearing subscript routes to the D18 exact
      *> evaluator unconditionally: (3+4)/2 = 3.5 - with EC-BOUND-SUBSCRIPT checking ON this raises the
      *> GR1b fatal (probed: "subscript value 3.500000000 is not an integer"); checking OFF (here) takes
      *> the DOCUMENTED lenient truncation to occurrence 3 -> 88. The old splice was C# integer division
      *> over long reads - the same 3 by accident of truncation, with no exact evaluation and no raise
      *> ever possible.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUB3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          02 R1 OCCURS 4.
             03 DOG PIC 9(2) OCCURS 4.
       01 G2.
          02 BAKER PIC 9 OCCURS 5.
       01 XCOUNTER PIC 9 VALUE 3.
       01 YCOUNTER PIC 9 VALUE 1.
       01 W-A PIC 9(4) VALUE 3.
       01 W-B PIC 9(4) VALUE 4.
       01 E1.
          02 E PIC 9(2) OCCURS 9.
       01 I PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 77 TO DOG(3, 2)
           DISPLAY "N2=" DOG (XCOUNTER (- YCOUNTER + 3))
           MOVE 2 TO BAKER(2)
           MOVE 66 TO DOG(2, 3)
           DISPLAY "TS=" DOG (BAKER (I) 3)
           MOVE 88 TO E(3)
           DISPLAY "Q-LENIENT=" E ((W-A + W-B) / 2)
           STOP RUN.
