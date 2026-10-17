*>--- conformance-case
*> id: da1_alphabet_hex_thru
*> rules: [GR-8.3.3.6.4-7, GR-12.3.7.4-7, GR-12.3.7.4-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> DA1 (CONFORMANCE-FIX-QUEUE, discovered during CA3): a HEXADECIMAL-format literal (X"hh", ISO §8.3.3.2 — each
      *> hex-digit pair is one character) in an ALPHABET clause is decoded to its character value, so a THRU range over
      *> hex literals assigns positions per §12.3.7.4 GR5. ALPHABET AL IS X"5A" THRU X"41" is the DESCENDING native run
      *> Z(X"5A")..A(X"41"), assigned ascending positions 1..26 — so LOW-VALUE (lowest position) = 'Z' (§8.3.3.6.4 GR7)
      *> and 'A' (position 26) collates ABOVE 'Z' (position 1) (§8.8.4.2.7). Pre-fix a hex literal fell through to raw
      *> text, its length != 1 skipped the THRU range, and the alphabet was silently left NATIVE (LOW-VALUE = X"00",
      *> 'A' < 'Z') — the DA1 defect. A char-literal THRU ("Z" THRU "A") always worked; only hex decode was missing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DA1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
       SPECIAL-NAMES. ALPHABET AL IS X"5A" THRU X"41".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE LOW-VALUE TO WS-X
           DISPLAY "LOW=" WS-X
           IF "A" > "Z" DISPLAY "A-GT-Z" ELSE DISPLAY "Z-GT-A" END-IF
           STOP RUN.
