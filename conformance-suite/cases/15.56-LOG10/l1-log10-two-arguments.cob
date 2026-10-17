      *>--- conformance-case
      *> id: l1-log10-two-arguments
      *> rules: [FMT-15.56.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.56.2 general format, the COMPLEMENT of the shape:
      *> "<u>FUNCTION</u> <u>LOG10</u> ( argument-1 )" shows exactly
      *> ONE argument and no repetition ellipsis, and ISO 15.3 says
      *> "The definition of a function specifies the number of
      *> arguments required" - so a second argument is not a value
      *> the function ignores, it is source the format does not admit.
      *>
      *> The positive half of this format is l1_log10_format, which
      *> writes the shape over every argument-1 shape 15.3 type 10
      *> admits. A format test that only ever writes the LEGAL shape
      *> cannot distinguish "the format is enforced" from "anything
      *> inside the parentheses is accepted", which is why this file
      *> exists beside it.
      *>
      *> Edition-invariant: LOG10 is a COBOL-85 Intrinsic Function
      *> Module member and 15.56.2 carries no edition-conditional
      *> syntax, so the rejection is owed at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1L10ARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION LOG10(1000, 10).
           STOP RUN.
