using UnityEngine;
using Cysharp.Threading.Tasks;
using Game.Core.StateMachine;
using System.Threading;

namespace Game.Gameplay.Player
{
    public abstract class PlayerBaseState : State
    {
        protected readonly PlayerController Pc;
        protected readonly PlayerContext Ctx;
        protected readonly InputManager Input;
        protected CancellationToken CancellationToken => Pc.CancellationToken;   

        protected PlayerBaseState(State parent, PlayerController pc, InputManager input)
            : base(parent)
        {
            Pc = pc;
            Ctx = pc.Ctx;
            Input = input;
        }

        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnFixedUpdate(float deltaTime) { }
        protected virtual State GetInitialState() => null;
        protected virtual State GetTransition() => null;

        public override void Enter()
        {
            base.Enter();
            OnEnter();
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            OnUpdate(deltaTime);

            var target = GetTransition();
            if (target != null)
                Machine.Sequencer.RequestTransition(Leaf(), target);
        }

        protected virtual void OnUpdate(float deltaTime) { }

        public override void Exit()
        {
            OnExit();
            base.Exit();
        }

        public override void FixedUpdate(float deltaTime)
        {
            base.FixedUpdate(deltaTime);
            OnFixedUpdate(deltaTime);
        }



        #region Collision Checks

        protected bool CheckGrounded()
        {
            var feet = Pc.GetFeetCollider();
            if (feet == null) return false;

            Bounds b = feet.bounds;
            float castDist = Mathf.Max(Ctx.MoveStats.GroundDetectionRayLength, 0.15f);
            Vector2 origin = new Vector2(b.center.x, b.min.y);
            Vector2 size = new Vector2(b.size.x * 0.8f, 0.05f);
            int mask = Ctx.MoveStats.GroundLayer | Ctx.MoveStats.OneWayPlatform;

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, castDist, mask);
            if (hit.collider == null) return false;

            if (((1 << hit.collider.gameObject.layer) & Ctx.MoveStats.OneWayPlatform) != 0)
            {
                if (Ctx.Velocity.y > 0f) return false;
            }

            return true;
        }

        protected bool CheckHeadBump()
        {
            var body = Pc.GetBodyCollider();
            var feet = Pc.GetFeetCollider();
            if (body == null || feet == null) return false;

            Bounds bodyB = body.bounds;
            Bounds feetB = feet.bounds;
            float castDist = Ctx.MoveStats.HeadDetectionRayLength;

            Vector2 origin = new Vector2(feetB.center.x, bodyB.max.y);
            Vector2 size = new Vector2(feetB.size.x * Ctx.MoveStats.HeadWidth, 0.05f);
            int mask = Ctx.MoveStats.GroundLayer | Ctx.MoveStats.WallLayer;

            RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.up, castDist, mask);
            return hit.collider != null && Ctx.Velocity.y > 0f;
        }

        protected void CheckWalls()
        {
            var body = Pc.GetBodyCollider();
            if (body == null) return;

            Bounds b = body.bounds;
            float castDist = Ctx.MoveStats.WallDetectionRayLength;
            Vector2 size = new Vector2(0.05f, b.size.y);

            Ctx.IsTouchingWallLeft = Physics2D.BoxCast(
                new Vector2(b.min.x, b.center.y), size, 0f,
                Vector2.left, castDist, Ctx.MoveStats.WallLayer
            ).collider != null;

            Ctx.IsTouchingWallRight = Physics2D.BoxCast(
                new Vector2(b.max.x, b.center.y), size, 0f,
                Vector2.right, castDist, Ctx.MoveStats.WallLayer
            ).collider != null;
        }

        protected virtual void IgnoreOneWay(bool ignore)
        {
            if (Ctx.LastOneWayIgnoreState != ignore)
            {
                Physics2D.IgnoreLayerCollision(
                    Pc.GetPlayerLayer(),
                    Pc.GetOneWayLayer(),
                    ignore
                );
                Ctx.LastOneWayIgnoreState = ignore;
            }
        }

        #endregion

        #region Effects

        protected async UniTaskVoid TriggerDustEffect()
        {
            if (!Ctx.PlayDust) return;

            var dust = Pc.GetDust();
            if (dust == null) return;

            dust.transform.localScale = Vector3.one;
            dust.Emit(1);

            var tcs = new UniTaskCompletionSource();
            LeanTween.scale(dust.gameObject, Vector3.zero, 0.5f)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    if (dust != null) dust.Stop();
                    tcs.TrySetResult();
                });

            await UniTask.WhenAny(tcs.Task, UniTask.Delay(1000, cancellationToken: this.CancellationToken));

            Ctx.PlayDust = false;
        }

        #endregion
    }
}   