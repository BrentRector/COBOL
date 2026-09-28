      *> reject-at: 2002 2014 2023
      *> kb/Work PB1371 — a constant conditional expression's complex condition is formed per ISO §8.8.4.9
      *> (§7.3.8.2 SR1 d)), and §8.8.4.11.3 Table 5 admits after NOT only a simple-condition or '(': "the pair
      *> 'NOT (' is permissible while the pair 'NOT NOT' is not permissible". The cce tier's NOT recursed
      *> (cceNot : NOT cceNot), so `>>IF NOT NOT 1 = 1` compiled and selected its text; it is now spelled like
      *> the runtime tier (NOT? leaf) and the fragment is malformed (COBOLNET1619) at every edition with >>IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68NNN.
       PROCEDURE DIVISION.
       MAIN.
       >>IF NOT NOT 1 = 1
           DISPLAY "TRUE".
       >>END-IF
           STOP RUN.
