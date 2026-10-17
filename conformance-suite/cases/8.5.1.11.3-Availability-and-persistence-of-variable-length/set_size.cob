*>--- conformance-case
*> id: set_size
*> rules: [GR-8.5.1.11.3-6, SR-14.9.39.3-33, SR-14.9.39.3-34, GR-14.9.39.4-38, GR-14.9.39.4-39]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 2023 §14.9.39 Format 16 — SET [SIZE OF] data-name TO n sets the current length of a DYNAMIC LENGTH
      *> elementary item. GR38 sets the length; GR39 initializes the positions ADDED when growing to SPACES (it
      *> NEVER restores previously-truncated content); shrinking drops the trailing positions. The program is
      *> deliberately shrink-then-grow (5→3→6) so the grown positions must be fresh spaces, not the dropped "LO".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SET-SIZE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D  PIC X DYNAMIC LENGTH.
       01 WS-N  PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "HELLO" TO WS-D.
           DISPLAY "A[" WS-D "]".
           SET SIZE OF WS-D TO 3.
           DISPLAY "B[" WS-D "]".
           SET SIZE OF WS-D TO 6.
           DISPLAY "C[" WS-D "]".
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "LEN=" WS-N.
           STOP RUN.
