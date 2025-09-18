using System.Collections.Generic;
using System.Threading.Tasks;
using SPT.Launcher.Models.Launcher;

namespace SPT.Launcher.Interfaces
{
    public interface IGameStarterFrontend
    {
        Task CompletePatchTask(IAsyncEnumerable<PatchResultInfo> task);
    }
}