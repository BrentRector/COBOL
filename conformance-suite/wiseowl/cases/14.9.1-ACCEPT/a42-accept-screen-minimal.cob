*>--- conformance-case
*> id: a42-accept-screen-minimal
*> rules: [SR-14.9.1.3-4, SR-14.9.1.3-5, GR-14.9.1.4-13, GR-14.9.1.4-14,
*>    GR-14.9.1.4-15, GR-14.9.1.4-16, GR-14.9.1.4-17, GR-14.9.1.4-18,
*>    GR-14.9.1.4-19, GR-14.9.1.4-20, GR-14.9.1.4-21, GR-14.9.1.4-22,
*>    GR-14.9.1.4-23, GR-14.9.1.4-25, FMT-14.9.1.2, DOC-A.1-3,
*>    DOC-A.1-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.1 - ACCEPT format 3 (screen), item 1 - the MINIMAL shape, token-identical to the format-1 device ACCEPT
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42ACC1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X VALUE "A".
       SCREEN SECTION.
       01 SG.
          05 SI1 PIC X TO WS-X.
       PROCEDURE DIVISION.
           ACCEPT SG END-ACCEPT.
           STOP RUN.
