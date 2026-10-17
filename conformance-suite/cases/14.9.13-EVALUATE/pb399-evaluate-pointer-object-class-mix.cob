*>--- conformance-case
*> id: pb399-evaluate-pointer-object-class-mix
*> rules: [SR-14.9.13.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR7 a): the selection objects "shall be valid operands for comparison to the
      *> corresponding operand in the set of selection subjects in accordance with 8.8.4.2, Simple relation
      *> conditions."  §8.8.4.2.1 makes a relation over a class-pointer operand a
      *> message-tag-object-or-pointer-reference relation condition, and §8.8.4.2.3 SR5 requires BOTH its
      *> operands to be "data items of class message-tag, object, or pointer".  A PIC X(3) object is not.
      *>
      *> ⛔ THE §8.8.4.2 BAND USED TO LIVE IN ConditionBinder's RELATION ARM AND NOT AT THE ONE
      *> BoundRelational CHECKPOINT, so `IF WS-P = WS-X` was rejected and the identical pair written as an
      *> EVALUATE selection pair was not — it reached the BACKEND and failed as a raw C# compiler error,
      *> `CS1503: cannot convert from 'string' to 'ManagedPointer?'` (kb/Work PB399).  §14.9.13.4 GR2 makes
      *> the pair a comparison "as if the corresponding relation condition were written", so it is owed
      *> every §8.8.4.2 rule the written relation is owed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399EQPX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       01 WS-X PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-P
               WHEN WS-X
                   DISPLAY "EQ-MATCHED"
               WHEN OTHER
                   DISPLAY "EQ-OTHER"
           END-EVALUATE.
           STOP RUN.
