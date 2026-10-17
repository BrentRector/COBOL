*>--- conformance-case
*> id: pb59_numeric_literal_argument
*> rules: [AR-15.12.3-1, RV-15.12.4-1, RV-15.18.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / RV-15.12.4-1, RV-15.18.4-1 — a numeric literal is a LEGAL string-channel argument for
      *> exactly the functions whose §15.x.3 rules admit one, and it renders as its own characters.
      *> §15.18.3 r1 lists class NUMERIC among CONCAT's argument classes and §15.18.4 r1 concatenates
      *> "all of the characters"; §15.12.3 r1 admits "an unsigned integer ... literal" as BASECONVERT
      *> argument-1 below base 11. Both shapes used to compile CLEAN and abort at RUN TIME with
      *> NotImplementedCobolFeatureException (the wrong-stage family): the renderer's string channel
      *> had no numeric-literal arm, pinned as designed by a drift test — the pin moved WITH the fix.
      *> The folded nested numeric function (FUNCTION LENGTH of a fixed item folds to a numeric
      *> literal at bind) reaches the same arm. The admission is PER-FUNCTION: NUMVAL and friends
      *> still refuse a numeric literal (the NUMVAL §15.67.3 and NUMVAL-F §15.69.3 rules exclude it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NUMLIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A2    PIC X(2) VALUE "AB".
       01 B2    PIC X(2) VALUE "CD".
       01 R8    PIC X(8).
       PROCEDURE DIVISION.
           MOVE FUNCTION CONCAT(A2 12) TO R8
           DISPLAY "CC12=[" R8 "]"
           MOVE FUNCTION CONCAT(A2 FUNCTION LENGTH(B2)) TO R8
           DISPLAY "CCLN=[" R8 "]"
           MOVE FUNCTION BASECONVERT(255, 10, 16) TO R8
           DISPLAY "BC10=[" R8 "]"
           MOVE FUNCTION BASECONVERT(1010, 2, 16) TO R8
           DISPLAY "BC2 =[" R8 "]"
           STOP RUN.
