﻿using SwissAcademic.Addons.UpdateBibliographicDataFromPubMedSearch.Properties;
using System;
using System.Windows.Forms;

namespace SwissAcademic.Addons.UpdateBibliographicDataFromPubMedSearch
{
    public partial class OverrideFieldsDialog : Form
    {
        #region Constructors

        public OverrideFieldsDialog()
        {
            InitializeComponent();

            chbOverrideAbstract.Text = Resources.OverrideAbstract;
            chbOverrideKeywords.Text = Resources.OverrideKeywords;
            chbOverrideTOC.Text = Resources.OverrideToc;
            chbRemoveNotes.Text = Resources.ClearNotes;
            btnCancel.Text = Resources.Cancel;
            btnOk.Text = Resources.Ok;
        }

        #endregion

        #region Properties

        public MacroSettings Settings => new MacroSettings
        {
            ClearNotes = chbRemoveNotes.Checked,
            OverwriteAbstract = chbOverrideAbstract.Checked,
            OverwriteKeywords = chbOverrideKeywords.Checked,
            OverwriteTableOfContents = chbOverrideTOC.Checked
        };

        #endregion

        #region Eventhandlers

        void BtnOk_Click(object sender, EventArgs e) => DialogResult = DialogResult.OK;

        void BtnCancel_Click(object sender, EventArgs e) => DialogResult = DialogResult.Cancel;

        #endregion
    }
}
