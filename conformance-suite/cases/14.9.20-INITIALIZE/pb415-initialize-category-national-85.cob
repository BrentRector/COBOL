*>--- conformance-case
*> id: pb415-initialize-category-national-85
*> rules: [FMT-14.9.20.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB415 — the EDITION EDGE of the INITIALIZE category-name list. ISO 14.9.20.2 prints thirteen
*> category names; five are COBOL-85 words and eight entered later with the data categories they name.
*> ISO 8.9 reserves NATIONAL from 2002 (tests/version-matrix/reserved-words.json r85=false), so at
*> --std 85 `REPLACING NATIONAL` names a category-name COBOL-85's list does not contain and the
*> initialize-category-2002 construct row refuses it COBOLNET0900.
*> It compiles at 2002 and above — the category-name is conforming there, and with no national item in G it
*> simply selects no receiving operand (14.9.20.4 GR5c2), which is the rule's own answer and not a gap.
*> The '85 POSITIVE control is tests/conformance/85/pb415_initialize_multi_category_85: the five classic
*> words, including a MULTI-category category-name, are COBOL-85 source and must compile there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB415NN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A1 PIC X(3) VALUE "xyz".
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE G REPLACING NATIONAL DATA BY "z".
           STOP RUN.
