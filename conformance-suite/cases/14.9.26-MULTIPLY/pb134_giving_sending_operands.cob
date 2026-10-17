*>--- conformance-case
*> id: pb134_giving_sending_operands
*> rules: [GR-14.9.26.4-2, GR-14.9.44.4-2, FMT-14.9.2.2, FMT-14.9.12.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB134 - the GIVING forms' SENDING operands (ISO 14.9.2.2 / 14.9.44.2 / 14.9.26.2 /
      *> 14.9.12.2 Format 2: `TO/FROM/BY/INTO {identifier-2 | literal-2}` - a literal or a
      *> function-identifier per 8.4.3.1.2 - where the old grammar demanded receivers and parse-errored).
      *> Derived: C = 1+2 = 3; D = 1 + SQRT(9) = 4 (the first fix attempt bound the function's ARGUMENT
      *> and computed 10 - the PB45 walk trap, re-sprung and re-pinned here); Q = 10/2 = 5; S = 5-1 = 4;
      *> M = 3*4 = 12.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. AR1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9(4).
       01 D PIC 9(4).
       01 N PIC 9(4) VALUE 9.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO 2 GIVING C
           DISPLAY "C=" C
           ADD 1 TO FUNCTION SQRT(N) GIVING D
           DISPLAY "D=" D
           DIVIDE 2 INTO 10 GIVING C
           DISPLAY "Q=" C
           SUBTRACT 1 FROM 5 GIVING C
           DISPLAY "S=" C
           MULTIPLY 3 BY 4 GIVING C
           DISPLAY "M=" C
           STOP RUN.
