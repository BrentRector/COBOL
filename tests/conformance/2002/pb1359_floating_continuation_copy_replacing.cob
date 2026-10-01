      *> kb/Work PB1359 - a literal continued with the FLOATING indicator
      *> inside COPY REPLACING pseudo-text. 7.2.3.3 SR8 lets pseudo-text be
      *> continued by the rules of reference format; 6.5 4) and 8) join the
      *> three lines BEFORE text manipulation, so pseudo-text-1 is the
      *> literal "HELLO", which matches the library text's "HELLO" and
      *> pseudo-text-2 is "WORLD".
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1359CP.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500     COPY PB1359BK REPLACING =="HEL"-
000600         "LO"== BY =="WOR"-
000700         "LD"==.
000800 PROCEDURE DIVISION.
000900     DISPLAY AA-X.
001000     STOP RUN.
