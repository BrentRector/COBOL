      *>--- conformance-case
      *> id: pb422_all_multichar_numeric_85
      *> rules: [SR-8.3.3.6.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
*> kb/Work PB422 - a multi-character ALL literal ASSOCIATED with a numeric item at COBOL-85, where the
*> association is admitted (an obsolete element of ANSI X3.23-1985, deleted by ISO 2002 - VCR Table 7 row
*> 7.13, a DERIVED edge; from 2002 ISO 8.3.3.6.3 SR3 prohibits it: "If the length of literal-1 is greater than
*> one, it is not permitted to be associated with a numeric or numeric-edited item").
*> Expected values, from the rules, not measured:
*>  MOVE ALL "57" TO N (PIC 9(3)): 14.9.25.4 GR6 3) b. - "the number of digits is the same as the number of
*>    digits in the receiving operand and the figurative constant is replicated in this item, from left to
*>    right" - with 8.3.3.6.4 GR2's repeat-and-truncate: "575".
*>  IF N = ALL "57" / ALL "56": 8.8.4.2.5 - the integer operand is treated as moved to an alphanumeric item
*>    "of the same length in terms of character positions as the number of digits in the integer" (3), and the
*>    figurative takes that associated length (8.3.3.6.4 GR2): "575" = "575" is EQ, "575" vs "565" is NE.
*>  EVALUATE N WHEN ALL "57": the same comparison through the selection-object pairing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB422P85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE ZERO.
       PROCEDURE DIVISION.
           MOVE ALL "57" TO N
           DISPLAY "N=[" N "]"
           IF N = ALL "57"
               DISPLAY "C1=EQ"
           ELSE
               DISPLAY "C1=NE"
           END-IF
           IF N = ALL "56"
               DISPLAY "C2=EQ"
           ELSE
               DISPLAY "C2=NE"
           END-IF
           EVALUATE N
               WHEN ALL "57" DISPLAY "E=57"
               WHEN OTHER DISPLAY "E=OTHER"
           END-EVALUATE
           STOP RUN.
