using System.Runtime.Serialization;

namespace RobotArmServer.Models
{
    [DataContract]
    public class RobotArmState
    {
        [DataMember] public int X { get; set; }

        [DataMember] public int Y { get; set; }

        [DataMember] public int Angle { get; set; }

        public RobotArmState()
        {
        }

        public RobotArmState(RobotArm arm)
        {
            X = arm.X;
            Y = arm.Y;
            Angle = arm.Angle;
        }

        public RobotArmState(int x, int y, int angle)
        {
            X = x;
            Y = y;
            Angle = angle;
        }

        public override string ToString()
        {
            return $"Position=({X},{Y}), Rotation={Angle}°";
        }
    }
}