*>--- conformance-case
*> id: pb1030-string-literal-subscript
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1030 - ISO 8.4.2.3.2: a subscript is ALL, an arithmetic expression, or an
      *> index-name optionally followed by + or - and an integer. A string literal is none of
      *> them; this compiled with a "not implemented" warning and aborted the run unit.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY E ("A").
           STOP RUN.
