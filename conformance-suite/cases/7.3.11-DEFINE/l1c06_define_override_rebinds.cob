      *>--- conformance-case
      *> id: l1c06_define_override_rebinds
      *> rules: [GR-7.3.11.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.11.4 GR3 DEFINE directive — OVERRIDE unconditionally
      *>   rebinds the compilation variable
      *> Rule: "If the OVERRIDE phrase is specified,
      *>   compilation-variable-name-1 is
      *> unconditionally set to reference the value of the specified
      *>   operand."
      *>   cite.py --check 7.3.11.4 "If the OVERRIDE phrase is
      *>     specified,
      *>   compilation-variable-name-1 is unconditionally set to
      *>     reference the value of the
      *>   specified operand."  -> OK  §7.3.11.4 3)  (General rules)
      *> Context: without OVERRIDE (and without OFF) a redefinition must
      *>   repeat the value
      *> of the last previous DEFINE of the name:
      *>   cite.py --check 7.3.11.3 "the last previous DEFINE directive
      *>     referring to
      *>   compilation-variable-name-1 shall have specified the same
      *>     value"
      *>   -> OK  §7.3.11.3 2)  (Syntax rules)
      *> and the name may then be used wherever a literal of its
      *>   category is permitted:
      *>   cite.py --check 7.3.11.4 "compilation-variable-name-1 may be
      *>     used in the
      *>   compilation group in any compiler directive where a literal
      *>     of the category
      *>   associated with the name is permitted"  -> OK  §7.3.11.4 1)
      *>     (General rules)
      *> Derivation of each expected line:
      *>   A V=2  V is 1, then "AS 2 OVERRIDE": GR3 sets V to 2
      *>     regardless of SR2, so
      *>          >>IF V = 2 keeps the first branch (V = 1 would print A
      *>            V=1).
      *>   B V=1  "AS 1 OVERRIDE" sets V back to 1; >>IF V = 1 keeps its
      *>     branch.
      *>   C V=1  a plain "DEFINE V AS 1" (no OVERRIDE) is legal only
      *>     because the LAST
      *>          previous DEFINE of V (the override) specified the
      *>            value 1 (SR2 third
      *>          arm); the program compiles and V is still 1.
      *>   D W=5  W is the alphanumeric "X"; "AS 5 OVERRIDE" rebinds it
      *>     unconditionally,
      *>          even to a numeric literal, so >>IF W = 5 (a numeric
      *>            relation, legal
      *>          only because W is now numeric) keeps its branch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06A.
       PROCEDURE DIVISION.
       MAIN-P.
       >>DEFINE V AS 1
       >>DEFINE V AS 2 OVERRIDE
       >>IF V = 2
           DISPLAY "A V=2".
       >>ELSE
           DISPLAY "A V=1".
       >>END-IF
       >>DEFINE V AS 1 OVERRIDE
       >>IF V = 1
           DISPLAY "B V=1".
       >>ELSE
           DISPLAY "B V=2".
       >>END-IF
       >>DEFINE V AS 1
       >>IF V = 1
           DISPLAY "C V=1".
       >>ELSE
           DISPLAY "C V=OTHER".
       >>END-IF
       >>DEFINE W AS "X"
       >>DEFINE W AS 5 OVERRIDE
       >>IF W = 5
           DISPLAY "D W=5".
       >>ELSE
           DISPLAY "D W=OTHER".
       >>END-IF
           STOP RUN.
