      *>--- conformance-case
      *> id: l1c06_define_literal_categories
      *> rules: [GR-7.3.11.4-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.11.4 GR8 DEFINE directive — literal-1 of every
      *>   category is what the name references
      *> Rule: "If literal-1 is specified, compilation-variable-name-1
      *>   references literal-1."
      *>   cite.py --check 7.3.11.4 "If literal-1 is specified,
      *>     compilation-variable-name-1
      *>   references literal-1."  -> OK  §7.3.11.4 8)  (General rules)
      *>   cite.py --check 7.3.11.4 "If the operand of the DEFINE
      *>     directive consists of a
      *>   single numeric literal, that operand is treated as a literal,
      *>     not as an
      *>   arithmetic-expression."  -> OK  §7.3.11.4 5)  (General rules)
      *>   cite.py --check 7.3.11.4 "compilation-variable-name-1 may be
      *>     used in the
      *>   compilation group in any compiler directive where a literal
      *>     of the category
      *>   associated with the name is permitted"  -> OK  §7.3.11.4 1)
      *>     (General rules)
      *> The observable is a constant conditional expression, whose
      *>   operands must be of the
      *> same category and, when not numeric, compared only for
      *>   (in)equality:
      *>   cite.py --check 7.3.8.2 "The operands shall be of the same
      *>     category."
      *>   -> OK  §7.3.8.2 1) a) 1.  (Syntax rules)
      *>   cite.py --check 7.3.8.2 "If literals are specified and they
      *>     are not numeric
      *>   literals, the relational operator shall be"  -> OK  §7.3.8.2
      *>     1) a) 2.
      *> Derivation of each expected line (each name must equal ITS
      *>   literal and differ
      *> from a near-miss literal of the same category; a wrong binding
      *>   prints -WRONG):
      *>   NUM=3      NUM references the numeric literal 3 (GR5 + GR8):
      *>     3 = 3, NOT > 3.
      *>   FRC=1.5    FRC references 1.5 (not truncated): = 1.5 and NOT
      *>     = 1.
      *>   ALN=ABC    ALN references "ABC": = "ABC", NOT = "ABD".
      *>   NAT=AB     NAT references the national literal N"AB": =
      *>     N"AB", NOT = N"AC".
      *>   BOO=10     BOO references the boolean literal B"10": = B"10",
      *>     NOT = B"01".
      *>   HEX=A      HEX references X"41", the alphanumeric literal
      *>     whose one character
      *>              is code 41 (DOC-A.1-95: U+0041, 'A'): = "A", NOT =
      *>                "B".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06B.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE NUM AS 3
       >>DEFINE FRC AS 1.5
       >>DEFINE ALN AS "ABC"
       >>DEFINE NAT AS N"AB"
       >>DEFINE BOO AS B"10"
       >>DEFINE HEX AS X"41"
       >>IF NUM = 3 AND NOT NUM > 3
           DISPLAY "NUM=3".
       >>ELSE
           DISPLAY "NUM-WRONG".
       >>END-IF
       >>IF FRC = 1.5 AND NOT FRC = 1
           DISPLAY "FRC=1.5".
       >>ELSE
           DISPLAY "FRC-WRONG".
       >>END-IF
       >>IF ALN = "ABC" AND ALN NOT = "ABD"
           DISPLAY "ALN=ABC".
       >>ELSE
           DISPLAY "ALN-WRONG".
       >>END-IF
       >>IF NAT = N"AB" AND NAT NOT = N"AC"
           DISPLAY "NAT=AB".
       >>ELSE
           DISPLAY "NAT-WRONG".
       >>END-IF
       >>IF BOO = B"10" AND BOO NOT = B"01"
           DISPLAY "BOO=10".
       >>ELSE
           DISPLAY "BOO-WRONG".
       >>END-IF
       >>IF HEX = "A" AND HEX NOT = "B"
           DISPLAY "HEX=A".
       >>ELSE
           DISPLAY "HEX-WRONG".
       >>END-IF
           STOP RUN.
