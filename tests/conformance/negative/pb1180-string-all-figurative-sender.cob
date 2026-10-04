      *> reject-at: 85 2002 2014 2023
      *> ISO 14.9.43.3 SR2 (kb/Work PB1180): "Literal-1 or literal-2 shall not be a figurative constant that begins with
      *> the word ALL." `ALL ZEROES` begins with ALL; the figurative's binder arm dropped the word, so the program
      *> compiled and moved ONE '0' ([0........ ]). The STRING sender, the STRING delimiter and UNSTRING's delimiter
      *> now ask the written operand's first token (StringUnstringOperandScreenTests holds every position).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1180NALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OUT1 PIC X(10) VALUE ".........".
       PROCEDURE DIVISION.
       MAIN.
           STRING ALL ZEROES DELIMITED BY SIZE INTO OUT1
           STOP RUN.
