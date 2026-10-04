      *> PB96 - a level-66 RENAMES THRU alias is the record's STORAGE WINDOW from data-name-2's first character to
      *> data-name-3's last (13.18.45.4 GR1/GR2): a REDEFINES view inside the range adds no characters (13.18.44 -
      *> B overlays A), a FROM / THRU that is itself a redefinition maps to the area it overlays, and a boundary that
      *> falls inside a leaf (CX: C THRU H, where H redefines only the first character of G1) is a partial part -
      *> the alias reads and writes exactly those characters. Before PB96 the span listed every leaf between FROM and
      *> THRU, views included: AC was "abcdabef" (8), not "abcdef" (6).
      *> kb/Work PB1283: the former alias AB (A THRU B, B a SHORTER redefinition of A) is not legal source -
      *> 13.18.45.3 SR11: "The end of the storage area described by data-name-3 shall follow the end of the storage
      *> area described by data-name-2" (cite.py --check 13.18.45.3 -> OK 11)); see negative/pb1283-renames-thru-redefinition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB96RN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(4) VALUE "abcd".
          05 B REDEFINES A PIC X(2).
          05 C PIC X(2) VALUE "ef".
          05 G.
             10 G1 PIC X(2) VALUE "gh".
             10 G2 PIC X(2) VALUE "ij".
             10 H REDEFINES G1 PIC X.
       66 AC RENAMES A THRU C.
       66 BC RENAMES B THRU C.
       66 CG RENAMES C THRU G1.
       66 BG RENAMES B THRU G.
       66 CX RENAMES C THRU H.
       PROCEDURE DIVISION.
           DISPLAY "AC=[" AC "] " FUNCTION LENGTH(AC).
           DISPLAY "BC=[" BC "] " FUNCTION LENGTH(BC).
           DISPLAY "CG=[" CG "] " FUNCTION LENGTH(CG).
           DISPLAY "BG=[" BG "] " FUNCTION LENGTH(BG).
           DISPLAY "CX=[" CX "] " FUNCTION LENGTH(CX).
           MOVE "12345678" TO BG.
           DISPLAY "A=[" A "] C=[" C "] G1=[" G1 "] G2=[" G2 "]".
           MOVE "XYZ" TO CX.
           DISPLAY "C=[" C "] G1=[" G1 "] AC=[" AC "]".
           STOP RUN.
