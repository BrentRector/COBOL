*>--- conformance-case
*> id: pb829-special-names-unrecognized-clause
*> rules: [FMT-12.3.7.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB829 - the CLOSED ISO 12.3.7.2 general format.  ONE grammar rule -
      *> `genericClause : IDENTIFIER (IDENTIFIER|literal)*`, described in the grammar as a
      *> "vendor/extension hook" - was wired into SIX sites spanning EIGHT closed general formats, and
      *> every one of them swallowed any word run the format does not define, at every edition and every
      *> strictness, with no diagnostic anywhere.  This program compiled CLEAN and RAN, with WIBBLE simply
      *> discarded.  ISO 4.2.2 makes a general format the definition of what may be written ("An
      *> implementation shall provide a warning mechanism ... to indicate violations of the general formats
      *> and the explicit syntax rules of standard COBOL"), so the word is now NAMED: COBOLNET1970.
      *> The 12.3.7.2 clause list was RENDERED from the printed page (PDF p320 / folio 290) before it was
      *> closed - closing a list against the lossy OCR diagram would REJECT LEGAL SOURCE, which is strictly
      *> worse than the silence it replaces.
      *> Rejected at 85 as well as 2023: the general format is a closed list in every edition of the
      *> standard, and neither WIBBLE nor WOBBLE is a reserved word at any of them.
      *> The residue here carries a LITERAL deliberately.  The printed format's third arm is
      *> `device-name-1 IS mnemonic-name-3` with IS un-underlined, so a bare TWO-WORD entry is
      *> shape-legal and what refuses it is 12.3.7.3 SR8 (the implementor specifies the available
      *> names) - a SEMANTIC rule, not this general format.  No arm of the format admits a literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB829SN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           WIBBLE "WOBBLE".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
