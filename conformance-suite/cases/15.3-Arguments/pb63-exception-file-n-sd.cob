*>--- conformance-case
*> id: pb63-exception-file-n-sd
*> rules: [AR-15.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.29.3 rule 1: "Argument-1 is optional and when specified shall be the name of a file connector
      *> that is specified in an FD statement." The -N twin cites ITS OWN clause, 15.29.3 (word for word 15.28.3 rule 1); an SD is not an FD. Before PB63 the resolver matched the bare name only, so
      *> this compiled clean and answered rule 2a's two spaces. The file-connector-name argument form is 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63EFNSD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "pb63neg.dat".
       DATA DIVISION.
       FILE SECTION.
       SD TF.
       01 SREC PIC X(5).
       WORKING-STORAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           COMPUTE L = FUNCTION LENGTH(FUNCTION EXCEPTION-FILE-N(TF))
           STOP RUN.
