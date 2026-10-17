*>--- conformance-case
*> id: pb894-prototype-after-definition
*> rules: [SR-10.6.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 10.6.2 SR1: "Within a compilation group, function-prototypes
      *> and program-prototypes shall precede all other types of source
      *> units." The prototype here follows a program definition.
      *> kb/Work PB894.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNOM.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PGNOM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNOP IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM PGNOP.
