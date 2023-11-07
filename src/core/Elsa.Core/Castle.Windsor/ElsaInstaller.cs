using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Elsa.Builders;
using Elsa.Caching;
using Elsa.Options;
using Elsa.Persistence;
using Elsa.Providers.Workflows;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Messaging;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Rebus.DataBus.InMem;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoMapper;

namespace Elsa.Castle.Windsor
{
    public class AutoMapperInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            // Collect all AutoMapper profiles
            var profiles = Assembly.GetExecutingAssembly()
                                   .GetTypes()
                                   .Where(t => typeof(Profile).IsAssignableFrom(t))
                                   .ToList();

            var config = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles)
                {
                    cfg.AddProfile(profile);
                }
            });

            var mapper = config.CreateMapper();

            // Register IMapper
            container.Register(Component.For<IMapper>().Instance(mapper).LifestyleSingleton());
        }
    }


    public class ElsaInstaller : IWindsorInstaller
    {

        public ElsaInstaller()
        {
            ElsaOptions = new ElsaOptions();
            DistributedLockingOptionsBuilder = new DistributedLockingOptionsBuilder(this);
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            AddAutoMapper = () =>
            {
            };

            container.Register(Component.For<InMemNetwork>().LifestyleSingleton());
            container.Register(Component.For<InMemorySubscriberStore>().LifestyleSingleton());
            container.Register(Component.For<InMemDataStore>().LifestyleSingleton());
            // container.AddMemoryCache();
            container.Register(Component.For<ICacheSignal>().ImplementedBy<CacheSignal>().LifestyleSingleton());

        }

        public ElsaOptions ElsaOptions { get; }
        public IServiceCollection Services { get; }
        public DistributedLockingOptionsBuilder DistributedLockingOptionsBuilder { get; }
        internal Action AddAutoMapper { get; private set; }
        internal bool WithCoreActivities { get; set; } = true;

        public ElsaInstaller NoCoreActivities()
        {
            WithCoreActivities = false;
            return this;
        }

        public ElsaInstaller WithContainerName(string name)
        {
            ElsaOptions.ContainerName = name;
            return this;
        }

        public ElsaInstaller ConfigureWorkflowChannels(Action<WorkflowChannelOptions> configure)
        {
            ElsaOptions.WorkflowChannelOptions.Channels = new List<string>();
            configure(ElsaOptions.WorkflowChannelOptions);
            return this;
        }

        public ElsaInstaller AddActivity<T>() where T : IActivity => AddActivity(typeof(T));

        public ElsaInstaller AddActivity(Type activityType)
        {
            Services.AddTransient(activityType);
            Services.AddTransient(sp => (IActivity)sp.GetRequiredService(activityType));
            ElsaOptions.ActivityFactory.Add(activityType, provider => (IActivity)ActivatorUtilities.GetServiceOrCreateInstance(provider, activityType));
            return this;
        }

        public ElsaInstaller AddActivitiesFrom(Assembly assembly) => AddActivitiesFrom(new[] { assembly });
        public ElsaInstaller AddActivitiesFrom(params Assembly[] assemblies) => AddActivitiesFrom((IEnumerable<Assembly>)assemblies);
        public ElsaInstaller AddActivitiesFrom(params Type[] assemblyMarkerTypes) => AddActivitiesFrom(assemblyMarkerTypes.Select(x => x.Assembly).Distinct());
        public ElsaInstaller AddActivitiesFrom<TMarker>() where TMarker : class => AddActivitiesFrom(typeof(TMarker));

        public ElsaInstaller AddActivitiesFrom(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(x => x.GetAllWithInterface<IActivity>());

            foreach (var type in types)
                AddActivity(type);

            return this;
        }

        public ElsaInstaller RemoveActivity<T>() where T : IActivity => RemoveActivity(typeof(T));

        public ElsaInstaller RemoveActivity(Type activityType)
        {
            ElsaOptions.ActivityFactory.Remove(activityType);
            return this;
        }

        public ElsaInstaller AddWorkflow<T>() where T : IWorkflow => AddWorkflow(typeof(T));

        public ElsaInstaller AddWorkflow(Type workflowType)
        {
            var workflowFactory = ElsaOptions.WorkflowFactory;

            if (workflowFactory.Types.Contains(workflowType))
                return this;

            Services.AddSingleton(workflowType);
            Services.AddSingleton(sp => (IWorkflow)sp.GetRequiredService(workflowType));
            workflowFactory.Add(workflowType, provider => (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(provider, workflowType));
            return this;
        }

        public ElsaInstaller UseTenantSignaler()
        {
            ElsaOptions.UseTenantSignaler = true;
            return this;
        }

        public ElsaInstaller AddWorkflow(IWorkflow workflow)
        {
            Services.AddSingleton(workflow);
            ElsaOptions.WorkflowFactory.Add(workflow.GetType(), workflow);
            return this;
        }

        public ElsaInstaller AddWorkflow<T>(Func<IServiceProvider, T> workflow) where T : class, IWorkflow
        {
            Services.AddSingleton(workflow);
            Services.AddSingleton<IWorkflow>(sp => sp.GetRequiredService<T>());
            ElsaOptions.WorkflowFactory.Add(typeof(T), sp => sp.GetRequiredService<T>());

            return this;
        }

        public ElsaInstaller AddWorkflowsFrom<T>() => AddWorkflowsFrom(typeof(T).Assembly);

        public ElsaInstaller AddWorkflowsFrom(Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IWorkflow>();

            foreach (var type in types)
                AddWorkflow(type);

            return this;
        }

        public ElsaInstaller RemoveWorkflow<T>() where T : IWorkflow => RemoveWorkflow(typeof(T));

        public ElsaInstaller RemoveWorkflow(Type workflowType)
        {
            ElsaOptions.WorkflowFactory.Remove(workflowType);
            return this;
        }

        public ElsaInstaller AddCompetingMessageType(Type messageType, string? queueName = default)
        {
            queueName ??= messageType.Name;
            ElsaOptions.CompetingMessageTypes.Add(new MessageTypeConfig(messageType, queueName));
            return this;
        }

        public ElsaInstaller AddCompetingMessageType<T>(string? queueName = default) => AddCompetingMessageType(typeof(T), queueName);

        public ElsaInstaller AddPubSubMessageType(Type messageType, string? queueName = default)
        {
            queueName ??= messageType.Name;

            ElsaOptions.PubSubMessageTypes.Add(new MessageTypeConfig(messageType, queueName));
            return this;
        }

        public ElsaInstaller AddPubSubMessageType<T>(string? queueName = default) => AddPubSubMessageType(typeof(T), queueName);

        public ElsaInstaller ConfigureDistributedLockProvider(Action<DistributedLockingOptionsBuilder> configureOptions)
        {
            configureOptions(DistributedLockingOptionsBuilder);
            return this;
        }

        public ElsaInstaller UseWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
        {
            ElsaOptions.WorkflowDefinitionStoreFactory = factory;
            return this;
        }

        public ElsaInstaller UseWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
        {
            ElsaOptions.WorkflowInstanceStoreFactory = factory;
            return this;
        }

        public ElsaInstaller UseWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
        {
            ElsaOptions.WorkflowExecutionLogStoreFactory = factory;
            return this;
        }

        public ElsaInstaller UseBookmarkStore(Func<IServiceProvider, IBookmarkStore> factory)
        {
            ElsaOptions.BookmarkStoreFactory = factory;
            return this;
        }

        public ElsaInstaller UseTriggerStore(Func<IServiceProvider, ITriggerStore> factory)
        {
            ElsaOptions.TriggerStoreFactory = factory;
            return this;
        }

        public ElsaInstaller UseAutoMapper(Action addAutoMapper)
        {
            AddAutoMapper = addAutoMapper;
            return this;
        }

        public ElsaInstaller UseJsonSerializer(Func<IServiceProvider, JsonSerializer> factory)
        {
            ElsaOptions.CreateJsonSerializer = factory;
            return this;
        }

        public ElsaInstaller ConfigureJsonSerializer(Action<IServiceProvider, JsonSerializer> configure)
        {
            ElsaOptions.JsonSerializerConfigurer = configure;
            return this;
        }

        public ElsaInstaller UseDefaultWorkflowStorageProvider<T>() where T : IWorkflowStorageProvider => UseDefaultWorkflowStorageProvider(typeof(T));

        public ElsaInstaller UseDefaultWorkflowStorageProvider(Type type)
        {
            ElsaOptions.DefaultWorkflowStorageProviderType = type;
            return this;
        }

        public ElsaInstaller UseServiceBus(Action<ServiceBusEndpointConfigurationContext> setup)
        {
            ElsaOptions.ConfigureServiceBusEndpoint = setup;
            return this;
        }

        public ElsaInstaller AddCustomTenantAccessor<T>() where T : class, ITenantAccessor
        {
            Services.AddScoped<ITenantAccessor, T>();
            return this;
        }

        public ElsaInstaller ExcludeWorkflowProviderFromStartupIndexing<T>() where T : IWorkflowProvider
        {
            ElsaOptions.WorkflowTriggerIndexingOptions.ExcludedProviders.Add(typeof(T));
            return this;
        }

    }
}
