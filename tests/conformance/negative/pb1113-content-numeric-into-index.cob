      *> reject-at: 2002 2014 2023
      *> kb/Work PB1113 -- ISO 14.8.2.3.3 2) b): "If the formal parameter
      *> is an index data item, the conformance rules are the same as for
      *> a SET statement with the argument as the sending operand and the
      *> corresponding formal parameter as the receiving operand." SET
      *> Format 1 into a class-index item takes identifier-2 "of class
      *> index" (14.9.39.3 SR2) and refuses arithmetic-expression-1 (SR3),
      *> so neither a PIC 9(4) item nor the literal 3 conforms -- both are
      *> COBOLNET1688. (Before the fix an index data item, which carries
      *> category numeric, took the COMPUTE arm and both ran.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB1113J" AS NESTED USING BY CONTENT N4
           CALL "PB1113J" AS NESTED USING BY CONTENT 3
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1113J.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L USAGE INDEX.
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-J"
           GOBACK.
       END PROGRAM PB1113J.
       END PROGRAM PB1113I.
