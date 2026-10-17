*>--- conformance-case
*> id: pb399_format3_relation_legal_shapes
*> rules: [SR-8.4.3.1.3-9, GR-8.8.4.2.1-11, SR-14.9.13.3-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> The ISO §8.8.4.2.2 FORMAT 3 relation shapes that are LEGAL, pinned at every surface that lowers to
      *> a relation — so moving that band out of ConditionBinder's relation arm and into the ONE
      *> BoundRelational checkpoint (kb/Work PB399) widened it to EVALUATE without rejecting legal source.
      *>
      *> §8.8.4.2.1: "a relation condition involving operands of class message-tag, object, or pointer is a
      *> message-tag-object-or-pointer-reference relation condition."  Format 3 admits IS [NOT] EQUAL TO /
      *> = / <>, and §8.8.4.2.3 SR5 requires both operands to be of one of those classes AND of the same
      *> category.  §8.8.4.2.16: "The operands are equal if they reference the same address."
      *> §8.4.3.10.1 makes NULL "a predefined address of class pointer", and §8.4.3.10.3 SR1 a) admits it
      *> "in a pointer-or-object-reference relation condition" by name — so NULL rides on either side and
      *> is exempt from SR5's same-category test, having no category of its own.
      *> §14.9.13.4 GR2 makes an EVALUATE selection pair a comparison "as if the corresponding relation
      *> condition were written", which is why the same four legs are written under EVALUATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB399FMT3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A     PIC X(4) VALUE "ABCD".
       01 WS-P     USAGE POINTER.
       01 WS-Q     USAGE POINTER.
       01 WS-PP    USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET WS-P TO NULL.
           SET WS-Q TO NULL.
           SET WS-PP TO NULL.
      *> Two operands of class pointer, of the SAME category (§8.8.4.2.1 item 11) — both set to the one
      *> predefined address, so they reference the same address and are equal (§8.8.4.2.16).
           IF WS-P = WS-Q
               DISPLAY "PTR-EQ"
           ELSE
               DISPLAY "PTR-NE"
           END-IF.
      *> NULL on the object side of a data-pointer relation.
           IF WS-P = NULL
               DISPLAY "PTR-NULL"
           ELSE
               DISPLAY "PTR-NOT-NULL"
           END-IF.
      *> NULL against a PROGRAM-POINTER: Table 2 puts data-pointer, program-pointer and function-pointer in
      *> ONE class, so this is a Format 3 relation too and NULL is admissible in it.
           IF WS-PP = NULL
               DISPLAY "PPTR-NULL"
           ELSE
               DISPLAY "PPTR-NOT-NULL"
           END-IF.
      *> NOT EQUAL is Format 3's other operator.
           IF WS-P NOT = NULL
               DISPLAY "PTR-NE-NULL"
           ELSE
               DISPLAY "PTR-EQ-NULL"
           END-IF.
      *> The same two relations written as EVALUATE selection pairs (§14.9.13.4 GR2).
           EVALUATE WS-P
               WHEN WS-Q
                   DISPLAY "EV-PTR-EQ"
               WHEN OTHER
                   DISPLAY "EV-PTR-NE"
           END-EVALUATE.
           EVALUATE WS-P
               WHEN NULL
                   DISPLAY "EV-PTR-NULL"
               WHEN OTHER
                   DISPLAY "EV-PTR-NOT-NULL"
           END-EVALUATE.
      *> A pointer that references a data item is not equal to the predefined NULL address.
           SET WS-P TO ADDRESS OF WS-A.
           IF WS-P = NULL
               DISPLAY "ADDR-NULL"
           ELSE
               DISPLAY "ADDR-NOT-NULL"
           END-IF.
           STOP RUN.
