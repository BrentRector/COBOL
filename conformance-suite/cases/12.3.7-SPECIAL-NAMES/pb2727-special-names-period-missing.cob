*>--- conformance-case
*> id: pb2727-special-names-period-missing
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2727 - ISO 12.3.7.2 closes a SPECIAL-NAMES paragraph that holds clauses with a
      *> separator period after the last one (5.2.1).  A paragraph with none before the DATA DIVISION
      *> header compiled clean at every edition; it is refused by name at its last clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N2727SM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXD IS "0" THRU "9"
           DECIMAL-POINT IS COMMA
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
