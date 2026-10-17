*>--- conformance-case
*> id: pb965_vlg_into_fixed_invoke
*> rules: [GR-14.2.3-8, SR-14.8.2.2-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB965 (finisher) - A VARIABLE-LENGTH GROUP ARGUMENT INTO A FIXED-LENGTH GROUP FORMAL OF A
      *> METHOD, and both mixed RETURNING pairs of an INVOKE.
      *>
      *> The INVOKE twin of pb965_vlg_into_fixed_boundary. 14.8.2.2: "If either the formal parameter or the
      *> argument is a variable length group, the formal parameter and the argument shall be compatible, as
      *> described in 8.5.1.12"; 14.8.3.2 says the same of a RETURNING pair and applies its length rule only
      *> when "neither of them is strongly typed or a variable length group". The compiler refused both
      *> RETURNING pairs (COBOLNET0828, "character length mismatch (formal 7, argument 5)") by comparing the
      *> variable-length group's collapsed width.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   LF   - VT has capacity 2 opposite LF's 3-occurrence LT; 14.6.9.2 space fills the third -> [ghkl ij].
      *>   SG   - the method's variable-length returning item LR (capacity 3) into the fixed SG (8.5.1.12.3).
      *>   VG   - 14.2.3 GR8: L1 := qq and LT(1..3) := m n o reach VG. LT(3) is a receiving reference to
      *>          VT with a subscript that "exceeds the current capacity of the table", so 8.5.1.9.3
      *>          creates the third occurrence and VCAP becomes 3 (kb/Work PB2690).
      *>   VG2  - the second method returns a FIXED item into VG: its 3 occurrences become VT at capacity 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB965FK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB965FK.
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
           INVOKE PB965FK "NEW" RETURNING O
           INVOKE O "XFER" USING VG RETURNING SG
           DISPLAY "SG=[" SG "]"
           DISPLAY "VG=[" V1 "][" VT(1) VT(2) VT(3) "][" V3 "] VCAP="
               VCAP
           INVOKE O "FIXR" RETURNING VG
           DISPLAY "VG2=[" V1 "][" VT(1) VT(2) VT(3) "][" V3 "] VCAP="
               VCAP
           STOP RUN.
       END PROGRAM PB965FI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB965FK INHERITS FROM BASE.
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

       END OBJECT.
       END CLASS PB965FK.
