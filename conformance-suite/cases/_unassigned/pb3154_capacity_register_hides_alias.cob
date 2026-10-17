*>--- conformance-case
*> id: pb3154_capacity_register_hides_alias
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3154 review R2-1 - a contained program's named OCCURS
      *>   DYNAMIC CAPACITY register HIDES its container's SYMBOLIC
      *>   CHARACTERS name and GLOBAL constant-name of the same spelling.
      *> ISO 13.18.38.3 SR30: "Data-name-3 shall not be defined elsewhere
      *>   in the source element" -> OK 13.18.38.3 30); the register is
      *>   a data-name of the element that declares the table.
      *> ISO 8.4.6.1: "even when it is a different type of user-defined
      *>   word, is to the item which that source element describes"
      *>   -> OK 8.4.6.1.
      *> ISO 8.4.6.2.1 3) a): "If the name is declared in source element
      *>   B, the item in source element B is the referenced item"
      *>   -> OK 8.4.6.2.1 3) a).
      *> NESTING: PB3154RO declares the symbolic-character S1 (67) and
      *>   the GLOBAL constant K (5). It contains PB3154RI, whose dynamic
      *>   tables name their CAPACITY registers S1 (FROM 3) and K
      *>   (FROM 4).
      *> EXPECTED OUTPUT, DERIVED:
      *>   INNER S1=3   PB3154RI's own register S1, the capacity 3.
      *>   INNER K=4    PB3154RI's own register K, the capacity 4.
      *>   OUTER B 5    the outer program's own names: ordinal 67 =
      *>                X"42" = "B" (12.3.7.4 GR11) and the constant 5.
      *> Before the fix the register was not asked: S1 moved the
      *>   figurative ALL "B" (refused COBOLNET0902 at 2023) and K
      *>   showed the outer constant 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154RO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S1 IS 67.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT GLOBAL AS 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB3154RI".
           DISPLAY "OUTER " S1 " " K.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3154RI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC 9 OCCURS DYNAMIC CAPACITY IN S1 FROM 3 TO 9.
       01 U.
          05 F PIC 9 OCCURS DYNAMIC CAPACITY IN K FROM 4 TO 9.
       01 N PIC 9.
       PROCEDURE DIVISION.
       INNER-PARA.
           MOVE S1 TO N.
           DISPLAY "INNER S1=" N.
           MOVE K TO N.
           DISPLAY "INNER K=" N.
       END PROGRAM PB3154RI.
       END PROGRAM PB3154RO.
