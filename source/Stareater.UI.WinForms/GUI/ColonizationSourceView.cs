﻿using System;
using System.Linq;
using System.Windows.Forms;
using Stareater.Controllers;
using Stareater.Controllers.Views;
using Stareater.Localization;
using Stareater.Utils.Collections;

namespace Stareater.GUI
{
	public partial class ColonizationSourceView : UserControl
	{
		private StellarisInfo sourceData = null;
		private readonly ColonizationController controller;
		
		public event Action OnStateChange;
		
		public ColonizationSourceView()
		{
			InitializeComponent();
		}
		
		public ColonizationSourceView(ColonizationController controller) : this()
		{
			if (controller == null)
				throw new ArgumentNullException("controller");
			this.controller = controller;
		}
		
		public StellarisInfo Data 
		{
			get
			{
				return sourceData;
			}
			set
			{
				this.sourceData = value;
				
				updateView();
			}
		}
		
		private void updateView()
		{
			var context = LocalizationManifest.Get.CurrentLanguage["FormColonization"];
			
			if (controller.Sources().Contains(sourceData))
			{
				this.controlButton.Image = Stareater.Properties.Resources.start;
				this.starName.Text = this.sourceData.HostStar.Name.ToText(LocalizationManifest.Get.CurrentLanguage);
			}
			else
			{
				this.controlButton.Image = Stareater.Properties.Resources.stop;
				this.starName.Text = context["stoppedColonization"].Text(
					new TextVar("star", this.sourceData.HostStar.Name.ToText(LocalizationManifest.Get.CurrentLanguage)).Get
				);
			}
		}
		
		private void controlButton_Click(object sender, EventArgs e)
		{
			if (controller.IsColonizing)
				controller.StopColonization(sourceData);
			else
				controller.StartColonization(sourceData);
			
			updateView();
			
			if (OnStateChange != null)
				OnStateChange();
		}
	}
}
