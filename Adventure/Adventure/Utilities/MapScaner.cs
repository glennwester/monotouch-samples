using System;
using System.Drawing;

namespace Adventure
{
	public class MapScaner
	{
		private readonly int _mapWidth;
		private readonly byte[] _map;

		public MapScaner(byte[] mapData, int mapWidth)
		{
			_map = mapData;
			_mapWidth = mapWidth;
		}

		public DataMap QueryLevelMap(Point point)
		{
			// Calc start index of pixel info
			int index = ConvertToIndex (point);

			return new DataMap {
				BossLocation = _map [index],			// Alpha chanel
				Wall = _map [++index],					// Red
				GoblinCaveLocation = _map [++index],	// Green
				HeroSpawnLocation = _map [++index]		// Blue
			};
		}

		public TreeMap QueryTreeMap(Point point)
		{
			// Calc start index of pixel info
			int index = ConvertToIndex (point);

			return new TreeMap {
				UnusedA = _map [index],				// Alpha chanel
				BigTreeLocation = _map [++index],	// Red
				SmallTreeLocation = _map [++index],	// Green
				UnusedB = _map [++index]			// Blue
			};
		}

		private int ConvertToIndex(Point point)
		{
			// One pixel take 4 bytes (Alpha + R + G + B)
			int index = 4 * (point.Y * _mapWidth + point.X);
			return index;
		}
	}
}

