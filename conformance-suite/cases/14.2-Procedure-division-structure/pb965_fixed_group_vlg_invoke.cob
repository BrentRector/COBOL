*>--- conformance-case
*> id: pb965_fixed_group_vlg_invoke
*> rules: [GR-14.2.3-8, SR-14.8.2.2-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB965 - A FIXED-LENGTH GROUP ARGUMENT INTO A VARIABLE-LENGTH GROUP FORMAL OF A METHOD.
      *>
      *> The INVOKE twin of pb965_fixed_group_vlg_boundary. 14.8.2.2: "If either the formal parameter or the
      *> argument is a variable length group, the formal parameter and the argument shall be compatible, as
      *> described in 8.5.1.12" - and 8.5.1.12.1 admits a fixed-length group on one side. ST starts at
      *> relative position 2 as L1 does, so they correspond (8.5.1.12.2) and ST crosses as a table of its
      *> fixed 3 occurrences (8.5.1.12.3). This used to stop the run with a not-implemented "Tier-C byte
      *> island" that blamed a dynamic member the FIXED argument does not have.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   M   - the formal sees capacity 3 and [CD][TTT][EF].
      *>   SG  - 14.2.3 GR8: the method's stores L0 := qq and L1(2) := u are visible in the argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965VI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB965C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB965C.
       01 SG.
          05 S1 PIC X(2) VALUE "CD".
          05 ST PIC X OCCURS 3 VALUE "T".
          05 S3 PIC X(2) VALUE "EF".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB965C "NEW" RETURNING O
           INVOKE O "XFER" USING SG
           DISPLAY "SG=[" SG "]"
           STOP RUN.
       END PROGRAM PB965VI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB965C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.

       METHOD-ID. XFER.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 L0 PIC X(2).
          05 L1 PIC X OCCURS DYNAMIC CAPACITY IN LCAP.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LG.
       MAIN-P.
           DISPLAY "M  LCAP=" LCAP " [" L0 "][" L1(1) L1(2) L1(3)
               "][" L3 "]"
           MOVE "qq" TO L0
           MOVE "u" TO L1(2).
       END METHOD XFER.

       END OBJECT.
       END CLASS PB965C.
