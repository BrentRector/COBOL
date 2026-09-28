      *> ISO §8.4.6.2.1 3) b) 1. across name classes - a containing
      *> program's GLOBAL data-name hides a FARTHER container's GLOBAL
      *> constant-name of the same spelling (kb/Work PB1047)
      *> RULE §8.4.6.2.1 3) b) 1.: "The item in source element A if the
      *>   name is declared in source element A."  cite.py --check
      *>   8.4.6.2.1 "The item in source element A if the name is
      *>   declared in source element A" -> OK §8.4.6.2.1 3) b) 1.
      *> RULE §8.4.6.2.2: "A constant-name, file-name, record-name,
      *>   report-name, screen-name, or type-name described with a
      *>   GLOBAL clause is a global name. All data-names and
      *>   screen-names subordinate to a global name are global names."
      *>   cite.py --check 8.4.6.2.2 "All data-names and screen-names
      *>   subordinate to a global name are global names" -> OK
      *> (The constant entry is COBOL-2002, §13.10; hence this edition.)
      *> NESTING: PB1047K (outer) declares CONSTANT K GLOBAL "OUK" and
      *>   contains PB1047N (middle, a GLOBAL group H holding K "MDK")
      *>   and PB1047S (declares no K). PB1047N contains PB1047T.
      *> EXPECTED OUTPUT, DERIVED:
      *>   T K=MDK  PB1047T declares no K; its container PB1047N does
      *>            (a global data-name) - 3) b) 1. - so the outer
      *>            constant, one element farther, is not referenced.
      *>   S K=OUK  control: PB1047S's only K is the outer constant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT GLOBAL AS "OUK".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047N"
           CALL "PB1047S"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H GLOBAL.
          05 K PIC X(3) VALUE "MDK".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047T"
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047T.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "T K=" K
           EXIT PROGRAM.
       END PROGRAM PB1047T.
       END PROGRAM PB1047N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047S.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "S K=" K
           EXIT PROGRAM.
       END PROGRAM PB1047S.
       END PROGRAM PB1047K.
