      *>--- conformance-case
      *> id: l1c35_unstring_zero_length_delimiter
      *> rules: [GR-14.9.48.4-9]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.48.4 GR9 — a zero-length delimiter item is ignored
      *> "If the data item referenced by identifier-2 or identifier-3 is
      *>   a
      *> zero-length item, that delimiter is ignored. When neither
      *>   literal-1
      *> nor literal-2 is specified and all data items referenced by
      *> identifier-2 and identifier-3 are zero-length items, it is as
      *>   if the
      *> DELIMITED phrase were not specified."  (sentences 3-4;
      *>   sentences
      *> 1-2 are pinned by 85/l1c35_unstring_multichar_delimiter)
      *> OK  §14.9.48.4 9)  (General rules)
      *> Edition: a zero-length item needs a DYNAMIC LENGTH elementary
      *>   item
      *> (constructs.json dynamic-length-item-2014, introducedIn 2014).
      *> OK  §8.6.4 "If no VALUE clause is specified, the length of that
      *>   item
      *>     in its initial state is zero."  (Z and Z2 below)
      *> OK  §14.9.48.4 8) "When any examination encounters two
      *>   contiguous
      *>     delimiters, the current receiving area shall be
      *>       space-filled"
      *> OK  §14.9.48.4 11) b) "If the DELIMITED BY phrase is not
      *>   specified,
      *>     the number of characters examined is equal to the size of
      *>       the
      *>     current receiving area"
      *> OK  §14.9.48.4 15) b) overflow: "all receiving areas have been
      *>   acted
      *>     upon, and the data item referenced by identifier-1 contains
      *>     characters that have not been examined"
      *> OK  §14.9.48.4 16) e) "The NOT ON OVERFLOW phrase, if
      *>   specified, is
      *>     ignored."
      *> Derivation. S = "AB,,CD;EF" (9 chars); A1..A3 PIC X(4) preset
      *> "****"; P = 1, T = 0 before each statement.
      *> ZA DELIMITED BY Z OR ",": Z ignored, "," is the only delimiter.
      *>    A1 "AB  "; the next character is "," again (two contiguous
      *>    delimiters) -> A2 "    "; A3 takes "CD;EF" truncated ->
      *>      "CD;E";
      *>    9 characters examined -> P = 10; 3 receivers -> T = 3; NOT.
      *>    A zero-length delimiter treated as matching would empty A1.
      *> ZB DELIMITED BY Z OR Z2 (all identifiers zero-length, no
      *>   literal):
      *>    as if DELIMITED were absent -> 4 characters per area: A1
      *>      "AB,,",
      *>    A2 "CD;E"; "F" not examined with all areas acted upon -> P =
      *>      9,
      *>    T = 2, overflow: ZB OVF (NOT ignored). Treating the phrase
      *>      as
      *>    present-but-never-matching would give A1 "AB,,", A2 "****",
      *>    P = 10 and NOT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C35E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC X(9) VALUE "AB,,CD;EF".
       01 Z  PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 Z2 PIC X DYNAMIC LENGTH LIMIT IS 8.
       01 A1 PIC X(4).
       01 A2 PIC X(4).
       01 A3 PIC X(4).
       01 P  PIC 99.
       01 T  PIC 9.
       PROCEDURE DIVISION.
           PERFORM PRESET
           UNSTRING S DELIMITED BY Z OR ","
               INTO A1 A2 A3 WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "ZA OVF"
               NOT ON OVERFLOW DISPLAY "ZA NOT"
           END-UNSTRING
           DISPLAY "ZA [" A1 "][" A2 "][" A3 "] P=" P " T=" T
           PERFORM PRESET
           UNSTRING S DELIMITED BY Z OR Z2
               INTO A1 A2 WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "ZB OVF"
               NOT ON OVERFLOW DISPLAY "ZB NOT"
           END-UNSTRING
           DISPLAY "ZB [" A1 "][" A2 "] P=" P " T=" T
           STOP RUN.
       PRESET.
           MOVE "****" TO A1 A2 A3
           MOVE 1 TO P
           MOVE 0 TO T.
