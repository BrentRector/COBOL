*>--- conformance-case
*> id: pb63-exception-file-sd
*> rules: [AR-15.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.28.3 rule 1: "Argument-1 is optional and when specified shall be the name of a file connector
      *> that is specified in an FD statement." An SD is a sort-merge file DESCRIPTION, not an FD (13.5): the sort file is not a file connector in the 15.28 sense. Before PB63 the resolver matched the bare name only, so
      *> this compiled clean and answered rule 2a's two spaces. The file-connector-name argument form is 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB63EFSD.
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
           COMPUTE L = FUNCTION LENGTH(FUNCTION EXCEPTION-FILE(TF))
           STOP RUN.
