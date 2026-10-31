using dRz.Abstractions.Infrastructure;
using dRz.Abstractions.Logger;
using dRz.LogBootstrap.Builder;
using dRz.LogBootstrap.drzNLog;
using NLog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace dRz.LogBootstrap
{
    /// <summary>
    /// найти или создать IDrzLoggerFactory
    /// </summary>
    public class NLogBootstrap
    {
        /// <summary>Gets the logger factory.</summary>
        /// <param name="addOnInfo">The add on information.</param>
        /// <returns></returns>
        public static IDrzLoggerFactory GetLoggerFactory(IAddOnInfo addOnInfo)
        {
            Lazy<IDrzLoggerFactory> lazyFactory = _factories.GetOrAdd(
                addOnInfo.Product,
                _ => new Lazy<IDrzLoggerFactory>(
                    () =>
                    {
                        NLogFactoryBuilder builder = new(addOnInfo.AssemblyDirectory,
                                                        addOnInfo.Product,
                                                        addOnInfo.ProductFamily,
                                                        Path.Combine(addOnInfo.ProductDataDirectory, "logs"),
                                                        Path.Combine(addOnInfo.CompanyDataDirectory, "internallogs")
                                                        );

                        LogFactory logFactory = builder.Build();

                        return new NLogLoggerFactory(logFactory);
                    },
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return lazyFactory.Value;
        }

        private static readonly ConcurrentDictionary<string, Lazy<IDrzLoggerFactory>> _factories = new();
    }
}