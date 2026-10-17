*>--- conformance-case
*> id: pb909-inspect-replacing-trailing
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 §14.9.22.2 General formats: the replacing-phrase prints CHARACTERS BY, ALL, LEADING and
*> FIRST - and nothing else. REPLACING TRAILING is a vendor extension the grammar accepts; it used to bind to
*> the DEFERRAL carrier, so the program compiled with a COBOLNET1756 "not implemented" warning and aborted
*> the run unit when the INSPECT executed. §4.2.2 requires the compile-time indication of "violations of the
*> general formats and the explicit syntax rules of standard COBOL"; no edition prints TRAILING, and this
*> implementation admits no vendor dialect, so every edition refuses. COBOLNET2269 (kb/Work PB909).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB909NIRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S PIC X(6) VALUE "AB    ".
       PROCEDURE DIVISION.
           INSPECT S REPLACING TRAILING " " BY "*".
           DISPLAY S.
           STOP RUN.
