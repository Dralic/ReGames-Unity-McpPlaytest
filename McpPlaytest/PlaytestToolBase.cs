using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace McpPlaytest
{
    public abstract class PlaytestToolBase
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool IsAsync { get; protected set; } = false;

        public virtual JObject Execute(JObject parameters)
        {
            return PlaytestSocketHandler.CreateErrorResponse(
                "Execute must be overridden if IsAsync is false.",
                "implementation_error"
            );
        }

        public virtual void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
        {
            tcs.TrySetException(new System.NotImplementedException("ExecuteAsync must be overridden if IsAsync is true."));
        }
    }
}
