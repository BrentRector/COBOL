      *> reject-at: 2002 2014 2023
      *> kb/Work PB1231 - 13.10.3 SR4: "The length of data-name-1 or data-name-2 shall not be dependent, directly
      *> or indirectly, upon the value of constant-name-1". W is described after K, which is legal by itself, but
      *> W's own PICTURE repetition is K, so the length K measures depends on K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1231LCY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS LENGTH OF W.
       01 W PIC X(K).
       PROCEDURE DIVISION.
           DISPLAY K
           STOP RUN.
