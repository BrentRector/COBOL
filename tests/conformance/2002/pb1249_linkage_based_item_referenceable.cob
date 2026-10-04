      *> ISO 13.7.3 SR4, lead-in: "A based data item may be referenced as described in
      *>   13.18.5, BASED clause; otherwise, a data item defined in the linkage section ...
      *>   may be referenced ... if, and only if, ..." (cite.py --check 13.7.3
      *>   "A based data item may be referenced as described in 13.18.5" -> OK 13.7.3 4))
      *> L-B is a linkage item that is NOT a USING operand, NOT subordinate to one and NOT
      *>   a redefinition: only its BASED clause (introduced in 2002) makes it referenceable.
      *> DERIVATION: SET ADDRESS OF L-B TO ADDRESS OF G (8.6.5 -- cite.py --check 8.6.5 "An association is established linking the based entry to actual data" -> OK; 13.18.5.4 GR1) associates the
      *>   based entry with G's storage, so DISPLAY L-B shows G's eight characters => ABCDEFGH
      *>   and a MOVE through L-B changes G => ZZZZZZZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G       PIC X(8) VALUE "ABCDEFGH" GLOBAL.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB1249D".
           DISPLAY G.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1249D.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-B     PIC X(8) BASED.
       PROCEDURE DIVISION.
       SUB-P.
           SET ADDRESS OF L-B TO ADDRESS OF G.
           DISPLAY L-B.
           MOVE "ZZZZZZZZ" TO L-B.
           GOBACK.
       END PROGRAM PB1249D.
       END PROGRAM PB1249C.
