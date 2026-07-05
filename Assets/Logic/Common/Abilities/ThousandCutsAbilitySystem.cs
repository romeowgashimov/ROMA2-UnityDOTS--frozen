using ROMA2.Logic.Common.Databases;
using ROMA2.Logic.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using ROMA2.Logic.Common.Extensions;
using UnityEngine;

namespace ROMA2.Logic.Common.Abilities
{
    [UpdateInGroup(typeof(AbilityCommandSystemGroup))]
    public partial struct ThousandCutsAbilitySystem : ISystem
    {
        private static readonly float _maxDistance = 4f;
        private static readonly float _fullAngle = 120;
        private float _minRequiredDot;
        private CollisionFilter _filter;
        
        public void OnCreate(ref SystemState state)
        {
            float halfAngleRad = radians(_fullAngle / 2);
            _minRequiredDot = cos(halfAngleRad);
            _filter = new()
            {
                BelongsTo = 1 << 6,
                CollidesWith = 1 << 1 | 1 << 2 | 1 << 4
            };

            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NetworkTime netTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!netTime.IsFirstTimeFullyPredictingTick) return;

            EntityCommandBuffer ECB = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            bool isServer = state.WorldUnmanaged.IsServer();
            
            foreach ((ThousandCutsAbility anyCommand, AbilityCommand mainCommand, Owner owner,
                         Entity commandEntity) in SystemAPI
                         .Query<ThousandCutsAbility, AbilityCommand, Owner>()
                         .WithAll<Simulate>()
                         .WithEntityAccess())
            {
                ThousandCutsAbility ability = anyCommand;
                ability.AbilityIndex = mainCommand.AbilityIndex;
                ability.IsFirstUse = true;
                ability.NeedToConfirmAbilities = mainCommand.NeedToConfirmAbilities;

                ECB.AddComponent(owner.Value, ability);
                ECB.SetComponentEnabled<ThousandCutsAbility>(owner.Value, true);
                ECB.DestroyEntity(commandEntity);
            }

            foreach ((RefRW<ThousandCutsAbility> ability, Owner owner) in SystemAPI
                    .Query<RefRW<ThousandCutsAbility>, Owner>()
                    .WithAll<AbilityCommand, Prefab>()
                    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState | EntityQueryOptions.IncludePrefab))
            {
                AttackProperties attackProps = SystemAPI.GetComponent<AttackProperties>(owner.Value);
                if (attackProps.CanAttack 
                    && ability.ValueRO.CutCount < ability.ValueRO.MaxCutCount) ability.ValueRW.CutCount++;
            }

            state.Dependency = new ThousandCutsAbilityJob
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                TeamLookup = SystemAPI.GetComponentLookup<Team>(true),
                CollisionFilter = _filter,
                ECB = ECB.AsParallelWriter(),
                NetTime = netTime,
                IsServer = isServer,
                MaxDistance = _maxDistance,
                MinRequiredDot = _minRequiredDot,
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel(state.Dependency);
        }
    }

    public partial struct ThousandCutsAbilityJob : IJobEntity
    {
        [ReadOnly] public CollisionFilter CollisionFilter;
        [ReadOnly] public CollisionWorld CollisionWorld;
        [ReadOnly] public ComponentLookup<Team> TeamLookup;
        public float MaxDistance;
        public float MinRequiredDot;
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        public NetworkTime NetTime;
        public bool IsServer;

        private void Execute(
            [ChunkIndexInQuery] int key,
            ref ThousandCutsAbility ability,
            in MagicalPower magicalPower,
            ref DynamicBuffer<SendDamageElement> sendDamages,
            in AbilityInput abilityInput,
            ref AbilityCooldownTicks abilityCooldownTicks,
            in Team team, 
            ref ActivatedAbilitiesCommands activatedAbilitiesCommands,
            in LocalTransform transform, 
            ref DynamicBuffer<AbilityCooldownTargetTicks> cooldownTargetTicks,
            in AimInput aimInput, 
            in AbilityManaCost manaCosts,
            ref CurrentMana currMana,
            in AbilityCommands abilityCommands,
            Entity owner)
        {
            int abilityIndex = ability.AbilityIndex;

            // Проверка, нужно ли подтверждать умение
            if (ability.NeedToConfirmAbilities && ability.IsFirstUse)
            {
                if (!abilityInput.ConfirmAbility.IsSet)
                {
                    if (abilityInput.CancelAbility.IsSet) 
                    {
                        ECB.SetComponentEnabled<ThousandCutsAbility>(key, owner, false);
                        activatedAbilitiesCommands[abilityIndex] = false;
                        ability.IsFirstUse = true;
                    }
                    return;
                }
            }

            if (ability.IsFirstUse)
            {
                // Обновление перезарядки
                cooldownTargetTicks.UpdateCooldown(
                    abilityCooldownTicks, 
                    NetTime, 
                    abilityIndex, 
                    IsServer);

                currMana.Value -= manaCosts.GetManaCost(abilityIndex);
                ability.IsFirstUse = false;
            }

            ability.CutDelay += DeltaTime;
            while (ability.CutCount > 0 && ability.CutDelay >= ability.MaxCutDelay)
            {
                NativeList<DistanceHit> hits = new(Allocator.Temp);
                float3 selfPosition = transform.Position;
                int totalDamage = ability.MagicalDamage + ability.MagicalPercentage / 100 * magicalPower.Value;

                if (CollisionWorld.OverlapSphere(selfPosition, MaxDistance, ref hits, CollisionFilter))
                {
                    foreach (DistanceHit hit in hits)
                    {
                        if (!TeamLookup.TryGetComponent(hit.Entity, out Team enemyTeam) 
                            || enemyTeam.Value == team.Value) continue;

                        float3 direction = normalize(hit.Position - selfPosition);
                        float dotProduct = dot(aimInput.Value, direction);

                        SendDamageElement element = new()
                        {
                            Receiver = hit.Entity,
                            Owner = owner,
                            AbilityIndex = abilityIndex
                        };

                        if (dotProduct >= 0.9)
                            element.TrueDamage = totalDamage;
                        else if (dotProduct >= MinRequiredDot)
                            element.MagicalDamage = totalDamage;

                        sendDamages.Add(element);
                    }
                }

                ability.CutCount--;
                ability.CutDelay = 0f;
                hits.Dispose();
            }

            if (ability.CutCount == 0)
            {
                // По-хорошему конфиг иметь, ну пофиг
                ability.CutCount = 2;
                ECB.SetComponent(key, abilityCommands[abilityIndex], ability);
                ECB.SetComponentEnabled<ThousandCutsAbility>(key, owner, false);
                activatedAbilitiesCommands[abilityIndex] = false;
                ability.IsFirstUse = true;
            }
        }
    }
}   