﻿using SwissAcademic.Addons.C6ToC5Export.Properties;
using SwissAcademic.Citavi;
using SwissAcademic.Citavi.Shell;
using SwissAcademic.Controls;
using System;
using System.Windows.Forms;

namespace SwissAcademic.Addons.C6ToC5Export
{
    public class Addon : CitaviAddOn
    {
        #region Properties

        public override AddOnHostingForm HostingForm => AddOnHostingForm.MainForm;

        #endregion

        #region Methods

        protected override void OnBeforePerformingCommand(BeforePerformingCommandEventArgs e)
        {
            e.Handled = true;

            if (e.Form is MainForm mainForm)
            {
                switch (e.Key)
                {
                    case (AddonKeys.ExportButtonCommand):
                        {
                            using (var saveFileDialog = new SaveFileDialog { Filter = C6ToC5ExportResources.ProjectFilters, CheckPathExists = true, Title = C6ToC5ExportResources.ExportTitle })
                            {
                                if (saveFileDialog.ShowDialog(e.Form) == DialogResult.OK)
                                {
                                    try
                                    {
                                        mainForm.Project.SaveAsXml(saveFileDialog.FileName, ProjectXmlExportCompatibility.Citavi5);
                                        MessageBox.Show(e.Form, C6ToC5ExportResources.ExportFinallyMessage, "Citavi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                    catch (Exception ex)
                                    {
                                        if (MessageBox.Show(e.Form, C6ToC5ExportResources.ExportExceptionMessage.FormatString(ex.Message), "Citavi", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                                        {
                                            Clipboard.SetText(e.ToString());
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
            }

            base.OnBeforePerformingCommand(e);
        }

        protected override void OnHostingFormLoaded(Form form)
        {
            if (form is MainForm mainForm)
            {
                var button = mainForm.GetMainCommandbarManager()
                                    .GetReferenceEditorCommandbar(MainFormReferenceEditorCommandbarId.Menu)
                                    .GetCommandbarMenu(MainFormReferenceEditorCommandbarMenuId.File)
                                    .GetCommandbarMenu("ThisProject")
                                    .InsertCommandbarButton(3, AddonKeys.ExportButtonCommand, C6ToC5ExportResources.ExportCitaviButtonText);
            }

            base.OnHostingFormLoaded(form);
        }

        protected override void OnLocalizing(Form form)
        {
            if (form is MainForm mainForm)
            {
                var button = mainForm.GetMainCommandbarManager()
                                     .GetReferenceEditorCommandbar(MainFormReferenceEditorCommandbarId.Menu)
                                     .GetCommandbarMenu(MainFormReferenceEditorCommandbarMenuId.File)
                                     .GetCommandbarMenu("ThisProject")
                                     .GetCommandbarButton(AddonKeys.ExportButtonCommand);

                if (button != null) button.Text = C6ToC5ExportResources.ExportCitaviButtonText;
            }

            base.OnLocalizing(form);
        }

        #endregion
    }
}