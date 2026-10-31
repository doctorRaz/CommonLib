using System;
using System.Reflection;

namespace drz.MulticadInterop.McNotificator
{
    /// <summary>
    /// Тип уведомления Multicad.
    /// Числовые значения соответствуют NotificationEnumMgd.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>Без иконки.</summary>
        neSimple = -1,

        /// <summary>Стандартное уведомление.</summary>
        neUsual = 0,

        /// <summary>Предупреждение.</summary>
        neWarning = 1,

        /// <summary>Ошибка в виде крестика.</summary>
        neCure = 2,

        /// <summary>Подсказка.</summary>
        neHint = 3,

        /// <summary>Ошибка.</summary>
        neError = 4,

        /// <summary>Справочная информация.</summary>
        neHelp = 5
    }

    /// <summary>
    /// Предоставляет reflection-доступ к перегрузкам McNotificator.CreateMessage
    /// без compile-time зависимости от конкретной версии Multicad.
    /// </summary>
    public static class MulticadNotificatorReflection
    {
        private static readonly Type NotificatorType =
            MulticadReflection.FindType(
                "Multicad.ApplicationServices.McNotificator",
                "Multicad.AplicationServices.McNotificator");

        /// <summary>Возвращает все перегрузки CreateMessage.</summary>
        /// <returns>Массив найденных методов.</returns>
        public static MethodInfo[] GetCreateMessageMethods()
        {
            return MulticadReflection.GetMethods(
                NotificatorType, "CreateMessage");
        }

        /// <summary>Находит перегрузку CreateMessage(string).</summary>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindCreateMessage()
        {
            return MulticadReflection.FindMethod(
                NotificatorType, "CreateMessage", typeof(string));
        }

        /// <summary>Находит перегрузку CreateMessage с указанным типом уведомления.</summary>
        /// <param name="type">Тип уведомления.</param>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindCreateMessage(NotificationType type)
        {
            return FindCreateMessage(type, false);
        }

        /// <summary>Находит перегрузку CreateMessage с указанным типом уведомления и, при необходимости, parentId.</summary>
        /// <param name="type">Тип уведомления.</param>
        /// <param name="withParentId">Признак поиска перегрузки с идентификатором родительского уведомления.</param>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindCreateMessage(NotificationType type, bool withParentId)
        {
            MethodInfo[] methods = GetCreateMessageMethods();

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();
                int expectedCount = withParentId ? 3 : 2;

                if (parameters.Length != expectedCount ||
                    parameters[0].ParameterType != typeof(string) ||
                    !parameters[1].ParameterType.IsEnum)
                {
                    continue;
                }

                if (withParentId && parameters[2].ParameterType != typeof(UIntPtr))
                {
                    continue;
                }

                object enumValue;

                try
                {
                    enumValue = Enum.ToObject(parameters[1].ParameterType, (int)type);
                }
                catch
                {
                    continue;
                }

                if (Convert.ToInt32(enumValue) == (int)type)
                {
                    return method;
                }
            }

            return null!;
        }
    }
}