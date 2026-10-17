*>--- conformance-case
*> id: l1_highest_algebraic_note_table
*> rules: [RV-15.43.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.4 r2 — HIGHEST-ALGEBRAIC returns "the positive algebraic value of greatest finite
      *> magnitude that may be represented in argument-1", exercised on the SEVEN argument shapes the rule's
      *> own NOTE table names, in the table's order, with the table's values.
      *>
      *> §15.43.4 r2's NOTE table, transcribed (the expected values below are read from it, not observed):
      *>     | S999                 | +999                                    |
      *>     | S9(4) BINARY         | +9999                                   |
      *>     | 99V9(3)              | +99.999                                 |
      *>     | $**,**9.99BCR        | +99999.99                               |
      *>     | $**,**9.99           | +99999.99                               |
      *>     | BINARY-CHAR SIGNED   | +127 (assuming an 8-bit representation)  |
      *>     | BINARY-CHAR UNSIGNED | +255 (assuming an 8-bit representation)  |
      *>
      *> ⛔ ROWS 2, 6 AND 7 ARE THE POINT OF THIS FILE, AND THEY ARE THE THREE THE CORPUS DID NOT HOLD.
      *> conformance:2002/highest_lowest_algebraic already pins the DISPLAY-usage shapes (S99, S9(4) display,
      *> 99V9(3), $**,**9.99). What no fixture pinned is the DISCRIMINATION the table draws between two kinds
      *> of "may be represented in argument-1":
      *>   · S9(4) BINARY has a PICTURE, and the table's answer is +9999 — the PICTURE's all-nines, NOT the
      *>     2-byte container's 32767. A fold that reached for the container would print +32767.000 here.
      *>   · BINARY-CHAR SIGNED / UNSIGNED have NO picture character-string at all, so the only representable
      *>     range is the CONTAINER's — and the container is NOT fixed by the standard. §13.18.60.4 GR21:
      *>     "The representation and length of a data item described with USAGE BINARY-CHAR, BINARY-SHORT,
      *>     BINARY-LONG, BINARY-DOUBLE, FLOAT-SHORT, FLOAT-LONG, or FLOAT-EXTENDED is implementor-defined."
      *>     GR12 states only MINIMUM ranges (BINARY-CHAR SIGNED -128 < n < 128, UNSIGNED 0 <= n < 256) and
      *>     then says "The implementor may allow a wider range", which §A.1 item 206 classes as an OPTIONAL,
      *>     documentation-required element. The NOTE's own parenthetical — "assuming an 8-bit representation"
      *>     — is the standard flagging precisely that assumption, inside an informative note.
      *>     ⛔ SO THESE TWO LINES PIN COBOL.NET'S DOCUMENTED DETERMINATION, NOT A STANDARD-FIXED VALUE.
      *>     docs/CONFORMANCE.md A.1 item 207 pins "BINARY-CHAR 1 byte ... two's complement, big-endian,
      *>     SIGNED and UNSIGNED the same width (GR21)", and item 206 declares the wider range "Not provided.
      *>     Each usage holds exactly the GR12 minimum range for its width: CHAR ±128 / 0–255". Given THAT
      *>     representation, §15.43.4 r2's "greatest finite magnitude that may be represented in argument-1"
      *>     is +127 and +255, and the pair is also a SIGN discrimination: the same 8 bits give 2^7-1 signed
      *>     and 2^8-1 unsigned. A conforming implementation with a 16-bit BINARY-CHAR would return
      *>     +32767 / +65535 and would not be wrong — these two lines move if item 206/207 ever move. The
      *>     five PICTURE'd rows above carry no such dependency: r2 plus the picture character-string fixes
      *>     them outright.
      *> Row 4 against row 5 is the third discrimination: adding the B and CR insertion symbols changes the
      *> SIGN-REPRESENTABILITY of the mask but not one digit position, so §15.43.4 r2 returns the same
      *> +99999.99 for both — a fold that let the sign symbols shift the capacity would disagree on one of them.
      *>
      *> §15.43.4 r2 states the returned value directly and HIGHEST-ALGEBRAIC has no equivalent arithmetic
      *> expression, so §15.4.1's closing sentence ("... unless otherwise specified in the function
      *> definition") leaves the FOLD itself normative under NATIVE arithmetic, which is what this file runs
      *> in — r2 is a question about argument-1's representable range, never about an arithmetic mode. What
      *> that range IS, is fixed by the standard for the five PICTURE'd rows and left implementor-defined for
      *> the two picture-less BINARY-CHAR rows (§13.18.60.4 GR21/GR12, above).
      *> §15.43.4 r1 constrains only a FLOATING-POINT numeric-edited argument-1; no argument here is one.
      *>
      *> Rendering: each folded value is MOVEd to PIC +99999.999, whose leftmost '+' is a FIXED INSERTION sign
      *> that prints '+' for a positive or zero value (§13.18.40.5 rule 5, Table 8) — every expected line
      *> therefore also asserts that the returned value is POSITIVE, which is half of what r2 says.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1HIALGNT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-S999   PIC S999.
       01 N-S9KB   PIC S9(4) BINARY.
       01 N-99V999 PIC 99V9(3).
       01 N-EDCR   PIC $**,**9.99BCR.
       01 N-ED     PIC $**,**9.99.
       01 N-BCS    USAGE BINARY-CHAR SIGNED.
       01 N-BCU    USAGE BINARY-CHAR UNSIGNED.
       01 SR       PIC +99999.999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-S999)   TO SR
           DISPLAY "S999=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-S9KB)   TO SR
           DISPLAY "S9K-BINARY=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-99V999) TO SR
           DISPLAY "99V999=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-EDCR)   TO SR
           DISPLAY "EDITED-BCR=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-ED)     TO SR
           DISPLAY "EDITED=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-BCS)    TO SR
           DISPLAY "BINCHAR-SIGNED=" SR
           MOVE FUNCTION HIGHEST-ALGEBRAIC(N-BCU)    TO SR
           DISPLAY "BINCHAR-UNSIGNED=" SR
           STOP RUN.
