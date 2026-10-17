*>--- conformance-case
*> id: l1_usage_display_character_size
*> rules: [GR-8.1.2-3, GR-13.18.60.4-7, DOC-A.1-209, DOC-A.1-213]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.60.4 GR7 — USAGE DISPLAY: size and representation of characters
      *> (Annex A.1 item 209).
      *>
      *> THE RULE. §13.18.60.4 GR7: "The implicit or explicit USAGE DISPLAY clause specifies
      *> that an alphanumeric coded character set shall be used to represent a data item in
      *> the storage of the computer, and that the data item is aligned on a character
      *> boundary. Alphanumeric characters shall be represented in the storage of the computer
      *> as characters of uniform size equal to or less than the size of characters in the
      *> computer's national character set. Each implementor shall specify the size and
      *> representation of characters stored for usage DISPLAY."
      *>
      *> Three testable obligations, and the implementor determination is ONE BYTE per
      *> character position:
      *> UNI  - "characters of UNIFORM size": the byte count is exactly n x s for a PIC X(n),
      *>        with the same s at every n. 1, 5, 9 for X(1), X(5), X(9) is s = 1 measured
      *>        three times; a non-uniform representation (a variable-width external encoding,
      *>        say) would not scale linearly.
      *> LEQN - "equal to or LESS THAN the size of characters in the computer's national
      *>        character set": the national determination is 2 bytes per position (GR8's twin
      *>        obligation, UTF-16 code units), so 1 <= 2 satisfies GR7. This leg is the one
      *>        that makes the DISPLAY size a CONSTRAINED choice and not a free one — it fails
      *>        for any DISPLAY size above the national size.
      *> POS  - the size of a character IS one byte, stated as the identity of the two length
      *>        functions on the same item: FUNCTION LENGTH counts alphanumeric character
      *>        POSITIONS (§15.50.4 r3) and FUNCTION BYTE-LENGTH counts BYTES (§15.14.1), and
      *>        they agree at 5 only because one position occupies one byte.
      *> ALIGN- "the data item is aligned on a character boundary": X(1) + X(3) + X(1) is
      *>        1 + 3 + 1 = 5 with no implicit FILLER anywhere — a character boundary is every
      *>        byte, so nothing is ever skipped to reach one.
      *> NUM  - the representation of a NUMERIC display item, which is USAGE DISPLAY by
      *>        implication (§13.18.60.3 SR13 b: "otherwise, a USAGE DISPLAY clause is
      *>        implied"): one character position, hence one byte, per digit position. 9(5) is
      *>        5. S9(5) with no SIGN clause is ALSO 5, because §13.18.52.4 GR5 a puts the
      *>        operational sign "associated with the leading (or, respectively, trailing)
      *>        digit position" — an existing position, not a new one. S9(5) SIGN IS TRAILING
      *>        SEPARATE is 6, because §13.18.52.4 GR6 a makes the sign "the leading (or,
      *>        respectively, trailing) character position ... this character position is not
      *>        a digit position". The three answers 5 / 5 / 6 are the whole GR7 x SIGN grid.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1UDSP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1  PIC X(1).
       01 X5  PIC X(5).
       01 X9  PIC X(9).
       01 N1  PIC N(1).
       01 D5  PIC 9(5).
       01 SD5 PIC S9(5).
       01 SS5 PIC S9(5) SIGN IS TRAILING SEPARATE.
       01 GD.
          05 GD-A PIC X(1).
          05 GD-B PIC X(3).
          05 GD-C PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNI=" FUNCTION BYTE-LENGTH(X1)
               " " FUNCTION BYTE-LENGTH(X5)
               " " FUNCTION BYTE-LENGTH(X9)
           DISPLAY "LEQN=" FUNCTION BYTE-LENGTH(X1)
               " " FUNCTION BYTE-LENGTH(N1)
           DISPLAY "POS=" FUNCTION LENGTH(X5)
               " " FUNCTION BYTE-LENGTH(X5)
           DISPLAY "ALIGN=" FUNCTION BYTE-LENGTH(GD)
           DISPLAY "NUM=" FUNCTION BYTE-LENGTH(D5)
               " " FUNCTION BYTE-LENGTH(SD5)
               " " FUNCTION BYTE-LENGTH(SS5)
           STOP RUN.
