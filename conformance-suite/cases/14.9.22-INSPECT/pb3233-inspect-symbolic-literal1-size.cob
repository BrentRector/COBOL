      *>--- conformance-case
      *> id: pb3233-inspect-symbolic-literal1-size
      *> rules: [SR-14.9.22.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3233 - ISO 14.9.22.3 SR6 read with SR3: a bare
      *> symbolic-character IS a figurative constant (12.3.7.4 GR11),
      *> so as literal-1 it is "an implicit one character data item"
      *> and "QR" (two characters) is not its size. It compiled clean.
      *> Expected: COBOLNET0846 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG3233S1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS STAR IS 43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "A*B*C*".
       PROCEDURE DIVISION.
           INSPECT X REPLACING ALL STAR BY "QR".
           DISPLAY X.
           STOP RUN.
