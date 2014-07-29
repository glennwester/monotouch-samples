using System;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace HelloGoodbye
{
	public class AAPLPerson
	{
		private const string AAPLPersonPhotoKey = "photo";
		private const string AAPLPersonAgeKey = "age";
		private const string AAPLPersonHobbiesKey = "hobbies";
		private const string AAPLPersonElevatorPitchKey = "elevatorPitch";

		public UIImage Photo { get; set; }
		public int Age { get; set; }
		public string Hobbies { get; set; }
		public string ElevatorPitch { get; set; }

		public static AAPLPerson PersonFromDictionary(NSDictionary dict)
		{
			AAPLPerson person = new AAPLPerson {
				Photo = UIImage.FromBundle((string)(NSString)dict[AAPLPersonPhotoKey]),
				Age = ((NSNumber)dict[AAPLPersonAgeKey]).Int32Value,
				Hobbies = (string)(NSString)dict[AAPLPersonHobbiesKey],
				ElevatorPitch = (string)(NSString)dict[AAPLPersonElevatorPitchKey],
			};

			return person;
		}
	}
}

