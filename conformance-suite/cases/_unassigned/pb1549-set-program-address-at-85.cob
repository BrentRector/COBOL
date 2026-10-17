      *>--- conformance-case
      *> id: pb1549-set-program-address-at-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1549 - the edition floor of the positive golden
      *> 2002/pb1549_set_program_address_fatal_resume: SET program-
      *> pointer TO ADDRESS OF PROGRAM (the address-identifier, ISO
      *> 8.4.3.13) and the program-pointer class it stores into arrived
      *> in COBOL-2002, with the exception-condition model it raises
      *> EC-PROGRAM-NOT-FOUND through, so below 2002 the statement is an
      *> introduction gate (COBOLNET0900). Nothing else here is past
      *> COBOL-85 except the pointer the statement needs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1549N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET PP TO ADDRESS OF PROGRAM "PB1549Z"
           STOP RUN.
