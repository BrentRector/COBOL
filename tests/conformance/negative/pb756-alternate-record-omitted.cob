      *> reject-at: 85 2002 2014 2023
      *> ISO §12.4.5.6.2 general format — RECORD is a REQUIRED word of the ALTERNATE RECORD KEY clause
      *> (kb/Work PB756):
      *>   ALTERNATE RECORD KEY IS { data-name-1 | record-key-name-1 SOURCE IS { data-name-2 } … } …
      *>   cite.py --check 12.4.5.6.2 "ALTERNATE RECORD KEY" -> OK §12.4.5.6.2 (figure notes: `ALTERNATE`,
      *>     `RECORD`, `SOURCE`, `DUPLICATES`, and `SUPPRESS` are underlined; `KEY`, `IS`, `WITH`, and
      *>     `WHEN` are not)
      *>   cite.py --check 5.2.2 "They are shown in uppercase and underlined in general formats" -> OK §5.2.2
      *> Measured on printed page 350 / folio 320: RECORD's box 131.86-171.34 carries a rule at
      *> 133.25-170.36 (94.0% cover). KEY and IS are optional words, RECORD is not, so `ALTERNATE KEY IS`
      *> is a syntax error at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB756N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb756n.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS F-K
               ALTERNATE KEY IS F-D.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-R.
          05 F-K PIC X(4).
          05 F-D PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
