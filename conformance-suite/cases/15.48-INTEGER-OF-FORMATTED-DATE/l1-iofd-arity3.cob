*>--- conformance-case
*> id: l1-iofd-arity3
*> rules: [FMT-15.48.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.2 general format — FUNCTION INTEGER-OF-FORMATTED-DATE
      *> ( argument-1 argument-2 ). Nothing follows argument-2 and nothing is
      *> bracketed, so there is no third position: unlike the FORMATTED-TIME /
      *> FORMATTED-DATETIME siblings, this format carries no optional offset
      *> argument to absorb one.
      *> The accepting side is 2014/l1_iofd_two_argument_format; the under-count
      *> is negative/l1-iofd-arity1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIOFD3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D8 PIC X(8) VALUE "19950215".
       01 R  PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R =
               FUNCTION INTEGER-OF-FORMATTED-DATE("YYYYMMDD" D8 0)
           STOP RUN.
       END PROGRAM L1NEGIOFD3.
