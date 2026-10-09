      *> reject-at: 2023
      *> kb/Work PB1965 - the KELVIN SIGN U+212A folds to k (Annex C.2
      *> (212A,006B), ISO 8.1.3.2 GR4 b)),
      *> so KEY is the reserved word KEY and
      *> cannot be a user-defined word (8.3.2.1 1)). The host's
      *> upper-casing kept the Kelvin sign apart from K, and the
      *> reserved-word screen let the entry through.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1965N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KEY PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY KEY.
           STOP RUN.
