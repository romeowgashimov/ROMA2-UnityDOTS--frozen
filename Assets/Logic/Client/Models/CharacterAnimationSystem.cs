using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ROMA2.Logic.Client.Models
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct CharacterAnimationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach ((ModelReference modelReference, Entity entity) in SystemAPI
                        .Query<ModelReference>()
                        .WithAny<ChampTag, MinionTag>()
                        .WithNone<AnimatorReference, CachedCharacterState>()
                        .WithEntityAccess())
            {
                if (modelReference.Value.TryGetComponent(out Animator animator))
                    ecb.AddComponent<AnimatorReference>(entity, new() { Value = animator });
                
                ecb.AddComponent<CachedCharacterState>(entity);
            }

            foreach ((GhostCharacterState charState, AnimatorReference animatorReference,
                         RefRW<CachedCharacterState> cachedState, AttackSpeed attackSpeed) in SystemAPI
                         .Query<GhostCharacterState, AnimatorReference, RefRW<CachedCharacterState>, AttackSpeed>()
                         .WithAll<ModelReference>())
            {
                if (charState.Value == cachedState.ValueRO.Value) return;

                Animator animator = animatorReference.Value;
                cachedState.ValueRW.Value = charState.Value;
                switch (charState.Value)
                {
                    case CharacterState.Idle:
                        animator.SetFloat(AnimationNames.Speed, 0f);
                        break;
                    case CharacterState.Move:
                        animator.SetFloat(AnimationNames.Speed, 10f);
                        break;
                    case CharacterState.Attack:
                        animator.SetTrigger(AnimationNames.Attack);
                        break;
                }
            }

            foreach ((AnimatorReference reference, Entity entity) in SystemAPI
                        .Query<AnimatorReference>()
                        .WithNone<LocalTransform>()
                        .WithEntityAccess())
            {
                Object.Destroy(reference.Value);
                ecb.RemoveComponent<AnimatorReference>(entity);
            }
        }
    }
}