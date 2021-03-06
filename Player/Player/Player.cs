﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Player {
	static void Main(string[] args) {

		List<Tuple<int, int, int>> factoriesDistances = new List<Tuple<int, int, int>>();
		string[] inputs;
		int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
		int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories

		Graph map = new Graph(factoryCount);

		for (int i = 0; i < linkCount; i++) {
			inputs = Console.ReadLine().Split(' ');
			int factory1 = int.Parse(inputs[0]);
			int factory2 = int.Parse(inputs[1]);
			int distance = int.Parse(inputs[2]);

			map.AddConnection(factory1, factory2, distance);

			Tuple<int, int, int> myTuple = Tuple.Create(factory1, factory2, distance);
			factoriesDistances.Add(myTuple);
		}

		bool firstTurn = true;
		// game loop
		while (true) {
			GameState game = null;
			List<Entity> entities = new List<Entity>();

			int weakestFactory = -1;
			int weakestFactoryCount = int.MaxValue;
			
			int mostProductiveUnownedFactory = -1;
			int bestUnownedFactoryProduction = -1;
			int bestUnownedFactoryCyborgCount = int.MaxValue;
			int bestUnownedFactoryDistance = int.MaxValue;

			int myBestFactory = -1;
			int myBestFactoryCount = -1;

			int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
			for (int i = 0; i < entityCount; i++) {
				inputs = Console.ReadLine().Split(' ');
				int entityId = int.Parse(inputs[0]);
				string entityType = inputs[1];
				int owner = int.Parse(inputs[2]);
				int arg2 = int.Parse(inputs[3]);  //identifier of the factory from where the troop leaves || number of cyborgs in the factory 
				int arg3 = int.Parse(inputs[4]);  //identifier of the factory targeted by the troop || factory production (between 0 and 3) 
				int arg4 = int.Parse(inputs[5]);  //number of cyborgs in the troop (positive integer)	
				int arg5 = int.Parse(inputs[6]);  //remaining number of turns before the troop arrives (positive integer)

				if (entityType == "FACTORY") {
					Factory factory = new Factory(entityId, owner, arg2, arg3);
					entities.Add(factory);
				}
				if (entityType == "TROOP") {
					Troops troops = new Troops(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.First(e => e.EntityId == arg3), arg4, arg5);
					entities.Add(troops);
				}
				if(entityType == "BOMB") {
					Bomb bomb = new Bomb(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.FirstOrDefault(e => e.EntityId == arg3), arg4);
					entities.Add(bomb);
				}
			}
			game = new GameState(map, entities);

			CommandBuilder action = new CommandBuilder();

			foreach(var ownedFactory in game.Factories.Where(f=>f.Owner == Players.Me).OrderBy(f => f.Production) ) {
				if(ownedFactory.CyborgCount >= 20 && ownedFactory.Production < 3) {
					action.AppendPowerup(ownedFactory.EntityId);
				}
				if (ownedFactory.CyborgCount > myBestFactoryCount) {
					myBestFactory = ownedFactory.EntityId;
					myBestFactoryCount = ownedFactory.CyborgCount;
				}
			}

			foreach (var unownedFactory in game.Factories.Where(f => f.Owner != Players.Me)) {
				if (unownedFactory.CyborgCount < weakestFactoryCount) {
					weakestFactory = unownedFactory.EntityId;
					weakestFactoryCount = unownedFactory.CyborgCount;
				}
				if(factoriesDistances.FirstOrDefault(t => t.Item1 == Math.Min(myBestFactory, unownedFactory) && t.Item2 == Math.Max(myBestFactory, unownedFactory)) == null) {
					continue;
				}
				if (unownedFactory.Production >= bestUnownedFactoryProduction ){
					int distanceToFactory = factoriesDistances.First(t => t.Item1 == Math.Min(myBestFactory, unownedFactory) && t.Item2 == Math.Max(myBestFactory, unownedFactory)).Item3;
					if (distanceToFactory < bestUnownedFactoryDistance && distanceToFactory < 9) {
						mostProductiveUnownedFactory = unownedFactory.EntityId;
						bestUnownedFactoryProduction = unownedFactory.Production;
						bestUnownedFactoryCyborgCount = unownedFactory.CyborgCount;
						if (unownedFactory.Production == bestUnownedFactoryProduction && unownedFactory.CyborgCount < bestUnownedFactoryCyborgCount) {
							mostProductiveUnownedFactory = unownedFactory.EntityId;
							bestUnownedFactoryProduction = unownedFactory.Production;
							bestUnownedFactoryCyborgCount = unownedFactory.CyborgCount;
						}
					}
				}
			}
			// Write an action using Console.WriteLine()
			// To debug: Console.Error.WriteLine("Debug messages...");
			if (firstTurn) {
				int troopsAvailable = myBestFactoryCount - 1;
				var factoriesToAttack = game.Factories
					.Where(f => f.Owner != Players.Me)
					.Where(f => f.Production >= 1)
					.OrderBy(f => factoriesDistances
						.First(d => d.Item1 == Math.Min(myBestFactory, f) && d.Item2 == Math.Max(myBestFactory, f)).Item3
					).ThenBy(f => f.CyborgCount);

				foreach (var factory in factoriesToAttack) {
					action.AppendMove(myBestFactory, factory, factory.CyborgCount + 1);
					troopsAvailable = troopsAvailable - factory.CyborgCount + 1;
					if(troopsAvailable <= 0) {
						break;
					}
				}


				firstTurn = false;
			}

			if (mostProductiveUnownedFactory != -1) { //If there is no factory to conquer, just wait. We're doing good
				if(bestUnownedFactoryCyborgCount >= 10 && game.Factories.First(f=>f==mostProductiveUnownedFactory).Owner == Players.Opponent) {
					action.AppendBomb(myBestFactory, mostProductiveUnownedFactory);
				}
				else if(Math.Min(myBestFactoryCount - 1, 4) > 0) {
					//Console.WriteLine(string.Format("MOVE {0} {1} {2}", myBestFactory, mostProductiveUnownedFactory, Math.Min(myBestFactoryCount - 1, 4)));
					action.AppendMove(myBestFactory, mostProductiveUnownedFactory, Math.Min(myBestFactoryCount - 1, 4));
				}
			}

			Console.WriteLine(action.Result);
		}
	}
}
#region Game Data Structures
public class Odinoo {

