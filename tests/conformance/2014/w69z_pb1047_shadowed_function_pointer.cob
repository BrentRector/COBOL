      *> ISO §8.4.6.2.1 3) a) - function-pointer-name-1 resolves by
      *> the nearest-declaring-element rule like every other data-name
      *> (kb/Work PB1047, wave 69 Z: a contained program's own
      *> FUNCTION-POINTER beside a container's GLOBAL one of the same
      *> name drew COBOLNET1501, and a contained data item hid nothing)
      *> RULE §8.4.3.2.3 SR4: "Function-pointer-name-1 shall be defined
      *>   as a function-pointer data item."  cite.py --check 8.4.3.2.3
      *>   "Function-pointer-name-1 shall be defined as a function-
      *>   pointer data item" -> OK §8.4.3.2.3 4)
      *> RULE §8.4.6.2.1 3) a): "If the name is declared in source
      *>   element B, the item in source element B is the referenced
      *>   item."  cite.py --check 8.4.6.2.1 "If the name is declared in
      *>   source element B, the item in source element B is the
      *>   referenced item" -> OK §8.4.6.2.1 3) a)
      *> So the name is resolved FIRST, and SR4 is asked of the item it
      *>   names: W69ZFI's FP is its own function-pointer (FP(5) calls
      *>   W69ZF3), and W69ZFI's FQ is its own table, so FQ(2) is a
      *>   subscripted data item, not a call through the container's
      *>   global function-pointer FQ.
      *> EXPECTED OUTPUT, DERIVED:
      *>   IN A=000000015     FUNCTION FP(5) -> W69ZF3: 5 * 3.
      *>   IN B=000000012     FP(4) (the FUNCTION-omitted form) -> 4 * 3.
      *>   IN C=0022          FQ(2) is the local table's element 2.
      *>   OUT A=000000010    the container's own FP -> W69ZF2: 5 * 2.
      *>   OUT B=000000014    the container's FQ -> W69ZF2: 7 * 2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W69ZF2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 2
           GOBACK.
       END FUNCTION W69ZF2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. W69ZF3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-ARG PIC S9(4).
       01 L-RES PIC S9(9).
       PROCEDURE DIVISION USING L-ARG RETURNING L-RES.
           COMPUTE L-RES = L-ARG * 3
           GOBACK.
       END FUNCTION W69ZF3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZFO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION W69ZF2
           FUNCTION W69ZF3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP GLOBAL USAGE FUNCTION-POINTER TO W69ZF2.
       01 FQ GLOBAL USAGE FUNCTION-POINTER TO W69ZF2.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
       P-MAIN.
           SET FP TO ADDRESS OF FUNCTION W69ZF2
           SET FQ TO ADDRESS OF FUNCTION W69ZF2
           CALL "W69ZFI"
           COMPUTE R = FUNCTION FP(5)
           DISPLAY "OUT A=" R
           COMPUTE R = FQ(7)
           DISPLAY "OUT B=" R
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W69ZFI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP USAGE FUNCTION-POINTER TO W69ZF3.
       01 T.
           05 FQ PIC 9(4) OCCURS 3 TIMES.
       01 R PIC 9(9).
       PROCEDURE DIVISION.
       P-MAIN.
           SET FP TO ADDRESS OF FUNCTION W69ZF3
           COMPUTE R = FUNCTION FP(5)
           DISPLAY "IN A=" R
           COMPUTE R = FP(4)
           DISPLAY "IN B=" R
           MOVE 11 TO FQ(1)
           MOVE 22 TO FQ(2)
           MOVE 33 TO FQ(3)
           DISPLAY "IN C=" FQ(2)
           GOBACK.
       END PROGRAM W69ZFI.
       END PROGRAM W69ZFO.
