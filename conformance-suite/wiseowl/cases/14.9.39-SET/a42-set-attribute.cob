*>--- conformance-case
*> id: a42-set-attribute
*> rules: [SR-14.9.39.3-15, SR-14.9.39.3-16, GR-14.9.39.4-11]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39 - SET format 6 (attribute), item 24 - note the printed format has NO 'TO'
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42SETA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X FROM WS-X.
       PROCEDURE DIVISION.
           SET SG ATTRIBUTE HIGHLIGHT ON.
           STOP RUN.
