      *> kb/Work PB1281 - ISO 13.18.44.2 prints REDEFINES data-name-2; 13.18.44.3 SR6 "Data-name-2 shall not be
      *> qualified." and SR5 "data-name-2 may be subordinate to an item whose data description entry contains an
      *> OCCURS clause. In this case, the reference to data-name-2 in the REDEFINES clause shall not be subscripted."
      *> cite.py: OK 13.18.44.3 6) and OK 13.18.44.3 5). NOTE 1: "If data-name-2 is not unique, no ambiguity of
      *> reference exists because of the required placement of the REDEFINES clause."
      *> Expected (from the rules, not measured):
      *>  - B redefines A within EACH occurrence of E (SR5 sentence 2, written unsubscripted): MOVE "ab12" TO A(2)
      *>    makes B1(2) = "ab" and B2(2) = 12; ADD 1 TO B2(2) gives 13 and A(2) reads "ab13".
      *>  - X is not unique (a data item of G and of H); Z REDEFINES X stands in H, so by NOTE 1's placement it
      *>    overlays H's X: Z = "CD", never G's "AB".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1281BARE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 TIMES.
             10 A PIC X(4).
             10 B REDEFINES A.
                15 B1 PIC X(2).
                15 B2 PIC 99.
       01 G.
          05 X PIC X(2) VALUE "AB".
       01 H.
          05 X PIC X(2) VALUE "CD".
          05 Z REDEFINES X PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ab12" TO A (2).
           ADD 1 TO B2 (2).
           DISPLAY B1 (2) " " B2 (2) " " A (2).
           DISPLAY Z.
           STOP RUN.
