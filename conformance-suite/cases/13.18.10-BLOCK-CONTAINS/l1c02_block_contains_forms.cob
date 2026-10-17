      *>--- conformance-case
      *> id: l1c02_block_contains_forms
      *> rules: [GR-13.18.10.4-1, GR-13.18.10.4-2, FMT-13.18.10.2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.10.2 FMT + §13.18.10.4 GR1/GR2 — every BLOCK
      *> CONTAINS arm, and an FD that omits the clause.
      *> Format: BLOCK [CONTAINS] [integer-1 TO] integer-2
      *>         {CHARACTERS | RECORDS}   (CONTAINS and CHARACTERS are
      *> optional words; BLOCK, TO, RECORDS required).
      *> cite.py OK lines:
      *>  OK §13.18.10.2 (General format)
      *>  OK §13.18.10.1 The BLOCK CONTAINS clause specifies the size
      *>     of a physical record.
      *>  OK §13.18.10.3 1) If integer-1 is specified, integer-2 shall
      *>     be greater than integer-1.  (10<100, 10<20 below)
      *>  OK §13.18.10.4 1) This clause is required except when one or
      *>     more of the following conditions exist
      *>  OK §13.18.10.4 1) a) A physical record contains one and only
      *>     one complete logical record.
      *>  OK §13.18.10.4 2) The size of a physical record may be stated
      *>     in terms of records unless one or more of the following
      *>     situations exists, in which case the RECORDS phrase shall
      *>     not be used
      *>  OK §A.3 5) The BLOCK CONTAINS clause has no effect if the
      *>     operating environment does not support the required
      *>     features  (docs/CONFORMANCE.md A.3 row 5: Claimed (inert)
      *>     "Accepted; no effect on the managed I-O model").
      *> Files:
      *>  FA  BLOCK CONTAINS 2 RECORDS        (CONTAINS, RECORDS)
      *>  FB  BLOCK 100                       (no CONTAINS, no unit
      *>                                  word: CHARACTERS by default)
      *>  FC  BLOCK CONTAINS 10 TO 100 CHARACTERS (TO, CHARACTERS)
      *>  FD2 BLOCK 10 TO 20 RECORDS          (TO, RECORDS, no CONTAINS)
      *>  FE  no BLOCK CONTAINS at all - GR1: the managed I-O model
      *>      writes one logical record per physical record, so
      *>      condition a) holds and the clause is not required.
      *>  GR2: none of the situations a)-c) exists (no records span
      *>      physical records, no padding, no grouping), so the RECORDS
      *>      phrase of FA and FD2 is permitted and must be accepted.
      *> Derivation: every FD is legal, so the program compiles. By
      *> A.3 5) (the documented inert claim) the clause changes nothing
      *> about the logical records, so each file, written with three
      *> 8-character records R1..R3 and read back sequentially, returns
      *> exactly those records in order and then AT END:
      *>   <tag>=<prefix>0001,<prefix>0002,<prefix>0003,END
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C02A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "L1C02AA.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FB ASSIGN TO "L1C02AB.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FC ASSIGN TO "L1C02AC.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FD2 ASSIGN TO "L1C02AD.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FE ASSIGN TO "L1C02AE.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FA
           BLOCK CONTAINS 2 RECORDS.
       01  FA-REC PIC X(8).
       FD  FB
           BLOCK 100.
       01  FB-REC PIC X(8).
       FD  FC
           BLOCK CONTAINS 10 TO 100 CHARACTERS.
       01  FC-REC PIC X(8).
       FD  FD2
           BLOCK 10 TO 20 RECORDS.
       01  FD2-REC PIC X(8).
       FD  FE.
       01  FE-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  W-REC   PIC X(8).
       01  W-LINE  PIC X(40).
       01  W-PTR   PIC 99.
       01  W-EOF   PIC X.
       01  W-I     PIC 9.
       01  W-PFX   PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FA
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
               STRING "AAAA000" W-I DELIMITED BY SIZE INTO FA-REC
               WRITE FA-REC
           END-PERFORM
           CLOSE FA
           OPEN INPUT FA
           MOVE SPACES TO W-LINE
           MOVE 1 TO W-PTR
           STRING "FA=" DELIMITED BY SIZE INTO W-LINE
               WITH POINTER W-PTR
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ FA
                   AT END
                       MOVE "Y" TO W-EOF
                       STRING "END" DELIMITED BY SIZE INTO W-LINE
                           WITH POINTER W-PTR
                   NOT AT END
                       STRING FA-REC "," DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
               END-READ
           END-PERFORM
           CLOSE FA
           DISPLAY W-LINE
           OPEN OUTPUT FB
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
               STRING "BBBB000" W-I DELIMITED BY SIZE INTO FB-REC
               WRITE FB-REC
           END-PERFORM
           CLOSE FB
           OPEN INPUT FB
           MOVE SPACES TO W-LINE
           MOVE 1 TO W-PTR
           STRING "FB=" DELIMITED BY SIZE INTO W-LINE
               WITH POINTER W-PTR
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ FB
                   AT END
                       MOVE "Y" TO W-EOF
                       STRING "END" DELIMITED BY SIZE INTO W-LINE
                           WITH POINTER W-PTR
                   NOT AT END
                       STRING FB-REC "," DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
               END-READ
           END-PERFORM
           CLOSE FB
           DISPLAY W-LINE
           OPEN OUTPUT FC
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
               STRING "CCCC000" W-I DELIMITED BY SIZE INTO FC-REC
               WRITE FC-REC
           END-PERFORM
           CLOSE FC
           OPEN INPUT FC
           MOVE SPACES TO W-LINE
           MOVE 1 TO W-PTR
           STRING "FC=" DELIMITED BY SIZE INTO W-LINE
               WITH POINTER W-PTR
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ FC
                   AT END
                       MOVE "Y" TO W-EOF
                       STRING "END" DELIMITED BY SIZE INTO W-LINE
                           WITH POINTER W-PTR
                   NOT AT END
                       STRING FC-REC "," DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
               END-READ
           END-PERFORM
           CLOSE FC
           DISPLAY W-LINE
           OPEN OUTPUT FD2
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
               STRING "DDDD000" W-I DELIMITED BY SIZE INTO FD2-REC
               WRITE FD2-REC
           END-PERFORM
           CLOSE FD2
           OPEN INPUT FD2
           MOVE SPACES TO W-LINE
           MOVE 1 TO W-PTR
           STRING "FD2=" DELIMITED BY SIZE INTO W-LINE
               WITH POINTER W-PTR
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ FD2
                   AT END
                       MOVE "Y" TO W-EOF
                       STRING "END" DELIMITED BY SIZE INTO W-LINE
                           WITH POINTER W-PTR
                   NOT AT END
                       STRING FD2-REC "," DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
               END-READ
           END-PERFORM
           CLOSE FD2
           DISPLAY W-LINE
           OPEN OUTPUT FE
           PERFORM VARYING W-I FROM 1 BY 1 UNTIL W-I > 3
               STRING "EEEE000" W-I DELIMITED BY SIZE INTO FE-REC
               WRITE FE-REC
           END-PERFORM
           CLOSE FE
           OPEN INPUT FE
           MOVE SPACES TO W-LINE
           MOVE 1 TO W-PTR
           STRING "FE=" DELIMITED BY SIZE INTO W-LINE
               WITH POINTER W-PTR
           MOVE "N" TO W-EOF
           PERFORM UNTIL W-EOF = "Y"
               READ FE
                   AT END
                       MOVE "Y" TO W-EOF
                       STRING "END" DELIMITED BY SIZE INTO W-LINE
                           WITH POINTER W-PTR
                   NOT AT END
                       STRING FE-REC "," DELIMITED BY SIZE
                           INTO W-LINE WITH POINTER W-PTR
               END-READ
           END-PERFORM
           CLOSE FE
           DISPLAY W-LINE
           STOP RUN.
