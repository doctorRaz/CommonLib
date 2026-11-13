using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Common.NC.Services.LogLevel;
using Moq;
using NUnit.Framework;
using System;
using System.IO;

namespace dRz.Test.Common.NC
{
    /// <summary>
    /// Проверяет управление файлами переопределения уровней без CAD-хоста.
    /// </summary>
    [TestFixture]
    public sealed class LogLevelSettingsManagerTests
    {
        private string _directory = null!;
        private Mock<IMessageService> _message = null!;
        private TestLogLevelPrompt _prompt = null!;

        /// <summary>
        /// Создаёт временный каталог и подготавливает зависимости для тестов.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "dRz.LogLevel.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _message = new Mock<IMessageService>();
            _prompt = new TestLogLevelPrompt();
        }

        /// <summary>
        /// Удаляет временный каталог после выполнения теста.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>
        /// Проверяет запись выбранного уровня логирования и уведомление пользователя.
        /// </summary>
        [Test]
        public void ManageLevel_WhenApplySelected_WritesLevelAndNotifiesUser()
        {
            _prompt.Action = LogLevelAction.Apply;
            _prompt.Level = LogLevelValue.Debug;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            Assert.That(File.ReadAllText(Path.Combine(_directory, "logger.level")), Is.EqualTo("Debug" + Environment.NewLine));
            _message.Verify(
                service => service.InfoMessage(
                    It.Is<string>(text => text.Contains("установлен уровень Debug")),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет удаление файла переопределения выбранного уровня логирования.
        /// </summary>
        [Test]
        public void ManageLevel_WhenDeleteSelected_RemovesOverrideFile()
        {
            string path = Path.Combine(_directory, "logger.level");
            File.WriteAllText(path, "Debug");
            _prompt.Action = LogLevelAction.Delete;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            Assert.That(File.Exists(path), Is.False);
            _message.Verify(
                service => service.InfoMessage(
                    It.Is<string>(text => text.Contains("Переопределение уровня логирования удалено")),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет отображение первой строки файла переопределения после удаления пробелов по краям.
        /// </summary>
        [Test]
        public void ManageLevel_WhenStatusSelected_ReportsTrimmedFirstLine()
        {
            File.WriteAllText(Path.Combine(_directory, "logger.level"), "  Warn  " + Environment.NewLine + "Error");
            _prompt.Action = LogLevelAction.Status;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            _message.Verify(
                service => service.InfoMessage(
                    It.Is<string>(text => text.Contains("задан уровень: Warn") && text.Contains("Фактический уровень")),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет, что отмена выбора действия не изменяет существующий файл переопределения.
        /// </summary>
        [Test]
        public void ManageLevel_WhenCanceled_DoesNotChangeOverrideFile()
        {
            string path = Path.Combine(_directory, "logger.level");
            File.WriteAllText(path, "Info");

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            Assert.That(File.ReadAllText(path), Is.EqualTo("Info"));
            _message.Verify(service => service.InfoMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Проверяет определение наименее подробного включённого уровня логирования для разных комбинаций флагов.
        /// </summary>
        /// <param name="trace">Признак включённого уровня Trace.</param>
        /// <param name="debug">Признак включённого уровня Debug.</param>
        /// <param name="info">Признак включённого уровня Info.</param>
        /// <param name="warn">Признак включённого уровня Warn.</param>
        /// <param name="error">Признак включённого уровня Error.</param>
        /// <param name="fatal">Признак включённого уровня Fatal.</param>
        /// <param name="expected">Ожидаемое наименование эффективного уровня.</param>
        [TestCase(true, false, false, false, false, false, "Trace")]
        [TestCase(false, true, true, false, false, false, "Debug")]
        [TestCase(false, false, true, true, false, false, "Info")]
        [TestCase(false, false, false, true, true, false, "Warn")]
        [TestCase(false, false, false, false, true, true, "Error")]
        [TestCase(false, false, false, false, false, true, "Fatal")]
        [TestCase(false, false, false, false, false, false, "Off")]
        public void GetEffectiveLogLevel_ReturnsLowestEnabledLevel(
            bool trace,
            bool debug,
            bool info,
            bool warn,
            bool error,
            bool fatal,
            string expected)
        {
            Mock<IDrzLogger> logger = new Mock<IDrzLogger>();
            logger.SetupGet(item => item.IsTraceEnabled).Returns(trace);
            logger.SetupGet(item => item.IsDebugEnabled).Returns(debug);
            logger.SetupGet(item => item.IsInfoEnabled).Returns(info);
            logger.SetupGet(item => item.IsWarnEnabled).Returns(warn);
            logger.SetupGet(item => item.IsErrorEnabled).Returns(error);
            logger.SetupGet(item => item.IsFatalEnabled).Returns(fatal);

            Assert.That(LogLevelSettingsManager.GetEffectiveLogLevel(logger.Object), Is.EqualTo(expected));
        }


        /// <summary>
        /// Проверяет, что отмена выбора уровня не создаёт файл переопределения.
        /// </summary>
        [Test]
        public void ManageLevel_WhenLevelSelectionCanceled_DoesNotCreateOverrideFile()
        {
            _prompt.Action = LogLevelAction.Apply;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            Assert.That(File.Exists(Path.Combine(_directory, "logger.level")), Is.False);
            _message.Verify(service => service.InfoMessage(It.IsAny<string>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<string>()), Times.Never);

            _message.Verify(service => service.ErrorMessage(It.IsAny<string>(),
                                                            It.IsAny<Exception>(),
                                                            It.IsAny<string>(),
                                                            It.IsAny<string>()),
                                                            Times.Never);
        }

        /// <summary>
        /// Проверяет уведомление об отсутствии файла и использовании программного уровня при запросе удаления.
        /// </summary>
        [Test]
        public void ManageLevel_WhenDeleteSelectedAndFileDoesNotExist_ReportsFallback()
        {
            _prompt.Action = LogLevelAction.Delete;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            _message.Verify(
                  service => service.InfoMessage(
                      It.Is<string>(text =>
                          text.Contains("отсутствует") &&
                          text.Contains("программное значение")),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
                  Times.Once);
        }

        /// <summary>
        /// Проверяет отображение фактического уровня, если файл переопределения отсутствует.
        /// </summary>
        [Test]
        public void ManageLevel_WhenStatusSelectedAndFileDoesNotExist_ReportsActualLevel()
        {
            _prompt.Action = LogLevelAction.Status;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Warn");

            _message.Verify(
                service => service.InfoMessage(
                    It.Is<string>(text => text.Contains("отсутствует") && text.Contains("Warn")),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет уведомление о пустой первой строке файла и отображение программного уровня.
        /// </summary>
        [Test]
        public void ManageLevel_WhenStatusSelectedAndFileIsEmpty_ReportsEmptyFirstLine()
        {
            File.WriteAllText(Path.Combine(_directory, "logger.level"), string.Empty);
            _prompt.Action = LogLevelAction.Status;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            _message.Verify(
                service => service.InfoMessage(
                    It.Is<string>(text => text.Contains("первая строка пуста") && text.Contains("Info")),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет отправку уведомления об ошибке, если запись файла переопределения невозможна.
        /// </summary>
        [Test]
        public void ManageLevel_WhenApplyCannotWriteFile_ReportsError()
        {
            // Каталог с именем файла имитирует ситуацию, в которой запись файла невозможна.
            Directory.CreateDirectory(Path.Combine(_directory, "logger.level"));
            _prompt.Action = LogLevelAction.Apply;
            _prompt.Level = LogLevelValue.Debug;

            CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info");

            _message.Verify(
                service => service.ErrorMessage(
                    It.Is<string>(text => text.Contains("Не удалось записать файл logger.level")),
                    It.IsAny<Exception>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// Проверяет исключение при передаче неизвестного значения действия.
        /// </summary>
        [Test]
        public void ManageLevel_WhenActionHasUnknownValue_ThrowsArgumentOutOfRangeException()
        {
            _prompt.Action = (LogLevelAction)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче неизвестного значения уровня логирования.
        /// </summary>
        [Test]
        public void ManageLevel_WhenLevelHasUnknownValue_ThrowsArgumentOutOfRangeException()
        {
            _prompt.Action = LogLevelAction.Apply;
            _prompt.Level = (LogLevelValue)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateManager().ManageLevel(_directory, "logger.level", "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо каталога настроек.
        /// </summary>
        /// <param name="directory">Каталог настроек.</param>
        [TestCase(null)]
        public void ManageLevel_WhenSettingsDirectoryIsNull_ThrowsArgumentNullException(string directory)
        {
            Assert.Throws<ArgumentNullException>(
                () => CreateManager().ManageLevel(directory, "logger.level", "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче пустого или состоящего из пробелов пути к каталогу настроек.
        /// </summary>
        /// <param name="directory">Некорректный путь к каталогу настроек.</param>
        [TestCase("")]
        [TestCase(" ")]
        public void ManageLevel_WhenSettingsDirectoryIsEmpty_ThrowsArgumentException(string directory)
        {
            Assert.Throws<ArgumentException>(
                () => CreateManager().ManageLevel(directory, "logger.level", "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо имени файла переопределения.
        /// </summary>
        [Test]
        public void ManageLevel_WhenFileNameIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => CreateManager().ManageLevel(_directory, null, "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче пустого или состоящего из пробелов имени файла.
        /// </summary>
        /// <param name="fileName">Некорректное имя файла.</param>
        [TestCase("")]
        [TestCase(" ")]
        public void ManageLevel_WhenFileNameIsEmpty_ThrowsArgumentException(string fileName)
        {
            Assert.Throws<ArgumentException>(
                () => CreateManager().ManageLevel(_directory, fileName, "уровня логирования", "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо наименования настройки.
        /// </summary>
        [Test]
        public void ManageLevel_WhenSettingNameIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(
                () => CreateManager().ManageLevel(_directory, "logger.level", null, "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче пустого или состоящего из пробелов наименования настройки.
        /// </summary>
        /// <param name="settingName">Некорректное наименование настройки.</param>
        [TestCase("")]
        [TestCase(" ")]
        public void ManageLevel_WhenSettingNameIsEmpty_ThrowsArgumentException(string settingName)
        {
            Assert.Throws<ArgumentException>(
                () => CreateManager().ManageLevel(_directory, "logger.level", settingName, "Info"));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо сервиса сообщений в конструктор.
        /// </summary>
        [Test]
        public void Constructor_WhenMessageIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LogLevelSettingsManager(null, _prompt));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо интерфейса выбора уровня логирования в конструктор.
        /// </summary>
        [Test]
        public void Constructor_WhenPromptIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new LogLevelSettingsManager(_message.Object, null));
        }

        /// <summary>
        /// Проверяет исключение при передаче null вместо логгера.
        /// </summary>
        [Test]
        public void GetEffectiveLogLevel_WhenLoggerIsNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => LogLevelSettingsManager.GetEffectiveLogLevel(null));
        }

        /// <summary>
        /// Проверяет выбор наиболее подробного уровня, если одновременно включено несколько уровней логирования.
        /// </summary>
        [Test]
        public void GetEffectiveLogLevel_WhenSeveralLevelsAreEnabled_ReturnsMostDetailedLevel()
        {
            Mock<IDrzLogger> logger = new Mock<IDrzLogger>();
            logger.SetupGet(item => item.IsTraceEnabled).Returns(false);
            logger.SetupGet(item => item.IsDebugEnabled).Returns(true);
            logger.SetupGet(item => item.IsInfoEnabled).Returns(true);
            logger.SetupGet(item => item.IsWarnEnabled).Returns(true);
            logger.SetupGet(item => item.IsErrorEnabled).Returns(true);
            logger.SetupGet(item => item.IsFatalEnabled).Returns(true);

            Assert.That(LogLevelSettingsManager.GetEffectiveLogLevel(logger.Object), Is.EqualTo("Debug"));
        }

        private LogLevelSettingsManager CreateManager()
        {
            return new LogLevelSettingsManager(_message.Object, _prompt);
        }

        private sealed class TestLogLevelPrompt : ILogLevelPrompt
        {
            public LogLevelAction? Action { get; set; }

            public LogLevelValue? Level { get; set; }

            public LogLevelAction? ChooseAction(string settingName) => Action;

            public LogLevelValue? ChooseLevel(string settingName) => Level;
        }
    }
}
