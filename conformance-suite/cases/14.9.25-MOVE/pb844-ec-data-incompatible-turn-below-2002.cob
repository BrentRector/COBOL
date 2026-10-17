*>--- conformance-case
*> id: pb844-ec-data-incompatible-turn-below-2002
*> rules: [GR-14.9.25.4-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §7.3.25 — the >>TURN compiler directive, and with it the whole EC checking model, is a
      *> COBOL-2002 introduction: at --std 85 the version-conformance pass rejects it (COBOLNET0900).
      *>
      *> THIS IS THE EDITION FLOOR OF §14.9.25.4 GR6 d) 1, not a bare directive test. The rule —
      *>   cite.py --check 14.9.25.4 "Otherwise, if the content of the sending operand would result in a
      *>     false value in a numeric class condition, the EC-DATA-INCOMPATIBLE exception condition is set
      *>     to exist" -> OK §14.9.25.4 6) 1.
      *> — is stated at every edition, but its only OBSERVABLE consequence is the exception condition, and
      *> a program can request that the condition be checked only from COBOL-2002 onward. So the MOVE below
      *> is legal COBOL-85 (§14.9.25.3 Table 16 admits alphanumeric -> numeric) and stays legal here; what
      *> COBOL-85 refuses is the REQUEST TO CHECK it. The positive halves are
      *> 2002/pb426_alnum_sender_data_incompatible (checking on, no false raise) and
      *> MoveAlphanumericSenderTests (checking on, the raise).
      *>
      *> The fixture's only 2002+ construct is the directive: delete the first line and it compiles at 85,
      *> which is what makes COBOLNET0900 here attributable to the directive and to nothing else.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB844N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AQ PIC X VALUE "Q".
       01 N3 PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE AQ TO N3
           DISPLAY N3
           STOP RUN.
