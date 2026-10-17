*>--- conformance-case
*> id: dynamic_length_item
*> rules: [GR-8.5.1.11.3-7, SR-13.18.19.3-3, GR-13.18.19.4-1, GR-14.9.25.4-8, RV-15.50.4-6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> DYNAMIC LENGTH elementary items (ISO 8.5.1.10 / 13.18.19, COBOL-2014):
      *> a variable-length, minimum-length-zero PIC X string. The current
      *> length varies with each receiving MOVE (8.5.1.10.4 — new length =
      *> sending length); FUNCTION LENGTH returns the current length (15.50.4
      *> r6). Exercises a short literal, a longer literal, a MOVE from a fixed
      *> item, and an empty (zero-length) result. Typed-native: the item IS a
      *> native string, never a byte window.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-LENGTH-ITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D  PIC X DYNAMIC LENGTH LIMIT IS 30.
       01 WS-S  PIC X(10) VALUE "ABCDEFGHIJ".
       01 WS-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "HI" TO WS-D.
           DISPLAY "1[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "1LEN=" WS-N.
           MOVE "A LONGER VALUE" TO WS-D.
           DISPLAY "2[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "2LEN=" WS-N.
           MOVE WS-S TO WS-D.
           DISPLAY "3[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "3LEN=" WS-N.
           MOVE "" TO WS-D.
           DISPLAY "4[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "4LEN=" WS-N.
           STOP RUN.
