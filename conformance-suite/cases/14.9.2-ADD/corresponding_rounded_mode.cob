*>--- conformance-case
*> id: corresponding_rounded_mode
*> rules: [SR-14.9.2.3-5, SR-14.9.2.3-6, GR-14.9.2.4-3, GR-14.9.2.4-5, FMT-14.9.2.2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.7.4 + §14.9.2/§14.9.44 — ROUNDED MODE on ADD/SUBTRACT CORRESPONDING. The mode applies
      *> uniformly to every corresponding pair. 1.25 -> 1.2 (TRUNCATION) vs 1.3 (NEAREST-AWAY-FROM-ZERO).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CORRMODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 P PIC 9V99 VALUE 1.25.
          05 Q PIC 9V99 VALUE 1.21.
       01 DST.
          05 P PIC 9V9 VALUE 0.
          05 Q PIC 9V9 VALUE 0.
       01 EP PIC 9.9.
       01 EQ PIC 9.9.
       PROCEDURE DIVISION.
       MAIN.
           ADD CORRESPONDING SRC TO DST ROUNDED MODE IS TRUNCATION.
           MOVE P OF DST TO EP. MOVE Q OF DST TO EQ.
           DISPLAY "TRUNC P=" EP " Q=" EQ.
           MOVE 0 TO P OF DST. MOVE 0 TO Q OF DST.
           ADD CORRESPONDING SRC TO DST
               ROUNDED MODE IS NEAREST-AWAY-FROM-ZERO.
           MOVE P OF DST TO EP. MOVE Q OF DST TO EQ.
           DISPLAY "NEAR  P=" EP " Q=" EQ.
           STOP RUN.
