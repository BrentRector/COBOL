*>--- conformance-case
*> id: float_binary
*> rules: [GR-13.18.60.4-14, GR-13.18.60.4-15]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> FLOAT-BINARY-32 / FLOAT-BINARY-64 (ISO 13.18.60.4 GR14 / GR15,
      *> COBOL-2014): the IEEE-754 binary32 / binary64 interchange formats,
      *> mapped EXACTLY to native float / double (the pinned ISO/IEC 60559
      *> formats are conforming). Arithmetic into a fixed receiver (integer
      *> results avoid float-display precision). Typed-native; no byte substrate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FLOAT-BINARY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B32 USAGE FLOAT-BINARY-32.
       01 WS-B64 USAGE FLOAT-BINARY-64.
       01 WS-R   PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 10.5 TO WS-B32.
           COMPUTE WS-R = WS-B32 * 2.
           DISPLAY "B32MUL=" WS-R.
           MOVE 100.25 TO WS-B64.
           COMPUTE WS-R = WS-B64 * 4.
           DISPLAY "B64MUL=" WS-R.
           COMPUTE WS-B64 = WS-B32 + WS-B64.
           COMPUTE WS-R = WS-B64.
           DISPLAY "SUM=" WS-R.
      *> FUNCTION BYTE-LENGTH folds to the pinned IEEE byte widths
      *> (13.18.60.4 GR14/GR15): binary32 = 4 bytes, binary64 = 8 bytes.
           MOVE FUNCTION BYTE-LENGTH(WS-B32) TO WS-R.
           DISPLAY "B32BYTES=" WS-R.
           MOVE FUNCTION BYTE-LENGTH(WS-B64) TO WS-R.
           DISPLAY "B64BYTES=" WS-R.
           STOP RUN.
