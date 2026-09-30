      *> PB1661 - ISO 14.9.13.4 GR3 a): the selection subject is "assigned
      *>   the value and class of the data item"; 8.8.4.2.13 3) compares an
      *>   index data item with an index-name (or another index data item)
      *>   by occurrence number. A subject read by more than one WHEN is
      *>   evaluated ONCE into an intermediate, and for an index data item
      *>   that intermediate was stored by MOVE - not the SET a class-index
      *>   item needs - so the comparison read a value that never arrived
      *>   and the EVALUATE took WHEN OTHER.
      *> cite.py --check 14.9.13.4 "assigned the value and class of the data
      *>   item" -> OK  14.9.13.4 3) a)
      *> Derivation: IX1 = 3, IX2 = 2, I1 = 3, I2 = 2, IDA = 3 (SET IDA TO
      *>   I1). E1: subject I1 - WHEN I2 (2) no, WHEN IDA (3) yes: I1=IDA.
      *>   E2: subject IDA - WHEN I2 no, WHEN I1 yes: IDA=I1. E3: subject
      *>   index-name IX1 - WHEN IX2 no, WHEN IDA yes: IX1=IDA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1661.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I1 USAGE INDEX.
       01 I2 USAGE INDEX.
       01 IDA USAGE INDEX.
       01 T.
          05 TE PIC X OCCURS 5 INDEXED BY IX1 IX2.
       PROCEDURE DIVISION.
           SET IX1 TO 3
           SET IX2 TO 2
           SET I1 TO IX1
           SET I2 TO IX2
           SET IDA TO I1
           EVALUATE I1
               WHEN I2 DISPLAY "I1=I2"
               WHEN IDA DISPLAY "I1=IDA"
               WHEN OTHER DISPLAY "E1 OTHER"
           END-EVALUATE
           EVALUATE IDA
               WHEN I2 DISPLAY "IDA=I2"
               WHEN I1 DISPLAY "IDA=I1"
               WHEN OTHER DISPLAY "E2 OTHER"
           END-EVALUATE
           EVALUATE IX1
               WHEN IX2 DISPLAY "IX1=IX2"
               WHEN IDA DISPLAY "IX1=IDA"
               WHEN OTHER DISPLAY "E3 OTHER"
           END-EVALUATE
           STOP RUN.
