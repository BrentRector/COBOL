*>--- conformance-case
*> id: pb974-repository-class-as-zero-length
*> rules: [SR-12.3.8.3-2, FMT-12.3.8.2, DOC-A.1-163]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.8.3 SR2: "Literal-1, literal-2, literal-3, literal-4, and literal-5 shall be alphanumeric
      *> literals or national literals and shall be neither figurative constants nor zero-length literals."
      *> The class-specifier's `AS literal-1` (12.3.8.2) is therefore refused when zero-length - unlike the
      *> CLASS-ID paragraph's own AS literal, whose 11.3.3 SR1 omits the zero-length exclusion (kb/Work PB974:
      *> the phrase did not parse at all on this specifier before). Below 2002 there is no REPOSITORY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB974NEG1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BOXY AS "".
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
