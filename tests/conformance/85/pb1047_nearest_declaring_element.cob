      *> ISO §8.4.6.2.1 - a name duplicated across nested programs
      *> names the item of the NEAREST source element that declares
      *> it, after qualification (kb/Work PB1047 / PB1243)
      *> RULE §8.4.6.2.1 1): "The set of names to be used for
      *>   determination of a referenced item consists of all names
      *>   that are defined in source element B and all global names
      *>   that are defined in source element A and in any source
      *>   elements that directly or indirectly contain source element
      *>   A. Using this set of names, the normal rules for
      *>   qualification ... are applied until one or more items is
      *>   identified."
      *> RULE §8.4.6.2.1 3) a): "If the name is declared in source
      *>   element B, the item in source element B is the referenced
      *>   item."  cite.py --check 8.4.6.2.1 "If the name is declared in
      *>   source element B, the item in source element B is the
      *>   referenced item" -> OK §8.4.6.2.1 3) a)
      *> RULE §8.4.6.2.1 3) b) 1.: "The item in source element A if the
      *>   name is declared in source element A."  cite.py --check
      *>   8.4.6.2.1 "The item in source element A if the name is
      *>   declared in source element A" -> OK §8.4.6.2.1 3) b) 1.
      *> RULE §8.4.6.2.2: "All data-names and screen-names subordinate
      *>   to a global name are global names ... All condition-names
      *>   associated with a global name are global names."
      *>   cite.py --check 8.4.6.2.2 "All data-names and screen-names
      *>   subordinate to a global name are global names" -> OK
      *> RULE §8.4.6.2.3: "the scope of an index-name is identical to
      *>   that of the data-name that names the table" (cite.py --check
      *>   8.4.6.2.3 -> OK).
      *> RULE §13.18.27.4 GR2: "A statement in a program contained
      *>   directly or indirectly within a program that describes a
      *>   global name may reference that name without describing it
      *>   again."  cite.py --check 13.18.27.4 "A statement in a program
      *>   contained directly or indirectly within a program that
      *>   describes" -> OK §13.18.27.4 2)
      *> NESTING: PB1047O (outer) contains PB1047M (middle), which
      *>   contains PB1047I and PB1047J. Outer and middle BOTH declare
      *>   a GLOBAL group G; the outer G alone holds Y and Z, the middle
      *>   G alone holds W. Condition-name C: outer's is FALSE, middle's
      *>   is TRUE, PB1047I's own is FALSE.
      *> EXPECTED OUTPUT, DERIVED:
      *>   I X=LOC       3) a): PB1047I declares X (in L).
      *>   I C=FALSE     3) a): PB1047I's own 88 C (false); the middle's
      *>                 C is true, so a wrong pick prints TRUE.
      *>   I IX=7        3) a) across name classes: PB1047I's data-name
      *>                 IX hides the outer table's global index-name.
      *>   I Y=OUY       2): Y is declared only under the OUTER G - a
      *>                 global name although that G's spelling is
      *>                 hidden by the middle's G (§8.4.6.2.2).
      *>   I Z=OUZ W=MDW 2): one item each, from the two G groups.
      *>   I XG=MID      1) qualification first: X OF G excludes the
      *>                 local L's X; of the outer's and the middle's,
      *>                 3) b) 1. picks the middle's.
      *>   J X=MID       3) b) 1.: PB1047J declares none; the middle
      *>                 (source element A) does.
      *>   J C=TRUE      3) b) 1.: the middle's condition-name.
      *>   O Y=NEW       GR2: PB1047I's MOVE to Y wrote the OUTER item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 X PIC X(3) VALUE "OUT".
          05 Y PIC X(3) VALUE "OUY".
          05 Z PIC X(3) VALUE "OUZ".
          05 CV PIC X VALUE "O".
             88 C VALUE "P".
       01 T GLOBAL.
          05 E PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047M"
           DISPLAY "O Y=" Y
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G GLOBAL.
          05 X PIC X(3) VALUE "MID".
          05 W PIC X(3) VALUE "MDW".
          05 CM PIC X VALUE "M".
             88 C VALUE "M".
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "PB1047I"
           CALL "PB1047J"
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L.
          05 X PIC X(3) VALUE "LOC".
          05 LC PIC X VALUE "L".
             88 C VALUE "Q".
       01 IX PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "I X=" X
           IF C
               DISPLAY "I C=TRUE"
           ELSE
               DISPLAY "I C=FALSE"
           END-IF
           DISPLAY "I IX=" IX
           DISPLAY "I Y=" Y
           DISPLAY "I Z=" Z " W=" W
           DISPLAY "I XG=" X OF G
           MOVE "NEW" TO Y
           EXIT PROGRAM.
       END PROGRAM PB1047I.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1047J.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "J X=" X
           IF C
               DISPLAY "J C=TRUE"
           ELSE
               DISPLAY "J C=FALSE"
           END-IF
           EXIT PROGRAM.
       END PROGRAM PB1047J.
       END PROGRAM PB1047M.
       END PROGRAM PB1047O.
