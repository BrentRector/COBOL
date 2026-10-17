*>--- conformance-case
*> id: pb25_figurative_intrinsic_argument
*> rules: [RV-15.57.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.3 SR1 - "A figurative constant may be used whenever 'literal'
      *> appears in a format or when a rule allows it", and 8.4.3.2.3 SR8 makes
      *> argument-1 "an identifier, a literal, a boolean expression, or an
      *> arithmetic expression". So FUNCTION LOWER-CASE(SPACE) is LEGAL SOURCE.
      *>
      *> IT COMPILED CLEAN AND ABORTED AT RUN TIME (fix-queue PB25) with
      *> "intrinsic string argument 'BoundFigurative'". IntrinsicRenderer's
      *> string-argument visitor rendered EmitText.LoudValue for BoundFigurative
      *> and BoundAllLiteral - even though OperandText.AsString, which the visitor's
      *> own BoundFieldOperand arm already delegates to, had implemented the right
      *> answer all along and cited the rule in its comments. One rule written in
      *> two places, and the second copy was a loud stage.
      *>
      *> THE LENGTHS ARE 8.3.3.6.4 GR3's, the "length of the string is NOT specified
      *> in the rules for the context" case - which is exactly a bare function
      *> argument, since nothing there fixes a size:
      *>   (b) a figurative constant other than ALL literal-1 is ONE character;
      *>   (c) otherwise, the length of literal-1.
      *> That is why SPACE is one space and ALL "AB" is the two characters "AB",
      *> rather than either being padded to the receiving item.
      *>
      *> FUNCTION LENGTH HAD ITS OWN COPY OF THE MISTAKE, in the binder rather than
      *> the renderer: one match arm refused "a numeric/figurative literal" and its
      *> comment asserted the rejection was spec-correct. Half of it was. 15.50.3 r1
      *> admits "an alphanumeric, national, or boolean literal", which excludes a
      *> NUMERIC literal and says nothing about figurative constants - and
      *> 8.3.3.6.4 GR1 makes SPACE an ALPHANUMERIC character value here, exactly
      *> what r1 permits. One code standing for two rules enforced neither.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB25FIGARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1  PIC X    VALUE "Z".
       01 W-4  PIC X(4) VALUE "ZZZZ".
       01 W-N  PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> GR3b - a figurative other than ALL literal-1 is ONE character. SPACE has
      *> no uppercase letters, so LOWER-CASE returns the one space unchanged.
           MOVE FUNCTION LOWER-CASE(SPACE) TO W-1.
           DISPLAY "1-LOWER-SPACE=[" W-1 "]".
           MOVE FUNCTION UPPER-CASE(SPACE) TO W-1.
           DISPLAY "2-UPPER-SPACE=[" W-1 "]".
      *> QUOTE is the '"' character (8.3.3.6.4 GR1), likewise one character.
           MOVE FUNCTION LOWER-CASE(QUOTE) TO W-1.
           DISPLAY "3-LOWER-QUOTE=[" W-1 "]".
      *> GR3c - ALL literal-1 is the LENGTH OF LITERAL-1, so two characters here,
      *> NOT one and NOT padded to the receiver.
           MOVE FUNCTION LOWER-CASE(ALL "AB") TO W-4.
           DISPLAY "4-LOWER-ALL=[" W-4 "]".
           MOVE FUNCTION UPPER-CASE(ALL "ab") TO W-4.
           DISPLAY "5-UPPER-ALL=[" W-4 "]".
      *> REVERSE over the same GR3c string - proves the argument really is two
      *> characters rather than one that happened to move correctly.
           MOVE FUNCTION REVERSE(ALL "AB") TO W-4.
           DISPLAY "6-REVERSE-ALL=[" W-4 "]".
      *> FUNCTION LENGTH: the GR3 lengths, read directly. These are the values that
      *> distinguish GR3b from GR3c, so they are the assertion that the rule - and
      *> not merely "it no longer crashes" - is implemented.
           MOVE FUNCTION LENGTH(SPACE) TO W-N.
           DISPLAY "7-LENGTH-SPACE=" W-N.
           MOVE FUNCTION LENGTH(ALL "ABC") TO W-N.
           DISPLAY "8-LENGTH-ALL3=" W-N.
           MOVE FUNCTION LENGTH(QUOTE) TO W-N.
           DISPLAY "9-LENGTH-QUOTE=" W-N.
      *> The controls: an ordinary literal and an ordinary data item, which always
      *> worked, so a regression in the shared path shows up here.
           MOVE FUNCTION LOWER-CASE("AB") TO W-4.
           DISPLAY "10-LOWER-LITERAL=[" W-4 "]".
           MOVE FUNCTION LENGTH("ABCD") TO W-N.
           DISPLAY "11-LENGTH-LITERAL=" W-N.
           STOP RUN.
