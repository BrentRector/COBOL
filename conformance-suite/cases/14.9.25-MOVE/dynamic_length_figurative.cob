*>--- conformance-case
*> id: dynamic_length_figurative
*> rules: [GR-14.9.25.4-2, GR-14.9.25.4-8, RV-15.50.4-6]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> DYNAMIC LENGTH figurative + count-1 PICTURE (P12 adversarial-review
      *> fixes). A figurative constant OTHER THAN `ALL literal` sends ONE
      *> character (ISO 8.3.3.6.4 GR3b), so MOVE SPACE/ZERO into a dynamic-
      *> length item yields length 1 (never 0); a figurative VALUE likewise
      *> initializes to length 1 (8.6.4). A zero-length literal "" is length 0.
      *> PIC X(01) is exactly one instance of X (13.18.19.3 SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-LENGTH-FIGURATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(01) DYNAMIC LENGTH LIMIT IS 10.
       01 WS-V PIC X DYNAMIC LENGTH VALUE SPACE.
       01 WS-N PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACES TO WS-D.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "SP=" WS-N "[" WS-D "]".
           MOVE ZERO TO WS-D.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "ZE=" WS-N "[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-V) TO WS-N.
           DISPLAY "VS=" WS-N "[" WS-V "]".
           MOVE "" TO WS-D.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "EM=" WS-N "[" WS-D "]".
           STOP RUN.
