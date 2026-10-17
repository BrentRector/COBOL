      *>--- conformance-case
      *> id: pb189_dynamic_table_element_operand
      *> rules: [GR-14.9.11.4-4]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB189DYNELEM.
      *> kb/Work PB189 - A SUBSCRIPTED ELEMENT OF A DYNAMIC-CAPACITY
      *> TABLE IS AN ORDINARY FIXED-LENGTH GROUP OPERAND.
      *> 8.5.1.12.1: "A variable-length group is a group item whose
      *> data description has at least one dynamic-length elementary
      *> item or dynamic-capacity table as a subordinate item. All
      *> other group items are referred to as fixed-length groups" -
      *> THE DYNAMIC AXIS BELONGS TO THE TABLE; ONE OCCURRENCE OF GT
      *> HAS NO SUCH SUBORDINATE. 14.9.11.4 GR4 TRANSFERS "THE DATA
      *> ITEM BEING TRANSFERRED" - THE ELEMENT - AND 14.9.11.3 SR1
      *> EXCLUDES ONLY CLASS MESSAGE-TAG, OBJECT OR POINTER. SO
      *> DISPLAY GT(n) SHOWS THE ELEMENT'S IMAGE: ZONED GT-N THEN GT-A.
      *> THE ELEMENT IS OBSERVED AT TWO CAPACITIES ALONGSIDE THE
      *> WHOLE-GROUP DISPLAY (A.1 ITEM 57 FORMAT, EVERY OCCURRENCE AT
      *> CURRENT CAPACITY) SO THE TWO ARE SEEN TO AGREE; OCCURRENCE 2,
      *> GROWN BUT UNTOUCHED, SHOWS THE DOCUMENTED SEED "00 ".
      *> THE SAME OPERAND AS A MOVE SENDER/RECEIVER (14.9.25.4 GR4 -
      *> A GROUP MOVE), REFERENCE-MODIFIED, AND AS A CALL ARGUMENT.
      *> BEFORE THE FIX EVERY ONE OF THESE WAS A RUN-TIME TIER-C LOUD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GP PIC X(3) VALUE "PFX".
          05 GT OCCURS DYNAMIC CAPACITY IN CAP FROM 1.
             10 GT-N PIC 9(2).
             10 GT-A PIC X.
       01 X PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 42 TO GT-N(1)
           MOVE "a" TO GT-A(1)
           DISPLAY "E1=[" GT(1) "]"
           DISPLAY "G1=[" G "]"
           MOVE 7 TO GT-N(3)
           MOVE "c" TO GT-A(3)
           DISPLAY "E1=[" GT(1) "] E2=[" GT(2) "] E3=[" GT(3) "]"
           DISPLAY "G2=[" G "]"
           MOVE GT(3) TO X
           DISPLAY "M=[" X "]"
           MOVE "99z" TO GT(2)
           DISPLAY "W=[" GT-N(2) "|" GT-A(2) "]"
           DISPLAY "R=[" GT(3)(2:2) "]"
           CALL "PB189SUB" USING GT(1)
           DISPLAY "C=[" GT(1) "]"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB189SUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(3).
       PROCEDURE DIVISION USING L.
           DISPLAY "L=[" L "]"
           MOVE "77q" TO L
           GOBACK.
       END PROGRAM PB189SUB.
       END PROGRAM PB189DYNELEM.
