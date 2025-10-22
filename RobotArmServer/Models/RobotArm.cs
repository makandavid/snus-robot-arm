namespace RobotArmServer.Models
{
    public class RobotArm
    {
        public int X { get; private set; } = 2;
        public int Y { get; private set; } = 2;
        public int Rotation { get; private set; } = 0;

        public bool MoveLeft() => TryMove(-1, 0);
        public bool MoveRight() => TryMove(1, 0);
        public bool MoveUp() => TryMove(0, -2);
        public bool MoveDown() => TryMove(0, 2);
        public bool Rotate()
        {
            Rotation = (Rotation + 90) % 360;
            return true;
        }

        private bool TryMove(int dx, int dy)
        {
            int newX = X + dx;
            int newY = Y + dy;

            // the grid is 5x5
            if (newX < 0 || newX > 4 || newY < 0 || newY > 4) { return false; }

            X = newX;
            Y = newY;
            return true;
        }

        public override string ToString() => $"Position=({X},{Y}), Rotation={Rotation}°";
    }
}