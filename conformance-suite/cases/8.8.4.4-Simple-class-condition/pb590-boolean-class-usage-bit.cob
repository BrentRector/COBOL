*>--- conformance-case
*> id: pb590-boolean-class-usage-bit
*> rules: [SR-8.8.4.4.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.4.3 SR3: "If the alphabet-name-1, ALPHABETIC, ALPHABETIC-LOWER, ALPHABETIC-UPPER,
      *> BOOLEAN, or class-name-1 phrase is specified, identifier-1 shall reference a data-item whose usage
      *> is display or national." A USAGE BIT item is neither, so the BOOLEAN class condition may not be
      *> written over it - the exact twin of SR8's rule for the NUMERIC phrase, which has always rejected a
      *> USAGE BIT operand (COBOLNET0844) while SR3 had no arm at all until kb/Work PB590.
      *>
      *> SR5 does NOT reach this operand (its category is boolean, not numeric or numeric-edited), which is
      *> why the rule reported here is SR3 and the table applies the category rules before the usage one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB590NG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BT PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           IF BT IS BOOLEAN
               DISPLAY "BOOL"
           END-IF
           STOP RUN.
