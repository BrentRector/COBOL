      *>--- conformance-case
      *> id: pb732_value_symbolic_character
      *> rules: [GR-8.3.3.6.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
*> kb/Work PB732 POSITIVE TWIN at COBOL-85 — the constant entry (ISO 13.10) is a COBOL-2002 element, so
*> at 85 the ONLY word a VALUE literal position admits is a symbolic-character: 8.3.3.6.2 Format 7 with
*> 8.3.3.6.3 SR4 ("Symbolic-character-1 shall be specified in the SYMBOLIC CHARACTERS clause of the
*> SPECIAL-NAMES paragraph"), whose integer-1 is the ORDINAL POSITION in the native character set
*> (12.3.7.4 GR11) - 66 is 'A'. Both spellings: the bare figurative (length 1, 8.3.3.6.4 GR3 b) and the
*> ALL form (repeated to the receiving size, GR2). The rejection PB732 landed must not reach these.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB732P85.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS SYM-A IS 66.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 S1 PIC X VALUE SYM-A.
01 S2 PIC X(3) VALUE ALL SYM-A.
01 S3 PIC 9 VALUE 5.
   88 S3-HIGH VALUE 5 THRU 9.
PROCEDURE DIVISION.
    DISPLAY "S1=" S1.
    DISPLAY "S2=" S2.
    IF S3-HIGH DISPLAY "S3=Y" ELSE DISPLAY "S3=N" END-IF.
    STOP RUN.
