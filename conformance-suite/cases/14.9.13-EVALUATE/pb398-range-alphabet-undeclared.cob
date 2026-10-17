*>--- conformance-case
*> id: pb398-range-alphabet-undeclared
*> rules: [SR-14.9.13.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.13.3 SR3 names alphabet-name-1, and 12.3.7 is where an alphabet-name is declared - the
      *> ALPHABET clause of SPECIAL-NAMES. A word that declares no alphabet supplies no collating sequence, so
      *> 14.7.8 rule 2's "the collating sequence defined by that alphabet" has nothing to resolve to. Refused
      *> rather than treated as the native order: silently ordering a range by a sequence the program did not
      *> ask for is the wrong-answer half of kb/Work PB398, not its parse-error half.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB398NEG4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-C
               WHEN "A" THRU "M" IN NO-SUCH-ALPHA DISPLAY "IN"
               WHEN OTHER                         DISPLAY "OUT"
           END-EVALUATE
           STOP RUN.
