*>--- conformance-case
*> id: pb132-call-byref-pointer
*> rules: [SR-14.9.4.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.3 SR10 (FORMAT 1): a BY REFERENCE (implied here - GR5's assumed mode) identifier-2
      *> shall be neither a strongly-typed group item nor a data item of class object or pointer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB132N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" USING PT
           STOP RUN.
