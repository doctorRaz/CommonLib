// Сбор сведений об объявлениях команд CAD.
using dRz.Abstractions.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#if NC

using Rtm = Teigha.Runtime;

#elif AC
using Autodesk.AutoCAD.Runtime;
#endif

namespace dRz.Common.NC.CadServices.Diagnostics
{
    /// <summary>
    /// Собирает объявления команд CAD и предоставляет выборки для диагностических отчётов.
    /// </summary>
    public sealed class CmdInfo
    {
        private readonly IReadOnlyCollection<Assembly> _assemblies;
        private readonly IDrzLogger? _logger;

        /// <summary>
        /// Все найденные объявления команд. Для каждого CommandMethodAttribute создаётся отдельная запись.
        /// </summary>
        public List<CommandEntry> Commands { get; } = new();

        /// <summary>Объявления команд, глобальное имя которых встречается ровно один раз.</summary>
        public IEnumerable<CommandEntry> RegisteredCommands =>
            Commands
                .GroupBy(command => command.CommandName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() == 1)
                .SelectMany(group => group);

        /// <summary>
        /// Группы объявлений с повторяющимся глобальным именем команды.
        /// Каждая группа содержит все методы, объявившие это имя.
        /// </summary>
        public IEnumerable<IGrouping<string, CommandEntry>> DuplicateCommands =>
            Commands
                .GroupBy(command => command.CommandName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

        /// <summary>Описание одного объявления команды.</summary>
        public sealed class CommandEntry
        {
            /// <summary>Глобальное имя команды из CommandMethodAttribute.</summary>
            public string CommandName { get; init; } = string.Empty;

            /// <summary>Полное имя типа и имя объявившего команду метода.</summary>
            public string MethodName { get; init; } = string.Empty;

            /// <summary>Описание метода; пустая строка, если DescriptionAttribute отсутствует.</summary>
            public string Description { get; init; } = string.Empty;
        }

        /// <summary>Создаёт сведения по сборке, содержащей этот класс.</summary>
        public CmdInfo(IDrzLogger? logger = null)
            : this(new[] { Assembly.GetExecutingAssembly() }, logger)
        {
        }

        /// <summary>Собирает объявления команд из указанной сборки.</summary>
        /// <param name="assembly">Сборка для анализа.</param>
        /// <param name="logger">Логгер ошибок рефлексии.</param>
        public CmdInfo(Assembly assembly, IDrzLogger? logger = null)
            : this(new[] { assembly ?? throw new ArgumentNullException(nameof(assembly)) }, logger)
        {
        }

        /// <summary>Собирает объявления команд из набора сборок.</summary>
        /// <param name="assemblies">Сборки для анализа.</param>
        /// <param name="logger">Логгер ошибок рефлексии.</param>
        public CmdInfo(IEnumerable<Assembly> assemblies, IDrzLogger? logger = null)
        {
            ArgumentNullException.ThrowIfNull(assemblies);

            _assemblies = assemblies
                .Select(assembly => assembly ?? throw new ArgumentException(
                    "Набор сборок не должен содержать null.", nameof(assemblies)))
                .Distinct()
                .ToArray();
            _logger = logger;

            CollectCommands();
        }

        /// <summary>
        /// Собирает объявления команд из всех переданных сборок.
        /// Ошибки отдельной сборки, типа или метода не прерывают весь анализ.
        /// </summary>
        private void CollectCommands()
        {
            foreach (Assembly assembly in _assemblies)
            {
                CollectCommands(assembly);
            }
        }

        /// <summary>
        /// Собирает объявления команд одной сборки.
        /// </summary>
        private void CollectCommands(Assembly assembly)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Используем доступные типы, даже если часть типов сборки не загрузилась.
                List<Type> availableTypes = new List<Type>(ex.Types.Length);
                foreach (Type? type in ex.Types)
                {
                    if (type != null)
                    {
                        availableTypes.Add(type);
                    }
                }

                types = availableTypes.ToArray();
                LogError(ex, $"Не все типы сборки '{assembly.FullName}' удалось загрузить.");
            }
            catch (Exception ex)
            {
                LogError(ex, $"Не удалось получить типы сборки '{assembly.FullName}'.");
                return;
            }

            foreach (Type type in types)
            {
                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods();
                }
                catch (Exception ex)
                {
                    LogError(ex, $"Не удалось получить методы типа '{type.FullName}'.");
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    CollectMethodCommands(method);
                }
            }
        }

        /// <summary>Добавляет отдельную запись для каждого CommandMethodAttribute метода.</summary>
        private void CollectMethodCommands(MethodInfo method)
        {
            object[] attributes;
            try
            {
                attributes = method.GetCustomAttributes(true);
            }
            catch (Exception ex)
            {
                LogError(ex, $"Не удалось прочитать атрибуты метода '{GetMethodName(method)}'.");
                return;
            }

            string description = string.Empty;
            foreach (object attribute in attributes)
            {
                if (attribute is DescriptionAttribute descriptionAttribute)
                {
                    description = descriptionAttribute.Description ?? string.Empty;
                    break;
                }
            }

            string methodName = GetMethodName(method);
            foreach (object attribute in attributes)
            {
                if (attribute is not Rtm.CommandMethodAttribute commandAttribute)
                {
                    continue;
                }

                string commandName = commandAttribute.GlobalName;
                if (string.IsNullOrWhiteSpace(commandName))
                {
                    LogError(new InvalidOperationException(
                        $"CommandMethodAttribute без глобального имени у метода '{methodName}'."), null);
                    continue;
                }

                Commands.Add(new CommandEntry
                {
                    CommandName = commandName,
                    MethodName = methodName,
                    Description = description
                });
            }
        }

        private static string GetMethodName(MethodInfo method)
        {
            return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
        }

        /// <summary>Записывает ошибку рефлексии, не позволяя сбою логгера прервать анализ.</summary>
        private void LogError(Exception exception, string? message)
        {
            try
            {
                _logger?.Error(exception, message);
            }
            catch
            {
                // Диагностика не должна влиять на инициализацию аддона или вывод отчёта.
            }
        }
    }
}
