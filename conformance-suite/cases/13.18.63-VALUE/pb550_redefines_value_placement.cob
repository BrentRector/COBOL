      *>--- conformance-case
      *> id: pb550_redefines_value_placement
      *> rules: [SR-13.18.63.3-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO/IEC 1989:2023 13.18.63.3 SR12 (kb/Work PB550): "The VALUE clause shall not be specified in a data
      *> description entry that contains a REDEFINES clause or in an entry that is subordinate to an entry
      *> containing a REDEFINES clause." The rule's LEGAL neighbours, each of which a careless screen refuses:
      *>   the redefined ANCHOR A keeps its VALUE; the REDEFINES entry B carries none; the level-88 BZ under B
      *>   is format 3, which SR24 does not bring under SR12; C follows B but is not subordinate to it.
      *> WHY EACH LINE CAN FAIL:
      *>   INIT=    A's VALUE initializes the area B shares (13.18.44.4 GR1): [AAAA|AAAA|C].
      *>   BZ-1=    N - the area holds AAAA, not BZ's ZZZZ.
      *>   BZ-2=    Y after MOVE "ZZZZ" TO B, and A reads ZZZZ through the same area.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB550RVPOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 A PIC X(4) VALUE "AAAA".
           05 B REDEFINES A PIC X(4).
              88 BZ VALUE "ZZZZ".
           05 C PIC X VALUE "C".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "INIT=[" A "|" B "|" C "]"
           IF BZ DISPLAY "BZ-1=Y" ELSE DISPLAY "BZ-1=N" END-IF
           MOVE "ZZZZ" TO B
           IF BZ DISPLAY "BZ-2=Y " A ELSE DISPLAY "BZ-2=N " A END-IF
           STOP RUN.
