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

-- Attempts for every student, 3 concepts, 2025-08-16..2025-08-22 (7 days)
DECLARE @start DATE = '2025-08-16';
DECLARE @end   DATE = '2025-08-22';

;WITH Dates AS
(
    SELECT @start AS d
    UNION ALL
    SELECT DATEADD(DAY, 1, d) FROM Dates WHERE d < @end
),
StudentsX AS
(
    SELECT e.ClassroomId AS class_id, e.StudentId
    FROM dbo.Enrollments e
),
Concepts AS
(
    SELECT 'dd' AS concept_id UNION ALL
    SELECT 'sv' UNION ALL
    SELECT 'acc'
)
INSERT dbo.AttemptRecords (class_id, student_id, concept_id, attempts_to_correct, time_to_correct_ms, ended_at)
SELECT  sx.class_id,
        sx.StudentId,
        c.concept_id,
        (ABS(CHECKSUM(NEWID())) % 3) + 1,                 -- 1..3 attempts
        ((ABS(CHECKSUM(NEWID())) % 240) + 45) * 1000,     -- 45..284 seconds (ms)
        DATEADD(SECOND, ABS(CHECKSUM(NEWID())) % 86400, CAST(d.d AS DATETIME2(0)))  -- random time in the day
FROM Dates d
CROSS JOIN StudentsX sx
CROSS JOIN Concepts c
OPTION (MAXRECURSION 32767);

-- Sprinkle some easy first-try solves on the first day
INSERT dbo.AttemptRecords (class_id, student_id, concept_id, attempts_to_correct, time_to_correct_ms, ended_at)
SELECT  e.ClassroomId, e.StudentId, 'dd',
        1, ((ABS(CHECKSUM(NEWID())) % 60) + 10) * 1000,
        DATEADD(SECOND, 60, CAST(@start AS DATETIME2(0)))
FROM dbo.Enrollments e
WHERE (ABS(CHECKSUM(NEWID())) % 4) = 0;
GO

PRINT '✅ KinemathikaDb rebuilt and seeded (classes 20–23, students + attempts for 2025-08-16..2025-08-22).';
