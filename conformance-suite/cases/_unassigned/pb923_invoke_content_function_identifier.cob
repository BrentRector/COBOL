*>--- conformance-case
*> id: pb923_invoke_content_function_identifier
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB923 — ISO 8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, and 15.2 lets a
      *> function "be used anywhere a sending data item of that class and category may be specified", so
      *> `BY CONTENT FUNCTION f(...)` is 14.9.23.2's identifier-5 (14.9.23.3 SR21 - a sending operand). Its
      *> conformance is chosen by the FORMAL (14.8.2.3.3 rule 2): a non-numeric formal takes rule 2 d)'s MOVE
      *> lane and a numeric formal rule 2 a)'s COMPUTE lane. The MOVE lane used to be unreachable - the sole
      *> function parsed as arithmetic-expression-1 and was refused COBOLNET0828. Values by the MOVE rules
      *> (14.9.25.4 GR6 a: alphanumeric receiver, left-justified, space-filled) and COMPUTE:
      *>   LOWER-CASE("  HI    ") = "  hi    "  -> "  hi    "
      *>   UPPER-CASE("  hi    ") = "  HI    "  -> "  HI    "
      *>   REVERSE("  HI    ")    = "    IH  "  -> "    IH  "
      *>   MAX(7 12)              = 12          -> PIC 9(4) 0012 (rule 2 a, COMPUTE)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB923POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB923PC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB923PC.
       01 X PIC X(8) VALUE "  HI    ".
       01 XL PIC X(8) VALUE "  hi    ".
       01 N PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB923PC "NEW" RETURNING O
           INVOKE O "TAKEX" USING BY CONTENT FUNCTION LOWER-CASE(X)
           INVOKE O "TAKEX" USING BY CONTENT FUNCTION UPPER-CASE(XL)
           INVOKE O "TAKEX" USING BY CONTENT FUNCTION REVERSE(X)
           INVOKE O "TAKEN" USING BY CONTENT FUNCTION MAX(N 12)
           STOP RUN.
       END PROGRAM PB923POS.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB923PC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKEX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC X(8).
       PROCEDURE DIVISION USING LK-X.
       MAIN-X.
           DISPLAY "LK-X=[" LK-X "]".
       END METHOD TAKEX.
       METHOD-ID. TAKEN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC 9(4).
       PROCEDURE DIVISION USING LK-N.
       MAIN-N.
           DISPLAY "LK-N=[" LK-N "]".
       END METHOD TAKEN.
       END OBJECT.
       END CLASS PB923PC.
