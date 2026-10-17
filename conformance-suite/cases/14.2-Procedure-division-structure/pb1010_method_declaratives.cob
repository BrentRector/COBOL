*>--- conformance-case
*> id: pb1010_method_declaratives
*> rules: [SR-14.2.2-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      >>TURN EC-SIZE CHECKING ON
      *> kb/Work PB1010 -- DECLARATIVES inside a METHOD definition.
      *> ISO 14.2.2 SR10: "Formats 1 and 2 may be specified in a source
      *> element if and only if that source element is a function
      *> definition, a function prototype definition, a method definition,
      *> a program definition, or a program prototype definition" -- so a
      *> method's procedure division may carry a declaratives portion.
      *> 14.9.49.4 GR3 selects over "the USE statements in the source
      *> element" and GR4 a) makes that the element containing the statement
      *> that caused the condition: the METHOD.  Legs: a Format 1 USE on an
      *> object file (the OPEN of a missing file, status 35, GR6 a) then
      *> GR7 b) implicit CONTINUE); a Format 3 USE (EC-SIZE, RESUME AT NEXT
      *> STATEMENT); a Format 4 USE (EXCEPTION OBJECT, RESUME AT a
      *> procedure-name of the method); a sibling method with no
      *> declaratives, whose failing OPEN of the same file runs neither its
      *> sibling's USE ON F nor the invoker's declaratives; and the
      *> invoker's own EC-ALL declarative, which no condition raised inside
      *> a method may select.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1010EX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       END OBJECT.
       END CLASS PB1010EX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1010C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS PB1010EX.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1010-no-such-file.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 FR PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 EN PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. OPENIT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 HITS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       FERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       FERR-P.
           ADD 1 TO HITS
           DISPLAY "  FILE-DECL FS=" FS " HITS=" HITS.
       SZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       SZ-P.
           ADD 1 TO HITS
           DISPLAY "  EC-DECL " FUNCTION EXCEPTION-STATUS " HITS=" HITS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           OPEN INPUT F
           DISPLAY "AFTER OPEN FS=" FS
           COMPUTE HITS = 50 * 3
           DISPLAY "AFTER COMPUTE"
           GOBACK.
       END METHOD OPENIT.
       METHOD-ID. PLAIN.
       PROCEDURE DIVISION.
           OPEN INPUT F
           DISPLAY "PLAIN FS=" FS.
       END METHOD PLAIN.
       METHOD-ID. THROWER.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X USAGE OBJECT REFERENCE PB1010EX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       EOS SECTION.
           USE AFTER EXCEPTION OBJECT PB1010EX.
       EOS-P.
           ADD 1 TO EN
           DISPLAY "  EO-DECL EN=" EN
           RESUME AT AFTER-RAISE.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           INVOKE PB1010EX "NEW" RETURNING X
           RAISE X
           DISPLAY "SKIPPED".
       AFTER-RAISE.
           DISPLAY "AFTER-RAISE EN=" EN
           GOBACK.
       END METHOD THROWER.
       END OBJECT.
       END CLASS PB1010C.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1010M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1010C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B USAGE OBJECT REFERENCE PB1010C.
       PROCEDURE DIVISION.
       DECLARATIVES.
       MSZ SECTION.
           USE AFTER EXCEPTION CONDITION EC-ALL.
       MSZ-P.
           DISPLAY "INVOKER DECL " FUNCTION EXCEPTION-STATUS.
       END DECLARATIVES.
       MAIN SECTION.
       M-P.
           INVOKE PB1010C "NEW" RETURNING B
           INVOKE B "OPENIT"
           INVOKE B "PLAIN"
           INVOKE B "THROWER"
           INVOKE B "THROWER"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM PB1010M.
