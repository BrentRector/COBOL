      *> reject-at: 85
      *> kb/Work PB2113 - the negative of
      *> 2002/pb2113_subscript_expressions_and_arguments. The argument
      *> list of a keyword-omitted function reference, MAX (V1 3), is
      *> written in the reference's own parentheses and parses as that
      *> reference's subscript list at every edition; whether it is a
      *> function is the binder's question. ISO 8.4.3.2.3 SR2 ("If
      *> intrinsic-function-name-1 or the ALL phrase is specified in the
      *> REPOSITORY paragraph ... the word FUNCTION may be omitted") is
      *> a COBOL 2002 rule, so at --std 85 the binder takes no keyword-
      *> omitted route (IntrinsicBinder.KeywordOmittedFunction's edition
      *> gate) and MAX is a data-name that no entry declares
      *> (COBOLNET1639, 8.4.2.1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2113N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1 PIC 9 VALUE 2.
       01 N PIC 99.
       PROCEDURE DIVISION.
           MOVE MAX (V1 3) TO N.
           DISPLAY N.
           STOP RUN.
