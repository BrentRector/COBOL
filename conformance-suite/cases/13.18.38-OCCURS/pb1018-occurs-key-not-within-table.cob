      *>--- conformance-case
      *> id: pb1018-occurs-key-not-within-table
      *> rules: [SR-13.18.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1018 - ISO 13.18.38.3 SR3: the first data-name-2 of
      *> the KEY phrase names the OCCURS entry or an entry subordinate
      *> to it. K OF C names no item under E (C is outside the table),
      *> and nothing checked the phrase until a SEARCH ALL read it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1018-NEG-KEY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 ASCENDING KEY IS K OF C INDEXED BY IX.
             10 A.
                15 K PIC 9.
       01 C.
          05 K PIC 9.
       PROCEDURE DIVISION.
       P0.
           DISPLAY "UNREACHABLE".
           STOP RUN.
