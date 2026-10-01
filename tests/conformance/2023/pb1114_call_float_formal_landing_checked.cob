      *> kb/Work PB1114 - the activating element's COMPUTE into a FLOATING-
      *> POINT formal is checked like the one into a fixed-point formal
      *> (kb/Work PB640): under EC-SIZE checking a value further from zero
      *> than the formal's format permits raises EC-SIZE-TRUNCATION in the
      *> CALLER, before control transfers.
      *>
      *> THE RULES. 14.2.3 GR9 (second branch: an AS NESTED CALL) makes the
      *> crossing "if the formal parameter is numeric, a COMPUTE statement
      *> without the ROUNDED phrase". 14.7.5 case 3 is a result "further
      *> from zero than permitted for the associated resultant data item",
      *> and its no-SIZE-ERROR-phrase rule 4 sets EC-SIZE-TRUNCATION; it is
      *> Table 13 FATAL, and 14.9.4.4 GR3 g) enters the called program only
      *> "if a fatal exception condition has not been raised". 14.7.4.3
      *> rule 10 (implied TRUNCATION) is the rounding: the representable
      *> value nearer to zero.
      *>
      *> EXPECTED OUTPUT, DERIVED:
      *>  B1  HUGE is 1.0E+300; FLOAT-SHORT (binary32) tops out near
      *>      3.4E+38, so the landing is case 3 => EC-SIZE-TRUNCATION, the
      *>      callee is NOT entered (no S= line), the USE declarative runs,
      *>      and RESUME AT NEXT STATEMENT continues after the CALL.
      *>  B2  OKV is 1.5, exactly representable: nothing raised, S=IN-RANGE.
      *>  B3  checking is OFF again: the no-phrase disposition (CONFORMANCE
      *>      DOC-A.1-70) lands the value nearer to zero, the largest finite
      *>      binary32 - finite, above 1.0E+38 and below 1.0E+39:
      *>      S=MAX-FINITE (an infinity would fail the upper bound).
       >>TURN EC-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114CK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 HUGE USAGE FLOAT-LONG VALUE 1.0E+300.
       01 OKV USAGE FLOAT-LONG VALUE 1.5.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-SIZE.
       H-P.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           DISPLAY "B1"
           CALL "PB1114CS" AS NESTED USING BY CONTENT HUGE
           DISPLAY "B2"
           CALL "PB1114CS" AS NESTED USING BY CONTENT OKV
           DISPLAY "B3"
       >>TURN EC-SIZE CHECKING OFF
           CALL "PB1114CS" AS NESTED USING BY CONTENT HUGE
           DISPLAY "B4"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1114CS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK USAGE FLOAT-SHORT.
       PROCEDURE DIVISION USING LK.
       M1.
           EVALUATE TRUE
               WHEN LK > 1.0E+38 AND LK < 1.0E+39
                   DISPLAY "S=MAX-FINITE"
               WHEN LK = 1.5
                   DISPLAY "S=IN-RANGE"
               WHEN OTHER
                   DISPLAY "S=OTHER"
           END-EVALUATE
           GOBACK.
       END PROGRAM PB1114CS.
       END PROGRAM PB1114CK.
