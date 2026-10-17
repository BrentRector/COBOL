      *>--- conformance-case
      *> id: l1c33_turn_within_statement
      *> rules: [GR-7.3.25.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.25.4 GR5 — TURN inside a statement: not its phrases
      *> "If specified within a statement, the TURN directive does not
      *> apply to any phrase of that statement. That TURN directive
      *> applies to any succeeding statement in the sequence of source
      *> lines, whether or not that succeeding statement is within the
      *> scope of the statement in which the TURN directive is
      *> specified."
      *>   cite.py: OK  §7.3.25.4 5)  (General rules)
      *> Supporting: §14.7.5 4) "if the result of the arithmetic
      *> statement is a value further from zero than permitted for the
      *> associated resultant data item, the EC-SIZE-TRUNCATION
      *> exception condition is set to exist" (cite.py: OK  §14.7.5 4));
      *> §14.6.13.1.1 a raised (checked) exception sets the last
      *> exception status (cite.py: OK  §14.6.13.1.1); §15.33.3 1)
      *> EXCEPTION-STATUS is spaces while no exception has been raised
      *> (cite.py: OK  §15.33.3 1)).
      *> DERIVATION. A and B are PIC 9 VALUE 9, so every ADD 1 is a size
      *> error (ON SIZE ERROR runs whether or not checking is on).
      *>   The outer ADD 1 TO A starts BEFORE the >>TURN, which sits
      *>   within that ADD statement, so the TURN does not apply to it
      *>   or to its ON SIZE ERROR phrase: no exception is raised,
      *>   the last exception status is still "none"
      *>   -> "P1 [<31 spaces>]".
      *>   The nested ADD 1 TO B inside the phrase is a SUCCEEDING
      *>   statement in source order, within the outer ADD's scope, so
      *>   EC-SIZE checking applies: EC-SIZE-TRUNCATION is raised and
      *>   its own ON SIZE ERROR shows it -> "P2 [EC-SIZE-TRUNCATION]".
      *>   A and B are unchanged (size error) -> "A=9 B=9".
      *> An implementation that applied the TURN to the enclosing ADD
      *> would show EC-SIZE-TRUNCATION on P1; one that ignored it for
      *> statements nested in that ADD would show spaces on P2.
      *> EDITION: >>TURN and FUNCTION EXCEPTION-STATUS are COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 9.
       01 B PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       M1.
           ADD 1 TO A
       >>TURN EC-SIZE CHECKING ON
             ON SIZE ERROR
               DISPLAY "P1 [" FUNCTION EXCEPTION-STATUS "]"
               ADD 1 TO B
                 ON SIZE ERROR
                   DISPLAY "P2 [" FUNCTION EXCEPTION-STATUS "]"
               END-ADD
           END-ADD.
           DISPLAY "A=" A " B=" B.
           STOP RUN.
