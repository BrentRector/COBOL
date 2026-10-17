*>--- conformance-case
*> id: pb46-invoke-by-content-boolean-numeric-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.8.2.3.3 rule 2d: a BY CONTENT argument whose formal parameter is
      *> not numeric, not an index item and not ANY LENGTH conforms by "the same
      *> [rules] as for a MOVE statement with the argument as the sending operand
      *> and the corresponding formal parameter as the receiving operand". For a
      *> boolean-expression-1 argument that is 14.9.25.3 Table 16's BOOLEAN row,
      *> whose NUMERIC and NUMERIC-EDITED columns are both "No".
      *>
      *> This is the conformance half of the PB46 boolean channel, and it is a
      *> JUDGEMENT rather than a parse failure: 14.9.23.2's BY CONTENT branch
      *> admits boolean-expression-1, so the operand is legal syntax and the
      *> pairing is what fails. Before the channel existed the same source was
      *> refused for the wrong reason entirely — the boolean operator was not
      *> consumed at all, so `B-AND` landed in a name slot (COBOLNET0901) and the
      *> statement reported "3 USING argument(s) for 1 formal parameter(s)", an
      *> arity diagnostic about a rule the program does not violate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46NEGBOOL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CPB46NB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CPB46NB.
       01 B1 PIC 1(4) USAGE BIT VALUE B"1100".
       01 B2 PIC 1(4) USAGE BIT VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CPB46NB "NEW" RETURNING O.
           INVOKE O "TAKEN" USING BY CONTENT B1 B-AND B2.
           STOP RUN.
       END PROGRAM PB46NEGBOOL.

       IDENTIFICATION DIVISION.
       CLASS-ID. CPB46NB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC S9(4).
       PROCEDURE DIVISION USING P.
       M.
           DISPLAY "N=[" P "]".
       END METHOD TAKEN.
       END OBJECT.
       END CLASS CPB46NB.
