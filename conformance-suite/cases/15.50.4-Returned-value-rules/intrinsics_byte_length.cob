*>--- conformance-case
*> id: intrinsics_byte_length
*> rules: [RV-15.50.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.14 BYTE-LENGTH — the byte-counting twin of FUNCTION LENGTH (§15.50); the D7 distinction
      *> (P11 Step 7). A COMPILE-TIME FOLD. Per-usage byte widths are IMPLEMENTOR-DEFINED (§13.18.60);
      *> COBOL.NET's pinned widths (docs/rearchitecture/PHASE-11-scout-notes.md spec:byte-length +
      *> COBOLNET_INTRINSICS_DESIGN): DISPLAY = 1 byte/position, NATIONAL = 2 bytes/position (UTF-16),
      *> BINARY(COMP) = StorageWidth (S9(4) -> 2), PACKED(COMP-3) = digits/2+1 (S9(5) -> 3), BINARY-CHAR = 1.
      *> The national probe proves BYTE-LENGTH != LENGTH (6 vs 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BYTELEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X    PIC X(5).
       01 WS-N    PIC N(3).
       01 WS-COMP PIC S9(4) USAGE COMP.
       01 WS-PACK PIC S9(5) USAGE COMP-3.
       01 WS-BC   USAGE BINARY-CHAR.
       01 WS-GRP.
          05 WS-G-A PIC X(3).
          05 WS-G-N PIC N(2).
       PROCEDURE DIVISION.
       MAIN.
      *> DISPLAY: 5 bytes = 5 positions.
           DISPLAY "BL-X=" FUNCTION BYTE-LENGTH(WS-X)
      *> NATIONAL: BYTE-LENGTH = 6 (2 bytes/position), FUNCTION LENGTH = 3 (positions) — the D7 divergence.
           DISPLAY "BL-N=" FUNCTION BYTE-LENGTH(WS-N)
           DISPLAY "LEN-N=" FUNCTION LENGTH(WS-N)
      *> BINARY S9(4): StorageWidth = 2 bytes.
           DISPLAY "BL-COMP=" FUNCTION BYTE-LENGTH(WS-COMP)
      *> PACKED S9(5): digits/2 + 1 = 3 bytes.
           DISPLAY "BL-PACK=" FUNCTION BYTE-LENGTH(WS-PACK)
      *> BINARY-CHAR: 1 byte.
           DISPLAY "BL-BC=" FUNCTION BYTE-LENGTH(WS-BC)
      *> Group: 3 (X(3)) + 2*2 (N(2)) = 7 bytes.
           DISPLAY "BL-GRP=" FUNCTION BYTE-LENGTH(WS-GRP)
      *> An alphanumeric literal (a valid §15.14.3 literal argument): 5 bytes.
           DISPLAY "BL-LIT=" FUNCTION BYTE-LENGTH("ABCDE")
           STOP RUN.
