      *> reject-at: 2002 2014 2023
      *> kb/Work PB1391, the EVALUATE seeding site of the same carry. 14.9.13.3 SR8 treats the partial-expression
      *> `= B2 OR <> B3` as the conditional expression that results from preceding it by the selection subject B1,
      *> i.e. `B1 = B2 OR B1 <> B3` with the second relation abbreviated. B1 = B2 is a boolean relation
      *> (8.8.4.2.1), so 8.8.4.12.3 SR1 forbids the abbreviation exactly as in an IF.
      *>   cite.py --check 14.9.13.3 "If a selection object is specified by partial-expression-1, that selection
      *>     object is treated as though it were specified as condition-2" -> OK 8)
      *>   cite.py --check 8.8.4.12.3 "Relation-condition-1 shall not be a boolean relation condition." -> OK 1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1391NEG4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B1 PIC 1 VALUE B"0".
       01 B2 PIC 1 VALUE B"1".
       01 B3 PIC 1 VALUE B"1".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE B1
               WHEN = B2 OR <> B3 DISPLAY "T"
               WHEN OTHER DISPLAY "F"
           END-EVALUATE
           STOP RUN.
