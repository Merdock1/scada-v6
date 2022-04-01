﻿// Copyright (c) Rapid Software LLC. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Scada.Admin.Extensions.ExtWirenBoard.Code;
using Scada.Admin.Extensions.ExtWirenBoard.Controls;
using Scada.Admin.Project;
using Scada.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scada.Admin.Extensions.ExtWirenBoard.Forms
{
    /// <summary>
    /// Represents a wizard form for creating a project configuration.
    /// <para>Представляет форму мастера для создания конфигурации проекта.</para>
    /// </summary>
    public partial class FrmWirenBoardWizard : Form
    {
        /// <summary>
        /// Specifies the wizard steps.
        /// </summary>
        private enum Step { SelectLine, ReadTopics, SelectTopics, SetEntityIDs, ValidateIDs }

        /// <summary>
        /// Specifies the step offsets.
        /// </summary>
        private enum StepOffset { Back = -1, None = 0, Next = 1 }

        private readonly IAdminContext adminContext;      // the Administrator context
        private readonly ScadaProject project;            // the project under development
        private readonly RecentSelection recentSelection; // the recently selected parameters

        private readonly CtrlLineSelect ctrlLineSelect;
        private readonly CtrlLog ctrlLog;
        private readonly CtrlTopicTree ctrlTopicTree;
        private readonly CtrlEntityID ctrlEntityID;

        private Step step; // the current waizard step


        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        private FrmWirenBoardWizard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        public FrmWirenBoardWizard(IAdminContext adminContext, ScadaProject project, RecentSelection recentSelection)
            : this()
        {
            this.adminContext = adminContext ?? throw new ArgumentNullException(nameof(adminContext));
            this.project = project ?? throw new ArgumentNullException(nameof(project));
            this.recentSelection = recentSelection ?? throw new ArgumentNullException(nameof(recentSelection));

            ctrlLineSelect = new CtrlLineSelect(adminContext, project, recentSelection) 
            { 
                Dock = DockStyle.Fill, 
                Visible = false 
            };

            ctrlLog = new CtrlLog { Dock = DockStyle.Fill, Visible = false };
            ctrlTopicTree = new CtrlTopicTree { Dock = DockStyle.Fill, Visible = false };
            ctrlEntityID = new CtrlEntityID { Dock = DockStyle.Fill, Visible = false };

            pnlMain.Controls.Add(ctrlLineSelect);
            pnlMain.Controls.Add(ctrlLog);
            pnlMain.Controls.Add(ctrlTopicTree);
            pnlMain.Controls.Add(ctrlEntityID);

            step = Step.SelectLine;
        }


        /// <summary>
        /// Validates the current step.
        /// </summary>
        private bool ValidateStep()
        {
            return step switch
            {
                Step.SelectLine => ctrlLineSelect.ValidateControl(),
                _ => true
            };
        }

        /// <summary>
        /// Makes a wizard step.
        /// </summary>
        private void MakeStep(StepOffset stepOffset)
        {
            if (stepOffset == StepOffset.Back && step > Step.SelectLine)
                step--;
            if (stepOffset == StepOffset.Next && step < Step.ValidateIDs)
                step++;

            ctrlLineSelect.Visible = false;
            ctrlLog.Visible = false;
            ctrlTopicTree.Visible = false;
            ctrlEntityID.Visible = false;
            btnBack.Visible = false;
            btnNext.Visible = false;
            btnCreate.Visible = false;

            switch (step)
            {
                case Step.SelectLine:
                    lblStep.Text = ExtensionPhrases.Step1Descr;
                    ctrlLineSelect.Visible = true;
                    btnNext.Visible = true;
                    ctrlLineSelect.SetFocus();
                    break;

                case Step.ReadTopics:
                    lblStep.Text = ExtensionPhrases.Step2Descr;
                    ctrlLog.Visible = true;
                    btnBack.Visible = true;
                    btnNext.Visible = true;
                    break;

                case Step.SelectTopics:
                    break;

                case Step.SetEntityIDs:
                    break;

                case Step.ValidateIDs:
                    break;
            }
        }


        private void FrmWirenBoardWizard_Load(object sender, EventArgs e)
        {
            FormTranslator.Translate(this, GetType().FullName);
            FormTranslator.Translate(ctrlLineSelect, ctrlLineSelect.GetType().FullName);
            FormTranslator.Translate(ctrlLog, ctrlLog.GetType().FullName);
            FormTranslator.Translate(ctrlTopicTree, ctrlTopicTree.GetType().FullName);
            FormTranslator.Translate(ctrlEntityID, ctrlEntityID.GetType().FullName);

            MakeStep(StepOffset.None);
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            MakeStep(StepOffset.Back);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (ValidateStep())
                MakeStep(StepOffset.Next);
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {

        }
    }
}