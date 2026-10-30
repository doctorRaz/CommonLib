using System;

namespace dRz.Abstractions.Services
{
    /// <summary>
    /// Предоставляет доступ к сервисам, зарегистрированным в контейнере аддона.
    /// </summary>
    /// <remarks>
    /// Интерфейс предназначен для передачи между сборками.
    /// Сборки не должны иметь зависимость от SimpleInjector и не должны
    /// получать доступ к контейнеру напрямую.
    ///
    /// Обычно экземпляр интерфейса передается один раз при инициализации
    /// библиотеки или через конструктор класса.
    /// </remarks>
    public interface IAddOnServices
    {
        /// <summary>
        /// Возвращает зарегистрированный сервис указанного типа.
        /// </summary>
        /// <typeparam name="TService">
        /// Тип требуемого сервиса.
        /// </typeparam>
        /// <returns>
        /// Экземпляр зарегистрированного сервиса.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Сервис данного типа отсутствует в контейнере, не зарегистрирован.
        /// </exception>
        TService Get<TService>() where TService : class;
    }
}