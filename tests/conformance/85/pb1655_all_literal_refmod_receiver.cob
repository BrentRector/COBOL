      *> kb/Work PB1655 - MOVE ALL literal-1 TO a reference-modified receiver.
      *> ISO 8.3.3.6.4 GR2: the string of characters of a figurative
      *> constant "is repeated character by character until the size of the
      *> resultant string is greater than or equal to the number of character
      *> positions in the associated data item", then truncated from the
      *> right to that size; 8.4.3.3.4 GR5 makes a reference-modified
      *> identifier that data item. The slice length may be known only at
      *> run time (a variable leftmost, the omitted-length form), so the
      *> repetition happens at run time. Before the fix the literal was
      *> stored ONCE and the rest of the slice space-filled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1655ALR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(9) VALUE ALL "-".
       01 G.
          05 G1 PIC X(3) VALUE "GGG".
          05 G3.
             10 G31 PIC X(2) VALUE "PQ".
             10 G32 PIC X(2) VALUE "RS".
       66 RALL RENAMES G1 THRU G3.
       01 NUM6 PIC 9(6) VALUE 111111.
       01 P PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
      *> A one-character literal fills every position of the slice.
           MOVE ALL "*" TO T(7:3).
           DISPLAY "T1=[" T "]".
      *> A two-character literal repeats and is truncated from the right.
           MOVE ALL "-" TO T.
           MOVE ALL "AB" TO T(P:5).
           DISPLAY "T2=[" T "]".
      *> The omitted-length form: the slice runs to the end of the item.
           MOVE ALL "-" TO T.
           MOVE ALL "xy" TO T(4:).
           DISPLAY "T3=[" T "]".
      *> A group slice and a RENAMES-alias slice.
           MOVE ALL "*" TO G(4:3).
           DISPLAY "G1=[" G "]".
           MOVE ALL "#" TO RALL(2:3).
           DISPLAY "G2=[" G "]".
      *> A slice of a numeric DISPLAY item is alphanumeric (8.4.3.3.4 GR6).
           MOVE ALL "12" TO NUM6(2:3).
           DISPLAY "N1=[" NUM6 "]".
           STOP RUN.
