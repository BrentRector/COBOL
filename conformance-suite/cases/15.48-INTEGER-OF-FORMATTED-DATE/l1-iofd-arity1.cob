*>--- conformance-case
*> id: l1-iofd-arity1
*> rules: [FMT-15.48.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.2 general format — FUNCTION INTEGER-OF-FORMATTED-DATE
      *> ( argument-1 argument-2 ). Neither argument is bracketed, so BOTH are
      *> required: the format admits exactly two, and one is not enough.
      *> The accepting side is 2014/l1_iofd_two_argument_format; the over-count
      *> is negative/l1-iofd-arity3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIOFD1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD")
           STOP RUN.
       END PROGRAM L1NEGIOFD1.
