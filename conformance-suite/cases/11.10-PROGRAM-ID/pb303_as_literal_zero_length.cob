*>--- conformance-case
*> id: pb303_as_literal_zero_length
*> rules: [SR-11.10.3-1]
*> source-format: free
*> portable: unverified
*>---
       *> kb/Work PB303 - the zero-length half of ISO 11.10.3 syntax rule 1 ("shall be
       *> neither a figurative constant NOR A ZERO-LENGTH LITERAL").  It is pinned apart
       *> from the figurative-constant half because 11.3.3 SR1 (CLASS-ID) omits exactly
       *> this exclusion while keeping the other - verified on the printed page - so the
       *> two halves are independently observable and the asymmetry is deliberate, not a
       *> transcription slip.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB303ZRO AS "".
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
