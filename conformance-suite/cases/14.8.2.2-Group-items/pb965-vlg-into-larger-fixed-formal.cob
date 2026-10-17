*>--- conformance-case
*> id: pb965-vlg-into-larger-fixed-formal
*> rules: [SR-14.8.2.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.8.2.2 rule 1: if the argument is passed by reference, "the formal parameter shall be
      *> described with the same number or a smaller number of bytes as the corresponding argument". The fixed
      *> formal LF is an alphanumeric group item (3.11), so the rule binds the pair, and 8.5.1.12.3 sets the
      *> lengths it compares: "For purposes of determining compatibility, the dynamic-capacity table is
      *> considered to be the same length as the corresponding table". VG therefore counts 2+3+2 = 7 and LF
      *> counts 2+3+2+3 = 10 - the formal is LARGER, so the AS NESTED CALL is rejected (COBOLNET1688). The
      *> counterpart of 2014/pb965_vlg_into_fixed_boundary, whose 7-against-7 pair is admitted (kb/Work PB965).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965NL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 V1 PIC X(2).
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN VCAP.
          05 V3 PIC X(2).
       PROCEDURE DIVISION.
           CALL "PB965NM" AS NESTED USING VG
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965NM.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 L1 PIC X(2).
          05 LT PIC X OCCURS 3.
          05 L3 PIC X(2).
          05 L4 PIC X(3).
       PROCEDURE DIVISION USING LF.
           GOBACK.
       END PROGRAM PB965NM.
       END PROGRAM PB965NL.
