      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1266 - ISO 13.18.38.3 SR7: "Index-name-1 may be
      *>   specified only in the following contexts" - a subscript,
      *>   PERFORM / SEARCH VARYING, SET and a relation condition. ADD's
      *>   receiver and a CALL USING operand are not among them, so both
      *>   below are refused - and the diagnostic says WHY (COBOLNET1637,
      *>   the category error DISPLAY / MOVE / COMPUTE already draw), where
      *>   it used to say COBOLNET1639 "'IX' is not defined" about a name
      *>   INDEXED BY declared.
      *> cite.py --check 13.18.38.3 "Index-name-1 may be specified only in
      *>   the following contexts" -> OK  13.18.38.3 7)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1266A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
           ADD 1 TO IX
           CALL "NOSUCH" USING IX
           STOP RUN.
