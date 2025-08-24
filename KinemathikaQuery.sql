/* -----------------------------------------------------------
   Fresh rebuild of KinemathikaDb with sample data (7 days)
   ----------------------------------------------------------- */

-- 1) Drop & recreate the database
USE master;
IF DB_ID(N'KinemathikaDb') IS NOT NULL
BEGIN
    ALTER DATABASE [KinemathikaDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [KinemathikaDb];
END;
GO
CREATE DATABASE [KinemathikaDb];
GO
USE [KinemathikaDb];
GO

/* 2) Schema ------------------------------------------------ */

-- Classrooms
CREATE TABLE dbo.Classrooms
(
    ClassroomId  INT            NOT NULL PRIMARY KEY,
    ClassName    NVARCHAR(100)  NOT NULL,
    IsArchived   BIT            NOT NULL DEFAULT(0)
);

-- Students
CREATE TABLE dbo.Students
(
    StudentId NVARCHAR(20)   NOT NULL PRIMARY KEY,
    Name      NVARCHAR(120)  NOT NULL,
    Email     NVARCHAR(256)  NOT NULL
);

-- Enrollments (Class <-> Student)
CREATE TABLE dbo.Enrollments
(
    ClassroomId INT           NOT NULL,
    StudentId   NVARCHAR(20)  NOT NULL,
    CONSTRAINT PK_Enrollments PRIMARY KEY (ClassroomId, StudentId),
    CONSTRAINT FK_Enrollments_Classrooms
        FOREIGN KEY (ClassroomId) REFERENCES dbo.Classrooms(ClassroomId),
    CONSTRAINT FK_Enrollments_Students
        FOREIGN KEY (StudentId)   REFERENCES dbo.Students(StudentId)
);
CREATE INDEX IX_Enrollments_Classroom ON dbo.Enrollments(ClassroomId);
CREATE INDEX IX_Enrollments_Student   ON dbo.Enrollments(StudentId);

-- Attempt records (now includes class_id)
CREATE TABLE dbo.AttemptRecords
(
    attempt_id           BIGINT        IDENTITY(1,1) PRIMARY KEY,
    class_id             INT           NOT NULL,
    student_id           NVARCHAR(20)  NOT NULL,
    concept_id           VARCHAR(10)   NOT NULL,  -- 'dd' | 'sv' | 'acc'
    attempts_to_correct  INT           NOT NULL,  -- >= 0
    time_to_correct_ms   INT           NOT NULL,  -- milliseconds
    ended_at             DATETIME2(0)  NOT NULL,  -- UTC/local; your choice

    CONSTRAINT FK_Attempt_Class
        FOREIGN KEY (class_id)   REFERENCES dbo.Classrooms(ClassroomId),
    CONSTRAINT FK_Attempt_Student
        FOREIGN KEY (student_id) REFERENCES dbo.Students(StudentId),
    CONSTRAINT CK_Attempt_Concept   CHECK (concept_id IN ('dd','sv','acc')),
    CONSTRAINT CK_Attempt_Attempts  CHECK (attempts_to_correct >= 0),
    CONSTRAINT CK_Attempt_Time      CHECK (time_to_correct_ms >= 0)
);

-- Helpful indexes for your API queries
CREATE INDEX IX_Attempts_ClassStudentDate ON dbo.AttemptRecords(class_id, student_id, ended_at);
CREATE INDEX IX_Attempts_StudentDate      ON dbo.AttemptRecords(student_id, ended_at);
CREATE INDEX IX_Attempts_Concept          ON dbo.AttemptRecords(concept_id);
GO

/* 3) Seed data --------------------------------------------- */

-- Classes 20..23
INSERT INTO dbo.Classrooms (ClassroomId, ClassName)
VALUES (20, N'Physics 1-A'),
       (21, N'Physics 1-B'),
       (22, N'Physics 1-C'),
       (23, N'Physics 1-D');

-- 12 students per class; enroll them
DECLARE @c INT = 20;
WHILE @c <= 23
BEGIN
    DECLARE @s INT = 1;
    WHILE @s <= 12
    BEGIN
        DECLARE @sid NVARCHAR(20) = CONCAT('C', RIGHT('00' + CAST(@c AS VARCHAR(3)), 2), '-', RIGHT('000' + CAST(@s AS VARCHAR(3)), 3));

        IF NOT EXISTS (SELECT 1 FROM dbo.Students WHERE StudentId = @sid)
        BEGIN
            INSERT dbo.Students (StudentId, Name, Email)
            VALUES (@sid,
                    CONCAT('Student ', @sid),
                    CONCAT(LOWER(REPLACE(@sid, '-', '')), '@class.local'));
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.Enrollments WHERE ClassroomId = @c AND StudentId = @sid)
        BEGIN
            INSERT dbo.Enrollments (ClassroomId, StudentId)
            VALUES (@c, @sid);
        END

        SET @s += 1;
    END
    SET @c += 1;
