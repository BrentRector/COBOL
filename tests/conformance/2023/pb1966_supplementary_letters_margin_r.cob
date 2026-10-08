      *> kb/Work PB1966 - fixed-form columns are CHARACTER POSITIONS, not UTF-16 code units
      *> (ISO/IEC 1989:2023 6.1 1) a); docs/CONFORMANCE.md DOC-A.1-157). Every Linear B syllable below
      *> is ONE position, so each of the three lines that reach position 72 is read whole: the data
      *> description ending in a period at 72, the literal whose first part runs to margin R (its
      *> continuation adds BB, 6.3.5 2)), and the statement ending in a period at 72. Counted in UTF-16
      *> units, margin R fell one position early per Linear B syllable before it and the text after it was lost.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1966P.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚            PIC 9(2).
000600 01  𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍 PIC X(80) VALUE "AAAAAAAAAAAAAAAAAAAAAAA
000700-    "BB".
000900 PROCEDURE DIVISION.
001000     MOVE 42 TO 𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚         .
001100     DISPLAY 𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍𐀎𐀏𐀐𐀑𐀒𐀓𐀔𐀕𐀖𐀗𐀘𐀙𐀚.
001200     DISPLAY 𐀔𐀕𐀖𐀗𐀘𐀙𐀚𐀛𐀜𐀝𐀞𐀟𐀠𐀡𐀢𐀣𐀤𐀥𐀦𐀍.
001300     STOP RUN.
