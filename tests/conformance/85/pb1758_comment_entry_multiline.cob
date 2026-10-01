      *> kb/Work PB1494, PB1758 - the COBOL-85 comment-entry. At
      *> COBOL-85 the IDENTIFICATION DIVISION's AUTHOR, INSTALLATION,
      *> DATE-WRITTEN, DATE-COMPILED and SECURITY paragraphs hold a
      *> free-text COMMENT-ENTRY that runs to the next Area-A word,
      *> with embedded periods, quotes and reserved words (kb/Work R61:
      *> accepted at 85, rejected from 2002). The logical conversion
      *> keeps the paragraph HEADER and discards the entry.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1758ML.
000300 AUTHOR. A. WRITER, "THE" AUTHOR.
000400 INSTALLATION. FEDERAL COMPUTER CENTER.
000500     5203 LEESBURG PIKE. AUTOMATED DATA AND
000600     TELECOMMUNICATION SERVICE (ADTS), MOVE TO DISPLAY.
000700 DATE-WRITTEN. 1985.
000800 SECURITY. NONE.
000900 PROCEDURE DIVISION.
001000     DISPLAY "OK".
001100     STOP RUN.
