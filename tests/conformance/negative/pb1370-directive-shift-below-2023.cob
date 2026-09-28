      *> reject-at: 2002 2014
      *> kb/Work PB1370 — a compile-time boolean expression "shall be formed in accordance with 8.8.2, Boolean
      *> expressions" (ISO §7.3.7.2 SR1) of the TARGETED edition, and the boolean shift operators B-SHIFT-L /
      *> B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC are a COBOL-2023 addition (Annex E.2 3)). Below 2023 a shift inside a
      *> >>DEFINE or >>IF expression is therefore the same COBOLNET0900 its runtime twin (COMPUTE B1 = B1 B-SHIFT-L 1)
      *> draws. It compiled clean at 2002 and 2014: the introduction gate lived only in the compilation-unit walk,
      *> which never reaches a directive fragment. The directive itself is a 2002 construct, so 2002 and 2014 are
      *> exactly the editions where only the SHIFT is out of edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W66NDS.
       PROCEDURE DIVISION.
       MAIN.
       >>DEFINE G AS B"1100" B-SHIFT-L 1
       >>IF B"1100" B-SHIFT-R 1 = B"0110"
           DISPLAY "SHIFTED".
       >>END-IF
           STOP RUN.
