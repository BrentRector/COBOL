*>--- conformance-case
*> id: pb530-picture-precedence-residue
*> rules: [SR-13.18.40.3-27]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB530 - ISO 1989:2023 13.18.40.3 SR27, carried BY Table 10 and pinned here with the rule's
      *> own named example: "No more than one of the following may be specified in character-string-1: a
      *> string of two or more symbols '+'; a string of two or more symbols '-'; a string of two or more
      *> currency symbols; a string of one or more symbols '*'; a string of one or more symbols 'Z'." NOTE 5
      *> names both controls - "the picture character-string +$$$ is valid, but ... +++$$$ is invalid" - and
      *> the compiler used to get the invalid one wrong in a way the missing diagnostic understates: it
      *> reported the digit positions of the '+' run ONLY and ignored the '$' run entirely, so every
      *> downstream consumer (the SR14 capacity gate, the MOVE geometry, FUNCTION LENGTH) was computed from a
      *> shape the standard does not define. Each entry is COBOLNET1935 - SR27 needs no rule of its own
      *> because Table 10's floating rows carry it - and SR27 is a syntax rule of every edition from 1985 on.
      *> The VALID control +$$$ is the positive golden 85/pb530_picture_floating_anchor_85 entry A09.
      *>
      *> PR01 +++$$$ - NOTE 5's own invalid string: two floating strings, a '+' string and a currency string.
      *>     Row floating-currency-left has a BLANK floating-sign-left column.
      *> PR02 ZZZ$$$ - the zero-suppression / floating-currency pair of the same rule (SR27's fifth bullet
      *>     counts a string of ONE or more 'Z', so a Z run and a currency run are already two of the five).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB530PRR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PR01 PIC +++$$$.
       01 PR02 PIC ZZZ$$$.
       PROCEDURE DIVISION.
           STOP RUN.
