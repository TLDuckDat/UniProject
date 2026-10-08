using UniProject.Share.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UniProject.Share.Infrastructure.Events
{
    public class InMemoryEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryEventBus> _logger;

        public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            using var scope = _serviceProvider.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();
            foreach (var handler in handlers)
            {
                try
                {
                    await handler.HandleAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý domain event {EventType} bởi {HandlerType}", typeof(TEvent).Name, handler.GetType().Name);
                }
            }
        }
    }

}
