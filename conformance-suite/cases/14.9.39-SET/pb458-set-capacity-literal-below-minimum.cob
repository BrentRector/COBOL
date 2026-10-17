*>--- conformance-case
*> id: pb458-set-capacity-literal-below-minimum
*> rules: [SR-14.9.39.3-30]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.39.3 SR30 - "Integer-1 shall be nonnegative and, if TO is specified, integer-1 shall be not
      *> less than the minimum capacity defined in the corresponding OCCURS clause and not greater than the
      *> expected capacity, if specified." 13.18.38.4 GR16 names the minimum capacity ("Integer-4 is the minimum
      *> capacity of the table") and GR17 the expected capacity ("Integer-5 is the expected capacity"), so all
      *> three of SR30's bounds are decidable at compile time from FROM 2 TO 10.
      *> This is a SYNTAX rule over Format 14's LITERAL alternative, so a violation is a refusal. 14.9.39.4
      *> GR29/GR30's run-time condition and minimum-capacity clamp are the rules for arithmetic-expression-4 and
      *> are untouched.
      *> ⛔ The clamp used to stand in for the screen: SET WS-CAP TO 1 compiled, ran, and answered a capacity of
      *> 2 - a DIFFERENT value from the one written, for a program the standard requires the compiler to reject.
      *> kb/Work PB458.
      *> Rejected from 2014: OCCURS DYNAMIC and Format 14 are COBOL-2014 introductions, so below 2014 the OCCURS
      *> clause itself takes the edition-band code and SR30 is not the subject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB458N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-CAP TO 1
           DISPLAY WS-CAP
           STOP RUN.
