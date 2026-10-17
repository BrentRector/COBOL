      *>--- conformance-case
      *> id: pb2937-skeleton-statement-rule
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2937 - an UNEXPANDED parameterized class binds its
      *> method bodies as itself (OO deep-dive D12 determination 6), so
      *> a statement rule no parameter-name touches is asked whether or
      *> not anything expands the class: the MOVE's receiving operand
      *> names nothing this source element declares, which 8.4.2.1
      *> refuses (COBOLNET1639) exactly as in an ordinary class. The
      *> INVOKE through the formal-typed ITEM is each expansion's and
      *> draws nothing here, so the 1639 is this rule's.
       IDENTIFICATION DIVISION.
       CLASS-ID. NSKSTM USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS T.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE T.
       01 C PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW"
           MOVE C TO UNDECLARED-NAME.
       END METHOD SHOW.
       END OBJECT.
       END CLASS NSKSTM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NSKSTMM.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
       END PROGRAM NSKSTMM.
