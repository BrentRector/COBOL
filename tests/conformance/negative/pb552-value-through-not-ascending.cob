      *> reject-at: 2002 2014 2023
      *> ISO §13.18.63.3 26) - every THROUGH pair of a condition-name's VALUE clause ascends.
      *> "When the THROUGH phrase is specified: a) when literal-2 is of a class other than alphanumeric
      *> or national, the value of literal-2 shall be less than the value of literal-3. b) when literal-2
      *> is of class alphanumeric or national, and the runtime collating sequence is known, the value of
      *> literal-2 shall be less than the value of literal-3."
      *> cite.py --check 13.18.63.3 "the value of literal-2 shall be less than the value of literal-3"
      *>   -> OK §13.18.63.3 26)
      *> "known" (b): every sequence this processor fixes at compile time - an alphabet named by IN, the
      *> PROGRAM COLLATING SEQUENCE, the native order - is known; only a LOCALE one is not (SR26 NOTE;
      *> docs/CONFORMANCE.md D-RANGE-KNOWN, kb/Work PB552). Each 88 below is one arm, each refused with
      *> COBOLNET2961; the .err pins the first. Before PB552 all compiled clean and each condition was
      *> simply never TRUE.
      *>   C-NUM   5 THRU 1          a) reversed numeric
      *>   C-EQ    5 THRU 5          a) "less than": an equal pair is refused too
      *>   C-LIST  1 THRU 3, 9 THRU 7    the SECOND pair of a list
      *>   C-DEC   9.5 THRU 1.5      a) non-integer
      *>   C-PCS   "A" THRU "M"      b) inverted under the PROGRAM COLLATING SEQUENCE AL (A=25, M=13)
      *>   C-IN    "D" THRU "P" IN AL    b) inverted in the NAMED alphabet (D=22, P=10), ascending natively
      *> The conforming twins compile: 1 THRU 5, "M" THRU "A" IN AL (pinned by 2002/pb398_range_in_alphabet,
      *> 2002/pb502_value_range_in_alphabet). The IN phrase is COBOL 2002; the rule is edition-invariant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB552NEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XCOMP PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CV PIC 9.
          88 C-NUM  VALUE 5 THRU 1.
          88 C-EQ   VALUE 5 THRU 5.
          88 C-LIST VALUE 1 THRU 3, 9 THRU 7.
       01 CK PIC 9V9.
          88 C-DEC  VALUE 9.5 THRU 1.5.
       01 CX PIC X.
          88 C-PCS  VALUE "A" THRU "M".
          88 C-IN   VALUE "D" THRU "P" IN AL.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
