      *> kb/Work PB1231 - a reference may PRECEDE the constant entry it names (ISO 13.10, 2002). No clause of 13.10
      *> or 8.4 orders them, and 13.10.3 SR4 ("The length of data-name-1 or data-name-2 shall not be dependent,
      *> directly or indirectly, upon the value of constant-name-1") and SR5 (the same for the literals of
      *> literal-1 / arithmetic-expression-1) forbid only a CIRCULAR dependence, which they could not do if every
      *> reference had to follow its entry. Expected values (13.10.4 GR1/GR3/GR4/GR6):
      *>   K  = 7   LENGTH OF W, W described after K;  A is X(K) and precedes both, so A is X(7)
      *>   KB = 4   K2 + 1 with K2 = 3 described later; B is X(KB) = X(4)
      *>   C is X(3)  PIC X(K3) before 01 K3 CONSTANT AS 3 (13.10.3 SR2, repetition)
      *>   T is 2   OCCURS KT TIMES before KT (SR2, an integer constant for integer-1)
      *>   V = 42   VALUE KV before KV (SR2, a literal position)
      *>   KL = 6   LENGTH OF L, L described in the LINKAGE SECTION after this section
      *>   D is X(6)  PIC X(KD), KD = LENGTH OF L: the value is needed before L's section is reached
      *> The negative half: negative/pb1231-constant-direct-cycle, pb1231-constant-indirect-cycle and
      *> pb1231-constant-length-cycle (the circular shapes SR4/SR5 forbid).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1231FWD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC X(K).
       01 K CONSTANT AS LENGTH OF W.
       01 W PIC X(7).
       01 B PIC X(KB).
       01 KB CONSTANT AS K2 + 1.
       01 K2 CONSTANT AS 3.
       01 C PIC X(K3).
       01 K3 CONSTANT AS 3.
       01 T.
          05 E PIC X OCCURS KT TIMES.
       01 KT CONSTANT AS 2.
       01 V PIC 9(2) VALUE KV.
       01 KV CONSTANT AS 42.
       01 KL CONSTANT AS LENGTH OF L.
       01 D PIC X(KD).
       01 KD CONSTANT AS LENGTH OF L.
       LINKAGE SECTION.
       01 L PIC X(6).
       PROCEDURE DIVISION.
           DISPLAY "K=" K " A=" FUNCTION LENGTH(A) " KB=" KB
               " B=" FUNCTION LENGTH(B) " C=" FUNCTION LENGTH(C)
               " T=" FUNCTION LENGTH(T) " V=" V " KL=" KL
               " D=" FUNCTION LENGTH(D)
           STOP RUN.
