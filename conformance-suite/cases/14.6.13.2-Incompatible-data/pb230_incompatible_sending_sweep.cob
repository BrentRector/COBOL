*>--- conformance-case
*> id: pb230_incompatible_sending_sweep
*> rules: [GR-14.6.13.2-2, GR-14.9.2.4-6, GR-14.9.8.4-1, GR-14.9.12.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> PB230 - ISO 14.6.13.2 rule 2: "When the content of a numeric sending item that is not described with a
      *> standard floating-point usage is referenced during the execution of a statement and the content of that
      *> sending operand would evaluate to false in a numeric class condition, the result of the reference is
      *> undefined and an EC-DATA-INCOMPATIBLE exception condition is set to exist, except in the following
      *> circumstances: - a sending item is referenced in a class condition, or - a sending item is processed in a
      *> VALIDATE statement."  The rule is NOT statement-specific (rule 4 is - it names a de-editing MOVE), so it
      *> reaches every reference: each arithmetic statement's operands (14.9.2.4 GR6 / 14.9.44.4 GR4 / 14.9.26.4 /
      *> 14.9.12.4 GR3 / 14.9.8.4 GR1a all point at 14.6.13.2), a relation condition, a sign condition, DISPLAY,
      *> and a numeric MOVE (14.9.25.4 GR6 d)1 restates it verbatim).  N is a numeric-DISPLAY leaf under a group
      *> REDEFINED as PIC X(3), which is how content that is not digits reaches a numeric item at all.
      *> THE TWO EXEMPTIONS ARE ASYMMETRIC WITH RULE 3's FOUR, and both halves are pinned here: a CLASS condition
      *> (L9) is exempt and answers false, while a SIGN condition (L10) is NOT - rule 3 exempts one for a float
      *> operand (8.8.4.7.4 GR2 gives a NaN sign test a defined answer) and rule 2 grants no such dash.
      *> Every raise is fatal (Table 13), so the USE declarative's RESUME AT NEXT STATEMENT abandons the rest of
      *> the raising statement: an ADD leaves N unchanged, an IF prints neither branch, a DISPLAY prints nothing.
      *> L12 is the table SORT, and it pins something a raise site alone does not give you.  14.9.40.4 GR8 makes a
      *> key comparison follow the relation-condition rules, so referencing an invalid numeric key is rule 2 like
      *> any other reference - but the comparison runs inside a COMPARER the framework hands to its array sort,
      *> and that sort CATCHES anything a comparer throws and re-throws it as a .NET InvalidOperationException on
      *> the assumption that a throwing comparer is an inconsistent one.  A COBOL comparer is not.  Wrapped, the
      *> fatal never matched the statement guard and the run unit died unhandled instead of running this very
      *> declarative; CobolTable.Sorted undoes the wrapper at the one place a COBOL comparer meets the framework.
      *> The keys are planted by a whole-group MOVE, which 14.9.25.4 GR4 performs "without consideration for the
      *> individual elementary items" - the ordinary way content that is not digits reaches a numeric item.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB230SWEEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9(3).
       01 XR REDEFINES G PIC X(3).
       01 R PIC 9(5).
       01 SRC PIC X(9) VALUE "003AB1002".
       01 TBL.
          05 ELT OCCURS 3 TIMES ASCENDING KEY IS KY INDEXED BY IX.
             10 KY PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-DATA-INCOMPATIBLE.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE 0 TO R.
           MOVE "AB1" TO XR.
           DISPLAY "L1 ADD".
           ADD 1 TO N.
           DISPLAY "L2 COMPUTE".
           COMPUTE R = N + 1.
           DISPLAY "   R=" R.
           DISPLAY "L3 SUBTRACT".
           SUBTRACT 1 FROM N.
           DISPLAY "L4 MULTIPLY".
           MULTIPLY N BY 2 GIVING R.
           DISPLAY "   R=" R.
           DISPLAY "L5 DIVIDE".
           DIVIDE N INTO 100 GIVING R.
           DISPLAY "   R=" R.
           DISPLAY "L6 relation".
           IF N > 5
               DISPLAY "   gt"
           ELSE
               DISPLAY "   ngt"
           END-IF.
           DISPLAY "L7 DISPLAY".
           DISPLAY "   [" N "]".
           DISPLAY "L8 MOVE".
           MOVE N TO R.
           DISPLAY "   R=" R.
           DISPLAY "L9 class condition (exempt)".
           IF N IS NUMERIC
               DISPLAY "   numeric"
           ELSE
               DISPLAY "   not numeric"
           END-IF.
           DISPLAY "L10 sign condition (not exempt)".
           IF N IS POSITIVE
               DISPLAY "   pos"
           ELSE
               DISPLAY "   npos"
           END-IF.
           DISPLAY "L11 compatible content".
           MOVE 42 TO N.
           ADD 1 TO N.
           DISPLAY "   [" N "]".
           DISPLAY "L12 table SORT key (through the framework comparer)".
           MOVE SRC TO TBL.
           SORT ELT ON ASCENDING KEY KY.
           DISPLAY "   sorted".
           STOP RUN.
