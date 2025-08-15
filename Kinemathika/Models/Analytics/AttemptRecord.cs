// Models/Analytics/AttemptRecord.cs
// WHAT IT DOES: maps 1:1 to dbo.AttemptRecords columns
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kinemathika.Models.Analytics
{
    [Table("AttemptRecords")]
    public class AttemptRecord
    {
        [Key]
        public int id { get; set; }

        [Column("student_id")]
        public string student_id { get; set; } = "";

        [Column("session_id")]
        public string session_id { get; set; } = "";

        [Column("concept_id")]
        public string concept_id { get; set; } = "";

        [Column("worksheet_id")]
        public string worksheet_id { get; set; } = "";

        [Column("problem_id")]
        public string problem_id { get; set; } = "";

        [Column("started_at")]
        public DateTime started_at { get; set; }

        [Column("ended_at")]
        public DateTime ended_at { get; set; }

        // Store as string in DB; we’ll read it as string for simplicity
        [Column("ended_status")]
        public string ended_status { get; set; } = "";

        [Column("first_attempt_correct")]
        public bool first_attempt_correct { get; set; }

        [Column("attempts_to_correct")]
        public int attempts_to_correct { get; set; }

        [Column("level_attempt_accuracy")]
        public double level_attempt_accuracy { get; set; }

        [Column("time_to_correct_ms")]
        public int time_to_correct_ms { get; set; }

        [Column("mastery_valid")]
        public bool mastery_valid { get; set; }
    }
}
