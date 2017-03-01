using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Player {

	static void Main(string[] args) {
		#region Initialization
		////////////////////////////// Initialization //////////////////////////////////
		string[] inputs;
		int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
		int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories

		Graph map = new Graph(factoryCount);
		Odinoo odinoo = new Odinoo();

		for (int i = 0; i < linkCount; i++) {
			inputs = Console.ReadLine().Split(' ');
			int factory1 = int.Parse(inputs[0]);
			int factory2 = int.Parse(inputs[1]);
			int distance = int.Parse(inputs[2]);

			map.AddConnection(factory1, factory2, distance);							
		}
		/////////////////////////////////////////////////////////////////////////////////
		#endregion

		// game loop
		while (true) {
			GameState game = null;
			List<Entity> entities = new List<Entity>();
			
			#region Read Game Info
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
				if (entityType == "BOMB") {
					Bomb bomb = new Bomb(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.FirstOrDefault(e => e.EntityId == arg3), arg4);
					entities.Add(bomb);
				}
			}
			#endregion

			game = new GameState(map, entities);
			string result = odinoo.Think(game);
			Console.WriteLine(result);
			//foreach (var ownedFactory in game.Factories.Where(f => f.Owner == Players.Me).OrderBy(f => f.Production)) {
			//	if (ownedFactory.CyborgCount >= 20 && ownedFactory.Production < 3) {
			//		action.AppendPowerup(ownedFactory.EntityId);
			//	}
			//}	
		}
	}
}
#region Game Data Structures
public class Odinoo {

	public IStrategy strategy;

	private int turn = 0;

	public Odinoo() {
		SwitchStrategy(new S_Powerful());
	}

	public void SwitchStrategy(IStrategy newStrategy) {
		this.strategy = newStrategy;		
	}

	public string Think(GameState game) {
		turn++;
		CommandBuilder action = new CommandBuilder();


		if (turn == 1) {
			Console.Error.WriteLine("Conqueror Mode");

			Conqueror_FirstTurn(game, ref action);
			action.AppendBomb(game.GetFactoriesOf(Players.Me).First(), game.GetFactoriesOf(Players.Opponent).First());
		} else {
			action = strategy.ExecuteStrategy(game, action);
		}


		return action.Result;
	}

	public void Conqueror_FirstTurn(GameState game, ref CommandBuilder action) {
		Factory myBestFactory = game.GetFactoriesOf(Players.Me).First();
		Console.Error.WriteLine("My best factory "+myBestFactory.EntityId+" has "+myBestFactory.CyborgCount);
		int troopsAvailable = myBestFactory.CyborgCount - 1;
		var factoriesToAttack = game.Factories
			.Where(f1 => f1.Owner == Players.Neutral)
			.Where(f2 => f2.Production >= 1)
			.OrderBy(f3 => game.Graph[myBestFactory, f3] )
			.ThenBy(f4 => f4.CyborgCount);

		foreach (var factory in factoriesToAttack) {
			//Console.Error.WriteLine("Attacking Factory " + factory.EntityId);
			action.AppendMove(myBestFactory, factory, factory.CyborgCount + 1);
			//Console.Error.WriteLine("Troops Before: " +troopsAvailable);
			troopsAvailable -= (factory.CyborgCount + 1);
			//Console.Error.WriteLine("Troops Afterwards: " + troopsAvailable);
			if (troopsAvailable <= 0) {
				break;
			}
		}					
	}

}

public interface IStrategy {
	CommandBuilder ExecuteStrategy(GameState game, CommandBuilder action);
}

public class S_Powerful : IStrategy {

