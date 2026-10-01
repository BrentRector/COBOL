      *> reject-at: 85
      *> The floating literal continuation indicator (ISO 6.2.3.1) is a
      *> COBOL-2002 introduction: COBOL-85 fixed-form source continues
      *> a literal only with the column-7 hyphen. At --std 85 the
      *> indicator is refused with the introduction band COBOLNET0900
      *> (constructs.json floating-literal-continuation-2002; kb/Work
      *> PB1359). The 2002 twin is the positive golden
      *> 2002/pb1359_floating_continuation_fixed.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1359G85.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(12) VALUE "AB"-
000600     "CD".
000700 PROCEDURE DIVISION.
000800 DISPLAY X.
000900     STOP RUN.
