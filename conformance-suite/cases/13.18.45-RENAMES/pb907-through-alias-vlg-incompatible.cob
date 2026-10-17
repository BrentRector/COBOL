*>--- conformance-case
*> id: pb907-through-alias-vlg-incompatible
*> rules: [GR-13.18.45.4-2, GR-14.9.25.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB907 - THE ALIAS SENDER GETS 8.5.1.12's COMPATIBILITY ANSWER, NOT A FALSE KIND CLAIM.
      *> 13.18.45.4 GR2 makes SALIAS "an alphanumeric group item", so 14.9.25.3 SR9 asks whether it and
      *> the variable-length group DST are compatible. They are not: D1 is a dynamic-capacity table at
      *> relative byte position 0 and SALIAS has no table there (8.5.1.12.1 rule 1, "For each
      *> dynamic-capacity table in either group there is a corresponding table in the other group", with
      *> 8.5.1.12.2's positional correspondence). The expected diagnostic is that reason - the one the
      *> structural twin (a plain group with SALIAS's layout) draws - and never "the sending operand is
      *> not a group item", which is what this program used to be told.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB907VLN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 S1 PIC X(3) VALUE "ABC".
          05 S2 PIC X(3) VALUE "DEF".
       66 SALIAS RENAMES S1 THROUGH S2.
       01 DST.
          05 D1 PIC X(1) OCCURS DYNAMIC CAPACITY IN CAPN.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SALIAS TO DST
           STOP RUN.
