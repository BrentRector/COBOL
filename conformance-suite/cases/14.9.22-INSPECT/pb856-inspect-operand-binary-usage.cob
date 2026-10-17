*>--- conformance-case
*> id: pb856-inspect-operand-binary-usage
*> rules: [SR-14.9.22.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB856 — the refusing half of ISO 14.9.22.3 SR2: "Identifier-3, ... , identifier-n shall reference an
*> elementary item described implicitly or explicitly as usage display or national." PB856 wrote the rule's
*> NATIONAL half, which had been missing; the rule still refuses every other usage, here a BINARY item as the
*> TALLYING FOR operand. Unchanged at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB856NBU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "ABABA".
       01 B PIC 9(4) USAGE BINARY VALUE 1.
       01 CT PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S TALLYING CT FOR ALL B
           STOP RUN.
