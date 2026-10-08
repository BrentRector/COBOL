      *> kb/Work PB2639 - the absolute value of the LARGEST-MAGNITUDE
      *>   signed 16-byte binary value, -2**127, is representable: an
      *>   unsigned 16-byte COMP-5 receiver holds 2**127 (its container
      *>   is [0, 2**128), 13.18.60.4 GR12).
      *>   14.9.25.4 GR6 b: "When an unsigned numeric item is the
      *>   receiving item, the absolute value of the sending value is
      *>   used". 14.7.5 rule 3: a size error only when the result is
      *>   "further from zero than permitted for the associated
      *>   resultant data item" - 2**127 is not.
      *>   S holds the 16-byte signed minimum 8000...00 (the container's
      *>   own bit pattern, written through the alphanumeric overlay).
      *>   Expected: no size error, U holds 2**127, whose container
      *>   image is 8000...00 again; the no-phrase statement stores it too.
      *>   cite.py --check 14.9.25.4 "When an unsigned numeric item is
      *>   the receiving item, the absolute value of the sending value
      *>   is used" -> OK 14.9.25.4 6)
      *>   cite.py --check 14.7.5 "further from zero than permitted for
      *>   the associated resultant data item" -> OK 14.7.5 3)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2639MV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S PIC S9(19) COMP-5.
       01 GX REDEFINES G PIC X(16).
       01 H.
          05 U PIC 9(19) COMP-5 VALUE 7.
       01 HX REDEFINES H PIC X(16).
       PROCEDURE DIVISION.
       MAIN.
           MOVE X"80000000000000000000000000000000" TO GX.
           COMPUTE U = S
               ON SIZE ERROR DISPLAY "size error"
               NOT ON SIZE ERROR DISPLAY "stored"
           END-COMPUTE.
           IF HX = X"80000000000000000000000000000000"
               DISPLAY "U holds 2**127"
           ELSE
               DISPLAY "U was not stored".
           MOVE X"00000000000000000000000000000007" TO HX.
           COMPUTE U = S.
           IF HX = X"80000000000000000000000000000000"
               DISPLAY "no-phrase U holds 2**127"
           ELSE
               DISPLAY "no-phrase U was not stored".
           STOP RUN.
