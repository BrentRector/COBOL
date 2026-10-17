*>--- conformance-case
*> id: l1_trim_leading_trailing_multi_arg2
*> rules: [RV-15.96.4-1, RV-15.96.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.96.4 r1 (LEADING) and r2 (TRAILING) READ THROUGH r5's NESTING — the two returned-value
      *> rules this row is about, each measured with ONE argument-2 and then with SEVERAL.
      *>   r1 "If LEADING is specified, the returned value is a character string that consists of the
      *>      characters in argument-1 beginning from the leftmost character position that does not contain
      *>      any argument-2."
      *>   r2 "If TRAILING is specified, the returned value is a character string that consists of the
      *>      characters in argument-1 beginning from the leftmost character position through the rightmost
      *>      character position after which all characters contain argument-2."
      *>   r5 "If multiple argument-2s are specified, each argument-2 is processed completely in the order
      *>      that they are specified prior to processing the next argument-2."
      *> (cite.py --check 15.96.4 on each of the three sentences -> OK at rules 1, 2 and 5.)
      *> In the 2023 corpus because TRIM arrives at COBOL-2014 and its argument-2 form at COBOL-2023
      *> (E.3.3 item 31).
      *>
      *> ⛔ r1 AND r2 EACH SAY "any argument-2" / "argument-2" IN THE SINGULAR, AND r5 IS WHAT FIXES THE
      *> READING. Taken alone, r1's "does not contain ANY argument-2" reads as a SET UNION over the
      *> argument-2s, and that is exactly what this compiler used to compute — one char[] union and one
      *> TrimStart(set). r5 and its NOTE ("The processing of TRIM (arg-1 arg-2-1 arg-2-2) is the same as
      *> TRIM (TRIM (arg-1 arg-2-1) arg-2-2)") make r1 and r2 rules about ONE argument-2 applied at each
      *> level of a nesting, which is a different function whenever the argument-2 characters INTERLEAVE at
      *> an edge. The NOTE's own example, TRIM("aabbcc" "c" "b") = "aa", agrees under both readings and so
      *> cannot separate them; conformance:2023/pb117_trim_sequential pins the unkeyworded (BOTH) shape and
      *> one LEADING case. This fixture measures r1 and r2 in their own right, each against its union twin.
      *>
      *> Hand-derived, one line at a time, from r1/r2 + r5 — the union answer is written beside every
      *> multi-argument case and differs from the derived one on all four:
      *>   L1  TRIM("bbxbb" LEADING "b")      one argument-2, r1's base case: the leftmost position not
      *>                                      containing 'b' is 3, so the result is "xbb".
      *>   T1  TRIM("bbxbb" TRAILING "b")     r2's base case: the rightmost position after which all
      *>                                      characters are 'b' is 3, so the result is "bbx".
      *>   L2  TRIM("bcba" LEADING "b" "c")   inner leading-'b' -> "cba"; outer leading-'c' -> "ba".
      *>                                      Union {b,c} strips b,c,b and gives "a".
      *>   T2  TRIM("abcb" TRAILING "b" "c")  inner trailing-'b' -> "abc"; outer trailing-'c' -> "ab".
      *>                                      Union {b,c} strips b,c,b and gives "a".
      *>   L3  TRIM("bcba" LEADING "c" "b")   the SAME two characters in the other order: inner leading-'c'
      *>                                      strips nothing ('b' guards the edge) -> "bcba"; outer
      *>                                      leading-'b' -> "cba". Union is order-blind and still gives "a".
      *>   T3  TRIM("abcb" TRAILING "c" "b")  inner trailing-'c' strips nothing -> "abcb"; outer
      *>                                      trailing-'b' -> "abc". Union still gives "a".
      *>   L4  TRIM("abcb" LEADING "a" "b" "c")  THREE argument-2s, to show the fold is a fold and not a
      *>                                      special case for two: 'a' -> "bcb", 'b' -> "cb", 'c' -> "b".
      *>                                      Union {a,b,c} consumes the whole argument and gives a
      *>                                      zero-length result.
      *> L2/L3 and T2/T3 are ORDER-CARRYING PAIRS: they hold argument-1 and the argument-2 SET fixed and
      *> vary only the order, so a reading that ignores order — every set reading does — gives one answer
      *> where the rules give two.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1TRIMLT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM("bbxbb" LEADING "b") TO R
           IF R = "xbb" DISPLAY "L1 OK" ELSE DISPLAY "L1 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("bbxbb" TRAILING "b") TO R
           IF R = "bbx" DISPLAY "T1 OK" ELSE DISPLAY "T1 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("bcba" LEADING "b" "c") TO R
           IF R = "ba" DISPLAY "L2 OK" ELSE DISPLAY "L2 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("abcb" TRAILING "b" "c") TO R
           IF R = "ab" DISPLAY "T2 OK" ELSE DISPLAY "T2 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("bcba" LEADING "c" "b") TO R
           IF R = "cba" DISPLAY "L3 OK" ELSE DISPLAY "L3 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("abcb" TRAILING "c" "b") TO R
           IF R = "abc" DISPLAY "T3 OK" ELSE DISPLAY "T3 BAD [" R "]" END-IF
           MOVE FUNCTION TRIM("abcb" LEADING "a" "b" "c") TO R
           IF R = "b" DISPLAY "L4 OK" ELSE DISPLAY "L4 BAD [" R "]" END-IF
           STOP RUN.
       END PROGRAM L1TRIMLT.
