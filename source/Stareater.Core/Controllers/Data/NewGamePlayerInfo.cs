﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Stareater.Players;

namespace Stareater.Controllers.Data
{
	public struct NewGamePlayerInfo
	{
		public string Name;
		public Color Color;
		public Organization Organization;

		public NewGamePlayerInfo(string Name, Color Color, Organization Organization)
		{
			this.Color = Color;
			this.Name = Name;
			this.Organization = Organization;
		}
	}
}