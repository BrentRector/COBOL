*>--- conformance-case
*> id: pb237-cancel-prototype-undeclared
*> rules: [SR-14.9.5.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 14.9.5.3 syntax rule 3: "Program-prototype-name-1 shall be a
      *> program prototype specified in the REPOSITORY paragraph." 14.9.5.2's operand
      *> brace is {identifier-1 | literal-1 | program-prototype-name-1}; MY-PROTO is a
      *> bare word, so it can only be identifier-1 or program-prototype-name-1, and it
      *> is neither - no data item of that name is defined and no program-specifier
      *> declares it (12.3.8.2), nor is it a containing program's name (8.4.6.8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB237NCAN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM OTHER-PROTO.
       PROCEDURE DIVISION.
       MAIN.
           CANCEL MY-PROTO.
           STOP RUN.
