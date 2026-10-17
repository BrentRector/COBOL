*>--- conformance-case
*> id: intrinsics_boolean_conv
*> rules: [GR-15.2-2, AR-15.3-3, RV-15.13.4-1, AR-15.45.3-1,
*>    RV-15.45.4-1, FMT-15.13.2, FMT-15.45.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.13 BOOLEAN-OF-INTEGER / §15.45 INTEGER-OF-BOOLEAN — the boolean-conversion pair (P11 Step 2).
      *> Values hand-derived in docs/rearchitecture/PHASE-11-scout-notes.md (spec:boolean): §15.13.4 r1 —
      *> rightmost boolean position = low-order binary digit, zero-filled or TRUNCATED ON THE LEFT to
      *> argument-2 positions (truncation is NORMAL: Annex D.10's 544 → low-6-bits worked example); the MOVE
      *> into a longer PIC 1(n) receiver then right-zero-fills (§14.6.8.6 — the OPPOSITE end). §15.45.4 r1 —
      *> the unsigned binary value of the whole bit configuration, MSB first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11BOOLCONV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIT-8      PIC 1(8) USAGE BIT.
       01 INT-ITEM   PIC 9(5) VALUE 544.
       01 N-3        PIC 9(3).
       01 N-5        PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.13.4 r1 exact fit: 5 = 101 over 8 positions, left zero-fill.
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8) TO BIT-8
           IF BIT-8 = B"00000101" DISPLAY "T1=OK"
              ELSE DISPLAY "T1=" BIT-8 END-IF
      *> Annex D.10: 544 = 0b1000100000 → the low 6 bits B"100000"; the MOVE into PIC 1(8)
      *> left-justifies and right-zero-fills (§14.6.8.6) → B"10000000".
           MOVE FUNCTION BOOLEAN-OF-INTEGER(INT-ITEM, 6) TO BIT-8
           IF BIT-8 = B"10000000" DISPLAY "T2=OK"
              ELSE DISPLAY "T2=" BIT-8 END-IF
      *> §15.45.4 r1 MSB-first: B"10000000" = 128 (an LSB-first bug would yield 1).
           COMPUTE N-3 = FUNCTION INTEGER-OF-BOOLEAN(BIT-8)
           DISPLAY "T3=" N-3
      *> §15.45.4 r1 over a boolean literal argument (§15.3 item 3).
           COMPUTE N-3 = FUNCTION INTEGER-OF-BOOLEAN(B"00000101")
           DISPLAY "T4=" N-3
      *> Round-trip identity for 0 <= n < 2^k (§15.13.4 r1 ∘ §15.45.4 r1, both unsigned MSB-first).
           COMPUTE N-5 = FUNCTION INTEGER-OF-BOOLEAN(
               FUNCTION BOOLEAN-OF-INTEGER(200, 8))
           DISPLAY "T5=" N-5
      *> §15.13.4 r1 left truncation: 256 mod 2^8 = 0 → all-zero bits.
           MOVE FUNCTION BOOLEAN-OF-INTEGER(256, 8) TO BIT-8
           IF BIT-8 = B"00000000" DISPLAY "T6=OK"
              ELSE DISPLAY "T6=" BIT-8 END-IF
           STOP RUN.
