*>--- conformance-case
*> id: pb2588-as-word-names-no-constant
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2588 - a WORD at an AS literal position stands there only as a
      *> constant-name (ISO 13.10.3 SR2: "constant-name-1 may be used anywhere that a
      *> format specifies a literal of the class and category of constant-name-1"), and a
      *> constant-name is the one its own source element declares.  N2588A's PROGRAM-ID
      *> names K2588-OTHER, which only N2588B declares: N2588A is a separate outermost
      *> program, so the word names no constant entry there, and ISO 11.10.3 SR1 ("Literal-1
      *> shall be an alphanumeric literal or a national literal") is not met.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2588A AS K2588-OTHER.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM N2588A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2588B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K2588-OTHER CONSTANT AS "N2588-OTHER".
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM N2588B.
