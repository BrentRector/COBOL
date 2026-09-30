      *> kb/Work PB152 - THE 14.6.2.3.2 ACTION-1 BACKGROUND over the DATA DIVISION, and its ORDER against
      *> action 2. Every expected value below is derived from the standard, never from an oracle.
      *>
      *> 14.6.2.3.2 numbers the initial-state actions and the first two settle both scope and sequence:
      *>   1) "If the INITIALIZE clause is specified in the OPTIONS paragraph, the storage allocated for the
      *>      implied or associated sections is set to the specified-fill-character."
      *>   2) "The internal data described in the working-storage section and local-storage section is
      *>      initialized as described in 13.18.63, VALUE clause."
      *> So the fill is a BACKGROUND laid down FIRST and then OVERWRITTEN by VALUE clauses - NOT a per-item
      *> substitute for a missing VALUE. B is the pin on that ordering: it fails if anyone implements the fill
      *> as "use the fill when there is no VALUE" while also failing to lay it under one that has a VALUE.
      *>
      *> THE FILL LITERAL IS X"5A", NOT "Z": 11.9.10.3 SR1 requires a "one-byte hexadecimal-alphanumeric
      *> literal", and 8.3.3.2.2 gives the alphanumeric literal exactly two formats - format 1 ("..." / '...')
      *> and format 2 (X"..."). "Hexadecimal-alphanumeric" names FORMAT 2. 0x5A is 'Z'.
      *>
      *> EXPECTED VALUES, per 11.9.10.4 GR5 c ("If literal-1 is specified, that literal is the
      *> specified-fill-character") over each item's own character positions:
      *>   A   PIC X(4)   - 4 alphanumeric positions            -> ZZZZ
      *>   NE  PIC ZZZ9   - numeric-edited, 4 positions          -> ZZZZ
      *>   NAT PIC N(3)   - national, 3 positions                -> ZZZ
      *>   BL  PIC 1(4)   - DISPLAY boolean, one character per position (13.18.60.3 SR13b implies DISPLAY;
      *>                    the D-B1 representation) -> ZZZZ
      *>   NUM PIC 9(4)   - action 1 names "the storage allocated", with NO numeric exception (owner decision
      *>                    kb/Work R53, which overturned the former "a native numeric carrier takes its zero"
      *>                    determination; 13.18.63.4 GR4 c)'s "undefined" is the NO-clause baseline of
      *>                    11.9.10.4 GR6). A USAGE DISPLAY item's storage is its 4 character positions -> ZZZZ
      *>   NB  PIC 9(4) COMP, NP PIC S9(3) COMP-3, NF COMP-1 - the BYTES of their storage take the fill byte
      *>                    X'5A'. Each sits alone in a group (NBG/NPG/NFG), whose first and last character
      *>                    positions are read with FUNCTION ORD (15.70.1: "The lowest ordinal position is
      *>                    1", so the byte X'5A' - the character U+005A - is 91) -> 91 91 for each
      *>   NG  a group of a DISPLAY and a COMP numeric leaf -> all four bytes X'5A' -> 91 91
      *>   NV  PIC 9(4) VALUE 7 - action 2 overwrites action 1 -> 0007
      *>   B   PIC X(4) VALUE "vv" - action 2 overwrites action 1; the VALUE stores left-justified with space
      *>                    fill to the item width -> "vv  "
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152BG.
       OPTIONS.
           INITIALIZE ALL TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC X(4).
       01 NE  PIC ZZZ9.
       01 NAT PIC N(3).
       01 BL  PIC 1(4).
       01 NUM PIC 9(4).
       01 NBG.
          05 NB PIC 9(4) COMP.
       01 NPG.
          05 NP PIC S9(3) COMP-3.
       01 NFG.
          05 NF COMP-1.
       01 NG.
          05 NGA PIC 9(2).
          05 NGB PIC 9(4) COMP.
       01 NV  PIC 9(4) VALUE 7.
       01 B   PIC X(4) VALUE "vv".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A=[" A "]".
           DISPLAY "NE=[" NE "]".
           DISPLAY "NAT=[" NAT "]".
           DISPLAY "BL=[" BL "]".
           DISPLAY "NUM=[" NUM "]".
           DISPLAY "NB=" FUNCTION ORD(NBG(1:1)) " "
               FUNCTION ORD(NBG(2:1)).
           DISPLAY "NP=" FUNCTION ORD(NPG(1:1)) " "
               FUNCTION ORD(NPG(2:1)).
           DISPLAY "NF=" FUNCTION ORD(NFG(1:1)) " "
               FUNCTION ORD(NFG(4:1)).
           DISPLAY "NG=" FUNCTION ORD(NG(1:1)) " "
               FUNCTION ORD(NG(4:1)).
           DISPLAY "NV=[" NV "]".
           DISPLAY "B=[" B "]".
           STOP RUN.
