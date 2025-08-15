// Models/Analytics/AttemptEndedStatus.cs
// what it does: enum used by AttemptRecord.ended_status
namespace Kinemathika.Models.Analytics
{
    public enum AttemptEndedStatus
    {
        correct,   // first submission eventually correct
        exit,      // user exited the problem/level
        timeout    // session timed out
    }
}
