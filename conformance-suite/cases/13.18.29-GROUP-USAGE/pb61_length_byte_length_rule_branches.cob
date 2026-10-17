*>--- conformance-case
*> id: pb61_length_byte_length_rule_branches
*> rules: [GR-13.18.29.4-3, AR-15.3-5, AR-15.3-11, AR-15.3-13, AR-15.3-14, AR-15.14.3-1, RV-15.14.4-1, RV-15.14.4-2, RV-15.14.4-4, RV-15.14.4-5, RV-15.14.4-6, RV-15.14.4-7, AR-15.50.3-1, RV-15.50.4-1, RV-15.50.4-2, RV-15.50.4-3, RV-15.50.4-4, RV-15.50.4-6, RV-15.50.4-7, RV-15.50.4-8, FMT-15.14.2, DOC-A.1-214, DOC-A.1-217]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB61 - FUNCTION LENGTH (15.50.4) and FUNCTION BYTE-LENGTH (15.14.4), one probe per RULE BRANCH, the
      *> expected values derived from the standard (never from an oracle). COBOL.NET's pinned widths
      *> (COBOLNET_INTRINSICS_DESIGN, BYTE-LENGTH): DISPLAY 1 byte/position, NATIONAL 2 (D-N1), COMP 9(4) 2,
      *> COMP-1 4, COMP-2 8, INDEX/POINTER 8, USAGE BIT ceil(n/8). An alphanumeric group's LENGTH is r3's
      *> "alphanumeric character positions", which under the 1-byte-per-position model equals its bytes.
      *> CARR:  15.50.4 r3 - a COMP-1/COMP-2/INDEX/POINTER item is "other than category boolean or usage
      *>        national", so LENGTH counts alphanumeric positions = the pinned byte width: 4 8 8 8, and
      *>        BYTE-LENGTH (15.14.4 r1) the same bytes. Before PB61 every carrier folded to 1.
      *> GG/GH: an alphanumeric group holding a national or carrier child - X(3)+N(2) = 3+4 = 7; X(3)+COMP 9(4)+
      *>        INDEX = 3+2+8 = 13. Before PB61 the national child counted one position per character.
      *>        (GH's carrier child was USAGE POINTER until kb/Work PB183 landed the 13.18.60.3 SR14 screen -
      *>        that rule confines the five pointer/object usage phrases to level 1 or a STRONG type
      *>        declaration, so `05 HP USAGE POINTER.` inside an ordinary group was NONCONFORMING SOURCE.
      *>        SR14's list OMITS INDEX where the neighbouring SR4 includes it, and INDEX carries the same
      *>        pinned 8-byte width, so the repair preserves the branch under test, the arithmetic and the
      *>        expected output byte for byte. Repairing nonconforming source - never bending valid COBOL.)
      *> ZL:    15.50.4 r2/r3 and 15.14.4 r1 over a ZERO-LENGTH literal (8.5.4 item 8 - "a literal whose ...
      *>        length at runtime is zero"): 0 0 0 0. Before PB61 a Math.Max(1, ...) clamp answered 1.
      *> RM:    15.14.3 r1 admits "a data item of any class or category" and 8.4.3.3.4 GR6 makes a ref-mod a
      *>        data item: BYTE-LENGTH(X5(2:3)) = 3, of a national slice N4(2:2) = 4 bytes, and X5(3:) = 3.
      *> ODO:   15.50.4 r4b / 15.14.4 r2b - an OCCURS DEPENDING group's length is "the rules of the OCCURS clause
      *>        for a sending data item" (13.18.38.4 GR8): 1 + 3x2 = 7, then 1 + 5x2 = 11 after MOVE 5; a
      *>        national ODO element is 4 bytes: 1 + 2x4 = 9. Before PB61 the ODO arm was a loud runtime stage.
      *> DL:    15.50.4 r6 / 15.14.4 r5 - a DYNAMIC LENGTH item's CURRENT length IN BYTES: "HELLO" -> 5,
      *>        N"ABCD" -> 8 (2 bytes per national position). Before PB61 the national case staged loud.
      *> VLG:   15.50.4 r7 / 15.14.4 r6 - a variable-length group (8.5.1.12.1) is r7a the fixed subordinates
      *>        + r7b each dynamic-length item's current length: 4 + 5 = 9; 4 + N"XYZ" (6 bytes) = 10.
      *> ODOD:  r4b AND r7b in ONE group - the ODO extent plus the dynamic leaf: 2 + 5 + 3x3 = 16 (before PB61
      *>        the ODO arm silently ignored the dynamic leaf: 11).
      *> DCG:   r7c / r6c - "the lengths of all subordinate dynamic-capacity tables based on their current
      *>        capacity": capacity 0 -> 2 + 0 = 2; SET CAP TO 4 -> 2 + 4x3 = 14. Without CAPACITY IN the table
      *>        grows through a receiving reference: DTN(3) -> capacity 3 -> 2 + 9 = 11. Before PB61 r7c staged loud.
      *> MIX:   all three r7 terms: 4 (fixed) + 8 (N"ABCD" dynamic) + 2 x (2 + 2) = 20.
      *> TYPE:  15.50.3 r1 / 15.14.3 r1 name "a type-name" outright: a group type of X(9) -> 9 9; a group type of
      *>        N(3) is an alphanumeric GROUP (13.18.29.4 GR3, no GROUP-USAGE) so r3 -> 6 6; an elementary
      *>        national type -> r2's 3 national positions and 6 bytes; a boolean USAGE BIT type -> r1's 5 boolean
      *>        positions and ceil(5/8) = 1 byte; X(7) -> 7 7. r4a: a type declaration with an ODO subordinate
      *>        takes "the rules of the OCCURS clause for a receiving data item" (GR8b - the maximum): 2 + 5x3 = 17.
      *> BIT:   15.50.4 r1 - an elementary boolean item's length is in BOOLEAN positions: PIC 1(12) USAGE BIT
      *>        -> 12 (BYTE-LENGTH 2), a DISPLAY boolean PIC 1(4) -> 4 4, the boolean literal B"10110" -> 5.
      *> PHYS:  15.14.2's optional PHYSICAL keyword (FMT-15.14.2) is accepted on BYTE-LENGTH as on LENGTH; under
      *>        COBOL.NET's 15.50.4 r8 / 15.14.4 r7 determination (the group is physically located where it is
      *>        defined) it returns the value the same reference returns without it: 7 7 and 9 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61LENBRANCHES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X5   PIC X(5) VALUE "ABCDE".
       01 N4   PIC N(4) VALUE N"ABCD".
       01 F1   COMP-1.
       01 F2   COMP-2.
       01 IX   USAGE INDEX.
       01 PT   USAGE POINTER.
       01 ORF USAGE OBJECT REFERENCE.
       01 PPT USAGE PROGRAM-POINTER.
       01 GG.
          05 GA PIC X(3).
          05 GN PIC N(2).
       01 GH.
          05 HA PIC X(3).
          05 HC PIC 9(4) COMP.
          05 HP USAGE INDEX.
       01 GOD.
          05 GON PIC 9 VALUE 3.
          05 GOE PIC X(2) OCCURS 1 TO 5 DEPENDING ON GON.
       01 GON2.
          05 G2N PIC 9 VALUE 2.
          05 G2E PIC N(2) OCCURS 1 TO 5 DEPENDING ON G2N.
       01 NN   PIC 9(2) VALUE 3.
       01 ODOD.
          05 DF1 PIC X(2).
          05 DDL PIC X DYNAMIC LENGTH.
          05 DT  OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
       01 DLA  PIC X DYNAMIC LENGTH.
       01 DLN  PIC N DYNAMIC LENGTH.
       01 VLG.
          05 VF PIC X(4) VALUE "ABCD".
          05 VD PIC X DYNAMIC LENGTH.
       01 VLN.
          05 WF PIC X(4).
          05 WD PIC N DYNAMIC LENGTH.
       01 DCG.
          05 F2X PIC X(2).
          05 DTB OCCURS DYNAMIC CAPACITY IN CAP PIC X(3).
       01 DCN.
          05 F3 PIC X(2).
          05 DTN OCCURS DYNAMIC PIC X(3).
       01 MIX.
          05 M1  PIC X(4).
          05 MDL PIC N DYNAMIC LENGTH.
          05 MT  OCCURS DYNAMIC CAPACITY IN MCAP.
             10 MA PIC X(2).
             10 MB PIC N(1).
       01 MYTYPE TYPEDEF.
          05 MTA PIC X(9).
       01 TNAT TYPEDEF.
          05 TNA PIC N(3).
       01 TNATE TYPEDEF PIC N(3).
       01 TBOOL TYPEDEF PIC 1(5) USAGE BIT.
       01 TALPH TYPEDEF PIC X(7).
       01 TODO TYPEDEF.
          05 TF1 PIC X(2).
          05 TT OCCURS 1 TO 5 DEPENDING ON NN PIC X(3).
       01 BB   PIC 1(12) USAGE BIT.
       01 BD   PIC 1(4).
       01 L    PIC 9(3).
       01 LB   PIC 9(3).
       PROCEDURE DIVISION.
           DISPLAY "CARR=" FUNCTION LENGTH(F1) " " FUNCTION BYTE-LENGTH(F1)
               " " FUNCTION LENGTH(F2) " " FUNCTION BYTE-LENGTH(F2)
               " " FUNCTION LENGTH(IX) " " FUNCTION BYTE-LENGTH(IX)
               " " FUNCTION LENGTH(PT) " " FUNCTION BYTE-LENGTH(PT)
               " " FUNCTION LENGTH(ORF) " " FUNCTION BYTE-LENGTH(ORF)
               " " FUNCTION LENGTH(PPT) " " FUNCTION BYTE-LENGTH(PPT)
           DISPLAY "GG=" FUNCTION LENGTH(GG) " " FUNCTION BYTE-LENGTH(GG)
               " GH=" FUNCTION LENGTH(GH) " " FUNCTION BYTE-LENGTH(GH)
           DISPLAY "ZL=" FUNCTION LENGTH("") " " FUNCTION BYTE-LENGTH("")
               " " FUNCTION LENGTH(N"") " " FUNCTION BYTE-LENGTH(N"")
           DISPLAY "RM=" FUNCTION LENGTH(X5(2:3)) " " FUNCTION BYTE-LENGTH(X5(2:3))
               " " FUNCTION BYTE-LENGTH(N4(2:2)) " " FUNCTION BYTE-LENGTH(X5(3:))
           DISPLAY "ODO=" FUNCTION LENGTH(GOD) " " FUNCTION BYTE-LENGTH(GOD)
           MOVE 5 TO GON
           DISPLAY "ODO5=" FUNCTION LENGTH(GOD) " " FUNCTION BYTE-LENGTH(GOD)
           DISPLAY "ODON=" FUNCTION LENGTH(GON2) " " FUNCTION BYTE-LENGTH(GON2)
           MOVE "HELLO" TO DLA
           MOVE N"ABCD" TO DLN
           DISPLAY "DL=" FUNCTION LENGTH(DLA) " " FUNCTION BYTE-LENGTH(DLA)
               " " FUNCTION LENGTH(DLN) " " FUNCTION BYTE-LENGTH(DLN)
           MOVE "HELLO" TO VD
           MOVE N"XYZ" TO WD
           DISPLAY "VLG=" FUNCTION LENGTH(VLG) " " FUNCTION BYTE-LENGTH(VLG)
               " " FUNCTION LENGTH(VLN) " " FUNCTION BYTE-LENGTH(VLN)
           MOVE "HELLO" TO DDL
           COMPUTE L = FUNCTION LENGTH(ODOD)
           COMPUTE LB = FUNCTION BYTE-LENGTH(ODOD)
           DISPLAY "ODOD=" L " " LB
           COMPUTE L = FUNCTION LENGTH(DCG)
           COMPUTE LB = FUNCTION BYTE-LENGTH(DCG)
           DISPLAY "DCG0=" L " " LB
           SET CAP TO 4
           COMPUTE L = FUNCTION LENGTH(DCG)
           COMPUTE LB = FUNCTION BYTE-LENGTH(DCG)
           DISPLAY "DCG4=" L " " LB
           COMPUTE L = FUNCTION LENGTH(DCN)
           MOVE "abc" TO DTN(3)
           COMPUTE LB = FUNCTION LENGTH(DCN)
           DISPLAY "DCN=" L " " LB
           MOVE N"ABCD" TO MDL
           SET MCAP TO 2
           COMPUTE L = FUNCTION LENGTH(MIX)
           COMPUTE LB = FUNCTION BYTE-LENGTH(MIX)
           DISPLAY "MIX=" L " " LB
           DISPLAY "TYPE=" FUNCTION LENGTH(MYTYPE) " " FUNCTION BYTE-LENGTH(MYTYPE)
               " " FUNCTION LENGTH(TNAT) " " FUNCTION BYTE-LENGTH(TNAT)
               " " FUNCTION LENGTH(TNATE) " " FUNCTION BYTE-LENGTH(TNATE)
               " " FUNCTION LENGTH(TBOOL) " " FUNCTION BYTE-LENGTH(TBOOL)
               " " FUNCTION LENGTH(TALPH) " " FUNCTION BYTE-LENGTH(TALPH)
           DISPLAY "TODO=" FUNCTION LENGTH(TODO) " " FUNCTION BYTE-LENGTH(TODO)
           DISPLAY "BIT=" FUNCTION LENGTH(BB) " " FUNCTION BYTE-LENGTH(BB)
               " " FUNCTION LENGTH(BD) " " FUNCTION BYTE-LENGTH(BD)
               " " FUNCTION LENGTH(B"10110")
           DISPLAY "PHYS=" FUNCTION BYTE-LENGTH(GG PHYSICAL) " " FUNCTION LENGTH(GG PHYSICAL)
               " " FUNCTION BYTE-LENGTH(VLG PHYSICAL) " " FUNCTION LENGTH(VLG PHYSICAL)
           STOP RUN.
       END PROGRAM PB61LENBRANCHES.
