      *> PB1264 - ISO 13.18.38.3 SR29: integer-4 and integer-5 may not
      *>   exceed the implementor's maximum (1,073,741,823 here, docs/
      *>   CONFORMANCE.md DOC-A.1-60). The maximum ITSELF is legal: TO
      *>   1073741823 states the highest capacity the table may ever have
      *>   and allocates nothing (the capacity is FROM 2).
      *> cite.py --check 13.18.38.3 "The implementor shall specify a
      *>   maximum permissible value for integer-4 and integer-5" -> OK
      *>   13.18.38.3 29)
      *> Derivation: the program compiles and its capacity register C is 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1264C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 T PIC X OCCURS DYNAMIC CAPACITY IN C FROM 2 TO 1073741823.
       PROCEDURE DIVISION.
           DISPLAY "C=" C
           STOP RUN.
