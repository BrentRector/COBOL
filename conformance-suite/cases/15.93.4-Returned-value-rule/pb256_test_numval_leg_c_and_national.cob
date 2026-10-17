*>--- conformance-case
*> id: pb256_test_numval_leg_c_and_national
*> rules: [RV-15.93.4-1, RV-15.95.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB256 — §15.93.4 r1 c) / §15.95.4 r1 c), the THIRD leg of the returned value, with its NOTE's
      *> three error kinds; and the NATIONAL alternative of §15.93.3 r1 / §15.95.3 r1, which no fixture had
      *> exercised in EITHER direction. Leg c) had been argued on a record whose rule text stops at leg b) and
      *> recorded on a record whose cited test is a digit-cap unit test, so its own text had no covering
      *> evidence anywhere; the national alternative was held as "correct by construction", which is reasoning.
      *>
      *> THE RULE (cite.py --check OK on each): §15.93.4 r1 "(FUNCTION LENGTH (argument-1) + 1)" with
      *>   "NOTE These errors include, but are not limited to: - argument-1 is zero-length, - argument-1
      *>   contains only spaces, - argument-1 contains valid characters but is incomplete, such as the string
      *>   ' +.'." §15.95.4 r1 c) carries the identical leg and the identical NOTE for TEST-NUMVAL-F.
      *> §15.93.3 r1 / §15.95.3 r1: "Argument-1 shall be an alphanumeric or national literal or a data item of
      *> class alphanumeric or national."
      *>
      *> ⛔ LEG c) IS THE LEG WHOSE VALUE IS AN ARITHMETIC EXPRESSION OVER ANOTHER FUNCTION, so the fixture
      *> measures that arithmetic rather than a constant: LEN pins FUNCTION LENGTH(NSP) = 4 beside NSP's own
      *> verdict of 5, on the SAME national item, so "LENGTH + 1" is observed and not assumed.
      *>
      *> Hand-derived, NATIVE arithmetic at --std 2023 (leg c) and the class rule are mode-independent):
      *>   Z    TEST-NUMVAL("")          -> 1   NOTE kind 1, zero-length: LENGTH 0 + 1
      *>   SP   TEST-NUMVAL(SP4)         -> 5   NOTE kind 2, only spaces: LENGTH 4 + 1
      *>   INC  TEST-NUMVAL(" +.")       -> 4   NOTE kind 3's VERBATIM example: LENGTH 3 + 1
      *>   SGN  TEST-NUMVAL("+")         -> 2   valid but incomplete, a sign alone: LENGTH 1 + 1
      *>   DOT  TEST-NUMVAL(".")         -> 2   valid but incomplete, a separator alone
      *>   SGS  TEST-NUMVAL("+ ")        -> 3   sign + trailing space-string, still no digit: LENGTH 2 + 1
      *>   FZ   TEST-NUMVAL-F("")        -> 1   the same leg on the E-form
      *>   FSP  TEST-NUMVAL-F(SP4)       -> 5
      *>   FIN  TEST-NUMVAL-F(" +.")     -> 4
      *>   FE   TEST-NUMVAL-F("1E")      -> 3   a dangling E is incomplete, NOT a character in error
      *>   FES  TEST-NUMVAL-F("1E+")     -> 4   nor is an exponent sign with no digit
      *> NATIONAL — positions are NATIONAL CHARACTER POSITIONS, one per position, which is why NB's verdict
      *> discriminates: a byte-counted image of N"12 34" would answer 7, not 4.
      *>   NA   TEST-NUMVAL(NA)   N"123.45  " -> 0  conforms; the trailing national spaces are r2's
      *>   NB   TEST-NUMVAL(NB)   N"12 34"    -> 4  §15.93.4 r1 b) 1 — first non-space after the space
      *>   NC   TEST-NUMVAL(NC)   N"123cr"    -> 0  §15.67.3 r1's national CR bullet, "uppercase or
      *>                                            lowercase, or a combination"
      *>   NL   TEST-NUMVAL(N"12.5")          -> 0  the national LITERAL form of the argument rule
      *>   LEN  FUNCTION LENGTH(NSP)          -> 4  leg c)'s own operand, on national character positions
      *>   NS   TEST-NUMVAL(NSP)  N"    "     -> 5  leg c) over a NATIONAL item: LENGTH 4 + 1
      *>   NF   TEST-NUMVAL-F(N"1.5E+3")      -> 0  the E-form over a national literal (§15.69.3 r1's
      *>                                            national-E bullet)
      *>   NFB  TEST-NUMVAL-F(NFB) N"1 2E+3"  -> 3  §15.95.4 r1 b) 1 over national positions
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB256LGC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R   PIC S9(9).
       01 SP4 PIC X(4) VALUE SPACES.
       01 NA  PIC N(8) VALUE N"123.45  ".
       01 NB  PIC N(5) VALUE N"12 34".
       01 NC  PIC N(5) VALUE N"123cr".
       01 NSP PIC N(4) VALUE N"    ".
       01 NFB PIC N(6) VALUE N"1 2E+3".
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION TEST-NUMVAL("")
           IF R = 1 DISPLAY "Z OK" ELSE DISPLAY "Z BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(SP4)
           IF R = 5 DISPLAY "SP OK" ELSE DISPLAY "SP BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(" +.")
           IF R = 4 DISPLAY "INC OK" ELSE DISPLAY "INC BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL("+")
           IF R = 2 DISPLAY "SGN OK" ELSE DISPLAY "SGN BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(".")
           IF R = 2 DISPLAY "DOT OK" ELSE DISPLAY "DOT BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL("+ ")
           IF R = 3 DISPLAY "SGS OK" ELSE DISPLAY "SGS BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("")
           IF R = 1 DISPLAY "FZ OK" ELSE DISPLAY "FZ BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(SP4)
           IF R = 5 DISPLAY "FSP OK" ELSE DISPLAY "FSP BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(" +.")
           IF R = 4 DISPLAY "FIN OK" ELSE DISPLAY "FIN BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E")
           IF R = 3 DISPLAY "FE OK" ELSE DISPLAY "FE BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F("1E+")
           IF R = 4 DISPLAY "FES OK" ELSE DISPLAY "FES BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(NA)
           IF R = 0 DISPLAY "NA OK" ELSE DISPLAY "NA BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(NB)
           IF R = 4 DISPLAY "NB OK" ELSE DISPLAY "NB BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(NC)
           IF R = 0 DISPLAY "NC OK" ELSE DISPLAY "NC BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(N"12.5")
           IF R = 0 DISPLAY "NL OK" ELSE DISPLAY "NL BAD " R END-IF
           COMPUTE R = FUNCTION LENGTH(NSP)
           IF R = 4 DISPLAY "LEN OK" ELSE DISPLAY "LEN BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL(NSP)
           IF R = 5 DISPLAY "NS OK" ELSE DISPLAY "NS BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(N"1.5E+3")
           IF R = 0 DISPLAY "NF OK" ELSE DISPLAY "NF BAD " R END-IF
           COMPUTE R = FUNCTION TEST-NUMVAL-F(NFB)
           IF R = 3 DISPLAY "NFB OK" ELSE DISPLAY "NFB BAD " R END-IF
           STOP RUN.
       END PROGRAM PB256LGC.
