      *>--- conformance-case
      *> id: l1c17_colon_separator
      *> rules: [SR-8.3.5-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.5 7) — the colon is a separator, required when shown
      *> "The COBOL character colon, except as part of the invocation
      *>  operator, is a separator and is required when shown in the
      *>  general formats."
      *> cite.py --check 8.3.5 "The COBOL character colon, except as
      *>   part of the invocation operator, is a separator and is
      *>   required when shown in the general formats."
      *>   -> OK §8.3.5 7)
      *> The general format showing the colon (reference modifier):
      *> cite.py --check 8.4.3.3.2 "identifier-1( leftmost-position :
      *>   [ length ] )" -> OK §8.4.3.3.2
      *> Because the colon IS a separator, no space is needed on either
      *> side of it (W-S(2:3)), and spaces may surround it
      *> (W-S (1 : 2)) - both spell the same reference modifier.  The
      *> "required" half is pinned by the negative
      *> l1c17-refmod-colon-missing.
      *>
      *> DERIVED OUTPUT ("ABCDE", 8.4.3.3 leftmost-position : length):
      *>   A=BCD    W-S(2:3)       positions 2..4
      *>   B=AB     W-S (1 : 2)    positions 1..2
      *>   C=CDE    W-S(3:)        length omitted: 3..end
      *>   D=B      W-E (2)(1:1)   subscript then modifier: "BB"(1:1)
      *>   E=CD     W-S(W-I:W-I - 1) with W-I = 3: expressions around
      *>            the colon, (3:2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S  PIC X(5) VALUE "ABCDE".
       01 W-TV PIC X(6) VALUE "AABBCC".
       01 W-T REDEFINES W-TV.
          05 W-E PIC XX OCCURS 3 TIMES.
       01 W-I  PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A=" W-S(2:3).
           DISPLAY "B=" W-S (1 : 2).
           DISPLAY "C=" W-S(3:).
           DISPLAY "D=" W-E (2)(1:1).
           DISPLAY "E=" W-S(W-I:W-I - 1).
           STOP RUN.
