using System;
using System.Reflection;

namespace drz.MulticadInterop.McParamManager
{
    /// <summary>
    /// Предоставляет reflection-доступ к McParamManager
    /// без compile-time зависимости от конкретной версии Multicad.
    /// </summary>
    public static class MulticadParamManagerReflection
    {
        private static readonly Type ParamManagerType =
            MulticadReflection.FindType(
                "Multicad.ApplicationServices.McParamManager",
                "Multicad.AplicationServices.McParamManager");

        /// <summary>
        /// Возвращает все методы McParamManager.
        /// </summary>
        /// <returns>Массив методов.</returns>
        public static MethodInfo[] GetMethods()
        {
            return ParamManagerType?.GetMethods() ?? Array.Empty<MethodInfo>();
        }

        /// <summary>
        /// Возвращает методы McParamManager с указанным именем.
        /// </summary>
        /// <param name="methodName">Имя метода.</param>
        /// <returns>Массив найденных методов.</returns>
        public static MethodInfo[] GetMethods(string methodName)
        {
            return MulticadReflection.GetMethods(ParamManagerType, methodName);
        }

        /// <summary>
        /// Находит метод McParamManager по имени и точной сигнатуре.
        /// </summary>
        /// <param name="methodName">Имя метода.</param>
        /// <param name="parameterTypes">Ожидаемые типы параметров.</param>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindMethod(string methodName, params Type[] parameterTypes)
        {
            return MulticadReflection.FindMethod(
                ParamManagerType, methodName, parameterTypes);
        }

        /// <summary>
        /// Находит перегрузку CallOptions с владельцем окна типа Form
        /// без compile-time зависимости от System.Windows.Forms.
        /// </summary>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindCallOptionsForm()
        {
            MethodInfo[] methods = GetMethods("CallOptions");

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType.FullName == "System.Windows.Forms.Form")
                {
                    return method;
                }
            }

            return null!;
        }

        /// <summary>
        /// Находит перегрузку SetParam(object, int).
        /// </summary>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindSetParam()
        {
            return FindMethod("SetParam", typeof(object), typeof(int));
        }

        /// <summary>
        /// Находит перегрузку SetParam(object, int, Standarts).
        /// </summary>
        /// <returns>Найденный метод или null, если метод не найден.</returns>
        public static MethodInfo FindSetParamWithStandarts()
        {
            MethodInfo[] methods = GetMethods("SetParam");

            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(object) &&
                    parameters[1].ParameterType == typeof(int) &&
                    parameters[2].ParameterType.IsEnum &&
                    parameters[2].ParameterType.Name == "Standarts")
                {
                    return method;
                }
            }

            return null!;
        }
    }
}