*>--- conformance-case
*> id: pb2937_skeleton_bodies_bind_as_themselves
*> rules: [GR-11.3.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2937 -- an UNEXPANDED parameterized class binds its
      *> method BODIES as itself (OO deep-dive D12 determination 6), so
      *> every rule of their text is asked, and a statement that reaches
      *> a method through a parameter-name waits for an expansion:
      *> 9.3.8.2.4 (cite.py OK) treats the definition "as if the actual
      *> parameter classes or interfaces were substituted for the
      *> parameters". PB2937SK writes its formals in every such
      *> statement position, all legal in an expansion whose actuals fit:
      *> an INVOKE through a formal-typed reference (a literal argument
      *> included), SUPER and B OF SUPER over the formal superclass B,
      *> an INVOKE of the formal class-name E (11.3.4 GR6, cite.py OK:
      *> a parameter-name stands where an object-class-name is
      *> permitted), SELF of a method only B can define, an inline
      *> invocation and an object property through a formal, SET of the
      *> factory object of E, SET between descriptions that name the
      *> same formal (one class, whatever it is) and between different
      *> formals (E's actual may implement I's). Each of those nested
      *> in an IF, EVALUATE or inline PERFORM defers alone, and so does
      *> an IF whose condition reads through a formal (they drew a
      *> false internal error COBOLNET2362). PB2937SC has a WRITTEN
      *> superclass, so its SUPER "SHOW" binds against PB2937SB here.
      *> Neither is expanded, so neither creates a class or displays.
      *> PI is an ordinary class spelled like the zero-argument
      *> intrinsic PI: a class-name is a user-defined word (8.3.2.1),
      *> and INVOKE PI "NEW" names the class (it drew COBOLNET1543).
      *> EXPECTED OUTPUT: PB2937SM creates a PI and invokes its SHOW,
      *> which displays "PI-OK", then displays "PB2937-OK".
       IDENTIFICATION DIVISION.
       CLASS-ID. PB2937SB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "SB".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2937SB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2937SK INHERITS FROM B USING B E I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS B
           CLASS E
           INTERFACE I
           PROPERTY SZP.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE E.
       PROCEDURE DIVISION RETURNING R.
           INVOKE E "NEW" RETURNING R.
           ADD 1 TO CNT.
       END METHOD MAKE.
       END FACTORY.
       OBJECT.
           IMPLEMENTS I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE E.
       01 ITF  USAGE OBJECT REFERENCE I.
       01 FAC  USAGE OBJECT REFERENCE FACTORY OF E.
       01 ONE  USAGE OBJECT REFERENCE E ONLY.
       01 ANY1 USAGE OBJECT REFERENCE.
       01 CNT2 PIC 9.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE E.
       01 Y USAGE OBJECT REFERENCE E ONLY.
       PROCEDURE DIVISION USING X Y RAISING E.
           SET ITEM TO X.
           SET ONE TO Y.
           SET ANY1 TO ITEM.
           SET ITF TO ITEM.
           IF ITEM = NULL DISPLAY "NULL" END-IF.
           IF ITEM = X DISPLAY "SAME" END-IF.
       END METHOD PUT.
       METHOD-ID. SHOW OVERRIDE.
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW".
           INVOKE ITEM "SZ" USING 3 RETURNING CNT2.
           INVOKE SUPER "SHOW".
           INVOKE B OF SUPER "SHOW".
           INVOKE SELF "PUT" USING ITEM ONE.
           INVOKE SELF "ONLY-B-HAS-IT".
           INVOKE E "NEW" RETURNING ITEM.
           INVOKE FAC "NEW" RETURNING ITEM.
           SET FAC TO E.
           SET ANY1 TO ITEM AS E.
           MOVE ITEM :: "SZ" TO CNT2.
           MOVE CNT2 TO SZP OF ITEM.
           IF CNT2 = 1 MOVE CNT2 TO SZP OF ITEM END-IF.
           EVALUATE TRUE WHEN CNT2 = 1
               INVOKE ITEM "SHOW"
               MOVE SZP OF ITEM TO CNT2 END-EVALUATE.
           PERFORM UNTIL CNT2 = 1
               MOVE ITEM :: "SZ" TO CNT2 END-PERFORM.
           IF ITEM :: "SZ" = 1 MOVE 1 TO CNT2 END-IF.
           RAISE ITEM.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2937SK.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB2937SC INHERITS FROM PB2937SB USING T.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB2937SB
           CLASS T.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HELD USAGE OBJECT REFERENCE T.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW OVERRIDE.
       PROCEDURE DIVISION.
           INVOKE SUPER "SHOW".
           INVOKE HELD "SHOW".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB2937SC.

       IDENTIFICATION DIVISION.
       CLASS-ID. PI INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "PI-OK".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PI.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2937SM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PI.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PI "NEW" RETURNING O.
           INVOKE O "SHOW".
           DISPLAY "PB2937-OK".
           STOP RUN.
       END PROGRAM PB2937SM.
