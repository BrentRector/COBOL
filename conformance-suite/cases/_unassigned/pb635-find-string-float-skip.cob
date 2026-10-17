*>--- conformance-case
*> id: pb635-find-string-float-skip
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB635 - THE MIXED-CLASS HALF OF THE ISO 15.3 TYPE-6 SCREEN, PINNED PER FUNCTION.
*> FIND-STRING is the one CATALOGUED function that mixes a STRING argument position with an INTEGER one:
*> 15.37.3 r1 "Argument-1 shall be a data item or literal of class alphabetic, alphanumeric, or national",
*> r2 the same class for argument-2, and r3 "argument-3 shall be an integer data item or integer literal".
*> A floating-point item at argument-3 is therefore refused by exactly the rule PB248 derived for every
*> other type-6 position: 15.3 type 6 admits "an arithmetic expression that will always result in an
*> integer value or an integer data item", and 14.6.8.3 sets a floating-point item's content to "the
*> algebraic value of the sending operand", so its DECLARED value set contains non-integers whatever this
*> reference happens to hold. That the run stores 1.0E0 is the same irrelevance as a PIC 9V9 holding 1.0.
*>
*> IT IS PINNED SEPARATELY FROM pb248-integer-arg-float-item BECAUSE THE FUNCTION IS THE ONE WHOSE OTHER
*> POSITIONS ARE STRINGS. That is what made PB635 possible: with the argument admitted under --permissive,
*> the emitter asked "is ANY argument floating" and moved the WHOLE call to the binary64 lane, converting
*> WS-HAY and WS-NDL to double and dropping LAST/ANYCASE. Strict never reaches that lane - this file is
*> the assertion that it does not - and the permissive lane's values are asserted by
*> FloatIntegerArgumentPermissiveTests.Pb635FindString_UnderPermissive_KeepsItsOwnLaneAndItsPhrases.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB635FSF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H     PIC X(12) VALUE "abcABCabcABC".
       01 ND    PIC X(3)  VALUE "ABC".
       01 FSK   USAGE FLOAT-LONG.
       01 P     PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE FSK = 1.0E0
           MOVE FUNCTION FIND-STRING(H ND START AFTER FSK) TO P
           DISPLAY "P=" P
           STOP RUN.
