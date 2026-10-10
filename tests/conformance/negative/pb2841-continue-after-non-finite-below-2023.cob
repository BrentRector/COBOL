      *> reject-at: 85 2002 2014
      *> kb/Work PB2841 - the CONTINUE AFTER ... SECONDS phrase is a COBOL-2023 addition (ISO 14.9.9.2 general
      *> format), so below 2023 the statement is refused by name (COBOLNET0900, the
      *> introduction band) whatever the interval holds. The positive golden is
      *> tests/conformance/2023/pb2841_continue_after_non_finite.cob: a FLOAT-LONG interval that is
      *> -infinity or a NaN is no EC-DATA-NOT-FINITE (14.6.13.2 item 3 names STANDARD floating-point
      *> usages only), -infinity takes GR1's less-than-zero leg and a NaN is a zero interval.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2841NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X.
          05 F USAGE FLOAT-LONG.
       PROCEDURE DIVISION.
       MAIN-P.
           CONTINUE AFTER F SECONDS.
           STOP RUN.
