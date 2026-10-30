using System;
using System.Collections.Generic;
using System.Reflection;

namespace drz.MulticadInterop
{
    /// <summary>
    /// Общие reflection-утилиты для доступа к API Multicad.
    /// </summary>
    internal static class MulticadReflection
    {
        /// <summary>
        /// Находит указанный тип в загруженных сборках.
        /// </summary>
        /// <param name="typeNames">Полные имена типа, допустимые для разных версий Multicad.</param>
        /// <returns>Найденный тип или null, если тип не найден.</returns>
        public static Type FindType(params string[] typeNames)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (string typeName in typeNames)
                    {
                        Type type = assembly.GetType(typeName, false);

                        if (type != null)
                        {
                            return type;
                        }
                    }
                }
                catch
                {
                    // Нативные и смешанные сборки могут не поддерживать GetType.
                }
            }

            return null!;
        }

        /// <summary>
        /// Возвращает методы указанного типа с заданным именем.
        /// </summary>
        /// <param name="type">Тип, в котором выполняется поиск.</param>
        /// <param name="methodName">Имя метода.</param>
        /// <returns>Массив найденных методов.</returns>
        public static MethodInfo[] GetMethods(Type type, string methodName)
        {
            if (type == null)
            {
                return Array.Empty<MethodInfo>();
            }

            List<MethodInfo> result = new List<MethodInfo>();

            foreach (MethodInfo method in type.GetMethods())
            {
                if (method.Name == methodName)
                {
                    result.Add(method);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Находит метод по имени и точному набору типов параметров.
        /// </summary>
        /// <param name="type">Тип, в котором выполняется поиск.</param>
        /// <param name="methodName">Имя метода.</param>
        /// <param name="parameterTypes">Ожидаемые типы параметров.</param>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindMethod(
            Type type,
            string methodName,
            params Type[] parameterTypes)
        {
            MethodInfo[] methods = GetMethods(type, methodName);

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                bool match = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return method;
                }
            }

            return null!;
        }
    }
}