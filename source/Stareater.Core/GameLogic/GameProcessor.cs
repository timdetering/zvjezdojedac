﻿using System;
using System.Collections.Generic;
using System.Linq;
using NGenerics.DataStructures.Mathematical;
using Stareater.GameData;
using Stareater.Players;
using Stareater.Ships;
using Stareater.Ships.Missions;
using Stareater.Galaxy;

namespace Stareater.GameLogic
{
	class GameProcessor
	{
		private readonly MainGame game;
		private readonly List<FleetMovement> fleetMovement = new List<FleetMovement>();
		private readonly Queue<SpaceBattleGame> conflicts = new Queue<SpaceBattleGame>();
		private readonly Queue<Player[]> audiences = new Queue<Player[]>();

		public GameProcessor(MainGame game)
		{
			this.game = game;
		}

		public bool IsOver
		{
			get
			{
				 //TODO(later) end game by leaving stareater
				return game.States.Colonies.Select(x => x.Owner).Distinct().Count() <= 1;
			}
		}
		
		#region Turn processing
		public void ProcessPrecombat()
		{
			this.CalculateBaseEffects();
			this.CalculateSpendings();
			this.CalculateDerivedEffects();
			this.commitFleetOrders();

			this.game.States.Reports.Clear();
			foreach (var playerProc in this.game.MainPlayers.Select(x => this.game.Derivates.Of(x)))
				playerProc.ProcessPrecombat(
					this.game.Statics,
					this.game.States,
					this.game.Derivates
				);
			this.game.Derivates.Natives.ProcessPrecombat(this.game.Statics, this.game.States, this.game.Derivates); 
			//TODO(v0.6) process natives postcombat
			
			this.moveShips();
			this.detectConflicts();
			this.enqueueAudiences();
		}

		public void ProcessPostcombat()
		{
			foreach (var star in this.game.States.Stars)
			{
				foreach (var trait in star.Traits)
					trait.Effect.PostcombatApply(this.game.States, this.game.Statics);
				star.Traits.ApplyPending();

				foreach(var planet in this.game.States.Planets.At[star])
				{
					foreach (var trait in planet.Traits)
						trait.Effect.PostcombatApply(this.game.States, this.game.Statics);
					planet.Traits.ApplyPending();
				}
			}

			this.doColonization();
			this.mergeFleets();
			
			foreach (var playerProc in this.game.MainPlayers.Select(x => this.game.Derivates.Of(x)))
				playerProc.ProcessPostcombat(this.game.Statics, this.game.States, this.game.Derivates);

			this.doRepairs();

			this.CalculateBaseEffects();
			this.CalculateSpendings();
			this.CalculateDerivedEffects();
			
			this.game.Turn++;
		}
		#endregion

		#region Derived stats calculation
		public void CalculateBaseEffects()
		{
			foreach (var stellaris in this.game.Derivates.Stellarises)
				stellaris.CalculateBaseEffects();
			foreach (var colonyProc in this.game.Derivates.Colonies)
				colonyProc.CalculateBaseEffects(this.game.Statics, this.game.Derivates.Of(colonyProc.Owner));
		}

		public void CalculateSpendings()
		{
			foreach (var colonyProc in this.game.Derivates.Colonies)
				colonyProc.CalculateSpending(
					this.game.Statics,
					this.game.Derivates.Of(colonyProc.Owner)
				);

			foreach (var stellaris in this.game.Derivates.Stellarises)
				stellaris.CalculateSpending(
					this.game.Derivates.Of(stellaris.Owner),
					this.game.Derivates.Colonies.At[stellaris.Location]
				);

			foreach (var player in this.game.Derivates.Players) {
				player.CalculateDevelopment(
					this.game.Statics,
					this.game.States,
					this.game.Derivates.Colonies.OwnedBy[player.Player]
				);
				player.CalculateResearch(
					this.game.Statics,
					this.game.States,
					this.game.Derivates.Colonies.OwnedBy[player.Player]
				);
			}
		}

