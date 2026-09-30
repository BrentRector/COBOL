      *> kb/Work PB152 - THE ONE-RULE-ONE-PLACE WITNESS: all three arms that consume the OPTIONS INITIALIZE
      *> clause must produce the SAME specified-fill-character.
      *>
      *> "What fills storage that has no VALUE clause" was composed in three places:
      *>   1. the ALLOCATE arm      - 14.9.3.4 GR8/GR9, landed by kb/Work PB151
      *>   2. the native-field arm  - the initial state of a WORKING-STORAGE item (14.6.2.3.2 action 1)
      *>   3. the Tier-B image arm  - the same item read through a REDEFINES window
      *> Arm 1 landed with its fill decoder PRIVATE to PtrEmitter, and that private copy carried its OWN map of
      *> 11.9.10.4 GR5 - which spelled HIGH-VALUES as U+FFFF while every other HIGH-VALUE in the compiler was the
      *> program collating sequence's highest character (8.3.3.6.4 GR6), then U+00FF under the native sequence. One
      *> rule, two places, TWO DIFFERENT ANSWERS, and the arm with the private copy was the one that disagreed
      *> with the rest of the compiler. This golden is what makes that impossible to reintroduce: it asks all
      *> three arms the same question in one program and compares each against the figurative constant the rule
      *> names, so an arm that keeps its own map fails HERE rather than in a user's program.
      *>
      *> EXPECTED: WS=1 IMG=65536 ALLOC=1 - every arm fills with the alphanumeric high value.
      *>
      *> The IMAGE arm is asserted with FUNCTION ORD rather than a comparison because, when it was written,
      *> `IF R(1:3) = HIGH-VALUES` ran into kb/Work PB297 - a reference-modified operand compared against the
      *> figurative HIGH-VALUE/LOW-VALUE answered wrong when the ref-mod length differed from the base item's
      *> width.  PB297 is FIXED; ORD stays as an independent channel. 15.70.1: "The ORD function returns an
      *> integer value that is the ordinal position of argument-1 in the program collating sequence. The lowest
      *> ordinal position is 1." Under the native sequence the ordinal is the character's position + 1, so the
      *> alphanumeric high value U+FFFF - the highest of the 65,536 native characters (owner decision kb/Work R52)
      *> - is ordinal 65536.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB152ARMS.
       OPTIONS.
           INITIALIZE ALL TO HIGH-VALUES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GA PIC X(3).
          05 GC PIC 9(4) COMP.
       01 R REDEFINES G PIC X(5).
       01 P USAGE POINTER.
       01 B3 PIC X(3) BASED.
       01 W-N PIC 9.
       01 W-O PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 0 TO W-N.
           IF GA = HIGH-VALUES MOVE 1 TO W-N END-IF.
           DISPLAY "WS=" W-N.
           MOVE FUNCTION ORD(R(1:1)) TO W-O.
           DISPLAY "IMG=" W-O.
           ALLOCATE 3 CHARACTERS RETURNING P.
           SET ADDRESS OF B3 TO P.
           MOVE 0 TO W-N.
           IF B3 = HIGH-VALUES MOVE 1 TO W-N END-IF.
           DISPLAY "ALLOC=" W-N.
           STOP RUN.
