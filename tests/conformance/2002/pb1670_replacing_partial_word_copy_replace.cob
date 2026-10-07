      *> kb/Work PB1670 - the LEADING / TRAILING partial-word phrases of
      *> COPY ... REPLACING (7.2.3.2) and of the format 1 REPLACE
      *> statement (7.2.4.2), at their introducing edition (constructs
      *> row replacing-partial-word-2002: a derived COBOL-2002 edge,
      *> VCR row 7.33). RULES (each run through cite.py --check):
      *>   7.2.3.4 9) b) "When the LEADING phrase is specified,
      *>     partial-word-1 matches the library text only if the
      *>     contiguous sequence of characters that forms partial-word-1
      *>     is equal ... to an equal number of contiguous characters
      *>     starting with the leftmost character position of a library
      *>     text-word"                                       -> OK 9)
      *>   7.2.3.4 9) f) "the library text-word is placed into the
      *>     resultant text with the matched characters either replaced
      *>     by partial-word-2 ..."; "The library text-word immediately
      *>     following the rightmost text-word that participated in the
      *>     match is then considered as the leftmost text-word" -> OK 9)
      *>   7.2.4.4 8) b) the same for the REPLACE statement's source
      *>     text-words                                       -> OK 8)
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [ONE]   library word PFX-ONE: the first operand (LEADING
      *>           ==PFX-==) matches its leftmost characters -> C1-ONE.
      *>   [TWO]   TWO-SFX: the first operand does not match, the next
      *>           (TRAILING ==-SFX==) matches the rightmost characters
      *>           -> TWO-C2.
      *>   [THREE] PFX-THREE-SFX: the first operand matches and the text-
      *>           word is consumed, so the TRAILING operand never sees
      *>           it -> C1-THREE-SFX (a rescan would give C1-THREE-C2).
      *>   [RA]    REPLACE: R-ALPHA -> D-ALPHA (LEADING).
      *>   [RB]    REPLACE: BETA-R -> BETA-E (TRAILING).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1670PW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY pb1670cb REPLACING LEADING ==PFX-== BY ==C1-==
                               TRAILING ==-SFX== BY ==-C2==.
       REPLACE LEADING ==R-== BY ==D-==
               TRAILING ==-R== BY ==-E==.
       01 R-ALPHA PIC X(2) VALUE "RA".
       01 BETA-R PIC X(2) VALUE "RB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" C1-ONE "]"
           DISPLAY "[" TWO-C2 "]"
           DISPLAY "[" C1-THREE-SFX "]"
           DISPLAY "[" D-ALPHA "]"
           DISPLAY "[" BETA-E "]"
           STOP RUN.
