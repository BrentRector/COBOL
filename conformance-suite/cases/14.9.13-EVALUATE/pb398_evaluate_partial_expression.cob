*>--- conformance-case
*> id: pb398_evaluate_partial_expression
*> rules: [SR-14.9.13.3-5, SR-14.9.13.3-6, SR-14.9.13.3-8,
*>    SR-14.9.13.3-10, GR-14.9.13.4-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR5 / SR7 d) / SR8 / SR6 e) + 14.9.13.4 GR4 a) 2. - partial-expression-1, the selection
      *> object whose LEFTMOST portion is elided. SR5: "A selection object is a partial-expression if the
      *> leftmost portion of the selection object is a relational operator, a class condition without the
      *> identifier, a sign condition without the identifier, or a sign condition without the arithmetic
      *> expression." SR8 gives it its meaning - the object "is treated as though it were specified as
      *> condition-2, where condition-2 is the conditional expression that results from preceding
      *> partial-expression-1 by the selection subject" - and GR4 a) 2. evaluates that expression and makes its
      *> truth value the result of the pair's analysis. kb/Work PB398: NONE of these four shapes parsed, so five
      *> rules had no code site and Table 15's Partial-expression row was a lookup no operand could reach.
      *>
      *> EXPECTED VALUES, each computed from the rewrite SR8 prescribes (never from a run):
      *>   A  WS-N=7, object `> 5`      -> `WS-N > 5`                    -> true   -> GT5
      *>   B  object `> 5 AND < 10`     -> `WS-N > 5 AND < 10`, which SR7 d) licenses ("a sequence of COBOL
      *>      words such that, were it preceded by the corresponding selection subject, a conditional expression
      *>      would result") and 8.8.4.12.4 GR1 reads as an abbreviated combined relation: 7>5 AND 7<10
      *>                                                                 -> true   -> BETWEEN
      *>   C  WS-X="123" PIC X(3), object `NUMERIC` -> `WS-X IS NUMERIC`, and 8.8.4.4.4 GR3 makes an operand of
      *>      only digits NUMERIC                                        -> true   -> NUM
      *>   D  WS-Y="AB1", object `IS NOT NUMERIC` -> `WS-Y IS NOT NUMERIC`
      *>                                                                 -> true   -> NOTNUM
      *>   E  WS-Z=-4, object `NEGATIVE` (SR5's "sign condition without the identifier") -> `WS-Z IS NEGATIVE`,
      *>      8.8.4.7.4 GR1: the value is less than zero                 -> true   -> NEG
      *>   F  object `NOT > 5` - the group's leading NOT (14.9.13.2's object figure brackets NOT over the first
      *>      five alternatives only, so `NOT >` is equally the relational operator of 8.8.4.12; both readings
      *>      give NOT(WS-N > 5), which is 7 NOT > 5)                     -> false  -> LE5-NO
      *>   G  `> 5 ALSO "123"` against `WS-N ALSO WS-X` - SR7 pairs by ordinal position, GR4 b) requires every
      *>      pair true                                                   -> true   -> PAIR
      *>   H  object `< 0 OR > 100` -> `WS-N < 0 OR > 100`                -> false  -> INRANGE
      *>   I  SR6 e) - "if the selection object is a partial expression and the selection subject is a data item
      *>      of the class boolean or numeric, the selection subject is treated as an identifier": WS-N is class
      *>      numeric, object `= 7` -> `WS-N = 7`                         -> true   -> EQ7
      *>   J  Table 15's Partial-expression row is 'Y' under the LITERAL column too: `EVALUATE 7 WHEN > 5`
      *>                                                                  -> true   -> LIT-GT5
      *>   K  and under the ARITHMETIC-EXPRESSION column: `EVALUATE WS-N + 1 WHEN > 7` -> 8 > 7
      *>                                                                  -> true   -> EXPR-GT7
      *>   L  SR6 e)'s OTHER antecedent - a BOOLEAN data item subject. BW is PIC 1 USAGE BIT holding B"1", which
      *>      SR6 d) would call boolean-expression-1; e) names the narrower case (the object is a partial
      *>      expression) and makes it identifier-1. Table 15's Partial-expression row is 'Y' under BOTH columns,
      *>      so the reclassification changes no verdict - what it fixes is WHAT THE SUBJECT IS in the spliced
      *>      condition, an identifier reference rather than an expression evaluation. The pair is what this line
      *>      pins: `BW = B"1"` over B"1"                                   -> true   -> BOOL-EQ1
      *>   M  and its negation, `NOT = B"1"` (the group's NOT over the same partial) -> false -> BOOL-EQ1
      *>   N  SR5's class shape reaching a SPECIAL-NAMES user class rather than a reserved class-name: `IS
      *>      MY-DIGIT` over CLASS MY-DIGIT IS "0" THRU "9", which 8.8.4.4.4 GR3 e) makes true when the operand
      *>      consists entirely of the class's characters - WS-X is "123"      -> true   -> CLASS-YES
      *>      (The IS is what makes this spelling unambiguous: a BARE class-name is indistinguishable from
      *>      identifier-2 in the grammar and still binds as identifier-2 - reported as its own mechanism.)
      *>
      *> EDITION: filed at 2002 because the traceability inventory records these rows 2002+, an edition edge
      *> INFERRED from the feature history and not from any edition text this repo holds (specs/ISO_COBOL.md
      *> carries only 2023, whose Annex E records no EVALUATE change). No introduction gate is fired, so the
      *> construct also parses at 85: gating on an unproven edge would REJECT LEGAL SOURCE if the inference is
      *> wrong, while not gating under-rejects at worst. Overturnable in one line (kb/Work PB398).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398PE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS MY-DIGIT IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9(3) VALUE 7.
       01 WS-X PIC X(3) VALUE "123".
       01 WS-Y PIC X(3) VALUE "AB1".
       01 WS-Z PIC S9(3) VALUE -4.
       01 BW   PIC 1 USAGE BIT VALUE B"1".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-N
               WHEN > 5          DISPLAY "A=GT5"
               WHEN OTHER        DISPLAY "A=LE5"
           END-EVALUATE
           EVALUATE WS-N
               WHEN > 5 AND < 10 DISPLAY "B=BETWEEN"
               WHEN OTHER        DISPLAY "B=OUTSIDE"
           END-EVALUATE
           EVALUATE WS-X
               WHEN NUMERIC      DISPLAY "C=NUM"
               WHEN OTHER        DISPLAY "C=NOTNUM"
           END-EVALUATE
           EVALUATE WS-Y
               WHEN IS NOT NUMERIC DISPLAY "D=NOTNUM"
               WHEN OTHER          DISPLAY "D=NUM"
           END-EVALUATE
           EVALUATE WS-Z
               WHEN NEGATIVE     DISPLAY "E=NEG"
               WHEN OTHER        DISPLAY "E=NOTNEG"
           END-EVALUATE
           EVALUATE WS-N
               WHEN NOT > 5      DISPLAY "F=LE5"
               WHEN OTHER        DISPLAY "F=LE5-NO"
           END-EVALUATE
           EVALUATE WS-N ALSO WS-X
               WHEN > 5 ALSO "123" DISPLAY "G=PAIR"
               WHEN OTHER          DISPLAY "G=NOPAIR"
           END-EVALUATE
           EVALUATE WS-N
               WHEN < 0 OR > 100 DISPLAY "H=OUTRANGE"
               WHEN OTHER        DISPLAY "H=INRANGE"
           END-EVALUATE
           EVALUATE WS-N
               WHEN = 7          DISPLAY "I=EQ7"
               WHEN OTHER        DISPLAY "I=NE7"
           END-EVALUATE
           EVALUATE 7
               WHEN > 5          DISPLAY "J=LIT-GT5"
               WHEN OTHER        DISPLAY "J=LIT-LE5"
           END-EVALUATE
           EVALUATE WS-N + 1
               WHEN > 7          DISPLAY "K=EXPR-GT7"
               WHEN OTHER        DISPLAY "K=EXPR-LE7"
           END-EVALUATE
           EVALUATE BW
               WHEN = B"1"       DISPLAY "L=BOOL-EQ1"
               WHEN OTHER        DISPLAY "L=BOOL-NE1"
           END-EVALUATE
           EVALUATE BW
               WHEN NOT = B"1"   DISPLAY "M=BOOL-NE1"
               WHEN OTHER        DISPLAY "M=BOOL-EQ1"
           END-EVALUATE
           EVALUATE WS-X
               WHEN IS MY-DIGIT  DISPLAY "N=CLASS-YES"
               WHEN OTHER        DISPLAY "N=CLASS-NO"
           END-EVALUATE
           STOP RUN.
