﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Stareater.AppData;
using Stareater.Controllers;
using Stareater.Controllers.Views.Ships;
using Stareater.GUI.ShipDesigns;
using Stareater.GuiUtils;
using Stareater.Localization;
using Stareater.Utils.Collections;
using Stareater.Utils.NumberFormatters;
using Stareater.Utils;

namespace Stareater.GUI
{
	public partial class FormShipDesigner : Form
	{
		private readonly Context context;
		private readonly ShipDesignController controller;

		private bool automaticName = true;
		private IList<HullInfo> hulls;
		private Dictionary<HullInfo, int> imageIndices = new Dictionary<HullInfo, int>();
		private EquipmentActionDispatcher equipmentAction = new EquipmentActionDispatcher();
		
		public FormShipDesigner()
		{
			InitializeComponent();
		}
		
		public FormShipDesigner(ShipDesignController controller) : this()
		{
			this.context = SettingsWinforms.Get.Language["FormDesign"];
			this.controller = controller;
			this.hulls = this.controller.Hulls().OrderBy(x => x.Size).ToList();
			
			var rand = new Random();
			
			foreach(var hull in hulls) {
				this.hullPicker.Items.Add(new Tag<HullInfo>(hull, hull.Name));
				this.imageIndices.Add(hull, rand.Next(hull.ImagePaths.Length));
			}
			
			this.hullPicker.SelectedIndex = 0;
		}
		
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Escape) 
				this.Close();
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void addSpecialEquip(SpecialEquipInfo equipInfo)
		{
			this.controller.AddSpecialEquip(equipInfo);

			var itemView = new ShipEquipmentItem();
			itemView.Data = new ShipComponentType<SpecialEquipInfo>(
				equipInfo.Name, equipInfo.ImagePath, equipInfo,
				equipmentAction.Dispatch
			);
			itemView.Amount = this.controller.SpecialEquipCount(equipInfo);

			//TODO(v0.5) ensure "special equipment" label is above
			equipmentList.Controls.Add(itemView);
			equipmentList.SelectedIndex = equipmentList.Controls.Count - 1;
		}
		
		private void changeHullImage(int direction)
		{
			if (this.hullPicker.SelectedItem == null)
				return;
			var hull = (this.hullPicker.SelectedItem as Tag<HullInfo>).Value;
			
			this.imageIndices[hull] = 
				(this.imageIndices[hull] + hull.ImagePaths.Length + direction) % 
				hull.ImagePaths.Length;
			this.hullImage.Image = ImageCache.Get[hull.ImagePaths[imageIndices[hull]]];
			
			this.controller.ImageIndex = imageIndices[hull];
			checkValidity();
		}
		
		private void checkValidity()
		{
			this.acceptButton.Enabled = controller.IsDesignValid;
		}
		
		private void updateInfos()
		{
			var percentFormat = new DecimalsFormatter(0, 1);
			var thousandsFormat = new ThousandsFormatter();

			double powerGenerated = this.controller.Reactor.Power;
			double powerUsed = this.controller.PowerUsed;
			
			this.armorInfo.Text = this.context["armor"].Text(
				new TextVar("totalHp", thousandsFormat.Format(this.controller.HitPoints)).Get
			);
			this.mobilityInfo.Text = this.context["mobility"].Text(
				new TextVar("mobility", thousandsFormat.Format(this.controller.Evasion)).Get
			);
			this.powerInfo.Text = this.context["power"].Text(
				new TextVar("powerPercent", percentFormat.Format(Methods.Clamp(1 - powerUsed / powerGenerated, 0, 1) * 100)).Get
			);
			this.sensorInfo.Text = this.context["sensors"].Text(
				new TextVar("detection", this.controller.Detection.ToString("0.#")).Get
			);
			this.stealthInfo.Text = this.context["stealth"].Text(
				new TextVar("jamming", this.controller.Jamming.ToString("0.#")).
					And("cloaking", this.controller.Cloaking.ToString("0.#")).Get
			);
			
			this.spaceInfo.SetSpace(this.controller.SpaceUsed, this.controller.SpaceTotal);
		}

