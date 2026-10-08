namespace UniProject.Share.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
    public abstract record DomainEvent : IDomainEvent
    {
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }

    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
    }

}
