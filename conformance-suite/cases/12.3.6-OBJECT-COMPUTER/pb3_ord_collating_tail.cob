*>--- conformance-case
*> id: pb3_ord_collating_tail
*> rules: [GR-12.3.6.4-2, GR-12.3.6.4-9, GR-12.3.6.4-11, RV-15.70.4-1,
*>    FMT-12.3.6.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB3 - ORD under a custom PROGRAM COLLATING SEQUENCE, including a character PAST the 256-entry
      *> alphanumeric weights table. ISO 12.3.7.4 GR7 1.3: "Any characters of the native collating sequence
      *> that are not specified in the literal phrase shall assume a position in the collating sequence that
      *> is greater than that of the highest character specified in this literal phrase. The relative order
      *> within the set of these unspecified characters is unchanged from the native collating sequence."
      *>
      *> Derivation for ALPHABET AL IS "A" ALSO "B" (12.3.7.4 GR7 1.6 - ALSO assigns ONE ordinal position):
      *>   {A,B} at 1 · 0x00..0x40 at 2..66 · 0x43..0xFF at 67..255 · then U+0100 at 256, U+0101 at 257,
      *>   ... and X"FF" (the byte 0xFF is U+FFFF, the last native character: the storage-byte law, kb/Work
      *>   PB2919) at 65535, since the 65,534 characters other than A and B take positions 2..65535.
      *> The old c+1 fallback returned 257 for U+0100, leaving ordinal 256 occupied by nothing - and 15.70.4
      *> r1 returns "the ordinal position", which a sequence containing a hole does not have.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3ORDPCS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "A" ALSO "B".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HI  PIC X VALUE "ÿ".
       01 HV  PIC X VALUE X"FF".
       01 U   PIC X VALUE "Ā".
       01 U2  PIC X VALUE "ā".
       01 R   PIC 9(5).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ORD("A")
           DISPLAY R
           COMPUTE R = FUNCTION ORD("B")
           DISPLAY R
           COMPUTE R = FUNCTION ORD("C")
           DISPLAY R
           COMPUTE R = FUNCTION ORD(HI)
           DISPLAY R
           COMPUTE R = FUNCTION ORD(U)
           DISPLAY R
           COMPUTE R = FUNCTION ORD(U2)
           DISPLAY R
           COMPUTE R = FUNCTION ORD(HV)
           DISPLAY R
           STOP RUN.
