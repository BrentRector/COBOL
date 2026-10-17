*>--- conformance-case
*> id: pb463_dyn_implementor_maximum
*> rules: [GR-8.5.1.9.6-2, GR-14.9.39.4-38, DOC-A.1-60, DOC-A.1-62]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The MAXIMUM SIZE of a dynamic-length elementary item written with NO LIMIT phrase (ISO 8.5.1.10.1 -
      *> "The maximum size of a dynamic-length elementary item is smallest of:" "the value declared in the LIMIT
      *> phrase", "the largest integer that can be stored in an item of the usage specified in the PREFIXED
      *> phrase", "the maximum permitted by the implementor", with 13.18.19.4 GR2 making the absent phrase implementor-defined). The maximum is REAL
      *> (docs/CONFORMANCE.md section 7 row DOC-A.1-62), which is what bounds SET SIZE's 14.9.39.4 GR38 clamp -
      *> and it does not interfere with ordinary lengths, which is what this program pins. kb/Work PB463: the
      *> absent phrase used to mean "no bound at all", so a request past 2**32 wrapped to its low bits and one
      *> past int.MaxValue threw out of generated code.
      *> Expected values are GR37-GR39 read directly: a grow space-fills the ADDED positions and never restores
      *> truncated content; a shrink drops the trailing ones; a not-nonnegative value gives length 0 and the
      *> nonfatal EC-STORAGE-NOT-AVAIL. FUNCTION EXCEPTION-STATUS is 31 characters, spaces when none exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB463-DYN-MAX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D    PIC X DYNAMIC LENGTH.
       01 WS-NEG  PIC S9    VALUE -1.
       01 WS-N    PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> 8.5.1.10.4 - the store replaces the content and the new length IS the sending length (no pad).
           MOVE "ABCDE" TO WS-D.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "MOVE LEN=" WS-N " [" WS-D "]".
      *> GR38 - 12 is at or below the maximum size, so the length is set to 12; GR39 - the seven ADDED
      *> positions are spaces. No condition exists (the negative control for an unbounded item).
           >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
           SET SIZE OF WS-D TO 12.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "GROW LEN=" WS-N " [" WS-D "] EC["
               FUNCTION EXCEPTION-STATUS "]".
      *> A shrink drops the trailing positions.
           SET SIZE OF WS-D TO 3.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "SHRINK LEN=" WS-N " [" WS-D "]".
      *> The minimum length is zero (13.18.19.4 GR1).
           SET SIZE OF WS-D TO 0.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "ZERO LEN=" WS-N " [" WS-D "]".
      *> GR39 - re-growing NEVER restores previously-truncated content: four spaces, not "ABCD".
           SET SIZE OF WS-D TO 4.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "REGROW LEN=" WS-N " [" WS-D "]".
      *> GR37 - a value that does not evaluate to a nonnegative number sets the length to 0 and sets
      *> EC-STORAGE-NOT-AVAIL, on an item with no LIMIT phrase exactly as on one with it.
           SET SIZE OF WS-D TO WS-NEG.
           MOVE FUNCTION LENGTH(WS-D) TO WS-N.
           DISPLAY "NEG LEN=" WS-N " EC[" FUNCTION EXCEPTION-STATUS "]".
           >>TURN EC-STORAGE-NOT-AVAIL CHECKING OFF
           STOP RUN.
