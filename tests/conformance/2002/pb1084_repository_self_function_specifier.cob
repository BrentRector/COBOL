      *> ISO §12.3.8.3 SR11 - a function-specifier naming the function
      *> definition it is written in is ignored (kb/Work PB1084)
      *> RULE §12.3.8.3 SR11: "If the specified function-prototype-
      *>   name-1 is the name of the function definition in which this
      *>   REPOSITORY paragraph is specified, references to
      *>   function-prototype-name-1 are to that function definition
      *>   and this function-specifier is ignored."
      *>   cite.py --check 12.3.8.3 "If the specified function-
      *>   prototype-name-1 is the name of the function definition in
      *>   which this REPOSITORY paragraph is specified" -> OK
      *>   §12.3.8.3 11)
      *> RULE §12.3.8.4 GR11 NOTE 2: "Literal-5, if specified, is the
      *>   externalized name of the function prototype; otherwise, the
      *>   externalized name is function-prototype-name-1."
      *>   cite.py --check 12.3.8.4 "Literal-5, if specified, is the
      *>   externalized name of the function prototype" -> OK
      *>   §12.3.8.4 11) a)
      *> SET-UP: PB1084O takes TWO arguments and returns 1000.
      *>   PB1084F (one argument, factorial) writes FUNCTION PB1084F AS
      *>   "PB1084O" in its own REPOSITORY and calls itself.
      *>   PB1084P names PB1084F plainly and, as a control, names
      *>   PB1084A AS "PB1084O".
      *> EXPECTED OUTPUT, DERIVED:
      *>   FACT5=0120  SR11: inside PB1084F the self-named specifier is
      *>               ignored, so PB1084F(L-N - 1) is the recursive
      *>               self-call: 5*4*3*2*1 = 120. Applying the AS
      *>               literal would bind the one-argument self-call
      *>               to PB1084O's two formals (a compile error) or,
      *>               if accepted, yield 5*1000.
      *>   ALIAS=1000  control: PB1084A is not the name of the element
      *>               the specifier is written in, so GR11 NOTE 2
      *>               applies and PB1084A(1, 2) activates PB1084O.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1084O.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-A L-B RETURNING L-R.
       P-MAIN.
           MOVE 1000 TO L-R
           GOBACK.
       END FUNCTION PB1084O.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1084F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1084F AS "PB1084O".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-N PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-N RETURNING L-R.
       P-MAIN.
           IF L-N <= 1
               MOVE 1 TO L-R
           ELSE
               COMPUTE L-R = L-N * FUNCTION PB1084F(L-N - 1)
           END-IF
           GOBACK.
       END FUNCTION PB1084F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1084P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1084F
           FUNCTION PB1084A AS "PB1084O".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9(4).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE WS-R = FUNCTION PB1084F(5)
           DISPLAY "FACT5=" WS-R
           COMPUTE WS-R = FUNCTION PB1084A(1, 2)
           DISPLAY "ALIAS=" WS-R
           STOP RUN.
       END PROGRAM PB1084P.
