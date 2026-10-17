*>--- conformance-case
*> id: ca3_figurative_display_pcs
*> rules: [GR-14.9.11.4-3, GR-14.9.11.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> CA3 (CONFORMANCE-FIX-QUEUE): when HIGH-VALUE / LOW-VALUE is referenced at runtime (DISPLAY included, ISO
      *> §8.3.3.6.4 GR6/GR7 + NOTE 2) it is the character with the highest / lowest ordinal position in the runtime
      *> alphanumeric PROGRAM COLLATING SEQUENCE — NOT the native pin. Under ALPHABET AL (Z..A occupy the first 26
      *> positions) the LOWEST position is 'Z', so LOW-VALUE = 'Z'. A MOVE of the figurative already threaded the
      *> PCS (WS = 'Z'); DISPLAY of the BARE figurative ignored it and emitted the native pin X"00" — a GR6/GR7
      *> divergence and an internal inconsistency with the MOVE path. Both paths now agree ('Z').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE LOW-VALUE TO WS-X
           DISPLAY "MOVED=" WS-X
           DISPLAY "BARE=" LOW-VALUE
           STOP RUN.
