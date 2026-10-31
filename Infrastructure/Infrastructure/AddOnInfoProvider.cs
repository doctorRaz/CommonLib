using dRz.Abstractions.Infrastructure;
using System;
using System.Reflection;

namespace dRz.Infrastructure.Infrastructure
{
    /// <summary>
    /// Регистрация и получение сведений о сборках аддонов.
    /// </summary>
    public static class AddOnInfoProvider
    {
        /// <summary>Получает сведения о сборке, содержащей тип <typeparamref name="T"/>, или создаёт их.</summary>
        /// <typeparam name="T">Тип, сборка которого используется для поиска.</typeparam>
        /// <returns>Сведения о сборке аддона.</returns>
        public static IAddOnInfo GetOrAdd<T>() => GetOrAdd(typeof(T).Assembly);

        /// <summary>Получает сведения о сборке, содержащей указанный тип, или создаёт их.</summary>
        /// <param name="type">Тип, сборка которого используется для поиска.</param>
        /// <returns>Сведения о сборке аддона.</returns>
        public static IAddOnInfo GetOrAdd(Type type) => GetOrAdd(type.Assembly);

        /// <summary>Получает сведения об указанной сборке или создаёт их.</summary>
        /// <param name="assembly">Сборка, для которой требуются сведения.</param>
        /// <returns>Сведения о сборке аддона.</returns>
        public static IAddOnInfo GetOrAdd(Assembly assembly)
        {
            //обращаемся к классу AddOnInfoRegistry напрямую, методы GetOrAdd будут удалены из IAddOnInfoRegistry
            AddOnInfoRegistry addOnInfoRegistry = new AddOnInfoRegistry();

            return addOnInfoRegistry.GetOrAdd(assembly);
        }
    }
}