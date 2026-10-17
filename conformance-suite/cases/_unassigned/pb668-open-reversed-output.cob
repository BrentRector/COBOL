      *>--- conformance-case
      *> id: pb668-open-reversed-output
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB668, COBOLNET2211. COBOL-85's OPEN general format
      *> writes REVERSED in the INPUT group ONLY (VERSION_CHANGE_
      *> REFERENCE row 7.12; this repository holds no 1985 text, so the
      *> rule comes from that row plus the surveyed implementations).
      *> The phrase's whole effect is a RETRIEVAL DIRECTION, and OUTPUT,
      *> I-O and EXTEND have no READ of the created/extended file for it
      *> to apply to. The group below is OUTPUT, so the phrase is not
      *> admissible even though the file is record sequential and the
      *> organization rule (COBOLNET2210) is satisfied - the two rules
      *> are independent and each needs its own witness, exactly as
      *> 14.9.27.3 SR5 and SR6 do for the sibling WITH NO REWIND.
      *> reject-at is 85 ALONE because at 2002 and later the phrase is
      *> refused first, by open-reversed-removed-2002 (COBOLNET0902).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB668N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb668n2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT S REVERSED
           STOP RUN.
