*>--- conformance-case
*> id: highest_algebraic_comp5_unsigned
*> rules: [GR-14.7.5-5, DOC-A.1-179, DOC-A.1-208]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R10 (Phase-B F73/F74/F75) - unsigned COMP-5 carriers own the full container range.
      *> Derived from ISO/IEC 1989:2023:
      *>   15.43.4 r2 - HIGHEST-ALGEBRAIC returns "the positive algebraic value of greatest finite
      *>                magnitude that may be represented in argument-1"; the 15.43.4 NOTE table's
      *>                BINARY-CHAR UNSIGNED row (+255 on an 8-bit container) fixes the container-range
      *>                reading for the fixed-width binary usages, and COMP-5's documented representation
      *>                (13.18.60.4 GR12; CONFORMANCE.md) is the same full-container rule:
      *>                8-byte (10-18 digits) -> 2^64-1; 16-byte (19-31 digits) -> 2^128-1 unsigned,
      *>                2^127-1 signed (LOWEST -2^127, 15.58.4 r2).
      *>   14.9.25.4 GR6d2b - an unsigned receiver stores the absolute value, no operational sign.
      *>   14.7.5 - the size error condition. F74's shift-mask bug collapsed the 16-byte container
      *>                range to [0,1) unsigned / empty signed, so EVERY checked 16-byte store reported
      *>                size error; the fixed boundary fires only genuinely (at the max: ADD 1 = SIZE,
      *>                SUBTRACT 1 = NOSIZE). The documented native intermediate is Int128
      *>                (8.8.1.3 implementor-defined; CONFORMANCE.md): reading an operand beyond it
      *>                raises the size error condition rather than silently wrapping.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R10COMP5U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W18   PIC 9(18) COMP-5.
       01 W19   PIC 9(19) COMP-5.
       01 S19   PIC S9(19) COMP-5.
       01 BIG20 PIC 9(20).
       01 R31   PIC 9(31).
       01 R31B  PIC 9(31).
       PROCEDURE DIVISION.
      *> F75: the 8-byte unsigned container max survives its own storage and a 20-digit receiver.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(W18) TO W18
           MOVE W18 TO BIG20
           DISPLAY "W18-MAX=" BIG20
      *> F73: the 16-byte unsigned container max folds (2^128-1, a 39-digit synthesized literal),
      *> stores, and truncates mod 10^31 into a 31-digit receiver.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(W19) TO R31
           DISPLAY "W19-MOD31=" R31
      *> The UInt128 carrier round trip: the fold stored into the item itself and read back out.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(W19) TO W19
           MOVE W19 TO R31B
           IF R31B = R31 DISPLAY "W19-ROUNDTRIP=OK"
              ELSE DISPLAY "W19-ROUNDTRIP=BAD " R31B END-IF
      *> The unsigned-wide relation compares the full range (never the Int128 arithmetic funnel).
           IF W19 = FUNCTION HIGHEST-ALGEBRAIC(W19) DISPLAY "W19-EQ=OK"
              ELSE DISPLAY "W19-EQ=BAD" END-IF
      *> F74: the 16-byte signed SIZE ERROR boundary is genuine - out of range one past the max,
      *> in range one under it, out of range one under the LOWEST fold.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(S19) TO S19
           ADD 1 TO S19
             ON SIZE ERROR DISPLAY "S19-ADD1=SIZE"
             NOT ON SIZE ERROR DISPLAY "S19-ADD1=NOSIZE"
           END-ADD
           SUBTRACT 1 FROM S19
             ON SIZE ERROR DISPLAY "S19-SUB1=SIZE"
             NOT ON SIZE ERROR DISPLAY "S19-SUB1=NOSIZE"
           END-SUBTRACT
           MOVE FUNCTION LOWEST-ALGEBRAIC(S19) TO S19
           SUBTRACT 1 FROM S19
             ON SIZE ERROR DISPLAY "S19-LOWSUB=SIZE"
             NOT ON SIZE ERROR DISPLAY "S19-LOWSUB=NOSIZE"
           END-SUBTRACT
      *> The documented Int128 intermediate: arithmetic on an operand beyond it (the item holds
      *> 2^128-1 here) raises the size error condition - loud, never a wrap.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(W19) TO W19
           ADD 1 TO W19
             ON SIZE ERROR DISPLAY "W19-ADD1=SIZE"
             NOT ON SIZE ERROR DISPLAY "W19-ADD1=NOSIZE"
           END-ADD
           STOP RUN.
