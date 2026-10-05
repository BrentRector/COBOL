*> reject-at: 2002 2014 2023
*> kb/Work PB1407. ISO 8.4.3.11.3 SR4 a) speaks of "subscripting and reference modification in identifier-1", so a
*> reference-modified ADDRESS OF operand is legal (W (3:2) below), but the reference modification is the 8.4.3.3.2 format
*> identifier-1( leftmost-position : [ length ] ) and its leftmost-position is required: ADDRESS OF W (:2) is refused
*> COBOLNET2876 by the ONE reader of the captured modifier, with no second COBOLNET0869 ("mis-subscripted") on top of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407AON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(8) VALUE "ABCDEFGH".
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF W (3:2)
           SET P TO ADDRESS OF W (:2)
           STOP RUN.
