*>--- conformance-case
*> id: pb78-object-computer-clause-without-name-85
*> rules: [FMT-12.3.6.2]
*> source-format: free
*> portable: unverified
*>---
      *> X3.23-1985's OBJECT-COMPUTER format hung PROGRAM COLLATING SEQUENCE off a REQUIRED computer-name; ISO 12.3.6.2
      *> (2002+) makes the name optional. Below 2002 the name-less clause form is the introduction gate
      *> computer-name-optional-2002 (COBOLNET0900), never a bare parse error. kb/Work PB78.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB78N85.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER.
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS STANDARD-1.
       PROCEDURE DIVISION.
           STOP RUN.
