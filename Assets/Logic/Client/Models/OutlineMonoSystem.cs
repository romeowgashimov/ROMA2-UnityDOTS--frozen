using ROMA2.Logic.Client.Controllers;
using ROMA2.Logic.Client.Data;
using ROMA2.Logic.Data;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using static Unity.Entities.Entity;

namespace ROMA2.Logic.Client.Models
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(OutlineSystem))]
    public partial struct OutlineMonoSystem : ISystem
    {
        private static readonly Color OUTLINE_COLOR_RED = new(1f, 0.3f, 0.3f, 1f);
        private static readonly Color OUTLINE_COLOR_BLUE = new(0.3f, 0.7f, 1f, 1f);
        private static readonly Color OUTLINE_COLOR_DEFAULT = new(1f, 0.7f, 1f, 1f);
        
        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRO<SelectedEntity> selectedEntity, RefRO<Team> team, RefRW<LastOutlinedEntity> outlinedEntity) in 
                     SystemAPI.Query<RefRO<SelectedEntity>, RefRO<Team>, RefRW<LastOutlinedEntity>>()
                     .WithAll<GhostOwnerIsLocal>())
            {
                Entity selected = selectedEntity.ValueRO.Value;
                Entity outlined = outlinedEntity.ValueRO.Value;

                // Сброс старой обводки
                if (selected != outlined && outlined != Null)
                {
                    if (SystemAPI.ManagedAPI.HasComponent<OutlineControllerReference>(outlined))
                    {
                        OutlineController controller = SystemAPI.ManagedAPI
                            .GetComponent<OutlineControllerReference>(outlined)
                            .Value;
                        controller.ToggleOutline(false);
                        outlinedEntity.ValueRW.Value = Null;
                    }
                }

                if (selected == Null) continue;
                if (selected == outlinedEntity.ValueRO.Value) continue;

                if (!SystemAPI.ManagedAPI.HasComponent<OutlineControllerReference>(selected)) continue;
                
                OutlineController targetController = SystemAPI.ManagedAPI
                    .GetComponent<OutlineControllerReference>(selected)
                    .Value;
                targetController.ToggleOutline(true);
                
                Color targetColor;
                if (SystemAPI.HasComponent<Team>(selected))
                {
                    Team targetTeam = SystemAPI.GetComponent<Team>(selected);
                    targetColor = team.ValueRO.Value == targetTeam.Value 
                        ? OUTLINE_COLOR_BLUE
                        : OUTLINE_COLOR_RED;
                }
                else targetColor = OUTLINE_COLOR_DEFAULT;
                targetController.SetColor(targetColor);
                
                outlinedEntity.ValueRW.Value = selected;
            }
        }
    }
}