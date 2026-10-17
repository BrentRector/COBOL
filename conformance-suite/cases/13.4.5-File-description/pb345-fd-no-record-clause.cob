*>--- conformance-case
*> id: pb345-fd-no-record-clause
*> rules: [SR-13.4.5.3-3, SR-13.18.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO/IEC 1989:2023 §13.4.5.3 syntax rule 3 a): "When no record description entries
*> are specified: a) a RECORD clause shall be specified in the file description entry"
*> -- restated on the clause itself by §13.18.43.3 syntax rule 1, "If no record
*> description entries are specified in a file description entry for a file that is not
*> a report file, the RECORD clause shall be specified."
*> The rule is load-bearing, not decorative: §14.9.30.4 GR6 sizes the implied record
*> description by "the maximum size established by the RECORD clause", so without one
*> this file has no record area at all. kb/Work PB345 -> COBOLNET1836.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb345n1.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       WORKING-STORAGE SECTION.
       01  W PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F1.
           READ F1 INTO W AT END CONTINUE END-READ.
           CLOSE F1.
           STOP RUN.
