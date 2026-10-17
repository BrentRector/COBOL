*>--- conformance-case
*> id: pb47-evaluate-table15-invalid-combination
*> rules: [SR-14.9.13.3-7, SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR10: "The permissible combinations of selection subject and selection object
      *> operands are indicated in Table 15, Combination of operands in the EVALUATE statement", and
      *> Table 15's caption is explicit — "a space indicates an invalid combination". Against a
      *> TRUE-or-FALSE selection subject the ONLY permissible objects are a condition, TRUE/FALSE, or ANY;
      *> the [NOT] identifier / literal / arithmetic-expression / range-expression rows are all blank.
      *>
      *> A SYNTAX RULE VIOLATION IS A COMPILE-TIME DIAGNOSTIC. This used to compile CLEAN and throw
      *> NotImplementedCobolFeatureException at RUN TIME (fix-queue PB47), whose text — "a COBOL feature
      *> that is not yet implemented was reached at run time" — was wrong twice over: the feature IS
      *> implemented, and the source is inadmissible. It was also COVERAGE-SHAPED: an invalid pairing on a
      *> WHEN branch that never executed never reported at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB47T15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Z PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
               WHEN W-Z
                   DISPLAY "MATCHED"
               WHEN OTHER
                   DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
