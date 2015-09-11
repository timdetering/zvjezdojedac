﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGenerics.DataStructures.Mathematical;
using Stareater.Controllers.Views.Ships;
using Stareater.Galaxy;
using Stareater.Ships;
using Stareater.Ships.Missions;
using Stareater.Utils;
using Stareater.Controllers.Data;

namespace Stareater.Controllers
{
	public class FleetController
	{
		private Game game;
		private GalaxyObjects mapObjects;
		private IVisualPositioner visualPositoner;
		
		public FleetInfo Fleet { get; private set; }

		private Dictionary<Design, long> selection = new Dictionary<Design, long>();
		private double eta = 0;
		private List<Vector2D> simulationWaypoints = null;
		
		internal FleetController(FleetInfo fleet, Game game, GalaxyObjects mapObjects, IVisualPositioner visualPositoner)
		{
			this.Fleet = fleet;
			this.game = game;
			this.mapObjects = mapObjects;
			this.visualPositoner = visualPositoner;
			
			if (!this.Fleet.AtStar) {
				var mission = this.Fleet.Mission as MoveMissionInfo;
				this.simulationWaypoints = new List<Vector2D>(mission.Waypoints);
				this.calcEta();
			}
		}
		
		public bool Valid
		{
			get { return this.game.States.Fleets.Contains(this.Fleet.FleetData); }
		}
		
		public IEnumerable<ShipGroupInfo> ShipGroups
		{
			get
			{
				return this.Fleet.FleetData.Ships.Select(x => new ShipGroupInfo(x));
			}
		}
		
		public bool CanMove
		{
			get 
			{ 
				foreach(var design in this.selection.Keys)
					if (design.IsDrive == null)
						return false;
				
				return true;
			}
		}
		
		public double Eta
		{
			get { return this.eta; }
		}
		
		public IList<Vector2D> SimulationWaypoints
		{
			get { return this.simulationWaypoints; }
		}
		
		public void DeselectGroup(ShipGroupInfo group)
		{
			selection.Remove(group.Data.Design);
			
			if (!this.CanMove)
				this.simulationWaypoints = null;
		}
		
		public void SelectGroup(ShipGroupInfo group, long quantity)
		{
			quantity = Methods.Clamp(quantity, 0, this.Fleet.FleetData.Ships.Design(group.Data.Design).Quantity);
			selection[group.Data.Design] = quantity;
			
			if (selection[group.Data.Design] <= 0)
				selection.Remove(group.Data.Design);
			
			if (!this.CanMove)
				this.simulationWaypoints = null;
		}
		
		public FleetController Send(IEnumerable<Vector2D> waypoints)
		{
			if (!this.Fleet.AtStar)
				return this;
			
			if (this.CanMove && waypoints != null && waypoints.LastOrDefault() != this.Fleet.FleetData.Position)
				return this.giveOrder(new MoveMission(waypoints.ToArray()));
			else if (this.game.States.Stars.AtContains(this.Fleet.FleetData.Position))
				return this.giveOrder(new StationaryMission(this.game.States.Stars.At(this.Fleet.FleetData.Position)));
			
			return this;
		}
		
		public void SimulateTravel(StarData destination)
		{
			if (!this.Fleet.AtStar)
				return;
			
			this.simulationWaypoints = new List<Vector2D>();
			//TODO(later): find shortest path
			//TODO(v0.5) prevent changing destination midfilght
			this.simulationWaypoints.Add(this.Fleet.FleetData.Position);
			this.simulationWaypoints.Add(destination.Position);
			
			this.calcEta();
		}

		
		private FleetInfo addFleet(ICollection<Stareater.Galaxy.Fleet> shipOrders, Fleet newFleet)
		{
			var similarFleet = shipOrders.FirstOrDefault(x => x.Mission == newFleet.Mission);
			
			if (similarFleet != null) {
				foreach(var shipGroup in newFleet.Ships)
					if (similarFleet.Ships.DesignContains(shipGroup.Design))
						similarFleet.Ships.Design(shipGroup.Design).Quantity += shipGroup.Quantity;
					else
						similarFleet.Ships.Add(shipGroup);
				
				var fleetInfo = this.mapObjects.InfoOf(similarFleet, this.Fleet.AtStar, this.visualPositoner);
				if (fleetInfo == null) {
					fleetInfo = new FleetInfo(similarFleet, this.Fleet.AtStar, this.visualPositoner);
					this.mapObjects.Add(fleetInfo);
				}
				
				return fleetInfo;
			}
			else {
				shipOrders.Add(newFleet);
				
				var fleetInfo = new FleetInfo(newFleet, this.Fleet.AtStar, this.visualPositoner);
				this.mapObjects.Add(fleetInfo);
				
				return fleetInfo;
			}
		}
		
