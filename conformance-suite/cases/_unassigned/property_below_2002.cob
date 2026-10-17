*>--- conformance-case
*> id: property_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The PROPERTY clause (ISO §13.18.42) is a COBOL-2002 OO introduction. Residue migration #7
      *> (DESIGN-version-conformance-pipeline.md): the parse-time {is2002()}? predicate AND the reverse-signature
      *> ReservedWordEditionHints arm are GONE — PROPERTY now parses at all editions (superset) and the introduction
      *> gate is enforced at BIND (DataBinder clause loop -> Check(PropertyClause2002)), so below 2002 it is an exact
      *> COBOLNET0900. (PROPERTY as a data-NAME stays a legal user word at 85 — only the CLAUSE is gated.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FOO PIC X PROPERTY.
       PROCEDURE DIVISION.
       M. STOP RUN.
