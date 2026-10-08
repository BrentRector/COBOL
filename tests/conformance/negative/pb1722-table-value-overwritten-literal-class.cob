      *> reject-at: 2002 2014 2023
      *> EVERY LITERAL A FORMAT 2 VALUE CLAUSE WRITES IS SCREENED, EVEN ONE A
      *> LATER PHRASE OVERWRITES (kb/Work PB1722). ISO/IEC 1989:2023
      *> 13.18.63.3 SR7: "If the item is of category numeric-edited and the
      *> literal is of class alphanumeric or national, the class of the
      *> literal shall conform to that of the data item". N"12" is national
      *> and NE is a usage-DISPLAY numeric-edited item, so the clause is
      *> non-conforming whatever GR15 later assigns to occurrence 1. The
      *> screen used to read the resolved element map, where the second
      *> phrase had replaced N"12", so the program compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1722NE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NE-G.
           05  NE PIC ZZ9 OCCURS 3 VALUE N"12" FROM (1) TO (1)
                                         "5" FROM (1) TO (1).
       PROCEDURE DIVISION.
           DISPLAY NE (1).
           STOP RUN.
