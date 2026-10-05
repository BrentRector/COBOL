      *> kb/Work PB1425 - identifier Format 7 whose identifier-3 is the object-view of a QUALIFIED data item:
      *> P OF A OF G AS C.
      *>
      *> THE RULE. ISO/IEC 1989:2023 8.4.3.1.4 GR1 applies a) the qualified-data-name-with-subscript first, then
      *> c) "an object-view applies to the identifier on the left", then d) "OF for object properties applies the
      *> property-name on the left to the identifier on the right". So BAL OF UG OF G AS PB1425VQA is the property
      *> BAL of the view (UG OF G) AS PB1425VQA, and 8.4.3.5.4 GR1 treats that reference "at compile-time as
      *> though it had the description specified by the AS phrase" - an object reference of class PB1425VQA,
      *> whose roster has the BAL accessors. 8.4.3.9.3 SR2 forbids a universal identifier-1, and the view is not
      *> one: UG's own description (universal, or BASE) is not the object of the property.
      *>
      *> EXPECTED OUTPUT (each value derived from those rules):
      *>   UNIV 00007     BAL OF UG OF G AS PB1425VQA, UG universal and referencing D, whose BAL is 7.
      *>   BASE 00007     BAL OF UB OF G AS PB1425VQA, UB typed to BASE (which has no BAL) - the view's class
      *>                  decides the roster, not UB's.
      *>   CALC 00008     COMPUTE N = BAL OF UG OF G AS PB1425VQA + 1 (an arithmetic operand).
      *>   SET 00042      MOVE 42 TO BAL OF UB OF G AS PB1425VQA, then BAL OF D: the SET reached D's object.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1425VQP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1425VQA
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GT TYPEDEF STRONG.
          05 UG USAGE OBJECT REFERENCE.
          05 UB USAGE OBJECT REFERENCE BASE.
       01 G TYPE GT.
       01 D USAGE OBJECT REFERENCE PB1425VQA.
       01 N PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1425VQA "NEW" RETURNING D
           MOVE 7 TO BAL OF D
           SET UG TO D
           SET UB TO D
           DISPLAY "UNIV " BAL OF UG OF G AS PB1425VQA
           DISPLAY "BASE " BAL OF UB OF G AS PB1425VQA
           COMPUTE N = BAL OF UG OF G AS PB1425VQA + 1
           DISPLAY "CALC " N
           MOVE 42 TO BAL OF UB OF G AS PB1425VQA
           DISPLAY "SET " BAL OF D
           STOP RUN.
       END PROGRAM PB1425VQP.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1425VQA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS PB1425VQA.
