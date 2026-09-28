      *> kb/Work PB1357 - the REPLACE statement's states (ISO 7.2.4.4
      *> GR4-GR7): an active statement, a last-in first-out queue of
      *> inactive ones, and cancellation. RULES (cite.py --check):
      *>   7.2.4.4 4) b) "held in a last-in first-out queue"   -> OK
      *>   7.2.4.4 6) a) "The ALSO phrase, if specified, has no
      *>     effect" (none active)                            -> OK
      *>   7.2.4.4 6) b) "A format 2 REPLACE statement has no
      *>     effect" (none active)                            -> OK
      *>   7.2.4.4 7) a) ALSO pushes the active statement; the new
      *>     one has "all the operands of the current statement
      *>     followed by the operands of the most recent statement
      *>     pushed"                                          -> OK
      *>   7.2.4.4 7) b) without ALSO: cancels the active one and
      *>     the whole queue                                  -> OK
      *>   7.2.4.4 7) c) LAST OFF pops; 7) d) OFF cancels all -> OK
      *> EXPECTED OUTPUT, DERIVED (why each leg can fail):
      *>   [X1-X2-] LAST OFF with none active: no effect.
      *>   [AAX2-]  ALSO with none active: active {XX1->AA}.
      *>   [AABB]   ALSO pushes: {XX2->BB, XX1->AA}.
      *>   [CCBB]   ALSO again: {XX1->CC, XX2->BB, XX1->AA}; the
      *>            current operand is compared FIRST (a queue in the
      *>            wrong order prints AABB).
      *>   [AABB]   LAST OFF pops {XX2->BB, XX1->AA}.
      *>   [AAX2-]  LAST OFF pops {XX1->AA}.
      *>   [X1-CC]  ALSO then a plain REPLACE: the queue is canceled.
      *>   [X1-X2-] LAST OFF with an empty queue: none active.
      *>   [X1-X2-] OFF cancels the active one AND the queue, so the
      *>            LAST OFF after it has no effect.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1357RAL02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AA PIC X(2) VALUE "AA".
       01 BB PIC X(2) VALUE "BB".
       01 CC PIC X(2) VALUE "CC".
       01 XX1 PIC X(3) VALUE "X1-".
       01 XX2 PIC X(3) VALUE "X2-".
       PROCEDURE DIVISION.
           REPLACE LAST OFF.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE ALSO ==XX1== BY ==AA==.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE ALSO ==XX2== BY ==BB==.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE ALSO ==XX1== BY ==CC==.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE LAST OFF.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE LAST OFF.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE ALSO ==XX2== BY ==BB==.
           REPLACE ==XX2== BY ==CC==.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE LAST OFF.
           DISPLAY "[" XX1 XX2 "]".
           REPLACE ==XX1== BY ==AA==.
           REPLACE ALSO ==XX2== BY ==BB==.
           REPLACE OFF.
           REPLACE LAST OFF.
           DISPLAY "[" XX1 XX2 "]".
           STOP RUN.
