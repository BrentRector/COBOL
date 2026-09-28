      *> kb/Work PB1213 — ONE screen for "a dynamic-length elementary item or a variable-length group", and ONE
      *> LENGTH fold for the CONSTANT entry and the LENGTH function.
      *> ISO/IEC 1989:2023 §8.5.1.12.1: "A variable-length group is a group item whose data description has at
      *> least one dynamic-length elementary item or dynamic-capacity table as a subordinate item." §13.10.3 SR12
      *> ("Data-name-1 and data-name-2 shall not be dynamic-length elementary items or variable-length groups")
      *> and §13.18.5.3 SR2 (a BASED subject) bar exactly that pair — and NOTHING ELSE. §13.10.4 GR6: the value
      *> "is determined as specified in the LENGTH intrinsic function with the exception that when data-name-2 is
      *> an occurs-depending group item, the maximum size of the data item is used". This golden pins:
      *>   K1 = LENGTH OF T (1): T is itself a dynamic-capacity table, not a variable-length group (nothing
      *>        variable is SUBORDINATE to it); one element is E1 X(3) + E2 9(2) = 5 character positions.
      *>        (Before the fix the private table walk refused this with an SR12 message.)
      *>   K2 = LENGTH OF E1 (1): an elementary item inside the dynamic table, fixed = 3.
      *>   K3 = LENGTH OF H: an occurs-depending group is NOT a variable-length group; GR6's exception gives
      *>        the maximum size = 4 + 2 * 5 = 14.
      *>   K4 = LENGTH OF GN: §15.50.4 r3 — an alphanumeric group counts alphanumeric character positions:
      *>        X(2) + N(2) at two positions per national character = 6 (the CONSTANT used to answer 4).
      *>   K5 = LENGTH OF BT: §15.50.4 r1 — an elementary boolean item counts BOOLEAN positions = 8 (not the one
      *>        byte a USAGE BIT PIC 1(8) occupies).
      *>   Each K is DISPLAYed beside FUNCTION LENGTH of the same item, which must agree (GR6).
      *>   BF, a BASED fixed-length group, allocates and holds its values (SR2 does not bite).
      *> Hand-derived stdout:
      *>   K=5 3 14 6 8
      *>   F=6 8
      *>   BF=ABC12
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1213VN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(2).
          05 T OCCURS DYNAMIC.
             10 E1 PIC X(3).
             10 E2 PIC 9(2).
       01 H.
          05 H1 PIC 9(4) VALUE 2.
          05 H2 PIC X(2) OCCURS 1 TO 5 DEPENDING ON H1.
       01 GN.
          05 GN1 PIC X(2).
          05 GN2 PIC N(2).
       01 BT PIC 1(8) USAGE BIT.
       01 K1 CONSTANT AS LENGTH OF T (1).
       01 K2 CONSTANT AS LENGTH OF E1 (1).
       01 K3 CONSTANT AS LENGTH OF H.
       01 K4 CONSTANT AS LENGTH OF GN.
       01 K5 CONSTANT AS LENGTH OF BT.
       01 BF BASED.
          05 BF1 PIC X(3).
          05 BF2 PIC 9(2).
       PROCEDURE DIVISION.
           DISPLAY "K=" K1 " " K2 " " K3 " " K4 " " K5.
           DISPLAY "F=" FUNCTION LENGTH(GN) " " FUNCTION LENGTH(BT).
           ALLOCATE BF.
           MOVE "ABC" TO BF1.
           MOVE 12 TO BF2.
           DISPLAY "BF=" BF.
           STOP RUN.
