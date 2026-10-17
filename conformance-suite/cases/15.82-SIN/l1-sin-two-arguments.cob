      *>--- conformance-case
      *> id: l1-sin-two-arguments
      *> rules: [FMT-15.82.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 15.82.2 general format, the COMPLEMENT of the shape:
      *> "<u>FUNCTION</u> <u>SIN</u> ( argument-1 )" shows exactly ONE
      *> argument and no repetition ellipsis, and ISO 15.3 says "The
      *> definition of a function specifies the number of arguments
      *> required" - so a second argument is not a value the function
      *> ignores, it is source the format does not admit.
      *>
      *> The positive half of this format is l1_sin_format_value. A
      *> format test that only ever writes the LEGAL shape cannot
      *> distinguish "the format is enforced" from "anything inside
      *> the parentheses is accepted", which is why this file exists
      *> beside it.
      *>
      *> Edition-invariant: SIN is a COBOL-85 Intrinsic Function
      *> Module member and 15.82.2 carries no edition-conditional
      *> syntax, so the rejection is owed at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SINARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6)V99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION SIN(1, 2).
           STOP RUN.
