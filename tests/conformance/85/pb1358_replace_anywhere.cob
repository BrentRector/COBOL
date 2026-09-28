      *> kb/Work PB1358 - a REPLACE statement is recognized wherever the
      *> character-string REPLACE is a text-word, not only as the first
      *> word of a line. RULES (cite.py --check):
      *>   7.2.4.3 1) "A REPLACE statement may be specified anywhere in
      *>     source text or in library text that a character-string
      *>     or a separator ... may appear"                  -> OK 1)
      *>   7.2.4.3 2) "shall be preceded by a space except when it is
      *>     the first statement in a compilation group"     -> OK 2)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [START] then [AAA]: the REPLACE after DISPLAY on one line is
      *>         processed (before: COBOL0001 near '=').
      *>   [B-C]  a REPLACE INSIDE a DISPLAY statement vanishes; the
      *>         word after it is replaced: DISPLAY "[" "B-C" "]".
      *>   [Y]    REPLACE-FLAG is one text-word, not a REPLACE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1358RA85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AAA PIC X(3) VALUE "AAA".
       01 REPLACE-FLAG PIC X VALUE "Y".
       PROCEDURE DIVISION.
           DISPLAY "[START]". REPLACE ==XX1== BY ==AAA==.
           DISPLAY "[" XX1 "]".
           DISPLAY "[" REPLACE ==XX2== BY =="B-C"==. XX2 "]".
           DISPLAY "[" REPLACE-FLAG "]".
           STOP RUN.
