*>--- conformance-case
*> id: pb490_simple_insertion_strings_2023
*> rules: [GR-13.18.40.5-6, GR-13.18.40.5-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB490 - the SIMPLE INSERTION editing symbols are a SET, and every consumer of that
      *> set reads ONE definition. 13.18.40.5 rule 3: "the symbols 'B', '0', '/', ',' and, if literal=1
      *> is specified, character-1 are used as the simple insertion editing symbols". Rules 6 and 7
      *> spend that set again in identical words - "Any of the simple insertion editing symbols
      *> embedded in this string or to the immediate right of this string are part of the string" - so
      *> a '0' or a '/' embedded in a zero-suppression or floating string takes the replacement
      *> character and is a landing position for the floating character. Two of the four symbols had
      *> been dropped at two of the consumers, and the membership test (EMBEDDED or to the immediate
      *> RIGHT, never LEFT) was missing altogether.
      *>
      *> Every expected byte is rule 7 a) - "the corresponding replacement character is placed into any
      *> character position immediately preceding whichever of the following is encountered first: the
      *> first nonzero numeric character in the item; the first character position for which no zero
      *> suppression with replacement is specified; the decimal point position" - or rule 6 a), which
      *> puts "a single occurrence of the replacement character(s) ... into the character position(s)
      *> immediately preceding" those same three, "Any character positions preceding this (these)
      *> insertion character(s) will contain the space character".
      *>
      *> SL12  ZZ/ZZ  <- 12: the '/' is embedded in the 'Z' string, so it is part of the string and
      *>       every position left of the '1' takes the replacement character space => "   12".
      *> ZE12  ZZ0ZZ  <- 12: identically for '0' => "   12".
      *> BE12  ZZBZZ  <- 12, CO12 ZZ,ZZZ <- 12: the two that always worked, pinned here so the four
      *>       cannot part company again => "   12", "    12".
      *> ST12  **/**  <- 12: rule 7's other replacement character, the asterisk => "***12".
      *> SL00  ZZ/ZZ  <- 0, ST00 **/** <- 0: rule 7 b) - all the numeric character positions are
      *>       suppression symbols and the value is zero, so "all character positions of the item will
      *>       contain the character space" / "the character asterisk" => "     ", "*****".
      *> LD0   0ZZ9 <- 1, LDS /ZZ9 <- 1, LDC ,ZZ9 <- 1: EMBEDDED or to the immediate RIGHT, never to
      *>       the LEFT. A simple insertion symbol preceding the string is not part of it and keeps its
      *>       insertion character - 13.18.40.4 GR15 says the same from the VALIDATE side, "the
      *>       character zero if the symbol '0' is neither part of floating insertion editing nor of
      *>       zero suppression with replacement editing" - and 13.18.40.6 Table 10 admits the
      *>       placement ('B 0 /' and ',' both carry an 'x' against the left-of-point 'Z *' row).
      *>       => "0  1", "/  1", ",  1".
      *> PS5   ++/++9 <- 5 and -5: the '/' is part of the floating string, so the single sign
      *>       occurrence lands immediately preceding the first nonzero numeric character, every
      *>       position before it a space => "    +5", "    -5".
      *> PS123 ++/++9 <- 123: the same walk stops one position earlier, so the sign lands ON the
      *>       embedded insertion position => "  +123"; CS123 $$/$$9 <- 123 => "  $123".
      *> PZ5   ++0++9 <- 5 => "    +5"; CS5 $$/$$9 <- 5 => "    $5".
      *> MS5   --/--9 <- 5: Table 9 - a floating '-' renders the space character over a positive value,
      *>       so the landing position holds a space => "     5"; <- -5 => "    -5".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB490S4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SL       PIC ZZ/ZZ.
       01 ZE       PIC ZZ0ZZ.
       01 BE       PIC ZZBZZ.
       01 CO       PIC ZZ,ZZZ.
       01 ST       PIC **/**.
       01 LD0      PIC 0ZZ9.
       01 LDS      PIC /ZZ9.
       01 LDC      PIC ,ZZ9.
       01 PS       PIC ++/++9.
       01 PZ       PIC ++0++9.
       01 CS       PIC $$/$$9.
       01 MS       PIC --/--9.
       PROCEDURE DIVISION.
           MOVE 12 TO SL ZE BE CO ST
           DISPLAY "SL12=[" SL "] ZE12=[" ZE "] BE12=[" BE "]"
           DISPLAY "CO12=[" CO "] ST12=[" ST "]"
           MOVE 0 TO SL ST
           DISPLAY "SL00=[" SL "] ST00=[" ST "]"
           MOVE 1 TO LD0 LDS LDC
           DISPLAY "LD0=[" LD0 "] LDS=[" LDS "] LDC=[" LDC "]"
           MOVE 5 TO PS PZ CS MS
           DISPLAY "PS5=[" PS "] PZ5=[" PZ "] CS5=[" CS "] MS5=[" MS "]"
           MOVE -5 TO PS MS
           DISPLAY "PSN5=[" PS "] MSN5=[" MS "]"
           MOVE 123 TO PS CS
           DISPLAY "PS123=[" PS "] CS123=[" CS "]"
           STOP RUN.
