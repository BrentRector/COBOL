*>--- conformance-case
*> id: pb1689_remarks_is_a_legal_paragraph_name
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB1689 - ISO 8.9 reserves REMARKS at no edition (and GnuCOBOL 3.2 leaves it a user
      *>   word outside its context; owner 2026-09-29): a legal paragraph-name and PERFORM target.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1689P.
       PROCEDURE DIVISION.
       START-IT.
           DISPLAY "START"
           PERFORM REMARKS
           STOP RUN.
       REMARKS.
           DISPLAY "IN-REMARKS".
