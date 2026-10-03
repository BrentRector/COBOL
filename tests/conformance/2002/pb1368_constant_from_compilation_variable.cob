      *> kb/Work PB1368 / PB1228 - CONSTANT ... FROM compilation-variable-name-1 (ISO 13.10, 2002).
      *> 7.3.11.4 GR1: "In text that follows a DEFINE directive specifying compilation-variable-name-1 without the
      *> OFF phrase, compilation-variable-name-1 may be used ... in a constant entry where the FROM phrase is
      *> specified." 13.10.4 GR1: the constant is "as if ... the text represented by compilation-variable-name-1
      *> were written where constant-name-1 is written"; GR2: its class and category are those of that literal.
      *>   CN = 3     a numeric literal (an integer, so it may set a PICTURE repetition, 13.10.3 SR2)
      *>   CA = ABC   an alphanumeric literal
      *>   CE = 14    an arithmetic expression, 2 + 3 * 4 (7.3.11.4 GR6 evaluates it when the DEFINE is processed)
      *>   CR = 2.5   a non-integer numeric literal: R2 = CR * 2 = 5.0
      *>   J  = 42    FROM CQ names the COMPILATION VARIABLE CQ, never the constant CQ (= 7) beside it:
      *>              8.3.2.2 1) "a compilation-variable-name may be the same as any other type of user-defined word"
      *>   CT = 1     the definition in force AT THE ENTRY: T is redefined to 2 (OVERRIDE) only after CT
      *>   CU = 2     ... and CU, written after the redefinition, reads 2
      *>   W  = ZZZ   PIC X(CN)
      *> The negative halves: negative/constant-from-compilation-variable (never defined, 13.10.3 SR8),
      *> negative/pb1368-constant-from-after-define-off (7.3.11.4 GR2) and
      *> negative/pb1368-constant-from-defined-later (a DEFINE after the entry does not reach it).
       >>DEFINE N AS 3
       >>DEFINE A AS "ABC"
       >>DEFINE E AS 2 + 3 * 4
       >>DEFINE R AS 2.5
       >>DEFINE CQ AS 42
       >>DEFINE T AS 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368CFROM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CN CONSTANT FROM N.
       01 CA CONSTANT FROM A.
       01 CE CONSTANT FROM E.
       01 CR CONSTANT FROM R.
       01 CQ CONSTANT AS 7.
       01 J  CONSTANT FROM CQ.
       01 CT CONSTANT FROM T.
       >>DEFINE T AS 2 OVERRIDE
       01 CU CONSTANT FROM T.
       01 W  PIC X(CN).
       01 R2 PIC 9V9.
       PROCEDURE DIVISION.
           MOVE ALL "Z" TO W
           COMPUTE R2 = CR * 2
           DISPLAY "CN=" CN " CA=" CA " CE=" CE " J=" J " CQ=" CQ
           DISPLAY "CT=" CT " CU=" CU " W=" W " R2=" R2
           STOP RUN.
