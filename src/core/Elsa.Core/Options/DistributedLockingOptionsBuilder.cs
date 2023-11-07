using System;
using Elsa.Castle.Windsor;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Options
{
    public class DistributedLockingOptionsBuilder
    {
        public DistributedLockingOptionsBuilder(ElsaInstaller elsaOptionsBuilder) => ElsaInstaller = elsaOptionsBuilder;
        public DistributedLockingOptionsBuilder(ElsaOptionsBuilder elsaOptionsBuilder) => ElsaOptionsBuilder = elsaOptionsBuilder;

        public ElsaInstaller? ElsaInstaller { get; }
        public ElsaOptionsBuilder? ElsaOptionsBuilder { get; }
        public IServiceCollection? Services => ElsaOptionsBuilder?.Services;

        public DistributedLockingOptionsBuilder UseProviderFactory(Func<IServiceProvider, Func<string, IDistributedLock>> factory)
        {
            if (ElsaOptionsBuilder is not null)
            {
                ElsaOptionsBuilder.ElsaOptions.DistributedLockingOptions.DistributedLockProviderFactory = factory;
            }
            return this;
        }
    }
}