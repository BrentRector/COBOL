*>--- conformance-case
*> id: pb416-initialize-target-class-index
*> rules: [SR-13.18.60.3-10, SR-14.9.20.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB416 — ISO 14.9.20.3 SR1: "Identifier-1 shall be strongly typed or of class alphabetic,
*> alphanumeric, boolean, message-tag, national, numeric, object, or pointer." 8.5.2.1 Table 2 lists NINE
*> classes and the rule admits eight, so the whole rule is one exclusion: class INDEX, which 8.5.2.8 gives to
*> "an elementary data item explicitly or implicitly described as usage index". The rule and its class list
*> are COBOL-85, so the rejection holds at every edition.
*> Before PB416 this compiled clean, stored nothing and printed nothing: 14.9.20.4 GR5a1's exclusion of an
*> index item CONTAINED IN identifier-1 was being applied to identifier-1 ITSELF, turning a syntax error into
*> a silent no-op. That exclusion's own silence is still correct and is pinned by the POSITIVE control,
*> tests/conformance/85/pb416_initialize_operand_screens_85, whose G contains a subordinate USAGE INDEX item
*> that INITIALIZE G skips without a word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB416NIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 IDX USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE IDX.
           STOP RUN.
