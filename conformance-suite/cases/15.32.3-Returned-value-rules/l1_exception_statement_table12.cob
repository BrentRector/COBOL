      *>--- conformance-case
      *> id: l1_exception_statement_table12
      *> rules: [RV-15.32.3-2, RV-15.32.3-3, RV-15.33.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.32.3 r3 - "The names of the statements are given in
      *> Table 12, Procedural statements, in the column labeled
      *> 'Statement name'." Table 12 (§14.5.1) has 50 rows and GO TO is
      *> its ONLY multi-word statement name, so that row is the one case
      *> separating a name taken from TABLE 12 from the tokens the
      *> source happens to spell. In the §14.9.17.2 format-2 diagram GO
      *> is underlined and TO is not, so TO is an OPTIONAL word: `GO P-A
      *> P-B DEPENDING ON ...` is a GO TO statement whose source
      *> contains no TO at all, and r3 still requires the name "GO TO".
      *> The underlining was read off the PRINTED page, not the
      *> transcription (render-spec-page.py 660 = printed folio 630):
      *> both formats print GO underlined with TO plain, and format 2
      *> prints DEPENDING underlined with ON plain.
      *>
      *> §15.32.3 r2 fixes the shape - "a 63-character alphanumeric
      *> character-string that is the name of the statement that caused
      *> the exception condition to be raised in uppercase letters,
      *> left-justified and space-filled on the right": GO TO is 5
      *> characters (58 trailing spaces), MOVE 4 (59), RAISE 5 (58).
      *>
      *> r2's UPPERCASE clause, and the same clause in §15.33.3 r1 ("All
      *> letters in the exception-name are returned as uppercase
      *> letters"), are pinned by the LOWERCASE sentence `raise
      *> exception ec-user-lo.` - §8.1.3.2 GR3 a): "COBOL basic letters
      *> appearing elsewhere within the compilation group are treated in
      *> a case-insensitive manner" - and both the statement name and
      *> the exception-name still come back uppercase.
      *>
      *> Which line pins which rule:
      *>   U-T        §15.32.3 r2+r3 - RAISE, uppercase-source control
      *>   L-T        §15.32.3 r2+r3 - RAISE, from LOWERCASE source
      *>   L-S        §15.33.3 r1    - EC-USER-LO uppercased, 31 wide
      *>   H-T (1st)  §15.32.3 r3    - MOVE
      *>   H-T (2nd)  §15.32.3 r3    - GO TO, the multi-word row
      *>
      *> Machinery: both >>TURN directives carry WITH LOCATION, which is
      *> what selects r2 over r1. EC-BOUND-SUBSCRIPT (§8.4.2.3.4 GR2 -
      *> a subscript "greater than the highest permissible occurrence
      *> number"; NOT GR1 b), which sets the same condition only for a
      *> non-integer arithmetic-expression subscript) is fatal in
      *> Table 13, so §14.6.13.1.3 5) runs the USE
      *> declarative and RESUME AT NEXT STATEMENT (§14.9.33.4 2) a) -
      *> "control is transferred to an implicit CONTINUE statement"
      *> after the raising statement) keeps the run unit alive past each
      *> raise; the subscript 7 on a 3-occurrence table raises inside
      *> the MOVE and then inside the GO TO's DEPENDING operand.
      *> EC-USER-* is nonfatal (§14.6.13.1.1), so the two RAISE legs
      *> need no handler.
       >>TURN EC-USER CHECKING ON WITH LOCATION
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ESTB12.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-T.
          05 WS-E PIC 9 OCCURS 3 TIMES.
       01 WS-I PIC 9 VALUE 7.
       01 WS-R PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       H-P.
           DISPLAY "H-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           RAISE EXCEPTION EC-USER-UP.
           DISPLAY "U-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           raise exception ec-user-lo.
           DISPLAY "L-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           DISPLAY "L-S=[" FUNCTION EXCEPTION-STATUS "]".
           MOVE WS-E (WS-I) TO WS-R.
           GO P-A P-B DEPENDING ON WS-E (WS-I).
           DISPLAY "AFTER".
           STOP RUN.
       P-A.
           DISPLAY "P-A".
           STOP RUN.
       P-B.
           DISPLAY "P-B".
           STOP RUN.
