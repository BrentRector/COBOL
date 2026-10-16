      *> kb/Work PB1099 review R1-3 - START ... WITH LENGTH on a NATIONAL
      *> record-key-name-1 (the SOURCE phrase's key) counts NATIONAL
      *> CHARACTER POSITIONS, and the positions run across the key's
      *> data-name-2 segments in the order specified.
      *>   cite.py --check 14.9.41.4 "if data-name-1 or record-key-name-1
      *>     is of class national, arithmetic-expression-1 is the number
      *>     of national character positions" -> OK 14.9.41.4 13)
      *>   cite.py --check 12.4.5.12.3 "Record-key-name-1 has the class
      *>     and category of data-name-2" -> OK 12.4.5.12.3 5)
      *>   cite.py --check 12.4.5.12.4 "Record-key-name-1 defines a record
      *>     key consisting of the concatenation of all occurrences of
      *>     data-name-2 in the order specified" -> OK 12.4.5.12.4 2)
      *>   cite.py --check 14.9.41.4 "or else the length of
      *>     record-key-name-1, if specified" -> OK 14.9.41.4 17) b)
      *> IX-RK is IX-B then IX-A, both PIC N(2), so it is a national key
      *> of four positions (eight bytes). The three records' keys are
      *>   REC1 N"BB01"   REC2 N"AB02"   REC3 N"BA03"
      *> and in ascending key order: AB02 (REC2), BA03 (REC3), BB01 (REC1).
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES:
      *>   W1 00 / W2 00 / W3 00   the three keys are distinct.
      *>   L1 00 / L1 00 REC3   IX-B = N"B " (MOVE pads with the national
      *>        space), START KEY IS = IX-RK WITH LENGTH 1: the first key
      *>        whose first POSITION is B is BA03. (A byte count would
      *>        compare one byte, 00, which every key starts with: REC2.)
      *>   L2 00 / L2 00 REC3   IX-B = N"A ", START KEY IS > IX-RK WITH
      *>        LENGTH 1: the first key whose first position exceeds A is
      *>        BA03. (By bytes: nothing exceeds 00 -> '23'.)
      *>   L3 00 / L3 00 REC1   IX-B = N"BB", IX-A = N"0Z", START KEY IS
      *>        = IX-RK WITH LENGTH 3: three positions BB0 run past IX-B
      *>        into IX-A (GR17 a) moves the relevant parts of the record
      *>        area); only BB01 begins BB0. (By bytes: 00 42 00 begins
      *>        BA03 -> REC3.)
      *>   L4 23   IX-B = N"AC", LENGTH 2: no key begins AC, invalid key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1099RN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb1099rn.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-RK SOURCE IS IX-B IX-A
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-A    PIC N(2).
          05 IX-DATA PIC X(4).
          05 IX-B    PIC N(2).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IXF
           MOVE N"01" TO IX-A
           MOVE "REC1" TO IX-DATA
           MOVE N"BB" TO IX-B
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W1 " FS
           MOVE N"02" TO IX-A
           MOVE "REC2" TO IX-DATA
           MOVE N"AB" TO IX-B
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W2 " FS
           MOVE N"03" TO IX-A
           MOVE "REC3" TO IX-DATA
           MOVE N"BA" TO IX-B
           WRITE IX-REC INVALID KEY CONTINUE END-WRITE
           DISPLAY "W3 " FS
           CLOSE IXF

           OPEN INPUT IXF
           MOVE N"B" TO IX-B
           START IXF KEY IS = IX-RK WITH LENGTH 1
               INVALID KEY CONTINUE END-START
           DISPLAY "L1 " FS
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L1 " FS " " IX-DATA

           MOVE N"A" TO IX-B
           START IXF KEY IS > IX-RK WITH LENGTH 1
               INVALID KEY CONTINUE END-START
           DISPLAY "L2 " FS
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L2 " FS " " IX-DATA

           MOVE N"BB" TO IX-B
           MOVE N"0Z" TO IX-A
           START IXF KEY IS = IX-RK WITH LENGTH 3
               INVALID KEY CONTINUE END-START
           DISPLAY "L3 " FS
           READ IXF NEXT AT END CONTINUE END-READ
           DISPLAY "L3 " FS " " IX-DATA

           MOVE N"AC" TO IX-B
           START IXF KEY IS = IX-RK WITH LENGTH 2
               INVALID KEY CONTINUE END-START
           DISPLAY "L4 " FS
           CLOSE IXF
           STOP RUN.
