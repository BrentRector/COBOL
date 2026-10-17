*>--- conformance-case
*> id: pb878_invoke_content_move_rules
*> rules: [SR-14.8.2.2-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.8.2.3.3 rule 2d: "Otherwise, the conformance rules are the same as for a MOVE
      *> statement with the argument as the sending operand and the corresponding formal parameter as the
      *> receiving operand." kb/Work PB878 routed this screen through the ONE 14.9.25.3 validity chain; this
      *> golden pins the crossings the chain ADMITS, with values computed from the MOVE rules:
      *>   1. PIC 9(4) VALUE 42 -> PIC X(4): Table 16 numeric-integer row, alphanumeric column "Yes";
      *>      14.9.25.4 GR6 a) "alignment and any necessary space filling shall take place as defined in
      *>      14.6.8" - the four digit characters fill the four positions -> "0042".
      *>   2. an alphanumeric GROUP "AB" + 9(2) 07 -> PIC X(4): 14.9.25.4 GR4, "treated exactly as if it
      *>      were an alphanumeric to alphanumeric elementary move, except that there is no conversion of
      *>      data" -> "AB07".
      *>   3. PIC X(3) "XYZ" -> a PIC X(4) formal: alphanumeric -> alphanumeric, space-filled on the
      *>      right (14.9.25.4 GR6 a) -> "XYZ ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB878POO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB878PC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB878PC.
       01 N4 PIC 9(4) VALUE 42.
       01 GRP.
          05 GA PIC X(2) VALUE "AB".
          05 GN PIC 9(2) VALUE 7.
       01 X3 PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB878PC "NEW" RETURNING O.
           INVOKE O "TAKE" USING BY CONTENT N4.
           INVOKE O "TAKE" USING BY CONTENT GRP.
           INVOKE O "TAKE" USING BY CONTENT X3.
           STOP RUN.
       END PROGRAM PB878POO.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB878PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(4).
       PROCEDURE DIVISION USING LK-X.
       MAIN.
           DISPLAY "LK-X=[" LK-X "]".
       END METHOD TAKE.
       END OBJECT.
       END CLASS PB878PC.
