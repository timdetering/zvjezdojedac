﻿using System;
using System.Collections.Generic;
using Stareater.Players;
using Stareater.Utils.Collections;

namespace Stareater.Ships
{
	partial class Design
	{
		public object PrimaryEquip { get; private set; } //TODO(v0.5): make type
		public object SecondaryEquip { get; private set; } //TODO(v0.5): make type
		public object Armor { get; private set; } //TODO(v0.5): make type
		public object Shield { get; private set; } //TODO(v0.5): make type
		public Dictionary<object, int> SpecialEquip { get; private set; } //TODO(v0.5): make type
		public object Sensors { get; private set; } //TODO(v0.5): make type
		public object Thrusters { get; private set; } //TODO(v0.5): make type
		public object ISDrive { get; private set; } //TODO(v0.5): make type
		public object Reactor { get; private set; } //TODO(v0.5): make type

		
		//public int id { get; private set; } //TODO(v0.5): might need id
		//private Dictionary<string, double> efekti = new Dictionary<string,double>(); //TODO(v0.5): might need
		//public object Hash { get; private set; } //TODO(v0.5): make type, might need
		
		private void initCost(Hull hull)
		{
			this.Cost = hull.TypeInfo.Cost.Evaluate(new Var("lvl", hull.Level).Get);
		}
		
		public string ImagePath 
		{ 
			get
			{
				return Hull.ImagePath;
			}
		}
	}
}