*>--- conformance-case
*> id: pb236-open-undeclared-file
*> rules: [FMT-14.9.27.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB236 - OPEN's general format (ISO 14.9.27.2) writes the operand as file-name-1, and ISO 8.4.2.1
*> fixes what a name must do: "In order to use a resource, a statement shall contain a reference that uniquely
*> identifies that resource." NOSUCH identifies no file connector, so it identifies no resource.
*> SEVEN binder sites - UNLOCK, OPEN, CLOSE, READ, DELETE, DELETE FILE, START - each wrote its own
*> FilesByName.TryGetValue and its own unreported BoundUnsupported. They now share one resolution step, and
*> the diagnostic is the EXISTING undefined-reference descriptor rather than an eighth spelling of it.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB236OPENUND.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC X(5).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT NOSUCH.
    STOP RUN.
