      *> kb/Work PB490 - a PICTURE EDITING character-1 in the IS form is one of the SIMPLE INSERTION
      *> editing symbols (13.18.40.5 rule 3: "the symbols 'B', '0', '/', ',' and, if literal=1 is
      *> specified, character-1 are used as the simple insertion editing symbols"), so it joins any
      *> zero-suppression or floating string it is embedded in or immediately right of (rules 6 and 7:
      *> "Any of the simple insertion editing symbols embedded in this string or to the immediate right
      *> of this string are part of the string"). The FOR form does NOT: rule 5 makes character-1 a
      *> FIXED insertion symbol - "When character-1 is used, and is not a simple insertion character,
      *> it represents literal-2, or literal-3 as the insertion characters" - and 13.18.40.3 SR12 is
      *> what separates the two forms, "If literal-1 is specified, character-1 is a fixed editing sign
      *> control symbol. If the FOR phrase is specified, character-1 is an extended editing sign
      *> control symbol". The two phrases here render the SAME character ':' for either sign, so only
      *> the FORM tells them apart - which is why the resolved rule carries it.
      *>
      *> NIS  ZT9 EDITING T IS ":" <- 5: the ':' is immediately right of the 'Z' string, so it is
      *>      part of it, and rule 7 a) replaces every position preceding the first character position
      *>      for which no zero suppression is specified (the '9') => "  5".
      *> NFOR UZ9 EDITING U FOR NEGATIVE IS ":" POSITIVE IS ":" <- 5: FIXED insertion, not part of
      *>      the string, and at the LEFT END where a fixed insertion symbol may stand (13.18.40.6: an
      *>      extended editing sign control symbol "has the same precedence as the 'cs' symbol in the column
      *>      and row of non-floating insertion symbols", so the mid-string spelling ZU9 is COBOLNET1935 -
      *>      kb/Work PB855); the ':' stands and the 'Z' after it suppresses => ": 5".
      *> NFL  ++T++9 EDITING T IS ":" <- 5: embedded in the FLOATING string, so rule 6 a) lands the
      *>      single '+' immediately preceding the first nonzero numeric character, every position
      *>      before it a space => "    +5".
      *> AE   XXTXX EDITING T IS ":" <- "ABCD": 13.18.40.4 GR7 admits character-1 as an
      *>      alphanumeric-edited constituent ("at least one symbol 'A' or one symbol 'X', and at least
      *>      one instance of character-1 or one of the symbols from the set 'B', '0', '/'"), Table 7
      *>      gives the category SIMPLE INSERTION, and rule 3 puts "the insertion character occupying
      *>      the same character position in the edited item as the associated symbol occupies in
      *>      character-string-1" => "AB:CD" (the mask LETTER 'T' is not an insertion character).
      *> AE2  XX/XX <- "ABCD": the fixed-symbol sibling of the same rule => "AB/CD".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB490EDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NIS      PIC ZT9 EDITING T IS ":".
       01 NFOR     PIC UZ9 EDITING U FOR NEGATIVE IS ":"
                                          POSITIVE IS ":".
       01 NFL      PIC ++T++9 EDITING T IS ":".
       01 AE       PIC XXTXX EDITING T IS ":".
       01 AE2      PIC XX/XX.
       PROCEDURE DIVISION.
           MOVE 5 TO NIS NFOR NFL
           MOVE "ABCD" TO AE
           MOVE "ABCD" TO AE2
           DISPLAY "NIS=[" NIS "] NFOR=[" NFOR "] NFL=[" NFL "]"
           DISPLAY "AE=[" AE "] AE2=[" AE2 "]"
           STOP RUN.
