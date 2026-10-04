      *> kb/Work PB1948 - ISO 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used
      *>   anywhere that a format specifies a literal of the class and category of constant-name-1."
      *>   cite.py: OK  13.10.3 2)  (Syntax rules)
      *> and 5.5 1) makes every integer-n of a general format "a fixed-point integer literal", so an
      *> integer constant-name stands at RESERVE integer-1 (12.4.5.14), BLOCK CONTAINS integer-1 and
      *> integer-2 (13.18.10), RECORD CONTAINS integer-1 (13.18.43), every LINAGE operand (13.18.34) and
      *> the SYMBOLIC CHARACTERS ordinals (12.3.7.2). 13.10.4 GR1 makes each "as if literal-1 ... were
      *> written where constant-name-1 is written".
      *>   cite.py: OK  5.5 1)  (Integer operands)
      *>   cite.py: OK  13.10.4 1)  (General rules)
      *>
      *> LINAGE IS KL(3) LINES WITH FOOTING AT KF(2) LINES AT TOP KT(0) LINES AT BOTTOM KU(0):
      *>   13.18.34.4 GR7 d) OPEN OUTPUT sets LINAGE-COUNTER to one; GR7 c) 3) a WRITE with no ADVANCING adds
      *>   one. W1 -> 2, which lies in the footing area that begins at KF (GR3: "the area of the page body
      *>   between the footing start and the page size, inclusive") so the END-OF-PAGE phrase runs
      *>   (14.9.51.4 GR26 b); W2 -> 3 (still the footing area, but this WRITE has no phrase); W3 -> 4
      *>   exceeds the page size KL so the device is repositioned and GR7 c) 4) resets the counter to one;
      *>   W4 -> 2 and W5 -> 3. KT and KU are ZERO constants: 13.18.34.3 SR4 "Integer-3, integer-4 may be
      *>   zero". A page size read as other than 3 moves the reset, a footing read as other than 2 moves
      *>   the phrase.
      *>   cite.py: OK  13.18.34.4 7) d)  (General rules)
      *>   cite.py: OK  13.18.34.3 4)  (Syntax rules)
      *>
      *> SYMBOLIC CHARACTERS: the order of the native set is the implementor's (12.3.7.4 GR6); this compiler's
      *> is the UTF-16 code-unit order, so ordinal n is the character whose code is n - 1 (66 -> 'A' ...
      *> 71 -> 'F'; DataBinder.SwitchBindSymbolic). The three spellings a constant-name ordinal can take: after ARE
      *> (SC-A SC-B), after IS (SC-C), with the optional IS/ARE omitted (SC-D KD, SC-E KE SC-F KF2 -- the
      *> names and the ordinals are told apart by which words the program defines as constants) and with a
      *> second group that omits it after a first that wrote it (SC-G IS KG SC-H KH).
      *>   cite.py: OK  12.3.7.3 16)  (Syntax rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1948A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SC-A SC-B ARE KA KB2
           SYMBOLIC CHARACTERS SC-C IS KC
           SYMBOLIC CHARACTERS SC-D KD SC-E KE SC-F KF2
           SYMBOLIC CHARACTERS SC-G IS KG SC-H KH.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb1948.prt" RESERVE KR AREAS.
       DATA DIVISION.
       FILE SECTION.
       FD PRTF BLOCK CONTAINS KR TO KB RECORDS
              RECORD CONTAINS KW CHARACTERS
              LINAGE IS KL LINES WITH FOOTING AT KF
              LINES AT TOP KT LINES AT BOTTOM KU.
       01 PREC PIC X(8).
       WORKING-STORAGE SECTION.
       01 KR CONSTANT AS 2.
       01 KB CONSTANT AS 5.
       01 KW CONSTANT AS 8.
       01 KL CONSTANT AS 3.
       01 KF CONSTANT AS 2.
       01 KT CONSTANT AS 0.
       01 KU CONSTANT AS 0.
       01 KA CONSTANT AS 66.
       01 KB2 CONSTANT AS 67.
       01 KC CONSTANT AS 68.
       01 KD CONSTANT AS 69.
       01 KE CONSTANT AS 70.
       01 KF2 CONSTANT AS 71.
       01 KG CONSTANT AS 72.
       01 KH CONSTANT AS 73.
       01 CN PIC 9.
       PROCEDURE DIVISION.
       M1.
           DISPLAY SC-A SC-B SC-C SC-D SC-E SC-F SC-G SC-H
           OPEN OUTPUT PRTF
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "OPEN C=" CN
           WRITE PREC FROM "LINE" AT END-OF-PAGE DISPLAY "EOP" END-WRITE
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "W1 C=" CN
           WRITE PREC FROM "LINE"
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "W2 C=" CN
           WRITE PREC FROM "LINE"
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "W3 C=" CN
           WRITE PREC FROM "LINE"
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "W4 C=" CN
           WRITE PREC FROM "LINE"
           MOVE LINAGE-COUNTER TO CN
           DISPLAY "W5 C=" CN
           CLOSE PRTF
           STOP RUN.
