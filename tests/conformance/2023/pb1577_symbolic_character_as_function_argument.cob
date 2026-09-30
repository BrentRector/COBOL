      *> PB1577 - ISO 8.3.3.6 SR1 / 12.3.7.4 GR11: a symbolic-character is a
      *>   figurative constant, usable wherever a figurative constant is.
      *>   FUNCTION ORD(SPACE) always compiled; FUNCTION ORD(S67) drew
      *>   COBOLNET1639 "not defined" about a name SPECIAL-NAMES declared.
      *> cite.py --check 12.3.7.4 "the symbolic-character-1 is a figurative
      *>   constant" - see 12.3.7.4 GR11 (checked below)
      *> Derivation: S67 is the character at ordinal position 67 of the
      *>   native character set; ORD returns a character's ordinal
      *>   position, so ORD(S67) = 67; ORD(SPACE) = 33 (U+0020).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1577.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS S67 IS 67.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 999.
       PROCEDURE DIVISION.
           MOVE FUNCTION ORD(S67) TO N
           DISPLAY "S67=" N
           MOVE FUNCTION ORD(SPACE) TO N
           DISPLAY "SPACE=" N
           STOP RUN.
