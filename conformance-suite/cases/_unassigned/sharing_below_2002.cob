*>--- conformance-case
*> id: sharing_below_2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The SHARING clause (ISO §12.4.5.15 / §14.9.27) is a COBOL-2002 introduction. Residue migration #3
      *> (DESIGN-version-conformance-pipeline.md): the parse-time {is2002()}? predicates (SELECT + OPEN) AND the
      *> reverse-signature ReservedWordEditionHints arm are GONE — SHARING parses at all editions (superset) and the
      *> introduction gate is enforced at BIND (DataBinder SELECT + BindOpen phrase -> Check(FileSharingClause2002)),
      *> so below 2002 it is an exact COBOLNET0900. (A file NAMED SHARING stays a legal user word at 85 — verified:
      *> the OPEN name-list disambiguation is byte-safe; only the SHARING WITH <mode> phrase is gated.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SHB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "shb.dat" SHARING WITH ALL OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(8).
       PROCEDURE DIVISION.
       M. STOP RUN.
