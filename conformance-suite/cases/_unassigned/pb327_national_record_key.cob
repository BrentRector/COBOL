      *>--- conformance-case
      *> id: pb327_national_record_key
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB327 - A NATIONAL RECORD KEY, and START's LENGTH in
      *> NATIONAL POSITIONS. 12.4.5.12.3 SR2 admits it: "Data-name-1
      *> and data-name-2 shall reference a data item of category
      *> alphanumeric or category NATIONAL within a record description
      *> entry associated with the file-name". 14.9.41.4 GR13 then
      *> fixes the LENGTH unit: "if data-name-1 or record-key-name-1
      *> is of class national, arithmetic-expression-1 is the number of
      *> national character positions" - so WITH LENGTH 2 is the first
      *> TWO NATIONAL POSITIONS, four bytes of the eight-byte key, and
      *> N"AB" positions on "ABBB", never on "AAAA" (which a two-BYTE
      *> reading would have matched first, its first two bytes being
      *> 00 41 exactly like the operand's). GR17e's comparison per
      *> 8.8.4.2.9 is satisfied by the byte compare: UTF-16BE pair
      *> order IS code-unit order over the repertoire 8.5.1.4 admits.
      *> GR14 - "If arithmetic-expression-1 does not evaluate to a
      *> positive nonzero integer that is less than or equal to the
      *> length of the associated key, the I-O status ... is set to
      *> '23', the invalid key condition exists" - is counted in the
      *> same unit: LENGTH 5 exceeds the key's FOUR national positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB327IX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb327ix.dat"
              ORGANIZATION IS INDEXED
              ACCESS MODE IS DYNAMIC
              RECORD KEY IS IX-PRIME.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-PRIME PIC N(4).
          05 IX-DATA  PIC X(6).
       WORKING-STORAGE SECTION.
       01 EOFF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF.
           MOVE N"AAAA" TO IX-PRIME.
           MOVE "ONE   " TO IX-DATA.
           WRITE IX-REC.
           MOVE N"ABBB" TO IX-PRIME.
           MOVE "TWO   " TO IX-DATA.
           WRITE IX-REC.
           MOVE N"ACCC" TO IX-PRIME.
           MOVE "THREE " TO IX-DATA.
           WRITE IX-REC.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE N"AB" TO IX-PRIME.
           START IXF KEY IS EQUAL TO IX-PRIME WITH LENGTH 2.
           READ IXF NEXT AT END MOVE "Y" TO EOFF.
           DISPLAY "S1=" IX-DATA.
           MOVE N"AC" TO IX-PRIME.
           START IXF KEY IS NOT LESS THAN IX-PRIME WITH LENGTH 2.
           READ IXF NEXT AT END MOVE "Y" TO EOFF.
           DISPLAY "S2=" IX-DATA.
           MOVE N"AB" TO IX-PRIME.
           START IXF KEY IS EQUAL TO IX-PRIME WITH LENGTH 5
               INVALID KEY DISPLAY "GR14=INVALID"
               NOT INVALID KEY DISPLAY "GR14=ACCEPTED".
           MOVE N"AAAA" TO IX-PRIME.
           READ IXF INVALID KEY DISPLAY "R1=INVALID".
           DISPLAY "R1=" IX-DATA.
           CLOSE IXF.
           STOP RUN.
