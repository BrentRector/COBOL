*>--- conformance-case
*> id: pb1938_through_alias_vlg_element_moves
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1938 - THE ELEMENT MOVES OF A VARIABLE-LENGTH GROUP MOVE REACH A TABLE OF A LEVEL-66 THROUGH ALIAS.
      *>
      *> 14.6.9.2: "Correspondingly numbered elements are moved according to the rules of the MOVE statement
      *> specified in 14.9.25, MOVE statement". 13.18.45.4 GR2: "When the THROUGH phrase is specified,
      *> data-name-1 defines an alphanumeric group item that includes all elementary items starting with
      *> data-name-2 ..." - so the alias is a group item (14.9.25.4 GR9 applies to it) that owns no
      *> subordinates: its tables are the RECORD's tables. 8.5.1.12.2: "Two tables correspond if at least one
      *> of them is a dynamic-capacity table and they occupy the same relative byte positions within their
      *> groups"; 8.5.1.12.3: "Two corresponding tables match when the byte length of their elements is equal".
      *> Before the fix a corresponding pair whose ELEMENTARY elements differ in description aborted the run
      *> with "a table reached through a storage form whose element places this implementation does not build".
      *>
      *> EXPECTED VALUES, DERIVED (14.6.8.2 4): "the data is aligned by decimal point and is transferred to the
      *> receiving digits with zero fill or truncation on either end as required"):
      *>   L1 - sender alias over ST PIC 9(4) OCCURS 2 (1234, 5678), dynamic receiver D1 PIC 99V99: the
      *>        receiving table is recreated with the sender's 2 elements, each 99V99 of the 9(4) value, so
      *>        34.00 and 78.00, displayed 3400 7800 (as character images they would read 12.34 and 56.78).
      *>   L2 - the reverse: a dynamic sender B1 PIC 9(4) (1234, 5678) into the alias over TT PIC 99V99
      *>        OCCURS 2; the same two values, 3400 and 7800, T1 takes "XY", T3 takes "UV", T0 keeps "AB".
      *>   L3 - a table leaf under an OCCURS group inside the alias's range: RTT of RG(1) and of RG(2) are
      *>        two tables of the alias, each corresponding to its own dynamic table of OUTD (PIC 9V9, the
      *>        byte length of a 9(2) element), and each takes ITS record occurrence's values: 12 and 34
      *>        give 2.0 and 4.0, 56 and 78 give 6.0 and 8.0. The headers RHD, the alias's fixed parts,
      *>        move as characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1938-ALIAS-ELEMENTS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 SRC.
          05 S0 PIC X(2) VALUE "AB".
          05 S1 PIC X(2) VALUE "CD".
          05 ST PIC 9(4) OCCURS 2.
          05 S3 PIC X(2) VALUE "EF".
       66 SALIAS RENAMES S1 THROUGH S3.
       01 DST.
          05 D0 PIC X(2).
          05 D1 PIC 99V99 OCCURS DYNAMIC CAPACITY IN DCAP.
          05 D3 PIC X(2).
       01 SRC2.
          05 T0 PIC X(2) VALUE "AB".
          05 T1 PIC X(2) VALUE "CD".
          05 TT PIC 99V99 OCCURS 2 VALUE 1.
          05 T3 PIC X(2) VALUE "EF".
       66 TALIAS RENAMES T1 THROUGH T3.
       01 BACK.
          05 B0 PIC X(2).
          05 B1 PIC 9(4) OCCURS DYNAMIC CAPACITY IN BCAP.
          05 B3 PIC X(2).
       01 REC.
          05 R0 PIC X VALUE "a".
          05 RG OCCURS 2.
             10 RHD PIC X VALUE "h".
             10 RTT PIC 9(2) OCCURS 2.
          05 R9 PIC X VALUE "z".
       66 RAL RENAMES R0 THROUGH R9.
       01 OUTD.
          05 O0 PIC X.
          05 OH1 PIC X.
          05 OT1 PIC 9V9 OCCURS DYNAMIC CAPACITY IN OC1.
          05 OH2 PIC X.
          05 OT2 PIC 9V9 OCCURS DYNAMIC CAPACITY IN OC2.
          05 O9 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1234 TO ST(1)
           MOVE 5678 TO ST(2)
           MOVE SALIAS TO DST
           MOVE DCAP TO WS-C
           DISPLAY "L1=" WS-C " [" D0 "][" D1(1) D1(2) "][" D3 "]"
           MOVE "XY" TO B0
           MOVE 1234 TO B1(1)
           MOVE 5678 TO B1(2)
           MOVE "UV" TO B3
           MOVE BACK TO TALIAS
           DISPLAY "L2=[" SRC2 "]"
           MOVE 12 TO RTT(1, 1)
           MOVE 34 TO RTT(1, 2)
           MOVE 56 TO RTT(2, 1)
           MOVE 78 TO RTT(2, 2)
           MOVE RAL TO OUTD
           MOVE OC1 TO WS-C
           DISPLAY "L3=" WS-C " [" O0 "][" OH1 "][" OT1(1) OT1(2) "]["
               OH2 "][" OT2(1) OT2(2) "][" O9 "]"
           STOP RUN.
