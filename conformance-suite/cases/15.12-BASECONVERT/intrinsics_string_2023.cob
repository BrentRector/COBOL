*>--- conformance-case
*> id: intrinsics_string_2023
*> rules: [FMT-15.12.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.18 CONCAT + §15.12 BASECONVERT — 2023 intrinsic additions (VERSION_CHANGE_REFERENCE rows 65/68).
      *> CONCAT: all characters of every argument in order (rules 1/4) — the fixed-width images pad. BASECONVERT:
      *> an unsigned integer's digits re-expressed from one base into another (0-9/A-F).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INTR23S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(2) VALUE "AB".
       01 WS-B PIC X(2) VALUE "CD".
       01 WS-R PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CONCAT(WS-A, WS-B) TO WS-R.
           DISPLAY "CAT=" WS-R.
           MOVE FUNCTION CONCAT("X", "Y", "Z") TO WS-R.
           DISPLAY "VAR=" WS-R.
           MOVE FUNCTION BASECONVERT("FF", 16, 10) TO WS-R.
           DISPLAY "DEC=" WS-R.
           MOVE FUNCTION BASECONVERT("255", 10, 16) TO WS-R.
           DISPLAY "HEX=" WS-R.
           MOVE FUNCTION BASECONVERT("1010", 2, 16) TO WS-R.
           DISPLAY "BIN=" WS-R.
           STOP RUN.
       END PROGRAM INTR23S.
