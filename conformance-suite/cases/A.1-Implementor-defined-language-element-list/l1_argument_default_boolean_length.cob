*>--- conformance-case
*> id: l1_argument_default_boolean_length
*> rules: [DOC-A.1-90]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 rule 14 - the result when a function's ARGUMENT rules are
      *> violated and checking for EC-ARGUMENT-FUNCTION is not enabled - at
      *> the BOOLEAN-OF-INTEGER site of docs/CONFORMANCE.md row DOC-A.1-90.
      *> Landed with the kb/Work PB383 fix, which put this site in the row's
      *> zero-length class where it belongs. Row DOC-A.1-93's twin rule (a
      *> returned value past the implementor's 8 191-position maximum) is a
      *> DIFFERENT obligation with its own witness,
      *> l1_returned_value_limit_boolean_repro; PB383 was one condition
      *> answering both, and answering both wrongly.
      *>
      *> THE RULE. 15.13.3 r2: "Argument-2 shall be a positive nonzero
      *> integer." Zero and a negative value each violate it, so
      *> EC-ARGUMENT-FUNCTION is set to exist and 15.3 rule 14 hands the
      *> result to the implementor: "If the EC-ARGUMENT-FUNCTION exception
      *> condition is set to exist and checking for EC-ARGUMENT-FUNCTION is
      *> not enabled, the implementor defines the result of the function
      *> reference."
      *>
      *> WHICH ANSWER THE ROW GIVES DEPENDS ON WHICH ARGUMENT WAS REJECTED,
      *> and that is the whole point of this golden. Row DOC-A.1-90's general
      *> clause is "the zero value of the type the function returns", but the
      *> row carves out the functions "where the returned LENGTH is itself
      *> derived from the rejected argument", whose answer is a ZERO-LENGTH
      *> value. 15.13.4 r1 makes the returned value "a boolean item of usage
      *> bit" whose length is argument-2, so:
      *>   A2ZERO / A2NEG  argument-2 itself is rejected. There is no length
      *>                   left to return, so the answer is the zero-length
      *>                   value: 14.9.11.4 GR1 - "If an operand is a
      *>                   zero-length data item or a zero-length literal, no
      *>                   data is transferred for that operand" - closes the
      *>                   brackets on nothing, and 15.50.4 r1 reads the
      *>                   length out as the number 0 ("an integer equal to
      *>                   the length of argument-1 in boolean positions").
      *>                   Both legs are needed: a one-position "0" would
      *>                   print [0] and measure 1.
      *>   A1NEG           argument-1 is rejected (15.13.3 r1) while
      *>                   argument-2 is VALID, so the returned length is
      *>                   fully determined and the row's GENERAL clause
      *>                   applies instead - the zero value of a boolean item
      *>                   of 8 positions is eight zero positions, length 8.
      *> A1NEG is the control: it proves the zero-length legs report the
      *> ARGUMENT-2 rule specifically and not "an error happened".
      *>
      *> Every argument is a data item, so the values reach the runtime guard
      *> rather than a bind-time literal screen.
      *>
      *> 15.50.3 r1 admits "a data item of any class or category" as LENGTH's
      *> argument; 8.4.3.2.1 makes a function-identifier a reference to "the
      *> unique data item that results from the evaluation of a function" and
      *> 8.4.3.2.4 r2 permits one as an argument - "An argument being
      *> evaluated may itself be a function-identifier".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1ADBOOLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A2ZERO PIC S9(5) VALUE 0.
       01 W-A2NEG  PIC S9(5) VALUE -4.
       01 W-A2OK   PIC S9(5) VALUE 8.
       01 W-A1NEG  PIC S9(5) VALUE -3.
       01 W-VAL    PIC S9(5) VALUE 5.
       01 W-LEN    PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A2ZERO=["
               FUNCTION BOOLEAN-OF-INTEGER(W-VAL, W-A2ZERO) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(W-VAL, W-A2ZERO)) TO W-LEN
           DISPLAY "A2ZEROLEN=" W-LEN
           DISPLAY "A2NEG=["
               FUNCTION BOOLEAN-OF-INTEGER(W-VAL, W-A2NEG) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(W-VAL, W-A2NEG)) TO W-LEN
           DISPLAY "A2NEGLEN=" W-LEN
           DISPLAY "A1NEG=["
               FUNCTION BOOLEAN-OF-INTEGER(W-A1NEG, W-A2OK) "]"
           MOVE FUNCTION LENGTH(
               FUNCTION BOOLEAN-OF-INTEGER(W-A1NEG, W-A2OK)) TO W-LEN
           DISPLAY "A1NEGLEN=" W-LEN
           DISPLAY "OK=["
               FUNCTION BOOLEAN-OF-INTEGER(W-VAL, W-A2OK) "]"
           STOP RUN.