	public CommandBuilder ExecuteStrategy(GameState game, CommandBuilder action) {
		List<Factory> myFactories = game.GetFactoriesOf(Players.Me).OrderByDescending(f1=>f1.CyborgCount).ToList();
		Factory myBestFactory = myFactories.First();


		var factoriesToAttack = game.Factories
			.Where(f1 => f1.Owner != Players.Me)
			.Where(f2 => f2.Production >= 1)
			.OrderBy(f4 => f4.CyborgCount)
			.ThenBy(f3 => game.Graph[myFactories.First(), f3]);

		var enemyFactory = factoriesToAttack.First();
		//foreach (var enemyFactory in factoriesToAttack) {
		List<Factory> myFactoriesCopy = new List<Factory>(myFactories);
		//Factory currFactory = myFactoriesCopy.First();
		//myFactoriesCopy.RemoveAt(0);
		//int troopsAvailable = currFactory.CyborgCount /2;

		Console.Error.WriteLine("Should I attack Factory " + enemyFactory.EntityId);

		int incomingTroopsToFactory = game.Troops.Where(t1 => t1.Target == enemyFactory).Aggregate(0, (agg, t2)=> agg + (int)t2.Owner * t2.TroopCount);
		Console.Error.WriteLine("Incoming Trops will alter " + incomingTroopsToFactory);

		int enemyFactoryCC = enemyFactory.CyborgCount + game.Graph[enemyFactory, myBestFactory] * enemyFactory.Production + incomingTroopsToFactory + 5;
		Console.Error.WriteLine("Estimated Troops to Conquer Factory " + enemyFactory + " is " + enemyFactoryCC);

		int troopsAvailable = 0;
		CommandBuilder conquerFactoryAction = new CommandBuilder();
		while(troopsAvailable < enemyFactoryCC) {
			if (myFactoriesCopy.Count > 0) {
				conquerFactoryAction.Clear();
				break;
			}

			var myFactory = myFactoriesCopy.First();
			myFactoriesCopy.RemoveAt(0);
			troopsAvailable += myFactory.CyborgCount / 3 * 2;
			conquerFactoryAction.AppendMove(myFactory, enemyFactory, myFactory.CyborgCount / 3 * 2);
		}
		
		return action.AppendActions(conquerFactoryAction);


		//action.AppendMove(myFactory, enemyFactory, enemyFactory.CyborgCount + 1);
		//Console.Error.WriteLine("Troops Before: " + troopsAvailable);
		//troopsAvailable -= (enemyFactory.CyborgCount + 1);
		//Console.Error.WriteLine("Troops Afterwards: " + troopsAvailable);
		//if (troopsAvailable <= 0) {
		//	break;
		//}
		//}

		return null;
			 
	}
}

public class S_IDontEvenKnow : IStrategy {

	public CommandBuilder ExecuteStrategy(GameState game, CommandBuilder action) {
		Console.Error.WriteLine("Standard Mode");
		//Bomb Command
		if (game.GetFactoriesOf(Players.Me).Count() <= 0) {
			return action;
		}

		var bombTarget = game.GetFactoriesOf(Players.Opponent)
			.Where(f1 => f1.CyborgCount >= 10 && f1.Production == 3)
			.OrderBy(f2 => game.Graph[f2.GetClosestFactory(game, Players.Me), f2])
			.FirstOrDefault();

		if (bombTarget != null) {
			action.AppendBomb(bombTarget.GetClosestFactory(game, Players.Me), bombTarget);
		}

		//Increase Commands
		foreach (var ownedFactory in game.Factories.Where(f => f.Owner == Players.Me).OrderBy(f => f.Production)) {
			if (ownedFactory.CyborgCount >= 20 && ownedFactory.Production < 3) {
				action.AppendPowerup(ownedFactory.EntityId);
			}
		}

		// Move Commands
		// Tuple represents Factory and its Strengh. Lower strength means easier to conquer
		List<Tuple<Factory, float>> sortedTargets = game.GetUnownedFactories(Players.Me)
			.Where(f3 => f3.Production > 0)
			.Select(f1 => Tuple.Create(f1, f1.CyborgCount / (f1.Production + float.Epsilon)))
			.OrderBy(f2 => f2.Item2)
			//.ThenBy(f3 => game.Graph[f3.Item1, f3.Item1.GetClosestFactory(game, Players.Me)])
			.ToList();

		Console.Error.WriteLine("sortedTargets");
		sortedTargets.ForEach(t => Console.Error.WriteLine(t.Item1.EntityId + " " + t.Item2));


		//If there is no factory to conquer, just wait. We're doing good
		if (sortedTargets.Count == 0) {
			return action;
		}

		float minStrength = sortedTargets.Min(f1 => f1.Item2);

		Factory target = sortedTargets
			.Where(f1 => f1.Item2 == minStrength)
			.OrderBy(f2 => f2.Item1.CyborgCount)
			.First().Item1;

		Factory myBestFactory = game.GetFactoriesOf(Players.Me).OrderByDescending(f1 => f1.CyborgCount).First();
		Console.Error.WriteLine("myBestFactory.EntityId " + myBestFactory.EntityId);
		Console.Error.WriteLine("target.EntityId" + target.EntityId);
		action.AppendMove(myBestFactory.EntityId, target.EntityId, Math.Min(Math.Max(0, myBestFactory.CyborgCount - 1), 4));

		return action;
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
	Neutral = 0, Me = 1, Opponent = -1
}

public class CommandBuilder {

