      *>--- conformance-case
      *> id: pb3153_inherited_special_names_words
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3153 - the SPECIAL-NAMES words a containing program
      *>   declares, as two contained programs see them (the legal half of
      *>   negative/pb3153-contained-program-paragraph-spells-an-
      *>   inherited-class-it-uses).
      *> PB3153P1 USES both inherited names: 12.3.4 GR1 applies the
      *>   containing program's configuration section to it, and 8.4.6.1
      *>   lets it reference a class-name and a symbolic-character the
      *>   containing program declares. PB3153B is the coded character
      *>   at ordinal position 66 of the native alphanumeric set
      *>   (12.3.7.4 GR11 b; ordinal 1 is X"00"), "A"; "7" is a member of
      *>   class PB3153D ("0" THRU "9", 12.3.7.4 GR12), so DIGIT.
      *> PB3153P2 declares PB3153B as a data-name and PB3153D as a
      *>   paragraph-name and never uses the class: legal, 8.4.6.1 "each
      *>   source element may use identical user-defined words", and its
      *>   reference to PB3153B "is to the item which that source element
      *>   describes" (8.4.6.1), so Q, not "A"; then PARA.
      *> cite.py --check 12.3.4 "apply to each directly or indirectly
      *>   contained source unit" -> OK 12.3.4 1); 8.4.6.1 "may be
      *>   referenced only by statements and entries either in that
      *>   source element that contains a configuration section or in any
      *>   source element contained within that source element" -> OK
      *>   8.4.6.1; 8.4.6.1 "a source element's reference to such a name,
      *>   even when it is a different type of user-defined word, is to
      *>   the item which that source element describes" -> OK 8.4.6.1;
      *>   12.3.7.4 "the coded character at ordinal position integer-1 in
      *>   the native alphanumeric character set" -> OK 12.3.7.4 11)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS PB3153B IS 66
           CLASS PB3153D IS "0" THRU "9".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB3153P1".
           CALL "PB3153P2".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE PB3153B TO X.
           DISPLAY X.
           MOVE "7" TO X.
           IF X IS PB3153D
               DISPLAY "DIGIT"
           ELSE
               DISPLAY "NOT DIGIT"
           END-IF.
           EXIT PROGRAM.
       END PROGRAM PB3153P1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3153P2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB3153B PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY PB3153B.
           PERFORM PB3153D.
           EXIT PROGRAM.
       PB3153D.
           DISPLAY "PARA".
       END PROGRAM PB3153P2.
       END PROGRAM PB3153P.
