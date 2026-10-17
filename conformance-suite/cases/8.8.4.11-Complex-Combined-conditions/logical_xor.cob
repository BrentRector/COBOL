*>--- conformance-case
*> id: logical_xor
*> rules: [FMT-8.8.4.11.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.9 / 8.8.4.11.3 — logical exclusive-or (XOR / EXCLUSIVE-OR): true iff exactly one operand
      *> condition is true. Precedence NOT > AND > XOR > OR, so `A OR B XOR C` parses as `A OR (B XOR C)`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XORTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9 VALUE 1.
       01 WS-B PIC 9 VALUE 0.
       01 WS-C PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF WS-A = 1 XOR WS-B = 1 DISPLAY "1T" ELSE DISPLAY "1F" END-IF.
           IF WS-A = 1 XOR WS-C = 1 DISPLAY "2T" ELSE DISPLAY "2F" END-IF.
           IF WS-B = 1 XOR WS-B = 1 DISPLAY "3T" ELSE DISPLAY "3F" END-IF.
           IF WS-A = 1 OR WS-B = 1 XOR WS-C = 1 DISPLAY "4T" ELSE DISPLAY "4F" END-IF.
           STOP RUN.
