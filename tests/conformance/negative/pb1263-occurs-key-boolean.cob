      *> reject-at: 2002 2014 2023
      *> kb/Work PB1263 - ISO 13.18.38.3 SR8: "The KEY phrase shall not be specified for a data item
      *> of class boolean, message-tag, object, or pointer." K is PIC 1, class boolean (the boolean
      *> PICTURE symbol 1 arrived in COBOL 2002, so 85 has no such item to refuse).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T OCCURS 3 ASCENDING KEY K INDEXED BY IX.
               10 K PIC 1.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
