      *> reject-at: 2002 2014 2023
      *> kb/Work PB1946 (verdict kb/Work PB1936) -- ISO 14.8.2.2 rule 2 sends a
      *> BY CONTENT argument into a group formal by the MOVE rules, and a
      *> group move copies the sending operand's characters as an
      *> alphanumeric-to-alphanumeric move with no conversion (14.9.25.4 GR4).
      *> An arithmetic expression has no description whose characters could be
      *> copied (a numeric literal has its own digits), so the BY CONTENT
      *> expression N3 * 2 into the 4-character group method formal is
      *> COBOLNET0828.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1946GK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1946GK.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 L1 PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-MG"
           GOBACK.
       END METHOD MG.
       END OBJECT.
       END CLASS PB1946GK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1946GM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1946GK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1946GK.
       01 N3 PIC 9(3) VALUE 123.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1946GK "NEW" RETURNING O
           INVOKE O "MG" USING BY CONTENT N3 * 2
           STOP RUN.
       END PROGRAM PB1946GM.