		private void calcEta()
		{
			var playerProc = game.Derivates.Players.Of(this.Fleet.Owner.Data);
			double baseSpeed = this.selection.Keys.
				Aggregate(double.MaxValue, (s, x) => Math.Min(playerProc.DesignStats[x].GalaxySpeed, s));
							
			//TODO(v0.5) loop through all waypoints
			var endStar = game.States.Stars.At(this.simulationWaypoints[1]);
			var speed = baseSpeed;
			
			if (this.Fleet.AtStar)
			{
				var startStar = game.States.Stars.At(this.simulationWaypoints[0]);
				if (game.States.Wormholes.At(startStar).Intersect(game.States.Wormholes.At(endStar)).Any())
					speed += 0.5; //TODO(later) consider making moddable
			}
			else
			{
				var mission = this.Fleet.Mission as MoveMissionInfo;
				var startStar = game.States.Stars.At(mission.Waypoints[0]);
				if (game.States.Wormholes.At(startStar).Intersect(game.States.Wormholes.At(endStar)).Any())
					speed += 0.5; //TODO(later) consider making moddable
			}
			
			var distance = (this.Fleet.FleetData.Position - this.simulationWaypoints[this.simulationWaypoints.Count - 1]).Magnitude();
			eta = distance > 0 ? distance / speed : 0;
		}
		
		private FleetController giveOrder(AMission newMission)
		{
			if (this.selection.Count == 0 || newMission == this.Fleet.FleetData.Mission)
				return this;
			
			//create regroup order if there is none
			HashSet<Fleet> shipOrders;
			if (!this.Fleet.FleetData.Owner.Orders.ShipOrders.ContainsKey(this.Fleet.FleetData.Position)) {
				shipOrders = new HashSet<Fleet>();
				this.Fleet.FleetData.Owner.Orders.ShipOrders.Add(this.Fleet.FleetData.Position, shipOrders);
			}
			else
				shipOrders = this.Fleet.FleetData.Owner.Orders.ShipOrders[this.Fleet.FleetData.Position];
			
			//remove current fleet from regroup
			shipOrders.Remove(this.Fleet.FleetData);
			this.mapObjects.Remove(this.Fleet);
			
			//add new fleet
			var newFleet = new Fleet(this.Fleet.FleetData.Owner, this.Fleet.FleetData.Position, newMission);
			foreach(var selectedGroup in this.selection)
				newFleet.Ships.Add(new ShipGroup(selectedGroup.Key, selectedGroup.Value));
			
			var newFleetInfo = this.addFleet(shipOrders, newFleet);
			
			//add old fleet remains
			var oldFleet = new Fleet(this.Fleet.FleetData.Owner, this.Fleet.FleetData.Position, this.Fleet.FleetData.Mission);
			foreach(var group in this.Fleet.FleetData.Ships) 
				if (this.selection.ContainsKey(group.Design) && group.Quantity - this.selection[group.Design] > 0)
					oldFleet.Ships.Add(new ShipGroup(group.Design, group.Quantity - this.selection[group.Design]));
			if (oldFleet.Ships.Count > 0)
				this.addFleet(shipOrders, oldFleet);

			return new FleetController(
				newFleetInfo, 
				this.game,
				this.mapObjects,
				this.visualPositoner
			);
		}
	}
}