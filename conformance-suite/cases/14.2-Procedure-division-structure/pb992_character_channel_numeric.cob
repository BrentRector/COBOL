      *>--- conformance-case
      *> id: pb992_character_channel_numeric
      *> rules: [GR-14.2.3-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB992 - a numeric USAGE DISPLAY item that a CHARACTER CHANNEL writes holds the characters it is
      *> given, whatever they are. Three channels, each derived from the standard before measuring:
      *>
      *> (1) ISO 14.9.25.4 GR4 (cite.py --check 14.9.25.4 "Any move that is not an elementary move, and does
      *>     not reference a variable-length group, is treated exactly as if it were an alphanumeric to
      *>     alphanumeric elementary move, except that there is no conversion of data from one form of internal
      *>     representation to another" -> OK 14.9.25.4 4)). MOVE G TO H stores G's characters into H unconverted.
      *> (2) ISO 14.2.3 GR8 (cite.py --check 14.2.3 "occupies the same storage area as the argument" -> OK
      *>     14.2.3 8)) with 14.8.2.3.2 rule 1 (cite.py --check 14.8.2.3.2 "the formal parameter shall be of the
      *>     same length as the corresponding argument" -> OK 14.8.2.3.2 1)): a Format-1 CALL pairs a numeric
      *>     argument with a PIC X(3) formal of the same length, and the formal's store IS the argument's store.
      *> (3) The same GR8 read the other way: a PIC X(3) argument's characters ARE the numeric formal's content.
      *>
      *> DERIVED VALUES (none depends on how the compiler keeps the item):
      *>   H=[   ]      three spaces - GR4 moves the group's characters, no conversion.
      *>   S=[12AB]     the four characters of G2, no conversion - a SIGNED item is no exception.
      *>   H NOT NUMERIC  spaces are not digits (the NUMERIC class test).
      *>   A=[   ]      the callee moved SPACES into its PIC X(3) formal, which occupies A's storage.
      *>   N=[ABC]      the callee's PIC 9(3) formal occupies the storage of the PIC X(3) argument C.
      *>   K=0015 / K+1=0016   an ordinary numeric crossing still computes: 10 + 5, then ADD 1.
      *> Before PB992 H, A and N displayed 000 and S displayed 012B - a native carrier held a VALUE decoded
      *> from the characters, never the characters themselves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992CH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 GX PIC X(3) VALUE SPACES.
       01 G2.
          05 G2X PIC X(4) VALUE "12AB".
       01 H PIC 9(3) VALUE 123.
       01 S PIC S9(3)V9 VALUE -12.5.
       01 A PIC 9(3) VALUE 456.
       01 C PIC X(3) VALUE "ABC".
       01 K PIC 9(4) VALUE 10.
       PROCEDURE DIVISION.
           MOVE G TO H
           DISPLAY "H=[" H "]"
           MOVE G2 TO S
           DISPLAY "S=[" S "]"
           IF H IS NUMERIC
               DISPLAY "H NUMERIC"
           ELSE
               DISPLAY "H NOT NUMERIC"
           END-IF
           CALL "PB992SP" USING A
           DISPLAY "A=[" A "]"
           CALL "PB992RD" USING C
           CALL "PB992AD" USING K
           DISPLAY "K=" K
           ADD 1 TO K
           DISPLAY "K+1=" K
           STOP RUN.
       END PROGRAM PB992CH.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992SP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 F PIC X(3).
       PROCEDURE DIVISION USING F.
           MOVE SPACES TO F
           EXIT PROGRAM.
       END PROGRAM PB992SP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992RD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 N PIC 9(3).
       PROCEDURE DIVISION USING N.
           DISPLAY "N=[" N "]"
           EXIT PROGRAM.
       END PROGRAM PB992RD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB992AD.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M PIC 9(4).
       PROCEDURE DIVISION USING M.
           ADD 5 TO M
           EXIT PROGRAM.
       END PROGRAM PB992AD.
