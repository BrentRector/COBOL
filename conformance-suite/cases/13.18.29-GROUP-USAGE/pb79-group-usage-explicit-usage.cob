*>--- conformance-case
*> id: pb79-group-usage-explicit-usage
*> rules: [SR-13.18.29.3-2, SR-13.18.29.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.29.3 SR3: "USAGE NATIONAL is implied for the subject of the entry. A USAGE clause shall not
      *> be explicitly specified for the subject of the entry" (SR2 says the same of BIT). kb/Work PB79.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB79N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG GROUP-USAGE NATIONAL USAGE NATIONAL.
          05 N1 PIC N(2).
       PROCEDURE DIVISION.
           STOP RUN.
