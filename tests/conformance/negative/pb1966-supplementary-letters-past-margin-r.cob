      *> reject-at: 2023
      *> ISO/IEC 1989:2023 6.1 1) a), 6.3.1: margin R is a CHARACTER POSITION (72), so text written past it
      *> is ignored however many supplementary-plane letters precede it. The period below stands at position
      *> 73, outside the program-text area, so the data description has no period (kb/Work PB1966: the
      *> counterpart of the positive golden, which pins that margin R moved no FURTHER out than 72).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1966N.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚             PIC 9(2).
000600 PROCEDURE DIVISION.
000700     STOP RUN.
