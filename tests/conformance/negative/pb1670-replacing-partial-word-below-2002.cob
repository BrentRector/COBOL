      *> reject-at: 85
      *> The LEADING / TRAILING partial-word phrases of COPY REPLACING
      *> (7.2.3.2) and REPLACE (7.2.4.2) are a COBOL-2002 introduction
      *> (constructs row replacing-partial-word-2002, VCR row 7.33;
      *> kb/Work PB1670): ANSI X3.23-1985 admits pseudo-text, identifier,
      *> literal and word operands only. Each of the two statements
      *> below draws COBOLNET0900 at --std 85.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1670N.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 COPY PB1670NB REPLACING LEADING ==PFX-== BY ==C1-==.
000600 REPLACE TRAILING ==-R== BY ==-E==.
000700 01 BETA-R PIC X(2) VALUE "RB".
000800 PROCEDURE DIVISION.
000900     DISPLAY C1-ONE BETA-E.
001000     STOP RUN.
