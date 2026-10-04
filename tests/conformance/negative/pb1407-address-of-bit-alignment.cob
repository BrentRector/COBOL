      *> reject-at: 2002 2014 2023
      *> kb/Work PB1407. ISO 8.4.3.11.3 SR4: "If identifier-1 is a bit data item, identifier-1 shall be described such
      *> that: a) subscripting and reference modification in identifier-1 consist of only fixed-point numeric literals or
      *> arithmetic expressions in which all operands are fixed-point numeric literals and the exponentiation operator is
      *> not specified; and b) it is aligned on a byte boundary." Each ADDRESS OF below breaks one half and is refused
      *> COBOLNET2786: B2 starts at bit 3 of its record (b); BX(I) subscripts with a data-name, BX(2 ** 1) with an
      *> exponentiation and BX(1)(N:2) positions the slice with a data-name (a). BX(2) and B1 are legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BR.
          05 B1 PIC 1(3) USAGE BIT.
          05 B2 PIC 1(5) USAGE BIT.
       01 BT.
          05 BX PIC 1(8) USAGE BIT OCCURS 3.
       01 I PIC 9 VALUE 1.
       01 N PIC 9 VALUE 1.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF B1
           SET P TO ADDRESS OF B2
           SET P TO ADDRESS OF BX(2)
           SET P TO ADDRESS OF BX(I)
           SET P TO ADDRESS OF BX(2 ** 1)
           SET P TO ADDRESS OF BX(1)(N:2)
           STOP RUN.
