*>--- conformance-case
*> id: pb303_as_class_id_literal
*> rules: [SR-11.3.3-1, FMT-11.3.2]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS-phrase literal screen is ONE screen serving five
       *> clauses that state one rule.  Each paragraph gets its own negative so a
       *> regression that unwires ONE call site cannot hide behind the other four.
       *> ISO 11.3.3 syntax rule 1 is the ONE clause of the five that states the rule
       *> WITHOUT the zero-length half: "Literal-1 shall be an alphanumeric literal or a
       *> national literal and shall not be a figurative constant."  The figurative half
       *> it DOES state is pinned here; 2023/pb303_as_class_id_zero_length_accepted pins
       *> the half it omits, so the asymmetry is observable from both sides.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB303CQ AS ZERO.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB303CQ.
