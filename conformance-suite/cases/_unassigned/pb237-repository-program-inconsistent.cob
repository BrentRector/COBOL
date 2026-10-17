*>--- conformance-case
*> id: pb237-repository-program-inconsistent
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 12.3.8.3 syntax rule 1: "If any object-class-name-1,
      *> interface-name-2, program-prototype-name-1, function-prototype-name-1,
      *> intrinsic-function-name-1 or property-name-1 is specified more than once in
      *> the REPOSITORY paragraph, all the specifications for that name shall be
      *> identical." A repeated name is legal; two DIFFERENT externalized names for it
      *> are not - 12.3.8.4 GR10 could not then say which program is called.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NDUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM SOME-PROTO AS "ONE"
           PROGRAM SOME-PROTO AS "TWO".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
