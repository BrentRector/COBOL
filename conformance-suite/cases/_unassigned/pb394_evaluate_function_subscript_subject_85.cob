      *>--- conformance-case
      *> id: pb394_evaluate_function_subscript_subject_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB394 - the POSITIVE replacement for the retired negative case
      *> pb17-function-subscript-evaluate-subject. That case pinned COBOLNET1509, the narrowed
      *> per-evaluation residue stage, on CONFORMING SOURCE: a function-bearing subscript in an
      *> EVALUATE selection SUBJECT was refused because this backend re-BOUND the subject once per
      *> selection pair, so the function-identifier's statement-scoped activation would have run once
      *> per WHEN. The subject is now bound ONCE for the statement (ISO 14.9.13.4 GR3 - "At the
      *> beginning of the execution of the EVALUATE statement, each selection subject is evaluated and
      *> assigned a value, a range of values, or a truth value"), which makes the hoist EXACT and the
      *> stage's premise false, so the source compiles and runs.
      *>
      *> The construct is legal by 8.4.3.1.2 Format 1 (a function-identifier IS an identifier), 15.4 (its
      *> returned value is a temporary elementary data item), 15.2 (an integer function is class and
      *> category numeric), 8.8.1.1 (an arithmetic expression may be an identifier referencing a numeric
      *> data item) and 8.4.2.3.2 with 8.4.2.3.4 GR1 b) (arithmetic-expression-1 is a subscript).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC: FUNCTION INTEGER(1) is 1 (15.49.4 r1 - the greatest
      *> integer <= argument-1), so the subject is W-E(1), whose content is 1, and 14.9.13.4 GR4 a) 6.
      *> makes the first WHEN's pair `subject = 1` true => ONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB394FNSUB85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO W-E (1)
           MOVE 2 TO W-E (2)
           EVALUATE W-E (FUNCTION INTEGER(1))
               WHEN 1 DISPLAY "ONE"
               WHEN 2 DISPLAY "TWO"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE
           STOP RUN.
