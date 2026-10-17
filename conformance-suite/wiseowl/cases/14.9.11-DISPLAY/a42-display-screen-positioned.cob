*>--- conformance-case
*> id: a42-display-screen-positioned
*> rules: [SR-14.9.11.3-3, GR-14.9.11.4-12, GR-14.9.11.4-13,
*>    GR-14.9.11.4-14, GR-14.9.11.4-15, GR-14.9.11.4-16,
*>    GR-14.9.11.4-17, GR-14.9.11.4-18, GR-14.9.11.4-19, FMT-14.9.11.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.11 - DISPLAY format 2 with COLUMN only (item 9) - it used to bind as a THREE-operand device DISPLAY
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42DSP2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X FROM WS-X.
       PROCEDURE DIVISION.
           DISPLAY SG COLUMN 5 END-DISPLAY.
           STOP RUN.
