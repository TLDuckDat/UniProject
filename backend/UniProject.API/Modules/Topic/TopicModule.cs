using Microsoft.Extensions.DependencyInjection;

namespace UniProject.API.Modules.Topic
{
    public static class TopicModule
    {
        public static IServiceCollection AddTopicModule(this IServiceCollection services)
        {
            services.AddScoped<ITopicService, TopicService>();
            return services;
        }
    }
}
