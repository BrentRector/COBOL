      *>--- conformance-case
      *> id: l1c27-same-as-type-of-enclosing-record
      *> rules: [SR-13.18.49.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.49.3 4) — data-name-1 has a TYPE naming the
      *> enclosing record
      *> "The description of data-name-1, including its subordinate data
      *> items, shall not contain a TYPE clause that references the
      *> record to which this entry is subordinate."
      *> cite.py --check 13.18.49.3 "shall not contain a TYPE clause
      *>   that references the record to which this entry is
      *>   subordinate"
      *>   -> OK §13.18.49.3 4)
      *> The SAME AS entry is B, subordinate to the record T (a
      *> TYPEDEF). Its data-name-1 is X, a level 1 group (SR7 holds)
      *> described by TYPE T: a TYPE clause that references T, the
      *> record B is subordinate to. This is SR4, not SR3: SR3 forbids a
      *> SAME AS that references "the subject of the entry or any group
      *> item to which this entry is subordinate" (B or T), and B's SAME
      *> AS references X. The only cycle runs through the TYPE clause.
      *> With X described as PIC X instead, the program compiles clean.
      *> Expected diagnostic: COBOLNET1557, the SAME AS cycle code (the
      *> catalog files it under SR3/SR4). The .err stops before the
      *> message's clause citation, which names SR3 for this SR4
      *> violation (a cosmetic mis-citation the adjudicator recorded).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 A PIC X.
          05 B SAME AS X.
       01 X TYPE T.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
