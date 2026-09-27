*> reject-at: 85
      *> The floating comment indicator *> (ISO §6.2.3.1) is a COBOL-
      *>   2002 introduction: COBOL-85 fixed-form source has only the
      *>   fixed comment indicators * and / (these header lines). At
      *>   --std 85 the inline *> is refused with the introduction band
      *>   COBOLNET0900 (constructs.json floating-comment-indicator-2002;
      *>   kb/Work PB1493). The 2002 twin is the positive golden
      *>   2002/pb1493_floating_comment_fixed.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1493G85.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500  01 X PIC X(12).
000600 PROCEDURE DIVISION.
000700     MOVE "ABC" TO X. *> an inline comment
000800     STOP RUN.
