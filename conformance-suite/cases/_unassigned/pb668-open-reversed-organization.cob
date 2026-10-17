      *>--- conformance-case
      *> id: pb668-open-reversed-organization
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB668, COBOLNET2210. REVERSED establishes the BACKWARD
      *> retrieval of ISO 14.9.30.4 GR21 c), which only a positionable
      *> record ordinal on a RECORD sequential medium supports, so the
      *> phrase is admitted for record sequential organization alone -
      *> one step narrower than its sibling WITH NO REWIND, whose
      *> 14.9.27.3 SR5 reaches both sequential kinds via 9.1.7.2 (the
      *> witness there is pb317-open-norewind-relative). The file below
      *> is RELATIVE, which SR5 also excludes, so the two phrases agree
      *> here and differ only on LINE SEQUENTIAL - and that difference
      *> is unreachable from conforming source at every edition, since
      *> LINE SEQUENTIAL arrived in 2023 (COBOLNET0900) and REVERSED was
      *> deleted in 2002 (COBOLNET0902). It is defence in depth under
      *> --permissive, which is why the screen tests the organization
      *> and not merely "not sequential".
      *> reject-at is 85 ALONE because at 2002 and later the phrase is
      *> refused first, by the open-reversed-removed-2002 edition gate
      *> (COBOLNET0902, VERSION_CHANGE_REFERENCE row 7.12) - so this
      *> syntax rule has exactly one edition at which it can be seen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB668N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "pb668n1.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS R-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 R-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT R REVERSED
           STOP RUN.
