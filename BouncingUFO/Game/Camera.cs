using BouncingUFO.Game.Actors;
using Foster.Framework;
using System.Numerics;

namespace BouncingUFO.Game
{
    public class Camera(Manager manager)
    {
        private ActorBase? followedActor;
        private Vector2 position;

        public Point2 Position => position.FloorToPoint2();
        public Matrix3x2 Matrix => Matrix3x2.CreateTranslation(position);

        public void FollowActor(ActorBase? actor)
        {
            followedActor = actor;
        }

        public void Update(Point2 bounds)
        {
            if (followedActor == null || followedActor.Frame == null) return;

            var actorCenter = new RectInt(followedActor.Position, followedActor.Frame.Size).Center;
            position = manager.Screen.Bounds.Center - actorCenter;

            if (bounds != Point2.Zero)
            {
                var clampedPosition = position.Clamp(-(bounds - manager.Screen.Bounds.BottomRight), Vector2.Zero);
                position = new(
                    bounds.X < manager.Screen.Bounds.Right ? manager.Screen.Bounds.Center.X - bounds.X / 2 : clampedPosition.X,
                    bounds.Y < manager.Screen.Bounds.Bottom ? manager.Screen.Bounds.Center.Y - bounds.Y / 2 : clampedPosition.Y);
            }
        }
    }
}
