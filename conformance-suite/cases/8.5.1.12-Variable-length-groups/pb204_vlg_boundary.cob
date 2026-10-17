*>--- conformance-case
*> id: pb204_vlg_boundary
*> rules: [GR-8.5.1.12.1-1, GR-8.5.1.12.1-2, GR-8.5.1.12.1-3,
*>    SR-14.9.4.3-25]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB204 - A VARIABLE-LENGTH GROUP ACROSS A FORMAT-2 CALL BOUNDARY.
      *>
      *> 14.9.4.3 SR25: "The rules for conformance specified in 14.8.2, Parameters and 14.8.3, Returning
      *> items apply." 14.8.2.2: "If either the formal parameter or the argument is a variable length group,
      *> the formal parameter and the argument shall be compatible, as described in 8.5.1.12, Variable-length
      *> groups." 14.8.3.2 says the same of a RETURNING pair. SR12's flat prohibition - "Identifier-2 shall
      *> not reference a variable-length group" - is FORMAT 1's rule and reaches neither AS NESTED nor INVOKE.
      *> So this program is conforming source, and it used to draw COBOLNET1688 at compile time.
      *>
      *> LEG 1 IS THE POSITIONAL-CORRESPONDENCE PROOF. LVG's fixed prefix is split 1+2 where VG's is one
      *> X(3) entry: 8.5.1.12.2 defines correspondence over RELATIVE BYTE POSITIONS, not over the declaration
      *> tree, so the two groups are compatible and the crossing must line the members up by position. Both
      *> dynamic-length items start at relative byte position 9 (3 prefix + one 3-byte element for the
      *> dynamic-capacity table - 8.5.1.12.3: a matched dynamic-capacity table "is considered to be the length
      *> of a single element", and dynamic-length items "are considered to be of zero length"), and both
      *> tables occupy position 3 with 3-byte elements, so 8.5.1.12.3's matching rule holds.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   A1 - VT opens at FROM 1 and MOVE ... TO VT-N(2) grows it to 2 (8.5.1.9.3). DISPLAY of a
      *>        variable-length group is the A.1 item 57 documented format (14.9.11.4 GR7): the members'
      *>        images in declaration order, each dynamic member at its CURRENT extent =
      *>        abc + 11p + 22q + wxyz + XY.
      *>   S1 - the crossing carries the fixed run "abc"+"XY" and the components ["11p22q", "wxyz"], so
      *>        LP1/LP2/LS take a/bc/XY by position and LT/LD take the components whole.
      *>   A2 - 14.2.3 GR8 makes the callee's stores visible to the caller: VP/VS come back from LP1+LP2/LS
      *>        (Zbc/QQ), VT at its GROWN capacity 3 and VD as the callee left it.
      *>   A3 - 14.2.3 GR9: BY CONTENT operates on a copy allocated by the ACTIVATING element, so S2's stores
      *>        reach nothing of the caller's and A3 = A2 exactly.
      *>   A4 - 14.8.3.2 / 14.2.3 GR7: the returning item's value transfers to the activating element's
      *>        RETURNING identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204VLB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 VG.
          05 VP PIC X(3).
          05 VT OCCURS DYNAMIC CAPACITY IN VC FROM 1.
             10 VT-N PIC 9(2).
             10 VT-A PIC X.
          05 VD PIC X DYNAMIC LENGTH.
          05 VS PIC X(2).
       01 RG.
          05 RP PIC X(3).
          05 RDYN PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO VP
           MOVE 11 TO VT-N(1)
           MOVE "p" TO VT-A(1)
           MOVE 22 TO VT-N(2)
           MOVE "q" TO VT-A(2)
           MOVE "wxyz" TO VD
           MOVE "XY" TO VS
           MOVE VC TO WS-C
           DISPLAY "A1=" WS-C " [" VG "]"
           CALL "PB204S1" AS NESTED USING VG
           MOVE VC TO WS-C
           DISPLAY "A2=" WS-C " [" VG "]"
           CALL "PB204S2" AS NESTED USING BY CONTENT VG
           MOVE VC TO WS-C
           DISPLAY "A3=" WS-C " [" VG "]"
           CALL "PB204S3" AS NESTED RETURNING RG
           DISPLAY "A4=[" RP "][" RDYN "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LVG.
          05 LP1 PIC X.
          05 LP2 PIC X(2).
          05 LT OCCURS DYNAMIC CAPACITY IN LC FROM 1.
             10 LT-N PIC 9(2).
             10 LT-A PIC X.
          05 LD PIC X DYNAMIC LENGTH.
          05 LS PIC X(2).
       PROCEDURE DIVISION USING LVG.
       S1.
           DISPLAY "S1=[" LP1 "][" LP2 "][" LT-N(1) LT-A(1) "][" LT-N(2) LT-A(2)
               "][" LD "][" LS "]"
           MOVE "Z" TO LP1
           MOVE 33 TO LT-N(3)
           MOVE "r" TO LT-A(3)
           MOVE "uv" TO LD
           MOVE "QQ" TO LS
           GOBACK.
       END PROGRAM PB204S1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 CVG.
          05 CP PIC X(3).
          05 CT OCCURS DYNAMIC CAPACITY IN CC FROM 1.
             10 CT-N PIC 9(2).
             10 CT-A PIC X.
          05 CD PIC X DYNAMIC LENGTH.
          05 CS PIC X(2).
       PROCEDURE DIVISION USING CVG.
       S2.
           DISPLAY "S2=[" CP "][" CD "][" CS "]"
           MOVE "###" TO CP
           MOVE "!!" TO CS
           MOVE "gone" TO CD
           GOBACK.
       END PROGRAM PB204S2.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB204S3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 SR.
          05 SP PIC X(3).
          05 SDYN PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION RETURNING SR.
       S3.
           MOVE "RET" TO SP
           MOVE "delivered" TO SDYN
           GOBACK.
       END PROGRAM PB204S3.
       END PROGRAM PB204VLB.
