      *> kb/Work PB1168 -- a REWRITE on a RECORD sequential file whose
      *> record-name-1 is not the size of the record being replaced.
      *>   python scripts/spec/cite.py --check 14.9.35.4 "For a record
      *>   sequential file, if the number of bytes in the data item
      *>   referenced by identifier-1, the runtime representation of
      *>   literal-1, or the record referenced by record-name-1 is not
      *>   equal to the number of bytes in the record being replaced,
      *>   the execution of the REWRITE statement is unsuccessful"
      *>                                          -> OK  §14.9.35.4 16)
      *>   python scripts/spec/cite.py --check 13.18.43.4 "Integer-1
      *>   specifies the number of bytes contained in each record in
      *>   the file."                             -> OK  §13.18.43.4 6)
      *>   python scripts/spec/cite.py --check 14.9.35.4 "If the
      *>   execution of the REWRITE statement is unsuccessful, no
      *>   logical record updating takes place"   -> OK  §14.9.35.4 14)
      *> F has no RECORD clause and no variable-length record, so its
      *> implicit clause is format 1 with integer-1 = 20, the largest
      *> record (§13.18.43.4 GR5 a)): every record in the file is 20
      *> bytes. REWRITE R1 sends a 10-byte record-name-1 over the
      *> 20-byte record 2 -- not equal -- so it is unsuccessful with
      *> '44' and record 2 keeps its 20 B's (GR16, GR14). REWRITE R2
      *> sends 20 bytes over record 3 -- equal -- so it succeeds with
      *> '00' and record 3 becomes 20 D's. G has an explicit RECORD
      *> CONTAINS 12 over a single 8-byte record: the record-name-1 is
      *> never the 12 bytes of a record in G, so its REWRITE is '44'.
      *> Before PB1168 the equality test ran only for a VARYING file,
      *> so both R1 and G1 were padded and replaced the record with
      *> '00'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1168RS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1168rs.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
           SELECT G ASSIGN TO "pb1168rg.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC X(10).
       01 R2 PIC X(20).
       FD G RECORD CONTAINS 12 CHARACTERS.
       01 G1 PIC X(8).
       WORKING-STORAGE SECTION.
       01 ST  PIC XX.
       01 W10 PIC X(10) VALUE "XXXXXXXXXX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F
           MOVE "AAAAAAAAAA" TO R1
           WRITE R1
           MOVE ALL "B" TO R2
           WRITE R2
           MOVE ALL "C" TO R2
           WRITE R2
           CLOSE F
           OPEN I-O F
           READ F
           READ F
           REWRITE R1 FROM W10
           DISPLAY "R1-ON-20=" ST
           READ F
           MOVE ALL "D" TO R2
           REWRITE R2
           DISPLAY "R2-ON-20=" ST
           CLOSE F
           OPEN INPUT F
           READ F
           READ F
           DISPLAY "REC2=" R2
           READ F
           DISPLAY "REC3=" R2
           CLOSE F
           OPEN OUTPUT G
           MOVE "GGGGGGGG" TO G1
           WRITE G1
           CLOSE G
           OPEN I-O G
           READ G
           MOVE "HHHHHHHH" TO G1
           REWRITE G1
           DISPLAY "G1-ON-12=" ST
           CLOSE G
           OPEN INPUT G
           READ G
           DISPLAY "GREC=" G1
           CLOSE G
           STOP RUN.
