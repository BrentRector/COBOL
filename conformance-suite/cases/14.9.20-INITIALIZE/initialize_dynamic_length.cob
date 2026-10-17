*>--- conformance-case
*> id: initialize_dynamic_length
*> rules: [GR-14.9.20.4-7]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> CA1 (CONFORMANCE-FIX-QUEUE): INITIALIZE of a dynamic-length elementary item sets its LENGTH to zero,
      *> ISO 14.9.20.4 GR7 — overriding the GR6c figurative SPACE fill. Pre-fix, INITIALIZE ran an implicit
      *> MOVE SPACE and left the item at length 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITDYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 30.
       01 WS-N PIC 9(2).
       PROCEDURE DIVISION.
           MOVE "HELLO" TO WS-D.
           INITIALIZE WS-D.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "LEN=" WS-N.
           STOP RUN.
