*>--- conformance-case
*> id: pb885-odo-depending-subscripted
*> rules: [SR-13.18.38.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> AN OCCURS ... DEPENDING ON OPERAND SHALL NOT BE SUBSCRIPTED (kb/Work PB885, the data-division arm).
      *> ISO/IEC 1989:2023 §13.18.38.3 SR2 (all formats): "Data-name-1 and data-name-2 shall not be
      *> subscripted." The capture used to glue the reference into the name WS-TE(2) and report it "not
      *> defined"; it is now the ONE data-name-n screen, which names the subscript (COBOLNET2024).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB885ODS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-TE PIC 9 OCCURS 3 TIMES.
       01 T1.
          05 E1 PIC X OCCURS 1 TO 5 DEPENDING ON WS-TE (2).
       PROCEDURE DIVISION.
           DISPLAY T1
           STOP RUN.