		public void CalculateDerivedEffects()
		{
			foreach (var colonyProc in this.game.Derivates.Colonies)
				colonyProc.CalculateDerivedEffects(this.game.Statics, this.game.Derivates.Of(colonyProc.Owner));
		}
		#endregion
		
		#region Conflict cycling
		public bool HasConflict
		{
			get
			{
				return this.conflicts.Count != 0;
			}
		}
		
		public SpaceBattleGame NextConflict()
		{
			return this.conflicts.Dequeue();
		}

		public void ConflictResolved(SpaceBattleGame battleGame)
		{
			//TODO(later) decide what to do with retreated ships, send them to nearest fiendly system?
			foreach(var unit in battleGame.Combatants.Concat(battleGame.Retreated))
			{
				unit.Ships.Damage = this.game.Derivates.Of(unit.Owner).DesignStats[unit.Ships.Design].HitPoints - unit.HitPoints;
				var fleet = new Fleet(unit.Owner, battleGame.Location, new LinkedList<AMission>());
				fleet.Ships.Add(unit.Ships);
				
				this.game.States.Fleets.Add(fleet);
			}
		}
		#endregion

		#region Diplomacy
		public bool HasAudience 
		{
			get
			{
				return this.audiences.Count != 0;
			}
		}

		public Player[] NextAudience()
		{
			return this.audiences.Dequeue();
		}

		public void AudienceConcluded(Player[] participants, HashSet<Treaty> treaties)
		{
			participants[0].Orders.AudienceRequests.Remove(Array.IndexOf(this.game.MainPlayers, participants[1]));
			
			foreach(var oldTreaty in this.game.States.Treaties.Of[participants[0]].Where(x => x.Party2 == participants[1]).ToList())
				this.game.States.Treaties.Remove(oldTreaty);
			this.game.States.Treaties.Add(treaties);
		}
		#endregion

		private void commitFleetOrders()
		{
			foreach (var player in this.game.AllPlayers)
			{
				foreach (var order in player.Orders.ShipOrders) 
				{
					var totalDamage = new Dictionary<Design, double>();
					var totalUpgrades = new Dictionary<Design, double>();
					var shipCount = new Dictionary<Design, double>();
					foreach (var fleet in this.game.States.Fleets.At[order.Key].Where(x => x.Owner == player))
					{
						foreach(var ship in fleet.Ships)
						{
							if (!shipCount.ContainsKey(ship.Design))
							{
								shipCount.Add(ship.Design, 0);
								totalDamage.Add(ship.Design, 0);
								totalUpgrades.Add(ship.Design, 0);
							}
							
							totalDamage[ship.Design] += ship.Damage;
							totalUpgrades[ship.Design] += ship.UpgradePoints;
							shipCount[ship.Design] += ship.Quantity;
						}
						
						this.game.States.Fleets.PendRemove(fleet);
					}
					this.game.States.Fleets.ApplyPending();
					
					foreach (var fleet in order.Value)
					{
						foreach(var ship in fleet.Ships)
						{
							ship.Damage = totalDamage[ship.Design] * ship.Quantity / shipCount[ship.Design];
							ship.UpgradePoints = totalUpgrades[ship.Design] * ship.Quantity / shipCount[ship.Design]; //TODO(v0.6) test
						}
						this.game.States.Fleets.Add(fleet);
					}
				}

				player.Orders.ShipOrders.Clear();
			}
		}

