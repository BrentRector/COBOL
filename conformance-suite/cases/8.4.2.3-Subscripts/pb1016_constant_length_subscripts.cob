*>--- conformance-case
*> id: pb1016_constant_length_subscripts
*> rules: [SR-8.4.2.3.3-2, SR-8.4.2.3.3-5, SR-13.10.3-3, GR-13.10.4-3, GR-13.10.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1016 - CONSTANT AS LENGTH OF data-name-2 WITH SUBSCRIPTS (ISO 13.10, 2002).
      *> 13.10.3 SR3: "All subscripts of data-name-1 and data-name-2 shall be literals." - so a subscript may
      *> be written on data-name-2, and 8.4.2.3.3 decides WHERE and HOW MANY: SR2 (only on an item that has, or
      *> is subordinate to, an OCCURS clause) and SR3 (one subscript per OCCURS clause). Every subscript below is
      *> legal - the count equals the OCCURS depth - including the one hung off a QUALIFICATION
      *> (CELL OF ROWX (3 2)), which the constant binder used to neither check nor count.
      *> 13.10.4 GR6: the value is that of the LENGTH function over data-name-2 - a subscript never changes it,
      *> since every occurrence shares one description:
      *>   K1 = 4    CELL is PIC X(4)
      *>   K2 = 8    ROWX holds CELL OCCURS 2: 2 x 4
      *>   K3 = 4    CELL again, qualified, subscripts written after the qualifier
      *>   K4 = 24   T holds ROWX OCCURS 3: 3 x 8 (an unsubscripted non-table group)
      *> The negative halves: negative/pb1016-constant-length-subscript-on-non-table (SR2) and
      *> negative/pb1016-constant-length-subscript-count (SR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1016CONSTLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 ROWX OCCURS 3.
             10 CELL PIC X(4) OCCURS 2.
       01 K1 CONSTANT AS LENGTH OF CELL (1, 2).
       01 K2 CONSTANT AS LENGTH OF ROWX (2).
       01 K3 CONSTANT AS LENGTH OF CELL OF ROWX (3 2).
       01 K4 CONSTANT AS LENGTH OF T.
       PROCEDURE DIVISION.
           DISPLAY "K1=" K1 " K2=" K2 " K3=" K3 " K4=" K4.
           STOP RUN.
