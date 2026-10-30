using dRz.Abstractions.Settings;
using NUnit.Framework;

namespace dRz.Test.Settings
{
    /// <summary>
    /// Проверяет нормализацию интервала проверки обновлений.
    /// </summary>
    [TestFixture]
    public sealed class UpdateCheckPolicyTests
    {
        /// <summary>
        /// Проверяет ограничение интервала диапазоном от одного до тридцати дней.
        /// </summary>
        /// <param name="configured">Настроенное количество дней.</param>
        /// <param name="expected">Ожидаемое нормализованное количество дней.</param>
        [TestCase(-100, 1)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(7, 7)]
        [TestCase(30, 30)]
        [TestCase(31, 30)]
        [TestCase(10000000, 30)]
        public void NormalizeIntervalDays_ClampsToOneThroughThirty(int configured, int expected)
        {
            Assert.That(UpdateCheckPolicy.NormalizeIntervalDays(configured), Is.EqualTo(expected));
        }
    }
}