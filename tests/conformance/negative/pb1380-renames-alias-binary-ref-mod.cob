      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1380 - a no-THROUGH level-66 RENAMES alias takes ALL of
      *> data-name-2's data attributes (ISO 13.18.45.4 GR1: "all of the data
      *> attributes of data-name-2 become the data attributes of
      *> data-name-1"), so reference-modifying it is reference-modifying a
      *> numeric item of USAGE BINARY, which 8.4.3.3.3 SR1 does not admit
      *> ("a numeric data item of usage display or national that is not
      *> subordinate to a strongly-typed group item"). Before the fix the
      *> resolver returned the alias's place BEFORE the SR1 screen and
      *> the reference modifier, so this compiled clean and moved the
      *> whole item. Expected: COBOLNET1647.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1380RMBIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(6).
       01 G.
          05 GB PIC 9(4) COMP VALUE 12.
          05 G2 PIC X(3) VALUE "HHH".
       66 RB RENAMES GB.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE RB (1:2) TO T.
           STOP RUN.
