/* =====================================================================
   RaceDay - Part 1, Section C: SQL Database Script
   ---------------------------------------------------------------------
   Target: Microsoft SQL Server (SSMS)
   Purpose: Creates the full RaceDay schema and seeds it with sample
            data (2 Organisers, 2 Participants, 3 Events, categories
            per event, and sample enrolments/results).

   Naming note: table names are pluralised versions of the ERD entity
   names (User -> Users, Event -> Events, EventType -> EventTypes,
   Category -> Categories, Enrolment -> Enrolments, Result -> Results).
   USER is a reserved word in T-SQL, so pluralising avoids having to
   bracket-escape it everywhere. This is a deliberate, purely cosmetic
   naming difference from the ERD - structure, keys and relationships
   are otherwise identical.

   Run this script in SSMS while connected to your SQL Server instance
   (e.g. (local)\SQLEXPRESS) - it creates its own RaceDay database, so
   you do NOT need to create or select a database first. Just hit
   Execute with any database selected in the toolbar (master is fine,
   the script switches itself).
   ===================================================================== */

/* =====================================================================
   0. Create the RaceDay database (if it doesn't already exist) and
   switch into it. Everything after this point runs inside RaceDay,
   never inside master.
   ===================================================================== */
IF DB_ID('RaceDay') IS NULL
BEGIN
    CREATE DATABASE RaceDay;
END

USE RaceDay;


SET NOCOUNT ON;


/* ---------------------------------------------------------------------
   Drop tables if they already exist (clean re-run support), in
   child-to-parent order to respect foreign keys.
   --------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Results', 'U') IS NOT NULL DROP TABLE dbo.Results;
IF OBJECT_ID('dbo.Enrolments', 'U') IS NOT NULL DROP TABLE dbo.Enrolments;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.EventTypes', 'U') IS NOT NULL DROP TABLE dbo.EventTypes;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;


/* =====================================================================
   1. Users
   Stores both Organisers and Participants, distinguished by Role.
   ===================================================================== */
CREATE TABLE dbo.Users
(
    UserId              INT IDENTITY(1,1)   NOT NULL,
    FullName            NVARCHAR(100)       NOT NULL,
    Email               NVARCHAR(150)       NOT NULL,
    PasswordHash        NVARCHAR(255)       NOT NULL,
    Role                NVARCHAR(20)        NOT NULL,
    ProfilePictureUrl   NVARCHAR(500)       NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser', 'Participant'))
);


/* =====================================================================
   2. EventTypes
   Lookup table: Run / Walk / Cycle.
   ===================================================================== */
CREATE TABLE dbo.EventTypes
(
    EventTypeId INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(30)      NOT NULL,

    CONSTRAINT PK_EventTypes PRIMARY KEY (EventTypeId),
    CONSTRAINT UQ_EventTypes_Name UNIQUE (Name)
);


/* =====================================================================
   3. Events
   Owned by an Organiser (Users), classified by EventType.
   ===================================================================== */
CREATE TABLE dbo.Events
(
    EventId         INT IDENTITY(1,1)   NOT NULL,
    Name            NVARCHAR(150)       NOT NULL,
    Description     NVARCHAR(1000)      NULL,
    Date            DATETIME2           NOT NULL,
    Location        NVARCHAR(150)       NOT NULL,
    Distance        DECIMAL(6,2)        NOT NULL,
    EventTypeId     INT                 NOT NULL,
    OrganiserId     INT                 NOT NULL,
    BannerImageUrl  NVARCHAR(500)       NULL,

    CONSTRAINT PK_Events PRIMARY KEY (EventId),
    CONSTRAINT FK_Events_EventTypes FOREIGN KEY (EventTypeId)
        REFERENCES dbo.EventTypes (EventTypeId),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Events_Distance CHECK (Distance > 0)
);


/* =====================================================================
   4. Categories
   Age/distance categories, scoped to one Event.
   ===================================================================== */
CREATE TABLE dbo.Categories
(
    CategoryId  INT IDENTITY(1,1) NOT NULL,
    EventId     INT               NOT NULL,
    Name        NVARCHAR(50)      NOT NULL,

    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId) ON DELETE CASCADE,
    CONSTRAINT UQ_Categories_EventName UNIQUE (EventId, Name)
);


/* =====================================================================
   5. Enrolments
   Links a Participant (Users) to an Event under a chosen Category.
   ===================================================================== */
CREATE TABLE dbo.Enrolments
(
    EnrolmentId     INT IDENTITY(1,1)   NOT NULL,
    ParticipantId   INT                 NOT NULL,
    EventId         INT                 NOT NULL,
    CategoryId      INT                 NOT NULL,
    EnrolmentStatus NVARCHAR(20)        NOT NULL DEFAULT 'Pending',
    EnrolmentDate   DATETIME2           NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT PK_Enrolments PRIMARY KEY (EnrolmentId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId)
        REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Enrolments_Events FOREIGN KEY (EventId)
        REFERENCES dbo.Events (EventId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId)
        REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (EnrolmentStatus IN ('Pending', 'Confirmed')),
    -- A Participant may only enrol once per Event (prevents duplicate enrolment)
    CONSTRAINT UQ_Enrolments_Participant_Event UNIQUE (ParticipantId, EventId)
);


