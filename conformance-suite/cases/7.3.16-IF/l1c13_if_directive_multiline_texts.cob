      *>--- conformance-case
      *> id: l1c13_if_directive_multiline_texts
      *> rules: [SR-7.3.16.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.16.3 SR6 — multi-line text-1/text-2 holding directives
      *> "Text-1 and text-2 may be any kind of source lines, including
      *> compiler directives. Text-1 and text-2 may consist of multiple
      *> lines."
      *>   cite.py: OK  §7.3.16.3 6)  (Syntax rules)
      *> Selection: §7.3.16.4 GR2 "all lines of text-1 are included in
      *> the resultant text and all lines of text-2 are omitted" (TRUE)
      *>   cite.py: OK  §7.3.16.4 2)  (General rules)
      *> and GR3 "all lines of text-2 are included in the resultant
      *> text and all lines of text-1 are omitted from the resultant
      *> text" (FALSE).
      *>   cite.py: OK  §7.3.16.4 3)  (General rules)
      *> DEFINE/IF/EVALUATE are processed only as encountered in the
      *> group being conditionally processed (§7.2.1 Step 2: "the
      *> expanded compilation group is read and the following compiler
      *> directives and substitutions are processed in the order
      *> encountered"), so a >>DEFINE line inside OMITTED text has no
      *> effect and one inside INCLUDED text does.
      *>   cite.py: OK  §7.2.1   (General)
      *> §7.3.8.4.4 GR2 "A defined condition using the IS NOT DEFINED
      *> syntax evaluates TRUE if compilation-variable-name-1 is not
      *> currently defined."
      *>   cite.py: OK  §7.3.8.4.4 2)  (General rule)
      *>
      *> DERIVATION (SW = 1).
      *> IF #1 (SW = 1, TRUE): text-1 = eleven lines holding a MOVE, a
      *>   DISPLAY, a >>DEFINE INNER AS 5, a nested >>IF/>>ELSE/>>END-IF
      *>   and a nested >>EVALUATE whose WHEN body is a DISPLAY written
      *>   over two lines.  All included (GR2):
      *>     A1 T1          (MOVE then DISPLAY; W is X(8), trailing
      *>                     spaces ignored)
      *>     A2 NESTED-IF   (INNER = 5 is TRUE — the included DEFINE
      *>                     took effect)
      *>     A3 NESTED-EVAL (EVALUATE INNER, WHEN 5)
      *>   text-2 (DISPLAY "BAD-B", a >>DEFINE OUTER, a nested IF) is
      *>   omitted: no BAD-B line, OUTER stays undefined.
      *> IF #2 (SW = 2, FALSE): text-1 (a >>DEFINE INNER AS 9 OVERRIDE,
      *>   DISPLAYs) omitted; text-2, four lines with a nested >>IF and
      *>   a two-line DISPLAY, included (GR3):
      *>     B1 TEXT2-FIRST
      *>     B2 TEXT2-NESTED
      *> After both: OUTER IS NOT DEFINED is TRUE -> OUTER-UNDEFINED;
      *>   INNER is still 5 (the omitted OVERRIDE took no effect) ->
      *>   INNER-IS-5.
       >>DEFINE SW AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C13B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
       >>IF SW = 1
           MOVE "T1" TO W
           DISPLAY "A1 " W
       >>DEFINE INNER AS 5
       >>IF INNER = 5
           DISPLAY "A2 NESTED-IF"
       >>ELSE
           DISPLAY "BAD-A2"
       >>END-IF
       >>EVALUATE INNER
       >>WHEN 5
           DISPLAY "A3 "
               "NESTED-EVAL"
       >>WHEN OTHER
           DISPLAY "BAD-A3"
       >>END-EVALUATE
       >>ELSE
           DISPLAY "BAD-B"
       >>DEFINE OUTER AS 1
       >>IF SW = 1
           DISPLAY "BAD-B-NESTED"
       >>END-IF
       >>END-IF
       >>IF SW = 2
       >>DEFINE INNER AS 9 OVERRIDE
           DISPLAY "BAD-C"
           DISPLAY "BAD-C2"
       >>ELSE
           DISPLAY "B1 TEXT2-FIRST"
       >>IF INNER = 5
           DISPLAY "B2 "
               "TEXT2-NESTED"
       >>END-IF
       >>END-IF
       >>IF OUTER IS NOT DEFINED
           DISPLAY "OUTER-UNDEFINED"
       >>END-IF
       >>IF INNER = 5
           DISPLAY "INNER-IS-5"
       >>END-IF
           STOP RUN.
