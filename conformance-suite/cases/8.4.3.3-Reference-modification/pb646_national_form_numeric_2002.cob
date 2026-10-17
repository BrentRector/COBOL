*>--- conformance-case
*> id: pb646_national_form_numeric_2002
*> rules: [GR-8.4.3.3.4-3, GR-13.18.40.4-1, GR-13.18.40.4-8, GR-13.18.49.4-3, GR-13.18.49.4-5, SR-13.18.60.3-12, SR-13.18.63.3-8]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB646 - the NATIONAL-FORM data of ISO 1989:2023 13.18.60.3 SR12: a numeric, a
      *> numeric-edited and a boolean PICTURE under USAGE NATIONAL.
      *>
      *> 13.18.60.3 SR12: "An elementary data item with usage national shall be described with a
      *> picture character-string that describes a boolean, national, national-edited, numeric, or
      *> numeric-edited data item." FIVE shapes; the first two of them (national, national-edited) were
      *> already live and the other three were refused BY NAME at COBOLNET0899 until this landing.
      *>
      *> WHAT USAGE NATIONAL CHANGES, AND WHAT IT DOES NOT.
      *>   13.18.40.4 GR1: "When the usage of the subject of the entry is national, each symbol
      *>     representing a character position defines a national character position." So the item's
      *>     digits, its separate sign and its insertion characters are NATIONAL character positions -
      *>     the same characters its DISPLAY twin holds, in a different character set.
      *>   13.18.60.4 GR8: "National characters shall be represented in the storage of the computer as
      *>     characters of a uniform size equal to or a multiple of the size of characters in the
      *>     computer's alphanumeric character set." The implementor pins that size; COBOL.NET pins TWO
      *>     bytes, UTF-16BE (data-model design D-N1), so an n-position item occupies 2n bytes.
      *> Nothing else moves: the VALUE is the same number, the arithmetic is the same arithmetic, and
      *> the character image is the same digit run (design D-N7).
      *>
      *> EVERY expected line below is computed from the standard, not read off a run:
      *>
      *> NNUM  PIC 9(3) USAGE NATIONAL VALUE 123 -> three '9' digit positions (GR14: "Each symbol '9'
      *>       represents a digit position that contains a numeral and is counted in the size of the
      *>       item"), so [123]; after ADD 1, [124]. FUNCTION LENGTH is 15.50.4 r2 - "an elementary
      *>       data item of usage national other than a boolean data item ... the length of argument-1
      *>       in national character positions" - so 3; FUNCTION BYTE-LENGTH is GR8's size, 2 x 3 = 6.
      *>
      *> NSGN  PIC S9(3) USAGE NATIONAL SIGN IS LEADING SEPARATE VALUE -45. 13.18.52.3 SR2 admits the
      *>       SIGN clause here by name ("The usage of an elementary item for which the SIGN clause is
      *>       specified shall be display or national") and GR6a makes the separate sign "the leading
      *>       (or, respectively, trailing) character position of the data item to which it applies;
      *>       this character position is not a digit position", with GR6b's operational signs '+' and
      *>       '-'. So the item is FOUR national character positions - [-045] - LENGTH 4 and
      *>       BYTE-LENGTH 8. (Its DISPLAY twin answers 4 for LENGTH too; the two spellings differ only
      *>       in GR8's per-character size.)
      *>
      *> GSE / SAMES  13.18.49.4 GR5: "If an alphanumeric group item, national group item, or
      *>       strongly-typed group item to which data-name-1 is subordinate contains a SIGN clause,
      *>       the effect is as though that SIGN clause had been specified for the subject of the
      *>       entry." GR3 says the same for a USAGE clause and GR1 copies the rest of the
      *>       description ("the effect ... is as though the data description identified by
      *>       data-name-1 had been coded in place of the SAME AS clause, excluding the level-number,
      *>       name, and the CONSTANT RECORD, EXTERNAL, GLOBAL, REDEFINES, and SELECT WHEN clauses" -
      *>       the VALUE clause is not in that list, so it comes too). SAMES therefore IS GSE's
      *>       description: usage national, PIC S9(3), SIGN IS LEADING SEPARATE, VALUE -45 - and by
      *>       13.18.52.4 GR6a over 13.18.40.4 GR1 that is FOUR national character positions,
      *>       [-045], LENGTH 4, BYTE-LENGTH 8 - the same three answers GSE itself gives. The two
      *>       routes by which an item acquires an ancestor's SIGN clause must agree; while they did
      *>       not, this shape measured 3 / 6 / [04N] (the over-punch image of a DISPLAY item).
      *>
      *> NED   PIC ZZ9 USAGE NATIONAL VALUE N"  7". 8.5.2.1 Table 2 puts "Numeric-edited (if usage is
      *>       national)" in class NATIONAL, so 13.18.63.3 SR7 - "If the item is of category
      *>       numeric-edited and the literal is of class alphanumeric or national, the class of the
      *>       literal shall conform to that of the data item" - requires a NATIONAL literal here, and
      *>       SR11 does not edit it ("Editing characters ... are used in editing of the initial value
      *>       when the data item is initialized and the literal is numeric"), so it stores verbatim:
      *>       [  7]. After MOVE 42, GR14's Z ("represents a ... digit position ... leading zeroes are
      *>       replaced by the space character") suppresses the one leading zero of 042: [ 42].
      *>       MOVE NED TO D3 then DE-EDITS (14.9.25.4 GR5 - "De-editing takes place only when the
      *>       sending operand is a numeric-edited data item and the receiving item is a numeric or a
      *>       numeric-edited data item"): [042].
      *>
      *> NBOOL PIC 1(4) USAGE NATIONAL VALUE B"1010". 13.18.40.4 GR8 ("To define an item as boolean,
      *>       character-string-1 shall contain only one or more occurrences of the symbol '1'") makes
      *>       it category boolean, SR12 admits the usage, and 13.18.63.3 SR10 takes the boolean
      *>       literal: [1010]. FUNCTION LENGTH is 15.50.4 r1, "the length of argument-1 in boolean
      *>       positions" - 4 - while BYTE-LENGTH is GR8's size over those 4 national positions, 8.
      *>
      *> NBZ / NBZN  13.18.8.3 SR2 admits BLANK WHEN ZERO on a usage-NATIONAL item by name ("The
      *>       subject of the entry shall be implicitly or explicitly described as usage display or
      *>       usage national"). 13.18.63.3 SR8: "When a numeric-edited data description includes the
      *>       BLANK WHEN ZERO clause and the VALUE clause uses either an alphanumeric or national
      *>       literal, the BLANK WHEN ZERO clause has no effect on initialization" - so NBZ, seeded
      *>       from N"  0", keeps [  0]; NBZN, seeded from the NUMERIC literal 0, is blanked by SR8's
      *>       NOTE 2 and 13.18.8.4 GR1: [   ]. After MOVE 0 the clause applies to both: [   ] / [   ].
      *>
      *> GL    13.18.60.4 GR1 - "If the USAGE clause is specified or implied at a group level, it
      *>       applies only to each elementary item in the group" - so GL is the SAME item its written
      *>       spelling produces: [012].
      *>
      *> NGA / NGB  A GROUP-LEVEL VALUE over a NATIONAL GROUP, which is the one group shape whose
      *>       subordinate numeric items may be national at all. 13.18.29.3 SR3: "All elementary items
      *>       subordinate to the subject of the entry shall be explicitly or implicitly described as
      *>       usage national" - and the rule contemplates numeric ones by name ("Any signed numeric
      *>       data items shall be described with the SIGN IS SEPARATE clause"). 13.18.63.3 SR14's
      *>       usage-DISPLAY requirement is written about items "subordinate to an alphanumeric group
      *>       item" and so does not reach here; SR5 requires the group VALUE literal to be a NATIONAL
      *>       literal, and 13.18.63.4 GR5 - "If a VALUE clause is specified in a data description
      *>       entry of a group item, the group area is initialized without consideration for the
      *>       individual elementary or group items contained within this group" - deposits it
      *>       POSITIONALLY. 13.18.29.4 GR2b measures that area as "PICTURE N(m), where m is the
      *>       length of the group": NGA's 2 national positions + NGB's 3 = 5, so N"AB123" fills it
      *>       exactly and the members take positions 1-2 and 3-5: [AB] and [123]. That is the same
      *>       distribution the ALPHANUMERIC twin AGA/AGB gets from "AB123" - the national form
      *>       differs only in 13.18.60.4 GR8's per-character size (design D-N7), never in which
      *>       characters land where.
      *>
      *> NNUM(2:2)  8.4.3.3.4 GR3: "If the data item referenced by identifier-1 is explicitly or
      *>       implicitly described as usage NATIONAL and its category is other than national, it is
      *>       operated upon for purposes of reference modification as if it were redefined as a data
      *>       item of class and category national of the same size", and GR6c makes the slice class
      *>       and category NATIONAL. Over [124] the slice is positions 2-3: [24]. Table 16's National
      *>       row then admits the national receiver.
      *>
      *> NUMERIC class condition: 8.8.4.4.4 - "If the usage of the data item referenced by identifier-1
      *>       is implicitly or explicitly display or national, the condition is true if the presence or
      *>       absence of an operational sign ... is in agreement with the data description ... and if
      *>       the content, except for the operational sign, consists entirely of the characters 0, 1,
      *>       2, 3, ..., 9" - true for both NNUM and NSGN.
      *>
      *> The BYTE image is pinned by conformance:2023/pb646_national_form_numeric_bytes (a REDEFINES
      *> over the same items) and the 85 rejection by
      *> conformance:negative/pb646-national-form-numeric-at-85 (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB646NF2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NNUM  PIC 9(3) USAGE NATIONAL VALUE 123.
       01 NSGN  PIC S9(3) USAGE NATIONAL SIGN IS LEADING SEPARATE VALUE -45.
       01 NED   PIC ZZ9 USAGE NATIONAL VALUE N"  7".
       01 NBOOL PIC 1(4) USAGE NATIONAL VALUE B"1010".
       01 NBZ   PIC ZZ9 USAGE NATIONAL BLANK WHEN ZERO VALUE N"  0".
       01 NBZN  PIC ZZ9 USAGE NATIONAL BLANK WHEN ZERO VALUE 0.
       01 GRP   USAGE NATIONAL.
          05 GL PIC 9(3) VALUE 12.
       01 GSGN  USAGE NATIONAL SIGN IS LEADING SEPARATE.
          05 GSE PIC S9(3) VALUE -45.
       01 SAMES SAME AS GSE.
       01 NG GROUP-USAGE NATIONAL VALUE N"AB123".
          05 NGA PIC N(2).
          05 NGB PIC 9(3).
       01 AG VALUE "AB123".
          05 AGA PIC X(2).
          05 AGB PIC 9(3).
       01 D3    PIC 9(3) VALUE 0.
       01 NW    PIC N(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "NNUM=[" NNUM "]"
           DISPLAY "NSGN=[" NSGN "]"
           DISPLAY "NED=[" NED "]"
           DISPLAY "NBOOL=[" NBOOL "]"
           DISPLAY "NBZ=[" NBZ "]"
           DISPLAY "NBZN=[" NBZN "]"
           DISPLAY "GL=[" GL "]"
      *> 13.18.49.4 GR1/GR3/GR5 - the SAME AS subject is the target's whole description, ancestor
      *> USAGE and SIGN clauses included.
           DISPLAY "GSE=[" GSE "]"
           DISPLAY "SAMES=[" SAMES "]"
           DISPLAY "SLEN=" FUNCTION LENGTH(SAMES) " "
               FUNCTION BYTE-LENGTH(SAMES)
      *> 13.18.63.4 GR5 over a national group - the positional deposit, against its alphanumeric twin.
           DISPLAY "NG=[" NGA "][" NGB "]"
           DISPLAY "AG=[" AGA "][" AGB "]"
      *> The value is the number, not the characters: arithmetic is the DISPLAY twin's arithmetic.
           ADD 1 TO NNUM
           DISPLAY "ADD=[" NNUM "]"
           MOVE 42 TO NED
           DISPLAY "EDIT=[" NED "]"
           MOVE NED TO D3
           DISPLAY "DEEDIT=[" D3 "]"
      *> 13.18.8.4 GR1 - a zero STORED into either blank-when-zero item blanks it.
           MOVE 0 TO NBZ
           MOVE 0 TO NBZN
           DISPLAY "BZ0=[" NBZ "][" NBZN "]"
      *> 15.50.4 r1 / r2 and 13.18.60.4 GR8.
           DISPLAY "LEN=" FUNCTION LENGTH(NNUM) " " FUNCTION LENGTH(NSGN)
               " " FUNCTION LENGTH(NBOOL)
           DISPLAY "BYTES=" FUNCTION BYTE-LENGTH(NNUM) " "
               FUNCTION BYTE-LENGTH(NSGN) " " FUNCTION BYTE-LENGTH(NBOOL)
      *> 8.8.4.4.4 - the NUMERIC class condition over a usage-national numeric item.
           IF NNUM IS NUMERIC THEN DISPLAY "NUM1=YES" ELSE DISPLAY "NUM1=NO"
           END-IF
           IF NSGN IS NUMERIC THEN DISPLAY "NUM2=YES" ELSE DISPLAY "NUM2=NO"
           END-IF
      *> 8.4.3.3.4 GR3/GR6c - the slice is class and category national.
           MOVE NNUM(2:2) TO NW
           DISPLAY "REFMOD=[" NW "]"
           STOP RUN.
