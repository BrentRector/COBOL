      *> kb/Work PB876 - the NATIONAL leg of 13.18.63.4 GR7 and GR8.
      *> GR7: "Each literal is aligned in the associated data item in accordance with 14.6.8 ... except that
      *> initialization is not affected by a JUSTIFIED clause and no editing takes place." So a VALUE literal that
      *> reaches a national-edited item is placed positionally and the B insertion character is NOT applied, where
      *> the MOVE control below (identical picture) edits it: N"ABCDE" through PIC NNBNN reads AB CD.
      *> GR8: "When a numeric-edited data description includes the BLANK WHEN ZERO clause and the VALUE clause uses
      *> either an alphanumeric or national literal, the BLANK WHEN ZERO clause has no effect on initialization."
      *> A national literal needs a numeric-edited item of usage national (8.5.2.1 Table 2). N"0000" therefore
      *> displays 0000, where the MOVE control of a zero (BLANK WHEN ZERO in effect) displays spaces, and
      *> N"  12" is placed as written. National items are shown through the alphanumeric image of DISPLAY.
      *> DISPLAY-OF's argument-1 "shall be of class national" (15.26.3 rule 1), and Table 2 puts a numeric-edited
      *> item of usage national in class national, so DISPLAY-OF(E5) is legal and returns the item's characters.
      *> Introduced in COBOL-2002 (USAGE NATIONAL and the national literal).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB876NAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NE PIC NNBNN USAGE NATIONAL VALUE N"ABCDE".
       01 NE2 PIC NNBNN USAGE NATIONAL.
       01 E1 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE N"0000".
       01 E5 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL VALUE N"  12".
       01 E6 PIC ZZZ9 BLANK WHEN ZERO USAGE NATIONAL.
       01 NUMZ PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           MOVE N"ABCDE" TO NE2.
           MOVE NUMZ TO E6.
           DISPLAY "NE=[" NE "] MOVE=[" NE2 "]".
           DISPLAY "E1=[" E1 "] E5=[" E5 "] MOVE=[" E6 "]".
           DISPLAY "DISPLAY-OF=[" FUNCTION DISPLAY-OF(E5) "]".
           STOP RUN.
