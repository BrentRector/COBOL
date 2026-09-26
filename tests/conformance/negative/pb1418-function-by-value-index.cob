      *> reject-at: 2002 2014 2023
      *> kb/Work PB1418 - ISO 8.4.3.2.3 SR10: "If function-prototype-name-1
      *> or function-pointer-name-1 is specified and the formal parameter
      *> corresponding to argument-1 is specified with a BY VALUE phrase,
      *> argument-1 shall be of class numeric, object, or pointer."
      *> 13.18.60.4 GR10: "The class and category of an index data item
      *> are index." WI is USAGE INDEX -> COBOLNET1554. (Before the fix
      *> the screen tested the item's storage category, numeric, and the
      *> function ran.)
      *> cite.py --check 8.4.3.2.3 "argument-1 shall be of class numeric,
      *>   object, or pointer" -> OK  8.4.3.2.3 10)  (Syntax rules)
      *> cite.py --check 13.18.60.4 "The class and category of an index
      *>   data item are index" -> OK  13.18.60.4 10)  (General rules)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. N1418IF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC S9(9) BINARY.
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-X RETURNING L-R.
           MOVE L-X TO L-R
           GOBACK.
       END FUNCTION N1418IF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1418IM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION N1418IF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WI USAGE INDEX.
       01 WR PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE WR = FUNCTION N1418IF(WI)
           DISPLAY "I=" WR
           STOP RUN.
       END PROGRAM N1418IM.
