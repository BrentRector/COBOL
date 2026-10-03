      *> reject-at: 85 2002 2014 2023
      *> ISO 8.4.2.3.3 SR4: "Index-name-1 shall correspond to a data description entry in the hierarchy of the
      *> table being referenced that contains an INDEXED BY phrase specifying that index-name." IXA is declared on
      *> T1, not in E2's hierarchy (T2 / R2 / E2), so E2(IXA, ALL) is refused exactly as E2(IXA, 1) is (COBOLNET1961).
      *> Before kb/Work PB1472 the table(ALL) argument of an intrinsic function rendered its non-ALL subscripts
      *> through a reader that carried no index-name collector, so this compiled clean and read through IXA's
      *> occurrence number of the OTHER table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1472ALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC 9 OCCURS 3 INDEXED BY IXA.
       01 T2.
          05 R2 OCCURS 3 INDEXED BY IXB.
             10 E2 PIC 9 OCCURS 3 VALUE 2.
       PROCEDURE DIVISION.
           SET IXA TO 2.
           DISPLAY FUNCTION SUM(E2(IXA, ALL)).
           STOP RUN.
