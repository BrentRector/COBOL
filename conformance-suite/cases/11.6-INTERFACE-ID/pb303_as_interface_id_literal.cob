*>--- conformance-case
*> id: pb303_as_interface_id_literal
*> rules: [SR-11.6.3-1, FMT-11.6.2]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS-phrase literal screen is ONE screen serving five
       *> clauses that state one rule.  Each paragraph gets its own negative so a
       *> regression that unwires ONE call site cannot hide behind the other four.
       *> ISO 11.6.3 syntax rule 1, on the INTERFACE-ID paragraph (ISO 11.6.2).
       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB303IQ AS SPACE.
       PROCEDURE DIVISION.
       END INTERFACE PB303IQ.
