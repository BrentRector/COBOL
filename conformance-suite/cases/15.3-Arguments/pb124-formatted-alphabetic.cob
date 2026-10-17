*>--- conformance-case
*> id: pb124-formatted-alphabetic
*> rules: [AR-15.3-1, AR-15.38.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.38.3 r1 (FORMATTED-CURRENT-DATE): the format argument shall be of category alphanumeric or
      *> national - a PIC A item is category alphabetic (8.5.2.2) and Table 21's cell prints no Alph1
      *> (kb/Work PB124 wave 5, AR-15.3-1; the whole FORMATTED-*/NUMVAL/LOCALE-DATE/TIME family rides the
      *> same 't' kind).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AL PIC A(5) VALUE "abc".
       01 R PIC 9(4).
       01 RS PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-CURRENT-DATE(AL) TO RS
           STOP RUN.
