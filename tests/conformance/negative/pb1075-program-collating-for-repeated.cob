      *> reject-at: 2002 2014 2023
      *> ISO 5.2.6.4: within braces that enclose choice indicators "one or more of the alternatives
      *> contained within the choice indicators shall be specified, but any single alternative shall be
      *> specified only once". The FOR ALPHANUMERIC / FOR NATIONAL pair of every COLLATING SEQUENCE
      *> brace is such a group, read by ONE reader (CollatingAlphabetPair, kb/Work PB1075): a repeated
      *> alternative is refused COBOLNET2104 instead of the last one silently winning.
      *> NEGATIVE (sibling arm): OBJECT-COMPUTER PROGRAM COLLATING SEQUENCE (12.3.6.2) repeats FOR
      *> NATIONAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1075N2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR NATIONAL IS NAT1
                                      FOR NATIONAL IS NAT1.
       SPECIAL-NAMES.
           ALPHABET NAT1 FOR NATIONAL IS NATIVE.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
