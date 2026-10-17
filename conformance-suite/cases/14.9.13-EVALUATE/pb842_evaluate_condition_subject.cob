      *>--- conformance-case
      *> id: pb842_evaluate_condition_subject
      *> rules: [GR-14.9.13.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB842 - AN EVALUATE SELECTION SUBJECT MAY BE ANY CONDITION (condition-1), NOT ONLY A CLASS TEST.
      *> Every expected line below is derived from the rule text, never from a run.
      *>
      *> ISO 14.9.13.2 - the selection-subject brace prints condition-1 beside identifier-1, literal-1,
      *>   arithmetic-expression-1, boolean-expression-1, TRUE and FALSE.
      *> ISO 14.9.13.4 GR3 - "At the beginning of the execution of the EVALUATE statement, each selection subject
      *>   is evaluated and assigned a value, a range of values, or a truth value", and GR3 e) - "Any selection
      *>   subject specified by condition-1 is assigned a truth value according to the rules for evaluating
      *>   conditional expressions".
      *> ISO 14.9.13.4 GR4 a) 4. - "If the selection object is either TRUE or FALSE, the selection subject is
      *>   condition-1. If the truth value of the selection subject and selection object match, the result of the
      *>   analysis is true."
      *> ISO 14.9.13.4 GR4 a) 3. - the same matching sentence for a condition-2 object (Table 15's
      *>   Condition x Condition cell).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC (W-N = 3, W-S = -2, W-NUM's content is "12A"):
      *> REL=T     W-N > 1 is true; the first arm's object FALSE does not match, the second arm's TRUE does.
      *> REL2=F    W-N < 1 is false; the FALSE arm matches.
      *> SIGN=T    W-S IS NEGATIVE is true (-2 < 0).
      *> CLS=F     W-NUM NUMERIC over the content "12A" is false (8.8.4.4) - the truth value is taken from the
      *>           item's CHARACTER CONTENT, never from a normalized numeric copy of it.
      *> AND=F     W-N > 1 AND W-N < 3 is false (3 < 3 is false).
      *> NOTP=F    NOT (W-N = 3) is false.
      *> ABBR=T    W-N = 1 OR 3 is the abbreviated combined relation W-N = 1 OR W-N = 3 (8.8.4.12), true.
      *> CC=MATCH  subject W-N = 3 is true, object W-S < 0 is true: the truth values match (GR4 a) 3.).
      *> ALSO=A2   subject 1 (W-N > 2) is true, subject 2 is the value -2: arm 1 fails on its first pair
      *>           (FALSE vs true), arm 2 matches both pairs (TRUE vs true, -2 = -2).
      *> ANY=Y     an ANY object matches whatever the subject's truth value is (GR4 a) 1.).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB842CSUBJ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 3.
       01 W-S PIC S9 VALUE -2.
       01 W-AN PIC X(3) VALUE "12A".
       01 W-NUM REDEFINES W-AN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE W-N > 1
               WHEN FALSE DISPLAY "REL=F"
               WHEN TRUE DISPLAY "REL=T"
           END-EVALUATE.
           EVALUATE W-N < 1
               WHEN TRUE DISPLAY "REL2=T"
               WHEN FALSE DISPLAY "REL2=F"
           END-EVALUATE.
           EVALUATE W-S IS NEGATIVE
               WHEN FALSE DISPLAY "SIGN=F"
               WHEN TRUE DISPLAY "SIGN=T"
           END-EVALUATE.
           EVALUATE W-NUM NUMERIC
               WHEN TRUE DISPLAY "CLS=T"
               WHEN FALSE DISPLAY "CLS=F"
           END-EVALUATE.
           EVALUATE W-N > 1 AND W-N < 3
               WHEN TRUE DISPLAY "AND=T"
               WHEN FALSE DISPLAY "AND=F"
           END-EVALUATE.
           EVALUATE NOT (W-N = 3)
               WHEN TRUE DISPLAY "NOTP=T"
               WHEN FALSE DISPLAY "NOTP=F"
           END-EVALUATE.
           EVALUATE W-N = 1 OR 3
               WHEN FALSE DISPLAY "ABBR=F"
               WHEN TRUE DISPLAY "ABBR=T"
           END-EVALUATE.
           EVALUATE W-N = 3
               WHEN W-S < 0 DISPLAY "CC=MATCH"
               WHEN OTHER DISPLAY "CC=NOMATCH"
           END-EVALUATE.
           EVALUATE W-N > 2 ALSO W-S
               WHEN FALSE ALSO ANY DISPLAY "ALSO=A1"
               WHEN TRUE ALSO -2 DISPLAY "ALSO=A2"
               WHEN OTHER DISPLAY "ALSO=A3"
           END-EVALUATE.
           EVALUATE W-N > 5
               WHEN ANY DISPLAY "ANY=Y"
           END-EVALUATE.
           STOP RUN.
