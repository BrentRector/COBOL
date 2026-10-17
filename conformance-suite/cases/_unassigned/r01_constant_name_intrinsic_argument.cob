*>--- conformance-case
*> id: r01_constant_name_intrinsic_argument
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.10.4 GR1: "the effect of specifying constant-name-1 in other than
      *> this entry is as if literal-1 ... were written where constant-name-1 is
      *> written", and 13.10.3 SR2 admits it "anywhere that a format specifies a
      *> literal of the class and category of constant-name-1".
      *>
      *> An intrinsic ARGUMENT is such a position for whatever class the
      *> function's own 15.x argument rule admits - and an alphanumeric or
      *> national constant-name was refused in EVERY one of them (fix-queue R01).
      *> BindArgOperand fell through to the 8.8.1.1 NUMERIC-expression bind, so
      *> the reference died as "constant-name 'K-TEXT' substitutes a literal of
      *> category Alphanumeric and is not a numeric operand". FUNCTION
      *> UPPER-CASE(K-TEXT) did not compile while FUNCTION UPPER-CASE("abcdef")
      *> did, for source GR1 makes identical - which is what assertions 1 and 2
      *> below pin, as a PAIR.
      *>
      *> The sweep found the defect scoped to argument positions: MOVE, IF,
      *> INSPECT and DISPLAY already substituted correctly, so this is not the
      *> whole substitution mechanism, only the argument path.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01CONSTARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-TEXT CONSTANT AS "abcdef".
       01 K-NAT  CONSTANT AS N"WXYZ".
       01 K-NUM  CONSTANT AS 4.
       01 R  PIC X(8).
       01 L  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
      *> 1-2 - THE PAIR. The constant and its literal must give the same answer;
      *> that equality IS 13.10.4 GR1, and it is a stronger assertion than the
      *> value alone.
           MOVE FUNCTION UPPER-CASE(K-TEXT) TO R.
           DISPLAY "1=[" R "]".
           MOVE FUNCTION UPPER-CASE("abcdef") TO R.
           DISPLAY "2=[" R "]".
      *> 3-4 - the same pairing for a NATIONAL constant.
           MOVE FUNCTION LENGTH(K-NAT) TO L.
           DISPLAY "3=" L.
           MOVE FUNCTION LENGTH(N"WXYZ") TO L.
           DISPLAY "4=" L.
      *> 5-6 - other string functions, to show the arm is not UPPER-CASE-shaped.
           MOVE FUNCTION REVERSE(K-TEXT) TO R.
           DISPLAY "5=[" R "]".
           MOVE FUNCTION LENGTH(K-TEXT) TO L.
           DISPLAY "6=" L.
      *> 7-8 - A NUMERIC CONSTANT DELIBERATELY KEEPS THE EXPRESSION PATH, and
      *> that is what makes 8 work: an argument carrying an operator is an
      *> expression, not a bare literal, and only the expression bind can take it.
           MOVE FUNCTION SQRT(K-NUM) TO L.
           DISPLAY "7=" L.
           MOVE FUNCTION MAX(K-NUM + 1, 2) TO L.
           DISPLAY "8=" L.
           STOP RUN.
