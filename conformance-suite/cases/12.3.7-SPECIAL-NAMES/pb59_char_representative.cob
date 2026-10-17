*>--- conformance-case
*> id: pb59_char_representative
*> rules: [GR-12.3.7.4-7, AR-15.15.3-2, RV-15.15.4-1, RV-15.15.4-2, DOC-A.1-90]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / RV-15.15.4-1, RV-15.15.4-2, AR-15.15.3-2 — CHAR under an ALSO-collapsing alphabet:
      *> the representative, the tail arithmetic, and the domain bound, all §12.3.7.4 GR7 1.3/1.6.
      *> ALPHABET AL IS "C" ALSO "A" ALSO "B": three characters share position 0 (literal-1 "C" is
      *> "the first character defined" — §15.15.4 r2's CHAR(1), NOT the lowest-coded member); the
      *> remaining 253 Latin-1 units take positions 1..253 (nextFree 254); every code unit above the
      *> 256-block continues at 254+ in native order, so CHAR(255) = U+0100 (pre-fix: the EC default,
      *> ORD 34) and the sequence has 254 + 65280 = 65534 positions (§15.15.3 r2: ordinal 65534 legal,
      *> 65535 refused — pre-fix the bound was inverted: 255 refused, 65536 admitted). Every value
      *> derived independently (python oracle, PB59-3 apply plan). ORD legs are the encoding-immune
      *> witnesses; the IF leg proves the comparison arm agrees with ORD about the tail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59CHREP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-PC
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "C" ALSO "A" ALSO "B".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C1    PIC X.
       01 ORDV  PIC 9(6).
       PROCEDURE DIVISION.
           MOVE FUNCTION CHAR(1) TO C1
           DISPLAY "CH1  =[" C1 "]"
           COMPUTE ORDV = FUNCTION ORD(FUNCTION CHAR(255))
           DISPLAY "OR255=" ORDV
           COMPUTE ORDV = FUNCTION ORD(FUNCTION CHAR(257))
           DISPLAY "OR257=" ORDV
           COMPUTE ORDV = FUNCTION ORD(FUNCTION CHAR(65534))
           DISPLAY "ORMAX=" ORDV
           MOVE FUNCTION CHAR(65535) TO C1
           IF C1 = SPACE
               DISPLAY "OVER =DEFAULT"
           ELSE
               DISPLAY "OVER =VALUE"
           END-IF
           COMPUTE ORDV = FUNCTION ORD("A")
           DISPLAY "ORDA =" ORDV
           COMPUTE ORDV = FUNCTION ORD("D")
           DISPLAY "ORDD =" ORDV
           IF FUNCTION CHAR(255) < FUNCTION CHAR(256)
               DISPLAY "CMP  =LT"
           ELSE
               DISPLAY "CMP  =GE"
           END-IF
           STOP RUN.
