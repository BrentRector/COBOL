*>--- conformance-case
*> id: pb303_as_literal_figurative
*> rules: [SR-11.10.3-1]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - ISO 11.10.3 syntax rule 1: "Literal-1 shall be an alphanumeric
       *> literal or a national literal and shall be neither a figurative constant nor a
       *> zero-length literal."  SPACE is a figurative constant (ISO 8.3.3.6), so the AS
       *> phrase may not carry it.  COBOLNET1794 is the ONE screen every AS site shares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303FIG AS SPACE.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