	private string _result = "";
	public string Result {
		get { return "WAIT" + _result; }
	}

	//public void AppendMove(Factory start, Factory target, int count) {
	//	Result += string.Format("MOVE {0} {1} {2};", start.EntityId, target.EntityId, count); 		
	//}

	public void Clear() {
		_result = "";
	}

	public CommandBuilder AppendActions(CommandBuilder actions) {
		CommandBuilder resultCommandBuilder = new CommandBuilder();
		resultCommandBuilder._result = this._result;
		resultCommandBuilder._result = actions._result;
		if (actions._result == "") {
			resultCommandBuilder._result = actions._result;
		} else {
			resultCommandBuilder._result += ";" + actions._result;
		}
		return resultCommandBuilder;
	}

	public void AppendMove(int startId, int targetId, int count) {
		_result += string.Format(";MOVE {0} {1} {2}", startId, targetId, count);
	}

	public void AppendBomb(int startId, int targetId) {
		_result += string.Format(";BOMB {0} {1}", startId, targetId);
	}

	public void AppendPowerup(int factoryId) {
		_result += ";INC " + factoryId;
	}

}

public class GameState {

	public List<Entity> Entities { get; private set; }
	public List<Factory> Factories { get; private set; }
	public List<Troops> Troops { get; private set; }
	public Graph Graph { get; private set; }

	public GameState(Graph mapGraph, List<Entity> entities) {
		this.Graph = mapGraph;
		this.Entities = entities;
		this.Factories = entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = entities.Where(e => e is Troops).Cast<Troops>().ToList();
	}

	public Factory GetFactory(int factoryId) {
		return Factories.First(f => f.EntityId == factoryId);
	}

	public List<Factory> GetFactoriesOf(Players owner) {
		return Factories.Where(f => f.Owner == owner).ToList();
	}

	public List<Factory> GetUnownedFactories(Players owner) {
		return Factories.Where(f => f.Owner != owner).ToList();
	}

	public int GetProduction(Players owner) {
		return Factories.Where(f1 => f1.Owner == owner).Select(f2 => f2.Production).Sum();
	}


	public GameState AdvanceInputless(int noOfTurns) {
		return AdvanceInputless_Listed(noOfTurns).Last();
	}

	public List<GameState> AdvanceInputless_Listed(int noOfTurns) {
		List<GameState> result = new List<GameState>();
		for (int i = 0; i < noOfTurns; i++) {
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

	public Factory GetClosestFactory(GameState game, Players owner) {
		return game.Factories.Where(f1 => f1.Owner == owner).OrderBy(f2 => game.Graph[this, f2]).FirstOrDefault();
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
