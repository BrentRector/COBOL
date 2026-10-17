*>--- conformance-case
*> id: pb303_as_method_id_literal
*> rules: [SR-11.7.3-1]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the AS-phrase literal screen is ONE screen serving five
       *> clauses that state one rule.  Each paragraph gets its own negative so a
       *> regression that unwires ONE call site cannot hide behind the other four.
       *> ISO 11.7.3 syntax rule 1, on the METHOD-ID paragraph (ISO 11.7.2 prints
       *> [AS literal-1] on the method-name-1 arm only).  The zero-length half is used
       *> here because METHOD-ID, unlike CLASS-ID, states it.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB303MQ.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. PB303MQM AS "".
       PROCEDURE DIVISION.
       MQ-P.
           GOBACK.
       END METHOD PB303MQM.
       END OBJECT.
       END CLASS PB303MQ.
