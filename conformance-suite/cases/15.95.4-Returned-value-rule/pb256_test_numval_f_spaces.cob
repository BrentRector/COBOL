*>--- conformance-case
*> id: pb256_test_numval_f_spaces
*> rules: [RV-15.95.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB256 — ⛔ THE DETERMINATION THAT USED TO BE MADE IN SILENCE: a space does NOT split the
      *> exponent's `n`. Two ISO sentences pull in opposite directions and they give DIFFERENT answers, so
      *> this fixture exists to pin the one COBOL.NET took (docs/CONFORMANCE.md §3).
      *>
      *> ⛔ THE RULES ARE THE NUMVAL-F FUNCTION'S. §15.95.1 says TEST-NUMVAL-F "verifies that the contents of
      *> argument-1 conform to the specification for argument-1 of the NUMVAL-F function", and §15.95.4 r1 a)
      *> imports §15.69.3 whole, so every citation below is to the NUMVAL-F clause and reaches this function
      *> through that import.
      *>
      *> §15.69.3 r5 (NUMVAL-F argument rules) — "Leading and trailing spaces in argument-1 are ignored. Embedded spaces in
      *> argument-1 are ignored EXCEPT between the first numeric digit and the last digit that precedes a
      *> letter 'E'" (cite.py --check 15.69.3 -> OK, rule 5). Its except-clause names only the SIGNIFICAND.
      *> Read as "elide the space and JOIN the runs on either side", an exponent written with an interior
      *> space would be one number: "1E+1 234" would have the four-digit exponent 1234 and CONFORM (0), and
      *> "1E+1 2345" would have the five-digit exponent 12345, whose fifth digit §15.95.4 r1 b) 5 puts at
      *> position 9. COBOL.NET answers 0 to NEITHER and 9 to neither — see EIN4/EIN5 below.
      *>
      *> THE OTHER READING WINS, on two independent grounds:
      *>  (a) §15.69.3 r1's format figure — verified on the PRINTED page 898, not only in the transcription
      *>      — draws `n` as ONE contiguous metavariable ("n is one, two, three, or four digits representing
      *>      the exponent"; cite.py --check 15.69.3 -> OK, rule 1) with [ space-string ] only BEFORE and
      *>      AFTER it. So the space in "1E+1 2345" is the legal TRAILING space-string and the '2' after it
      *>      is simply outside the format.
      *>  (b) §15.95.4 r1 b) 1 governs the case directly and is UNQUALIFIED as to which digit run: "if one
      *>      or more spaces are embedded within a string of numeric characters, the returned value is the
      *>      position of the first non-space character following the spaces" (cite.py --check 15.95.4 ->
      *>      OK, 1) b) 1.) — while sub-notes 2, 3 and 4 of that same list each say "of the significand"
      *>      explicitly. The drafters distinguished inside one list, so sub-note 1 reaches the exponent.
      *> "ignored" therefore means the space CONTRIBUTES NOTHING where the figure admits one, never that the
      *> runs on either side join.
      *>
      *> Hand-derived, under NATIVE arithmetic at --std 2023 (§15.95.4 r1 b) 5's own precondition names the
      *> standard modes; under native the same positions fall out of leg b) generic, because r1's where-
      *> clause caps `n` at four digits with no arithmetic-mode qualification — the standard-decimal side is
      *> conformance:2014/pb256_test_numval_standard_decimal):
      *>   EIN4  "1E+1 234"    -> 6   the '2' at position 6 (⛔ 0 under the joined reading)
      *>   EIN5  "1E+1 2345"   -> 6   the '2' at position 6 (⛔ 9 under the joined reading)
      *>   ESIG  "1E + 1 2"    -> 8   the same, with the figure's other two space slots occupied
      *>   EFIVE "1E+12345"    -> 8   CONTIGUOUS five-digit exponent: r1 b) 5's own shape, fifth digit at 8
      *>   EBEF  "1E+ 1234"    -> 0   the figure's [ space-string ] BETWEEN the exponent sign and n
      *>   EAFT  "1E+1234 "    -> 0   the figure's TRAILING [ space-string ] after n
      *>   SIG1  "0 1E+2"      -> 3   §15.95.4 r1 b) 1's VERBATIM example (the significand's own space)
      *>   SIG2  "1 2E+3"      -> 3   the same rule one position along
      *>   ALLSP " + 1.5E + 1 " -> 0  every space in a figure-licensed slot
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256FSP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+1 234")
           IF R = 6 DISPLAY "EIN4 OK" ELSE DISPLAY "EIN4 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+1 2345")
           IF R = 6 DISPLAY "EIN5 OK" ELSE DISPLAY "EIN5 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E + 1 2")
           IF R = 8 DISPLAY "ESIG OK" ELSE DISPLAY "ESIG BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+12345")
           IF R = 8 DISPLAY "EFIVE OK" ELSE DISPLAY "EFIVE BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+ 1234")
           IF R = 0 DISPLAY "EBEF OK" ELSE DISPLAY "EBEF BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+1234 ")
           IF R = 0 DISPLAY "EAFT OK" ELSE DISPLAY "EAFT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("0 1E+2")
           IF R = 3 DISPLAY "SIG1 OK" ELSE DISPLAY "SIG1 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1 2E+3")
           IF R = 3 DISPLAY "SIG2 OK" ELSE DISPLAY "SIG2 BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(" + 1.5E + 1 ")
           IF R = 0 DISPLAY "ALLSP OK" ELSE DISPLAY "ALLSP BAD " R END-IF
           STOP RUN.
       END PROGRAM PB256FSP.
