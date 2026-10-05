      *> reject-at: 2002 2014 2023
      *> kb/Work PB1089 - 12.3.7.3 SR28: "If literal-7 is of class alphanumeric, the associated currency symbol may
      *> be used only to define a numeric-edited item with usage display. If literal-7 is of class national, the
      *> associated currency symbol may be used only to define a numeric-edited item with usage national." The
      *> currency set recorded symbol -> string and DISCARDED literal-7's class at the CURRENCY SIGN clause, so no
      *> PICTURE could be asked; each of these compiled and ran. The class is now half of the set's definition
      *> (CurrencyDefinition) and the one USAGE x PICTURE screen asks it, for a written USAGE and for one acquired
      *> from the group (13.18.60.4 GR1) alike: COBOLNET2883.
      *>
      *> NC1  a NATIONAL literal-7 ('#' = N"EUR") on a DISPLAY item.
      *> NC2  an ALPHANUMERIC literal-7 ('U' = "USD") on a NATIONAL item.
      *> NC3  the same alphanumeric symbol on an item whose NATIONAL usage comes from its group.
      *> NC4  a bare NATIONAL literal-7 (N"@" - one character is both string and symbol, SR22) on a DISPLAY item.
      *> NC5  the implied '$': SR25 implies CURRENCY SIGN '$' PICTURE SYMBOL '$', whose literal-7 is the ALPHANUMERIC
      *>      literal '$', so SR28's first sentence confines '$' to usage display. (The standard's text is the only
      *>      authority here: the implied clause's literal-7 is written alphanumeric and SR28 says "may be used only".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1089NC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS N"EUR" WITH PICTURE SYMBOL "#"
           CURRENCY SIGN IS "USD" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS N"@".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC #9.99.
       01 NC2 PIC U9.99 USAGE NATIONAL.
       01 NC3G USAGE NATIONAL.
          05 NC3 PIC U9.99.
       01 NC4 PIC @9.99.
       01 NC5 PIC $9.99 USAGE NATIONAL.
       PROCEDURE DIVISION.
           STOP RUN.
