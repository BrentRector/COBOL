      *> kb/Work PB152 - THE ARM-SPECIFIC WITNESS for the TIER-B IMAGE seed.
      *>
      *> "What fills storage that has no VALUE clause" was composed in THREE places and only one had ever been
      *> fixed. PB151 wired the ALLOCATE arm (14.9.3.4 GR8/GR9) with a fill decoder PRIVATE to PtrEmitter, so
      *> the native-field arm (PicInfo.DefaultInitializer) and THIS one - the string backing a REDEFINES class
      *> composes for its members (GroupImageCodec) - could not have reused it even if someone had tried.
      *>
      *> ⛔ WITHOUT THIS GOLDEN the image arm can be left unfixed and every other PB152 golden still passes:
      *> they all read the item through its own typed field, which the native arm seeds. R reads the SAME
      *> storage through the class's shared byte window, which only the image arm seeds. Verified by reverting
      *> the image arm alone and watching this file - and only this file - go red.
      *>
      *> EXPECTED, with INITIALIZE ALL TO X"5A" (= 'Z') in force, per 14.6.2.3.2 action 1:
      *>   GA PIC X(3), no VALUE           -> ZZZ    (3 alphanumeric positions take the fill)
      *>   R  REDEFINES G PIC X(5)         -> ZZZ followed by GC's TWO bytes. GC is PIC 9(4) COMP: its
      *>      storage is two BYTES and action 1 fills "the storage allocated" with no numeric exception
      *>      (owner decision kb/Work R53; COBOLNET_DATA_MODEL_DESIGN D23), so both bytes are X'5A':
      *>      ORD 91 each (the byte X'5A' is the character U+005A, ordinal 90 + 1).
      *>
      *> The carrier bytes are asserted with FUNCTION ORD, not with a comparison against LOW-VALUES. 15.70.1:
      *> "The ORD function returns an integer value that is the ordinal position of argument-1 in the program
      *> collating sequence. The lowest ordinal position is 1." - so ORD of a byte b is b + 1. The
      *> comparison spelling was avoided DELIBERATELY while kb/Work PB297 was open: a reference-modified operand
      *> compared against the figurative LOW-VALUE/HIGH-VALUE answered WRONG whenever the ref-mod length differed
      *> from the base item's width.  PB297 is FIXED; ORD is kept here as an INDEPENDENT second channel.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152TIERB.
       OPTIONS.
           INITIALIZE ALL TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GA PIC X(3).
          05 GC PIC 9(4) COMP.
       01 R REDEFINES G PIC X(5).
       01 W-N PIC 9.
       01 W-O PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "GA=[" GA "]".
           MOVE 0 TO W-N.
           IF R(1:3) = "ZZZ" MOVE 1 TO W-N END-IF.
           DISPLAY "IMG-FILL=" W-N.
           MOVE FUNCTION ORD(R(4:1)) TO W-O.
           DISPLAY "IMG-CARRIER-B4=" W-O.
           MOVE FUNCTION ORD(R(5:1)) TO W-O.
           DISPLAY "IMG-CARRIER-B5=" W-O.
           STOP RUN.
