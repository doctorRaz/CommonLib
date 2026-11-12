using dRz.Common.NC.CadServices;
using dRz.Common.NC.Services.LogLevel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using App = HostMgd.ApplicationServices;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// Получает выбор пользователя из командной строки NanoCAD.
    /// </summary>
    internal sealed class NanoCadLogLevelPrompt : ILogLevelPrompt
    {
        private readonly App.Document? _document;

        public NanoCadLogLevelPrompt(App.Document? document)
        {
            _document = document;
        }

        public LogLevelAction? ChooseAction(string settingName)
        {
            if (_document == null)
            {
                return null;
            }

            List<KeywordOption> options = new List<KeywordOption>
            {
                new KeywordOption(nameof(LogLevelAction.Apply), LogLevelAction.Apply),
                new KeywordOption(nameof(LogLevelAction.Delete), LogLevelAction.Delete),
                new KeywordOption(nameof(LogLevelAction.Status), LogLevelAction.Status)
            };

            Enum? answer = KeywordPrompt.GetAnswer(
                _document,
                options,
                $"\nДействие для {settingName}: ",
                allowNone: true);

            return answer is LogLevelAction action ? action : null;
        }

        public LogLevelValue? ChooseLevel(string settingName)
        {
            if (_document == null)
            {
                return null;
            }

            List<KeywordOption> options = new List<KeywordOption>
            {
                new KeywordOption(nameof(LogLevelValue.Trace), LogLevelValue.Trace),
                new KeywordOption(nameof(LogLevelValue.Debug), LogLevelValue.Debug),
                new KeywordOption(nameof(LogLevelValue.Info), LogLevelValue.Info),
                new KeywordOption(nameof(LogLevelValue.Warn), LogLevelValue.Warn),
                new KeywordOption(nameof(LogLevelValue.Error), LogLevelValue.Error),
                new KeywordOption(nameof(LogLevelValue.Fatal), LogLevelValue.Fatal),
                new KeywordOption(nameof(LogLevelValue.Off), LogLevelValue.Off)
            };

            Enum? answer = KeywordPrompt.GetAnswer(
                _document,
                options,
                $"\nНовый уровень для {settingName}: ",
                allowNone: true);

            return answer is LogLevelValue level ? level : null;
        }
    }
}