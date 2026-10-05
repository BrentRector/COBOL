*> reject-at: 85 2002 2014 2023
      *> kb/Work PB2018 - EVALUATE's selection objects are compared "in accordance with 8.8.4.2" (14.9.13.3 SR7 a),
      *> so an index-name inside an arithmetic-expression selection object is refused exactly as it is in a relation
      *> (13.18.38.3 SR7; 8.8.4.2.13; 8.8.1.1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2018EVA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 5 INDEXED BY K.
       01 N PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
           SET K TO 2
           EVALUATE N WHEN K + 1 DISPLAY "UNDER-REJECT" END-EVALUATE
           STOP RUN.
