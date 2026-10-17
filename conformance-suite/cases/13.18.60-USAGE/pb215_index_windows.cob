      *>--- conformance-case
      *> id: pb215_index_windows
      *> rules: [SR-13.18.60.3-10, DOC-A.1-194]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> THE TWO INDEX CLAUSES LIST DIFFERENT CONTEXTS (kb/Work PB215), and this
      *> program uses every legal side of the difference at once.
      *>
      *> ISO 13.18.60.3 SR10: "An index data item may be referenced explicitly
      *> only in a SEARCH or SET statement, a relation condition, an intrinsic
      *> function argument" ... - so SET IDX TO IX, SET IX TO IDX and IF IDX = IX
      *> are legal. ISO 13.18.38.3 r7 admits an index-NAME "as a subscript" and
      *> "in the VARYING phrase of a PERFORM statement" - so E(IX + 1) and
      *> PERFORM VARYING V FROM IX are legal. (The illegal sides - an index DATA
      *> item in a subscript or in PERFORM VARYING - are the pb215 negatives.)
      *>
      *> EXPECTED OUTPUT, from the rules:
      *>   SET IX TO 3; SET IDX TO IX; SET IX TO 1; SET IX TO IDX -> IX is 3 again,
      *>   so the relation IDX = IX is true            -> REL=EQ
      *>   E(IX + 1) is occurrence 4 of "ABCDE"         -> SUB=D
      *>   14.9.28.4 GR12: the initialization value of FROM IX is "the occurrence
      *>   number corresponding to the value of the index" = 3; BY 1 augments;
      *>   UNTIL V > 4 stops after 4                    -> V=03, V=04
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB215P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       01 V PIC 99.
       01 T.
          05 E PIC X OCCURS 5 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCDE" TO T
           SET IX TO 3
           SET IDX TO IX
           SET IX TO 1
           SET IX TO IDX
           IF IDX = IX
               DISPLAY "REL=EQ"
           ELSE
               DISPLAY "REL=NE"
           END-IF
           DISPLAY "SUB=" E(IX + 1)
           PERFORM VARYING V FROM IX BY 1 UNTIL V > 4
               DISPLAY "V=" V
           END-PERFORM
           STOP RUN.
