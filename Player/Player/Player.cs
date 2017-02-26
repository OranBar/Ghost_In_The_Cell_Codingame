using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

class Player {
	static void Main(string[] args) {
		string[] inputs;
		int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
		int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
		for (int i = 0; i < linkCount; i++) {
			inputs = Console.ReadLine().Split(' ');
			int factory1 = int.Parse(inputs[0]);
			int factory2 = int.Parse(inputs[1]);
			int distance = int.Parse(inputs[2]);
		}

		// game loop

		while (true) {
			GameState game = null;
			List<Entity> entities = new List<Entity>();

			int weakestFactory = -1;
			int weakestFactoryCount = int.MaxValue;

			int mostProductiveUnownedFactory = -1;
			int bestUnownedFactoryProduction = -1;
			int bestUnownedFactoryCyborgCount = int.MaxValue;

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

					if (owner == 1) {
						if (arg2 > myBestFactoryCount) {
							myBestFactory = entityId;
							myBestFactoryCount = arg2;
						}
					} else {
						if (arg2 < weakestFactoryCount) {
							weakestFactory = entityId;
							weakestFactoryCount = arg2;
						}
						if (arg3 >= bestUnownedFactoryProduction) {
							mostProductiveUnownedFactory = entityId;
							bestUnownedFactoryProduction = arg3;
							bestUnownedFactoryCyborgCount = arg2;
							if (arg3 == bestUnownedFactoryProduction && arg2 < bestUnownedFactoryCyborgCount) {
								mostProductiveUnownedFactory = entityId;
								bestUnownedFactoryProduction = arg3;
								bestUnownedFactoryCyborgCount = arg2;
							}
						}
					}
				}
				if (entityType == "TROOP") {
					Troops troops = new Troops(entityId, owner, (Factory)entities.First(e => e.EntityId == arg2), (Factory)entities.First(e => e.EntityId == arg3), arg4, arg5);
					entities.Add(troops);
				}
			}
			game = new GameState(entities);

			// Write an action using Console.WriteLine()
			// To debug: Console.Error.WriteLine("Debug messages...");
			if (weakestFactory == -1) {
				// Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
				Console.WriteLine("WAIT");
			} else {
				if (Math.Min(myBestFactoryCount - 1, 4) < 0) {
					Console.WriteLine("WAIT");
				} else {
					Console.WriteLine(string.Format("MOVE {0} {1} {2}", myBestFactory, mostProductiveUnownedFactory, Math.Min(myBestFactoryCount - 1, 4)));
				}
			}
		}
	}
}
#region Game Data Structures
public enum Players {
	Neutral = 0, Me = 1, Opponent = 2
}

public class GameState {

	public List<Entity> Entities { get; private set; }
	public List<Factory> Factories { get; private set; }
	public List<Troops> Troops { get; private set; }

	public GameState(List<Entity> entities) {
		this.Entities = entities;
		this.Factories = entities.Where(e => e is Factory).Cast<Factory>().ToList();
		this.Troops = entities.Where(e => e is Troops).Cast<Troops>().ToList();
	}
}

public abstract class Entity {

	public int EntityId { get; private set; }
	public Players Owner { get; private set; }


	public Entity(int entityId, int owner) {
		this.EntityId = entityId;
		this.Owner = (Players)owner;
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
}

public class Troops : Entity {

	public Factory Home { get; private set; }
	public Factory Target { get; private set; }
	public int TroopCount { get; private set; }
	public int Eta { get; private set; }

	public Troops(int entityId, int owner, Factory home, Factory target, int troopCount, int eta) : base(entityId, owner) {
		this.Home = home;
		this.Target = target;
		this.TroopCount = troopCount;
		this.Eta = eta;
	}
}
#endregion