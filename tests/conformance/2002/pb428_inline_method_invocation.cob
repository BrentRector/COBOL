      *> ISO 1989:2023 §8.4.3.4 — INLINE METHOD INVOCATION, the §8.4.3.1.2 Format 4 identifier.
      *> General format (§8.4.3.4.2, rendered from the canonical PDF page 163 / printed folio 133):
      *>     { object-class-name-1 | identifier-1 } :: literal-1
      *>         [ ( { arithmetic-expression-1 | boolean-expression-1 | identifier-2 | literal-2
      *>               | OMITTED } … ) ]
      *> §8.7.4 makes '::' the invocation operator; §8.4.3.4.4 GR1 defines the construct as the equivalent
      *> INVOKE statement delivering into a temporary whose description is the invoked method's RETURNING
      *> item (GR1 b)). kb/Work PB428: before this, the construct was a raw parse error in EVERY position.
      *> Every expected value below is DERIVED from the standard, not captured:
      *>   1 MOVE …            §14.9.25.4 GR5 / Table 16 alphanumeric→alphanumeric: left-justified, space
      *>                       filled, so the PIC X(7) "ACCOUNT" arrives in PIC X(8) as "ACCOUNT ".
      *>   2 argument          §8.4.3.4.4 GR1 a): the parenthesised operands are the USING arguments, so
      *>                       BAL 0 + 10 = 10, moved to PIC 9(4) → 0010.
      *>   3 arithmetic        §8.8.1.1: the invocation is an identifier operand — BAL 10 + 5 = 15, + 1 = 16.
      *>   4 DISPLAY           §14.9.11.4 GR3: the operand's character positions, "ACCOUNT ".
      *>   5 reference-mod     §8.4.3.1.4 GR1 g) applies the modifier to the identifier on its left;
      *>                       §8.4.3.3.4 GR5 takes positions 1..3 of "ACCOUNT " → "ACC".
      *>   6 chained           §8.4.3.1.3 SR1 — identifier is defined recursively, so the temporary of one
      *>                       invocation is identifier-1 of the next: MAKE yields a fresh ACC, GETNAME
      *>                       yields "ACCOUNT ".
      *>   7 class-name        §8.4.3.4.2's object-class-name-1 arm, through the factory method OPENACC
      *>                       (whose SELF "New" creates the account). New ITSELF is not inline-invocable:
      *>                       §16.2 describes its RETURNING item with ACTIVE-CLASS, which §8.4.3.4.3 SR4
      *>                       excludes (negative pb1548-inline-new-active-class; kb/Work PB1548).
      *>   8 relation          §8.8.4.2.1 over two alphanumeric operands of equal size (§8.8.4.2.3 rule 2).
      *>   9 nested argument   the argument is itself Format 4; §14.8.2.3.3 rule 2d (MOVE rules) governs the
      *>                       crossing, because the GR1 c) temporary is not a data item defined in any of
      *>                       §14.9.23.3 SR9's sections and GR6 a)2 therefore assumes BY CONTENT.
      *>  10 expression arg     kb/Work PB1135 / decision R59: `(5 + 1)` is ONE argument, the arithmetic-expression-1
      *>                       of the printed brace (the spacing makes the + the binary operator, §8.7.1) - 6 is
      *>                       added to BAL 15 (after 2 and 3) = 21.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB428IMI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB428ACC
           CLASS PB428FAC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB428ACC.
       01 F1 USAGE OBJECT REFERENCE PB428FAC.
       01 W  PIC X(8).
       01 W3 PIC X(3).
       01 N  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB428ACC "NEW" RETURNING A1.
           INVOKE PB428FAC "NEW" RETURNING F1.
           MOVE A1 :: "GETNAME" TO W.
           DISPLAY "1=" W.
           MOVE A1 :: "ADDTO" (10) TO N.
           DISPLAY "2=" N.
           COMPUTE N = A1 :: "ADDTO" (5) + 1.
           DISPLAY "3=" N.
           DISPLAY "4=" A1 :: "GETNAME".
           MOVE A1 :: "GETNAME" (1:3) TO W3.
           DISPLAY "5=" W3.
           MOVE F1 :: "MAKE" :: "GETNAME" TO W.
           DISPLAY "6=" W.
           MOVE PB428ACC :: "OPENACC" :: "GETNAME" TO W.
           DISPLAY "7=" W.
           IF A1 :: "GETNAME" = "ACCOUNT " THEN
               DISPLAY "8=YES"
           ELSE
               DISPLAY "8=NO"
           END-IF.
           MOVE A1 :: "ECHO" (A1 :: "GETNAME") TO W.
           DISPLAY "9=" W.
           MOVE A1 :: "ADDTO" (5 + 1) TO N.
           DISPLAY "10=" N.
           STOP RUN.
       END PROGRAM PB428IMI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428ACC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. OPENACC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NEW USAGE OBJECT REFERENCE PB428ACC.
       PROCEDURE DIVISION RETURNING LK-NEW.
       MAIN.
           INVOKE SELF "NEW" RETURNING LK-NEW.
       END METHOD OPENACC.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BAL PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       METHOD-ID. ADDTO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-AMT PIC 9(4).
       01 LK-RES PIC 9(4).
       PROCEDURE DIVISION USING LK-AMT RETURNING LK-RES.
       MAIN.
           ADD LK-AMT TO BAL.
           MOVE BAL TO LK-RES.
       END METHOD ADDTO.
       METHOD-ID. ECHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-IN  PIC X(8).
       01 LK-OUT PIC X(8).
       PROCEDURE DIVISION USING LK-IN RETURNING LK-OUT.
       MAIN.
           MOVE LK-IN TO LK-OUT.
       END METHOD ECHO.
       END OBJECT.
       END CLASS PB428ACC.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428FAC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB428ACC.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-OBJ USAGE OBJECT REFERENCE PB428ACC.
       PROCEDURE DIVISION RETURNING LK-OBJ.
       MAIN.
           INVOKE PB428ACC "NEW" RETURNING LK-OBJ.
       END METHOD MAKE.
       END OBJECT.
       END CLASS PB428FAC.
