*>--- conformance-case
*> id: pb965-fixed-table-off-position
*> rules: [SR-14.8.2.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.8.2.2: "If either the formal parameter or the argument is a variable length group,
      *> the formal parameter and the argument shall be compatible, as described in 8.5.1.12". 8.5.1.12.1
      *> rule 1 requires that "for each dynamic-capacity table in either group there is a corresponding table
      *> in the other", and 8.5.1.12.2 makes tables correspond only when "they occupy the same relative byte
      *> positions within their groups". SG's table ST starts at relative position 3; the formal's dynamic
      *> table L1 starts at position 2, opposite a plain byte of S1. The pair is NOT compatible, so the
      *> AS NESTED CALL is rejected at compile time - the counterpart of the compatible pair that
      *> 2014/pb965_fixed_group_vlg_boundary proves crosses correctly (kb/Work PB965).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SG.
          05 S1 PIC X(3) VALUE "CDE".
          05 ST PIC X OCCURS 3 VALUE "T".
          05 S3 PIC X(2) VALUE "EF".
       PROCEDURE DIVISION.
           CALL "PB965NS" AS NESTED USING SG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 L0 PIC X(2).
          05 L1 PIC X OCCURS DYNAMIC CAPACITY IN LCAP.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LG.
           GOBACK.
       END PROGRAM PB965NS.
       END PROGRAM PB965NEG.
