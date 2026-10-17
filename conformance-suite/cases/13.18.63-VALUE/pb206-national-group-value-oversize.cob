*>--- conformance-case
*> id: pb206-national-group-value-oversize
*> rules: [SR-13.18.63.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.63.3 SR5, sentence 3: "National literals in the VALUE clause of a national group item shall
      *> not exceed the size of the group item."  GD carries GROUP-USAGE NATIONAL, so 8.5.2.1 gives it "class
      *> and category national" and 13.18.29.4 GR2b treats it as though it were PICTURE N(m) with m its four
      *> national positions - P1's two plus P2's two.  N"ABCDEF" is six.
      *> MEASURED BEFORE THIS SCREEN (kb/Work PB206, on 1d949007): compiled CLEAN, P1 = AB and P2 = CD - the
      *> tail of the literal silently dropped.  The GROUP-USAGE clause is COBOL-2002 (group-usage-clause-2002),
      *> which is why 85 is not in the reject-at band: there the entry is refused for the clause itself.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB206N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GD GROUP-USAGE NATIONAL VALUE N"ABCDEF".
          05 P1 PIC N(2).
          05 P2 PIC N(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY P1
           STOP RUN.
