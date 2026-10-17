*>--- conformance-case
*> id: pb463-set-size-below-2023
*> rules: [GR-14.9.39.4-38]
*> source-format: free
*> portable: unverified
*>---
      *> SET [SIZE OF] data-name-3 TO ... (ISO 14.9.39 Format 16) is a COBOL-2023 introduction: the DYNAMIC
      *> LENGTH clause the item is declared with arrived in COBOL-2014 (8.5.1.10 / 13.18.19), but the SET format
      *> that changes an item's current length did not. Below 2023 the statement is refused with the edition-band
      *> COBOLNET0900 - the maximum-size rules 14.9.39.4 GR37-GR39 apply to (8.5.1.10.1, docs/CONFORMANCE.md
      *> section 7 row DOC-A.1-62) have no statement to govern there. kb/Work PB463.
      *> The declaration itself is legal at 2014, so this fixture isolates the SET format: at 85 and 2002 the
      *> DYNAMIC LENGTH clause takes the same code, which is why all three editions are named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB463N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO WS-D
           SET SIZE OF WS-D TO 12
           DISPLAY WS-D
           STOP RUN.
