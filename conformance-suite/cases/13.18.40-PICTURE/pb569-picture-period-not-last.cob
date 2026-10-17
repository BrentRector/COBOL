*>--- conformance-case
*> id: pb569-picture-period-not-last
*> rules: [SR-13.18.40.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.40.3 SR7 - the '.' arm of the same rule (see pb569-picture-comma-not-last). In
      *> "999., VALUE" the '.' is followed by ',' - so it is a picture symbol, not the separator period
      *> (8.3.5 rule 3 needs a following space) - and the ',' by a space, a separator (8.3.5 rule 2):
      *> character-string-1 is "999." and VALUE follows it. kb/Work PB569's refutation called the '.'
      *> arm structurally enforced; it is not - this compiled silently at every edition as [000.].
      *> Refused COBOLNET2419.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB569N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC 999., VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "[" W1 "]"
           STOP RUN.
