      *>--- conformance-case
      *> id: l1c12_hex_national_no_character
      *> rules: [GR-8.3.3.5.4-5, DOC-A.1-97, DOC-A.1-98]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.5.4 GR5 — hex national literal: the DOCUMENTED
      *>   no-corresponding-character result
      *> "The implementor defines the result of specifying a
      *>   hex-character-sequence-1 for which no
      *> corresponding character in that coded character set exists."
      *>   cite.py --check 8.3.3.5.4 "The implementor defines the result
      *>     of specifying a
      *>     hex-character-sequence-1 for which no corresponding
      *>       character in that coded character set
      *>     exists."  -> OK  §8.3.3.5.4 5)  (General rules)
      *> SR5 (four digits per national character is the implementor's
      *>   choice, CONFORMANCE DOC-A.1-122):
      *>   cite.py --check 8.3.3.5.3 "Each hex-character-sequence-1
      *>     shall consist of the number of
      *>     hexadecimal digits that the implementor has specified as
      *>       the number of hexadecimal digits that
      *>     map to a national character."  -> OK  §8.3.3.5.3 5)
      *>       (Syntax rules)
      *> ORD (§15.70.4 2): "the ordinal position of argument-1 in the
      *>   current national program collating
      *> sequence"  -> cite.py OK  §15.70.4 2)
      *> THE DOCUMENTED CHOICE (docs/CONFORMANCE.md row DOC-A.1-97): "A
      *>   group that is not a character on
      *> its own, such as a surrogate half D800-DFFF or a noncharacter
      *>   such as FFFE or FFFF, is stored
      *> unchanged, as one character position, with no diagnostic and no
      *>   exception condition. ...
      *> FUNCTION LENGTH(NX"D800") is 1, FUNCTION ORD gives the code
      *>   plus 1 (55297 for D800, 65536 for
      *> FFFF), and comparison uses the code unit's value. Two groups
      *>   that form a valid surrogate pair
      *> stay two character positions. On DISPLAY to a UTF-8 device, an
      *>   unpaired surrogate is written as
      *> U+FFFD."
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   LENGTH(NX"D800") is one position; the valid pair NX"D800DC00"
      *>     is two  => LEN:1 2
      *>   ORD of D800, FFFF, 0041 held in WS-N: code + 1
      *>     => ORD:55297 65536 00066
      *>   ORD of the pair's halves in WS-P: D800 -> 55297, DC00 ->
      *>     56321        => ORD:55297 56321
      *>   WS-N(2:1) (the stored FFFF) vs NX"D800" compares code
      *>     units, FFFF > D800  => GT
      *>   (the subject is a data item: §8.8.4.2.1 "A relation
      *>     condition shall contain at least one reference to an
      *>     operand that is not a literal" -> cite.py OK
      *>     §8.8.4.2.1 13))
      *>   DISPLAY of WS-L, the unpaired low surrogate DC00, writes
      *>     U+FFFD       => [�] (the replacement
      *>   character, UTF-8 EF BF BD, between the brackets)
      *>   with every EC checked, none of these literals raises a
      *>     condition     => NO-EC
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC N(3) VALUE NX"D800FFFF0041".
       01  WS-P    PIC N(2).
       01  WS-L    PIC N(1) VALUE NX"DC00".
       01  O1      PIC 9(5).
       01  O2      PIC 9(5).
       01  O3      PIC 9(5).
       01  L1      PIC 9.
       01  L2      PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE NX"D800DC00" TO WS-P.
           MOVE FUNCTION LENGTH(NX"D800") TO L1.
           MOVE FUNCTION LENGTH(NX"D800DC00") TO L2.
           DISPLAY "LEN:" L1 " " L2.
           MOVE FUNCTION ORD(WS-N(1:1)) TO O1.
           MOVE FUNCTION ORD(WS-N(2:1)) TO O2.
           MOVE FUNCTION ORD(WS-N(3:1)) TO O3.
           DISPLAY "ORD:" O1 " " O2 " " O3.
           MOVE FUNCTION ORD(WS-P(1:1)) TO O1.
           MOVE FUNCTION ORD(WS-P(2:1)) TO O2.
           DISPLAY "ORD:" O1 " " O2.
           IF WS-N(2:1) > NX"D800"
               DISPLAY "GT"
           ELSE
               DISPLAY "NOT-GT"
           END-IF.
           DISPLAY "[" WS-L "]".
           IF FUNCTION EXCEPTION-STATUS = SPACES
               DISPLAY "NO-EC"
           ELSE
               DISPLAY "EC:" FUNCTION EXCEPTION-STATUS
           END-IF.
           STOP RUN.
