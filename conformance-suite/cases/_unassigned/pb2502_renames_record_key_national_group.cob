*>--- conformance-case
*> id: pb2502_renames_record_key_national_group
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2502 review round 1 (R1-1) - a level-66 RENAMES item WITHOUT THROUGH that renames a
      *> GROUP-USAGE NATIONAL group takes ALL of data-name-2's attributes (13.18.45.4 GR1), so it is category
      *> national and a legal RECORD KEY exactly as the national group itself is (12.4.5.12.3 SR2). The compiler
      *> read the alias's own (empty) group usage and refused it as a national group item.
      *>   cite.py --check 13.18.45.4 "all of the data attributes of data-name-2 become the data attributes of data-name-1"
      *>     -> OK 13.18.45.4 1)
      *>   cite.py --check 12.4.5.12.3 "category alphanumeric or category national within a record description entry"
      *>     -> OK 12.4.5.12.3 2)
      *>   cite.py --check 13.18.29.4 "a national group is treated as though it were an elementary data item of usage national"
      *>     -> OK 13.18.29.4 2) b)
      *>
      *> EXPECTED VALUES, DERIVED: record 1 is P="aa", G=N"BBcc"; record 2 is P="zz", G=N"AAdd"; record 3 repeats
      *> record 1's prime key N"BBcc", which 12.4.5.12.4 GR1 forbids, so its WRITE is the duplicate-key failure,
      *> I-O status 22 (9.1.13.5 2) b)). READ KEY IS KA with N"AAdd" retrieves record 2 (P=zz); START KA = N"AAdd"
      *> then READ NEXT delivers zz, then aa (ascending key order, 9.1.8.2), then the at end condition, status 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2502NATKEY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2502-national-key.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KA
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 P PIC X(2).
          05 G GROUP-USAGE NATIONAL.
             10 G1 PIC N(2).
             10 G2 PIC N(2).
       66 KA RENAMES G.
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "aa" TO P.
           MOVE N"BB" TO G1.
           MOVE N"cc" TO G2.
           WRITE R.
           DISPLAY "W1=" WS-FS.
           MOVE "zz" TO P.
           MOVE N"AA" TO G1.
           MOVE N"dd" TO G2.
           WRITE R.
           DISPLAY "W2=" WS-FS.
           MOVE "bb" TO P.
           MOVE N"BB" TO G1.
           MOVE N"cc" TO G2.
           WRITE R.
           DISPLAY "W3=" WS-FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE N"AA" TO G1.
           MOVE N"dd" TO G2.
           READ F KEY IS KA.
           DISPLAY "KA AAdd->" P.
           START F KEY = KA.
           READ F NEXT.
           DISPLAY "NEXT " P.
           READ F NEXT.
           DISPLAY "NEXT " P.
           READ F NEXT.
           DISPLAY "END " WS-FS.
           CLOSE F.
           STOP RUN.