		private void detectConflicts()
		{
			var visits = new Dictionary<Vector2D, ICollection<FleetMovement>>();
			var conflictPositions = new Dictionary<Vector2D, double>();
			var decidedFleet = new HashSet<Fleet>();
			
			foreach(var step in this.fleetMovement.OrderBy(x => x.ArrivalTime))
	        {
				if (!visits.ContainsKey(step.LocalFleet.Position))
					visits.Add(step.LocalFleet.Position, new List<FleetMovement>());
				
				if (decidedFleet.Contains(step.OriginalFleet) || visits[step.LocalFleet.Position].Any(x => x.OriginalFleet == step.OriginalFleet))
					continue;
				
				var fleets = visits[step.LocalFleet.Position];
				fleets.Add(step);
				
				if (!game.States.Stars.At.Contains(step.LocalFleet.Position))
					continue; //TODO(later) no deepspace interception for now
				var star = game.States.Stars.At[step.LocalFleet.Position];
				
				//TODO(v0.6) doesn't detect attacking undefended colonies
				var players = new HashSet<Player>(fleets.Where(x => x.ArrivalTime < step.ArrivalTime).Select(x => x.OriginalFleet.Owner));
				players.UnionWith(game.States.Colonies.AtStar[star].Select(x => x.Owner));
				players.Remove(step.LocalFleet.Owner);

				bool inConflict = step.LocalFleet.Owner == game.StareaterOrganelles ? 
					players.Any() :
					game.States.Treaties.Of[step.LocalFleet.Owner].Any(x => players.Contains(x.Party1) || players.Contains(x.Party2));

				if (inConflict)
				{
					if (!conflictPositions.ContainsKey(step.LocalFleet.Position))
						conflictPositions.Add(step.LocalFleet.Position, step.ArrivalTime);
					decidedFleet.UnionWith(fleets.Where(x => x.ArrivalTime < step.ArrivalTime).Select(x => x.OriginalFleet));
				}
	        }
			
			this.conflicts.Clear();
			foreach(var position in conflictPositions.OrderBy(x => x.Value))
				if (this.game.States.Stars.At.Contains(position.Key))
					conflicts.Enqueue(new SpaceBattleGame(position.Key, visits[position.Key], position.Value, this.game));
			//TODO(later) deep space interception
			
			//FIXME(later) could make "fleet trail" if fleet visits multiple stars in the same turn
			this.game.States.Fleets.Clear();
			foreach(var fleet in visits.Where(x => !conflictPositions.ContainsKey(x.Key)).SelectMany(x => x.Value))
				this.game.States.Fleets.Add(fleet.LocalFleet);
		}

		private void doColonization()
		{
			foreach(var project in this.game.States.ColonizationProjects)
			{
				var playerProc = this.game.Derivates.Of(project.Owner);
				bool colonyExists = this.game.States.Colonies.AtPlanet.Contains(project.Destination);
				
				var colonizers = this.game.States.Fleets.At[project.Destination.Star.Position].Where(
					x => 
					{
						if (x.Owner != project.Owner || x.Missions.Count == 0)
							return false;
						
						var mission = x.Missions.First.Value as ColonizationMission;
						return mission != null && mission.Target == project.Destination;
					});
					
				var arrivedPopulation = colonizers.SelectMany(x => x.Ships).Sum(x => playerProc.DesignStats[x.Design].ColonizerPopulation * x.Quantity);
				var colonizationTreshold = this.game.Statics.ColonyFormulas.ColonizationPopulationThreshold.Evaluate(null);
				
				if (!colonyExists && arrivedPopulation >= colonizationTreshold)
				{
					var colony = new Colony(0, project.Destination, project.Owner);
					var colonyProc = new ColonyProcessor(colony);
					colonyProc.CalculateBaseEffects(this.game.Statics, this.game.Derivates.Players.Of[colony.Owner]);
					
					foreach(var fleet in colonizers)
					{
						foreach(var shipGroup in fleet.Ships)
						{
							var shipStats = playerProc.DesignStats[shipGroup.Design];
							var groupPopulation = shipStats.ColonizerPopulation * shipGroup.Quantity;
							var landingLimit = (long)Math.Ceiling((colonizationTreshold - colony.Population) / shipStats.ColonizerPopulation);
							var shipsLanded = Math.Min(shipGroup.Quantity, landingLimit);
							
							colonyProc.AddPopulation(shipsLanded * shipStats.ColonizerPopulation);
							
							foreach(var building in shipStats.ColonizerBuildings)
								if (colony.Buildings.ContainsKey(building.Key))
									colony.Buildings[building.Key] += building.Value * shipGroup.Quantity;
								else
									colony.Buildings.Add(building.Key, building.Value * shipGroup.Quantity);	
							
							shipGroup.Quantity -= shipsLanded;
							if (shipGroup.Quantity < 1)
								fleet.Ships.PendRemove(shipGroup);
						}
						
						fleet.Ships.ApplyPending();
						if (fleet.Ships.Count == 0)
							game.States.Fleets.PendRemove(fleet);
					}
					game.States.Fleets.ApplyPending();
					
					this.game.States.Colonies.Add(colony);
					this.game.Derivates.Colonies.Add(colonyProc);

					if (this.game.States.Stellarises.At[project.Destination.Star].All(x => x.Owner != project.Owner))
					{
						var stellaris = new StellarisAdmin(project.Destination.Star, project.Owner);
						this.game.States.Stellarises.Add(stellaris);
						this.game.Derivates.Stellarises.Add(new StellarisProcessor(stellaris));
					}
				}
				
				if (colonyExists || !colonyExists && arrivedPopulation >= colonizationTreshold)
				{
					project.Owner.Orders.ColonizationOrders.Remove(project.Destination);
					this.game.States.ColonizationProjects.PendRemove(project);
				}
			}
			
			this.game.States.ColonizationProjects.ApplyPending();
		}

