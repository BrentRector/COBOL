*>--- conformance-case
*> id: exception_statement_ambient
*> rules: [GR-7.3.25.4-7, GR-14.6.13.1.3-4, GR-14.9.9.4-1, RV-15.30.3-1,
*>    RV-15.32.3-1, RV-15.32.3-2, DOC-A.1-139, DOC-A.1-204]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
>>TURN EC-RANGE-SEARCH-NO-MATCH CHECKING ON WITH LOCATION
>>TURN EC-SIZE-TRUNCATION CHECKING ON
      *> kb/Work R14 - the (statement, location) pair travels on the AMBIENT statement context, so raise
      *> sites that cannot thread it positionally (SEARCH's range conditions, CONTINUE AFTER) still answer
      *> ISO/IEC 1989:2023 15.32.3 r2 / 15.30.3 r2 under WITH LOCATION:
      *>   15.32.3 r2 - the returned value is the NAME OF THE STATEMENT that caused the condition, from
      *>                Table 12's 'Statement name' column (r3), 63 chars left-justified space-filled.
      *>   15.32.3 r1 - without LOCATION (and this implementation saves nothing then - CONFORMANCE.md),
      *>                63 spaces; the rule is PER-CONDITION, so the WITH LOCATION on the SEARCH condition
      *>                must not contaminate the EC-SIZE-TRUNCATION answer (the R06 rule, re-pinned here
      *>                across the R14 channel change).
      *>   14.9.28.4 GR14 - an exception-checking PERFORM implicitly enables its WHEN-named conditions
      *>                over imperative-statement-1 (WITH LOCATION iff the PERFORM says LOCATION) - and
      *>                imp-1 may START ON THE PERFORM'S OWN LINE, which previously escaped the enable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R14ECSTMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E PIC 99 OCCURS 5 TIMES INDEXED BY IX.
       01 SMALL-X PIC 99.
       01 NEG PIC S9 VALUE -1.
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1.
           SEARCH E WHEN E(IX) = 99 CONTINUE END-SEARCH.
           DISPLAY "S1=[" FUNCTION EXCEPTION-STATEMENT "]".
           DISPLAY "S2=[" FUNCTION EXCEPTION-LOCATION "]".
           COMPUTE SMALL-X = 12345 ON SIZE ERROR CONTINUE END-COMPUTE.
           DISPLAY "C1=[" FUNCTION EXCEPTION-STATEMENT "]".
           DISPLAY "C2=[" FUNCTION EXCEPTION-LOCATION "]".
           PERFORM WITH LOCATION CONTINUE AFTER NEG SECONDS WHEN EC-CONTINUE-LESS-THAN-ZERO CONTINUE END-PERFORM.
           DISPLAY "T1=[" FUNCTION EXCEPTION-STATEMENT "]".
           DISPLAY "T2=[" FUNCTION EXCEPTION-LOCATION "]".
           STOP RUN.
