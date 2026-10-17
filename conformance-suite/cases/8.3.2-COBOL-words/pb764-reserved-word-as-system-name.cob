*>--- conformance-case
*> id: pb764-reserved-word-as-system-name
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB764 - ISO 8.3.2.1 1): "Reserved words shall not be used as
*> user-defined words or system-names." external-locale-name-1 (the LOCALE
*> clause, 12.3.7.2) and entry-convention-name-1 (the ENTRY-CONVENTION
*> clause, 11.9.7) are SYSTEM-NAMES (8.3.2.3.1); NESTED and USER-DEFAULT are 8.9-reserved
*> from 2002 - so both slots below are refused with COBOLNET0901. The 8.9
*> funnel's retired keyword ladder exempted BOTH slots whole (it could not
*> tell the keyword COBOL from the name beside it), so this compiled clean
*> before PB764 split the keyword off as a `formatWord`.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB764SYSNAME.
       OPTIONS.
           ENTRY-CONVENTION IS NESTED.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE L1 IS USER-DEFAULT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE".
           STOP RUN.
