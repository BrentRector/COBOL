*>--- conformance-case
*> id: pb1066_gr14_implicit_pop_all_define
*> rules: [GR-14.9.28.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1066 - the implicit PUSH ALL / POP ALL that 14.9.28.4 GR14
      *> places around an exception-checking PERFORM's handlers also saves
      *> and restores the compilation-variable table. Before PB1066 the two
      *> implicit ops reached only the directive states held after the
      *> parse, so a >>DEFINE written in a WHEN phrase outlived END-PERFORM
      *> (L1 printed ZZ-DEFINED, L2 YY-UNDEFINED).
      *>
      *> THE RULES:
      *>   14.9.28.4 GR14 - "An implicit PUSH ALL followed by TURN OFF ALL is
      *>     assumed at the end of imperative-statement-1. Immediately
      *>     preceding the END PERFORM phrase, there is an implicit POP ALL"
      *>   7.3.22.4 GR2 - "If ALL is specified, the state of all of the
      *>     directives other than EVALUATE, IF, PAGE, POP, or PUSH are saved."
      *>   7.3.22.4 GR3 - "... such as a DEFINE directive, all instances of
      *>     that directive are pushed."
      *>   7.3.20.4 GR3 - "... all instances of that directive that were
      *>     pushed and not popped are restored."
      *>   7.3.8.4.4 GR1 - "A defined condition using the IS DEFINED syntax
      *>     evaluates TRUE if compilation-variable-name-1 is currently
      *>     defined."
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES (each handler runs: the
      *> STRING into a 3-character item overflows):
      *>   H1           the first handler runs.
      *>   L1 ZZ-UNDEFINED  >>DEFINE ZZ is written in a WHEN phrase, after
      *>                the implicit PUSH ALL; the implicit POP ALL before
      *>                END-PERFORM restores the table in which ZZ is not
      *>                defined.
      *>   H2           the second handler runs.
      *>   L2 YY-DEFINED    >>DEFINE YY OFF in a FINALLY phrase is undone the
      *>                same way: YY, defined before the PERFORM, is defined
      *>                again after END-PERFORM.
      *>   L3 WW-DEFINED    CONTROL: >>DEFINE WW written in imperative-
      *>                statement-1 is BEFORE the PUSH ALL, so it is part of
      *>                the saved table and survives the POP ALL.
      *> Directives sit at COLUMN 8 (column 7 is the fixed-form indicator area).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1066G14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       PROCEDURE DIVISION.
       M-1.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>DEFINE ZZ AS 1
               DISPLAY "H1"
           END-PERFORM
       >>IF ZZ IS DEFINED
           DISPLAY "L1 ZZ-DEFINED"
       >>ELSE
           DISPLAY "L1 ZZ-UNDEFINED"
       >>END-IF

       >>DEFINE YY AS 1
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               DISPLAY "H2"
           FINALLY
       >>DEFINE YY OFF
               CONTINUE
           END-PERFORM
       >>IF YY IS DEFINED
           DISPLAY "L2 YY-DEFINED"
       >>ELSE
           DISPLAY "L2 YY-UNDEFINED"
       >>END-IF

           PERFORM
       >>DEFINE WW AS 1
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
               CONTINUE
           END-PERFORM
       >>IF WW IS DEFINED
           DISPLAY "L3 WW-DEFINED"
       >>ELSE
           DISPLAY "L3 WW-UNDEFINED"
       >>END-IF
           STOP RUN.
