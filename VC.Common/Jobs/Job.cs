using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VC.Jobs
{
    public interface IJobResult
    {
        bool Success { get; }
        Status Status { get; }
    }

    [Serializable]
    public class JobResult : IJobResult
    {
        public bool Success { get; set; }
        public Status Status { get; set; }
        public string StatusMessage { get; set; }
    }

    public interface IJob : IHasId, IHasGuid
    {
        IJobResult Execute();
    }

    public abstract class Job : IJob
    {
        public string Guid { get; protected set; }
        public string Id { get; protected set; }

        protected Job(string guid)
        {
            Guid = guid;
            Id = GetType().Name;
        }

        public abstract IJobResult Execute();
    }

    public class TestJob : Job
    {
        public TestJob()
            : base(Util.NewGuid())
        {
            
        }

        public override IJobResult Execute()
        {
            Util.Msg("TestJob Executing - Weee!");

            return new JobResult()
                   {
                       Success = true,
                       StatusMessage = "Test Complete"
                   };
        }
    }
}
