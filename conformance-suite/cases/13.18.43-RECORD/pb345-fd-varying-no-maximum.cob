*>--- conformance-case
*> id: pb345-fd-varying-no-maximum
*> rules: [SR-13.18.43.3-1]
*> source-format: free
*> portable: unverified
*>---
*> The SECOND arm of COBOLNET1836, and the one a RECORD-clause-presence test alone would
*> miss: the clause is written, and still establishes no maximum. ISO/IEC 1989:2023
*> §14.9.30.4 GR6 sizes the implied record description by "the maximum size established
*> by the RECORD clause"; a format-2 RECORD IS VARYING with no TO phrase leaves that to
*> §13.18.43.4 GR10 -- "If integer-3 is not specified, the maximum number of bytes to be
*> contained in any record of the file is equal to the greatest number of bytes described
*> for a record in that file." -- and this file describes no record at all.
*> kb/Work PB345 -> COBOLNET1836.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb345n2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD IS VARYING IN SIZE FROM 5 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  W PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F1.
           READ F1 INTO W AT END CONTINUE END-READ.
           CLOSE F1.
           STOP RUN.
