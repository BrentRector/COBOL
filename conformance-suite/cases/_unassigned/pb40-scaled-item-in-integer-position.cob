*>--- conformance-case
*> id: pb40-scaled-item-in-integer-position
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 type 6 (Integer): "An arithmetic expression that will always
      *> result in an integer value or an integer data item shall be specified."
      *> A PIC 9V9 item is neither — it is a numeric data item WITH digits to the
      *> right of the decimal point, and as an arithmetic expression it does not
      *> always result in an integer. 15.15.3 r1 makes CHAR's argument an integer.
      *>
      *> The sibling of pb40-numeric-function-in-integer-position: the same rule,
      *> the same silent acceptance, a different operand shape. Both were admitted
      *> because a scaled numeric item is class NUMERIC, which is all the screen
      *> could see.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB40NEGSCALED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-F PIC 9V9 VALUE 6.5.
       01 W-R PIC X.
       PROCEDURE DIVISION.
           MOVE FUNCTION CHAR(W-F) TO W-R
           STOP RUN.
