      *>--- conformance-case
      *> id: pb2919_hex_literal_ff_is_the_storage_byte
      *> rules: [GR-8.3.3.2.4-4, DOC-A.1-95]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2919 - THE BYTE 0xFF HAS ONE CHARACTER. A hexadecimal
      *> alphanumeric literal X"hh" is the alphanumeric character whose
      *> bit configuration is hh:
      *>   cite.py --check 8.3.3.2.4 "each of which has the bit
      *>     configuration specified by one occurrence of
      *>     hex-character-sequence-1"  -> OK  §8.3.3.2.4 4)
      *> A binary item's storage is two's complement, most significant
      *> byte first (§13.18.60.4 GR4 leaves the precise representation
      *> to the implementor, docs/CONFORMANCE.md DOC-A.1-205):
      *>   cite.py --check 13.18.60.4 "The USAGE BINARY clause
      *>     specifies that a radix of 2 is used to represent a numeric
      *>     item in the storage of the computer"  -> OK  §13.18.60.4 4)
      *> so S = -2 in PIC S9(9) COMP is the four bytes FF FF FF FE, and
      *> an alphanumeric REDEFINES of it is the string of those four
      *> bytes (§13.18.44.4 GR1: the redefining item's storage starts
      *> at the first bit of the redefined item). Byte 0xFF is the
      *> character U+FFFF, the native
      *> HIGH-VALUE, in EVERY byte-to-character image (the storage-byte
      *> law, docs/CONFORMANCE.md DOC-A.1-31), so the literal that
      *> spells the same bytes is the same string.
      *> DERIVATION OF EVERY .out LINE:
      *>   MOVE -2 TO S; GX = X"FFFFFFFE"           -> EQ-LIT
      *>   MOVE X"FFFFFFFE" TO HX; the binary T is
      *>     the two's-complement value -2          -> T=-0000002
      *>   S and T hold the same value, so GX = HX  -> EQ-IMAGE
      *>   MOVE ALL X"FF" TO HX: T = -1, and it is
      *>     the same string as HIGH-VALUES         -> ALLFF-T=-0000001
      *>                                               ALLFF-HV
      *>   X"FF" = HIGH-VALUE in an alphanumeric
      *>     item; its ordinal is the last (65536)  -> XFF-HV, ORD=65536
      *>   U+00FF (FUNCTION CHAR(256)) is a different character,
      *>     below HIGH-VALUE                       -> YU-NOT-HV
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2919HX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(9) COMP.
       01 GX REDEFINES G PIC X(4).
       01 H.
          05 T PIC S9(9) COMP.
       01 HX REDEFINES H PIC X(4).
       01 XA PIC X.
       01 TS PIC -9(7).
       01 OV PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE -2 TO S.
           IF GX = X"FFFFFFFE" DISPLAY "EQ-LIT" ELSE DISPLAY "NE-LIT"
           END-IF.
           MOVE X"FFFFFFFE" TO HX.
           MOVE T TO TS.
           DISPLAY "T=" TS.
           IF GX = HX DISPLAY "EQ-IMAGE" ELSE DISPLAY "NE-IMAGE"
           END-IF.
           MOVE ALL X"FF" TO HX.
           MOVE T TO TS.
           DISPLAY "ALLFF-T=" TS.
           IF HX = HIGH-VALUES DISPLAY "ALLFF-HV" ELSE
               DISPLAY "ALLFF-NOT-HV"
           END-IF.
           MOVE X"FF" TO XA.
           IF XA = HIGH-VALUE DISPLAY "XFF-HV" ELSE
               DISPLAY "XFF-NOT-HV"
           END-IF.
           MOVE FUNCTION ORD(XA) TO OV.
           DISPLAY "ORD=" OV.
           MOVE FUNCTION CHAR(256) TO XA.
           IF XA = HIGH-VALUE DISPLAY "YU-HV" ELSE DISPLAY "YU-NOT-HV"
           END-IF.
           STOP RUN.
