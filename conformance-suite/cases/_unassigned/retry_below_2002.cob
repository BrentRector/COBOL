*>--- conformance-case
*> id: retry_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The RETRY phrase (ISO §14.7.9) is a COBOL-2002 introduction. Residue migration #4
      *> (DESIGN-version-conformance-pipeline.md): the parse-time {is2002()}? predicates AND the reverse-signature
      *> ReservedWordEditionHints arm are GONE — RETRY parses at all editions (superset; the OPEN site via the
      *> retryPhraseAhead() forward-detect) and the introduction gate is enforced at BIND (GateRetryIntro ->
      *> Check(RetryPhrase2002)) on every I/O statement that carries it, so below 2002 it is an exact COBOLNET0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RTB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "rtb.dat" ORGANIZATION INDEXED RECORD KEY RK.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 RK PIC X(4).
       PROCEDURE DIVISION.
       M. READ F RETRY 3 TIMES. STOP RUN.
