*>--- conformance-case
*> id: pb572_national_redefines_receiver
*> rules: [GR-13.18.60.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB572 - A NATIONAL LEAF REACHED THROUGH A REDEFINES VIEW IS A MOVE RECEIVER LIKE ANY OTHER.
      *>
      *> 13.18.60.4 GR2: "The USAGE clause specifies the manner in which a data item is represented in the
      *> storage of a computer. It does not affect the use of the data item". A PIC X(6) DISPLAY leaf under
      *> a redefined record is a MOVE receiver, so the PIC N(3) NATIONAL leaf in the same position is one
      *> too; 14.9.25.4 GR6 a) admits a national receiving operand with "alignment and any necessary space
      *> filling ... as defined in 14.6.8". The program was once refused with COBOLNET0899 ("a reference
      *> shape COBOL.NET does not yet implement as a receiver") on the redefined side of the class; the
      *> REDEFINES-over-national refusal was removed by PB231, and this golden pins all three positions.
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   R1 - A (redefined record's national leaf) receives N"ABC" whole: ABC.
      *>   R2 - A2 (a national leaf in the REDEFINING record) receives N"XYZ" whole: XYZ.
      *>   R3 - K2 (an elementary PIC N(2) REDEFINES K1 PIC X(4)) receives N"PQ": PQ.
      *>   R4 - MOVE A TO K2: national to national, 14.6.8.5 "aligned at the leftmost character position
      *>        in the data item with space fill or truncation to the right" - K2 holds 2 positions: AB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB572NRV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC N(3) USAGE NATIONAL.
       01 H REDEFINES G.
          05 B PIC X(6).
       01 G2.
          05 B2 PIC X(6).
       01 H2 REDEFINES G2.
          05 A2 PIC N(3) USAGE NATIONAL.
       01 K.
          05 K1 PIC X(4).
          05 K2 REDEFINES K1 PIC N(2) USAGE NATIONAL.
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"ABC" TO A
           DISPLAY "R1=[" A "]"
           MOVE N"XYZ" TO A2
           DISPLAY "R2=[" A2 "]"
           MOVE N"PQ" TO K2
           DISPLAY "R3=[" K2 "]"
           MOVE A TO K2
           DISPLAY "R4=[" K2 "]"
           STOP RUN.
