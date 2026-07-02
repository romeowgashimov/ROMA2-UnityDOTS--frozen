using ROMA2.Logic.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace ROMA2.Logic.Common.Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct BasicMeleeAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameplayingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            state.Dependency = new BasicMeleeAttackJob
            {
                CurrentTick = networkTime.ServerTick,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true)
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate), typeof(InAttackArea), typeof(MeleeAttack))]
    public partial struct BasicMeleeAttackJob : IJobEntity
    {
        private const int SIMULATION_TICK_RATE = 60;
        
        [ReadOnly] public NetworkTick CurrentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        [BurstCompile]
        private void Execute(
            ref DynamicBuffer<AttackCooldown> attackCooldown,
            in TargetEntity targetEntity, 
            Entity owner,
            in Team team,
            in AttackSpeed attackSpeed, 
            in PhysicalPower physicalPower,
            ref AttackProperties properties,
            ref DynamicBuffer<SendDamageElement> sendDamages)
        {
            if (!TransformLookup.HasComponent(targetEntity.Value)) return;
            if (!attackCooldown.GetDataAtTick(CurrentTick, out AttackCooldown cooldownExpirationTick))
                cooldownExpirationTick.Value = NetworkTick.Invalid;
            
            properties.CanAttack = !cooldownExpirationTick.Value.IsValid
                         || CurrentTick.IsNewerThan(cooldownExpirationTick.Value);
            if (!properties.CanAttack) return;

            int totalDamage = physicalPower.Value;
            
            sendDamages.Add(new()
            {
                PhysicalDamage = totalDamage,
                Receiver = targetEntity.Value,
                Owner = owner,
                AbilityIndex = -1
            });
            
            NetworkTick newCooldownTick = CurrentTick;
            newCooldownTick.Add((uint)attackSpeed.Value * SIMULATION_TICK_RATE);
            attackCooldown.AddCommandData(new() { Tick = CurrentTick, Value = newCooldownTick });
        }
    }
}