/* =====================================================================
   6. Results
   One-to-one with Enrolments (unique FK). Only exists once an event
   has concluded and the Organiser has captured the outcome.
   ===================================================================== */
CREATE TABLE dbo.Results
(
    ResultId            INT IDENTITY(1,1)  NOT NULL,
    EnrolmentId         INT                NOT NULL,
    FinishTime          TIME(0)            NOT NULL,
    FinishingPosition    INT                NOT NULL,

    CONSTRAINT PK_Results PRIMARY KEY (ResultId),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId)
        REFERENCES dbo.Enrolments (EnrolmentId),
    CONSTRAINT UQ_Results_Enrolment UNIQUE (EnrolmentId),
    CONSTRAINT CK_Results_Position CHECK (FinishingPosition > 0)
);



/* =====================================================================
   SEED DATA
   ===================================================================== */

/* ---- Users: 2 Organisers, 2 Participants ---------------------------- */
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, ProfilePictureUrl)
VALUES
    ('Thabo Nkosi',     'thabo.nkosi@raceday.co.za',     'AQAAAAIAAYagAAAAEHash001==', 'Organiser',   NULL),
    ('Lerato Dube',      'lerato.dube@raceday.co.za',     'AQAAAAIAAYagAAAAEHash002==', 'Organiser',   NULL),
    ('Sipho Mahlangu',   'sipho.mahlangu@example.com',    'AQAAAAIAAYagAAAAEHash003==', 'Participant', NULL),
    ('Anika van Wyk',    'anika.vanwyk@example.com',      'AQAAAAIAAYagAAAAEHash004==', 'Participant', NULL);


/* ---- EventTypes ------------------------------------------------------ */
INSERT INTO dbo.EventTypes (Name)
VALUES ('Run'), ('Walk'), ('Cycle');


/* ---- Events: 3 events, each owned by an Organiser -------------------- */
-- OrganiserId 1 = Thabo Nkosi, OrganiserId 2 = Lerato Dube
-- EventTypeId 1 = Run, 2 = Walk, 3 = Cycle
INSERT INTO dbo.Events (Name, Description, Date, Location, Distance, EventTypeId, OrganiserId, BannerImageUrl)
VALUES
    ('Pretoria Spring Fun Run', 'A community fun run through the Pretoria CBD and Union Buildings gardens.',
        '2026-10-18T07:00:00', 'Pretoria, Gauteng', 10.00, 1, 1, NULL),
    ('Cape Peninsula Cycle Classic', 'A scenic cycling event along the Cape Peninsula coastline.',
        '2026-11-08T06:00:00', 'Cape Town, Western Cape', 100.00, 3, 2, NULL),
    ('Durban Beachfront Charity Walk', 'A family-friendly charity walk along the Durban beachfront promenade.',
        '2026-09-27T08:00:00', 'Durban, KwaZulu-Natal', 5.00, 2, 1, NULL);


/* ---- Categories: 2-3 per event ---------------------------------------- */
-- EventId 1 = Pretoria Spring Fun Run
INSERT INTO dbo.Categories (EventId, Name) VALUES
    (1, 'Under 20'),
    (1, 'Senior (20-39)'),
    (1, 'Veteran (40+)');
-- EventId 2 = Cape Peninsula Cycle Classic
INSERT INTO dbo.Categories (EventId, Name) VALUES
    (2, '50km'),
    (2, '100km');
-- EventId 3 = Durban Beachfront Charity Walk
INSERT INTO dbo.Categories (EventId, Name) VALUES
    (3, 'Open'),
    (3, 'Family (with children)');


/* ---- Enrolments -------------------------------------------------------- */
-- ParticipantId 3 = Sipho Mahlangu, ParticipantId 4 = Anika van Wyk
INSERT INTO dbo.Enrolments (ParticipantId, EventId, CategoryId, EnrolmentStatus)
VALUES
    (3, 1, 2, 'Confirmed'),  -- Sipho -> Pretoria Fun Run -> Senior (20-39)
    (4, 1, 3, 'Confirmed'),  -- Anika -> Pretoria Fun Run -> Veteran (40+)
    (3, 2, 5, 'Pending'),    -- Sipho -> Cape Peninsula Cycle Classic -> 100km
    (4, 3, 6, 'Confirmed');  -- Anika -> Durban Beachfront Charity Walk -> Open


/* ---- Results: captured for the two Confirmed, already-concluded style enrolments ---- */
-- EnrolmentId 1 = Sipho @ Pretoria Fun Run, EnrolmentId 2 = Anika @ Pretoria Fun Run
INSERT INTO dbo.Results (EnrolmentId, FinishTime, FinishingPosition)
VALUES
    (1, '00:52:14', 47),
    (2, '00:58:03', 112);


/* =====================================================================
   Verification
   Run this on its own after the script finishes - DB_NAME() should
   return 'RaceDay', and each SELECT should return your seeded rows.
   ===================================================================== */
SELECT DB_NAME() AS CurrentDatabase;

SELECT * FROM Users;
SELECT * FROM EventTypes;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM Enrolments;
SELECT * FROM Results;