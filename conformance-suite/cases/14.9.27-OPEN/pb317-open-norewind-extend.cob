      *>--- conformance-case
      *> id: pb317-open-norewind-extend
      *> rules: [SR-14.9.27.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.27.3 SR6: "The NO REWIND phrase may be specified only
      *> when the INPUT or OUTPUT phrase is specified." The group below
      *> is EXTEND, so the phrase is not admissible even though the file
      *> is sequential and SR5 is satisfied - the two syntax rules are
      *> independent and each needs its own witness. 14.9.27.4 GR12 a)
      *> corroborates SR6 by naming only EXTEND as the mode that
      *> suppresses the beginning-of-file positioning the phrase talks
      *> about. Edition-invariant, hence all four editions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB317N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb317n2.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           OPEN EXTEND S WITH NO REWIND
           STOP RUN.
