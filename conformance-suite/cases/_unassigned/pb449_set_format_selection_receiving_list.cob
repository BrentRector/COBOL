      *>--- conformance-case
      *> id: pb449_set_format_selection_receiving_list
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.39.2 Formats 1 and 2 - the format is selected from the WHOLE receiving list, and every legal
      *> list still binds. Format 1's receiving brace is { index-name-1 | identifier-1 } ... and 14.9.39.3 SR1
      *> makes identifier-1 "a data item of class index or an integer data item", so an index-name, an index data
      *> item and an integer item may stand in ONE receiving list. Format 2's is { index-name-3 } ... alone.
      *>
      *> ⛔ THE POINT OF THIS GOLDEN IS ORDER-INDEPENDENCE (kb/Work PB449). The format used to be sniffed from
      *> receivers[0] by each candidate arm in turn, so the SAME operands could bind one way when written in one
      *> order and another way - or crash - when written in the other. Lines 3 and 9 below write the two
      *> index-names in OPPOSITE orders on purpose; both are Format 2 / Format 1 and both must answer the same.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE GENERAL RULES, NOT MEASURED:
      *>   SET IX1 IX2 TO 2      - GR2 a)1.c: each index-name refers to the element corresponding to 2.
      *>   SET WS-A TO IX1       - GR2 c): a numeric identifier-1 is set to the OCCURRENCE NUMBER, so WS-A = 2.
      *>   SET IX2 IX1 UP BY 3   - GR4 b): each index-name is incremented by 3, so both refer to occurrence 5.
      *>   E (IX1) after that    - occurrence 5 of "AABBCCDDEE" split 5 x PIC X(2), i.e. "EE".
      *>   SET IDXD TO IX1       - GR2 b): an INDEX DATA ITEM receives index-name-2's content UNCHANGED.
      *>   SET IX2 TO IDXD       - GR2 a)2.b: identifier-2 (class index) puts IX2 on occurrence 5.
      *>   SET IX1 IDXD WS-B TO IX2 - one receiving list of all three identifier-1 kinds, sender index-name-2 as
      *>                           SR4 requires; IX2 is on occurrence 3 by then, so WS-B = 3 and E (IX1) = "CC".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB449-SET-FORMAT-SEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(2) OCCURS 5 TIMES INDEXED BY IX1 IX2.
       01 IDXD USAGE INDEX.
       01 WS-A PIC 9(1).
       01 WS-B PIC 9(1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AABBCCDDEE" TO T.
           SET IX1 IX2 TO 2.
           SET WS-A TO IX1.
           DISPLAY "F1=" WS-A " E=" E (IX2).
           SET IX2 IX1 UP BY 3.
           SET WS-B TO IX2.
           DISPLAY "F2=" WS-B " E=" E (IX1).
           SET IDXD TO IX1.
           SET IX2 TO 1.
           SET IX2 TO IDXD.
           SET WS-A TO IX2.
           DISPLAY "IDXD=" WS-A " E=" E (IX2).
           SET IX2 TO 3.
           SET IX1 IDXD WS-B TO IX2.
           DISPLAY "MIX=" WS-B " E=" E (IX1).
           STOP RUN.
