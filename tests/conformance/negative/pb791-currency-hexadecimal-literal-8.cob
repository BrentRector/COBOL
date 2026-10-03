      *> reject-at: 2002 2014 2023
      *> kb/Work PB791 - ISO 12.3.7.3 SR26 (cite.py --check 12.3.7.3 "It shall be neither a figurative constant nor a
      *> hexadecimal literal"): literal-8 shall not be a hexadecimal literal, WHATEVER IT DECODES TO. X"24" decodes
      *> to '$', which is outside SR27's forbidden set, so the clause used to bind with no diagnostic at all (only a
      *> hexadecimal literal-8 spelling a forbidden character, such as X"41", was refused - by SR27, for a reason SR26
      *> makes irrelevant).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB791D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN "USD" PICTURE SYMBOL X"24".
       DATA DIVISION.
       PROCEDURE DIVISION.
           STOP RUN.
