      *>--- conformance-case
      *> id: l1c04-implicit-index-move-target
      *> rules: [GR-8.5.2.8-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.2.8 GR1 — an IMPLICITLY usage-index item is of class
      *>   index, so it is not a MOVE operand.
      *> cite.py --check 8.5.2.8 "An elementary data item explicitly or
      *>   implicitly described as usage index" -> OK §8.5.2.8 1)
      *> cite.py --check 13.18.60.3 "An index data item may be
      *>   referenced explicitly only in a SEARCH or SET statement"
      *>   -> OK §13.18.60.3 10)
      *> I1 inherits USAGE INDEX from G (implicit), so it is an index
      *> data item and MOVE 5 TO I1 references it outside the places
      *> SR10 lists. COBOLNET0809 = MOVE operand of class index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE INDEX.
          05 I1.
          05 I2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO I1.
           STOP RUN.
