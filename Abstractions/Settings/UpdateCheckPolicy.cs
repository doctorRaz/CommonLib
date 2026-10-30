namespace dRz.Abstractions.Settings
{
    /// <summary>
    /// Содержит правила нормализации интервала автоматической проверки обновлений.
    /// </summary>
    public static class UpdateCheckPolicy
    {
        /// <summary>
        /// Ограничивает интервал проверки обновлений допустимым диапазоном от 1 до 30 дней.
        /// </summary>
        /// <param name="intervalDays">Заданный интервал в днях.</param>
        /// <returns>Интервал в диапазоне от 1 до 30 дней.</returns>
        public static int NormalizeIntervalDays(int intervalDays)
        {
            if (intervalDays < 1)
            {
                return 1;
            }

            if (intervalDays > 30)
            {
                return 30;
            }

            return intervalDays;
        }
    }
}