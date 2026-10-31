using System;
using System.Drawing;
using System.Reflection;

namespace drz.MulticadInterop.McParamManager
{
    /// <summary>
    /// Предоставляет API-обёртку над McParamManager
    /// без compile-time зависимости от конкретной версии Multicad.
    /// </summary>
    public static class MulticadParamManager
    {
        private static readonly MethodInfo CallOptionsFormMethod =
            MulticadParamManagerReflection.FindCallOptionsForm();

        private static readonly MethodInfo CallOptionsHandleMethod =
            MulticadParamManagerReflection.FindMethod(
                "CallOptions", typeof(string), typeof(IntPtr));

        private static readonly MethodInfo GetBoolParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetBoolParam", typeof(int));

        private static readonly MethodInfo GetColorParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetColorParam", typeof(int));

        private static readonly MethodInfo GetDoubleParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetDoubleParam", typeof(int));

        private static readonly MethodInfo GetInt32ParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetInt32Param", typeof(int));

        private static readonly MethodInfo GetInt64ParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetInt64Param", typeof(int));

        private static readonly MethodInfo GetProfiledLayerNameMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetProfiledLayerName", typeof(string));

        private static readonly MethodInfo GetStringParamMethod =
            MulticadParamManagerReflection.FindMethod(
                "GetStringParam", typeof(int));

        private static readonly MethodInfo SetParamMethod =
            MulticadParamManagerReflection.FindSetParam();

        private static readonly MethodInfo SetParamWithStandartsMethod =
            MulticadParamManagerReflection.FindSetParamWithStandarts();

        /// <summary>
        /// Вызов диалога настроек с владельцем окна в виде объекта Form.
        /// </summary>
        /// <param name="helpIndexName">Имя раздела справки настроек.</param>
        /// <param name="sender">Окно-владелец диалога.</param>
        public static void CallOptions(string helpIndexName, object sender)
        {
            InvokeVoid(CallOptionsFormMethod, helpIndexName, sender);
        }

        /// <summary>
        /// Вызов диалога настроек по дескриптору окна.
        /// </summary>
        /// <param name="helpIndexName">Имя раздела справки настроек.</param>
        /// <param name="handle">Дескриптор окна-владельца диалога.</param>
        public static void CallOptions(string helpIndexName, IntPtr handle)
        {
            InvokeVoid(CallOptionsHandleMethod, helpIndexName, handle);
        }

        /// <summary>
        /// Получает логический параметр Multicad.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static bool GetBoolParam(int idParam)
        {
            return (bool)Invoke(GetBoolParamMethod, idParam);
        }

        /// <summary>
        /// Получает параметр Multicad типа Color.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение цвета.</returns>
        public static Color GetColorParam(int idParam)
        {
            return (Color)Invoke(GetColorParamMethod, idParam);
        }

        /// <summary>
        /// Получает параметр Multicad типа double.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static double GetDoubleParam(int idParam)
        {
            return (double)Invoke(GetDoubleParamMethod, idParam);
        }

        /// <summary>
        /// Получение параметра с автоматическим приведением типа.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static int GetInt32Param(int idParam)
        {
            return (int)Invoke(GetInt32ParamMethod, idParam);
        }

        /// <summary>
        /// Получает параметр Multicad типа long.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static long GetInt64Param(int idParam)
        {
            return (long)Invoke(GetInt64ParamMethod, idParam);
        }

        /// <summary>
        /// Получение имени слоя, начинающегося с префикса текущего профиля.
        /// Если переданное имя слоя уже начинается с одного из префиксов, оно не изменяется.
        /// </summary>
        /// <param name="layerName">Имя слоя.</param>
        /// <returns>Имя слоя с префиксом текущего профиля.</returns>
        public static string GetProfiledLayerName(string layerName)
        {
            return (string)Invoke(GetProfiledLayerNameMethod, layerName);
        }

        /// <summary>
        /// Получает строковый параметр Multicad.
        /// </summary>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>Значение параметра.</returns>
        public static string GetStringParam(int idParam)
        {
            return (string)Invoke(GetStringParamMethod, idParam);
        }

        /// <summary>
        /// Устанавливает параметр Multicad.
        /// </summary>
        /// <param name="param">Новое значение параметра.</param>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <returns>true, если параметр установлен успешно.</returns>
        public static bool SetParam(object param, int idParam)
        {
            return (bool)Invoke(SetParamMethod, param, idParam);
        }

        /// <summary>
        /// Устанавливает параметр Multicad с указанием стандартов оформления.
        /// </summary>
        /// <param name="param">Новое значение параметра.</param>
        /// <param name="idParam">Идентификатор параметра.</param>
        /// <param name="std">Стандарты оформления.</param>
        /// <returns>true, если параметр установлен успешно.</returns>
        public static bool SetParam(object param, int idParam, Standarts std)
        {
            if (SetParamWithStandartsMethod == null)
            {
                throw new InvalidOperationException(
                    "McParamManager.SetParam(object, int, Standarts) не найден");
            }

            Type runtimeStandartsType =
                SetParamWithStandartsMethod.GetParameters()[2].ParameterType;

            object runtimeStandarts =
                Enum.ToObject(runtimeStandartsType, (int)std);

            return (bool)SetParamWithStandartsMethod.Invoke(
                null,
                new object[] { param, idParam, runtimeStandarts });
        }

        private static object Invoke(MethodInfo method, params object[] parameters)
        {
            if (method == null)
            {
                throw new InvalidOperationException("Метод McParamManager не найден");
            }

            return method.Invoke(null, parameters);
        }

        private static void InvokeVoid(MethodInfo method, params object[] parameters)
        {
            Invoke(method, parameters);
        }
    }

    /// <summary>
    /// Стандарты оформления Multicad.
    /// </summary>
    [Flags]
    public enum Standarts
    {
        /// <summary>ГОСТ.</summary>
        GOST = 1,

        /// <summary>СПДС.</summary>
        SPDS = 2,

        /// <summary>ISO.</summary>
        ISO = 4,

        /// <summary>ISO для машиностроения.</summary>
        ISO_MECH = 4,

        /// <summary>DIN.</summary>
        DIN = 8,

        /// <summary>CSN.</summary>
        CSN = 16,

        /// <summary>PN.</summary>
        PN = 32,

        /// <summary>JUS.</summary>
        JUS = 64,

        /// <summary>GB.</summary>
        GB = 128,

        /// <summary>NF.</summary>
        NF = 256,

        /// <summary>IS.</summary>
        IS = 512,

        /// <summary>ISO для архитектуры.</summary>
        ISO_ARCH = 1024,

        /// <summary>Все архитектурные стандарты.</summary>
        ALL_ARCH = 1026,

        /// <summary>ANSI.</summary>
        ANSI = 2048,

        /// <summary>Все машиностроительные стандарты.</summary>
        ALL_MECH = 3069,

        /// <summary>Все иностранные стандарты.</summary>
        ALL_FOREIGN = 4092,

        /// <summary>Все стандарты без СПДС.</summary>
        ALL_NO_SPDS = 4093,

        /// <summary>Все стандарты.</summary>
        ALL = 4095,

        /// <summary>Пользовательские стандарты.</summary>
        CUSTOM = 32768
    }
}