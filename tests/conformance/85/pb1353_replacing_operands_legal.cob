      *> kb/Work PB1353 - the operand rules REJECT only what they name;
      *> the legal operand forms stay legal. RULES (cite.py --check):
      *>   7.2.4.3 3) pseudo-text-1: "one or more text-words, at least
      *>     one of which shall be neither a separator comma nor a
      *>     separator semicolon"                            -> OK 3)
      *>   7.2.4.3 4) "Pseudo-text-2 shall contain zero, one, or more
      *>     text-words"                                     -> OK 4)
      *>   7.2.2.5 2) a literal INCLUDING its delimiters is one
      *>     text-word, so an == inside it is content        -> OK 2)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [A==B] pseudo-text-2 is the one literal "A==B" (a reader
      *>          that ends pseudo-text at the first == splits it).
      *>   [ZZ]   ==XX2 , XX3== holds a separator comma BESIDE words:
      *>          legal, and it matches "XX2, XX3" (comma = space).
      *>   []     ==XX4== BY ==== deletes the word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1353OK85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
           REPLACE ==XX1== BY =="A==B"==
                   ==XX2 , XX3== BY =="ZZ"==
                   ==XX4== BY ====.
           DISPLAY "[" XX1 "]".
           DISPLAY "[" XX2, XX3 "]".
           DISPLAY "[" XX4 "]".
           STOP RUN.
