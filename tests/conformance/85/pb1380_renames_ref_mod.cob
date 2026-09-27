      *> kb/Work PB1380 - a REFERENCE-MODIFIED level-66 RENAMES entry is
      *> the SLICE, never the whole alias. ISO 8.4.3.3.4 GR5: "Reference
      *> modification creates a unique data item that is a subset of the
      *> data item referenced by identifier-1"; 8.4.3.3.3 SR1 admits "an
      *> alphanumeric group item", which 13.18.45.4 GR2 says a THROUGH
      *> alias is; 13.18.45.4 GR1 gives a no-THROUGH alias "all of the
      *> data attributes of data-name-2" and its storage. Before the fix
      *> the resolver built the alias's place and returned BEFORE the
      *> reference modifier was applied, so every leg below bound the
      *> WHOLE alias: A1 moved all six characters, B1 overwrote the span.
      *> G = "ABC" + 123 + ("PQ" "RS") = ABC123PQRS (10 positions).
      *> A1 RN(2:3)    positions 2-4 of ABC123         -> BC1
      *> A2 RN(4:)     4 to the end of ABC123 (GR5c)   -> 123
      *> A3 RALL(3:5)  positions 3-7 of ABC123PQRS     -> C123P
      *> A4 RD2(2:2)   no-THROUGH alias of G2 (GR1)    -> 23
      *> A5 RG(2:2)    no-THROUGH alias of group G3    -> QR
      *> A6 RN(1:3) = "ABC" (a condition operand)     -> EQ
      *> B1 "XY" INTO RN(2:2)      only positions 2-3 -> AXY123PQRS
      *> B2 "9" INTO RD2(1:1)      numeric alias      -> G2 = 923
      *> B3 "8-*=" INTO RALL(6:4)  crosses G2, G31, G32 -> AXY928-*=S
      *> B4 "Z" INTO RG(2:1)       group alias        -> G3 = -Z=S
      *> B5 SPACES INTO RALL(9:)   figurative fill, positions 9-10
      *>                                              -> AXY928-Z
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1380-RENAMES-RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G1 PIC X(3) VALUE "ABC".
          05 G2 PIC 9(3) VALUE 123.
          05 G3.
             10 G31 PIC X(2) VALUE "PQ".
             10 G32 PIC X(2) VALUE "RS".
       66 RN RENAMES G1 THRU G2.
       66 RD2 RENAMES G2.
       66 RG RENAMES G3.
       66 RALL RENAMES G1 THRU G3.
       01 T PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE RN (2:3) TO T.
           DISPLAY "A1=[" T "]".
           MOVE RN (4:) TO T.
           DISPLAY "A2=[" T "]".
           MOVE RALL (3:5) TO T.
           DISPLAY "A3=[" T "]".
           MOVE RD2 (2:2) TO T.
           DISPLAY "A4=[" T "]".
           MOVE RG (2:2) TO T.
           DISPLAY "A5=[" T "]".
           IF RN (1:3) = "ABC"
               DISPLAY "A6=EQ"
           ELSE
               DISPLAY "A6=NE".
           MOVE "XY" TO RN (2:2).
           DISPLAY "B1=[" G "]".
           MOVE "9" TO RD2 (1:1).
           DISPLAY "B2=[" G2 "]".
           MOVE "8-*=" TO RALL (6:4).
           DISPLAY "B3=[" G "]".
           MOVE "Z" TO RG (2:1).
           DISPLAY "B4=[" G3 "]".
           MOVE SPACES TO RALL (9:).
           DISPLAY "B5=[" G "]".
           STOP RUN.
