*>--- conformance-case
*> id: pb528-picture-composition
*> rules: [SR-13.18.40.3-12, SR-13.18.40.3-16, SR-13.18.40.3-17, SR-13.18.40.3-18, SR-13.18.40.3-19, SR-13.18.40.3-20, SR-13.18.40.3-21, SR-13.18.40.3-22, SR-13.18.40.3-24]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB528 - the COMPOSITION syntax rules of ISO 1989:2023 13.18.40.3. PictureAnalyzer's symbol
      *> loop tested MEMBERSHIP only, so every one of these compiled clean at all four editions and several
      *> RAN, producing an image no rule defines (PIC 99.99.99 turned 123456 into 56.00.00; PIC ZZ**9 rendered
      *> 7 as "  **7", two replacement characters in one item). Each entry below is COBOLNET1934, and each
      *> rule is a syntax rule of every edition from 1985 on, so all four reject.
      *>
      *> PB01  SR16 - "The symbol 'P' may appear only as a continuous string of 'P's in the leftmost or
      *>            rightmost digit positions in character-string-1": 9P9's P is at neither end.
      *> PB02  SR16 - two separate strings of 'P's are not "a continuous string".
      *> PB03  SR17 - "The symbol 'P' and the symbol '.' are mutually exclusive in character-string-1."
      *> PB04  SR18 - "The symbol 'S', if present, shall be the first symbol in character-string-1."
      *> PB05  SR18 - the same rule with 'S' last.
      *> PB06  SR19 - "the symbol 'V' shall either immediately precede the first symbol 'P' or immediately
      *>            follow the last symbol 'P'": in 9V9PP a '9' stands between the V and the P string.
      *> PB07  SR20 - "The symbol 'V' and the symbol '.' are mutually exclusive in character-string-1."
      *> PB08  SR21 - "The symbol 'Z' and the symbol '*' are mutually exclusive in character-string-1"; one item
      *>            has ONE replacement character (13.18.40.5 rule 7).
      *> PB09  SR22 - "Neither the symbol 'S' nor the symbol '*' shall be specified in character-string-1 when
      *>            the BLANK WHEN ZERO clause is specified for the subject of the entry" - the '*' leg.
      *> PB10  SR22 - the same rule's 'S' leg. This one was worse than silent: the BLANK WHEN ZERO promotion
      *>            required !signed, so the clause was DISCARDED and the item stayed pure numeric.
      *> PB11  SR24 - "For fixed insertion with editing sign control symbols, only one currency symbol and only
      *>            one editing sign control symbol may be used in character-string-1" - two signs. They are
      *>            not a floating string: 13.18.40.5 rule 6 needs the two occurrences ADJACENT but for the
      *>            simple insertion symbols, and three '9's stand between these.
      *> PB12  SR24 - the same rule's currency leg.
      *> PB13  SR12 b - "Each of the symbols from the set 'CR', 'DB', 'E', 'S', 'V' '.' may appear only once in
      *>            character-string-1" - two 'V'.
      *> PB14  SR12 b - three '.' (MOVE 123456 through this rendered 56.00.00).
      *> PB15  SR12 b - two 'CR' (NOTE 2: 'CR' is ONE symbol though it is two characters).
      *> PB16  SR12 a - "Character-string-1 shall contain: at least on[e] one of the symbols from the set 'A',
      *>            'N', 'X', 'Z', '1', '9', *', or at least two occurrences of one of the symbols from the
      *>            set character-1, 'x', '+', '-', and the currency symbol." BBB has neither, so it
      *>            describes no digit position and no character position at all.
      *> PB17  SR12 a - a single '+' is one occurrence, not two (PIC ++ is legal and is in the positive golden).
      *> PB18  SR23 - "The editing sign control symbols '+', '-', 'CR', and 'DB' are mutually exclusive in
      *>            character-string-1", the floating-point edited exception aside.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528CMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB01 PIC 9P9.
       01 PB02 PIC PP9PP.
       01 PB03 PIC PP99.99.
       01 PB04 PIC 9S9.
       01 PB05 PIC 999S.
       01 PB06 PIC 9V9PP.
       01 PB07 PIC 9V9.9.
       01 PB08 PIC ZZ**9.
       01 PB09 PIC ***9 BLANK WHEN ZERO.
       01 PB10 PIC S9(5) BLANK WHEN ZERO.
       01 PB11 PIC +999+.
       01 PB12 PIC $999$.
       01 PB13 PIC 9V9V9.
       01 PB14 PIC 99.99.99.
       01 PB15 PIC 999CRCR.
       01 PB16 PIC BBB.
       01 PB17 PIC +.
       01 PB18 PIC +999CR.
       PROCEDURE DIVISION.
           STOP RUN.
