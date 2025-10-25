using System.Threading;
using System.Threading.Tasks;

namespace ChemGa.Interfaces;

public interface ICronJob
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}
