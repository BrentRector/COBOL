*>--- conformance-case
*> id: pb64t1-set-locale-pointer-sr27
*> rules: [SR-14.9.39.3-27]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR27: "Identifier-10 shall reference an elementary data item of category data-pointer." The TO
      *> operand of SET LOCALE (Format 11) is a locale-name, USER-DEFAULT, SYSTEM-DEFAULT, or a data-pointer holding a
      *> saved locale (14.9.39.4 GR23a / GR21) - WS-N is a numeric item, neither. COBOLNET1668 (kb/Work PB64 T1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB64T1SR27.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4).
       PROCEDURE DIVISION.
           SET LOCALE LC_ALL TO WS-N.
           STOP RUN.
