      *> kb/Work PB1941 - a constant's LENGTH OF / BYTE-LENGTH OF operand may be SUBORDINATE to the very entry
      *> whose OCCURS clause demands the constant (ISO 13.10, 2002). 13.10.3 SR4 ("The length of data-name-1 or
      *> data-name-2 shall not be dependent, directly or indirectly, upon the value of constant-name-1") forbids
      *> only a circular dependence, and an element's length does not depend on its table's occurrence count.
      *> The operand is a table element, so it is subscripted (8.4.2.3.3 SR5 - a constant entry is not among the
      *> exemptions) by literals (13.10.3 SR3). Expected values (13.10.4 GR6 "determined as specified in the
      *> LENGTH intrinsic function", GR5 the BYTE-LENGTH one):
      *>   K3 = 4   LENGTH OF W3 (1), W3 PIC X(4) - so A3 OCCURS 4 TIMES
      *>   KE = 3   LENGTH OF E OF G OF T2 (1 1), qualified through T2, the entry demanding KE
      *>   KN = 8   BYTE-LENGTH OF WN (1), WN PIC N(4) under a group USAGE NATIONAL (4 x 2 bytes)
      *>   KO = 5   LENGTH OF OE (1), OE PIC X(5) - integer-2 of an OCCURS DEPENDING ON table
      *>   T1 (1) is KE x E = 3 x 3 = 9 characters; W3 (4) and E (2 3) are the last occurrences
      *> The negative half: negative/pb1941-constant-length-cycle-open-entry (SR4's circular shape).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941SUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A3 OCCURS K3.
             10 W3 PIC X(4).
          05 T1 OCCURS 2.
             10 T2 OCCURS KE.
                15 G.
                   20 E PIC X(3).
          05 AN OCCURS KN USAGE NATIONAL.
             10 WN PIC N(4).
          05 N PIC 9 VALUE 2.
          05 OD OCCURS 1 TO KO DEPENDING ON N.
             10 OE PIC X(5).
       01 K3 CONSTANT AS LENGTH OF W3 (1).
       01 KE CONSTANT AS LENGTH OF E OF G OF T2 (1 1).
       01 KN CONSTANT AS BYTE-LENGTH OF WN (1).
       01 KO CONSTANT AS LENGTH OF OE (1).
       PROCEDURE DIVISION.
           MOVE "WXYZ" TO W3 (4)
           MOVE "ABC" TO E (2 3)
           DISPLAY "K3=" K3 " KE=" KE " KN=" KN " KO=" KO
               " T1=" FUNCTION LENGTH(T1 (1))
               " W3=" W3 (4) " E=" E (2 3)
           STOP RUN.
