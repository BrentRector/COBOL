*>--- conformance-case
*> id: rewrite-invalid-key-sequential-org
*> rules: [SR-14.9.35.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.35.3 SR2 FIRST ARM: "Neither the INVALID KEY phrase nor
      *> the NOT INVALID KEY phrase shall be specified for a REWRITE
      *> statement that references a file with SEQUENTIAL ORGANIZATION or
      *> a file with relative organization and sequential access mode."
      *> This arm bound through SequentialIoBinder.BindRewrite, which did
      *> not read the phrase AT ALL -- parsed and dropped, no diagnostic.
      *> Its relative twin is rewrite-invalid-key-relative-sequential; a
      *> fix landing only one of them repeats the two-arm defect shape
      *> that made this rule wrong in the first place (kb/Work PB144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB144N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "n2.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN I-O S
           READ S
           REWRITE S-REC
               INVALID KEY CONTINUE
           END-REWRITE
           STOP RUN.
