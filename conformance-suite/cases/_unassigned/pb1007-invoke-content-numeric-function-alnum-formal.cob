*>--- conformance-case
*> id: pb1007-invoke-content-numeric-function-alnum-formal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1007 - the refusing half. 14.8.2.3.3 2) d) makes a BY CONTENT argument for a
      *> non-numeric formal conform "as for a MOVE statement"; NUMVAL is a NUMERIC function (15.2 item 4),
      *> Table 16's Noninteger row, which 14.9.25.3 SR10 refuses into an alphanumeric receiver - exactly as
      *> `MOVE FUNCTION NUMVAL("3.7") TO P1` is refused. The integer twin is admitted
      *> (conformance:2002/pb1007_invoke_content_integer_function).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1007NI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1007NC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1007NC.
       PROCEDURE DIVISION.
           INVOKE PB1007NC "NEW" RETURNING O
           INVOKE O "MX" USING BY CONTENT FUNCTION NUMVAL("3.7")
           STOP RUN.
       END PROGRAM PB1007NI.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1007NC INHERITS FROM BASE.
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
           DISPLAY "P1=[" P1 "]".
       END METHOD MX.
       END OBJECT.
       END CLASS PB1007NC.
