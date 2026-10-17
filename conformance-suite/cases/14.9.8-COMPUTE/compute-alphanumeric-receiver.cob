      *>--- conformance-case
      *> id: compute-alphanumeric-receiver
      *> rules: [SR-14.9.8.3-1]
      *> flag: {kind: syntax, at: [85, 2002, 2014, 2023]}
      *> control: compute-numeric-receiver
      *> source-format: fixed
      *>---
      *> ISO 14.9.8.3 SR 1: "Identifier-1 shall reference either an
      *> elementary numeric item or an elementary numeric-edited item."
      *> R-ALPHA is alphanumeric, so the COMPUTE whose sequence number
      *> area reads << violates a syntax rule, which the warning
      *> mechanism of 4.2.2 indicates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSCAREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R-ALPHA PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
<<         COMPUTE R-ALPHA = 1 + 2
           DISPLAY "R-ALPHA=" R-ALPHA
           STOP RUN.
