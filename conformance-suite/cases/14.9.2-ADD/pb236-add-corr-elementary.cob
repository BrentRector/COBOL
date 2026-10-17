*>--- conformance-case
*> id: pb236-add-corr-elementary
*> rules: [SR-14.9.2.3-6, SR-14.9.44.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - ISO 14.9.2.3 SR6: "Identifier-4 and identifier-5 shall be alphanumeric group items,
*> national group items, variable-length groups, or strongly-typed group items and shall not be described
*> with level-number 66." ELEM is elementary, so the statement violates SR6 and 4.2.2 paragraph 2 puts the
*> verdict in the COMPILE-TIME mechanism. Before PB236 the binder had the predicate AND the citation and
*> still returned BoundUnsupported, so this program compiled clean and threw
*> NotImplementedCobolFeatureException - telling the programmer THE COMPILER was incomplete when the SOURCE
*> was wrong - and only if the statement was reached.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236CORRELEM.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ELEM PIC 9(3) VALUE 5.
01 GRP.
   05 A PIC 9(3) VALUE 1.
PROCEDURE DIVISION.
MAIN.
    ADD CORRESPONDING ELEM TO GRP.
    STOP RUN.
