      *> reject-at: 2023
      *> ISO 13.18.44.3 SR15: the required alignment of the subject of a
      *> REDEFINES entry shall be the same as the alignment of the data item it
      *> redefines.  B begins at bit 2 of the record (8.5.1.6.3: an elementary
      *> bit data item immediately following one of the same level takes the next
      *> bit position).  C is a bit group whose FIRST item is ALIGNED (13.18.1.4
      *> GR1: "aligned on the first bit of the first available byte boundary"),
      *> and "the alignment of the start of a group item and the alignment of the
      *> first item within that group, when the first item is a bit data item,
      *> are at the same bit position in storage" (8.5.1.6.3), so C requires a
      *> byte boundary too: the ONE refusal (kb/Work PB3167 - the screen asked
      *> only C's own clauses and accepted it).  D, a plain bit group over E at
      *> bit 14, needs no byte and is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3167N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GROUP-USAGE BIT.
          05 A PIC 1(2) USAGE BIT.
          05 B PIC 1(12) USAGE BIT.
          05 C REDEFINES B GROUP-USAGE BIT.
             10 CA PIC 1(3) USAGE BIT ALIGNED.
          05 E PIC 1(4) USAGE BIT.
          05 D REDEFINES E GROUP-USAGE BIT.
             10 DA PIC 1(3) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY G
           STOP RUN.
