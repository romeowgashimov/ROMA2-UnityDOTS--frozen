using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace ROMA2.Logic.Common.Abilities
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct InitializeAbilityCommandSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndPredictedSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach((AbilityCommands abilityCommands, Entity owner) in SystemAPI
                    .Query<AbilityCommands>()
                    .WithEntityAccess())
            {
                // Пока два, так как умений сейчас только 2
                for (int i = 0; i < 2; i++)
                {
                    ecb.AddComponent<Owner>(abilityCommands[i], new()
                    {
                        Value = owner
                    });
                }
                // Лучше сделать компонент флаг, но пока так
                state.Enabled = false;
            }
        }
    }
}