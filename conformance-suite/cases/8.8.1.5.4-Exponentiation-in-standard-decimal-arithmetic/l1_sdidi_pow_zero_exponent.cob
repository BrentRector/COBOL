*>--- conformance-case
*> id: l1_sdidi_pow_zero_exponent
*> rules: [GR-8.8.1.5.4-1, GR-8.8.1.5.4-4, GR-14.7.5-1, GR-14.7.5-L3.1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.1.5.4 1 and §8.8.1.5.4 4 — exponentiation at a ZERO exponent under STANDARD-DECIMAL.
      *> ⚠ CLAUSE IDENTITY: §8.8.1.4.4 (standard-BINARY exponentiation) carries rules 1 and 4 in VERBATIM the
      *> same words.  The rules pinned here are the §8.8.1.5.4 ones, which is why the program declares
      *> ARITHMETIC IS STANDARD-DECIMAL — the ONE thing that selects this clause over its twin.
      *> Nothing in the corpus wrote `** 0` under standard-decimal: conformance:2023/pb18_native_power_exact_
      *> and_rule6 exercises 5 ** 0 and 0 ** 0 but declares no OPTIONS paragraph, so it is native arithmetic
      *> (§8.8.1.3) and closes §8.8.1.2 6 a on the native arm only.
      *>
      *> r1 — "When operand-2 is zero and operand-1 is other than zero, the result shall be equivalent to the
      *> evaluation of the arithmetic expression", that expression being the display paragraph (1) that
      *> follows the sentence in the standard — "(1)" is a paragraph of its own, NOT part of the quotable rule
      *> text, and a quotation that splices it onto the sentence does not pass cite.py --check.  Exactly one,
      *> then; r1 states no restriction on operand-1's
      *> SIGN or SCALE, and §8.8.1.5.2 NOTE 1 ("An SDIDI can contain the unique values +0 and -0.  For purposes
      *> of numeric processing and sign tests in COBOL, both values are treated as the unique value 0") makes
      *> every spelling of a zero exponent the same operand-2.  The six r1 legs vary exactly those axes: a
      *> positive base, a NEGATIVE base, a fractional base, a zero exponent held in a signed data item, a
      *> zero exponent written as a scaled literal, and a zero exponent written as a floating-point literal.
      *> The negative base is admitted, not excluded: §8.8.1.2 6 c requires only that "the evaluation of the
      *> exponent shall result in an integer" when the base is less than zero, and 0 is an integer.
      *> The relation leg compares the SDIDI intermediate against the literal 1 directly — r1's "equivalent to
      *> the evaluation of the arithmetic expression" — with (1) as that expression — is an EXACT claim, not a
      *> rounded one.
      *>
      *> r4 — "When both operand-1 and operand-2 are equal to zero, the EC-SIZE-EXPONENTIATION exception
      *> condition is set to exist."  §8.8.1.2 6 a, which the clause preamble applies "regardless of the mode
      *> of arithmetic that is in effect", adds "the size error condition is raised" for the same operands, so
      *> §14.7.5's SIZE-ERROR-phrase rules apply: rule 1 says "the values of all of the resultant data items
      *> remain unchanged from the values they had at the start of the execution of the arithmetic statement"
      *> and rule 3 transfers control to the phrase.  The receiver therefore still reads 7.  FUNCTION
      *> EXCEPTION-STATUS answers with the condition's own name — §15.33.3 1: "A 31-character, left-justified,
      *> alphanumeric character string that is the exception-name" and "all unused characters are alphanumeric
      *> spaces" — so the bracketed field is 22 name characters and 9 spaces.
      *> The WHEN leg observes r4's own words (the exception CONDITION being set) through an exception-name
      *> dispatch rather than through §8.8.1.2 6 a's size error condition, and it is a separate statement with
      *> no SIZE ERROR phrase because §14.7.5 says that when one is specified "no statements in an applicable
      *> WHEN phrase in a containing PERFORM statement are executed".
      *>
      *> THE ARMED COMPLEMENT (last two legs): a zero base with a POSITIVE exponent SATISFIES §8.8.1.2 6 a's
      *> precondition, r4 does not apply (operand-2 is not zero), and §8.8.1.5.4 2 c makes 0 ** 3 the exact
      *> ((0 * 0) * 0) = 0 with no size error.  Without this the raises above would be no evidence that the
      *> gate discriminates — an implementation that rejected every zero base would look identical.
      >>TURN EC-SIZE-EXPONENTIATION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SDP01.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L1P-R   PIC 9V9(4).
       01 L1P-K   PIC 9(4).
       01 L1P-POS PIC S9(4)V9(4) VALUE 7.5.
       01 L1P-NEG PIC S9(4)V9(4) VALUE -3.25.
       01 L1P-ZE  PIC S9(4)V9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE L1P-R = L1P-POS ** 0.
           DISPLAY "POSBASE=" L1P-R.
           COMPUTE L1P-R = L1P-NEG ** 0.
           DISPLAY "NEGBASE=" L1P-R.
           COMPUTE L1P-R = 0.125 ** 0.
           DISPLAY "FRACBAS=" L1P-R.
           COMPUTE L1P-R = L1P-POS ** L1P-ZE.
           DISPLAY "ZEROITM=" L1P-R.
           COMPUTE L1P-R = L1P-POS ** 0.000.
           DISPLAY "ZSCALED=" L1P-R.
           COMPUTE L1P-R = L1P-POS ** 0.0E+0 END-COMPUTE.
           DISPLAY "ZFLOATE=" L1P-R.
           IF L1P-POS ** 0 = 1
               DISPLAY "EXACT1 =EQ"
           ELSE
               DISPLAY "EXACT1 =NE"
           END-IF.
           MOVE 7 TO L1P-K.
           COMPUTE L1P-K = 0 ** 0
               ON SIZE ERROR DISPLAY "R4PHRSE=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON SIZE ERROR DISPLAY "R4PHRSE=NO-SIZE-ERROR"
           END-COMPUTE.
           DISPLAY "R4RECVR=" L1P-K.
           SET LAST EXCEPTION TO OFF.
           PERFORM
               COMPUTE L1P-K = L1P-ZE ** L1P-ZE
           WHEN EC-SIZE-EXPONENTIATION
               DISPLAY "R4WHEN =[" FUNCTION EXCEPTION-STATUS "]"
               RESUME AT NEXT STATEMENT
           END-PERFORM.
           MOVE 7 TO L1P-K.
           COMPUTE L1P-K = 0 ** 3
               ON SIZE ERROR DISPLAY "COMPLMT=SIZE-ERROR"
               NOT ON SIZE ERROR DISPLAY "COMPLMT=NO-SIZE-ERROR"
           END-COMPUTE.
           DISPLAY "COMRECV=" L1P-K.
           STOP RUN.
