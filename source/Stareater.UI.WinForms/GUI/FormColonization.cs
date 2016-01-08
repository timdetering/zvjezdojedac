﻿using System;
using System.Linq;
using System.Windows.Forms;
using Stareater.AppData;
using Stareater.Controllers;

namespace Stareater.GUI
{
	public partial class FormColonization : Form
	{
		private readonly GameController controller;
		
		public FormColonization()
		{
			InitializeComponent();
		}
		
		public FormColonization(GameController controller) : this()
		{
			this.controller = controller;
			
			var projects = controller.ColonizationProjects().ToList();
			projectList.RowStyles.Clear();
			for (int i = 0; i < projects.Count; i++)
				projectList.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			
			for (int i = 0; i < projects.Count; i++) 
			{
				var itemView = new ColonizationTargetView(projects[i], controller);
				projectList.Controls.Add(itemView);
			}
			
			var context = SettingsWinforms.Get.Language["FormColonization"];
			this.Text = context["title"].Text();
		}
		
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape) 
				this.Close();
			return base.ProcessCmdKey(ref msg, keyData);
		}
	}
}