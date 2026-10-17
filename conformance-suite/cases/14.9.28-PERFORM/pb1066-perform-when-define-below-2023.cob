*>--- conformance-case
*> id: pb1066-perform-when-define-below-2023
*> rules: [GR-14.9.28.4-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1066 - the negative half of
      *> conformance:cases/14.9.28-PERFORM/pb1066_gr14_implicit_pop_all_define. The implicit
      *> PUSH ALL / POP ALL of ISO 14.9.28.4 GR14 belongs to the exception-
      *> checking (Format 3) PERFORM, which is new in COBOL-2023, so below
      *> 2023 the PERFORM ... WHEN statement that brackets the >>DEFINE is
      *> rejected (COBOLNET0900, the edition gate); at 85 the >>DEFINE
      *> directive itself is also unintroduced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1066N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       PROCEDURE DIVISION.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>DEFINE ZZ AS 1
               DISPLAY "H1"
           END-PERFORM
       >>IF ZZ IS DEFINED
           DISPLAY "ZZ-DEFINED"
       >>END-IF
           STOP RUN.
