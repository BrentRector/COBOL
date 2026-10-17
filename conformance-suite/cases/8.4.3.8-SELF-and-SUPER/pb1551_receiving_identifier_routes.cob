*>--- conformance-case
*> id: pb1551_receiving_identifier_routes
*> rules: [SR-8.4.3.8.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1551 - every receiving position parses the identifier
      *> formats that are never a receiving operand (SELF, SUPER, NULL, an
      *> object-view, an address-identifier) only so it can refuse each by
      *> its own ISO 8.4.3 rule. This program writes the LEGAL neighbours of
      *> each superset shape and pins that they still parse, route and run
      *> as before:
      *>   SET O TO SELF / IF O = SELF  - SELF the SENDER (8.4.3.8.4 GR1:
      *>                    "SELF and SUPER both reference the object that
      *>                    was used to invoke the method")       -> SAME
      *>   INVOKE ... RETURNING R       - a data reference receives the
      *>                    method's result
      *>   ALLOCATE ... RETURNING P, SET ADDRESS OF B TO P - SET Format 7's
      *>                    own receiver ADDRESS OF data-name-1 is no
      *>                    data-address-identifier (Annex D.9.2.1) and
      *>                    keeps its format; B shows the stored text -> ABCD
      *>   SET P2 TO ADDRESS OF B, IF P2 = P - the data-address-identifier
      *>                    as the SENDER (8.4.3.11.4 GR1)        -> EQ
      *>   ADD 1 TO N GIVING M, MULTIPLY 2 BY M - the mixed arithmetic
      *>                    operand as Format 2's sender and Format 1's
      *>                    receiver: M = (0 + 1) * 2             -> 0002
      *>   SET PP TO ADDRESS OF PROGRAM, CALL PP - the program-address-
      *>                    identifier as the SENDER (8.4.3.13.4)  -> SUB
      *>   SET U TO NULL, IF U = NULL   - NULL the SENDER         -> NUL
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551PM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1551PC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OB USAGE OBJECT REFERENCE PB1551PC.
       01 U  USAGE OBJECT REFERENCE.
       01 R  PIC X(4).
       01 P  USAGE POINTER.
       01 P2 USAGE POINTER.
       01 PP USAGE PROGRAM-POINTER.
       01 N  PIC 9(4) VALUE 0.
       01 M  PIC 9(4) VALUE 0.
       01 B  PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB1551PC "NEW" RETURNING OB
           INVOKE OB "ISSELF" RETURNING R
           DISPLAY R
           ALLOCATE 4 CHARACTERS RETURNING P
           SET ADDRESS OF B TO P
           MOVE "ABCD" TO B
           DISPLAY B
           SET P2 TO ADDRESS OF B
           IF P2 = P DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           FREE P
           ADD 1 TO N GIVING M
           MULTIPLY 2 BY M
           DISPLAY M
           SET PP TO ADDRESS OF PROGRAM "PB1551PS"
           CALL PP
           SET U TO NULL
           IF U = NULL DISPLAY "NUL" ELSE DISPLAY "OBJ" END-IF
           STOP RUN.
       END PROGRAM PB1551PM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1551PS.
       PROCEDURE DIVISION.
       MAIN-S.
           DISPLAY "SUB"
           GOBACK.
       END PROGRAM PB1551PS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1551PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ISSELF.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE.
       LINKAGE SECTION.
       01 RS PIC X(4).
       PROCEDURE DIVISION RETURNING RS.
           SET O TO SELF
           IF O = SELF MOVE "SAME" TO RS ELSE MOVE "DIFF" TO RS END-IF.
       END METHOD ISSELF.
       END OBJECT.
       END CLASS PB1551PC.
