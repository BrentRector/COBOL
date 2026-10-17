*>--- conformance-case
*> id: pb151_options_fill
*> rules: [GR-14.9.3.4-8, GR-14.9.3.4-9]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB151OF.
       OPTIONS.
           INITIALIZE ALL TO X"51".
      *> The fill literal is X"51" (= 'Q') and NOT "Q": 11.9.10.3 SR1 says
      *> "literal-1 shall specify a one-byte hexadecimal-alphanumeric
      *> literal", and "hexadecimal-alphanumeric" is 8.3.3.2.2's FORMAT 2
      *> (X"nn"), not loose wording for "any short quoted literal". This
      *> golden shipped with the format-1 spelling, which is nonconforming
      *> source; kb/Work PB152 landed the SR1 screen (COBOLNET1727) and
      *> repaired it. 0x51 IS 'Q', so FILL=QQQ is unchanged byte for byte.
      *> kb/Work PB151 - ISO 14.9.3.4 GR8: with no INITIALIZED phrase the
      *> allocated content "depends on the INITIALIZE clause of the
      *> OPTIONS paragraph. If it is specified, the content is that of
      *> the specified-fill-character" - the clause previously had ZERO
      *> consumers and every allocation space-filled. The INITIALIZED
      *> phrase still wins with binary zeros (GR6), pinned by the
      *> low-value comparison leg.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 B3 PIC X(3) BASED.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE 3 CHARACTERS RETURNING P
           SET ADDRESS OF B3 TO P
           DISPLAY "FILL=" B3
           ALLOCATE 3 CHARACTERS INITIALIZED RETURNING P
           SET ADDRESS OF B3 TO P
           IF B3 = LOW-VALUES DISPLAY "INIT=ZEROS" END-IF
           STOP RUN.
