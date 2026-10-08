      *> reject-at: 2002 2014 2023
      *> kb/Work PB1941 - 13.10.3 SR4: "The length of data-name-1 or data-name-2 shall not be dependent, directly
      *> or indirectly, upon the value of constant-name-1". A's PICTURE demands K while R is being described, and
      *> K measures W, a later entry of the same record. Measuring a later entry of an open record is legal by
      *> itself (conformance/2002/pb1941_constant_length_inside_open_record), but W's own subordinate is X(K),
      *> so the length K measures depends on K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941CYC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(K).
          05 W.
             10 WA PIC X(K).
       01 K CONSTANT AS LENGTH OF W.
       PROCEDURE DIVISION.
           DISPLAY K
           STOP RUN.
