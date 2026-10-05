      *> kb/Work R22's data-item-wins control: SQRT is a catalogued intrinsic name, but intrinsic
      *> names are not reserved words, and here SQRT is a DECLARED data item - so SQRT(2) is a
      *> subscripted data reference, never a function call and never COBOLNET1543 (the declared item
      *> wins the 8.4.3.2.3 SR2 discrimination in IntrinsicBinder.KeywordOmittedFunction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. KOFSHADOW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 SQRT PIC 9 OCCURS 3 TIMES.
       01 WS-R PIC 9.
       PROCEDURE DIVISION.
           MOVE 7 TO WS-R
           MOVE WS-R TO SQRT(2)
           MOVE SQRT(2) TO WS-R.
           DISPLAY WS-R.
           STOP RUN.
