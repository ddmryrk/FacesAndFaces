using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace Faces.WebMvc.Services
{
    public class BusService : IHostedService
    {
        private readonly IBusControl busControl;
        public BusService(IBusControl busControl)
            => this.busControl = busControl;

        public Task StartAsync(CancellationToken cancellationToken)
            => busControl.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
            => busControl.StopAsync(cancellationToken);
    }
}
