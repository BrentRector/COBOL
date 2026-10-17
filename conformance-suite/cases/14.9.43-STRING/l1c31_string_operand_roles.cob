      *>--- conformance-case
      *> id: l1c31_string_operand_roles
      *> rules: [SR-14.9.43.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.43.3 10) — literal-1/identifier-1 send, identifier-3
      *> receives.
      *> Rule: "Literal-1 or the data item referenced by identifier-1 is
      *> the sending operand. The data item referenced by identifier-3
      *> is the receiving operand."
      *> cite.py --check 14.9.43.3 "Literal-1 or the data item
      *>   referenced by identifier-1 is the sending operand."
      *>   -> OK  §14.9.43.3 10)  (Syntax rules)
      *> Supporting: §14.9.43.4 GR3 a) (characters go FROM the sending
      *> operand TO identifier-3), GR7 (only the referenced portion of
      *> identifier-3 changes).
      *> SND (identifier-1) = "ABCD", RCV (identifier-3) = 8 '-'.
      *> T1  STRING SND "XY" DELIMITED BY SIZE INTO RCV
      *>     sending operands SND then literal "XY" -> RCV receives
      *>     ABCD then XY in positions 1-6, 7-8 unchanged: ABCDXY--;
      *>     SND, a sending operand, is not changed: ABCD.
      *> T2  STRING RCV DELIMITED BY "-" INTO SND
      *>     roles follow position, not declaration: RCV now SENDS,
      *>     delimited at its first '-' (position 7). SND is first set
      *>     to "wxyz"; "ABCDXY" is transferred into SND (4 chars) until
      *>     SND is full: SND = ABCD. RCV, now the sending operand, is
      *>     unchanged: ABCDXY--.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SND PIC X(4) VALUE "ABCD".
       01 RCV PIC X(8) VALUE ALL "-".
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING SND "XY" DELIMITED BY SIZE INTO RCV.
           DISPLAY "T1 SND=" SND " RCV=" RCV.
           MOVE "wxyz" TO SND.
           STRING RCV DELIMITED BY "-" INTO SND.
           DISPLAY "T2 SND=" SND " RCV=" RCV.
           STOP RUN.
