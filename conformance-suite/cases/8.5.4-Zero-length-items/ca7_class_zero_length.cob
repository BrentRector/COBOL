*>--- conformance-case
*> id: ca7_class_zero_length
*> rules: [GR-8.5.4-4, GR-8.8.4.4.4-1]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> CA7 (CONFORMANCE-FIX-QUEUE): a class condition (ALPHABETIC/-UPPER/-LOWER, a user class-name, NUMERIC,
      *> BOOLEAN) on a ZERO-LENGTH operand is FALSE without the word NOT (ISO 8.8.4.4.4 GR1); NOT reverses it (GR2).
      *> After MOVE "" the dynamic-length WS-D has current length 0, so IS ALPHABETIC is false -> ELSE -> NOTALPHA.
      *> Pre-fix CobolClass.IsAlphabetic("") returned true (the empty char loop fell through) -> THEN -> ALPHA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLSZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "" TO WS-D
           IF WS-D IS ALPHABETIC
               DISPLAY "ALPHA"
           ELSE
               DISPLAY "NOTALPHA"
           END-IF
           IF WS-D IS NOT ALPHABETIC
               DISPLAY "NOT-ALPHA-TRUE"
           END-IF
           STOP RUN.
