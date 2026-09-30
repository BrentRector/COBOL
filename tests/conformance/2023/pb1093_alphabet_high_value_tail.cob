       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1093AL.
      *> kb/Work PB1093 (owner decision R52) - A SPECIAL-NAMES
      *> HIGH-VALUE IS THE HIGHEST CHARACTER OF THE NATIVE SEQUENCE,
      *> U+FFFF (§12.3.7.4 GR10: "When specified as literals in the
      *> SPECIAL-NAMES paragraph, the figurative constants HIGH-VALUE
      *> and LOW-VALUE are associated with those characters having the
      *> highest and lowest positions ... in the native alphanumeric
      *> collating sequence"). SO "ALPHABET AL IS HIGH-VALUE "B" "A""
      *> PUTS U+FFFF AT POSITION 1, "B" AT 2, "A" AT 3, AND EVERY OTHER
      *> NATIVE CHARACTER AFTER THEM IN NATIVE ORDER (GR7 k3).
      *> WHY EACH LEG CAN FAIL:
      *>  FF     - X"FF" (U+00FF) IS IN THE TAIL, ABOVE "A" (IT SORTED
      *>           FIRST WHEN THE PIN MADE IT HIGH-VALUE).
      *>  ORDFF  - ITS ORDINAL: 3 SPECIFIED + 253 TAIL CHARACTERS
      *>           BELOW IT (U+0000..U+00FE LESS "A" AND "B") + 1 = 257.
      *>  LV     - THE RUNTIME LOW-VALUE IS THE CHARACTER AT THE LOWEST
      *>           POSITION (GR9) - U+FFFF, ORDINAL 1.
      *>  HV     - THE RUNTIME HIGH-VALUE IS THE CHARACTER AT THE
      *>           HIGHEST POSITION (GR8), THE LAST TAIL CHARACTER,
      *>           U+FFFE: ORDINAL 65536.
      *>  WIDE   - U+0100 ("Ā") IS BELOW THAT HIGH-VALUE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS HIGH-VALUE "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       01 Y PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"FF" TO X.
           IF X < "A" DISPLAY "FF=LT-A" ELSE DISPLAY "FF=GT-A" END-IF.
           DISPLAY "ORDFF=" FUNCTION ORD(X).
           MOVE LOW-VALUE TO Y.
           DISPLAY "LV=" FUNCTION ORD(Y).
           MOVE HIGH-VALUE TO Y.
           DISPLAY "HV=" FUNCTION ORD(Y).
           MOVE "Ā" TO X.
           IF X > Y DISPLAY "WIDE=GT-HV" ELSE DISPLAY "WIDE=LE-HV"
           END-IF.
           STOP RUN.
