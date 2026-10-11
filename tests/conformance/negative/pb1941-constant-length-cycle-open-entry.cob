      *> reject-at: 2002 2014 2023
      *> kb/Work PB1941 - 13.10.3 SR4: "The length of data-name-1 or data-name-2 shall not be dependent, directly
      *> or indirectly, upon the value of constant-name-1". A3's OCCURS clause demands K3, and K3 measures W3, an
      *> entry subordinate to A3. Measuring a subordinate of the entry being described is legal by itself
      *> (conformance/2002/pb1941_constant_length_inside_open_entry), but W3's own PICTURE is X(K3), so the
      *> length K3 measures depends on K3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1941CYE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A3 OCCURS K3.
             10 W3 PIC X(K3).
       01 K3 CONSTANT AS LENGTH OF W3 (1).
       PROCEDURE DIVISION.
           DISPLAY K3
           STOP RUN.
