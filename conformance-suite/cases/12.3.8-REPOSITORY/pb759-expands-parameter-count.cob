*>--- conformance-case
*> id: pb759-expands-parameter-count
*> rules: [GR-12.3.8.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB759 -- ISO 12.3.8.4 GR5: "The number of parameters in the USING phrase of the EXPANDS phrase
      *> of the class-specifier shall be the same as the number of parameters in the USING clause of the
      *> CLASS-ID paragraph of object-class-name-2."  PB759NCHOLD declares ONE formal; the specifier supplies
      *> TWO actual parameters, so no class can be created from it -> COBOLNET2240.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB759NCBOX.
       END CLASS PB759NCBOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB759NCHOLD USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ELEM.
       END CLASS PB759NCHOLD.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB759NC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB759NCBOX
           CLASS PB759NCHOLD
           CLASS PB759NCHB EXPANDS PB759NCHOLD USING PB759NCBOX PB759NCBOX.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB759NC.
