using dRz.Abstractions.Logger;
using dRz.Common.NC.Infrastructure;
using dRz.Common.NC.Services.LogLevel;
using System.ComponentModel;
using App = HostMgd.ApplicationServices;
using Rtm = Teigha.Runtime;

namespace dRz.Common.NC.Commands.Common
{
    /// <summary>
    /// Предоставляет команды NanoCAD для управления уровнями логирования.
    /// </summary>
    public sealed class LogLevelCommand
    {
        /// <summary>
        /// Настраивает уровень логирования внутренней диагностики.
        /// </summary>
        [Rtm.CommandMethod(CommandPrefix.Value + "_DiagnosticLevel", Rtm.CommandFlags.Session)]
        [Description("Настройка уровня логирования внутренней диагностики")]
        public void SetDiagnosticLevel()
        {
            LogLevelSettingsManager manager = new LogLevelSettingsManager(
                AddOnContext.Msg,
                new NanoCadLogLevelPrompt(App.Application.DocumentManager.MdiActiveDocument));

            manager.ManageLevel(
                AddOnContext.AddOnInfo.AssemblyDirectory,
                "diagnostic.level",
                "логирования внутренней диагностики",
                AddOnContext.NLogFactory.InternalLogLevel);
        }

        /// <summary>
        /// Настраивает уровень логирования основного логгера.
        /// </summary>
        [Rtm.CommandMethod(CommandPrefix.Value + "_LogLevel", Rtm.CommandFlags.Session)]
        [Description("Настройка уровня логирования")]
        public void SetLogLevel()
        {
            IDrzLogger logger = AddOnContext.NLogFactory.GetLogger(typeof(LogLevelCommand));
            LogLevelSettingsManager manager = new LogLevelSettingsManager(
                AddOnContext.Msg,
                new NanoCadLogLevelPrompt(App.Application.DocumentManager.MdiActiveDocument));

            manager.ManageLevel(
                AddOnContext.AddOnInfo.AssemblyDirectory,
                "logger.level",
                "логирования",
                LogLevelSettingsManager.GetEffectiveLogLevel(logger));
        }
    }
}