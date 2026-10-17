      *>--- conformance-case
      *> id: pb566_blank_when_zero_stored_value
      *> rules: [GR-13.18.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB566 - BLANK WHEN ZERO blanks on THE VALUE BEING STORED, not on the sending value.
      *> ISO/IEC 1989:2023 13.18.8.4 GR1: "When the BLANK WHEN ZERO clause is specified for a data item, the
      *> content of the data item is set to all spaces when the item is a receiving operand and the value being
      *> stored is zero." 13.18.8.1 says it in one line - the clause "causes the blanking of an item when a
      *> value of zero is being stored in it".
      *> WHICH value is stored is settled by 14.9.25.4 GR6 d), whose closing paragraph sends the store to
      *> 14.6.8 ("Alignment of the numeric value by decimal point, any necessary zero filling, any truncation
      *> of digits, and transfer of the algebraic data into the receiving data item, take place as defined in
      *> 14.6.8"), and 14.6.8.2 r4 is "the data is aligned by decimal point and is transferred to the receiving
      *> digits with zero fill or truncation ON EITHER END as required". So a sending value that truncates away
      *> to zero at either end STORES zero, and the clause blanks. The arithmetic arms reach the same rule
      *> through 14.7.5 / 14.6.8 with the resultant already at the receiver's scale.
      *> Until 2026-09-20 the guard tested the raw SENDING pair, so the MOVE arm rendered digits where the
      *> arithmetic arms on identical items blanked - a silent wrong answer through the commonest verb.
      *>
      *> Each expected line below is derived from the rules above, not from the compiler:
      *>   MOVE-LOW-E   0.4 -> PIC ZZZ9 (scale 0): 14.6.8.2 r4 truncates the fraction; 0 stored -> 4 spaces.
      *>   MOVE-LOW-P   0.4 -> PIC 9(3): the same store; 13.18.8.4 GR2 makes it numeric-edited -> 3 spaces.
      *>   COMP-E/MULG-P  the arithmetic arms of the same two items - identical images (that is the point).
      *>   MOVE-HIGH    100 -> PIC 99: r4's OTHER end truncates the leading 1; 00 stored -> 2 spaces.
      *>   MOVE-HIGH-NZ 101 -> PIC 99: 01 stored, not zero -> "01" (the clause does nothing).
      *>   MOVE-NEG    -0.4 -> PIC ZZZ9: stores zero (algebraic zero has no sign) -> 4 spaces.
      *>   MOVE-CS-0  0.001 -> PIC $$$9.99: r4 truncates to 0.00 -> ALL 7 positions spaces, currency included
      *>                (GR1 says "the content of the data item", so no symbol renders).
      *>   MOVE-CS-NZ  1.25 -> PIC $$$9.99: nonzero, so 13.18.40.5 rule 6 a) floats the '$' to the rightmost
      *>                suppressed position of its zone -> "  $1.25".
      *>   VALUE-ZERO   VALUE 0 with the clause: 13.18.63.4 GR8's NOTE - the clause DOES affect initialization
      *>                when the VALUE literal is numeric -> 3 spaces. 13.18.63 SR6 exempts the literal zero from
      *>                the 2023-only numeric-literal VALUE, so this entry is legal at COBOL-85 too.
      *>   INIT-VERB    INITIALIZE moves ZEROES to the item (14.9.20.4 GR6 c) -> 3 spaces.
      *>   DEEDIT-BACK  13.18.8.4 GR3 - the blanked item is a sending operand and the object of the operation
      *>                is numeric, so its value is zero -> "00000".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB566BWZ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-E      PIC ZZZ9    BLANK WHEN ZERO.
       01 WS-P      PIC 9(3)    BLANK WHEN ZERO.
       01 WS-HI     PIC 99      BLANK WHEN ZERO.
       01 WS-CS     PIC $$$9.99 BLANK WHEN ZERO.
       01 WS-VZ     PIC 9(3)    BLANK WHEN ZERO VALUE 0.
       01 WS-SRC    PIC 9V9     VALUE 0.4.
       01 WS-NSRC   PIC S9V9    VALUE -0.4.
       01 WS-BIG    PIC 9(3)    VALUE 100.
       01 WS-BACK   PIC 9(3)V99 VALUE 55.
       PROCEDURE DIVISION.
           MOVE WS-SRC TO WS-E
           DISPLAY "MOVE-LOW-E[" WS-E "]"
           MOVE WS-SRC TO WS-P
           DISPLAY "MOVE-LOW-P[" WS-P "]"
           COMPUTE WS-E = WS-SRC
           DISPLAY "COMP-E[" WS-E "]"
           MULTIPLY WS-SRC BY 1 GIVING WS-P
           DISPLAY "MULG-P[" WS-P "]"
           MOVE WS-BIG TO WS-HI
           DISPLAY "MOVE-HIGH[" WS-HI "]"
           MOVE 101 TO WS-HI
           DISPLAY "MOVE-HIGH-NZ[" WS-HI "]"
           MOVE WS-NSRC TO WS-E
           DISPLAY "MOVE-NEG[" WS-E "]"
           MOVE 0.001 TO WS-CS
           DISPLAY "MOVE-CS-0[" WS-CS "]"
           MOVE 1.25 TO WS-CS
           DISPLAY "MOVE-CS-NZ[" WS-CS "]"
           DISPLAY "VALUE-ZERO[" WS-VZ "]"
           MOVE 9 TO WS-VZ
           INITIALIZE WS-VZ
           DISPLAY "INIT-VERB[" WS-VZ "]"
           MOVE WS-SRC TO WS-P
           MOVE WS-P TO WS-BACK
           DISPLAY "DEEDIT-BACK[" WS-BACK "]"
           STOP RUN.
