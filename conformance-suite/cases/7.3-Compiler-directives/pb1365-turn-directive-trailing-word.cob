*>--- conformance-case
*> id: pb1365-turn-directive-trailing-word
*> rules: [SR-7.3.3-3, SR-7.3.3-4, SR-7.3.3-6, FMT-7.3.25.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1365 - ISO 7.3.3 SR3: a compiler directive "may be
      *>   followed only by space characters and an optional inline
      *>   comment". TURN is a stage-owned directive: its word loop
      *>   stopped at CHECKING ON|OFF [WITH LOCATION] and never looked at
      *>   what followed, so JUNK below took effect in silence. It is now
      *>   COBOLNET0718.
      *> cite.py --check 7.3.3 "may be followed only by space characters
      *>   and an optional inline comment" -> OK  7.3.3 3)
       >>TURN EC-ALL CHECKING OFF JUNK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1365N.
       PROCEDURE DIVISION.
           STOP RUN.
