*>--- conformance-case
*> id: l1-fraction-part-class
*> rules: [AR-15.42.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.42.3 r1 — "Argument-1 shall be of the class numeric."
      *>
      *> WS-X below is a data item described PIC X(4), which §8.5.2.1 Table 2 (Class and category
      *> relationships for elementary data items) puts in class ALPHANUMERIC, and §15.3's argument type 10
      *> ("Numeric. An arithmetic expression or a numeric data item shall be specified.") admits neither an
      *> alphanumeric item nor an alphanumeric literal. ⛔ THE VALUE IS DELIBERATELY THE CHARACTERS "12.5":
      *> the rule screens the CLASS of the operand, not whether its content happens to spell a number, so a
      *> compiler that reinterpreted the characters would compute 0.5 from source the standard forbids and
      *> report nothing. §15.42's only conversion-from-text sibling is FUNCTION NUMVAL (§15.66), and the
      *> program that wants this behaviour has to write it.
      *> Expected: COBOLNET1627, the intrinsic-argument-class diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FPCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "12.5".
       01 R    PIC S9V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION FRACTION-PART(WS-X).
           DISPLAY R.
           STOP RUN.
