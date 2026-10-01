      *> ISO 13.18.19.3 SR1: "The character-string specified in that PICTURE clause shall be one instance
      *> of the picture symbol 'N', or 'X'." 13.18.40.3 SR6: the parenthesized integer "indicates the number
      *> of consecutive occurrences of the symbol", so X, X(1), X(01) and N(001) are each ONE instance and every
      *> spelling is accepted (kb/Work PB1210: DYNAMIC LENGTH and ANY LENGTH now ask ONE predicate). Expected
      *> (derived, 13.18.19.4 1): the length varies with the value moved in, and 15.50.4 6): FUNCTION LENGTH of
      *> a dynamic-length elementary item is "the current length of argument-1 in bytes": the X(01) and X(001)
      *> items holding five characters give LEN=05, the national item N(001) holding three characters (two bytes
      *> each) gives LEN=06. A refusal fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1210DYN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D1 PIC X(01) DYNAMIC LENGTH.
       01 D2 PIC X(001) DYNAMIC LENGTH.
       01 D3 PIC N(001) DYNAMIC LENGTH.
       01 N PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "HELLO" TO D1.
           MOVE "WORLD" TO D2.
           MOVE N"ABC" TO D3.
           MOVE FUNCTION LENGTH(D1) TO N.
           DISPLAY "LEN=" N " VAL=" D1.
           MOVE FUNCTION LENGTH(D2) TO N.
           DISPLAY "LEN=" N " VAL=" D2.
           MOVE FUNCTION LENGTH(D3) TO N.
           DISPLAY "LEN=" N.
           STOP RUN.
