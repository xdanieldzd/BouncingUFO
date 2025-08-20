using Foster.Framework;
using GameTest1.Game.Actors;
using System.Numerics;

namespace GameTest1.Game
{
    public class Camera(Manager manager)
    {
        private ActorBase? followedActor;
        private Vector2 position;

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
                if (bounds.X < manager.Screen.Bounds.Right || bounds.Y < manager.Screen.Bounds.Bottom)
                    position = manager.Screen.Bounds.Center - bounds / 2;
                else
                    position = position.Clamp(-(bounds - manager.Screen.Bounds.BottomRight), Vector2.Zero);
            }
        }
    }
}
