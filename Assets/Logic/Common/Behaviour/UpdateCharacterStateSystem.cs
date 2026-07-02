using ROMA2.Logic.Data;
using ROMA2.Logic.Navigation;
using Unity.Burst;
using Unity.Entities;

namespace ROMA2.Logic.Common.Behaviour
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct UpdateCharacterStateSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRW<GhostCharacterState> charState, FollowPathProperties pathProperties,
                         AttackProperties attackProperties, EnabledRefRO<InAttackArea> inAttackArea) in SystemAPI
                         .Query<RefRW<GhostCharacterState>, FollowPathProperties, AttackProperties, 
                             EnabledRefRO<InAttackArea>>()
                         .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                CharacterState defaultState = CharacterState.Idle;
                if (!pathProperties.ReachedTheTarget) defaultState = CharacterState.Move;
                if (attackProperties.CanAttack) defaultState = CharacterState.Attack;
                charState.ValueRW.Value = defaultState;
            }
        }
    }
}