		private void doRepairs()
		{
			foreach(var stellaris in this.game.States.Stellarises)
			{
				var player = stellaris.Owner;
				var localFleet = this.game.States.Fleets.
					At[stellaris.Location.Star.Position].
					Where(x => x.Owner == player).ToList();
				var repairPoints = this.game.Derivates.Colonies.
					At[stellaris.Location.Star].
					Where(x => x.Owner == player).
					Aggregate(0.0, (sum, x) => sum + x.RepairPoints);
				
				var designStats = this.game.Derivates.Of(player).DesignStats;
				var repairCostFactor = this.game.Statics.ShipFormulas.RepairCostFactor;
				var damagedShips = localFleet.SelectMany(x => x.Ships).Where(x => x.Damage > 0);
				var totalNeededRepairPoints = damagedShips.Sum(x => repairCostFactor * x.Damage * x.Design.Cost / designStats[x.Design].HitPoints);
				
				foreach(var shipGroup in damagedShips)
				{
					var repirPerHp = repairCostFactor * shipGroup.Design.Cost / designStats[shipGroup.Design].HitPoints;
					var fullRepairCost = shipGroup.Damage * repirPerHp;
					var investment = repairPoints * fullRepairCost / totalNeededRepairPoints;
					
					if (fullRepairCost < investment)
					{
						shipGroup.Damage = 0;
						investment -= investment - fullRepairCost;
					}
					else
						shipGroup.Damage += investment / repirPerHp;
					
					repairPoints -= investment;
					totalNeededRepairPoints -= fullRepairCost;
				}

				var refitOrders = player.Orders.RefitOrders;
				var refitCosts = this.game.Derivates.Of(player).RefitCosts;
				var groupsFrom = new Dictionary<ShipGroup, Fleet>();

				foreach(var fleet in localFleet)
					foreach(var shipGroup in fleet.Ships)
						groupsFrom.Add(shipGroup, fleet);
				var upgradableShips = localFleet.
					SelectMany(x => x.Ships).
					Where(x => refitOrders.ContainsKey(x.Design) && refitOrders[x.Design] != null).ToList();
				var totalNeededUpgradePoints = upgradableShips.
					Select(x => refitCosts[x.Design][refitOrders[x.Design]] * x.Quantity - x.UpgradePoints).
					Aggregate(0.0, (sum, x) => x > 0 ? sum + x : sum);
				
				foreach(var shipGroup in upgradableShips)
				{
					var refitTo = refitOrders[shipGroup.Design];
					var refitCost = refitCosts[shipGroup.Design][refitOrders[shipGroup.Design]];
					var fullUpgradeCost = refitCost * shipGroup.Quantity - shipGroup.UpgradePoints;
					var investment = repairPoints * fullUpgradeCost / totalNeededUpgradePoints;
					
					if (fullUpgradeCost < investment)
					{
						shipGroup.UpgradePoints = refitCost * shipGroup.Quantity;
						investment -= investment - fullUpgradeCost;
					}
					else
						shipGroup.UpgradePoints += investment;
					
					repairPoints -= investment;
					totalNeededUpgradePoints -= fullUpgradeCost;
					var upgradedShips = refitCost > 0 ? (long)Math.Floor(shipGroup.UpgradePoints / refitCost) : shipGroup.Quantity;
					
					if (upgradedShips > 0)
					{
						shipGroup.Quantity -= upgradedShips;
						shipGroup.UpgradePoints -= upgradedShips * refitCost;
						
						var fleet = groupsFrom[shipGroup];
						var existingGroup = fleet.Ships.FirstOrDefault(x => x.Design == refitTo);
						
						if (shipGroup.Quantity <= 0)
							fleet.Ships.Remove(shipGroup);
						
						if (existingGroup != null)
							existingGroup.Quantity += upgradedShips;
						else
							fleet.Ships.Add(new ShipGroup(refitTo, upgradedShips, 0, 0));
					}
				}
			}
		}

