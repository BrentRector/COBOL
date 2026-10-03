      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1082 - ISO 12.3.6.3 SR1: "Alphabet-name-1 shall reference an alphabet that defines an alphanumeric
      *> collating sequence" (cite.py --check 12.3.6.3 "Alphabet-name-1 shall reference an alphabet that defines an
      *> alphanumeric collating sequence"). NOSUCH names no alphabet: no ALPHABET clause declares it. It used to stay
      *> inert at every edition, leaving the program on the native order with no diagnostic, while the
      *> alphabet-name-2 slot refused the same shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1082A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. ANY-COMPUTER
           PROGRAM COLLATING SEQUENCE IS NOSUCH.
       DATA DIVISION.
       PROCEDURE DIVISION.
           IF "A" < "B" DISPLAY "LT" END-IF
           STOP RUN.
