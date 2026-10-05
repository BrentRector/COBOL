*> reject-at: 85 2002 2014 2023
      *> kb/Work R29 - 13.18.38.3 r7's closed context list admits an index-name only in a subscript,
      *> PERFORM/SEARCH VARYING, SET, or a relation condition, and 8.8.1.1 names no index-names among
      *> arithmetic operands. COMPUTE N = IX + 1 computed the occurrence number silently (the classic
      *> vendor extension - GnuCOBOL accepts it); the disposition is the DA6/PB1 shape: strict rejects
      *> with the r7 citation, --permissive accepts as the documented occurrence-number coercion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R29NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG.
          05 T PIC 9 OCCURS 5 TIMES INDEXED BY IX.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           SET IX TO 2.
           COMPUTE N = IX + 1.
           DISPLAY N.
           STOP RUN.
