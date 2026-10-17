*>--- conformance-case
*> id: ec_storage_not_avail
*> rules: [GR-14.9.39.4-37, GR-14.9.39.4-38]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> SET [SIZE OF] dynamic-length-item TO arithmetic-expression-5 (ISO §14.9.39 Format 16, COBOL-2023): the
      *> nonfatal EC-STORAGE-NOT-AVAIL condition. GR37 — a value that does not evaluate to a nonnegative number sets
      *> the length to 0 and sets EC-STORAGE-NOT-AVAIL; the sign test is on the EVALUATED value, so a fractional
      *> negative in (-1,0) still triggers it (before the toward-zero truncation). GR38 — a value above the item's
      *> maximum size clamps to that maximum and sets EC-STORAGE-NOT-AVAIL. A within-range SET raises nothing (the
      *> negative control). All amounts here are data items (arithmetic-expression-5); the integer-2 literal form is
      *> compile-time bounded by SR34. Observed via FUNCTION EXCEPTION-STATUS (a 31-char left-justified name; 31
      *> spaces when no exception exists).
      >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EC-STORAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D    PIC X DYNAMIC LENGTH LIMIT IS 5.
       01 WS-THREE PIC 9    VALUE 3.
       01 WS-FRAC PIC S9V9  VALUE -0.5.
       01 WS-NEG  PIC S9    VALUE -1.
       01 WS-BIG  PIC 99    VALUE 9.
       01 WS-N    PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO WS-D.
      *> valid resize (5 -> 3): within range, raises nothing (the negative control).
           SET SIZE OF WS-D TO WS-THREE.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "OK LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
      *> GR37 fractional negative (-0.5): the evaluated value is negative -> length 0 + EC-STORAGE-NOT-AVAIL.
           SET SIZE OF WS-D TO WS-FRAC.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "FRAC LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
      *> GR37 integer negative (-1) -> length 0 + EC-STORAGE-NOT-AVAIL.
           SET SIZE OF WS-D TO WS-NEG.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "NEG LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
      *> GR38 above the LIMIT (9 > 5) -> clamp to 5 + EC-STORAGE-NOT-AVAIL.
           MOVE "XY" TO WS-D.
           SET SIZE OF WS-D TO WS-BIG.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "CLAMP LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
