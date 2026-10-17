*>--- conformance-case
*> id: pb2498_length_odo_bit_national_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2498 + PB2964 - FUNCTION LENGTH and BYTE-LENGTH of a group
      *> whose OCCURS DEPENDING table sits among USAGE BIT items, and of a bit
      *> group and a national group that hold such a table.
      *>
      *> 15.50.4 rule 4b / 15.14.4 rule 2b: when a subordinate entry has the
      *> DEPENDING phrase, the length is determined by the rules of the
      *> OCCURS clause for a SENDING data item (13.18.38.4 GR8: the current
      *> occurrences). 8.5.1.6.3 packs bit items of one level into shared
      *> bytes and rounds a partly used byte up (15.14.4 rule 4 / 15.50.4
      *> rule 9), so the extent is counted in BIT positions and then rounded.
      *>
      *> G1: N byte (8 bits), X bit, then TB bits sharing X's byte.
      *>     N1 = 5: 8 + 1 + 5 = 14 bits = 2 bytes
      *>     N1 = 8: 8 + 1 + 8 = 17 bits = 3 bytes
      *>     N1 = 1: 8 + 1 + 1 = 10 bits = 2 bytes
      *> G2 is a BIT GROUP: 15.50.4 rule 1 gives its length in BOOLEAN
      *> POSITIONS, not in the bytes they occupy; BYTE-LENGTH (15.14.4 rule
      *> 4) rounds up.
      *>     N2 = 5: 1 + 5 = 6 positions, 1 byte
      *>     N2 = 8: 1 + 8 = 9 positions, 2 bytes
      *> G3 is a NATIONAL GROUP: 15.50.4 rule 2 gives its length in NATIONAL
      *> CHARACTER POSITIONS; BYTE-LENGTH counts bytes, two per national
      *> position (docs/CONFORMANCE.md D-N1).
      *>     N3 = 5: 5 positions, 10 bytes
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2498ODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC 9.
       01 G1.
          05 N PIC 9.
          05 X PIC 1 USAGE BIT.
          05 TB PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N1.
       01 N2 PIC 9.
       01 G2 GROUP-USAGE BIT.
          05 BA PIC 1 USAGE BIT.
          05 TB2 PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N2.
       01 N3 PIC 9.
       01 G3 GROUP-USAGE NATIONAL.
          05 TN PIC N OCCURS 1 TO 8 DEPENDING ON N3.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 8 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=8 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 1 TO N1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 N=1 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE 5 TO N2
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 N=5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G2) TO R
           DISPLAY " " R
           MOVE 8 TO N2
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 N=8 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G2) TO R
           DISPLAY " " R
           MOVE 5 TO N3
           MOVE FUNCTION LENGTH(G3) TO R
           DISPLAY "G3 N=5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G3) TO R
           DISPLAY " " R
           STOP RUN.
