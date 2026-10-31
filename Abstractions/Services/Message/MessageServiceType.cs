namespace dRz.Abstractions.Services.Message
{
    /// <summary>
    ///перечисление типов интерфейсов
    /// </summary>
    public enum MessageServiceType
    {
        /// <summary>если есть активный документ то ком строка, иначе окошко</summary>
        Default,

        /// <summary>ком строка</summary>
        CommandLine,

        /// <summary>окошки</summary>
        Window,

        /// <summary>мультикад окошко</summary>
        McNotifi
    }
}