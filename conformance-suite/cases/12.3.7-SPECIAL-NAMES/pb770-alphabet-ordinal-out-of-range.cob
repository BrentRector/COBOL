*>--- conformance-case
*> id: pb770-alphabet-ordinal-out-of-range
*> rules: [SR-12.3.7.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 12.3.7.3 SR14 b1: "Each numeric literal shall be an unsigned integer and shall have a value within
      *> the range of one through the maximum number of characters in the native alphanumeric character set."
      *> Ordinals are 1-based, so 0 is outside it.
      *>
      *> The .err pins the CONSTRUCT as well as the code. This check DID fire before kb/Work PB770 - through
      *> the CLASS clause's helper, so it reported `COBOLNET1671: CLASS : the ordinal 0 ... 12.3.7.3 SR17 b2`
      *> on a program that has no CLASS clause and under a rule about a different construct. A golden that
      *> only asserted "it is rejected" would have called that green.
     
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB770ALPHABETORDINAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALF IS 0 THRU 5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER PIC X.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
