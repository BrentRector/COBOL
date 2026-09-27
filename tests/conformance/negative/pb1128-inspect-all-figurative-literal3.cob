      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1128 - ISO 14.9.22.3 SR3: "Literal-1, literal-2, literal-3, and literal-4 shall
      *> not be a figurative constant that begins with the word ALL." ALL SPACES begins with the
      *> word; the screen used to test only ALL literal-1 and ALL symbolic-character-1 and bound
      *> this literal-3 as the bare SPACE. (Literal-5 is not named: CONVERTING ... TO ALL "Q" is
      *> legal, 2002/pb1128_inspect_identifier1_class_store.cob.) Expected: COBOLNET1757
      *> (statement-operand-rule) at every edition - SR3 is unchanged text.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1128ALLFIG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "AAAA".
       PROCEDURE DIVISION.
           INSPECT X REPLACING ALL "A" BY ALL SPACES
           DISPLAY X
           STOP RUN.
