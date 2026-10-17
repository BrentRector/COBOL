*>--- conformance-case
*> id: a42-screen-sign
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.52 - the SIGN clause of a screen description entry
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42SIGN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC S9 SIGN IS LEADING SEPARATE CHARACTER.
       PROCEDURE DIVISION.
           STOP RUN.
