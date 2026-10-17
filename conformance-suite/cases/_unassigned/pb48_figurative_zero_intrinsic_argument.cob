*>--- conformance-case
*> id: pb48_figurative_zero_intrinsic_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.4 GR4 - "The zero format represents the numeric value '0',
      *> one or more of the boolean character '0', or one or more of the character
      *> '0' in the computer's runtime coded character set, DEPENDING ON CONTEXT."
      *> GR1 fixes which character reading applies: "when a figurative constant
      *> represents a character value, the figurative constant represents an
      *> alphanumeric character value ... the character value representation of the
      *> figurative constant ZERO (ZEROS, ZEROES) ... is the value of the character
      *> '0'". 8.4.3.2.3 SR8 admits a LITERAL as argument-1 and 8.3.3.6.3 SR1 admits
      *> a figurative constant "whenever 'literal' appears in a format or when a
      *> rule allows it" - so a bare ZERO argument is legal to EVERY function below,
      *> and which of GR4's readings applies is decided by the function's own 15.3
      *> argument type. THE CONTEXT DECIDES; the token cannot.
      *>
      *> IT WAS DECIDED BY A TOKEN (fix-queue PB48). ZeroTokenRewriter converts ZERO
      *> to the arithmetic ZERO_ARITH whenever it is adjacent to '(' or ')', and an
      *> argument list is delimited by exactly those characters - so every bare ZERO
      *> argument reached the binder as class NUMERIC before any function was known.
      *> FUNCTION LOWER-CASE(ZERO) was REJECTED as a class error and
      *> FUNCTION NUMVAL(ZERO) aborted at RUN TIME. The lexer now types the
      *> argument-list parens FNARG_LPAREN/FNARG_RPAREN (8.4.3.2.3 SR6 - that '(' is
      *> ALWAYS the argument list, never a grouping paren), so the rewriter's own
      *> rule about ARITHMETIC parens became true as written.
      *>
      *> THE TWO ARMS DISAGREED, WHICH IS HOW THE ANSWER WAS KNOWN IN ADVANCE. The
      *> keyword-omitted form (8.4.3.2.3 SR2) re-lexes its arguments through
      *> FunctionArgFragment, where there are no parens to be adjacent to, so
      *> LOWER-CASE(ZERO) already returned "0" while FUNCTION LOWER-CASE(ZERO) was
      *> rejected. Both forms are the SAME reference and are asserted equal below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB48FIGZERO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-1  PIC X    VALUE "Z".
       01 W-4  PIC X(4) VALUE "ZZZZ".
       01 W-N  PIC 9(4) VALUE 0.
       01 W-S  PIC S9(4)V99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> ── THE CHARACTER READING (GR1 + GR4). A 15.3 type-2 alphanumeric argument
      *> ── is a character context, so ZERO is the one character '0' (GR3b fixes the
      *> ── length at one, since a bare argument specifies none).
           MOVE FUNCTION LOWER-CASE(ZERO) TO W-1.
           DISPLAY "01-LOWER-ZERO=[" W-1 "]".
           MOVE FUNCTION UPPER-CASE(ZERO) TO W-1.
           DISPLAY "02-UPPER-ZERO=[" W-1 "]".
           MOVE FUNCTION REVERSE(ZERO) TO W-1.
           DISPLAY "03-REVERSE-ZERO=[" W-1 "]".
           MOVE FUNCTION TRIM(ZERO) TO W-1.
           DISPLAY "04-TRIM-ZERO=[" W-1 "]".
      *> 15.70.4 - ORD returns the ordinal position in the collating sequence, and
      *> "the lowest ordinal position is 1". '0' is position 48 in the native set,
      *> so its ordinal is 49. This is the assertion that the argument really is the
      *> CHARACTER '0' and not the number 0 that happened to move correctly.
           MOVE FUNCTION ORD(ZERO) TO W-N.
           DISPLAY "05-ORD-ZERO=" W-N.
      *> NUMVAL (15.68.3 r1) takes an alphanumeric/national argument and returns its
      *> numeric value - so ZERO is the STRING "0" here and the value is 0. Both
      *> readings appear in this one call, which is why it is the sharpest case.
           MOVE FUNCTION NUMVAL(ZERO) TO W-S.
           DISPLAY "06-NUMVAL-ZERO=" W-S.

      *> ── GR3 LENGTHS. (b) a figurative other than ALL literal-1 is ONE character;
      *> ── (c) otherwise the length of literal-1. 15.50.3 r1 and 15.14.3 r1 both
      *> ── admit "an alphanumeric ... literal", which GR1 makes this.
           MOVE FUNCTION LENGTH(ZERO) TO W-N.
           DISPLAY "07-LENGTH-ZERO=" W-N.
           MOVE FUNCTION BYTE-LENGTH(ZERO) TO W-N.
           DISPLAY "08-BYTELEN-ZERO=" W-N.
      *> BYTE-LENGTH kept PB25's defect one method away from where PB25 fixed it:
      *> its default arm named "a numeric/figurative literal" as invalid, so
      *> BYTE-LENGTH(SPACE) aborted at run time on source 15.14.3 r1 admits.
           MOVE FUNCTION BYTE-LENGTH(SPACE) TO W-N.
           DISPLAY "09-BYTELEN-SPACE=" W-N.
           MOVE FUNCTION BYTE-LENGTH(ALL "AB") TO W-N.
           DISPLAY "10-BYTELEN-ALL2=" W-N.

      *> ── THE NUMERIC READING (GR4 first clause; 8.8.1.1 lists the figurative
      *> ── constant ZERO among the operands an arithmetic expression may be built
      *> ── from). A 15.3 type-6/type-10 argument is a numeric context.
           MOVE FUNCTION ABS(ZERO) TO W-N.
           DISPLAY "11-ABS-ZERO=" W-N.
           MOVE FUNCTION SQRT(ZERO) TO W-N.
           DISPLAY "12-SQRT-ZERO=" W-N.
           MOVE FUNCTION INTEGER(ZERO) TO W-N.
           DISPLAY "13-INTEGER-ZERO=" W-N.
      *> 15.36.4 - FACTORIAL(0) is 1.
           MOVE FUNCTION FACTORIAL(ZERO) TO W-N.
           DISPLAY "14-FACTORIAL-ZERO=" W-N.
           MOVE FUNCTION REM(ZERO 5) TO W-N.
           DISPLAY "15-REM-ZERO-5=" W-N.

      *> ── THE MIXED LIST. 15.59.3 r2: "All arguments shall be of the same class",
      *> ── so the arguments that HAVE a class are the context GR4 defers to. With a
      *> ── numeric partner ZERO is numeric; with an alphanumeric partner it is the
      *> ── character '0', and 15.59.4 r1 compares by 8.8.4.2 relation rules - "A"
      *> ── (65) exceeds "0" (48). Returning "0" here was a WRONG ANSWER, not a
      *> ── crash: the body choice and the result type were two separate arms and
      *> ── only one of them had ever been asked about figurative constants.
           MOVE FUNCTION MAX(ZERO 5) TO W-N.
           DISPLAY "16-MAX-ZERO-5=" W-N.
           MOVE FUNCTION MIN(ZERO 5) TO W-N.
           DISPLAY "17-MIN-ZERO-5=" W-N.
           MOVE FUNCTION MAX(ZERO "A") TO W-1.
           DISPLAY "18-MAX-ZERO-A=[" W-1 "]".
           MOVE FUNCTION MIN(ZERO "A") TO W-1.
           DISPLAY "19-MIN-ZERO-A=[" W-1 "]".
      *> SPACE has only the character reading (GR5), so it forces the string
      *> comparison on its own. This aborted at run time before PB48.
           MOVE FUNCTION MAX(SPACE "A") TO W-1.
           DISPLAY "20-MAX-SPACE-A=[" W-1 "]".
           MOVE FUNCTION MIN(SPACE "A") TO W-1.
           DISPLAY "21-MIN-SPACE-A=[" W-1 "]".
      *> Every argument neutral: no context to defer to, so GR4's first-listed
      *> numeric value stands (and 8.8.1.1 reads it the same way).
           MOVE FUNCTION MAX(ZERO ZERO) TO W-N.
           DISPLAY "22-MAX-ZERO-ZERO=" W-N.

      *> ── THE TWO REFERENCE FORMS ARE ONE REFERENCE (8.4.3.2.3 SR2). These are the
      *> ── two arms that disagreed; they must now agree, value for value.
           MOVE LOWER-CASE(ZERO) TO W-1.
           DISPLAY "23-OMIT-LOWER-ZERO=[" W-1 "]".
           MOVE NUMVAL(ZERO) TO W-S.
           DISPLAY "24-OMIT-NUMVAL-ZERO=" W-S.
           MOVE LENGTH(ZERO) TO W-N.
           DISPLAY "25-OMIT-LENGTH-ZERO=" W-N.

      *> ── THE CONTROLS. A grouping paren INSIDE an argument list is still an
      *> ── ordinary LPAREN, and ZERO adjacent to an arithmetic operator is still
      *> ── rewritten - so every arithmetic reading below is unchanged by PB48.
           COMPUTE W-N = FUNCTION ABS(ZERO + 1).
           DISPLAY "26-ABS-ZERO-PLUS1=" W-N.
           COMPUTE W-N = FUNCTION ABS((ZERO)).
           DISPLAY "27-ABS-PAREN-ZERO=" W-N.
           COMPUTE W-N = FUNCTION MAX((ZERO + 2) 1).
           DISPLAY "28-MAX-GROUPED=" W-N.
           COMPUTE W-N = (ZERO + 3) * 2.
           DISPLAY "29-GROUPED-ARITH=" W-N.
      *> 8.4.3.2.3 SR6's precondition is "if a function's definition PERMITS
      *> arguments" - a catalog question - so the paren after a ZERO-ARGUMENT
      *> function name is a REFERENCE MODIFIER even though the lexer types it as an
      *> argument-list paren. 15.21.3 fixes CURRENT-DATE at 21 positions, so (1:8)
      *> is 8. This is the PB8 shape and it must survive the new token types.
           COMPUTE W-N = FUNCTION LENGTH(FUNCTION CURRENT-DATE (1:8)).
           DISPLAY "30-REFMOD-ZEROARG=" W-N.
      *> ⚠ The ref-mod positions ARE arithmetic expressions (8.4.3.3.3 SR4), so the
      *> rewriter now counts the COLON as arithmetic context and a ZERO written in a
      *> position still parses. That is asserted in CobolLexerModeDriftTests and not
      *> here, deliberately: a ref-modified FUNCTION RESULT whose position is an
      *> arithmetic expression rather than a literal is a PRE-EXISTING loud stage
      *> (measured on the pre-PB48 tree, and filed as its own defect note), so this
      *> golden could only have asserted the crash.
           STOP RUN.
