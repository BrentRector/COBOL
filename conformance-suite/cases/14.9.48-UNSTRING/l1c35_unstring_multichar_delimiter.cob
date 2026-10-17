      *>--- conformance-case
      *> id: l1c35_unstring_multichar_delimiter
      *> rules: [GR-14.9.48.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR9 — a multi-character delimiter is contiguous,
      *>   ordered
      *> "Each literal-1 or the data item referenced by identifier-2
      *>   represents
      *> one delimiter. When a delimiter contains two or more
      *>   characters, all
      *> of the characters shall be present in contiguous positions of
      *>   the
      *> sending item, and in the order given, to be recognized as a
      *> delimiter."  (sentences 1-2; the zero-length sentences 3-4 are
      *> pinned by 2014/l1c35_unstring_zero_length_delimiter)
      *> OK  §14.9.48.4 9)  (General rules)
      *> OK  §14.9.48.4 11) d) delimiter moved to identifier-5 by MOVE;
      *>   at the
      *>     end of identifier-1, identifier-5 is space filled.
      *> OK  §14.9.48.4 11) e) COUNT = characters examined, excluding
      *>   the
      *>     delimiter.
      *> Derivation. S = "XBAYABZA-B" (10 chars), R1..R3 PIC X(5) preset
      *> "*****", D1..D3 PIC XX preset "##".
      *> M1 DELIMITED BY "AB": "BA" at 2-3 is the wrong order and "A-B"
      *>   at
      *>    8-10 is not contiguous; the only delimiter is "AB" at 5-6.
      *>    R1 = "XBAY " D1 = "AB" C1 = 4; R2 = "ZA-B " D2 = "  " (end
      *>      of S)
      *>    C2 = 4; S is exhausted (GR11g), so R3/D3/C3 keep their
      *>      preset
      *>    "*****", "##", 9.
      *>    An implementation matching any single character of the
      *>    delimiter, or either order, would split at position 2.
      *> M2 the same with the delimiter in identifier-2 W-AB = "AB" over
      *>    T = "12AB34A": R1 = "12   " C1 = 2, R2 = "34A  " C2 = 3 (the
      *>    final lone "A" is not the delimiter), R3 "*****" C3 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S    PIC X(10) VALUE "XBAYABZA-B".
       01 T    PIC X(7) VALUE "12AB34A".
       01 W-AB PIC XX VALUE "AB".
       01 R1 PIC X(5).
       01 R2 PIC X(5).
       01 R3 PIC X(5).
       01 D1 PIC XX.
       01 D2 PIC XX.
       01 D3 PIC XX.
       01 C1 PIC 9.
       01 C2 PIC 9.
       01 C3 PIC 9.
       PROCEDURE DIVISION.
           PERFORM PRESET
           UNSTRING S DELIMITED BY "AB"
               INTO R1 DELIMITER IN D1 COUNT IN C1
                    R2 DELIMITER IN D2 COUNT IN C2
                    R3 DELIMITER IN D3 COUNT IN C3
           END-UNSTRING
           DISPLAY "M1 [" R1 "][" D1 "]" C1 " [" R2 "][" D2 "]" C2
                   " [" R3 "][" D3 "]" C3
           PERFORM PRESET
           UNSTRING T DELIMITED BY W-AB
               INTO R1 COUNT IN C1
                    R2 COUNT IN C2
                    R3 COUNT IN C3
           END-UNSTRING
           DISPLAY "M2 [" R1 "]" C1 " [" R2 "]" C2 " [" R3 "]" C3
           STOP RUN.
       PRESET.
           MOVE "*****" TO R1 R2 R3
           MOVE "##" TO D1 D2 D3
           MOVE 9 TO C1 C2 C3.
