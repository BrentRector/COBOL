      *>--- conformance-case
      *> id: pb317-open-norewind-relative
      *> rules: [SR-14.9.27.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.27.3 SR5: "The NO REWIND phrase may be specified only
      *> for sequential files." The file below is RELATIVE, so the
      *> phrase is not admissible. The rule is edition-invariant, hence
      *> all four editions. It is the OPEN twin of 14.9.6.3 SR1, which
      *> negative/pb140-close-norewind-indexed already pins for CLOSE;
      *> only the CLOSE spelling had a screen before kb/Work PB317.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB317N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "pb317n1.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT R WITH NO REWIND
           STOP RUN.
