      *> kb/Work PB314 -- FIND-STRING counts OVERLAPPING matches.
      *>   python scripts/spec/cite.py --check 15.37.4 "the character
      *>   position of the first occurrence where the string
      *>   represented by argument-2 matches a substring within
      *>   argument-1"                          -> OK  §15.37.4 1)
      *>   python scripts/spec/cite.py --check 15.37.4 "argument-3
      *>   represents the number of matches to ignore before
      *>   determining the character position that shall be
      *>   returned"                            -> OK  §15.37.4 2)
      *>   python scripts/spec/cite.py --check 15.37.4 "If no match is
      *>   found, the function shall return zero" -> OK  §15.37.4 3)
      *> DETERMINATION (docs/CONFORMANCE.md "DETERMINATION -- FIND-STRING
      *> counts overlapping matches"): §15.37.4 states no consumption or
      *> resumption rule, unlike SUBSTITUTE's §15.87.4 r3 ("...plus the
      *> length of argument-2"), so every character POSITION at which
      *> argument-2 equals a substring of argument-1 is a match, whether
      *> or not it overlaps the one before. GnuCOBOL 3.2 lists
      *> FIND-STRING as not implemented, so rule 1's precedence has no
      *> dialect to consult.
      *> DERIVATION. H = "AAAAA" (5 characters), N = "AA": N matches at
      *> positions 1, 2, 3 and 4 (overlapping); a non-overlapping
      *> reading would find only 1 and 3.
      *>   no LAST, no argument-3         first match                  1
      *>   LAST                           last match                   4
      *>   argument-3 = 1                 ignore 1 -> the 2nd match    2
      *>   argument-3 = 2                 ignore 2 -> the 3rd match    3
      *>   LAST, argument-3 = 1           ignore the last -> the 3rd   3
      *>   argument-3 = 4                 ignore all four -> r3        0
      *> The literal form: "AAAA" has matches at 1, 2, 3, so ("AAAA" "AA" 2)
      *> ignores two and returns the third, 3 (a non-overlapping reading
      *> would have matches 1 and 3 only and return 0).
      *> ANYCASE (r4): "aAaAa" matches "AA" at 1..4 when case is folded,
      *> so LAST ANYCASE is 4.
      *> Fails if the function advances by len(argument-2) after a match.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB314FSO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H  PIC X(5) VALUE "AAAAA".
       01 H2 PIC X(5) VALUE "aAaAa".
       01 N  PIC X(2) VALUE "AA".
       01 P  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FIND-STRING(H N) TO P.
           DISPLAY "FIRST=" P.
           MOVE FUNCTION FIND-STRING(H N LAST) TO P.
           DISPLAY "LAST=" P.
           MOVE FUNCTION FIND-STRING(H N 1) TO P.
           DISPLAY "SKIP1=" P.
           MOVE FUNCTION FIND-STRING(H N 2) TO P.
           DISPLAY "SKIP2=" P.
           MOVE FUNCTION FIND-STRING(H N LAST 1) TO P.
           DISPLAY "LAST-SKIP1=" P.
           MOVE FUNCTION FIND-STRING(H N 4) TO P.
           DISPLAY "SKIP4=" P.
           MOVE FUNCTION FIND-STRING("AAAA" "AA" 2) TO P.
           DISPLAY "LIT-SKIP2=" P.
           MOVE FUNCTION FIND-STRING(H2 N LAST ANYCASE) TO P.
           DISPLAY "ANYCASE-LAST=" P.
           STOP RUN.
       END PROGRAM PB314FSO.
