*>--- conformance-case
*> id: usage_packed_no_sign
*> rules: [GR-13.18.60.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 USAGE clause §13.18.60.4 GR11 — PACKED-DECIMAL WITH NO SIGN reserves no sign nibble (SR31 forbids
      *> 'S'); the value is always considered zero-or-positive. The ONLY observable difference from plain unsigned
      *> packed is the byte width: NO SIGN = ceil(Digits/2); plain packed = Digits/2+1 (still a sign nibble). The
      *> delta shows only for EVEN digit counts — 9(6): 3 vs 4. A negative MOVE stores the magnitude (§8.5.1.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NOSIGN23.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-PLAIN   PIC 9(6) PACKED-DECIMAL.
       01 WS-NOSIGN  PIC 9(6) PACKED-DECIMAL WITH NO SIGN.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -150 TO WS-NOSIGN
           DISPLAY "VAL=" WS-NOSIGN
           DISPLAY "BL-PLAIN=" FUNCTION BYTE-LENGTH(WS-PLAIN)
           DISPLAY "BL-NOSIGN=" FUNCTION BYTE-LENGTH(WS-NOSIGN)
           STOP RUN.
