      *> kb/Work PB1227 - CONSTANT AS BYTE-LENGTH OF data-name-1 (ISO 13.10, 2002).
      *> 13.10.4 GR5: "the class and category of constant-name-1 is numeric. Constant-name-1 is an integer. The
      *> value of constant-name-1 is determined as specified in the BYTE-LENGTH intrinsic function with the
      *> exception that when data-name-1 is an occurs-depending group item, the maximum size of the data item is
      *> used." The byte widths are this compiler's documented ones (FUNCTION BYTE-LENGTH, COBOLNET_INTRINSICS_DESIGN):
      *> DISPLAY 1 byte per position, NATIONAL 2 bytes per position, a COMP-5 S9(9) item 4 bytes.
      *>   K1 = 4    W  PIC X(4)
      *>   K2 = 6    WN PIC N(3)
      *>   K3 = 4    WB PIC S9(9) COMP-5
      *>   K4 = 6    G = X(2) + N(2): 2 + 4
      *>   K5 = 13   T = TN PIC 9 + E PIC X(3) OCCURS 1 TO 4 DEPENDING ON TN: the MAXIMUM, 1 + 4 x 3 (GR5's exception)
      *>   K6 = 4    C OF R (3 2): C is PIC N(2); 13.10.3 SR3 literal subscripts never change the length
      *>   BUF is PIC X(K2), 6 positions: K2 is an integer, so it may set a PICTURE repetition (13.10.3 SR2)
      *> LOCAL-STORAGE (13.6.2: "[77-level-description-entry | constant-entry | record-description-entry |
      *> type-declaration-entry] ..."): a 77, a constant entry, a type declaration and a record of that type.
      *>   LK = 5    BYTE-LENGTH OF L77, PIC X(5)
      *>   LT = AB   LR is TYPE LTYPE, whose LT is PIC X(LK2), LK2 = LK - 3 = 2: MOVE "ABC" keeps "AB"
      *> The negative halves: negative/pb1227-constant-byte-length-any-length (SR10) and
      *> negative/pb1227-constant-length-any-length (SR10's data-name-2 half).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1227BYTELEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W  PIC X(4).
       01 WN PIC N(3).
       01 WB PIC S9(9) COMP-5.
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC N(2).
       01 T.
          05 TN PIC 9.
          05 E  PIC X(3) OCCURS 1 TO 4 DEPENDING ON TN.
       01 TT.
          05 R OCCURS 3.
             10 C PIC N(2) OCCURS 2.
       01 K1 CONSTANT AS BYTE-LENGTH OF W.
       01 K2 CONSTANT AS BYTE-LENGTH OF WN.
       01 K3 CONSTANT AS BYTE-LENGTH OF WB.
       01 K4 CONSTANT AS BYTE-LENGTH OF G.
       01 K5 CONSTANT AS BYTE-LENGTH OF T.
       01 K6 CONSTANT AS BYTE-LENGTH OF C OF R (3 2).
       01 BUF PIC X(K2).
       LOCAL-STORAGE SECTION.
       77 L77 PIC X(5).
       01 LK CONSTANT AS BYTE-LENGTH OF L77.
       01 LK2 CONSTANT AS LK - 3.
       01 LTYPE TYPEDEF.
          05 LT PIC X(LK2).
       01 LR TYPE LTYPE.
       PROCEDURE DIVISION.
           MOVE "ABC" TO LT OF LR
           DISPLAY "K1=" K1 " K2=" K2 " K3=" K3 " K4=" K4 " K5=" K5 " K6=" K6
           DISPLAY "BUF=" FUNCTION LENGTH(BUF) " LK=" LK " LT=" LT OF LR
           STOP RUN.
