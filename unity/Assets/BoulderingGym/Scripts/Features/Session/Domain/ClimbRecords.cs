using System;
using System.Collections.Generic;

namespace Project.Features.Session.Domain
{
    [Serializable]
    public sealed class ProblemRecord
    {
        public string problemId;
        public int attempts;
        public int tops;

        /// <summary>Fastest top in seconds; negative while never topped.</summary>
        public float bestSeconds = -1f;

        public bool IsTopped => tops > 0;
    }

    /// <summary>Personal results per problem, saved between sessions.</summary>
    [Serializable]
    public sealed class ClimbRecords
    {
        public List<ProblemRecord> problems = new();

        public ProblemRecord Find(string problemId) => problems.Find(record => record.problemId == problemId);

        public void AddAttempt(string problemId) => Get(problemId).attempts++;

        /// <summary>Counts a top; true when it beats the previous best time.</summary>
        public bool AddTop(string problemId, float seconds)
        {
            var record = Get(problemId);
            record.tops++;
            var isBest = record.bestSeconds < 0f || seconds < record.bestSeconds;
            if (isBest)
            {
                record.bestSeconds = seconds;
            }

            return isBest;
        }

        private ProblemRecord Get(string problemId)
        {
            var record = Find(problemId);
            if (record != null)
            {
                return record;
            }

            record = new ProblemRecord { problemId = problemId };
            problems.Add(record);
            return record;
        }
    }
}
