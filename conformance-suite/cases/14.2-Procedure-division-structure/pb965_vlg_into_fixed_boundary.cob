*>--- conformance-case
*> id: pb965_vlg_into_fixed_boundary
*> rules: [GR-14.2.3-8, SR-14.8.2.2-2]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB965 (finisher) - A VARIABLE-LENGTH GROUP ARGUMENT INTO A FIXED-LENGTH GROUP FORMAL, and
      *> the mixed RETURNING pair, on an AS NESTED CALL.
      *>
      *> 14.8.2.2: "If either the formal parameter or the argument is a variable length group, the formal
      *> parameter and the argument shall be compatible, as described in 8.5.1.12". 14.8.2.2 rule 1 (the
      *> formal "shall be described with the same number or a smaller number of bytes as the corresponding
      *> argument") binds the fixed side, and 8.5.1.12.3 sets the lengths it compares: "For purposes of
      *> determining compatibility, the dynamic-capacity table is considered to be the same length as the
      *> corresponding table". VG's VT corresponds to LF's 3-occurrence LT, so VG counts 2+3+2 = 7 against
      *> LF's 7. The compiler used to compare VG's collapsed width (5, one element for VT) and refuse this
      *> source (COBOLNET1688), and the two RETURNING pairs too (COBOLNET1736) - although 14.8.3.2 applies
      *> its length rule only when "neither of them is strongly typed or a variable length group".
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   LF   - VT has capacity 2 opposite a 3-occurrence table; 14.6.9.2 fits it: the missing third
      *>          occurrence is space filled -> [ghkl ij].
      *>   VG   - 14.2.3 GR8 ("operates as if the formal parameter occupies the same storage area as the
      *>          argument"): L1 := qq reaches V1. The callee stores nothing into an occurrence past
      *>          the argument table's capacity (8.5.1.9.3 creates one only for a receiving
      *>          reference), so VCAP stays 2.
      *>   LC   - BY CONTENT, the same view; the callee's MOVE never reaches VG (14.2.3 GR9).
      *>   LP   - a table-less formal of 2 characters is a PREFIX (14.8.2.2 rule 1); VT lies past its last
      *>          character (8.5.1.12.2's last sentence). P1 := PP reaches V1 and the rest of VG survives.
      *>   VG2  - RETURNING a FIXED item into VG: RT's 3 occurrences become VT at capacity 3 (8.5.1.12.3).
      *>   SG   - RETURNING a variable-length item into a fixed SG: WT's 3 occurrences fill ST.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SG.
          05 S1 PIC X(2) VALUE "CD".
          05 ST PIC X OCCURS 3 VALUE "T".
          05 S3 PIC X(2) VALUE "EF".
       01 VG.
          05 V1 PIC X(2).
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN VCAP.
          05 V3 PIC X(2).
       PROCEDURE DIVISION.
           MOVE "gh" TO V1 MOVE "ij" TO V3
           MOVE "k" TO VT(1) MOVE "l" TO VT(2)
           CALL "PB965FF" AS NESTED USING VG
           DISPLAY "VG=[" V1 "][" VT(1) VT(2) "][" V3 "] VCAP=" VCAP
           CALL "PB965FC" AS NESTED USING BY CONTENT VG
           DISPLAY "VGC=[" V1 "][" VT(1) VT(2) "][" V3 "] VCAP=" VCAP
           CALL "PB965FP" AS NESTED USING VG
           DISPLAY "VGP=[" V1 "][" VT(1) VT(2) "][" V3 "] VCAP=" VCAP
           CALL "PB965FR" AS NESTED RETURNING VG
           DISPLAY "VG2=[" V1 "][" VT(1) VT(2) VT(3) "][" V3 "] VCAP="
               VCAP
           CALL "PB965FV" AS NESTED RETURNING SG
           DISPLAY "SG=[" SG "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF.
          05 L1 PIC X(2).
          05 LT PIC X OCCURS 3.
          05 L3 PIC X(2).
       PROCEDURE DIVISION USING LF.
           DISPLAY "LF=[" LF "]"
           MOVE "qq" TO L1
           GOBACK.
       END PROGRAM PB965FF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC.
          05 C1 PIC X(2).
          05 CT PIC X OCCURS 3.
          05 C3 PIC X(2).
       PROCEDURE DIVISION USING LC.
           DISPLAY "LC=[" LC "]"
           MOVE ALL "z" TO LC
           GOBACK.
       END PROGRAM PB965FC.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP.
          05 P1 PIC X(2).
       PROCEDURE DIVISION USING LP.
           DISPLAY "LP=[" LP "]"
           MOVE "PP" TO P1
           GOBACK.
       END PROGRAM PB965FP.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RFX.
          05 R1 PIC X(2).
          05 RT PIC X OCCURS 3.
          05 R3 PIC X(2).
       PROCEDURE DIVISION RETURNING RFX.
           MOVE "mnopqrs" TO RFX
           GOBACK.
       END PROGRAM PB965FR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB965FV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV.
          05 W1 PIC X(2).
          05 WT PIC X OCCURS DYNAMIC CAPACITY IN WCAP.
          05 W3 PIC X(2).
       PROCEDURE DIVISION RETURNING RV.
           MOVE "tu" TO W1 MOVE "yz" TO W3
           MOVE "v" TO WT(1) MOVE "w" TO WT(2) MOVE "x" TO WT(3)
           GOBACK.
       END PROGRAM PB965FV.
       END PROGRAM PB965F.
