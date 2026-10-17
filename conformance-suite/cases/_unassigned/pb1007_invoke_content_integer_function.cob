*>--- conformance-case
*> id: pb1007_invoke_content_integer_function
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1007 - an INTEGER function sent BY CONTENT to a non-numeric formal, beside the MOVE of
      *> the same value. 14.8.2.3.3 2) d): "the conformance rules are the same as for a MOVE statement with
      *> the argument as the sending operand and the corresponding formal parameter as the receiving
      *> operand", so each INV- line must show what the MOVE- line above it shows. An integer function
      *> (15.2 item 5) is Table 16's Integer row, admitted into alphanumeric, national and numeric-edited
      *> receivers; the INVOKE used to REFUSE it (COBOLNET0828, "requires a category-numeric formal").
      *>   INTEGER(3.7) = 3 (15.44.1)          -> PIC X(4)   "3   " (literal form, left-justified)
      *>   INTEGER-PART(-12.5) = -12 (15.49.1) -> PIC X(4)   "12  " (GR6 a: the sign is not moved)
      *>   ORD("A") = 66 (15.70.1)             -> PIC N(4)   "66  "
      *>   INTEGER-PART(-12.5) = -12           -> PIC -(4)9  "  -12"
      *>   NUMVAL("12.5") = 12.5 -> PIC 9(4) (a numeric formal: 2) a), COMPUTE, truncation) "0012"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1007IV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1007IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O  USAGE OBJECT REFERENCE PB1007IC.
       01 N  PIC 9V9 VALUE 3.7.
       01 R  PIC X(4).
       01 RN PIC N(4).
       01 RE PIC -(4)9.
       01 R9 PIC 9(4).
       PROCEDURE DIVISION.
           INVOKE PB1007IC "NEW" RETURNING O
           MOVE FUNCTION INTEGER(N) TO R
           DISPLAY "MOVE-X=[" R "]"
           INVOKE O "MX" USING BY CONTENT FUNCTION INTEGER(N)
           MOVE FUNCTION INTEGER-PART(-12.5) TO R
           DISPLAY "MOVE-X=[" R "]"
           INVOKE O "MX" USING BY CONTENT FUNCTION INTEGER-PART(-12.5)
           MOVE FUNCTION ORD("A") TO RN
           DISPLAY "MOVE-N=[" RN "]"
           INVOKE O "MN" USING BY CONTENT FUNCTION ORD("A")
           MOVE FUNCTION INTEGER-PART(-12.5) TO RE
           DISPLAY "MOVE-E=[" RE "]"
           INVOKE O "ME" USING BY CONTENT FUNCTION INTEGER-PART(-12.5)
           MOVE FUNCTION NUMVAL("12.5") TO R9
           DISPLAY "MOVE-9=[" R9 "]"
           INVOKE O "M9" USING BY CONTENT FUNCTION NUMVAL("12.5")
           STOP RUN.
       END PROGRAM PB1007IV.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1007IC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. MX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC X(4).
       PROCEDURE DIVISION USING P1.
           DISPLAY "INV-X=[" P1 "]".
       END METHOD MX.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC N(4).
       PROCEDURE DIVISION USING P1.
           DISPLAY "INV-N=[" P1 "]".
       END METHOD MN.
       METHOD-ID. ME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC -(4)9.
       PROCEDURE DIVISION USING P1.
           DISPLAY "INV-E=[" P1 "]".
       END METHOD ME.
       METHOD-ID. M9.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC 9(4).
       PROCEDURE DIVISION USING P1.
           DISPLAY "INV-9=[" P1 "]".
       END METHOD M9.
       END OBJECT.
       END CLASS PB1007IC.