	public string Think(GameState game) {
		CommandBuilder action = new CommandBuilder();
		//if (weakestFactory != -1) { //If there is no factory to conquer, just wait. We're doing good
		//	if (Math.Min(myBestFactoryCount - 1, 4) > 0) {
		//		//Console.WriteLine(string.Format("MOVE {0} {1} {2}", myBestFactory, mostProductiveUnownedFactory, Math.Min(myBestFactoryCount - 1, 4)));
		//		action.AppendMove(myBestFactory, mostProductiveUnownedFactory, Math.Min(myBestFactoryCount - 1, 4));
		//	}
		//}

		return action.Result;
	}

}

public class Graph {

	private int[][] adjacencyMatrix;
	private int noOfNodes;

	public int this[int x, int y] {
		get { return adjacencyMatrix[x][y]; }
		private set { adjacencyMatrix[x][y] = adjacencyMatrix[y][x] = value; }
	}

	public Graph(int noOfNodes) {
		this.noOfNodes = noOfNodes;
		adjacencyMatrix = new int[noOfNodes][];
		for (int i = 0; i < adjacencyMatrix.Length; i++) { adjacencyMatrix[i] = new int[noOfNodes]; }
	}

	public void AddConnection(int node1, int node2, int cost) {
		Debug.Assert(node1 < noOfNodes);
		Debug.Assert(node2 < noOfNodes);
		Debug.Assert(cost > 0);

		adjacencyMatrix[node1][node2] = adjacencyMatrix[node2][node1] = cost;
	}
}

public enum Players {
	Neutral = 0, Me = 1, Opponent = 2
}	  

public class CommandBuilder {
					  
	public string Result { get; private set; }

	public CommandBuilder() {
		Result = "WAIT";	
	}
									 
	//public void AppendMove(Factory start, Factory target, int count) {
	//	Result += string.Format("MOVE {0} {1} {2};", start.EntityId, target.EntityId, count); 		
	//}

	public void AppendMove(int startId, int targetId, int count) {
		Result += string.Format(";MOVE {0} {1} {2}", startId, targetId, count);
	}

	public void AppendBomb(int startId, int targetId) {
		Result += string.Format(";BOMB {0} {1}", startId, targetId);
	}

	public void AppendPowerup(int factoryId) {
		Result += ";INC " + factoryId;		
	}

} 

public class GameState {

