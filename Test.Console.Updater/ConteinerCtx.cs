using dRz.Abstractions.Services;
using dRz.AddOnRuntime;
using dRz.Common.NC.Infrastructure;
using System;

namespace dRz.Test.Console.Updater
{
    /// <summary>
    /// Создаёт composition root консольного теста.
    /// Временная инициализация AddOnCtx нужна для существующего UpdateAsync,
    /// который пока получает зависимости через статический контекст.
    /// </summary>
    internal static class ConteinerCtx
    {
        private static AddOnCompositionRoot? _root;

        public static AddOnCompositionRoot Run()
        {
            if (_root != null)
            {
                return _root;
            }

            try
            {
                AddOnCompositionRoot root = new AddOnCompositionRoot(typeof(ConteinerCtx).Assembly);

                // Временный адаптер для существующего UpdateAsync.
                AddOnContext.Initialize(root.Get<IAddOnServices>());

                _root = root;
                return root;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AddOnCompositionRoot initialization failed", ex);
            }
        }
    }
}