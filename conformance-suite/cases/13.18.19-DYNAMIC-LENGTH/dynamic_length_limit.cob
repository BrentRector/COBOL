*>--- conformance-case
*> id: dynamic_length_limit
*> rules: [GR-13.18.19.4-2, GR-14.9.25.4-8]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> DYNAMIC LENGTH LIMIT (ISO 13.18.19.4 GR2 / 8.5.1.10.4): a receiving
      *> MOVE truncates on the RIGHT to the LIMIT with no padding. Also the
      *> VALUE initial length (8.6.4 — a VALUE clause defines the initial
      *> length) and an item WITHOUT a LIMIT phrase (the implementor-defined
      *> maximum, here unbounded within the native string).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DYN-LENGTH-LIMIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L  PIC X DYNAMIC LENGTH LIMIT IS 5.
       01 WS-U  PIC X DYNAMIC LENGTH VALUE "SEED".
       01 WS-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDEFGHIJ" TO WS-L.
           DISPLAY "L[" WS-L "]".
           MOVE FUNCTION LENGTH(WS-L) TO WS-N.
           DISPLAY "LLEN=" WS-N.
           DISPLAY "U[" WS-U "]".
           MOVE FUNCTION LENGTH(WS-U) TO WS-N.
           DISPLAY "ULEN=" WS-N.
           MOVE "A VERY LONG UNBOUNDED STRING VALUE" TO WS-U.
           DISPLAY "U2[" WS-U "]".
           MOVE FUNCTION LENGTH(WS-U) TO WS-N.
           DISPLAY "U2LEN=" WS-N.
           STOP RUN.
