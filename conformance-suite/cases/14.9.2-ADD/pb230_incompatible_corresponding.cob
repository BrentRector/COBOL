*>--- conformance-case
*> id: pb230_incompatible_corresponding
*> rules: [GR-14.9.2.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB230 - ISO 14.7.6 last paragraph: "For any statement with the CORRESPONDING phrase, if any of the
      *> implied statements would set the EC-DATA-INCOMPATIBLE exception condition to exist, the
      *> EC-DATA-INCOMPATIBLE exception condition is set to exist after all of the implied statements are
      *> completed."  The condition is AGGREGATED, not raised where it arises - which matters because
      *> EC-DATA-INCOMPATIBLE is fatal (Table 13), so raising it at the first offending pair would abandon every
      *> pair to its right, and 14.7.6 says the implied statements complete first.  It is the same discipline the
      *> clause's SIZE ERROR paragraph gets ("the imperative-statement in the SIZE ERROR phrase is executed after
      *> all of the implied statements are completed").
      *> G1's leaves are numeric-DISPLAY leaves under a group REDEFINED as PIC X(9), so planting "0Q1002003"
      *> makes A of G1 hold "0Q1" - not digits, so 8.8.4.4.4 GR3 n)1.a's numeric class condition is false for it -
      *> while B ("002") and C ("003") are valid.  ADD CORRESPONDING must therefore still add ALL THREE pairs and
      *> raise once at the end: the offending pair's result is undefined (14.6.13.2 rule 2), and this compiler's
      *> documented deterministic decode contributes no digit for a non-digit position, so A2 = 0 + 01 = 00001.
      *> The MOVE CORRESPONDING leg pins the same sentence for a non-arithmetic CORRESPONDING statement ("for ANY
      *> statement with the CORRESPONDING phrase"): every pair moves, then the one condition is set.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB230CORR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A PIC 9(3).
          05 B PIC 9(3).
          05 C PIC 9(3).
       01 X1 REDEFINES G1 PIC X(9).
       01 G2.
          05 A PIC 9(5).
          05 B PIC 9(5).
          05 C PIC 9(5).
       01 G3.
          05 A PIC 9(5).
          05 B PIC 9(5).
          05 C PIC 9(5).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE ZERO TO A OF G2 B OF G2 C OF G2.
           MOVE ZERO TO A OF G3 B OF G3 C OF G3.
           MOVE "0Q1002003" TO X1.
           DISPLAY "L1 ADD CORRESPONDING".
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "   A2=" A OF G2 " B2=" B OF G2 " C2=" C OF G2.
           DISPLAY "L2 MOVE CORRESPONDING".
           MOVE CORRESPONDING G1 TO G3.
           DISPLAY "   A3=" A OF G3 " B3=" B OF G3 " C3=" C OF G3.
           DISPLAY "L3 all pairs compatible".
           MOVE "001002003" TO X1.
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "   A2=" A OF G2 " B2=" B OF G2 " C2=" C OF G2.
           STOP RUN.
