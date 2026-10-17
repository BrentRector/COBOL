*>--- conformance-case
*> id: a42-special-names-crt-status
*> rules: [SR-12.3.7.3-1, SR-12.3.7.3-4, SR-12.3.7.3-29, SR-12.3.7.3-30,
*>    GR-12.3.7.4-15, GR-12.3.7.4-16, DOC-A.1-41, DOC-A.1-91]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7 - the SPECIAL-NAMES CRT STATUS clause (item 25); the CRT status is the 9.2.3 conceptual entity
      *> Annex A.4.2 (ACCEPT and DISPLAY screen handling) is Not claimed - docs/CONFORMANCE.md
      *> section 5; A.4.1 admits an optional element's syntax only when support is claimed.
      *> Witness for kb/Work PB260.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. A42CRT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CRT STATUS IS WS-CRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-CRT PIC X(4).
       01 WS-X PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
