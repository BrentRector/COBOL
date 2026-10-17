*>--- conformance-case
*> id: pb492_national_edited_2023
*> rules: [GR-13.18.40.4-1, GR-13.18.40.4-2, GR-13.18.40.4-10,
*>    GR-13.18.40.5-2, GR-13.18.40.5-3, SR-13.18.60.3-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB492 - CATEGORY NATIONAL-EDITED (ISO 1989:2023 8.5.2.11, 13.18.40.4 GR10).
      *>
      *> 13.18.40.4 GR10: "To define an item as national-edited, character-string-1 shall include
      *> - at least one symbol 'N', and - at least one instance of character-1 or one of the symbols
      *> from the set 'B', '0', '/'." 13.18.40.5 rule 2's Table 7 gives the category exactly ONE type
      *> of editing, "| National-edited | Simple insertion |", and rule 3 says what that does: "Simple
      *> insertion editing results in the insertion character occupying the same character position in
      *> the edited item as the associated symbol occupies in character-string-1." GR1 makes every
      *> symbol representing a character position a NATIONAL character position ("When the usage of the
      *> subject of the entry is national, each symbol representing a character position defines a
      *> national character position"), and GR2 puts the insertion characters in the national form
      *> ("When the usage of the item being edited is national, the value is the national character
      *> representation").
      *>
      *> EVERY expected byte below is computed from GR14's per-symbol text plus rule 3, position by
      *> position - never read off a run:
      *>   N  "Each symbol 'N' represents a national character position that shall contain a character
      *>      from the computer's national character set" - a DATA position, filled from the sending
      *>      operand left to right (14.9.25.4 GR6 a: "alignment and any necessary space filling shall
      *>      take place as defined in 14.6.8").
      *>   B  "Each symbol 'B' represents a character position into which the character space will be
      *>      inserted during editing."
      *>   0  "Each symbol '0' (zero) represents a character position into which the character zero
      *>      will be inserted during editing."
      *>   /  "Each symbol '/' (slant) represents a character position into which the character slant
      *>      will be inserted during editing."
      *> and each of the four "is counted in the size of the item", so NNBNN is FIVE positions and
      *> N(2)B0/N(2) is SEVEN.
      *>
      *>   NEB   NNBNN       <- N"ABCD"  => A B [space] C D              = "AB CD"
      *>   NE0   NN0NN       <- N"ABCD"  => A B [zero]  C D              = "AB0CD"
      *>   NESL  N/N         <- N"AB"    => A [slant] B                  = "A/B"
      *>   NEMIX N(2)B0/N(2) <- N"ABCD"  => A B [space][zero][slant] C D = "AB 0/CD"
      *>   NESH  NNBNN       <- N"AB"    => the sending operand is exhausted after the two leading
      *>         data positions, so 14.6.8 space-fills the remaining ones and the 'B' still inserts
      *>         its space at position 3                                = "AB   "
      *>
      *> Editing applies to EVERY valid elementary move into the item, not only to a literal source:
      *> 14.9.25.4 GR6 - "Any necessary conversion of data from one form of internal representation to
      *> another takes place during valid elementary moves, along with any editing specified for, or
      *> de-editing implied by, the receiving data item." MOVE national -> national-edited is
      *> 14.9.25.3 Table 16's National row against the "National, National-edited" column = Yes.
      *>
      *> INITIALIZE: 14.9.20.4 GR4 - "Otherwise, the implicit statement is: MOVE sending-operand TO
      *> receiving-operand" - and GR6 c)'s table gives receiving operand national-edited the sending
      *> operand "| National-edited | Figurative constant national SPACES |". Because that implicit
      *> statement is a MOVE, GR6's editing applies to it too: the DATA positions take national spaces
      *> and each INSERTION position still takes its own character. NNBNN becomes five spaces (the
      *> 'B' inserts a space where the data would also have been a space), while NN0NN becomes
      *> "  0  " - the zero survives, which is exactly what separates editing from a plain fill.
      *>
      *> VALUE: 13.18.63.3 SR5 - "If the item is of category national or national-edited, literals in
      *> the VALUE clause shall be national literals" - and SR11 - "Editing characters in a picture
      *> character-string for an alphanumeric-edited or national-edited data item do not cause editing of
      *> the initial value when the data item is initialized", with NOTE 3: "The programmer is responsible
      *> for specifying the value of a literal associated with an alphanumeric-edited or national-edited
      *> item in edited form." So VALUE N"AB CD" is stored EXACTLY as written - the 'B' does NOT re-insert
      *> and the data positions are not re-packed - and the image is "AB CD". An ALPHANUMERIC literal
      *> there is SR5's other half and is refused:
      *> conformance:cases/13.18.63-VALUE/pb492-national-edited-value-literal-class (COBOLNET0898).
      *>
      *> COMPARISON: 8.5.2.1 Table 2 puts category national-edited in class NATIONAL
      *> ("| National | National<br>National-edited<br>Numeric-edited (if usage is national) |"), so
      *> the edited image compares against a national literal with no conversion.
      *>
      *> National-edited data is a COBOL-2002 introduction; the 85 rejection is
      *> conformance:cases/13.18.40-PICTURE/pb492-national-edited-at-85 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB492NE3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEB      PIC NNBNN.
       01 NE0      PIC NN0NN.
       01 NESL     PIC N/N.
       01 NEMIX    PIC N(2)B0/N(2).
       01 NESH     PIC NNBNN.
       01 N-SRC    PIC N(4) VALUE N"ABCD".
       01 NEV      PIC NNBNN VALUE N"AB CD".
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"ABCD" TO NEB
           MOVE N"ABCD" TO NE0
           MOVE N"AB" TO NESL
           MOVE N"ABCD" TO NEMIX
           MOVE N"AB" TO NESH
           DISPLAY "NEB=[" NEB "]"
           DISPLAY "NE0=[" NE0 "]"
           DISPLAY "NESL=[" NESL "]"
           DISPLAY "NEMIX=[" NEMIX "]"
           DISPLAY "NESH=[" NESH "]"
      *> Field-to-field: a category-national sending ITEM into a national-edited receiver.
           MOVE N-SRC TO NEB
           DISPLAY "FLD=[" NEB "]"
      *> INITIALIZE - national SPACES into the data positions; the insertion positions keep their own
      *> characters, so NE0 still shows its zero.
           INITIALIZE NEB
           INITIALIZE NE0
           DISPLAY "INITB=[" NEB "]"
           DISPLAY "INIT0=[" NE0 "]"
      *> Class national (8.5.2.1 Table 2) - the edited image compares against a national literal.
           MOVE N"ABCD" TO NEB
           DISPLAY "VAL=[" NEV "]"
           IF NEB = N"AB CD" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO"
           STOP RUN.
