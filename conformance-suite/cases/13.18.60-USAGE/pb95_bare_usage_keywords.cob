*>--- conformance-case
*> id: pb95_bare_usage_keywords
*> rules: [FMT-13.18.60.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB95 - ISO 13.18.60.2 makes [USAGE IS] optional for EVERY usage: POINTER, OBJECT REFERENCE, NATIONAL, BIT,
      *> PROGRAM-POINTER (and FUNCTION-POINTER) were rejected without the USAGE keyword while DISPLAY / COMP /
      *> BINARY / INDEX / the FLOAT-* forms parsed bare. Every bare usage now parses; the 2002 words that are user
      *> words at 85 (NATIONAL, BIT, PROGRAM-POINTER, FUNCTION-POINTER) sit behind the edition predicate so an '85
      *> item NAMED BIT still parses. Found landing PB79.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB95BU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P POINTER.
       01 O OBJECT REFERENCE.
       01 PP PROGRAM-POINTER.
       01 N PIC N(2) NATIONAL VALUE N"ab".
       01 B PIC 1(3) BIT VALUE B"101".
       01 X PIC X(2) DISPLAY VALUE "hi".
       PROCEDURE DIVISION.
           DISPLAY N B X " B-BYTES=" FUNCTION BYTE-LENGTH(B).
           IF P = NULL AND O = NULL AND PP = NULL DISPLAY "NULLS" END-IF.
           STOP RUN.
