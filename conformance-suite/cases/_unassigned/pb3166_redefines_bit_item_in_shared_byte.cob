*>--- conformance-case
*> id: pb3166_redefines_bit_item_in_shared_byte
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3166 - REDEFINES of a USAGE BIT item (or GROUP-USAGE BIT
      *> group) that SHARES A BYTE with its same-level bit neighbours.
      *> Every value is derived from the standard, none read off a run.
      *>
      *> 13.18.44.1: the clause "allows the same computer storage area to be
      *> described by different data description entries".
      *> 13.18.44.4 GR1: "Storage association for the subject of the entry
      *> starts at the first bit of the data item referenced by data-name-2",
      *> so a redefinition of a bit item that begins in the middle of a byte
      *> begins at that bit, and the item adds no storage.
      *> 8.5.1.6.3: a bit item "immediately following" a bit item or bit group
      *> of the same level is "at the next bit position in storage", so the
      *> neighbours before AND after the redefined item share its byte, the
      *> redefining entry (which occupies nothing new) being transparent to
      *> the adjacency.
      *>
      *> A  01 GA: RA 1(2) at bits 0-1; RSUB (group, 6 bits) at bits 2-7 =
      *>    RSA bit 2, RTA bits 3-7; RSUB2 redefines RSUB: RS2A bits 2-3,
      *>    RS2B bits 4-7.  RA=11 RSA=1 RTA=10101 -> RSUB 110101 ->
      *>    RS2A = 11, RS2B = 0101.  One byte: LENGTH(GA) = 1.
      *> B  01 GB: RA 1(2) bits 0-1, RB 1(6) bits 2-7, RB2 redefines RB with
      *>    1(2), then RZ PIC X on the next byte.  RA=11, RB=101010: RB2 = 10
      *>    (RB's first two bits).  LENGTH(GB) = 2.
      *> C  01 GC: RC1 1(6) VALUE 101010, RC2 redefines RC1 with 1(2), RC3
      *>    1(2) VALUE 11.  RC3 follows RC1's six bits in the SAME byte (the
      *>    redefiner takes no storage): the byte is 10101011 = 171, and
      *>    FUNCTION ORD is one more, 172.  RC3 = 11.
      *> D  01 GD GROUP-USAGE BIT: DA 1(2), DB 1(3), DB2 redefines DB 1(2),
      *>    DC 1(3), all level 05, one run of 8 bits.  DA=11 DB=101 DC=010:
      *>    DB2 = 10 and the group's 8 boolean positions are 11 101 010.
      *> E  01 GE: EA 1(2) = 01, EB 1(3) = 101, EB2 redefines EB 1(2),
      *>    EK 1(3) = 110, EZ X = Z (by VALUE).  Group move GE to GF (the same
      *>    members without the redefinition) copies the two bytes.  MOVE 01
      *>    to EB2 replaces EB's first two bits and leaves its third: EB = 011.
      *> USAGE BIT and GROUP-USAGE BIT are COBOL-2002 introductions, so this is
      *> the introducing edition; the rule is edition-invariant above it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3166RB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GA.
          05 RA PIC 1(2) USAGE BIT.
          05 RSUB GROUP-USAGE BIT.
             10 RSA PIC 1 USAGE BIT.
             10 RTA PIC 1(5) USAGE BIT.
          05 RSUB2 REDEFINES RSUB GROUP-USAGE BIT.
             10 RS2A PIC 1(2) USAGE BIT.
             10 RS2B PIC 1(4) USAGE BIT.
       01 GB.
          05 RBA PIC 1(2) USAGE BIT.
          05 RB PIC 1(6) USAGE BIT.
          05 RB2 REDEFINES RB PIC 1(2) USAGE BIT.
          05 RZ PIC X.
       01 GC.
          05 RC1 PIC 1(6) USAGE BIT VALUE B"101010".
          05 RC2 REDEFINES RC1 PIC 1(2) USAGE BIT.
          05 RC3 PIC 1(2) USAGE BIT VALUE B"11".
       01 CX REDEFINES GC PIC X.
       01 GD GROUP-USAGE BIT.
          05 DA PIC 1(2).
          05 DB PIC 1(3).
          05 DB2 REDEFINES DB PIC 1(2).
          05 DC PIC 1(3).
       01 GE.
          05 EA PIC 1(2) USAGE BIT VALUE B"01".
          05 EB PIC 1(3) USAGE BIT VALUE B"101".
          05 EB2 REDEFINES EB PIC 1(2) USAGE BIT.
          05 EK PIC 1(3) USAGE BIT VALUE B"110".
          05 EZ PIC X VALUE "Z".
       01 GF.
          05 FA PIC 1(2) USAGE BIT.
          05 FB PIC 1(3) USAGE BIT.
          05 FC PIC 1(3) USAGE BIT.
          05 FZ PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"11" TO RA
           MOVE B"1" TO RSA
           MOVE B"10101" TO RTA
           DISPLAY "A " RS2A " " RS2B " " FUNCTION LENGTH(GA)
           MOVE B"11" TO RBA
           MOVE B"101010" TO RB
           DISPLAY "B " RB2 " " RB " " FUNCTION LENGTH(GB)
           DISPLAY "C " RC2 " " RC3 " " FUNCTION ORD(CX)
           MOVE B"11" TO DA
           MOVE B"101" TO DB
           MOVE B"010" TO DC
           DISPLAY "D " DB2 " " GD
           MOVE GE TO GF
           DISPLAY "E " EB2 " " FA " " FB " " FC " " FZ
           MOVE B"01" TO EB2
           DISPLAY "E " EB " " EB2 " " EA " " EK
           STOP RUN.
