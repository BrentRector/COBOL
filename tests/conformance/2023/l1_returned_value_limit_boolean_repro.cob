      *> ISO 15.4 Returned values, at BOOLEAN-OF-INTEGER - the one
      *> function whose returned length a program names directly
      *> (15.13.4 r1: a boolean item of argument-2 positions), so the
      *> over-maximum leg can be asked without building the value.
      *> docs/CONFORMANCE.md row DOC-A.1-93; the sibling golden
      *> l1_returned_value_length_limit witnesses the functions whose
      *> length grows from their data.
      *>
      *> THE RULE IS NOT AN ARGUMENT RULE. 15.13.3 r2 requires
      *> argument-2 to be "a positive nonzero integer", which every
      *> length below is. What the last legs violate is 15.4: "If the
      *> length of the returned value exceeds the maximum length
      *> specified by the implementor for a returned value, an
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist." Row
      *> DOC-A.1-93 sets the maximum at 1,073,741,791 character
      *> positions, the carrier's ceiling (kb/Work PB2631: it is not the
      *> 8,191-position literal maximum, so 8 192 positions are a legal
      *> returned value).
      *>
      *> THE SUBSTITUTED RESULT. Checking is not enabled here, so 15.3
      *> rule 14 applies: "If the EC-ARGUMENT-FUNCTION exception
      *> condition is set to exist and checking for EC-ARGUMENT-FUNCTION
      *> is not enabled, the implementor defines the result of the
      *> function reference." Row DOC-A.1-93 defines it as A ZERO-LENGTH
      *> VALUE, read two ways:
      *>   OVER    14.9.11.4 GR1 - "If an operand is a zero-length
      *>           data item or a zero-length literal, no data is
      *>           transferred for that operand" - so the brackets close
      *>           on nothing.
      *>   OVERLEN 15.50.4 r1 - LENGTH of a boolean argument "is an
      *>           integer equal to the length of argument-1 in boolean
      *>           positions", so the length is READ OUT as the number
      *>           0. A function-identifier is a legal argument
      *>           (8.4.3.2.4 r2).
      *>
      *> AT8192 IS PAST THE LITERAL MAXIMUM AND INSIDE THIS ONE. Value 5
      *> is 101 binary and 15.13.4 r1 puts "the rightmost boolean
      *> position" at the low-order digit, so positions 8 189-8 192 are
      *> 0101.
      *>
      *> NEG IS THE DISCRIMINATOR. Argument-1 negative violates 15.13.3
      *> r1, but argument-2 is VALID, so the returned length is fully
      *> determined and row DOC-A.1-90's general clause - "the zero
      *> value of the type the function returns" - gives eight real
      *> boolean positions, not a zero-length value. Three rules, three
      *> arms, two documented answers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1RVLBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B8192 PIC 1(8192) USAGE BIT.
       01 W-LEN PIC 9(10).
       01 W-NEG PIC S9(5) VALUE -3.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "INLIMIT=[" FUNCTION BOOLEAN-OF-INTEGER(5, 8) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8)) TO W-LEN
           DISPLAY "INLIMITLEN=" W-LEN
           MOVE FUNCTION BOOLEAN-OF-INTEGER(5, 8192) TO B8192
           DISPLAY "AT8192TAIL=[" B8192(8189:4) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 8192)) TO W-LEN
           DISPLAY "AT8192LEN=" W-LEN
           DISPLAY "OVER=[" FUNCTION BOOLEAN-OF-INTEGER(5, 1073741792)
               "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(5, 1073741792)) TO W-LEN
           DISPLAY "OVERLEN=" W-LEN
           DISPLAY "NEG=[" FUNCTION BOOLEAN-OF-INTEGER(W-NEG, 8) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(W-NEG, 8)) TO W-LEN
           DISPLAY "NEGLEN=" W-LEN
           STOP RUN.
