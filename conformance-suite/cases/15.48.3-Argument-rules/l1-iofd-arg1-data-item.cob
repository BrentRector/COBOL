*>--- conformance-case
*> id: l1-iofd-arg1-data-item
*> rules: [AR-15.48.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.48.3 r1 — "Argument-1 shall be a national or alphanumeric
      *> LITERAL." The LITERAL half: a PIC X(8) data item holding the very same
      *> eight characters is of an admitted class and is still not a literal, so
      *> the class screen alone cannot reject it. Its content is only known at run
      *> time, which is why the rule closes the shape and not merely the class.
      *> The accepting side is 2014/l1_iofd_national_literal_arg1 (both admitted
      *> classes, written as literals).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1NEGIOFDITEM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FMT PIC X(8) VALUE "YYYYMMDD".
       01 D8  PIC X(8) VALUE "19950215".
       01 R   PIC 9(7).
       PROCEDURE DIVISION.
           COMPUTE R =
               FUNCTION INTEGER-OF-FORMATTED-DATE(FMT D8)
           STOP RUN.
       END PROGRAM L1NEGIOFDITEM.
