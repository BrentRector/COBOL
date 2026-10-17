      *>--- conformance-case
      *> id: l1c35_unstring_or_delimiter_sequence
      *> rules: [GR-14.9.48.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR10 — OR'd delimiters: one match, no shared
      *>   characters,
      *> applied in the sequence written
      *> "When two or more delimiters are specified in the DELIMITED BY
      *>   phrase,
      *> an OR condition exists between them. Each delimiter is compared
      *>   to
      *> the sending field. If a match occurs, the character(s) in the
      *>   sending
      *> field is considered to be a single delimiter. No character(s)
      *>   in the
      *> sending field shall be considered a part of more than one
      *>   delimiter.
      *> Each delimiter is applied to the sending field in the sequence
      *> specified in the UNSTRING statement."
      *> OK  §14.9.48.4 10)  (General rules)  [both quoted sentences
      *>   checked]
      *> OK  §14.9.48.4 11) b) "the examination proceeds left to right
      *>   until
      *>     a delimiter specified by either literal-1 or the value of
      *>       the
      *>     data item referenced by identifier-2 is encountered"
      *> OK  §14.9.48.4 11) f) "further examined beginning with the
      *>   first
      *>     character position to the right of the delimiter"
      *> OK  §14.9.48.4 8) two contiguous delimiters space-fill the
      *>   area.
      *> Receivers R1..R3 PIC X(3) preset "***", D1..D3 PIC XX preset
      *>   "##".
      *> Q1 "XABY" BY "A" OR "AB": at position 2 "A" is applied first
      *>   and
      *>    matches: R1 "X" D1 "A"; examination resumes at "B": R2 "BY",
      *>    D2 spaces (end of S); R3/D3 untouched.
      *>    -> Q1 [X  ][A ][BY ][  ][***][##]
      *> Q2 same S BY "AB" OR "A": "AB" is applied first at position 2:
      *>    R1 "X" D1 "AB", R2 "Y" D2 spaces.
      *>    -> Q2 [X  ][AB][Y  ][  ][***][##]
      *>    (Q1/Q2 differ only in the written sequence; a longest-match
      *>      or
      *>    last-listed rule prints the same line twice.)
      *> Q3 "XAYZ" BY "Y" OR "A": the left-to-right examination meets
      *>   "A"
      *>    (position 2) before "Y": R1 "X" D1 "A"; "Y" follows at once
      *>      (two
      *>    contiguous delimiters): R2 spaces D2 "Y"; R3 "Z" D3 spaces.
      *>    -> Q3 [X  ][A ][   ][Y ][Z  ][  ]
      *>    (searching all of S for "Y" first would give R1 "XA".)
      *> Q4 "XABCY" BY "AB" OR "BC": "AB" matches at 2-3; its "B" may
      *>   not
      *>    also begin "BC", so examination resumes at "C": R1 "X" D1
      *>      "AB",
      *>    R2 "CY" D2 spaces.
      *>    -> Q4 [X  ][AB][CY ][  ][***][##]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S4 PIC X(4) VALUE "XABY".
       01 S4B PIC X(4) VALUE "XAYZ".
       01 S5 PIC X(5) VALUE "XABCY".
       01 R1 PIC X(3).
       01 R2 PIC X(3).
       01 R3 PIC X(3).
       01 D1 PIC XX.
       01 D2 PIC XX.
       01 D3 PIC XX.
       PROCEDURE DIVISION.
           PERFORM PRESET
           UNSTRING S4 DELIMITED BY "A" OR "AB"
               INTO R1 DELIMITER IN D1 R2 DELIMITER IN D2
                    R3 DELIMITER IN D3
           END-UNSTRING
           DISPLAY "Q1 [" R1 "][" D1 "][" R2 "][" D2 "][" R3 "][" D3 "]"
           PERFORM PRESET
           UNSTRING S4 DELIMITED BY "AB" OR "A"
               INTO R1 DELIMITER IN D1 R2 DELIMITER IN D2
                    R3 DELIMITER IN D3
           END-UNSTRING
           DISPLAY "Q2 [" R1 "][" D1 "][" R2 "][" D2 "][" R3 "][" D3 "]"
           PERFORM PRESET
           UNSTRING S4B DELIMITED BY "Y" OR "A"
               INTO R1 DELIMITER IN D1 R2 DELIMITER IN D2
                    R3 DELIMITER IN D3
           END-UNSTRING
           DISPLAY "Q3 [" R1 "][" D1 "][" R2 "][" D2 "][" R3 "][" D3 "]"
           PERFORM PRESET
           UNSTRING S5 DELIMITED BY "AB" OR "BC"
               INTO R1 DELIMITER IN D1 R2 DELIMITER IN D2
                    R3 DELIMITER IN D3
           END-UNSTRING
           DISPLAY "Q4 [" R1 "][" D1 "][" R2 "][" D2 "][" R3 "][" D3 "]"
           STOP RUN.
       PRESET.
           MOVE "***" TO R1 R2 R3
           MOVE "##" TO D1 D2 D3.
