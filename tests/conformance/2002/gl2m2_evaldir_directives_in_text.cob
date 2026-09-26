      *> ISO §7.3.13.3 SR8 — EVALUATE directive text-1 / text-2 holding
      *>   compiler directives and spanning several lines
      *> Rule: "Text-1 and text-2 may be any kind of source lines,
      *>   including compiler directives. Text-1 and text-2 may consist
      *>   of multiple lines."
      *>   cite.py --check 7.3.13.3 "Text-1 and text-2 may be any kind
      *>     of source lines, including compiler directives. ..." -> OK
      *>     §7.3.13.3 8)  (Syntax rules)
      *> Selection rules (each -> OK under cite.py --check):
      *>   §7.3.13.4 GR4 (trailing paragraph; cite.py labels it 4) b))
      *>     "If a WHEN phrase evaluates to TRUE, all lines of text-1
      *>     associated with that WHEN phrase are included in the
      *>     resultant text." - and all other text-1 / text-2 omitted.
      *>   §7.3.13.4 GR4 b) "If the THROUGH phrase is specified, a TRUE
      *>     result is returned if the selection subject lies in the
      *>     inclusive range ..."
      *>   §7.3.13.4 GR8 "For each WHEN phrase in turn, the
      *>     constant-conditional-expression is evaluated in accordance
      *>     with 7.3.8".
      *>   §7.3.13.4 GR9 "If no WHEN phrase evaluates to TRUE,
      *>     all lines of text-2 associated with the WHEN OTHER phrase,
      *>     if specified, are included in the resultant text. All lines
      *>     of text-1 associated with other WHEN phrases are omitted
      *>     from the resultant text."
      *>   §7.3.13.3 SR9 "A nested EVALUATE directive specified in
      *>     text-1 or in text-2 is considered a new EVALUATE directive."
      *>     - so the inner >>WHEN OTHER / >>END-EVALUATE close the INNER
      *>     directive, not the outer one.
      *>   §7.2.1 Step 2: "the expanded compilation group is read and
      *>     the following compiler directives and substitutions are
      *>     processed in the order encountered" - a >>DEFINE inside
      *>     OMITTED text is not in the group read, so it has no effect;
      *>     one inside INCLUDED text does.
      *>   §7.3.8.4.4 GR2 "A defined condition using the IS NOT DEFINED
      *>     syntax evaluates TRUE if compilation-variable-name-1 is not
      *>     currently defined."
      *> Every nested directive below is complete and well formed
      *>   (§7.2.1: compiler directives shall be syntactically correct in
      *>   the initial source text, false paths included).
      *> DERIVATION (L1G2SW = 2):
      *> EVALUATE #1, format 1, subject 2: WHEN 1 is FALSE, WHEN 2 is
      *>   TRUE. Its text-1 is 14 lines: a DISPLAY, a >>DEFINE, a nested
      *>   >>IF/>>ELSE/>>END-IF and a nested format-2 >>EVALUATE whose
      *>   >>WHEN OTHER text-2 is a DISPLAY written over two lines. All
      *>   included (GR4):
      *>     A1 W2-FIRST            the first line of text-1
      *>     A2 NESTED-IF           L1G2IN = 5 is TRUE: the INCLUDED
      *>                            >>DEFINE took effect
      *>     A3 NESTED-EVAL-OTHER   L1G2IN > 9 is FALSE, so the inner
      *>                            WHEN OTHER text-2 (two lines) is
      *>                            included (GR9)
      *>   WHEN 1's text-1 (BAD-W1, >>DEFINE L1G2OUT AS 1) and WHEN
      *>   OTHER's text-2 (BAD-OTHER, >>DEFINE L1G2OUT AS 2) are omitted.
      *> EVALUATE #2, format 2: L1G2SW = 1 is FALSE, so WHEN OTHER's
      *>   text-2 is included (GR9): a DISPLAY and a nested format-1
      *>   >>EVALUATE L1G2IN with a THRU range and a WHEN OTHER:
      *>     B1 OTHER-FIRST
      *>     B2 OTHER-NESTED        5 is not in 1 THRU 4 (GR4 b)), and
      *>                            WHEN 5 is TRUE (GR4 a)); its
      *>                            WHEN OTHER text-2 (BAD-B2-OTHER)
      *>                            is omitted (GR4)
      *>   The omitted text-1 held >>DEFINE L1G2IN AS 9 OVERRIDE.
      *> After both:
      *>     OUT-UNDEFINED          both >>DEFINE L1G2OUT lines were in
      *>                            omitted text
      *>     IN-IS-5                the omitted OVERRIDE took no effect
      *> 2002 dir: the >> compiler-directive facility is refused below
      *>   2002 (COBOLNET0900).
      *> Editions: Annex E.2 8) changed the end-of-directive omission
      *>   rules (GR6 format 1, GR10 format 2) so that the whole
      *>   condition is true only when both constituent conditions
      *>   are true; before 2023 it held when EITHER held (no WHEN
      *>   TRUE, or no WHEN OTHER encountered).
      *>   cite.py --check E.2 "have been changed to ensure that the
      *>     whole condition is now true only when both of the
      *>     constituent conditions are true" -> OK  §E.2 8)
      *>   EVERY EVALUATE here carries a >>WHEN OTHER, so under either
      *>   wording GR6/GR10 omit nothing GR4/GR9 would include and
      *>   never decide an output line: the .out is the same at
      *>   2002, 2014 and 2023.
       >>DEFINE L1G2SW AS 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1G2M03.
       PROCEDURE DIVISION.
       MAIN-P.
       >>EVALUATE L1G2SW
       >>WHEN 1
           DISPLAY "BAD-W1"
       >>DEFINE L1G2OUT AS 1
       >>WHEN 2
           DISPLAY "A1 W2-FIRST"
       >>DEFINE L1G2IN AS 5
       >>IF L1G2IN = 5
           DISPLAY "A2 NESTED-IF"
       >>ELSE
           DISPLAY "BAD-A2"
       >>END-IF
       >>EVALUATE TRUE
       >>WHEN L1G2IN > 9
           DISPLAY "BAD-A3"
       >>WHEN OTHER
           DISPLAY "A3 "
               "NESTED-EVAL-OTHER"
       >>END-EVALUATE
       >>WHEN OTHER
           DISPLAY "BAD-OTHER"
       >>DEFINE L1G2OUT AS 2
       >>END-EVALUATE
       >>EVALUATE TRUE
       >>WHEN L1G2SW = 1
       >>DEFINE L1G2IN AS 9 OVERRIDE
           DISPLAY "BAD-C"
       >>WHEN OTHER
           DISPLAY "B1 OTHER-FIRST"
       >>EVALUATE L1G2IN
       >>WHEN 1 THRU 4
           DISPLAY "BAD-B2"
       >>WHEN 5
           DISPLAY "B2 OTHER-NESTED"
       >>WHEN OTHER
           DISPLAY "BAD-B2-OTHER"
       >>END-EVALUATE
       >>END-EVALUATE
       >>IF L1G2OUT IS NOT DEFINED
           DISPLAY "OUT-UNDEFINED"
       >>END-IF
       >>IF L1G2IN = 5
           DISPLAY "IN-IS-5"
       >>END-IF
           STOP RUN.
