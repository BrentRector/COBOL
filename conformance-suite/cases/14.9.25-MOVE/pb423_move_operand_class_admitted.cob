*>--- conformance-case
*> id: pb423_move_operand_class_admitted
*> rules: [SR-14.9.25.3-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB423. ISO 1989:2023 14.9.25.3 SR1 - "The class of identifier-1 or identifier-2 shall not be
      *> index, message-tag, object, or pointer" - is a prohibition, so its POSITIVE witness is that the screen
      *> rejects the classes the rule NAMES and nothing else. This program declares the very items whose classes
      *> SR1 bars and then references them only where the standard admits them, so every statement is legal and
      *> the widened screen must stay silent on all of them.
      *>
      *>   - SET P TO ADDRESS OF X        - 13.18.60.3 SR9 lists a SET statement among the references a
      *>                                    data-pointer data item may appear in; MOVE is not on that list.
      *>   - SET Q TO P                   - the same rule, pointer to pointer.
      *>   - MOVE X TO Y                  - two category-alphanumeric items: class alphanumeric on both sides
      *>                                    (8.5.2.1 Table 2), which SR1 does not reach at all.
      *>
      *> EXPECTED VALUES, COMPUTED FROM THE RULES (never measured):
      *>   Y  - 14.9.25.4 GR4 copies X's five characters into Y's five: HELLO.
      *>   EQ - 8.8.4.2.16 (Comparison of pointer operands): "The operands are equal if they reference the same
      *>        address." Q was SET from P, so both reference X's address and the relation is true: SAME.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB423CLASSOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "HELLO".
       01 Y PIC X(5) VALUE SPACES.
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X
           SET Q TO P
           MOVE X TO Y
           DISPLAY "Y=[" Y "]"
           IF Q = P
               DISPLAY "EQ=SAME"
           ELSE
               DISPLAY "EQ=DIFF"
           END-IF
           STOP RUN.
