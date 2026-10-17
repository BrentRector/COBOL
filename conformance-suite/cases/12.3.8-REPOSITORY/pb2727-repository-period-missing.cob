*>--- conformance-case
*> id: pb2727-repository-period-missing
*> rules: [FMT-12.3.8.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - ISO 12.3.8.2 closes a REPOSITORY paragraph that holds specifiers with a
      *> separator period after the last one (5.2.1).  A paragraph with none before the DATA DIVISION
      *> header compiled clean; it is refused by name at its last specifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727RM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PI INTRINSIC
           FUNCTION E INTRINSIC
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
