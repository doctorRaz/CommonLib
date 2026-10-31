using dRz.Abstractions.Logger;
using dRz.Abstractions.Services.Message;
using dRz.Abstractions.Settings;
using dRz.Abstractions.Updater;
using dRz.Settings;
using Moq;
using NUnit.Framework;

namespace dRz.Test.Settings
{
    /// <summary>
    /// Проверяет загрузку и сохранение настроек приложения.
    /// </summary>
    [TestFixture]
    public sealed class SettingsManagerTests
    {
        private string _directory = null!;
        private string _filePath = null!;
        private Mock<IDrzLogger> _logger = null!;
        private Mock<IDrzLoggerFactory> _loggerFactory = null!;
        private Mock<IMessageService> _messageService = null!;

        /// <summary>
        /// Подготавливает временный каталог и зависимости для теста.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "dRz.Settings.Tests", Guid.NewGuid().ToString("N"));
            _filePath = Path.Combine(_directory, "settings.json");
            _logger = new Mock<IDrzLogger>();
            _loggerFactory = new Mock<IDrzLoggerFactory>();
            _loggerFactory.Setup(factory => factory.GetLogger<JsonSettingsStore>()).Returns(_logger.Object);
            _messageService = new Mock<IMessageService>();
        }

        /// <summary>
        /// Удаляет временные файлы, созданные тестом.
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
        /// Проверяет создание и сохранение настроек по умолчанию, если файл отсутствует.
        /// </summary>
        [Test]
        public void Load_WhenFileDoesNotExist_CreatesAndSavesDefaults()
        {
            IAddonSettings settings = CreateManager().Load();

            Assert.That(settings.General.ShowCommandInfoOnStartup, Is.True);
            Assert.That(settings.Updates.Mode, Is.EqualTo(UpdateMode.CheckAndInstall));
            Assert.That(settings.Updates.UpdateCheckIntervalDays, Is.EqualTo(7));
            Assert.That(File.Exists(_filePath), Is.True);
        }

        /// <summary>
        /// Проверяет сохранение и последующую загрузку настроек обновления.
        /// </summary>
        [Test]
        public void SaveAndLoad_PreservesUpdateSettings()
        {
            IAddonSettings settings = CreateManager().Load();
            settings.General.ShowCommandInfoOnStartup = false;
            settings.Updates.Mode = UpdateMode.CheckAndInstall;
            settings.Updates.UpdateCheckIntervalDays = 12;
            settings.Updates.LastCheck = new DateTime(2026, 10, 14, 12, 30, 0, DateTimeKind.Utc);
            settings.Updates.LastResult = UpdateResult.Installed;
            settings.Save();

            IAddonSettings reloaded = CreateManager().Load();

            Assert.That(reloaded.General.ShowCommandInfoOnStartup, Is.False);
            Assert.That(reloaded.Updates.Mode, Is.EqualTo(UpdateMode.CheckAndInstall));
            Assert.That(reloaded.Updates.UpdateCheckIntervalDays, Is.EqualTo(12));
            Assert.That(reloaded.Updates.LastCheck, Is.EqualTo(settings.Updates.LastCheck));
            Assert.That(reloaded.Updates.LastResult, Is.EqualTo(UpdateResult.Installed));
        }

        /// <summary>
        /// Проверяет применение значений по умолчанию при отсутствии секции общих настроек.
        /// </summary>
        [Test]
        public void Load_WhenGeneralSectionIsMissing_UsesGeneralDefaults()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_filePath, "{ \"Updates\": { \"Mode\": \"CheckAndNotify\" } }");

            IAddonSettings settings = CreateManager().Load();

            Assert.That(settings.General.ShowCommandInfoOnStartup, Is.True);
        }

        /// <summary>
        /// Проверяет обработку некорректного JSON и замену файла настройками по умолчанию.
        /// </summary>
        [Test]
        public void Load_WhenJsonIsInvalid_LogsErrorAndReplacesFileWithDefaults()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_filePath, "{ invalid json");

            IAddonSettings settings = CreateManager().Load();

            Assert.That(settings.Updates.Mode, Is.EqualTo(UpdateMode.CheckAndInstall));
            Assert.That(File.ReadAllText(_filePath), Does.Contain("CheckAndInstall"));
            _logger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.Is<string>(message => message.Contains("Будут использованы значения по умолчанию"))), Times.Once);
        }

        /// <summary>
        /// Проверяет журналирование, уведомление и исключение при невозможности сохранить настройки.
        /// </summary>
        [Test]
        public void Save_WhenFilePathIsDirectory_LogsNotifiesAndThrows()
        {
            IAddonSettings settings = CreateManager().Load();
            File.Delete(_filePath);
            Directory.CreateDirectory(_filePath);

            Assert.Catch<Exception>(() => settings.Save());
            _logger.Verify(logger => logger.Error(It.IsAny<Exception>(), It.Is<string>(message => message.Contains("Не удалось сохранить настройки"))), Times.Once);
            _messageService.Verify(
                service => service.ErrorMessage(
                    It.IsAny<string>(),
                    It.IsAny<Exception>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once);
        }

        private SettingsManager CreateManager()
        {
            JsonSettingsStore store = new JsonSettingsStore(_filePath, _loggerFactory.Object, _messageService.Object);
            return new SettingsManager(store);
        }
    }
}