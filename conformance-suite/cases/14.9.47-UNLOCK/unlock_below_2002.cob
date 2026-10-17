*>--- conformance-case
*> id: unlock_below_2002
*> rules: [FMT-14.9.47.2]
*> source-format: free
*> portable: unverified
*>---
      *> UNLOCK (ISO §14.9.47) is a COBOL-2002 introduction. Residue migration #5
      *> (DESIGN-version-conformance-pipeline.md): the parse-time {is2002()}? predicate AND the reverse-signature
      *> ReservedWordEditionHints arm are GONE — UNLOCK now parses at all editions (superset) and the introduction
      *> gate is enforced at BIND (BindUnlock → ConstructRegistry.Check(UnlockStatement2002)), so below 2002 it is
      *> an exact COBOLNET0900 with the construct identity known, not a post-hoc guess.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ULB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "ulb.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(8).
       PROCEDURE DIVISION.
       M. UNLOCK F. STOP RUN.
