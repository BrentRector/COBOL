*>--- conformance-case
*> id: pb399-evaluate-range-pointer-class
*> rules: [SR-14.9.13.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.13.3 SR4 names FOUR classes a range-expression's operands may not be of: "The two
      *> operands in a range-expression shall be of the same class and shall not be of class boolean,
      *> message-tag, object, or pointer."  This range's ends are USAGE POINTER items, class pointer by
      *> §8.5.2.1 Table 2.
      *>
      *> The exclusion has teeth of its own: a range lowers to `selection-subject >= left-part AND
      *> selection-subject <= right-part` (§14.9.13.4 GR4 a) 5.) and §8.8.4.2.2 Format 3 prints no ordering
      *> operator at all for a message-tag/object/pointer relation, so the lowering has no comparison to
      *> mean.  Before this screen the compiler emitted it anyway and ORDERED RAW ADDRESSES (kb/Work PB399).
      *> USAGE POINTER is a COBOL-2002 introduction, so the case is written from 2002 up.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399RPTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       01 WS-Q USAGE POINTER.
       01 WS-R USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE WS-P
               WHEN WS-Q THRU WS-R
                   DISPLAY "PTR-MATCHED"
               WHEN OTHER
                   DISPLAY "PTR-OTHER"
           END-EVALUATE.
           STOP RUN.
