      *>--- conformance-case
      *> id: pb2988-capacity-register-two-written-entries
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2988 - ISO 13.18.38.3 SR30: "Data-name-3 shall not be
      *>   defined elsewhere in the source element" -> OK 13.18.38.3
      *>   30). B is a SAME AS copy of A, so its DC is A's one
      *>   definition reproduced (legal); E's CAPACITY IN DC is a
      *>   SECOND written definition of the name and is refused
      *>   COBOLNET1523 - "defined elsewhere" compares the written
      *>   definitions, not their copies.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2988M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A.
          05 D PIC X OCCURS DYNAMIC CAPACITY IN DC.
       01 B SAME AS A.
       01 C.
          05 E PIC X OCCURS DYNAMIC CAPACITY IN DC.
       PROCEDURE DIVISION.
           STOP RUN.
