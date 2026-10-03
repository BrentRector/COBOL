      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1660 - ISO 6.3.6: a blank line is one that contains only
      *> space characters between margin C and margin R. The line below holds
      *> a no-break space (U+00A0), which is not the COBOL character space, so
      *> it is not blank: it is program text, and no COBOL word or separator
      *> contains it. It was logically discarded as a blank line.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1660N2.
000300 PROCEDURE DIVISION.
000400  
000500     STOP RUN.
