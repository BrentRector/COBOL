*>--- conformance-case
*> id: pb963-turn-file-name-below-2002
*> rules: [GR-7.3.25.4-6, GR-7.3.25.4-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB963 - THE EDITION FLOOR OF >>TURN's file-name-1 ARM.
      *>   cite.py --check 7.3.25.4 "if file-name-1 is specified,
      *>     checking is enabled only for exception conditions
      *>     associated with that file-name" -> OK §7.3.25.4 6)
      *> The TURN directive (§7.3.25) is a COBOL-2002 introduction, so at
      *> --std 85 the version-conformance pass rejects it (COBOLNET0900).
      *> Everything else below is legal COBOL-85: delete the directive
      *> line and it compiles at 85, which makes COBOLNET0900 here
      *> attributable to the directive and to nothing else. The positive
      *> half is 2002/pb963_turn_file_name_arms.
       >>TURN EC-I-O-AT-END F1 CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB963N85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB963N1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END
               DISPLAY "F1 AT END".
           CLOSE F1.
           STOP RUN.
