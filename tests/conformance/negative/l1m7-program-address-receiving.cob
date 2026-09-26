      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.13.3 SR4 — a program-address-identifier (ADDRESS OF
      *> PROGRAM literal-1) written as the RECEIVING operand of SET
      *> (program-pointer-assignment format).
      *> Rule: "This identifier format shall not be specified as a
      *> receiving operand."
      *> cite.py: OK  §8.4.3.13.3 4)  (Syntax rules)
      *> §14.9.39.4 GR16: "The address identified by identifier-8 is
      *> stored in each data item referenced by identifier-7" — so in
      *> SET identifier-7 TO identifier-8 the operand after SET is the
      *> receiving operand.
      *> cite.py: OK  §14.9.39.4 16)  (General rules)
      *> The first SET is the legal direction (the program-address-
      *> identifier is the SENDER; literal-1 "L1M7PAS" is alphanumeric
      *> and non-empty, §8.4.3.13.3 SR2).  The second SET writes the
      *> same identifier as the receiver, which only SR4 forbids.  The
      *> grammar admits the identifier only in sender slots, so the
      *> refusal is a syntax error.  CobolErrorListener re-codes a
      *> syntax error with its leading hint's code, and the hint that
      *> fires on the reserved word PROGRAM where an identifier is
      *> expected is COBOL0201 (observed by the row's adjudicator).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PP TO ADDRESS OF PROGRAM "L1M7PAS"
           SET ADDRESS OF PROGRAM "L1M7PAS" TO PP
           STOP RUN.
       END PROGRAM L1M7PAR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M7PAS.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM L1M7PAS.