END
GO

ALTER TABLE dbo.AttemptRecords
    ADD problem_no TINYINT NOT NULL CONSTRAINT DF_Attempt_problem_no DEFAULT(1);
GO

ALTER TABLE dbo.AttemptRecords
    ADD CONSTRAINT CK_Attempt_ProblemNo CHECK (problem_no BETWEEN 1 AND 15);
GO

-- Backfill existing rows with a stable random 1..15 (only run once on legacy data)
;WITH S AS
(
  SELECT attempt_id,
         (ABS(CHECKSUM(NEWID())) % 15) + 1 AS n
  FROM dbo.AttemptRecords
)
UPDATE a SET a.problem_no = s.n
FROM dbo.AttemptRecords a
JOIN S ON S.attempt_id = a.attempt_id;
GO

-- Attempts for every student, 3 concepts, 2025-08-16..2025-08-22 (7 days)
/* === Reseed AttemptRecords with explicit problem_no (1..15) and varied completion === */

-- Window to spread timestamps
DECLARE @start DATE = '2025-08-16';
DECLARE @end   DATE = '2025-08-22';
DECLARE @days  INT  = DATEDIFF(DAY, @start, @end) + 1;

-- Start clean (we’re rebuilding fresh anyway)
TRUNCATE TABLE dbo.AttemptRecords;

-- Helpful index for the new column (safe to re-run)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Attempts_StudentConceptProblem' AND object_id = OBJECT_ID(N'dbo.AttemptRecords'))
    CREATE INDEX IX_Attempts_StudentConceptProblem ON dbo.AttemptRecords(student_id, concept_id, problem_no);

-- Static sets
DECLARE @Concepts TABLE (concept_id VARCHAR(10));
INSERT @Concepts VALUES ('dd'),('sv'),('acc');

DECLARE @Problems TABLE (problem_no TINYINT);
INSERT @Problems VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15);

;WITH StudentsX AS
(
    SELECT e.ClassroomId AS class_id, e.StudentId
    FROM dbo.Enrollments e
),
/* For each (student, concept) choose how far they “completed”.
   ~25% go all the way to 15; others vary. */
StudentConcepts AS
(
    SELECT 
        sx.class_id,
        sx.StudentId,
        c.concept_id,
        CompletedMax =
            CASE 
                WHEN ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 1)) % 100 < 25 THEN 15                                    -- 25% reach 15
                WHEN ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 2)) % 100 < 55 THEN 12 + (ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 3)) % 4)  -- 12..15
                WHEN ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 4)) % 100 < 85 THEN  8 + (ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 5)) % 5)  -- 8..12
                ELSE 3 + (ABS(CHECKSUM(NEWID(), sx.StudentId, c.concept_id, 6)) % 5)                                                                 -- 3..7
            END
    FROM StudentsX sx
    CROSS JOIN @Concepts c
)
-- Completed problems (problem_no <= CompletedMax)
INSERT dbo.AttemptRecords (class_id, student_id, concept_id, problem_no, attempts_to_correct, time_to_correct_ms, ended_at)
SELECT
    sc.class_id,
    sc.StudentId,
    sc.concept_id,
    p.problem_no,
    1 + (ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p.problem_no, 7)) % 3) AS attempts_to_correct,   -- 1..3
    ((45 + (ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p.problem_no, 8)) % 240)) * 1000) AS time_ms, -- 45..284s
    DATEADD(SECOND,
            ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p.problem_no, 9)) % 86400,
            DATEADD(DAY,
                    ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p.problem_no, 10)) % @days,
                    CAST(@start AS DATETIME2(0))))
FROM StudentConcepts sc
JOIN @Problems p ON p.problem_no <= sc.CompletedMax
UNION ALL
-- Incomplete attempts beyond their depth (~20% of remaining problems), attempts 3..5
SELECT
    sc.class_id,
    sc.StudentId,
    sc.concept_id,
    p2.problem_no,
    3 + (ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p2.problem_no, 11)) % 3),                        -- 3..5
    ((45 + (ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p2.problem_no, 12)) % 240)) * 1000),
    DATEADD(SECOND,
            ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p2.problem_no, 13)) % 86400,
            DATEADD(DAY,
                    ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p2.problem_no, 14)) % @days,
                    CAST(@start AS DATETIME2(0))))
FROM StudentConcepts sc
JOIN @Problems p2 ON p2.problem_no > sc.CompletedMax
WHERE (ABS(CHECKSUM(NEWID(), sc.StudentId, sc.concept_id, p2.problem_no, 15)) % 5) = 0;                      -- 20%
GO

PRINT '✅ AttemptRecords seeded with varied completion depths (problem_no 1..15 for dd/sv/acc).';
