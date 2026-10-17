*>--- conformance-case
*> id: pb64t6_locale_edit_dpc
*> rules: [GR-12.3.7.4-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB64 T6 — DECIMAL-POINT IS COMMA is INERT for format-2 editing (ISO 12.3.7.4 GR14: "The DECIMAL-POINT IS
      *> COMMA clause has no effect on the editing or de-editing of a data item described with the locale format
      *> of the PICTURE clause"; its NOTE 3: the character WRITTEN for the decimal separator is always the
      *> period). This program is the byte-identity proof: under DPC the same edit produces the SAME bytes the
      *> non-DPC sibling (pb64t6_locale_edit_rules line 1) produces - the output separator is the LOCALE's, and
      *> the picture's '.' keeps its alignment role with no comma/period swap. The de-edit recovers the value
      *> (the literal 1234,50 is the DPC spelling of 1234.50).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T6DP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC +$ZZZZZZ9.99 LOCALE IS US SIZE IS 20.
       01 N PIC S9(7)V99 VALUE 1234,50.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N TO A
           DISPLAY "[" A "]"
           MOVE ZERO TO N
           MOVE A TO N
           IF N = 1234,50
               DISPLAY "DPC OK"
           ELSE
               DISPLAY "DPC BAD " N
           END-IF
           STOP RUN.
