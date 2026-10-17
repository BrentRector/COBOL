*>--- conformance-case
*> id: pb695_value_false_optional_words
*> rules: [SR-13.18.63.3-31, FMT-13.18.63.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE VALUE CLAUSE'S FORMAT-3 OPTIONAL WORDS, AND ITS TWO TRAILING BRACKETS IN PRINTED ORDER
      *> (kb/Work PB695 family 2 - the sibling sweep, invisible to the optional-word audit because a word
      *> REQUIRED INSIDE an optional group never reaches its candidate list).
      *> ISO 13.18.63.2 format 3, read off the printed page (PDF p546 / folio 516):
      *>     { VALUE IS | VALUES ARE } { literal-2 [ {THROUGH|THRU} literal-3 ] } ... [ IN alphabet-name-1 ]
      *>     [ WHEN SET TO FALSE IS literal-4 ]
      *> The only rules on that bracket sit under FALSE (x 228.02-255.10, 92% cover); WHEN, SET, TO and IS
      *> carry none, and the transcription's own figure note says the same. 8.3.2.4.3 therefore makes
      *> `88 CN VALUE 1 FALSE 0` the same clause as `88 CN VALUE 1 WHEN SET TO FALSE 0`.
      *> The SECOND defect the same measurement found: the printed format ends `... [ IN alphabet-name-1 ]`
      *> and puts `[ WHEN SET TO FALSE IS literal-4 ]` on the LINE AFTER it (format 5 likewise brackets IN
      *> before its {IS INVALID | ARE VALID} tail), and the grammar had the two the other way round - so
      *> `VALUE 1 IN AL1 WHEN SET TO FALSE 0`, the PRINTED spelling, was a COBOL0001 while an order no
      *> format prints was accepted. 5.2.6.2 makes bracket order part of the format.
      *> Expected values. 8.8.4.5.1 tests a conditional variable "to determine whether or not its value is
      *> equal to one of the values associated with condition-name-1", so with C1 = 1 both
      *> CN-BARE and CN-ORDER are true and CN-RANGE (2 THRU 3) is false; with C1 = 3 CN-RANGE is true and
      *> CN-BARE is false. Literal-4 is the value 13.18.63.4 GR20 places in the conditional variable for
      *> `SET condition-name TO FALSE`. Only its SPELLING is under test HERE - the GR20 store itself is
      *> conformance-suite/cases/13.18.63-VALUE/pb555_value_false_phrase's subject (kb/Work PB555, which bound literal-4;
      *> this file predates that and used to say the phrase was accepted-inert). The three literal-4 values
      *> written below are all OUTSIDE their condition's VALUE set, so 13.18.63.3 SR27 is satisfied and this
      *> program stays a POSITIVE case: 0 against VALUE 1, 0 against VALUES 2 THRU 3, "Z" against
      *> "A" THRU "C".
      *> CN-ORDER's subject is C2, NOT C1, and its operand is an ALPHANUMERIC THROUGH RANGE, because
      *> 13.18.63.3 SR31 admits the IN phrase "only when the literals specified in the THROUGH phrase are
      *> of class alphanumeric or national" (kb/Work PB890). This line used to read
      *> `88 CN-ORDER VALUE 1 IN AL1 WHEN SET TO FALSE 0.` -- a numeric SINGLETON with IN, which the rule
      *> excludes twice over -- and a guard in DataBinder.BindCondition skipped the screen to keep it
      *> green. The BRACKET ORDER this program exists to pin is unchanged: IN alphabet-name-1 first, the
      *> WHEN SET TO FALSE bracket second. ORDER1 is still `yes` and the expectation is DERIVED, not
      *> measured: AL1 IS STANDARD-1, so 14.7.8 rule 2 orders "A" < "B" < "C" and C2 = "B" is inside the
      *> range; SR26's ascending-endpoint requirement is satisfied by the same ordering.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695VALFW.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 IS STANDARD-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C1              PIC 9 VALUE 1.
           88  CN-BARE     VALUE 1 FALSE 0.
           88  CN-RANGE    VALUES ARE 2 THRU 3 FALSE 0.
       01  C2              PIC X VALUE "B".
           88  CN-ORDER    VALUE "A" THRU "C" IN AL1 WHEN SET TO FALSE "Z".
       PROCEDURE DIVISION.
       MAIN-P.
           IF CN-BARE DISPLAY "BARE1=yes" ELSE DISPLAY "BARE1=no" END-IF
           IF CN-RANGE DISPLAY "RANGE1=yes" ELSE DISPLAY "RANGE1=no" END-IF
           IF CN-ORDER DISPLAY "ORDER1=yes" ELSE DISPLAY "ORDER1=no" END-IF
           MOVE 3 TO C1
           IF CN-BARE DISPLAY "BARE3=yes" ELSE DISPLAY "BARE3=no" END-IF
           IF CN-RANGE DISPLAY "RANGE3=yes" ELSE DISPLAY "RANGE3=no" END-IF
           DISPLAY "DONE"
           STOP RUN.
