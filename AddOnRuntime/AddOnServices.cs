using dRz.Abstractions.Services;
using SimpleInjector;

namespace dRz.AddOnRuntime
{
    /// <summary>
    /// Реализация <see cref="IAddOnServices"/>,
    /// предоставляющая доступ к сервисам контейнера.
    /// </summary>
    internal sealed class AddOnServices : IAddOnServices
    {
        private readonly Container _container;

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="container">
        /// Контейнер SimpleInjector.
        /// </param>
        public AddOnServices(Container container)
        {
            _container = container;
        }

        /// <summary>
        /// Возвращает зарегистрированный сервис указанного типа.
        /// </summary>
        /// <typeparam name="TService">
        /// Тип требуемого сервиса.
        /// </typeparam>
        /// <returns>
        /// Экземпляр зарегистрированного сервиса.
        /// </returns>
        /// <exception cref="ActivationException">
        /// Сервис данного типа отсутствует в контейнере.
        /// </exception>
        public TService Get<TService>() where TService : class
        {
            return _container.GetInstance<TService>();
        }
    }
}