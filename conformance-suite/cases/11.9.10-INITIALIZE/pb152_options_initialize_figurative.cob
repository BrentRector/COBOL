*>--- conformance-case
*> id: pb152_options_initialize_figurative
*> rules: [GR-11.9.10.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB152 - ISO 11.9.10.4 GR5's FOUR FIGURATIVE fill arms, one runtime element each because a
      *> source unit may carry only one OPTIONS paragraph. (GR5 c, literal-1, is pb152_options_initialize_
      *> background's subject.) GR5: "The character following 'TO' creates the specified-fill-character."
      *>   a) "If BINARY ZEROES is specified, a string of binary zeros is the specified-fill-character."
      *>   b) "If HIGH-VALUES is specified, the alphanumeric high value character is the specified-fill-
      *>      character."
      *>   d) "If LOW-VALUES is specified, the alphanumeric low value character is the specified-fill-
      *>      character."
      *>   e) "If SPACES is specified, the alphanumeric space is the specified-fill-character."
      *>
      *> Each subprogram lays its fill over a VALUE-LESS PIC X(4) (14.6.2.3.2 action 1) and compares the whole
      *> item against the figurative constant the rule names. Comparisons are on the WHOLE item, never on a
      *> reference modification.  That was because kb/Work PB297 was open - a ref-mod compared against
      *> LOW-VALUE/HIGH-VALUE answered wrong when the lengths differed.  PB297 is FIXED; the whole-item
      *> spelling stays because it is what 8.3.3.6.4 GR2 is being asserted over here.
      *>
      *> BINARY ZEROES and LOW-VALUES coincide for this compiler: the alphanumeric low value of the UTF-16
      *> repertoire IS U+0000, which is also the binary zero. The two arms are still written separately - they
      *> are two different rules, and a fill map that dropped one of them would otherwise pass.
      *>
      *> EXPECTED: BZ=1 HV=1 LV=1 SP=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152FIGMAIN.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB152FIGBZ".
           CALL "PB152FIGHV".
           CALL "PB152FIGLV".
           CALL "PB152FIGSP".
           STOP RUN.
       END PROGRAM PB152FIGMAIN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152FIGBZ.
       OPTIONS.
           INITIALIZE ALL TO BINARY ZEROES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       P.
           MOVE 0 TO W-N.
           IF A = LOW-VALUES MOVE 1 TO W-N END-IF.
           DISPLAY "BZ=" W-N.
       END PROGRAM PB152FIGBZ.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152FIGHV.
       OPTIONS.
           INITIALIZE ALL TO HIGH-VALUES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       P.
           MOVE 0 TO W-N.
           IF A = HIGH-VALUES MOVE 1 TO W-N END-IF.
           DISPLAY "HV=" W-N.
       END PROGRAM PB152FIGHV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152FIGLV.
       OPTIONS.
           INITIALIZE ALL TO LOW-VALUES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       P.
           MOVE 0 TO W-N.
           IF A = LOW-VALUES MOVE 1 TO W-N END-IF.
           DISPLAY "LV=" W-N.
       END PROGRAM PB152FIGLV.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152FIGSP.
       OPTIONS.
           INITIALIZE ALL TO SPACES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(4).
       01 W-N PIC 9.
       PROCEDURE DIVISION.
       P.
           MOVE 0 TO W-N.
           IF A = SPACES MOVE 1 TO W-N END-IF.
           DISPLAY "SP=" W-N.
       END PROGRAM PB152FIGSP.
