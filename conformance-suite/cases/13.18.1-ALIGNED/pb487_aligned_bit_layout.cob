*>--- conformance-case
*> id: pb487_aligned_bit_layout
*> rules: [GR-13.18.1.4-1, GR-13.18.1.4-2, GR-13.18.1.4-3, FMT-13.16.2,
*>    FMT-13.18.1.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.1 ALIGNED clause (kb/Work PB487) - the clause had NO grammar rule anywhere in the frontend
      *> and was swallowed by the 13.16.2 vendor-extension catch-all, so it was ACCEPTED and INERT: the bit
      *> group below occupied ONE byte with or without it.  Every value here is derived from the standard.
      *>
      *> 13.18.1.4 GR1: "An ALIGNED clause causes the subject of the entry to be aligned on the first bit of
      *> the first available byte boundary.  Implicit filler bits may be generated to complete the assignment
      *> of bits as described in 8.5.1.6.3."
      *> 13.18.1.4 GR2: "An ALIGNED clause specified for a multiple-occurrence data item applies to each
      *> occurrence of that item."
      *> 8.5.1.6.3, rule 1: without ALIGNED, "an elementary bit data item immediately following an elementary
      *> bit data item ... of the same level" is placed "at the next bit position in storage"; rule 4: a
      *> trailing bit item in an alphanumeric group is padded "to fill an integral number of characters".
      *> 15.50.4 r9: FUNCTION LENGTH rounds a non-integral occupancy "to the next larger integer value".
      *>
      *> DERIVATIONS (8 bits per character position - the 8.1.2 implementor choice, CONFORMANCE.md 4.2.16):
      *>   NOALIGN  B1 bits 0-3; B2 shares the byte by rule 1 -> bits 4-7; extent 8 bits  -> LENGTH 1
      *>   ALIGNED  B1 bits 0-3; B2 takes GR1's next byte      -> bits 8-11; extent 12 bits,
      *>            rule 4 pads to 16                                                    -> LENGTH 2
      *>   TABLE    T occurs 3, each occurrence byte-aligned by GR2: bits 0-3, 8-11, 16-19;
      *>            extent 20 bits, rule 4 pads to 24                                    -> LENGTH 3
      *>   NOTABLE  the same table without ALIGNED: 12 contiguous bits, padded to 16      -> LENGTH 2
      *> The stored values re-read per occurrence prove the OFFSET walk agrees with the EXTENT walk - the two
      *> used to spell 8.5.1.6.3's placement rule separately, which is how an ALIGNED table could have been
      *> sized right and addressed wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB487AL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NOALIGN.
          05 N1 PIC 1(4) USAGE BIT.
          05 N2 PIC 1(4) USAGE BIT.
       01 ALIGNGRP.
          05 B1 PIC 1(4) USAGE BIT.
          05 B2 PIC 1(4) USAGE BIT ALIGNED.
       01 TBLALIGN.
          05 T PIC 1(4) USAGE BIT OCCURS 3 ALIGNED.
       01 TBLPLAIN.
          05 U PIC 1(4) USAGE BIT OCCURS 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOALIGN=" FUNCTION LENGTH(NOALIGN)
           DISPLAY "ALIGNED=" FUNCTION LENGTH(ALIGNGRP)
           DISPLAY "TABLE=" FUNCTION LENGTH(TBLALIGN)
           DISPLAY "NOTABLE=" FUNCTION LENGTH(TBLPLAIN)
           MOVE B"1100" TO B1
           MOVE B"0011" TO B2
           DISPLAY "B1=" B1 " B2=" B2
           MOVE B"1000" TO T(1)
           MOVE B"0100" TO T(2)
           MOVE B"0010" TO T(3)
           DISPLAY "T=" T(1) "/" T(2) "/" T(3)
           STOP RUN.
