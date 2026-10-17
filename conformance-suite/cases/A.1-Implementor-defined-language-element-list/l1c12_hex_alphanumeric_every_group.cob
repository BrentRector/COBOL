      *>--- conformance-case
      *> id: l1c12_hex_alphanumeric_every_group
      *> rules: [DOC-A.1-95, DOC-A.1-96]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.2.4 GR5 — hex alphanumeric literal: the DOCUMENTED
      *>   no-corresponding-character result
      *> "The implementor defines the result of specifying a
      *>   hex-character-sequence-1 for which no
      *> corresponding character in that coded character set exists."
      *>   cite.py --check 8.3.3.2.4 "The implementor defines the result
      *>     of specifying a
      *>     hex-character-sequence-1 for which no corresponding
      *>       character in that coded character set
      *>     exists."  -> OK  §8.3.3.2.4 5)  (General rules)
      *> GR4: the value is "a string of alphanumeric characters, each of
      *>   which has the bit configuration
      *> specified by one occurrence of hex-character-sequence-1"
      *>   cite.py -> OK  §8.3.3.2.4 4)  (General rules)
      *> SR6 (two digits per character is the implementor's choice,
      *>   docs/CONFORMANCE.md DOC-A.1-95):
      *>   cite.py --check 8.3.3.2.3 "Each hex-character-sequence-1
      *>     shall consist of the number of
      *>     hexadecimal digits that the implementor has specified as
      *>       the number of hexadecimal digits that
      *>     map to an alphanumeric character."  -> OK  §8.3.3.2.3 6)
      *>       (Syntax rules)
      *> ORD (§15.70.4 1): "the ordinal position of argument-1 in the
      *>   current alphanumeric program
      *> collating sequence"  -> cite.py OK  §15.70.4 1)
      *> THE DOCUMENTED CHOICE (docs/CONFORMANCE.md row DOC-A.1-95):
      *>   "The case does not arise: every
      *> hex-character-sequence has a corresponding character." Group hh
      *>   is the character with that DISPLAY byte (U+00hh, except
      *>   FF, which is U+FFFF by the storage-byte law), stored
      *> "including the control codes 00-1F, 7F and 80-9F, with no
      *>   diagnostic and no exception
      *> condition. Example: X"80FF0041" is four characters whose
      *>   FUNCTION ORD values are 129, 65536, 1
      *> and 66."  (Native collating sequence: ordinal = code + 1.)
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   X"80FF0041" is four characters, one per group
      *>     => LEN:4
      *>   its characters' ORD values: 80->129, FF->65536, 00->1, 41->66
      *>     => ORD:00129 65536 00001 00066
      *>   the edge groups 1F, 7F, 9F in WS-H: 31+1, 127+1, 159+1
      *>     => ORD:00032 00128 00160
      *>   X"41" is the character "A" (same bit configuration, GR4)
      *>     => EQ-A
      *>   with every EC checked, no exception condition is raised by
      *>     any
      *>   of these literals (none is "missing" a character)
      *>     => NO-EC
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C12L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G    PIC X(4).
       01  WS-H    PIC X(3) VALUE X"1F7F9F".
       01  O1      PIC 9(5).
       01  O2      PIC 9(5).
       01  O3      PIC 9(5).
       01  O4      PIC 9(5).
       01  WS-LEN  PIC 9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE X"80FF0041" TO WS-G.
           MOVE FUNCTION LENGTH(X"80FF0041") TO WS-LEN.
           DISPLAY "LEN:" WS-LEN.
           MOVE FUNCTION ORD(WS-G(1:1)) TO O1.
           MOVE FUNCTION ORD(WS-G(2:1)) TO O2.
           MOVE FUNCTION ORD(WS-G(3:1)) TO O3.
           MOVE FUNCTION ORD(WS-G(4:1)) TO O4.
           DISPLAY "ORD:" O1 " " O2 " " O3 " " O4.
           MOVE FUNCTION ORD(WS-H(1:1)) TO O1.
           MOVE FUNCTION ORD(WS-H(2:1)) TO O2.
           MOVE FUNCTION ORD(WS-H(3:1)) TO O3.
           DISPLAY "ORD:" O1 " " O2 " " O3.
           IF X"41" = "A"
               DISPLAY "EQ-A"
           ELSE
               DISPLAY "NE-A"
           END-IF.
           IF FUNCTION EXCEPTION-STATUS = SPACES
               DISPLAY "NO-EC"
           ELSE
               DISPLAY "EC:" FUNCTION EXCEPTION-STATUS
           END-IF.
           STOP RUN.