		private void acceptButton_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}
		
		private void hullSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (this.hullPicker.SelectedItem == null)
				return;
			var hull = (this.hullPicker.SelectedItem as Tag<HullInfo>).Value;
			
			if (this.automaticName)
				nameInput.Text = hull.Name; //TODO(later): get hull and organization specific name
			
			this.hullImage.Image = ImageCache.Get[hull.ImagePaths[this.imageIndices[hull]]];
			
			this.controller.Hull = hull;
			this.controller.ImageIndex = this.imageIndices[hull];
			
			this.hasIsDrive.Visible = this.controller.AvailableIsDrive != null;
			this.hasIsDrive.Checked &= this.hasIsDrive.Visible;
			this.isDriveImage.Visible = this.hasIsDrive.Checked;
			
			this.controller.HasIsDrive = this.hasIsDrive.Checked;
			
			if (this.controller.AvailableIsDrive != null) {
				var speedFormat = new DecimalsFormatter(0, 2);

				this.hasIsDrive.Text = this.context["isDrive"].Text(
					new TextVar("name", this.controller.AvailableIsDrive.Name).
					And("speed", speedFormat.Format(this.controller.AvailableIsDrive.Speed)).Get
				);
				this.isDriveImage.Image = ImageCache.Get[this.controller.AvailableIsDrive.ImagePath];
			}
			
			this.checkValidity();
			this.updateInfos();
		}
		
		private void imageLeft_ButtonClick(object sender, EventArgs e)
		{
			this.changeHullImage(-1);
		}
		
		private void imageRight_ButtonClick(object sender, EventArgs e)
		{
			this.changeHullImage(1);
		}
		
		private void nameInput_KeyPress(object sender, KeyPressEventArgs e)
		{
			this.automaticName = false;
		}
		
		private void nameInput_TextChanged(object sender, EventArgs e)
		{
			controller.Name = nameInput.Text;
			this.checkValidity();
		}
		
		private void hasIsDrive_CheckedChanged(object sender, EventArgs e)
		{
			this.controller.HasIsDrive = this.hasIsDrive.Checked;
			this.isDriveImage.Visible = this.hasIsDrive.Checked;
			this.checkValidity();
			updateInfos();
		}
		
		private void pickShieldAction_Click(object sender, EventArgs e)
		{
			Action<ShieldInfo> selectShield = 
				x => this.controller.Shield = x;
			
			var shields = new IShipComponentType[] { new ShipComponentType<ShieldInfo>(this.context["unselectComponent"].Text(), null, null, selectShield) }.Concat(
				this.controller.Shields().Select(x => new ShipComponentType<ShieldInfo>(
				x.Name,
				x.ImagePath,
				x, selectShield
			)));
			
			using(var form = new FormPickComponent(shields))
				form.ShowDialog();
			
			if (this.controller.Shield != null)
			{
				this.pickShieldAction.Text = this.controller.Shield.Name;
				this.shieldImage.Visible = true;
				this.shieldImage.Image = ImageCache.Get[this.controller.Shield.ImagePath];
			}
			else
			{
				this.pickShieldAction.Text = this.context["noShield"].Text();
				this.shieldImage.Visible = false;
				this.shieldImage.Image = null;
			}
			updateInfos();
		}

		private void addEquipAction_Click(object sender, EventArgs e)
		{
			var equipmnet = this.controller.SpecialEquipment().Where(x => !this.controller.HasSpecialEquip(x)).Select(x => new ShipComponentType<SpecialEquipInfo>(
				x.Name,
				x.ImagePath,
				x, addSpecialEquip
			));

			using (var form = new FormPickComponent(equipmnet))
				form.ShowDialog();

			updateInfos();
		}

		private void removeEquipAction_Click(object sender, EventArgs e)
		{
			if (this.equipmentList.SelectedItem == null || !(this.equipmentList.SelectedItem is ShipEquipmentItem))
				return;
			
			var selectedItem = this.equipmentList.SelectedItem as ShipEquipmentItem;
			
			this.equipmentAction.SpecialEquipmentAction = x => this.controller.SpecialEquipSetAmount(x, 0);
			selectedItem.Data.Dispatch();
			
			this.equipmentList.Controls.Remove(selectedItem); //TODO(v0.5) remove "special equipment" label if there is no special equipment
		}

		private void moreEquipAction_Click(object sender, EventArgs e)
		{
			if (this.equipmentList.SelectedItem == null || !(this.equipmentList.SelectedItem is ShipEquipmentItem))
				return;
			
			var selectedItem = this.equipmentList.SelectedItem as ShipEquipmentItem;
			
			this.equipmentAction.SpecialEquipmentAction = x => 
			{
				this.controller.SpecialEquipSetAmount(x, this.controller.SpecialEquipCount(x) + 1);
				selectedItem.Amount = this.controller.SpecialEquipCount(x);
			};
			selectedItem.Data.Dispatch();
		}

		private void lessEquipAction_Click(object sender, EventArgs e)
		{
			if (this.equipmentList.SelectedItem == null || !(this.equipmentList.SelectedItem is ShipEquipmentItem))
				return;
			
			var selectedItem = this.equipmentList.SelectedItem as ShipEquipmentItem;
			
			this.equipmentAction.SpecialEquipmentAction = x => 
			{
				this.controller.SpecialEquipSetAmount(x, this.controller.SpecialEquipCount(x) - 1);
				
				if (this.controller.SpecialEquipCount(x) == 0)
					this.equipmentList.Controls.Remove(selectedItem); //TODO(v0.5) remove "special equipment" label if there is no special equipment
				else
					selectedItem.Amount = this.controller.SpecialEquipCount(x);
			};
			selectedItem.Data.Dispatch();
		}

		private void customAmountAction_Click(object sender, EventArgs e)
		{
			if (this.equipmentList.SelectedItem == null || !(this.equipmentList.SelectedItem is ShipEquipmentItem))
				return;
			
			var selectedItem = this.equipmentList.SelectedItem as ShipEquipmentItem;
		}
	}
}
