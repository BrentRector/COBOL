*>--- conformance-case
*> id: binary_usage
*> rules: [GR-13.18.60.4-12]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60 — COBOL-2002 fixed-width binary usages BINARY-CHAR/SHORT/LONG/DOUBLE.
      *> No PICTURE; native two's-complement of 1/2/4/8 bytes. SIGNED is the default; UNSIGNED
      *> widens the positive range. Realized as COMP-5 (full binary capacity) at runtime.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BINUSAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BC   USAGE BINARY-CHAR.
       01 BCU  USAGE BINARY-CHAR UNSIGNED.
       01 BS   USAGE BINARY-SHORT.
       01 BL   USAGE BINARY-LONG.
       01 BD   USAGE BINARY-DOUBLE.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 127 TO BC.
           DISPLAY "BC=" BC.
           MOVE -128 TO BC.
           DISPLAY "BCNEG=" BC.
           MOVE 255 TO BCU.
           DISPLAY "BCU=" BCU.
           MOVE 30000 TO BS.
           ADD 2000 TO BS.
           DISPLAY "BS=" BS.
           MOVE 2000000000 TO BL.
           DISPLAY "BL=" BL.
           MOVE 9000000000000000000 TO BD.
           DISPLAY "BD=" BD.
           MOVE 100 TO BC.
           COMPUTE BL = BC * 10.
           DISPLAY "COMP=" BL.
           STOP RUN.
