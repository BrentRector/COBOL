      *> kb/Work PB2087 (the INVOKE arm) - a FIXED-length group that the
      *> program also passes BY REFERENCE lives in a storage cell (every
      *> BY REFERENCE operand is claimed onto one, so that a formal can
      *> occupy "the same storage area as the argument", 14.2.3 GR8), and
      *> it is still the same fixed group when it RECEIVES a method's
      *> VARIABLE-length returning item or crosses into a variable-length
      *> formal. 14.8.3.2: "If either the sending or the receiving operand
      *> is a variable length group, the sending operand and the receiving
      *> operand shall be compatible, as described in 8.5.1.12".
      *> cite.py --check 14.8.3.2 "If either the sending or the receiving
      *>   operand is a variable length group" -> OK 14.8.3.2
      *> cite.py --check 14.2.3 "If the argument is passed by reference,
      *>   the activated runtime element operates as if the formal
      *>   parameter occupies the same storage area as the argument"
      *>   -> OK 14.2.3 8)
      *> The values are pb965_vlg_into_fixed_invoke's (the same program
      *> plus the TOUCH that puts SG in a cell): LF=[ghkl ij],
      *> SG=[tuvwxyz], VG=[qq][mn][ij] VCAP=0000000002,
      *> VG2=[mn][opq][rs] VCAP=0000000003, and TOUCH sees SG's 7
      *> characters. Before the fix the RETURNING delivery into the cell
      *> view of SG stopped at a run-time "not yet implemented" loud.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2087V14.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2087VK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE P2087VK.
       01 SG.
          05 S1 PIC X(2) VALUE "CD".
          05 ST PIC X OCCURS 3 VALUE "T".
          05 S3 PIC X(2) VALUE "EF".
       01 VG.
          05 V1 PIC X(2).
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN VCAP.
          05 V3 PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "gh" TO V1 MOVE "ij" TO V3
           MOVE "k" TO VT(1) MOVE "l" TO VT(2)
           INVOKE P2087VK "NEW" RETURNING O
           INVOKE O "XFER" USING VG RETURNING SG
           DISPLAY "SG=[" SG "]"
           DISPLAY "VG=[" V1 "][" VT(1) VT(2) "][" V3 "] VCAP=" VCAP
           INVOKE O "FIXR" RETURNING VG
           DISPLAY "VG2=[" V1 "][" VT(1) VT(2) VT(3) "][" V3 "] VCAP="
               VCAP
           INVOKE O "TOUCH" USING SG
           STOP RUN.
       END PROGRAM P2087V14.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2087VK INHERITS FROM BASE.
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
       01 LF.
          05 L1 PIC X(2).
          05 LT PIC X OCCURS 3.
          05 L3 PIC X(2).
       01 LR.
          05 R1 PIC X(2).
          05 RT PIC X OCCURS DYNAMIC CAPACITY IN RCAP.
          05 R3 PIC X(2).
       PROCEDURE DIVISION USING LF RETURNING LR.
       MAIN-P.
           DISPLAY "LF=[" LF "]"
           MOVE "qq" TO L1
           MOVE "m" TO LT(1) MOVE "n" TO LT(2) MOVE "o" TO LT(3)
           MOVE "tu" TO R1 MOVE "yz" TO R3
           MOVE "v" TO RT(1) MOVE "w" TO RT(2) MOVE "x" TO RT(3).
       END METHOD XFER.

       METHOD-ID. FIXR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RFX.
          05 F1 PIC X(2).
          05 FT PIC X OCCURS 3.
          05 F3 PIC X(2).
       PROCEDURE DIVISION RETURNING RFX.
       MAIN-P.
           MOVE "mnopqrs" TO RFX.
       END METHOD FIXR.

       METHOD-ID. TOUCH.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TG PIC X(7).
       PROCEDURE DIVISION USING TG.
       MAIN-P.
           DISPLAY "TOUCH " TG.
       END METHOD TOUCH.

       END OBJECT.
       END CLASS P2087VK.
