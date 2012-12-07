﻿namespace Stareater.GUI
{
	partial class FormNewGame
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.setupPlayersButton = new System.Windows.Forms.Button();
			this.startButton = new System.Windows.Forms.Button();
			this.textBox4 = new System.Windows.Forms.TextBox();
			this.setupStartButton = new System.Windows.Forms.Button();
			this.setupMapButton = new System.Windows.Forms.Button();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.SuspendLayout();
			// 
			// setupPlayersButton
			// 
			this.setupPlayersButton.Location = new System.Drawing.Point(12, 12);
			this.setupPlayersButton.Name = "setupPlayersButton";
			this.setupPlayersButton.Size = new System.Drawing.Size(170, 23);
			this.setupPlayersButton.TabIndex = 0;
			this.setupPlayersButton.Text = "Setup players";
			this.setupPlayersButton.UseVisualStyleBackColor = true;
			this.setupPlayersButton.Click += new System.EventHandler(this.setupPlayersButton_Click);
			// 
			// startButton
			// 
			this.startButton.Location = new System.Drawing.Point(283, 244);
			this.startButton.Name = "startButton";
			this.startButton.Size = new System.Drawing.Size(75, 23);
			this.startButton.TabIndex = 6;
			this.startButton.Text = "Start";
			this.startButton.UseVisualStyleBackColor = true;
			// 
			// textBox4
			// 
			this.textBox4.Location = new System.Drawing.Point(188, 157);
			this.textBox4.Multiline = true;
			this.textBox4.Name = "textBox4";
			this.textBox4.ReadOnly = true;
			this.textBox4.Size = new System.Drawing.Size(170, 81);
			this.textBox4.TabIndex = 5;
			// 
			// setupStartButton
			// 
			this.setupStartButton.Location = new System.Drawing.Point(188, 128);
			this.setupStartButton.Name = "setupStartButton";
			this.setupStartButton.Size = new System.Drawing.Size(170, 23);
			this.setupStartButton.TabIndex = 4;
			this.setupStartButton.Text = "Starting population";
			this.setupStartButton.UseVisualStyleBackColor = true;
			// 
			// setupMapButton
			// 
			this.setupMapButton.Location = new System.Drawing.Point(188, 12);
			this.setupMapButton.Name = "setupMapButton";
			this.setupMapButton.Size = new System.Drawing.Size(170, 23);
			this.setupMapButton.TabIndex = 2;
			this.setupMapButton.Text = "Map";
			this.setupMapButton.UseVisualStyleBackColor = true;
			// 
			// textBox3
			// 
			this.textBox3.Location = new System.Drawing.Point(188, 41);
			this.textBox3.Multiline = true;
			this.textBox3.Name = "textBox3";
			this.textBox3.ReadOnly = true;
			this.textBox3.Size = new System.Drawing.Size(170, 81);
			this.textBox3.TabIndex = 3;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 41);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(170, 197);
			this.flowLayoutPanel1.TabIndex = 1;
			// 
			// FormNewGame
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(372, 282);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.setupPlayersButton);
			this.Controls.Add(this.startButton);
			this.Controls.Add(this.textBox4);
			this.Controls.Add(this.setupStartButton);
			this.Controls.Add(this.setupMapButton);
			this.Controls.Add(this.textBox3);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormNewGame";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Nova igra";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button setupPlayersButton;
		private System.Windows.Forms.Button startButton;
		private System.Windows.Forms.TextBox textBox4;
		private System.Windows.Forms.Button setupStartButton;
		private System.Windows.Forms.Button setupMapButton;
		private System.Windows.Forms.TextBox textBox3;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;

	}
}