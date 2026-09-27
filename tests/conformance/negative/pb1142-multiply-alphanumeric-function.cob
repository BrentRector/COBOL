      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1142 - ISO 14.9.26.3 SR1: "Identifier-1 and identifier-2 shall reference a data item
      *> of category numeric." A function-identifier references a temporary whose class and category are
      *> the function's type (15.2), and UPPER-CASE's is alphanumeric, so it is not a MULTIPLY operand
      *> (COBOLNET0844, the 8.8.1.1 class screen COMPUTE already applied). The operand-wrapper walk bound
      *> the function UNSCREENED and digit-decoded "12": B became 3.3 x 12 = 39.6 under STRICT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1142N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B PIC 9(3)V9 VALUE 3.3.
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY FUNCTION UPPER-CASE("12") BY B.
           DISPLAY B.
           STOP RUN.
