      *>--- conformance-case
      *> id: pb241-continue-after-zero-below-2023
      *> rules: [SR-14.9.9.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB241 - ISO 14.9.9.3 SR2 makes a bare CONTINUE
      *> equivalent to CONTINUE AFTER 0 SECONDS, but only the bare
      *> spelling exists below COBOL-2023: the AFTER ... SECONDS phrase
      *> is a 2023 addition (14.9.9.2 general format; Annex E.3.3 item
      *> 14), so even its zero interval is refused by name
      *> (COBOLNET0900, the introduction band). The positive golden is
      *> 2023/pb241_continue_bare_equals_after_zero.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB241NG.
       PROCEDURE DIVISION.
       MAIN-P.
           CONTINUE.
           CONTINUE AFTER 0 SECONDS.
           STOP RUN.
