#if DEBUG
using drz.MulticadInterop.McParamManager;
using dRz.Common.NC.Infrastructure;
using HostMgd.EditorInput;
using System.ComponentModel;
using System.Drawing;
using Teigha.Runtime;
using App = HostMgd.ApplicationServices;

namespace dRz.NC.Commands.Test.McParam
{
    public class McParam
    {
        private App.Document doc => App.Application.DocumentManager.MdiActiveDocument;
        private Editor ed => doc.Editor;

        /// <summary>
        /// Переключает базу данных Multicad.
        /// </summary>
        [CommandMethod(CommandPrefix.Value + "_SetParam", CommandFlags.Session)]
        [Description("Переключение базы данных Multicad")]
        public void ChangedbMod()
        {

            PromptStringOptions opts = new PromptStringOptions("enter base:")
            {
                AllowSpaces = true
            };

            PromptResult pr = ed.GetString(opts);

            if (PromptStatus.OK == pr.Status)
            {
                MulticadParamManager.SetParam(pr.StringResult, 9);
            }
        }


        [CommandMethod(CommandPrefix.Value + "_GetStringParam", CommandFlags.Session)]
        public static void ParamMethods()
        {
            //тянем имя непечатного слоя из настроек МС
            string newLayerNameRav = MulticadParamManager.GetStringParam(1042);
            AddOnContext.Msg.InfoMessage(newLayerNameRav);

            //имя с приставкой из настроек, 
            string newLayerName = MulticadParamManager.GetProfiledLayerName(newLayerNameRav);
            AddOnContext.Msg.InfoMessage(newLayerNameRav);
        }

        [CommandMethod(CommandPrefix.Value + "_GetStringParam2", CommandFlags.Session)]
        public static void ParamMethods2()
        {
            //тянем имя непечатного слоя из настроек МС
            string newLayerNameRav = MulticadParamManager.GetStringParam(1042);
            AddOnContext.Msg.InfoMessage(newLayerNameRav);


            newLayerNameRav = "test";
            //имя с приставкой из настроек, 
            string newLayerName = MulticadParamManager.GetProfiledLayerName(newLayerNameRav);
            AddOnContext.Msg.InfoMessage(newLayerNameRav);
        }

        [CommandMethod(CommandPrefix.Value + "_GetStringParam3", CommandFlags.Session)]
        public static void ParamMethods3()
        {

            //тянем имя профиля
            string combobox_profile = MulticadParamManager.GetStringParam(25005);
            AddOnContext.Msg.InfoMessage(combobox_profile);



            bool combobox_bool = MulticadParamManager.GetBoolParam(1048);
            AddOnContext.Msg.InfoMessage($"combobox_profile: {combobox_bool}");

            //MulticadParamManager.CallOptions("",IntPtr.Zero);

            double combobox_lineweight = MulticadParamManager.GetDoubleParam(2335);
            AddOnContext.Msg.InfoMessage($"combobox_lineweight: {combobox_lineweight}");

            Color combobox_color = MulticadParamManager.GetColorParam(11135);
            AddOnContext.Msg.InfoMessage($"combobox_color: {combobox_color}");

        }


    }
}
#endif