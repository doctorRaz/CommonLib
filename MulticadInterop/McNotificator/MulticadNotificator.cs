using System;
using System.Collections.Generic;
using System.Reflection;

namespace drz.MulticadInterop.McNotificator
{
    /// <summary>
    /// Предоставляет доступ к McNotificator без compile-time зависимости
    /// от конкретной версии Multicad.
    /// </summary>
    public static class MulticadNotificator
    {
        private static readonly MethodInfo CreateMessageMethod = MulticadNotificatorReflection.FindCreateMessage();

        private static readonly Dictionary<NotificationType, MethodInfo>
            CreateMessageMethods = FindCreateMessageMethods();

        private static readonly Dictionary<NotificationType, MethodInfo>
            CreateMessageWithParentMethods = FindCreateMessageWithParentMethods();

        /// <summary>
        /// Создаёт обычное уведомление.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <returns>Идентификатор созданного уведомления.</returns>
        public static UIntPtr CreateMessage(string message)
        {
            if (CreateMessageMethod == null)
            {
                throw new InvalidOperationException(
                    "McNotificator.CreateMessage не найден");
            }

            return (UIntPtr)CreateMessageMethod.Invoke(
                null,
                new object[] { message });
        }

        /// <summary>
        /// Создаёт уведомление указанного типа.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="type">Тип уведомления.</param>
        /// <returns>Идентификатор созданного уведомления.</returns>
        public static UIntPtr CreateMessage(string message, NotificationType type)
        {
            if (!CreateMessageMethods.TryGetValue(type, out MethodInfo method))
            {
                throw new InvalidOperationException(
                    "McNotificator.CreateMessage с типом уведомления не найден");
            }

            Type enumType = method.GetParameters()[1].ParameterType;
            object enumValue = Enum.ToObject(enumType, (int)type);

            return (UIntPtr)method.Invoke(
                null,
                new object[] { message, enumValue });
        }

        /// <summary>
        /// Создаёт уведомление указанного типа с идентификатором родительского уведомления.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="type">Тип уведомления.</param>
        /// <param name="parentId">Идентификатор родительского уведомления.</param>
        /// <returns>Идентификатор созданного уведомления.</returns>
        public static UIntPtr CreateMessage(
            string message,
            NotificationType type,
            UIntPtr parentId)
        {
            if (!CreateMessageWithParentMethods.TryGetValue(
                    type,
                    out MethodInfo method))
            {
                throw new InvalidOperationException(
                    "McNotificator.CreateMessage с parentId не найден");
            }

            Type enumType = method.GetParameters()[1].ParameterType;
            object enumValue = Enum.ToObject(enumType, (int)type);

            return (UIntPtr)method.Invoke(
                null,
                new object[] { message, enumValue, parentId });
        }

        /// <summary>
        /// Кэширует перегрузки CreateMessage для поддерживаемых типов уведомлений.
        /// </summary>
        private static Dictionary<NotificationType, MethodInfo> FindCreateMessageMethods()
        {
            Dictionary<NotificationType, MethodInfo> result =
                new Dictionary<NotificationType, MethodInfo>();

            foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
            {
                MethodInfo method =
                    MulticadNotificatorReflection.FindCreateMessage(type);

                if (method != null)
                {
                    result[type] = method;
                }
            }

            return result;
        }

        /// <summary>
        /// Кэширует перегрузки CreateMessage с parentId для поддерживаемых типов уведомлений.
        /// </summary>
        private static Dictionary<NotificationType, MethodInfo>
            FindCreateMessageWithParentMethods()
        {
            Dictionary<NotificationType, MethodInfo> result =
                new Dictionary<NotificationType, MethodInfo>();

            foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
            {
                MethodInfo method =
                    MulticadNotificatorReflection.FindCreateMessage(type, true);

                if (method != null)
                {
                    result[type] = method;
                }
            }

            return result;
        }
    }
}