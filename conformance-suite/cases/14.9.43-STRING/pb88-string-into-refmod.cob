*>--- conformance-case
*> id: pb88-string-into-refmod
*> rules: [SR-14.9.43.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.43.3 SR4: "Identifier-3 shall not be reference-modified." kb/Work PB88: this compiled clean and
      *> died at run time (a BoundUnsupported stage on illegal source); COBOLNET1651 at bind now.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB88NSTRRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(10).
       PROCEDURE DIVISION.
           STRING "AB" DELIMITED SIZE INTO R(2:4).
           STOP RUN.