		private void enqueueAudiences()
		{
			foreach(var player in this.game.MainPlayers)
			{
				foreach(var audience in player.Orders.AudienceRequests)
					this.audiences.Enqueue(new [] { player, this.game.MainPlayers[audience] }); //TODO(v0.6) eliminate duplicates
				
				player.Orders.AudienceRequests.Clear();
			}
		}
		
		private void mergeFleets()
		{
			var filter = new InvalidMissionVisitor(this.game);
			foreach(var fleet in this.game.States.Fleets)
			{
				var newfleet = filter.Check(fleet);
				
				if (newfleet != null)
				{
					this.game.States.Fleets.PendAdd(newfleet);
					this.game.States.Fleets.PendRemove(fleet);
				}
			}
			this.game.States.Fleets.ApplyPending();
			
			/*
 			 * Aggregate fleets, if there are multiple fleets of the same owner 
			 * at the same star with same missions, merge them to one fleet.
 			 */
			foreach(var star in game.States.Stars) 
			{
				var perPlayerFleets = this.game.States.Fleets.At[star.Position].GroupBy(x => x.Owner);
				foreach(var fleets in perPlayerFleets) 
				{
					var missionGroups = new Dictionary<LinkedList<AMission>, List<Fleet>>();

					foreach (var fleet in fleets)
					{
						var missionKey = missionGroups.Keys.FirstOrDefault(x => x.SequenceEqual(fleet.Missions));
						
						if (missionKey == null)
						{
							missionKey = fleet.Missions;
							missionGroups.Add(missionKey, new List<Fleet>());
						}
						missionGroups[missionKey].Add(fleet);
					}

					foreach (var grouping in missionGroups.Where(x => x.Value.Count > 1))
					{
						var newFleet = new Fleet(grouping.Value[0].Owner, grouping.Value[0].Position, grouping.Key);
						foreach (var fleet in grouping.Value)
						{
							this.game.States.Fleets.PendRemove(fleet);
							foreach (var ship in fleet.Ships)
								if (newFleet.Ships.WithDesign.Contains(ship.Design))
									newFleet.Ships.WithDesign[ship.Design].Quantity += ship.Quantity;
								else
									newFleet.Ships.Add(new ShipGroup(ship.Design, ship.Quantity, ship.Damage, ship.UpgradePoints));
						}
						this.game.States.Fleets.PendAdd(newFleet);
					}
				}
			}
			this.game.States.Fleets.ApplyPending();
		}
		
		private void moveShips()
		{
			this.fleetMovement.Clear();
			
			foreach (var fleet in this.game.States.Fleets)
			{
				var fleetProcessor = new FleetProcessingVisitor(fleet, game);
				this.fleetMovement.AddRange(fleetProcessor.Run());
			}
		}
	}
}
