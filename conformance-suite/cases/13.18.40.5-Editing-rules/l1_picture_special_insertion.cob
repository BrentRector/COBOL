      *>--- conformance-case
      *> id: l1_picture_special_insertion
      *> rules: [GR-13.18.40.5-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.5 editing rule 4 PICTURE clause (special
      *> insertion editing) —
      *> "The symbol '.' is used as the special insertion editing
      *> symbol. Special insertion editing results in the period
      *> character occupying the same character position in the edited
      *> item as the symbol '.' occupies in character-string-1."
      *>   python scripts/spec/cite.py --check 13.18.40.5 "Special
      *>   insertion editing results in the period character occupying
      *>   the same character position in the edited item as the symbol
      *>   '.' occupies in character-string-1."
      *>   -> OK  §13.18.40.5 4)  (Editing rules)
      *>
      *> ⛔ THE RULE IS ABOUT A POSITION, so a fixture that only showed
      *> "a period appears somewhere" would not exercise it. Three
      *> masks are used whose '.' sits at THREE DIFFERENT character
      *> positions, and for each the actual character at that position
      *> and at its two neighbours is displayed. The edited items are
      *> moved to alphanumeric items first (Table 16 of §14.9.25 gives
      *> Yes for numeric-edited into alphanumeric) so the window is cut
      *> with plain reference modification.
      *>
      *>   EA  PIC 9(4).99   7 positions; '.' is the 5th symbol of
      *>                     character-string-1, so the period shall be
      *>                     at position 5 and TA(4:3) shall read 3.5
      *>   EB  PIC 9.9(4)    6 positions; '.' is the 2nd symbol, so
      *>                     TB(1:3) shall read 3.5
      *>   EZ  PIC ***.**    6 positions; '.' is the 4th symbol, so
      *>                     TC(3:3) shall read *.*
      *>
      *> NAMING. The third item is EZ, not the EC the mask order would
      *> suggest: §8.9 lists EC among the reserved words and §8.3.2.4.1
      *> says "Reserved words shall not be used as system-names or
      *> user-defined words.", so EC is not available here.
      *>
      *> THE VALUES. §14.6.8.2 rule 5 sends a fixed-point numeric
      *> sending operand to §13.18.40's editing rules for a
      *> numeric-edited receiving item, and the special insertion
      *> symbol is also the decimal point about which that alignment
      *> happens:
      *>   MOVE 3.5 TO EA — four integer digit positions and two
      *>     fractional ones, so 0003 . 50: [0003.50].
      *>   MOVE 3.5 TO EB — one integer digit position and four
      *>     fractional ones, so 3 . 5000: [3.5000].
      *>   MOVE 0 TO EZ — every numeric character position is the
      *>     zero-suppression symbol '*' and the value is zero, so
      *>     editing rule 7b applies: "all character positions of the
      *>     item will contain the character asterisk, but the decimal
      *>     separator, when specified, will appear in the item" —
      *>     [***.**]. This leg is the sharpest reading of rule 4: the
      *>     period holds its position even when every digit position
      *>     around it has been replaced.
      *>
      *> DISTINGUISHABILITY. An implementation that appended the period
      *> at a fixed place (say always before the last two positions),
      *> or that dropped it under full asterisk replacement, or that
      *> aligned the value without regard to where the symbol stands,
      *> would move at least one of the three windows off 3.5 / 3.5 /
      *> *.* while the bracketed images stayed plausible.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PSINS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 EA PIC 9(4).99.
       01 EB PIC 9.9(4).
       01 EZ PIC ***.**.
       01 TA PIC X(7).
       01 TB PIC X(6).
       01 TC PIC X(6).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 3.5 TO EA.
           MOVE 3.5 TO EB.
           MOVE 0 TO EZ.
           MOVE EA TO TA.
           MOVE EB TO TB.
           MOVE EZ TO TC.
           DISPLAY "EA=[" EA "]".
           DISPLAY "EB=[" EB "]".
           DISPLAY "EZ=[" EZ "]".
           DISPLAY "A456=[" TA(4:3) "]".
           DISPLAY "B123=[" TB(1:3) "]".
           DISPLAY "C345=[" TC(3:3) "]".
           STOP RUN.
