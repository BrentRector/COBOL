      *> kb/Work PB2739 - a data name that merely ENDS in -ID (CUSTOMER-ID)
      *> is not a PROGRAM-ID / CLASS-ID / ... paragraph header, so a MOVE
      *> to it does not put the text back in the IDENTIFICATION DIVISION,
      *> and a PROCEDURE DIVISION paragraph named REMARKS copied from a
      *> library after it is program text (6.5 5), not a comment-entry.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB2739ID.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 CUSTOMER-ID PIC 9 VALUE 1.
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     MOVE 2 TO CUSTOMER-ID.
000900     PERFORM REMARKS.
001000     STOP RUN.
001100     COPY PB2739ID.
