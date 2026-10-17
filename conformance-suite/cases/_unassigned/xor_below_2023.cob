*>--- conformance-case
*> id: xor_below_2023
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The logical XOR / EXCLUSIVE-OR operator (ISO §8.8.4.9) is a COBOL-2023 introduction. Residue migration #1
      *> (DESIGN-version-conformance-pipeline.md): the parse-time {is2023()}? predicate AND the reverse-signature
      *> ReservedWordEditionHints arm are GONE — XOR parses at all editions (superset) and the introduction gate is
      *> enforced at BIND (BindXorSequence -> Check(LogicalXorOperator2023)) only when the operator is genuinely
      *> present, so below 2023 it is an exact COBOLNET0900. (XOR as a data-NAME stays a legal user word below 2023.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XRB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       M. IF A = 1 XOR B = 1 DISPLAY "Y" END-IF. STOP RUN.