	public List<Entity> Entities { get; private set; }
	public List<Factory> Factories { get; private set; }
	public List<Troops> Troops { get; private set; }
	public Graph MapGraph { get; private set; }

	public GameState(Graph mapGraph, List<Entity> entities) {
		this.MapGraph = mapGraph;
		this.Entities = entities;
		this.Factories = entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = entities.Where(e => e is Troops).Cast<Troops>().ToList();
	}

	public List<Factory> GetFactoriesOf(Players owner) {
		return Factories.Where(f=>f.Owner == owner).ToList();
	}

	public List<Factory> GetUnownedFactories(Players owner) {
		return Factories.Where(f => f.Owner != owner).ToList();
	}

	public GameState AdvanceInputless(int noOfTurns) {
		return AdvanceInputless_Listed(noOfTurns).Last();
	}

	public List<GameState> AdvanceInputless_Listed(int noOfTurns) {
		List<GameState> result = new List<GameState>();
		for(int i=0; i<noOfTurns; i++) {
			result.Add(AdvanceInputlessStep());
		}
		return result;

	}

	private GameState AdvanceInputlessStep() {

		//Move existing troops and bombs
		//Execute user orders
		//Produce new cyborgs in all factories
		//Solve battles
		//Make the bombs explode
		//Check end conditions


		return null;
	}

}

public abstract class Entity {

	public int EntityId { get; private set; }
	public Players Owner { get; private set; }
	
	public static implicit operator int(Entity value) {
		return value.EntityId;
	}

	public Entity(int entityId, int owner) {
		this.EntityId = entityId;
		this.Owner = (Players)owner;
	}
}

public struct PredictedState {

	public Players owner;
	public int cyborgCount;

	public PredictedState(Players owner, int cyborgCount) {
		this.owner = owner;
		this.cyborgCount = cyborgCount;			
	}

}

public class Factory : Entity {

	public int CyborgCount { get; private set; }
	public int Production { get; private set; }

	public Factory(int entityId, int owner, int cyborgCount, int production) : base(entityId, owner) {
		Debug.Assert(production <= 3);
		Debug.Assert(cyborgCount >= 0);

		this.CyborgCount = cyborgCount;
		this.Production = production;
	}
	

	//public PredictedState FuturePredictedState(GameState game) {
	//	int enemyIncomingTroops = game.Troops.Where(t1 => t1.Owner == Players.Opponent).Where(t2 => t2.Target == this.EntityId).Select(t3 => t3.TroopCount).Sum();
	//	int alliedIncomingTroops = game.Troops.Where(t1 => t1.Owner == Players.Me).Where(t2 => t2.Target == this.EntityId).Select(t3 => t3.TroopCount).Sum();

	//	if (Owner == Players.Neutral) {
	//		if (CyborgCount > enemyIncomingTroops + alliedIncomingTroops) {
	//			return new PredictedState(Players.Neutral, CyborgCount - enemyIncomingTroops - alliedIncomingTroops);
	//		} 
	//	}

	//	int maxEta = GetMaxIncomingTroopEta(game);

	//	for (int i=0; i<maxEta; i++) {

	//	}
		
	//	return 0;
	//}

	//public List<Troops> GetLastTroopsIncoming(GameState game) {
	//	return game.Troops.Where(t=>t.Eta == GetMaxIncomingTroopEta(game)).ToList();
	//}

	//public int GetMaxIncomingTroopEta(GameState game) {
	//	return GetIncomingTroops(game).Max(t => t.Eta);
	//}

	//public List<Troops> GetIncomingTroops(GameState game) {
	//	return game.Troops.Where(t => t.Target == this.EntityId).ToList();
	//}
}

public class Troops : Entity {

	public Factory Start { get; private set; }
	public Factory Target { get; private set; }
	public int TroopCount { get; private set; }
	public int Eta { get; private set; }

	public Troops(int entityId, int owner, Factory start, Factory target, int troopCount, int eta) : base(entityId, owner) {
		this.Start = start;
		this.Target = target;
		this.TroopCount = troopCount;
		this.Eta = eta;
	}
}

public class Bomb : Entity {

	public Factory Start { get; private set; }
	public Factory Target { get; private set; }
	public int Eta { get; private set; }


	public Bomb(int entityId, int owner, Factory start, Factory target, int eta) : base(entityId, owner) {
		this.Start = start;
		this.Target = target;
		this.Eta = eta;
	}

}
#endregion
		  