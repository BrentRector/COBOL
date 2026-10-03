      *> reject-at: 2002 2014 2023
      *> kb/Work PB1537 - the predefined NULL is no literal at all (ISO 8.4.3.1.2 Format 8 predefined-address;
      *> 8.4.3.10.3 SR1 lists the only places it may be written, none of them a SPECIAL-NAMES literal; cite.py --check
      *> 8.4.3.10.3 "it may be used only as a sending operand in an INITIALIZE or a SET statement"). LOCALE literal-4 is
      *> refused by the ONE SR1 refusal (COBOLNET2576), not by a figurative-constant or class rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB1537B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE LA IS NULL.
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
