*>--- conformance-case
*> id: pb400_evaluate_boolean_sr6_pair
*> rules: [SR-14.9.13.3-6, GR-14.9.13.4-3, GR-14.9.13.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR6 is a MIRRORED PAIR, and this exercises both arms plus the
      *> two identity arms beside them:
      *>   a) "If the selection subject is TRUE or FALSE and the selection object is a
      *>      boolean expression that results in one boolean character, the selection
      *>      object is treated as a boolean condition and therefore condition-2."
      *>   b) "If the selection object is TRUE or FALSE and the selection subject is a
      *>      boolean expression that results in one boolean character, the selection
      *>      subject is treated as a boolean condition and therefore condition-1."
      *>   c)/d) against a counterpart that is NOT TRUE or FALSE the boolean operand
      *>      stays boolean-expression-2 / boolean-expression-1, and Table 15 marks
      *>      boolean-expression × boolean-expression permissible; §14.9.13.4 GR4 a) 6.
      *>      then lowers the pair to `selection-subject = selection-object`.
      *> Arm b) did not exist: the subject stayed a value, the TRUE object bound as a
      *> standalone condition, and the emitted program died at run time on conforming
      *> source (kb/Work PB400).  §8.8.2 makes "an identifier referencing a boolean data
      *> item" and "a boolean literal" boolean expressions, and its rules 9/10 give each
      *> its result length — the length SR6 turns on.
      *> §8.8.4.3.4 GR1 fixes the truth value: "Boolean-expression-1 evaluates true if
      *> the result of the expression is 1 and evaluates false if the result is 0."
      *> ⛔ THE OPERATOR SHAPE OF boolean-expression-2, added 2026-09-22 with kb/Work PB394's landing:
      *> the two SR6 a) legs above use the DEGENERATE forms (a bare identifier, a bare literal), and the
      *> traceability row for §14.9.13.4 GR4 recorded exactly that as a reservation - boolean OPERATORS in
      *> a condition context had been staged grammar residue, so no witness reached the alternative the
      *> printed selection-object figure names boolean-expression-2.  They do reach it now, and these two
      *> legs are what says so.  §8.7.2 lists B-AND ("AND operation (boolean conjunction)") and B-OR among
      *> the boolean operators, §8.8.2 rule 6 its formation table, and §8.8.4.3.4 GR1 the truth value
      *> of the one-character result:
      *>   OPOR   W-B1 B-OR W-B0  = B"1" -> one boolean character -> SR6 a) condition-2 -> true  -> Y
      *>   OPAND  W-B1 B-AND W-B0 = B"0" -> one boolean character -> SR6 a) condition-2 -> false -> N
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB400BOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-B1 PIC 1 USAGE BIT VALUE B"1".
       01 W-B0 PIC 1 USAGE BIT VALUE B"0".
       01 W-B2 PIC 1(2) USAGE BIT VALUE B"01".
       PROCEDURE DIVISION.
       MAIN-P.
      *> SR6 b) — a one-boolean-character SUBJECT against TRUE/FALSE objects.
           EVALUATE W-B1
               WHEN TRUE
                   DISPLAY "SR6B-1=T"
               WHEN FALSE
                   DISPLAY "SR6B-1=F"
           END-EVALUATE.
           EVALUATE W-B0
               WHEN TRUE
                   DISPLAY "SR6B-0=T"
               WHEN FALSE
                   DISPLAY "SR6B-0=F"
           END-EVALUATE.
      *> SR6 a) — the SAME items in the transposed cell.
           EVALUATE TRUE
               WHEN W-B1
                   DISPLAY "SR6A-1=T"
               WHEN OTHER
                   DISPLAY "SR6A-1=F"
           END-EVALUATE.
           EVALUATE TRUE
               WHEN W-B0
                   DISPLAY "SR6A-0=T"
               WHEN OTHER
                   DISPLAY "SR6A-0=F"
           END-EVALUATE.
      *> A boolean LITERAL is a boolean expression too (§8.8.2), and its own length is
      *> one boolean character, so SR6 applies to it in both positions as well.
           EVALUATE B"1"
               WHEN TRUE
                   DISPLAY "LIT-1=T"
               WHEN FALSE
                   DISPLAY "LIT-1=F"
           END-EVALUATE.
           EVALUATE B"0"
               WHEN TRUE
                   DISPLAY "LIT-0=T"
               WHEN FALSE
                   DISPLAY "LIT-0=F"
           END-EVALUATE.
      *> SR6 c)/d) — neither side TRUE or FALSE, so BOTH stay boolean expressions and
      *> GR4 a) 6. compares them for equality.  A TWO-position operand is legal here
      *> (Table 15 boolean-expression × boolean-expression = 'Y'); it is only against
      *> TRUE or FALSE that SR6's one-character test excludes it.
           EVALUATE W-B2
               WHEN B"01"
                   DISPLAY "B2-EQ=Y"
               WHEN OTHER
                   DISPLAY "B2-EQ=N"
           END-EVALUATE.
           EVALUATE W-B2
               WHEN B"10"
                   DISPLAY "B2-NE=N"
               WHEN OTHER
                   DISPLAY "B2-NE=Y"
           END-EVALUATE.
           EVALUATE W-B1
               WHEN W-B0
                   DISPLAY "B1B0=EQ"
               WHEN OTHER
                   DISPLAY "B1B0=NE"
           END-EVALUATE.
      *> SR6 a) over a boolean expression built with an OPERATOR - the alternative the
      *> figure calls boolean-expression-2, beyond its degenerate identifier/literal forms.
           EVALUATE TRUE
               WHEN W-B1 B-OR W-B0
                   DISPLAY "OPOR=Y"
               WHEN OTHER
                   DISPLAY "OPOR=N"
           END-EVALUATE.
           EVALUATE TRUE
               WHEN W-B1 B-AND W-B0
                   DISPLAY "OPAND=Y"
               WHEN OTHER
                   DISPLAY "OPAND=N"
           END-EVALUATE.
           STOP RUN.
