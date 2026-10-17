      *>--- conformance-case
      *> id: pb158-stacked-unary-identifier
      *> rules: [GR-8.8.1.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 8.8.1.2 Table 3, row "Unary + or -" x column "Unary + or
      *> -" = '-' (an invalid pair). REJECTS AT EVERY EDITION: Table 3
      *> is a formation rule with no introducedIn, so this is not an
      *> edition gate and --permissive has no arm for it (that mode
      *> softens REMOVED constructs only). The operand is an
      *> IDENTIFIER, so the 8.3.3.3.2 rule 2 signed-literal reading is
      *> unavailable no matter how the sign is spaced. kb/Work PB158.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB158N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 5.
       01 R PIC S9(6) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = - - A.
           STOP RUN.
