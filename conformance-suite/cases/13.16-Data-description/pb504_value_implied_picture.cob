*>--- conformance-case
*> id: pb504_value_implied_picture
*> rules: [SR-13.16.3-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.16.3 SR9, the VALUE-implied PICTURE (kb/Work PB504; the ALL-literal length is kb/Work PB831,
      *> the figurative admission is kb/Work PB828).  The rule had NO implementation site: BindEntry's pic
      *> chain is keyed on USAGE and has no arm keyed on the VALUE literal, so every program below was either
      *> rejected by the 13.16.3 SR8 closing guard or, for the figurative spellings, given the ONE-CHARACTER
      *> recovery item and silently truncated.  Every value here is derived from the standard.
      *>
      *> 13.16.3 SR9: "The PICTURE clause may be omitted for an elementary item when an alphanumeric, boolean,
      *> or national literal that is not a zero-length literal is specified in the data-item format of the
      *> VALUE clause.  A PICTURE clause is implied as follows: a) if the literal is alphanumeric,
      *> 'PICTURE X(length)' b) if the literal is boolean, 'PICTURE 1(length)' c) if the literal is national,
      *> 'PICTURE N(length)' where length is the length of the literal as specified in 8.3.3, Literals."
      *> 8.3.3.6.4 GR3: the length of a figurative constant whose context does not specify one - "b) When a
      *> figurative constant is other than ALL literal-1, the length of the string is one character.
      *> ... c) The length of the string is the length of literal-1."
      *> 8.3.3.6.4 GR1: a figurative constant "used in a context requiring national characters ... represents
      *> a national character value.  Otherwise ... an alphanumeric character value."
      *> 13.18.60.3 SR13 a): "if the explicit or IMPLICIT picture character-string contains the symbol 'N', a
      *> USAGE NATIONAL clause is implied" - the standard's own word for what SR9 supplies.
      *>
      *> DERIVATIONS (15.50.4 - FUNCTION LENGTH is the item's character positions):
      *>   ALNUM  "HELLO" is alphanumeric, 5 characters      -> SR9 a) PICTURE X(5), value HELLO,  LENGTH 5
      *>   HEXAL  X"414243" is alphanumeric (8.3.3.2), and 8.3.3.2.3 r6 groups its digits two per character
      *>          -> 3 characters                            -> SR9 a) PICTURE X(3), value ABC,    LENGTH 3
      *>   NATNL  N"HELLO" is national, 5 characters         -> SR9 c) PICTURE N(5), value HELLO,  LENGTH 5
      *>   BOOLN  B"1011" is boolean, 4 boolean characters   -> SR9 b) PICTURE 1(4), value 1011,   LENGTH 4
      *>   FIGSP  SPACE is not ALL literal-1, so GR3 b) gives length 1; GR1's "otherwise" arm makes it
      *>          alphanumeric                               -> SR9 a) PICTURE X(1), value " ",    LENGTH 1
      *>   ALLAB  ALL "AB" IS ALL literal-1, so GR3 c) gives literal-1's length, 2
      *>                                                     -> SR9 a) PICTURE X(2), value AB,     LENGTH 2
      *>   ALLA   ALL "A"  - the same rule at length 1       -> SR9 a) PICTURE X(1), value A,      LENGTH 1
      *>   NATSP  A VALUE SPACE under a group whose USAGE NATIONAL clause applies to it (13.18.60.4 GR1) is a
      *>          context requiring national characters, so GR1's first arm and GR3 b) give it
      *>                                                     -> SR9 c) PICTURE N(1),                LENGTH 1
      *>   NATAB  ALL N"AB" - GR3 c) on a national literal-1 -> SR9 c) PICTURE N(2), value AB,     LENGTH 2
      *>   GROUP  the implied member composes into its group's character image exactly as a written one does:
      *>          X(5) "HELLO" then X(3) "XYZ"               -> HELLOXYZ, LENGTH 8
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB504IP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ALNUM VALUE "HELLO".
       01 HEXAL VALUE X"414243".
       01 NATNL VALUE N"HELLO".
       01 BOOLN VALUE B"1011".
       01 FIGSP VALUE SPACE.
       01 ALLAB VALUE ALL "AB".
       01 ALLA  VALUE ALL "A".
       01 NATGRP USAGE NATIONAL.
          05 NATSP VALUE SPACE.
          05 NATAB VALUE ALL N"AB".
       01 GROUP-R.
          05 GA VALUE "HELLO".
          05 GB PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "ALNUM=[" ALNUM "] " FUNCTION LENGTH(ALNUM)
           DISPLAY "HEXAL=[" HEXAL "] " FUNCTION LENGTH(HEXAL)
           DISPLAY "NATNL=[" NATNL "] " FUNCTION LENGTH(NATNL)
           DISPLAY "BOOLN=[" BOOLN "] " FUNCTION LENGTH(BOOLN)
           DISPLAY "FIGSP=[" FIGSP "] " FUNCTION LENGTH(FIGSP)
           DISPLAY "ALLAB=[" ALLAB "] " FUNCTION LENGTH(ALLAB)
           DISPLAY "ALLA=[" ALLA "] " FUNCTION LENGTH(ALLA)
           DISPLAY "NATSP=" FUNCTION LENGTH(NATSP)
           DISPLAY "NATAB=[" NATAB "] " FUNCTION LENGTH(NATAB)
           DISPLAY "GROUP=[" GROUP-R "] " FUNCTION LENGTH(GROUP-R)
           MOVE "WORLD" TO ALNUM
           DISPLAY "MOVED=[" ALNUM "]"
           STOP RUN.
