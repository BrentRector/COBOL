*>--- conformance-case
*> id: pb390-set-switch-status-condition-name
*> rules: [SR-14.9.39.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - ISO 14.9.39.3 SR6: "Condition-name-1 shall be associated with a conditional variable."
*> 8.4.4.1 says there are TWO kinds of condition-name - the level-88 kind, associated with a conditional
*> variable, and the SPECIAL-NAMES kind, associated with the on/off status of an implementor-defined
*> switch - and SET Format 4 admits only the first. The switch is set through Format 3, whose general
*> format (rendered from the printed page, not the OCR) prints mnemonic-name-1 and no condition-name.
*> SW-ON is the DISCRIMINATING operand: the rule exists for exactly this case, and before PB390 the
*> compiler compiled the program and aborted the run unit under "SET 'SW-ON' TO TRUE (not a
*> condition-name)" - a message denying the very fact the rule turns on, since SW-ON IS a condition-name.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390SETSR6.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SWITCH-1 IS SW-M ON STATUS IS SW-ON OFF STATUS IS SW-OFF.
PROCEDURE DIVISION.
MAIN.
    SET SW-ON TO TRUE.
    STOP RUN.
