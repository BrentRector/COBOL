*>--- conformance-case
*> id: pb46-call-as-prototype-name
*> rules: [SR-14.9.4.3-16, FMT-14.9.4.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.4.3 syntax rule 16: "Program-prototype-name-1 shall be
      *> specified in a program-specifier in the REPOSITORY paragraph." This source
      *> writes `AS MY-PROTO` with no REPOSITORY paragraph at all, so MY-PROTO is
      *> neither a 12.3.8.2 program-specifier name nor - 8.4.6.8's only other
      *> spelling - the program-name of a containing program definition.
      *>
      *> It is a PERMANENT conformance rejection, not staged non-support: the
      *> program-specifier is accepted (kb/Work PB237) and the registry resolves it,
      *> so a name that is absent from it is a syntax-rule violation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46ASPROTO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" AS MY-PROTO USING BY CONTENT N.
           STOP RUN.
