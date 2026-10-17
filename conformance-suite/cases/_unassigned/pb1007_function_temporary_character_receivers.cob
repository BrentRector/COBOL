      *>--- conformance-case
      *> id: pb1007_function_temporary_character_receivers
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1007 - an INTEGER function moved to MORE THAN ONE receiver. ISO 14.9.25.4 GR1: a
      *> function-identifier "is evaluated only once, immediately before data is moved to the first of
      *> the receiving operands", and the rule writes the shape out as MOVE a TO temp / MOVE temp TO b /
      *> MOVE temp TO c - so every receiver must store what a MOVE of the function itself stores.
      *> 15.4 puts the returned value "in a temporary elementary data item" whose representation is the
      *> implementor's (15.4.1); DOC-A.1-92 makes a numeric function's value used as TEXT its literal
      *> form, and 15.2 item 5 gives an integer function "no digits to the right of the decimal point".
      *> Before the fix the temporary's 30-digit image was moved left-justified: every character
      *> receiver below held 0000.
      *>
      *> M1  N = 3.7: INTEGER(N) = 3 (15.44.1: "the greatest integer value that is less than or
      *>     equal to the argument") into two PIC X(4): the literal form "3", left-justified,
      *>     space-filled -> [3   ][3   ].
      *> M2  the single-receiver MOVE of the same value is the control -> [3   ].
      *> M3  INTEGER-PART(-12.5) = -12 into PIC X(4) and a numeric-edited -(4)9.99: GR6 a) does not
      *>     move the operational sign to the character receiver -> [12  ]; the edited one -> "  -12.00".
      *> M4  ORD("A") = 66 (15.70.1: the ordinal position "in the program collating sequence", the
      *>     lowest being 1; natively "A" is character 65) into PIC X(4) and 9(4) -> [66  ] and 0066.
      *> M5  MAX(3 7) = 7 into two PIC X(2) -> [7 ][7 ].
      *> M6  INTEGER(N) TO IX TE(IX): GR1's "Item identification for identifier-2 is performed
      *>     immediately before the data is moved to the respective data item", so TE(IX) is TE(3) after
      *>     IX receives 3 -> TB = "        3   ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1007FT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9V9 VALUE 3.7.
       01 R1 PIC X(4).
       01 R2 PIC X(4).
       01 S1 PIC X(2).
       01 S2 PIC X(2).
       01 N4 PIC 9(4).
       01 E  PIC -(4)9.99.
       01 IX PIC 9 VALUE 1.
       01 TB.
          05 TE PIC X(4) OCCURS 3.
       PROCEDURE DIVISION.
           MOVE FUNCTION INTEGER(N) TO R1 R2
           DISPLAY "M1=[" R1 "][" R2 "]"
           MOVE SPACES TO R1
           MOVE FUNCTION INTEGER(N) TO R1
           DISPLAY "M2=[" R1 "]"
           MOVE FUNCTION INTEGER-PART(-12.5) TO R1 E
           DISPLAY "M3=[" R1 "][" E "]"
           MOVE FUNCTION ORD("A") TO R2 N4
           DISPLAY "M4=[" R2 "][" N4 "]"
           MOVE FUNCTION MAX(3 7) TO S1 S2
           DISPLAY "M5=[" S1 "][" S2 "]"
           MOVE SPACES TO TB
           MOVE FUNCTION INTEGER(N) TO IX TE(IX)
           DISPLAY "M6=[" TB "]"
           STOP RUN.
