>>SOURCE FORMAT FREE
*> kb/Work PB1359 - the FLOATING literal continuation indicator, free form (6.4.2: 'When such a
*> literal is incomplete at the end of a line, the incomplete portion of the literal shall be
*> terminated by a floating continuation indicator' - the ONLY way to continue a literal in free
*> form). 6.5 4) and 8) as in the fixed-form golden. A comment line and a blank line between the
*> parts; the first nonblank character of each continuation line is the opening quotation symbol.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1359FR.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 W-A PIC X(20) VALUE "ABC"-
   "DEF".
01 W-B PIC X(12) VALUE 'IT''S'-
*> a comment line between the parts

   ' OK'.
01 W-N PIC N(6) VALUE N"AB"-
   "CD".
01 W-X PIC X(3) VALUE X"4142"-
   "43".
01 W-S PIC X(12) VALUE "AB  "-   
   "CD".
01 W-T PIC X(11) VALUE "ONE"-
   "TWO"-
   "THREE".
PROCEDURE DIVISION.
    DISPLAY "[" W-A "]".
    DISPLAY "[" W-B "]".
    IF W-N = N"ABCD" DISPLAY "N-OK".
    DISPLAY "[" W-X "]".
    DISPLAY "[" W-S "]".
    DISPLAY "[" W-T "]".
    REPLACE ==XX1== BY ==MOVE "HELLO-WO"-
        "RLD" TO W-A==.
    XX1.
    DISPLAY "[" W-A "]".
    STOP RUN.
