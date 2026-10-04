      *> kb/Work PB986 - ISO 13.18.45.3 SR3: "Data-name-1, data-name-2 and data-name-3 shall not be subject to
      *>   any OCCURS clauses." The rule bars an OCCURS on the three NAMED operands only; a table that lies
      *>   INSIDE the range they delimit is legal (SR8 bars only a variable-length item or an occurs-depending
      *>   table from the range). 13.18.45.4 GR2: with THROUGH, data-name-1 "defines an alphanumeric group
      *>   item that includes all elementary items starting with data-name-2 ... and concluding with
      *>   data-name-3", so SALIAS is the storage window from SG through S3, the table cells included.
      *>   cite.py: OK  13.18.45.3 3)  (Syntax rules)
      *>   cite.py: OK  13.18.45.4 2)  (General rules)
      *> The window of SALIAS is 3 (SG) + 3 (S2) + 4 (ST: 2 occurrences of S4 + S5) + 8 (GT: 2 occurrences of
      *> GA OCCURS 3 + GB) + 4 (NT: 2 x 2 occurrences of NL) + 2 (SR) + 3 (S3) = 27 characters, in storage
      *> order, so reading it gives c12 def x1y2 uvw4rst5 pqrs gh ijk; the TALIAS window S2 through SR is the
      *> 21 characters from def to gh. MOVE of a 27-character literal to SALIAS (13.18.45.4 GR2 makes it an
      *> alphanumeric item over the window) distributes its characters over every cell, so SRC then holds
      *> ab + that literal. Before the fix each leaf under a table GROUP counted once and the alias was refused
      *> COBOLNET1655 "the record's leaves do not tile the alias's storage window".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB986A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 S0 PIC X(2) VALUE "ab".
          05 SG.
             10 S1 PIC X VALUE "c".
             10 N1 PIC 9(2) VALUE 12.
          05 S2 PIC X(3) VALUE "def".
          05 ST OCCURS 2.
             10 S4 PIC X.
             10 S5 PIC 9.
          05 GT OCCURS 2.
             10 GA PIC X OCCURS 3.
             10 GB PIC 9.
          05 NT OCCURS 2.
             10 NI OCCURS 2.
                15 NL PIC X.
          05 SR PIC X(2) VALUE "gh".
          05 S3 PIC X(3) VALUE "ijk".
          66 SALIAS RENAMES SG THROUGH S3.
          66 TALIAS RENAMES S2 THROUGH SR.
       PROCEDURE DIVISION.
       M1.
           MOVE "x1" TO ST (1)
           MOVE "y2" TO ST (2)
           MOVE "uvw4" TO GT (1)
           MOVE "rst5" TO GT (2)
           MOVE "pq" TO NT (1)
           MOVE "rs" TO NT (2)
           DISPLAY "[" SALIAS "]"
           DISPLAY "[" TALIAS "]"
           MOVE "Z98UVWa1b2ghi3jkl4mnopQRSTU" TO SALIAS
           DISPLAY SRC
           DISPLAY S4 (2) S5 (2) GA (2 3) NL (2 1)
           STOP RUN.
