*>--- conformance-case
*> id: pb64t1-set-locale-pointer-sr28
*> rules: [SR-14.9.39.3-28]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR28: "Identifier-11 shall reference an elementary data item of category data-pointer." The
      *> save-locale form (Format 12) places a saved-locale HANDLE into the pointer (14.9.39.4 GR26; DESIGN-locale-facility
      *> L4) - an alphanumeric receiver cannot hold it. COBOLNET1668 (kb/Work PB64 T1; this fixture was PB92's
      *> documented-non-support refusal of the whole format, COBOLNET1518, until T1 implemented it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SR28.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-L PIC X(20).
       PROCEDURE DIVISION.
           SET WS-L TO LOCALE LC_ALL.
           STOP RUN.
