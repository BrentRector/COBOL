*>--- conformance-case
*> id: pb2589_repository_function_keyword_omitted
*> rules: []
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2589 - the REPOSITORY FUNCTION entry is a COBOL-2002
      *> introduction (12.3.8.2 intrinsic-function-specifier), and it is the
      *> declaration that lets a function-identifier omit the word FUNCTION
      *> (8.4.3.2.3 SR2). At 2002 both names below are declared, so SUM(1 2)
      *> and LENGTH(A) are function references, not subscripted data items.
      *> Values by the function definitions: SUM of 1 and 2 is 3, shown in a
      *> PIC 99 item as 03 (15.88 returned value); LENGTH of an alphanumeric
      *> item of 7 positions is 7 (15.50.4 rule 3), shown as 07. The twin
      *> negative/pb2589-repository-function-at-85 pins the refusal below 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2589POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SUM LENGTH INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(7).
       01 N PIC 99.
       01 M PIC 99.
       PROCEDURE DIVISION.
           COMPUTE N = SUM(1 2)
           COMPUTE M = LENGTH(A)
           DISPLAY N " " M
           STOP RUN.
