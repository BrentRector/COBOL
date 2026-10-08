*> reject-at: 2002 2014 2023
*> ISO 1989:2023 8.4.3.13.3 SR1 (the program-address-identifier): "Identifier-1 shall be of category
*> alphanumeric or national." AE (PIC XXXBXX) is category ALPHANUMERIC-EDITED (8.5.2.4), a category of
*> its own in 8.5.2.1 Table 2, so it cannot name the program. A REFERENCE-MODIFIED view of it would be
*> legal: 8.4.3.3.4 GR6 a) makes that view category alphanumeric (the positive golden
*> pb850_edited_category_screens). The screen read the category alone and accepted it (kb/Work PB850).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB850SPAE.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 PP USAGE PROGRAM-POINTER.
01 AE PIC XXXBXX VALUE "PB8 50".
PROCEDURE DIVISION.
MAIN.
    SET PP TO ADDRESS OF PROGRAM AE
    STOP RUN.
END PROGRAM PB850SPAE.
