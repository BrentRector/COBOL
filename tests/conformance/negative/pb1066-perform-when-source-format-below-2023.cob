      *> reject-at: 85 2002 2014
      *> kb/Work PB1066 - the negative half of
      *> conformance:2023/pb1066_gr14_implicit_pop_all_source_format.
      *> The implicit PUSH ALL / POP ALL of ISO 14.9.28.4 GR14 belongs
      *> to the exception-checking (Format 3) PERFORM, which is new in
      *> COBOL-2023, so below 2023 the PERFORM ... WHEN statement that
      *> brackets the >>SOURCE FORMAT is rejected (COBOLNET0900, the
      *> edition gate); at 85 the >>SOURCE FORMAT directive itself is
      *> also unintroduced.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1066SFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-D PIC X(3).
       PROCEDURE DIVISION.
           PERFORM
               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
           WHEN EC-OVERFLOW-STRING
       >>SOURCE FORMAT IS FREE
               DISPLAY "H1"
           END-PERFORM
000100     DISPLAY "AFTER".
           STOP RUN.
