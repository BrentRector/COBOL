      *> kb/Work PB1185 - a figurative constant in a STRING / UNSTRING operand
      *> position takes the program collating sequence of the characters its
      *> CONTEXT requires (ISO 8.3.3.6.4 GR6: "If the context of the figurative
      *> constant requires national characters, the national program collating
      *> sequence is used; otherwise, the alphanumeric program collating sequence
      *> is used"). UNSTRING 14.9.48.4 GR7 makes a figurative delimiter a national
      *> literal when identifier-1 is national; STRING 14.9.43.4 GR2 gives a
      *> figurative literal-1 / literal-2 the usage of identifier-3.
      *> The two sequences here have DIFFERENT extremes: alphanumeric STD-SEQ
      *> (literal alphabet "ZY") has LOW-VALUE "Z" (12.3.7 GR9 - position 0),
      *> national REV-NAT (N"CBA") has LOW-VALUE "C" and HIGH-VALUE the largest
      *> unspecified code unit. Before the fix every figurative in these
      *> positions took the ALPHANUMERIC sequence, so a national sender split at
      *> "Z" (or never matched) instead of at "C".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1185FDN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS STD-SEQ
               FOR NATIONAL IS REV-NAT.
       SPECIAL-NAMES.
           ALPHABET STD-SEQ IS "ZY"
           ALPHABET REV-NAT FOR NATIONAL IS N"CBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-SRC PIC N(5) VALUE N"AZCBD".
       01 N-1   PIC N(5).
       01 N-2   PIC N(5).
       01 N-H   PIC N(5).
       01 NG GROUP-USAGE NATIONAL.
          05 NG1 PIC N(5).
       01 X-SRC PIC X(5) VALUE "AZCBD".
       01 X-1   PIC X(5).
       01 X-2   PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
      *> UNSTRING, national sender: LOW-VALUE is the national "C".
           UNSTRING N-SRC DELIMITED BY LOW-VALUE INTO N-1 N-2.
           DISPLAY "UN-LOW=[" N-1 "][" N-2 "]".
      *> UNSTRING, national sender: HIGH-VALUE is the national sequence's,
      *> the same character MOVE HIGH-VALUE stores into a national item.
           MOVE N"AB" TO N-H(1:2).
           MOVE HIGH-VALUE TO N-H(3:1).
           MOVE N"CD" TO N-H(4:2).
           MOVE SPACES TO N-1 N-2.
           UNSTRING N-H DELIMITED BY HIGH-VALUE INTO N-1 N-2.
           DISPLAY "UN-HIGH=[" N-1 "][" N-2 "]".
      *> UNSTRING, alphanumeric sender: LOW-VALUE is the alphanumeric "Z".
           UNSTRING X-SRC DELIMITED BY LOW-VALUE INTO X-1 X-2.
           DISPLAY "UX-LOW=[" X-1 "][" X-2 "]".
      *> STRING into a national identifier-3: the delimiter is national.
           MOVE SPACES TO NG1.
           STRING N-SRC DELIMITED BY LOW-VALUE INTO NG.
           DISPLAY "SN-DELIM=[" NG1 "]".
      *> STRING into a national identifier-3: a figurative SENDER is a
      *> one-character national item, the national LOW-VALUE "C".
           MOVE N"XY" TO NG1.
           STRING N"XY" LOW-VALUE DELIMITED BY SIZE INTO NG.
           DISPLAY "SN-SEND=[" NG1 "]".
           STOP RUN.
