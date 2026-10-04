      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 11.10.3 SR5: "The INITIAL clause shall not be specified if any program that directly or
      *>   indirectly contains this program is a recursive program." S5I1507 is INITIAL and is contained in the
      *>   RECURSIVE S5O1507. COBOLNET2755. The positive twin is conformance/2002/pb1507_source_unit_shapes_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S5O1507 IS RECURSIVE.
       PROCEDURE DIVISION.
           DISPLAY "OUT"
           CALL "S5I1507"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S5I1507 IS INITIAL.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM S5I1507.
       END PROGRAM S5O1507.
