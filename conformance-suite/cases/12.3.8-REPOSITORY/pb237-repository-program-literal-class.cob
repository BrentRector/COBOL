*>--- conformance-case
*> id: pb237-repository-program-literal-class
*> rules: [SR-12.3.8.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 12.3.8.3 syntax rule 2: "Literal-1, literal-2, literal-3,
      *> literal-4, and literal-5 shall be alphanumeric literals or national literals
      *> and shall be neither figurative constants nor zero-length literals."
      *> literal-3 here is a FIGURATIVE CONSTANT, which the rule names outright.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NLIT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM SOME-PROTO AS ZERO.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
