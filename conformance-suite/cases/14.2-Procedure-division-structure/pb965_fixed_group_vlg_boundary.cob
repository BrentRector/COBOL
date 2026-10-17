*>--- conformance-case
*> id: pb965_fixed_group_vlg_boundary
*> rules: [GR-14.2.3-8, SR-14.8.2.2-2, GR-14.9.4.4-4]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB965 - A FIXED-LENGTH GROUP OPPOSITE A VARIABLE-LENGTH GROUP ACROSS A CALL BOUNDARY.
      *>
      *> 14.8.2.2: "If either the formal parameter or the argument is a variable length group, the formal
      *> parameter and the argument shall be compatible, as described in 8.5.1.12, Variable-length groups."
      *> 8.5.1.12.1 admits the pair ("only one of the operands may be a variable-length group"). 14.8.3.2 says
      *> the same of a RETURNING pair. 8.5.1.12.2: "Two tables correspond if at least one of them is a
      *> dynamic-capacity table and they occupy the same relative byte positions within their groups" - ST
      *> (and the alias's AQ, and the returning items' tables) start at relative position 2, as the dynamic
      *> table does. 8.5.1.12.3 treats the fixed table "as though it were a dynamic-capacity table whose
      *> capacity is either its fixed number of occurrences", so it crosses at capacity 3.
      *> This used to cross as an EMPTY carrier (capacity 0, every field blank) and the callee's stores were
      *> lost - a silent wrong answer; RETURNING a fixed group into a variable-length receiver aborted.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   N/X - the formal sees the argument's own content: capacity 3, [CD][TTT][EF].
      *>   C1  - 14.2.3 GR8 ("operates as if the formal parameter occupies the same storage area as the
      *>         argument"): L0 := qq and L1(2) := u land in SG, so SRC = AB qq TuT EF.
      *>   C2  - the same through a level-66 THROUGH alias (13.18.45.4 GR2 - a group item): ab qq tut ef.
      *>   C3  - a separately compiled callee (no AS NESTED), BY REFERENCE: rr and L1(3) := v land.
      *>   C4  - 14.2.3 GR9, BY CONTENT: a copy - the callee's stores reach nothing, SRC is unchanged.
      *>   R1  - 14.6.5 / 14.8.3.2: the fixed returning item's content mnopqrs lands in VG, its table
      *>         at capacity 3 (8.5.1.12.3).
      *>   R2  - the reverse: a capacity-2 dynamic table into a 3-occurrence fixed table; 14.6.9.2's rule
      *>         for a non-dynamic receiving table space-fills the remaining element: tu vw_ yz.
      *>   M1/M2 - 14.9.25.4 GR9 between LEAD and VG: LA (at position 0) is opposite V1's plain bytes and so
      *>         corresponds to NOTHING; only LB (at position 2) corresponds to VT. VT takes bbb, V1 takes aa,
      *>         and the move back returns zz bbb cc.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 S0 PIC X(2) VALUE "AB".
          05 SG.
             10 S1 PIC X(2) VALUE "CD".
             10 ST PIC X OCCURS 3 VALUE "T".
             10 S3 PIC X(2) VALUE "EF".
       01 ASRC.
          05 A0 PIC X(2) VALUE "ab".
          05 A1 PIC X(2) VALUE "cd".
          05 AQ PIC X OCCURS 3 VALUE "t".
          05 A3 PIC X(2) VALUE "ef".
       66 AALIAS RENAMES A1 THROUGH A3.
       01 LEAD.
          05 LA PIC X OCCURS 2 VALUE "a".
          05 LB PIC X OCCURS 3 VALUE "b".
          05 LC PIC X(2) VALUE "cc".
       01 VG.
          05 V1 PIC X(2).
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN VCAP.
          05 V3 PIC X(2).
       01 FG.
          05 F1 PIC X(2).
          05 FT PIC X OCCURS 3.
          05 F3 PIC X(2).
       PROCEDURE DIVISION.
           CALL "PB965N" AS NESTED USING SG
           DISPLAY "C1 SRC=[" SRC "]"
           CALL "PB965N" AS NESTED USING AALIAS
           DISPLAY "C2 ASRC=[" ASRC "]"
           CALL "PB965X" USING SG
           DISPLAY "C3 SRC=[" SRC "]"
           CALL "PB965X" USING BY CONTENT SG
           DISPLAY "C4 SRC=[" SRC "]"
           CALL "PB965F" RETURNING VG
           DISPLAY "R1 VCAP=" VCAP " VG=[" V1 "][" VT(1) VT(2) VT(3)
               "][" V3 "]"
           CALL "PB965V" RETURNING FG
           DISPLAY "R2 FG=[" FG "]"
           MOVE LEAD TO VG
           DISPLAY "M1 VCAP=" VCAP " VG=[" V1 "][" VT(1) VT(2) VT(3)
               "][" V3 "]"
           MOVE "zz" TO V1
           MOVE VG TO LEAD
           DISPLAY "M2 LEAD=[" LEAD "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965N.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 L0 PIC X(2).
          05 L1 PIC X OCCURS DYNAMIC CAPACITY IN LCAP.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LG.
           DISPLAY "N  LCAP=" LCAP " [" L0 "][" L1(1) L1(2) L1(3)
               "][" L3 "]"
           MOVE "qq" TO L0
           MOVE "u" TO L1(2)
           GOBACK.
       END PROGRAM PB965N.
       END PROGRAM PB965M.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965X.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 L0 PIC X(2).
          05 L1 PIC X OCCURS DYNAMIC CAPACITY IN LCAP.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LG.
           DISPLAY "X  LCAP=" LCAP " [" L0 "][" L1(1) L1(2) L1(3)
               "][" L3 "]"
           MOVE "rr" TO L0
           MOVE "v" TO L1(3)
           GOBACK.
       END PROGRAM PB965X.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RFX.
          05 R1 PIC X(2).
          05 RT PIC X OCCURS 3.
          05 R3 PIC X(2).
       PROCEDURE DIVISION RETURNING RFX.
           MOVE "mnopqrs" TO RFX
           GOBACK.
       END PROGRAM PB965F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965V.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV.
          05 W1 PIC X(2).
          05 WT PIC X OCCURS DYNAMIC CAPACITY IN WCAP.
          05 W3 PIC X(2).
       PROCEDURE DIVISION RETURNING RV.
           MOVE "tu" TO W1
           MOVE "yz" TO W3
           MOVE "v" TO WT(1)
           MOVE "w" TO WT(2)
           GOBACK.
       END PROGRAM PB965V.
