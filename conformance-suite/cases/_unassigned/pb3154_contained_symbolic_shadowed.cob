      *>--- conformance-case
      *> id: pb3154_contained_symbolic_shadowed
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3154 review R1-1 - a containing program's SYMBOLIC
      *>   CHARACTERS name is HIDDEN by a nearer declaration of the same
      *>   spelling, of any type.
      *> ISO 8.4.6.1: "a source element's reference to such a name, even
      *>   when it is a different type of user-defined word, is to the
      *>   item which that source element describes" -> OK 8.4.6.1.
      *> ISO 8.4.6.2.1 3) b) 2.: "The item in the containing source
      *>   element if the name is not declared in source element A and
      *>   is declared in the source element containing source element
      *>   A" -> OK 8.4.6.2.1 3) b) 2.
      *> NESTING: PB3154SO declares the symbolic-characters S1 (67),
      *>   S2 (68) and S3 (69). It contains PB3154SM, which declares a
      *>   local data-name S1, an index-name S2 and a GLOBAL group
      *>   holding S3; PB3154SM contains PB3154SI, which declares none.
      *> EXPECTED OUTPUT, DERIVED:
      *>   MID S1=M     PB3154SM's own data-name S1 (8.4.6.1).
      *>   MID S2=2     PB3154SM's own index-name S2, SET TO 2.
      *>   MID S3=G     PB3154SM's own global data-name S3.
      *>   INNER S1=B   PB3154SM's S1 is LOCAL, so PB3154SI sees only
      *>                the outer symbolic: ordinal 67 = X"42" = "B"
      *>                (12.3.7.4 GR11).
      *>   INNER S3=G   the nearer containing element PB3154SM declares
      *>                S3 (a global name), so its item is referenced,
      *>                not the outer symbolic one element farther.
      *>   OUTER S1S2S3=BCD  the outer program's own symbolics.
      *> Before the fix MID S1, MID S3 and INNER S3 showed the outer
      *>   symbolic ("B", "D", "D") in place of the nearer item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154SO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 S2 S3 ARE 67 68 69.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3154SM".
           STRING S1 S2 S3 DELIMITED BY SIZE INTO X.
           DISPLAY "OUTER S1S2S3=" X.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154SM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S1 PIC X VALUE "M".
       01 T.
          05 E PIC X OCCURS 3 INDEXED BY S2.
       01 H GLOBAL.
          05 S3 PIC X VALUE "G".
       01 N PIC 9.
       PROCEDURE DIVISION.
       MID-PARA.
           DISPLAY "MID S1=" S1.
           SET S2 TO 2.
           SET N TO S2.
           DISPLAY "MID S2=" N.
           DISPLAY "MID S3=" S3.
           CALL "PB3154SI".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC X.
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE S1 TO Y.
           DISPLAY "INNER S1=" Y.
           MOVE S3 TO Y.
           DISPLAY "INNER S3=" Y.
       END PROGRAM PB3154SI.
       END PROGRAM PB3154SM.
       END PROGRAM PB3154SO.
