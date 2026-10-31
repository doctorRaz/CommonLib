using HostMgd.ApplicationServices;
using HostMgd.EditorInput;
using System;
using System.Collections.Generic;

namespace dRz.Common.NC.CadServices
{
    /// <summary>
    /// Предоставляет выбор ключевого слова в командной строке CAD.
    /// </summary>
    public static class KeywordPrompt
    {
        /// <summary>
        /// Показывает список ключевых слов и возвращает связанное с выбранным словом значение.
        /// </summary>
        /// <param name="doc">Документ, в котором выполняется команда.</param>
        /// <param name="optionsList">Варианты выбора.</param>
        /// <param name="message">Текст запроса.</param>
        /// <param name="allowNone">Разрешает принять значение по умолчанию при нажатии Enter.</param>
        /// <returns>Значение выбранного ключевого слова или <see langword="null"/>, если ввод отменён или не распознан.</returns>
        public static Enum? GetAnswer(
            Document doc,
            IReadOnlyList<KeywordOption> optionsList,
            string message,
            bool allowNone = false)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (optionsList == null)
            {
                throw new ArgumentNullException(nameof(optionsList));
            }

            PromptKeywordOptions options = new PromptKeywordOptions(message)
            {
                AllowNone = allowNone,
                AppendKeywordsToMessage = true
            };

            foreach (KeywordOption keyword in optionsList)
            {
                options.Keywords.Add(
                    keyword.GlobalName,
                    keyword.LocalName,
                    keyword.DisplayName,
                    keyword.IsVisible,
                    keyword.IsEnabled);

                if (keyword.IsDefault)
                {
                    options.Keywords.Default = keyword.GlobalName;
                }
            }

            PromptResult result = doc.Editor.GetKeywords(options);

            if (result.Status == PromptStatus.None && allowNone)
            {
                foreach (KeywordOption keyword in optionsList)
                {
                    if (keyword.IsDefault)
                    {
                        return keyword.Answer;
                    }
                }

                return null;
            }

            if (result.Status != PromptStatus.OK)
            {
                return null;
            }

            foreach (KeywordOption keyword in optionsList)
            {
                if (string.Equals(
                    keyword.GlobalName,
                    result.StringResult,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return keyword.Answer;
                }
            }

            return null;
        }
    }
}