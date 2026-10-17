*>--- conformance-case
*> id: pb238_call_format2_operands
*> rules: [SR-14.9.4.3-17, SR-14.9.4.3-23, GR-14.9.4.4-8, GR-14.9.4.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB238 - ISO 14.9.4.4 GR8: "An argument that consists merely of a single identifier or
      *> literal is regarded as an identifier or literal rather than an arithmetic or boolean expression."
      *> ONE reduction, every Format-2 argument spelling, plus the two channels that had no arm at all.
      *> EXPECTED VALUES, DERIVED:
      *>  S1 (twice) - F is FLOAT-LONG 1.5. GR8 makes the bare `F` identifier-4, NOT arithmetic-expression-1;
      *>    `F + 0` IS arithmetic-expression-1. Both cross to a BY VALUE `PIC S9(3)V99` formal, where
      *>    14.2.3 GR10 makes the transfer "a COMPUTE statement without the ROUNDED phrase" => +001.50.
      *>    MOVE to PIC ZZ9.99 suppresses the two leading zeros => "  1.50". THE TWO SPELLINGS MUST AGREE:
      *>    before this fix BOTH were narrowed to the integer lane at the CALLER and arrived as 001.00.
      *>  S3 - K CONSTANT AS 42 substitutes the literal 42 (13.10.4 GR1/GR2), so `USING K` is a
      *>    bare literal-2 (GR8; the admission is 13.10.3 SR2 - a constant-name "may be used anywhere that a
      *>    format specifies a literal"). A literal never meets 14.9.4.3 SR3, so 14.9.4.4 GR9 a)2 assumes
      *>    BY CONTENT; 14.2.3 GR9's allocated record takes the formal's own description with the argument as
      *>    a COMPUTE sending operand => +0042; MOVE to PIC ZZZ9 => "  42". It used to draw COBOLNET1548
      *>    ("constant-name shall not be specified as a receiving operand") - legal source, refused.
      *>  S4 - boolean-expression-1, the third of SR17's operand set. B"1100" B-AND B"1010" is bit-wise
      *>    conjunction (8.8.2) => B"1000"; 8.8.2 rule 10 fixes the value length at the largest boolean
      *>    ITEM referenced (4), and the formal is PIC 1(4), so DISPLAY shows "1000". The whole channel
      *>    used to be refused outright at bind.
      *>  S6 - `USING "AB" B1 B-AND B2` is TWO arguments, and the first one lands in a booleanExpression
      *>    node on the SECOND one's B-operator (the {boolExprAhead()}? predicate's scan runs to the
      *>    statement's period). GR8 reduces it back: an operator-free booleanExpression IS its bare
      *>    valueOperand, whose two legs are `arithmeticExpression | nonNumericLiteral` - so "AB" is
      *>    literal-2 and crosses to the PIC X(2) formal => "AB", beside B"1000" for the second. Recovering
      *>    only the arithmetic leg left the literal classified as nothing and staged a RUN-TIME
      *>    NotImplemented ("CALL USING argument '\"AB\"'") on conforming source.
      *>  S5 - `USING 7` with a BY VALUE formal: 14.9.4.4 GR9 b) assumes BY VALUE (the mode is derived from
      *>    the formal, not left to the copy-out), 14.9.4.3 SR23 is satisfied because 7 is a numeric
      *>    literal, and GR10's COMPUTE gives +0007; MOVE to PIC ZZZ9 => "   7".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238OPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F USAGE FLOAT-LONG VALUE 1.5.
       01 K CONSTANT AS 42.
       01 B1 PIC 1(4) USAGE BIT VALUE B"1100".
       01 B2 PIC 1(4) USAGE BIT VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB238S1" AS NESTED USING BY VALUE F
           CALL "PB238S1" AS NESTED USING BY VALUE F + 0
           CALL "PB238S3" AS NESTED USING K
           CALL "PB238S4" AS NESTED USING BY CONTENT B1 B-AND B2
           CALL "PB238S5" AS NESTED USING 7
           CALL "PB238S6" AS NESTED USING "AB" B1 B-AND B2
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC ZZ9.99.
       LINKAGE SECTION.
       01 LN1 PIC S9(3)V99.
       PROCEDURE DIVISION USING BY VALUE LN1.
       M1.
           MOVE LN1 TO R
           DISPLAY "S1=" R
           GOBACK.
       END PROGRAM PB238S1.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238S3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R3 PIC ZZZ9.
       LINKAGE SECTION.
       01 LK PIC S9(4).
       PROCEDURE DIVISION USING BY REFERENCE LK.
       M3.
           MOVE LK TO R3
           DISPLAY "S3=" R3
           GOBACK.
       END PROGRAM PB238S3.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238S4.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING BY REFERENCE LB.
       M4.
           DISPLAY "S4=" LB
           GOBACK.
       END PROGRAM PB238S4.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238S5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R5 PIC ZZZ9.
       LINKAGE SECTION.
       01 L5 PIC S9(4).
       PROCEDURE DIVISION USING BY VALUE L5.
       M5.
           MOVE L5 TO R5
           DISPLAY "S5=" R5
           GOBACK.
       END PROGRAM PB238S5.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238S6.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LT PIC X(2).
       01 LB6 PIC 1(4) USAGE BIT.
       PROCEDURE DIVISION USING BY REFERENCE LT LB6.
       M6.
           DISPLAY "S6=" LT " " LB6
           GOBACK.
       END PROGRAM PB238S6.
       END PROGRAM PB238OPS.
