      *> reject-at: 2002 2014 2023
      *> kb/Work PB553 — the NEGATIVE half of the signed Format-2 VALUE subscript. The grammar rule
      *> `signedIntegerLiteral : (PLUS | MINUS)? INTEGERLIT` is a SUPERSET parse: the sign is its own token
      *> in these slots, so `( + 1 )` — a sign separated from its digits by a space — also parses. The
      *> standard does not admit it: ISO §8.3.3.3.2 2) says "A literal shall not contain more than one sign
      *> character. If a sign is used, it shall appear as the leftmost character of the literal." A literal
      *> is ONE character-string, so a space between the sign and the digits makes two things out of one.
      *> SignedIntegerLiteral.Screen narrows it BY NAME (COBOLNET2155) on the token stream's own indices —
      *> ANTLR's GetText() strips the space, so `+ 1` and `+1` are indistinguishable to any caller that goes
      *> to the text. Rejected at every edition that HAS the Format 2 (table) VALUE clause, i.e. 2002 on;
      *> below 2002 the clause itself does not exist and the entry draws the introduction gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB553NAD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-G.
           05  S-T PIC X(2) OCCURS 3 VALUE "AB" FROM ( + 1) TO (+3).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY S-T(1)
           STOP RUN.
